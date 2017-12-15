module Utils

open Fable.Core.JsInterop
open Fable.Import.React

let getValueFromEvent (evt: FormEvent) =
    evt.currentTarget?value |> unbox<string>

let getCheckedFromEvent (evt: FormEvent) =
    evt.currentTarget?``checked`` |> unbox<bool>

let getDateFromEvent: FormEvent -> System.DateTime option =
    getValueFromEvent
    >> fun d ->
        try
            let parsed =
                createNew Fable.Import.JS.Date d
                |> unbox<System.DateTime>

            // Checking the date's time will return NaN in JS if the date was invalid.
            if (parsed.TimeOfDay.ToString()) = "NaN" then
                None
            else Some parsed
        with
        | _ -> None

let spaceToDash (s: string) = s.Replace(" ", "-")

let toLower (s: string) = s.ToLower()