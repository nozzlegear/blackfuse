module Stores.Dashboard

open Fable.Import

let orders = Mobx.boxedObservable<(int * Domain.Order list) option> None

/// Whether the dashboard is in a loading state, computed by checking if the `orders` value is None.
let loading = Mobx.computed(fun _ -> Mobx.get orders |> Option.isNone)