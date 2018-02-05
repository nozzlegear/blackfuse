module Pages.Auth.CompleteOauth

open Fable
open Fable.Core
open Fable.Import
open Fable.PowerPack
open Fable.PowerPack.PromiseImpl
open Domain.Requests.OAuth
module S = Stores.Auth
module R = Fable.Helpers.React
module P = R.Props
module C = Components

let completeOauth _ =
    if Mobx.get S.OAuth.loading then ignore()
    else
        S.OAuth.beginningAttempt()

        promise {
            let! result = Services.Auth.loginOrRegister Browser.window.location.search

            match result with
            | Ok _ ->
                match JsCookie.get Constants.CookieName with
                | Some token ->
                    S.logIn token
                    Router.push Paths.Client.home
                    S.OAuth.reset()
                | None ->
                    S.OAuth.receivedError "Error parsing authorization cookie. Please try again."
            | Error e ->
                Fable.Import.Browser.console.error e
                S.OAuth.receivedError e.message
        }
        |> Promise.start

let Page dict =
    let oauthCompleter = C.AfterMount "complete-oauth" completeOauth

    fun _ ->
        let error, loading = Mobx.get S.OAuth.error, Mobx.get S.OAuth.loading

        let footer =
            match loading, error with
            | true, _ -> None
            | false, Some _ ->
                Some
                <| R.div [] [
                    Router.link Paths.Client.Auth.login None [P.ClassName "btn"] [R.str "Try again."]
                ]
            | false, None -> Some oauthCompleter

        let body =
            match loading, error with
            | true, _
            | false, None -> [R.progress [] []]
            | false, Some _ -> [ReactIcons.errorIcon [ReactIcons.Size 150; ReactIcons.Color "red"]]
            |> R.div [P.ClassName "text-center"]

        let description =
            match error with
            | Some _ -> "Encountered an error while completing Shopify OAuth handshake."
            | None -> "Please wait."

        C.Box
        <| match error with Some _ -> "OAuth Error" | None -> "Signing In..."
        <| Some description
        <| error
        <| footer
        <| [body]
    |> MobxReact.Observer