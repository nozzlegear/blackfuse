module Pages.Billing.Result

open Fable.Import
open Fable.PowerPack
module S = Stores.Billing
module R = Fable.Helpers.React
module P = R.Props
module C = Components
module Mobx = Fable.Import.Mobx
module MobxReact = Fable.Import.MobxReact

let completeBillingSetup _ =
    if not <| Mobx.get S.loading then
        S.startLoading()

        promise {
            let! result = Services.Billing.createOrUpdateCharge Browser.window.location.search

            match result with
            | Error e ->
                Fable.Import.Browser.console.error e
                S.receivedError e.message
            | Ok _ ->
                match JsCookie.get Constants.CookieName with
                | Some token ->
                    Stores.Auth.logIn token
                    Router.push (Paths.Client.home.ToString())
                    S.reset()
                | None ->
                    S.receivedError "Error parsing authorization cookie. Please try again."
        }
        |> Promise.start

let Page () =
    let completer = C.AfterMount "complete-billing" completeBillingSetup

    fun _ ->
        let error, loading = Mobx.get S.error, Mobx.get S.loading

        let footer =
            match loading, error with
            | true, _ -> None
            | false, Some _ ->
                Some
                <| R.div [] [
                    Router.link Paths.Client.Billing.index None [P.ClassName "btn"] [
                        R.str "Try again."
                    ]
                ]
            | false, None -> Some completer

        let body =
            match loading, error with
            | true, _
            | false, None -> [R.progress [] []]
            | false, Some _ -> [ReactIcons.errorIcon [ReactIcons.Size 150; ReactIcons.Color "red"]]
            |> R.div [P.ClassName "text-center"]

        let description =
            match error with
            | Some _ -> sprintf "Encountered an error while activating your %s subscription." Constants.AppName
            | None -> "Please wait."

        match error with Some _ -> "Subscription Error" | None -> "Activating free trial."
        |> Box.title
        |> Box.description (Some description)
        |> Box.error error
        |> Box.footer footer
        |> Box.make [body]
        
    |> MobxReact.Observer