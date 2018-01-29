module Pages.Home.Index

open Fable
open Fable.Core
open Fable.Import
open Fable.PowerPack
module R = Fable.Helpers.React
module P = R.Props
module C = Components
module S = Stores.Home

let loadPage page = promise {
    ()
}

let Page (page: int) =
    let loadAfterMount() = 
        R.div [] [
            R.progress [] []
            C.AfterMount (fun _ -> loadPage page |> Promise.start)
        ]

    fun _ ->

        let body = 
            match Mobx.get S.orders with 
            | None -> loadAfterMount()
            | Some (i, _) when i <> page -> loadAfterMount()
            | Some (_, orders) -> R.h1 [] [sprintf "%i orders loaded" (Seq.length orders) |> R.str]

        R.div [] [ 
            body

                // TODO: Load orders for this page

            // match Mobx.get S.orders with
            // |
            // R.h1 [] [
            //     R.str <| sprintf "Hello. You're currently logged in. Your shop is %s at %s." (Option.toString session.shopName) (Option.toString session.myShopifyUrl)
            // ]
            // R.str "You're on the dashboard page."
            // R.button [Type "button"; OnClick (ignore >> NavStore.openDialog)] [
            //     R.str "Click to open the dialog"
            // ]
            sprintf "You're on home page %i." page
            |> R.str
        ]
    |> MobxReact.Observer

let PageZero () = Page 0