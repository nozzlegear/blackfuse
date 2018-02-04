module Stores.Dashboard

open Fable.Import

let orders = Mobx.boxedObservable<Domain.Requests.Orders.ListOrdersResponse option> None

let error = Mobx.boxedObservable<string option> None

/// Whether the dashboard is in a loading state, computed by checking if the `orders` value is None.
let loading = Mobx.computed(fun _ -> Mobx.get orders |> Option.isNone)

let receivedError msg = Mobx.runInAction (fun _ -> Mobx.set error (Some msg))

let receivedOrders o = Mobx.runInAction (fun _ -> 
    Mobx.set error None 
    Mobx.set orders (Some o)
)