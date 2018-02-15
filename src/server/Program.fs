// Learn more about F# at http://fsharp.org

open System
open Suave
open Suave.Operators
open Errors
open System.Text.RegularExpressions
open Suave.Files
open System.IO

let errorHandler (err: Exception) (msg: string) ctx =
    let errorResponse =
        match err with
        | :? HttpException as ex -> ex.toErrorResponse()
        | _ ->
            // Call the default error handler to preserve default logging behavior
            defaultConfig.errorHandler err msg ctx
            |> ignore

            let err = serverError msg
            err.toErrorResponse()

    Writers.writeJson errorResponse.statusCode errorResponse ctx

let wildcardRoute = request (fun req ->
    let apiRegex = Regex "(?i)^/?api/"
    let publicRegex = Regex "(?i)^/?public/.*"
    let faviconRegex = Regex "(?i)^/?favicon\.ico"

    match req.path with
    | p when apiRegex.IsMatch p ->
        // Request to API path fell through, route was not found.
        sprintf "No API route found at path %A %s" req.method req.path
        |> notFound
        |> raise

    | p when publicRegex.IsMatch p ->
        // User is requesting a file from the public folder
        // Trim the leading slash from req.path, else Path.Combine will ignore everything and just return the req.path because it looks absolute.
        let pathWithoutSlash = if req.path.StartsWith "/" then req.path.Substring 1 else req.path
        let filename = Path.Combine(Folder.publicFolder, "../", pathWithoutSlash)

        if not <| File.Exists filename
        then raise <| notFound (sprintf "File %s does not exist." p)
        else
            let mime, compression =
                Path.GetExtension filename
                |> Suave.Writers.defaultMimeTypesMap
                |> Option.map (fun m -> m.name, m.compression)
                |> Option.defaultValue ("application/octet-stream", false)

            sendFile filename compression
            >=> Writers.setMimeType mime

    | p when faviconRegex.IsMatch p ->
        // Some browsers automatically send a request to /favicon.ico, despite what you might specify in your html.
        browseFile Folder.publicFolder "images/favicon-16x16.png"

    | _ ->
        // Wildcard, send the index.html and let the client figure out its own 404s
        let indexPath = System.IO.Path.Combine(Folder.publicFolder, "index.html")

        sendFile indexPath true
)

[<EntryPoint>]
let main _ =
    printfn "Configuring CouchDB databases."
    Database.configureDatabases |> Async.RunSynchronously
    printfn "Databases configured."

    let allRoutes =
        Routes.Auth.routes
        @Routes.Billing.routes
        @Routes.Orders.routes
        @Routes.Webhooks.routes
        @[wildcardRoute] // Wildcard should come last
        |> choose

    let config =
        { defaultConfig with
            errorHandler = errorHandler
            bindings = [HttpBinding.createSimple Protocol.HTTP "0.0.0.0" 3000] }

    Suave.Web.startWebServer config allRoutes

    0 // return an integer exit code
