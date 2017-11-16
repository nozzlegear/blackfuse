// Learn more about F# at http://fsharp.org

open System
open Suave
open Suave.Operators
open Exceptions
open Domain
open System.Text.RegularExpressions
open Suave.Files

[<EntryPoint>]
let main argv =
    let errorHandler (err: Exception) (msg: string) ctx =
        let errorResponse =
            match err with
            | :? HttpException as ex -> ex.toErrorResponse()
            | _ ->
                // Call the default error handler to preserve default logging behavior
                defaultConfig.errorHandler err msg ctx |> ignore
                ErrorResponse.FromStatus msg Status.Code.InternalServerError
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
        >=> Writers.setMimeType "application/json"
        <| ctx

    let wildcardRoute = request (fun req ->
        let apiRegex = Regex "(?i)^api/"
        let publicRegex = Regex "(?i)^public/.*"
        let faviconRegex = Regex "(?i)^favicon\.ico"

        match req.path with
        | p when apiRegex.IsMatch p ->
            // Request to API path fell through, route was not found.
            raise <| HttpException(sprintf "No API route found at path %s" req.path, Status.Code.NotFound)
        | p when publicRegex.IsMatch p ->
            // TODO: Check if file exists, throw exception if it doesn't
            raise <| System.NotImplementedException("Public path not implemented")
        | p when faviconRegex.IsMatch p ->
            // Some browsers automatically send a request to /favicon.ico, despite what you might specify in your html.
            raise <| System.NotImplementedException("Favicon path not implemented")
        | _ ->
            // Wildcard, send the index.html and let the client figure out its own 404s
            let indexPath = System.IO.Path.Combine(Folder.publicFolder, "index.html")

            sendFile indexPath true
    )

    let config =
        { defaultConfig with
            errorHandler = errorHandler
            bindings = [HttpBinding.createSimple Protocol.HTTP "0.0.0.0" 3000] }

    Suave.Web.startWebServer config (choose [wildcardRoute])

    0 // return an integer exit code
