module Pages.Billing.GetUrl

open Fable.Import
open Fable.PowerPack
module S = Stores.Billing
module R = Fable.Helpers.React
module P = R.Props
module C = Components
module Mobx = Fable.Import.Mobx
module MobxReact = Fable.Import.MobxReact

let getUrlAndRedirect () =
    if not <| Mobx.get S.loading then
        S.startLoading()

        promise {
            let! result = Services.Billing.createChargeUrl()

            match result with
            | Error e ->
                Fable.Import.Browser.console.error e
                S.receivedError e.message
            | Ok r ->
                // Redirect to the "accept charge" URL
                Browser.window.location.href <- r.url
        }
        |> Promise.start


let Page () =
    fun _ ->
        let error, loading = Mobx.get S.error, Mobx.get S.loading

        let footer =
            match loading with
            | true -> R.progress [] []
            | false ->
                R.div [] [
                    R.button [P.ClassName "btn blue"; P.OnClick (ignore >> getUrlAndRedirect)] [
                        R.str "Start my free trial!"
                    ]
                ]
            |> Some

        let body =
            R.div [] [
                R.p [] [
                    sprintf "Thanks for checking out %s! On the next page you'll be asked to start your free %i-day trial. The price of a subscription is $%.2f/month, but you can cancel your free trial at any time with no obligations." Constants.AppName Constants.FreeTrialDays Constants.SubscriptionPrice
                    |> R.str
                ]
                R.p [] [
                    sprintf "To cancel your trial, or to cancel your subscription, simply go to your Shopify store's admin page and uninstall the %s app." Constants.AppName
                    |> R.str
                ]
            ]

        sprintf "Start your free trial of %s." Constants.AppName
        |> Box.title
        |> Box.error error
        |> Box.footer footer
        |> Box.make [body]
    |> MobxReact.Observer