module Stores.Billing

open Domain
open Fable.Import

let loading = Mobx.boxedObservable false
let error = Mobx.boxedObservable<string option> None

let startLoading() = Mobx.runInAction (fun _ ->
    Mobx.set loading true
    Mobx.set error None
)

let receivedError msg = Mobx.runInAction (fun _ ->
    Mobx.set loading false
    Mobx.set error <| Some msg
)

let reset() = Mobx.runInAction (fun _ ->
    Mobx.set loading false
    Mobx.set error None
)