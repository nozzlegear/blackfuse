module Filters

open Suave
open Suave.Cookie
open ShopifySharp

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
        |> Option.defaultValue (RequestErrors.FORBIDDEN "You are not authorized to access that resource." ctx)
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
        |> Option.defaultValue (RequestErrors.FORBIDDEN "You are not authorized to access that resource." ctx)
}

let validShopifyRequest part = request <| fun req ctx ->
    match AuthorizationService.IsAuthenticRequest(req.rawQuery, ServerConstants.shopifySecretKey) with
    | true -> part ctx
    | false -> RequestErrors.FORBIDDEN "Request did not pass Shopify's web request authorization scheme." ctx