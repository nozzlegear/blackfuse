module Pages.Billing.Result

open Fable
open Fable.Core
open Fable.Import
open Fable.PowerPack
open Fable.PowerPack.PromiseImpl
open Domain.Requests.OAuth
module S = Stores.Billing
module R = Fable.Helpers.React
module P = R.Props
module C = Components
module Mobx = Fable.Import.Mobx
module MobxReact = Fable.Import.MobxReact

let completeBillingSetup _ =
    if not <| Mobx.get S.loading then
        S.startLoading()


let Page dict =
    fun _ ->
        let error, loading = Mobx.get S.error, Mobx.get S.loading

        let footer =
            match loading with
            | true -> None
            | false ->
                Some
                <| R.div [] [
                    R.button [P.ClassName "btn blue"; P.OnClick completeBillingSetup] [
                        R.str "Start my free trial!"
                    ]
                ]

        let body =
            R.div [] [
                R.str "not yet implemented"
            ]

        C.Box
        <| match error with Some _ -> "OAuth Error" | None -> "Signing In..."
        <| None
        <| error
        <| footer
        <| [body]
    |> MobxReact.Observer