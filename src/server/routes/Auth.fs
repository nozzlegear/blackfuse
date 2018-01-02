module Routes.Auth

open System
open Domain
open Suave
open Filters
open Operators
open ShopifySharp
open Errors

let createSessionCookie (data: Requests.Auth.LoginOrRegister) =
    // Create a cookie that has no (practical) expiration date. Auth expiration is instead dictated by the JWT token
    let cookieExpiration = DateTimeOffset.UtcNow.AddYears 10

    { email = "temporary@your-shopify-store.com"
      created = Date.toUnixTimestamp <| DateTime.UtcNow.AddDays -90.
      hashedPassword = "temp"
      id = "abcd-1234" }
    |> Jwt.encode Jwt.DefaultExpiration
    |> HttpCookie.createKV Constants.CookieName
    |> fun c -> { c with httpOnly = false; expires = Some cookieExpiration }

let getShopifyOAuthUrl = request <| fun req ctx -> async {
    let body =
        req.rawForm
        |> Json.parseFromBody<Requests.Auth.LoginOrRegister>
        |> fun t -> t.Validate()
        |> function
        | Ok b -> b
        | Error e -> raise <| Errors.fromValidation e

    let! isValidDomain = AuthorizationService.IsValidShopDomainAsync body.domain |> Async.AwaitTask

    if not isValidDomain then
        HttpException ("The domain you entered is not a valid Shopify shop's domain.", Status.UnprocessableEntity)
        |> raise

    let redirectUrl =
        if not ServerConstants.isLive then
            Utils.withPathAndProtocolBack "localhost:8000"
        else
            // Get the app's domain so we can combine it with the oauth redirect path but not have to hardcode localhost/live domain
            match req.header("host") with
            | Choice1Of2 h ->
                // Make sure the uri has a protocol and host. In most cases the raw string does not have a protocol,
                // and passing "localhost:3000" to a uribuilder makes it think there's no host either.
                Utils.withPathAndProtocolBack h
            | Choice2Of2 _ ->
                Errors.HttpException("Unable to determine host URL.", Status.InternalServerError)
                |> raise
        <| ServerConstants.shopifyOauthRedirectPath

    // Sending the user to the oauth url will let us onboard them if they haven't installed the app,
    // and let us log them in if they have.
    let oauthUrl =
        AuthorizationService.BuildAuthorizationUrl(
            ServerConstants.authScopes,
            body.domain,
            ServerConstants.shopifyApiKey,
            redirectUrl.ToString())

    return!
        Successful.OK <| sprintf """{"url":"%s"}""" (oauthUrl.ToString())
        >=> Writers.setMimeType Json.MimeType
        <| ctx
}

let login = request <| fun req ctx -> async {
    let body =
        req.rawForm
        |> Json.parseFromBody<Requests.Auth.LoginOrRegister>
        |> fun t -> t.Validate()
        |> function
        | Ok b -> b
        | Error e -> raise <| fromValidation e

    // TODO: Lookup user in database, validate their password with bcrypt

    return!
        Successful.OK "{}"
        >=> Writers.setMimeType Json.MimeType
        >=> Cookie.setCookie (createSessionCookie body)
        <| ctx
}

let checkUserState = context <| fun ctx ->
    printfn "User state: %A" ctx.userState
    Successful.OK "This required auth and a user state"

let routes = [
    POST >=> choose [
        path "/api/v1/auth" >=> login
        path "/api/v1/auth/get-shopify-oauth-url" >=> getShopifyOAuthUrl
    ]
]