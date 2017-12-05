module Routes.Auth

open System
open Domain
open Suave
open Filters
open Operators

let rec createSession = request <| fun req ctx -> async {
    let body =
        req.rawForm
        |> Json.parseFromBody<Requests.Auth.CreateSession>
        |> fun t -> t.Validate()
        |> function
        | Ok b -> b
        | Error e -> raise <| Errors.fromValidation e

    // TODO: Lookup user in database, validate their password with bcrypt

    // Create a cookie that has no (practical) expiration date. Auth expiration is instead dictated by the JWT token
    let cookieExpiration = DateTimeOffset.UtcNow.AddYears 10
    let cookie =
        { email = body.username
          created = Date.toUnixTimestamp <| DateTime.UtcNow.AddDays -90.
          hashedPassword = "temp"
          id = "abcd-1234" }
        |> Jwt.encode Jwt.DefaultExpiration
        |> HttpCookie.createKV Constants.CookieName
        |> fun c -> { c with httpOnly = false; expires = Some cookieExpiration }

    return!
        Successful.OK "{}"
        >=> Writers.setMimeType Json.MimeType
        >=> Cookie.setCookie cookie
        <| ctx
}

let checkUserState = context <| fun ctx ->
    printfn "User state: %A" ctx.userState
    Successful.OK "This required auth and a user state"

let routes = [
    POST >=> choose [
        path "/api/v1/auth" >=> createSession
    ]
]