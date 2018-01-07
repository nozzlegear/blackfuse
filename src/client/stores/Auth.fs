module Stores.Auth

open Domain
module mobx = Fable.Import.Mobx

let token =
    Fable.Import.JsCookie.get Constants.CookieName
    |> mobx.boxedObservable<string option>

let session = mobx.computed <| fun _ ->
    match mobx.get token with
    | Some t ->
        Some <| Fable.Import.JwtSimple.decodeNoVerify<SessionToken> t
    | None -> None

let isAuthenticated = mobx.computed (fun _ -> Option.isSome <| mobx.get session)

let logIn (token': string) = mobx.runInAction <| fun _ ->
    mobx.set token (Some token')

let logOut () = mobx.runInAction <| fun _ ->
    mobx.set token None

module Form =
    let domain = mobx.boxedObservable<string option> None

    let error = mobx.boxedObservable<string option> None

    let loading = mobx.boxedObservable<bool> false

    let updateDomain domain' = mobx.runInAction <| fun _ -> mobx.set domain domain'

    let updateError error' = mobx.runInAction <| fun _ -> mobx.set error error'

    let updateLoading toValue = mobx.runInAction <| fun _ -> mobx.set loading toValue

    let clearForm () = mobx.runInAction <| fun _ ->
        mobx.set error None
        mobx.set domain None
        mobx.set loading false

module OAuth =
    let loading = mobx.boxedObservable<bool> false

    /// Whether the user has attempted to complete oauth at least once.
    let hasAttempted = mobx.boxedObservable<bool> false

    let error = mobx.boxedObservable<string option> None

    /// Call when the user is attempting to complete OAuth.
    let beginningAttempt () = mobx.runInAction <| fun _ ->
        mobx.set hasAttempted true
        mobx.set loading true
        mobx.set error None

    /// Call when the user has attempted to complete OAuth but failed. Will set the error message, plus set loading to false.
    let receivedError msg = mobx.runInAction <| fun _ ->
        mobx.set loading false
        mobx.set error (Some msg)

    /// Call when the user is navigating away from the page.
    let reset () = mobx.runInAction <| fun _ ->
        mobx.set hasAttempted false
        mobx.set loading false
        mobx.set error None