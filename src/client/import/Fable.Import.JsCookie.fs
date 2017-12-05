module Fable.Import.JsCookie

open System
open Fable.Core
open Fable.Core.JsInterop

type IJSCookie =
    abstract get: name: string -> (string option)
    abstract set: name: string -> value: string -> obj option // Returns null, must be represented with option

let private imported = import<IJSCookie> "*" "js-cookie"

let get = imported.get

let set = imported.set