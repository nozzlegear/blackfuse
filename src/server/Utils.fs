module Utils

open System
open Suave

/// Takes a URL string, ensures it has a protocol and sets the path. Defaults to http for localhost and https for anything else, but
/// will not alter any string that already has an http or https protocol. Additionally, any existing path will be overwritten, not appended to.
let withPathAndProtocol path (url': string) =
    let url = url'.ToLower()

    if url.StartsWith Uri.UriSchemeHttps || url.StartsWith Uri.UriSchemeHttps then
        url
    else
        if url.Contains "localhost" then Uri.UriSchemeHttp else Uri.UriSchemeHttps
        |> fun protocol -> sprintf "%s%s%s" protocol Uri.SchemeDelimiter url
    |> UriBuilder
    |> fun u ->
        // Set the port to -1 when not on localhost, which will prevent it from showing when doing uri.ToString()
        u.Port <- if u.Port = 443 || u.Port = 80 then -1 else u.Port
        u.Path <- path

        u.Uri

/// The same as withPathAndProtocol but with its arguments reversed.
let withPathAndProtocolBack url path = withPathAndProtocol path url

/// Combines a request's host header into an absolute URL with path and protocol using that same header as the domain.
let toAbsoluteUrl (req: HttpRequest) =
    if not ServerConstants.isLive
    then withPathAndProtocolBack "localhost:8000"
    else
        // Get the app's domain so we can combine it with the oauth redirect path but not have to hardcode localhost/live domain
        match req.header "host" with
        | Choice1Of2 h ->
            // Make sure the uri has a protocol and host. In most cases the raw string does not have a protocol,
            // and passing "localhost:3000" to a uribuilder makes it think there's no host either.
            withPathAndProtocolBack h
        | Choice2Of2 _ ->
            Errors.HttpException("Unable to determine host URL.", Status.InternalServerError)
            |> raise
