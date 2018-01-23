// Learn more about F# at http://fsharp.org

open System
open Suave
open Suave.Operators
open Errors
open Domain
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

    let httpCode =
        match HttpCode.tryParse errorResponse.statusCode with
        | Choice1Of2 code -> code
        | Choice2Of2 msg ->
            printfn "Failed to parse statusCode %i to Suave HttpCode: %s" errorResponse.statusCode msg
            HTTP_500

    errorResponse
    |> Json.stringify
    |> Successful.OK
    >=> Writers.setStatus httpCode
    >=> Writers.setMimeType Json.MimeType
    <| ctx


let resolvePath (rootPath : string) (fileName : string) =
    let fileName =
      if Path.DirectorySeparatorChar.Equals('/') then fileName
      else fileName.Replace('/', Path.DirectorySeparatorChar)
    let calculatedPath =
      Path.Combine(rootPath, fileName.TrimStart([| Path.DirectorySeparatorChar; Path.AltDirectorySeparatorChar |]))
      |> Path.GetFullPath

    printfn "CALCULATED PATH IS %s" calculatedPath
    printfn "ROOT PATH IS %s" rootPath

    if calculatedPath.StartsWith rootPath then
      calculatedPath
    else raise <| Exception("File canonalization issue.")

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
        browseFile (Path.Combine(Folder.publicFolder, "../")) req.path
        // raise <| System.NotImplementedException("Public path not implemented")

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
        @Routes.Webhooks.routes
        @[wildcardRoute] // Wildcard should come last
        |> choose

    let config =
        { defaultConfig with
            errorHandler = errorHandler
            bindings = [HttpBinding.createSimple Protocol.HTTP "0.0.0.0" 3000] }

    Suave.Web.startWebServer config allRoutes

    0 // return an integer exit code
