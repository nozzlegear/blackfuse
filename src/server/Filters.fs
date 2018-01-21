module Filters

open Suave
open Suave.Cookie
open ShopifySharp

let forbidden = RequestErrors.FORBIDDEN "You are not authorized to access that resource."

/// Verifies that the request is authenticated via JWT cookie, then passes the SessionToken to the next function if so.
/// ```
/// let myRoute = withSession <| fun user req ctx -> async {
///
/// }
/// ```
let withSession part: HttpContext -> Async<HttpContext option> = request <| fun req ctx -> async {
    return!
        req.cookies.TryFind Constants.CookieName
        |> Option.bind (fun c -> Jwt.tryDecode c.value)
        |> Option.map (fun t -> part t req ctx)
        |> Option.defaultValue (forbidden ctx)
}

/// Uses `withSession` to verify that the request is authenticated via JWT cookie, then pulls the full User model from the database and passes it to the next function.
/// ```
/// let myRoute = withUser <| fun user req ctx -> async {
///
/// }
/// ```
let withUser part: HttpContext -> Async<HttpContext option> = withSession <| fun user req ctx -> async {
    let! user = Database.getUserById user.id (Some user.rev)

    return!
        user
        |> Option.map (fun u -> part u req ctx)
        |> Option.defaultValue (forbidden ctx)
}

/// Validates a Shopify webhook request before calling the next part.
let validShopifyWebhook part = request <| fun req ctx -> async {
    let reqBody = System.Text.Encoding.UTF8.GetString req.rawForm
    let headers =
        req.headers
        |> Seq.cast<System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>>

    return!
        match AuthorizationService.IsAuthenticWebhook(headers, reqBody, ServerConstants.shopifySecretKey) with
        | false -> forbidden ctx
        | true -> part ctx
}