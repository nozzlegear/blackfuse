module Fable.Import.RouteMatcher

open System
open Fable.Core
open Fable.Core.JsInterop

type IMatcher =
    abstract parse: currentLocation: string -> (obj option)
    abstract stringify: parameters: Map<string, string> -> string

type IRouteMatcher =
    abstract routeMatcher: route: string -> IMatcher

let private imported = import<IRouteMatcher> "*" "route-matcher"

let create = imported.routeMatcher