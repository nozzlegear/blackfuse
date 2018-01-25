module Stores.Home

open Fable.Import

let orders = Mobx.boxedObservable<(int * Domain.Order list) option> None