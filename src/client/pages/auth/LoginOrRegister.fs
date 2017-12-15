module Pages.Auth.LoginOrRegister

open Fable
open Fable.Core
open Fable.Import
open Fable.PowerPack
open Fable.PowerPack.PromiseImpl
open Domain.Requests.Auth
module S = Stores.Auth
module R = Fable.Helpers.React
module P = R.Props
module C = Components
module Mobx = Fable.Import.Mobx
module MobxReact = Fable.Import.MobxReact

type PageType =
    | Register
    | Login

let login pageType _ =
    let validation =
        { username = Mobx.get S.Form.username |> Option.defaultValue ""
          password = Mobx.get S.Form.password |> Option.defaultValue "" }
        |> fun d ->
            match pageType with
            | Register -> LoginOrRegister.ValidateRegister d
            | Login -> LoginOrRegister.ValidateLogin d

    match validation with
    | Error e ->
        Validation.getMessage e
        |> Some
        |> Mobx.set S.Form.error
    | Ok data ->
        S.Form.updateLoading true
        S.Form.updateError None

        promise {
            let! result =
                match pageType with
                | Register -> Services.Auth.register data
                | Login -> Services.Auth.login data

            match result with
            | Ok _ ->
                match JsCookie.get Constants.CookieName with
                | Some token ->
                    S.Form.clearForm()
                    S.logIn token
                    Router.push "/"
                | None ->
                    "Error parsing authorization cookie. Please try again."
                    |> Some
                    |> S.Form.updateError
            | Error e ->
                Fable.Import.Browser.console.error e
                S.Form.updateError <| Some e.message

            S.Form.updateLoading false
        }
        |> Promise.start

let Page (pageType: PageType) dict =
    fun _ ->
        let footer =
            match Mobx.get S.Form.loading with
            | true ->
                R.progress [] []
            | false ->
                R.div [] [
                    R.button [P.ClassName "btn blue"; P.OnClick (ignore >> login pageType)] [
                        match pageType with
                        | Register -> "Register Account"
                        | Login -> "Sign In"
                        |> R.str
                    ]

                    match pageType with
                    | Register ->
                        [
                            R.str "Already have an account? "
                            Router.link "/auth/login" [] [R.str "Sign in!"]
                        ]
                    | Login ->
                        [
                            R.str "No account? "
                            Router.link "/auth/register" [] [R.str "Create one!"]
                        ]
                    |> R.div []
                ]
        let usernameOrEmpty = Mobx.get S.Form.username |> Option.defaultValue ""
        let passwordOrEmpty = Mobx.get S.Form.password |> Option.defaultValue ""
        let getValue = Utils.getValueFromEvent

        C.Box
        <| match pageType with | Login -> "Login." | Register -> "Create an account."
        <| Some "Enter your username and password to log in to your account."
        <| Mobx.get S.Form.error
        <| Some footer
        <| [
            R.form [] [
                R.input [P.Type "text"; P.Value usernameOrEmpty; P.OnChange (getValue >> Some >> S.Form.updateUsername)]
                |> C.ControlGroup "Username"
                R.input [P.Type "password"; P.Value passwordOrEmpty; P.OnChange (getValue >> Some >> S.Form.updatePassword)]
                |> C.ControlGroup "Password"
            ]
        ]
    |> MobxReact.Observer