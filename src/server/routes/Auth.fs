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

let getShopifyAuthUrl = request <| fun req ctx -> async {
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

    // TODO: Merge the domain with the app id to construct a login url
    return!
        Successful.OK <| sprintf """{"url":"%s"}""" body.domain
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
        path "/api/v1/auth/get-shopify-auth-url" >=> getShopifyAuthUrl
    ]
]