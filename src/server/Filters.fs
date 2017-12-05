module Filters

open Suave
open Suave.Operators
open Suave.Filters
open Suave.Cookie

/// Verifies that the request is authenticated. If so this will the given WebPart, else returns a Forbidden response and ends the request. Can be used with the authentication <!!> operator: path "/" <!!> myAuthedWebpart.
let authenticate part = request <| fun req ctx ->
    let authHeader = req.header "let-me-through"

    match authHeader with
    | Choice1Of2 header ->
        printfn "Choice 1. The header value is %s" header

        Writers.setUserData "user" "henlo world"
        >=> part
        <| ctx
    | Choice2Of2 msg ->
        printfn "Choice 2. %s" msg
        // Return a Forbidden status because Unauthorized has a bug in Edge where it can't read the body.
        RequestErrors.FORBIDDEN "Nope" ctx

/// Authentication operator. Injects an authentication mechanism between the two webparts so that the second webpart will not execute if the user is not authenticated (in which case a Forbidden result is returned instead.)
let inline (+.+) part1 (part2: WebPart): WebPart =
    part1 >=> authenticate part2