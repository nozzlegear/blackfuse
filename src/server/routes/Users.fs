module Routes.Users

open System
open Domain
open Suave
open Filters
open Operators

let register = request <| fun req ctx -> async {
    let body =
        req.rawForm
        |> Json.parseFromBody<Requests.Auth.LoginOrRegister>
        |> fun t -> t.Validate()
        |> function
        | Ok b -> b
        | Error e -> raise <| Errors.fromValidation e

    // TODO: Make sure user doesn't exist in database
    // TODO: Hash the password

    return!
        Successful.OK "{}"
        >=> Writers.setMimeType Json.MimeType
        >=> Cookie.setCookie (Auth.createSessionCookie body)
        <| ctx
}

let routes = [
    POST >=> choose [
        path "/api/v1/users" >=> register
    ]
]