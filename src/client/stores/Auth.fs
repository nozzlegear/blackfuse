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
    let username = mobx.boxedObservable<string option> None

    let password = mobx.boxedObservable<string option> None

    let error = mobx.boxedObservable<string option> None

    let loading = mobx.boxedObservable<bool> false

    let updateUsername username' = mobx.runInAction <| fun _ -> mobx.set username username'

    let updatePassword password' = mobx.runInAction <| fun _ -> mobx.set password password'

    let updateError error' = mobx.runInAction <| fun _ -> mobx.set error error'

    let updateLoading toValue = mobx.runInAction <| fun _ -> mobx.set loading toValue

    let clearForm () = mobx.runInAction <| fun _ ->
        mobx.set error None
        mobx.set username None
        mobx.set password None
        mobx.set loading false

