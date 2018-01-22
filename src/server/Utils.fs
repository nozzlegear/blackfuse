module Utils

open System

/// Takes a URL string, ensures it has a protocol and sets the path. Defaults to http for localhost and https for anything else, but
/// will not alter any string that already has an http or https protocol. Additionally, any existing path will be overwritten, not appended to.
let withPathAndProtocol path (url: string) =
    let url = url.ToLower()

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

/// Combines the app's domain with the given path into an absolute URL with a protocol.
let toAbsoluteUrl = withPathAndProtocolBack ServerConstants.domain