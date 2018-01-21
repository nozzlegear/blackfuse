module Routes.Auth

open System
open Domain
open Suave
open Filters
open Operators
open ShopifySharp
open Errors

let createSessionCookie (user: Domain.User) =
    // Create a cookie that has no (practical) expiration date. Auth expiration is instead dictated by the JWT token
    let cookieExpiration = DateTimeOffset.UtcNow.AddYears 10

    user
    |> Jwt.encode Jwt.DefaultExpiration
    |> HttpCookie.createKV Constants.CookieName
    |> fun c -> { c with httpOnly = false; expires = Some cookieExpiration }

let getShopifyOAuthUrl = request <| fun req ctx -> async {
    let queryKey = "domain"
    let domain =
        match req.queryParam queryKey with
        | Choice1Of2 s -> s
        | Choice2Of2 _ -> raise <| HttpException(sprintf "Missing querystring key %s." queryKey, Status.UnprocessableEntity)

    let! isValidDomain = AuthorizationService.IsValidShopDomainAsync domain |> Async.AwaitTask

    if not isValidDomain then
        HttpException ("The domain you entered is not a valid Shopify shop's domain.", Status.UnprocessableEntity)
        |> raise

    // Sending the user to the oauth url will let us onboard them if they haven't installed the app,
    // and let us log them in if they have.
    let oauthUrl =
        AuthorizationService.BuildAuthorizationUrl(
            ServerConstants.authScopes,
            domain,
            ServerConstants.shopifyApiKey,
            Utils.toAbsoluteUrl req Paths.Auth.completeOAuth |> string)

    return!
        Successful.OK <| sprintf """{"url":"%s"}""" (oauthUrl.ToString())
        >=> Writers.setMimeType Json.MimeType
        <| ctx
}

let shopifyLoginOrRegister = request <| fun req ctx -> async {
    let body =
        req.rawForm
        |> Json.parseFromBody<Requests.OAuth.CompleteShopifyOauth>
        |> fun t -> t.Validate()
        |> function
        | Ok b -> b
        | Error e -> raise <| fromValidation e

    if not <| AuthorizationService.IsAuthenticRequest(body.rawQueryString, ServerConstants.shopifySecretKey) then
        raise <| HttpException("Request did not pass Shopify's validation scheme.", Status.Forbidden)

    let qs =
        AuthorizationService.ParseRawQuerystring(body.rawQueryString) :> seq<_>
        |> Seq.map (|KeyValue|)
        |> Map.ofSeq
    let code = qs.Item "code"
    let shopUrl = qs.Item "shop"

    // Activate the code to get a new accesstoken, whether the user is new or already exists. Getting a new token for an existing
    // user will not invalidate any previous API usage, billing charges, etc, and makes it much easier to reintegrate a returning user.
    let! accessToken =
        AuthorizationService.Authorize(
            code,
            shopUrl,
            ServerConstants.shopifyApiKey,
            ServerConstants.shopifySecretKey)
        |> Async.AwaitTask
    let! shop =
        ShopService(shopUrl, accessToken).GetAsync()
        |> Async.AwaitTask

    // Lookup user in database to see if we're creating a new user or logging in and updating an existing one.
    let! dbUser = Database.getUserByShopId <| shop.Id.GetValueOrDefault 0L
    let! user =
        match dbUser with
        | Some u ->
            { u with shopifyAccessToken = Some accessToken }
            |> Database.updateUser u.id u.rev
        | None ->
            { shopifyAccessToken = Some accessToken
              created = Date.toUnixTimestamp DateTime.UtcNow
              id = "" // Will be filled by CouchDB
              rev = "" // Will be filled by CouchDB
              myShopifyUrl = Some shopUrl
              shopName = Some shop.Name
              shopId = shop.Id.Value
              shopifyChargeId = None }
            |> Database.createUser

    return!
        Successful.OK "{}"
        >=> Writers.setMimeType Json.MimeType
        >=> Cookie.setCookie (createSessionCookie user)
        <| ctx
}

let routes = [
    POST >=> choose [
        path "/api/v1/auth/oauth/shopify" >=> shopifyLoginOrRegister
    ]
    GET >=> choose [
        path "/api/v1/auth/oauth/shopify" >=> getShopifyOAuthUrl
    ]
]