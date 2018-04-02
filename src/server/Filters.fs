module Filters

open Suave
open Suave.Cookie
open ShopifySharp
open System.Collections.Generic
open Microsoft.Extensions.Primitives

let forbidden = 
    let error = 
        Errors.forbidden "You are not authorized to access that resource. Please try logging in again."
        |> Errors.toErrorResponse

    Writers.writeJson error.statusCode error 

/// Verifies that the request is authenticated via JWT cookie, then passes the SessionToken to the next function if so.
/// ```
/// let myRoute = withSession <| fun user req ctx -> async {
///
/// }
/// ```
let withSession part: HttpContext -> Async<HttpContext option> = request <| fun req ctx -> async {
    return!
        req.cookies.TryFind Constants.CookieName
        |> Option.bind (fun c -> Session.tryDecode c.value)
        |> Option.map (fun session -> 
            // Pull the latest version of the session from the database to make sure it hasn't been deleted (invalidated)
            Database.getSession (Database.CouchPerUser.UserId session.user.id) session.id None 
            |> Async.Bind (Option.map (fun _ -> part session req ctx) >> Option.defaultValue (forbidden ctx))
        )
        |> Option.defaultValue (forbidden ctx)
}

/// Uses `withSession` to verify that the request is authenticated via JWT cookie, then pulls the full User model from the database and passes it along with the verified session record to the next function.
/// ```
/// let myRoute = withUserAndSession <| fun user session req ctx -> async {
///
/// }
/// ```
let withUserAndSession part: HttpContext -> Async<HttpContext option> = withSession <| fun session req ctx -> async {
    let! user = Database.getUserById session.user.id (Some session.user.rev)

    return!
        user
        |> Option.map (fun u -> part u session req ctx)
        |> Option.defaultValue (forbidden ctx)
}

/// Validates a Shopify webhook request before calling the next part.
let validShopifyWebhook part = request <| fun req ctx -> async {
    let reqBody = System.Text.Encoding.UTF8.GetString req.rawForm
    let headers =
        req.headers
        |> Seq.map (fun (name, value) -> KeyValuePair<string, StringValues>(name, StringValues value))

    return!
        match AuthorizationService.IsAuthenticWebhook(headers, reqBody, ServerConstants.shopifySecretKey) with
        | false -> forbidden ctx
        | true -> part ctx
}

open Paths

/// Parses a path using the UrlParser module and applies its arguments to the next webpart.
let parsePath (p: Path<'a>) (part: 'a -> HttpContext -> Async<HttpContext option>) (x: HttpContext): Async<HttpContext option> = 
    p.Parse x.request.path x.request.rawQuery 
    |> Option.map (fun arg -> part arg x)
    |> Option.defaultValue (Async.Return None)

/// Operator shortcut for Filters.parsePath
let inline (>@>) path part = parsePath path part    

/// Operator shortcut for Filters.parsePath, but it drops the value parsed from the path.
let inline (>@->) path part = parsePath path (fun _ -> part)