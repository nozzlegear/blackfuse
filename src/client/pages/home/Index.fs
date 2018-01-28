module Pages.Home.Index

open Fable
open Fable.Core
open Fable.Import
open Fable.PowerPack
module R = Fable.Helpers.React
module P = R.Props
module C = Components
module S = Stores.Home

[<Pojo>]
type RouteDict = { page: int option }

let Page (dict: obj) =
    let betterDict = dict :?> Map<string, obj>

    Browser.console.log("Got page dictionary", betterDict)

    fun _ ->

        R.div [] [
            // match Mobx.get S.orders with
            // |
            // R.h1 [] [
            //     R.str <| sprintf "Hello. You're currently logged in. Your shop is %s at %s." (Option.toString session.shopName) (Option.toString session.myShopifyUrl)
            // ]
            // R.str "You're on the dashboard page."
            // R.button [Type "button"; OnClick (ignore >> NavStore.openDialog)] [
            //     R.str "Click to open the dialog"
            // ]
            R.str "Temp"
        ]
    |> MobxReact.Observer