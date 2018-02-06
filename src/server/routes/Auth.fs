module Routes.Auth

open System
open Domain
open Suave
open Filters
open Operators
open ShopifySharp
open Errors

/// Takes a user and turns it into a Session, adding the signature label and creating the database entry.
let createSession (user: Domain.User) = 
    { id = ""  // Will be created by CouchDB
      rev = "" // Will be created by CouchDB 
      signature = "" // Will be created with Jwt.sign
      created = DateTime.UtcNow
      user = ParedUser.FromUser user }
    |> Jwt.sign 
    |> Database.createSession

let createSessionCookie (session: Session) = 
    // Create a cookie that has no (practical) expiration date. Auth expiration is instead dictated by the JWT token
    let cookieExpiration = DateTimeOffset.UtcNow.AddYears 10
    let cookie = 
        session 
        |> Json.stringify
        |> HttpCookie.createKV Constants.CookieName 

    { cookie with 
        httpOnly = false 
        expires = Some cookieExpiration }

let createUrl = request <| fun req ctx -> async {
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
            Utils.toAbsoluteUrl Paths.Client.Auth.completeOAuth |> string)

    return!
        Successful.OK <| sprintf """{"url":"%s"}""" (oauthUrl.ToString())
        >=> Writers.setMimeType Json.MimeType
        <| ctx
}

let loginOrRegister = request <| fun req ctx -> async {
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
            { u with shopifyAccessToken = Some accessToken; myShopifyUrl = Some shopUrl; shopName = Some shop.Name }
            |> Database.updateUser u.id u.rev
        | None ->
            { shopifyAccessToken = Some accessToken
              created = Date.toUnixTimestamp DateTime.UtcNow
              id = "" // Will be filled by CouchDB
              rev = "" // Will be filled by CouchDB
              myShopifyUrl = Some shopUrl
              shopName = Some shop.Name
              shopId = shop.Id.Value
              subscription = None }
            |> Database.createUser

    // Create webhooks if we're not on localhost (hooks can't be sent to localhost).
    // WebhookProcessor will handle if they've already been created.
    if not <| ServerConstants.domain.ToLower().Contains "localhost"
    then WebhookProcessor.post <| WebhookProcessor.CreateAll user

    let! session = createSession user

    return!
        Successful.OK "{}"
        >=> Writers.setMimeType Json.MimeType
        >=> Cookie.setCookie (createSessionCookie session)
        <| ctx
}

let routes = [
    PUT >=> path Paths.Api.Auth.loginOrRegister >=> loginOrRegister
    POST >=> path Paths.Api.Auth.createUrl >=> createUrl
]