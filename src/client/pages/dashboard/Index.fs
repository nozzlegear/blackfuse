module Pages.Dashboard.Index

open Fable
open Fable.Core
open Fable.Import
open Fable.PowerPack
module R = Fable.Helpers.React
module P = R.Props
module C = Components
module S = Stores.Dashboard

let loadPage page =
    if Mobx.get S.loading 
    then ()
    else 
    promise {
        let! result = Services.Orders.listOrders page 100

        match result with 
        | Ok r -> 
            
        ()
    }
    |> Promise.start

let Page (page: int) =
    let loadAfterMount() = 
        R.div [P.ClassName "loading"] [
            R.h1 [] [R.str "Loading orders, please wait."]
            R.progress [] []
            C.AfterMount (fun _ -> loadPage page)
        ]

    fun _ ->

        let body = 
            match Mobx.get S.orders with 
            | None -> loadAfterMount()
            | Some (i, _) when i <> page -> loadAfterMount()
            | Some (_, orders) -> R.h1 [] [sprintf "%i orders loaded" (Seq.length orders) |> R.str]

        R.div [] [ 
            body
        ]
    |> MobxReact.Observer

let PageZero () = Page 0