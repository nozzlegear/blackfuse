module Utils

open Fable.Core.JsInterop

let getValueFromEvent (evt: Fable.Import.React.FormEvent) =
    evt.currentTarget?value |> unbox<string>