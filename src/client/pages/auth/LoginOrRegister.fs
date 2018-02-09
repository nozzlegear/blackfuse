module Pages.Auth.LoginOrRegister

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
module Mobx = Fable.Import.Mobx
module MobxReact = Fable.Import.MobxReact

type PageType =
    | Register
    | Login

let login pageType =
    match Mobx.get S.Form.domain |> Option.defaultValue "" with
    | null
    | "" ->
        Some "MyShopify domain cannot be empty."
        |> Mobx.set S.Form.error
    | domain ->
        S.Form.updateLoading true
        S.Form.updateError None

        promise {
            let! result = Services.Auth.createOauthUrl domain

            match result with
            | Ok r ->
                // Redirect to the Shopify OAuth page which will log the user in or register them.
                Browser.window.location.href <- r.url

                // match JsCookie.get Constants.CookieName with
                // | Some token ->
                //     S.Form.clearForm()
                //     S.logIn token
                //     Router.push "/"
                // | None ->
                //     "Error parsing authorization cookie. Please try again."
                //     |> Some
                //     |> S.Form.updateError
            | Error e ->
                Fable.Import.Browser.console.error e
                S.Form.updateError <| Some e.message
                S.Form.updateLoading false
        }
        |> Promise.start

let Page (pageType: PageType) dict =
    fun _ ->
        let onSubmit (e: React.SyntheticEvent) = 
            e.preventDefault()
            login pageType

        let footer =
            match Mobx.get S.Form.loading with
            | true ->
                R.progress [] []
            | false ->
                R.div [] [
                    R.button [P.ClassName "btn blue"; P.OnClick onSubmit] [
                        match pageType with
                        | Register -> "Connect Shopify Store"
                        | Login -> "Authenticate With Shopify"
                        |> R.str
                    ]

                    match pageType with
                    | Register ->
                        [
                            R.str "Already have an account? "
                            Router.link Paths.Client.Auth.login None [] [R.str "Sign in!"]
                        ]
                    | Login ->
                        [
                            R.str "No account? "
                            Router.link Paths.Client.Auth.register None [] [R.str "Get one!"]
                        ]
                    |> R.div []
                ]
        let description =
            match pageType with
            | Login -> sprintf "Enter your myshopify.com domain below to authenticate and sign in to your %s account." Constants.AppName
            | Register -> sprintf "Enter your myshopify.com domain below to connect your Shopify store with %s." Constants.AppName
        let domain = Mobx.get S.Form.domain |> Option.defaultValue ""

        match pageType with | Login -> "Login." | Register -> "Create an account."
        |> Box.title
        |> Box.description (Some description)
        |> Box.error (Mobx.get S.Form.error)
        |> Box.footer (Some footer)
        |> Box.make [
            R.form [P.OnSubmit onSubmit] [
                C.TextboxWithLabel "Your *.myshopify.com domain:" domain (Some >> S.Form.updateDomain)
            ]
        ]
        
    |> MobxReact.Observer