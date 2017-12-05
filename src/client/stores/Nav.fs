module Stores.Nav

open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
module mobx = Fable.Import.Mobx

let navIsOpen = mobx.boxedObservable false

let dialogIsOpen = mobx.boxedObservable false

let computedMessage = mobx.computed (fun _ -> sprintf "My secret message: %b" <| mobx.get navIsOpen)

let runner = mobx.autorun(fun _ -> printfn "Autorunner running. The navIsOpen value is: %b" <| mobx.get navIsOpen)

let setNavIsOpen toValue = mobx.runInAction(fun _ -> mobx.set navIsOpen toValue)

let toggleNavIsOpen () = mobx.runInAction(fun _ -> mobx.get navIsOpen |> not |> mobx.set navIsOpen)

let openDialog () = mobx.runInAction(fun _ -> mobx.set dialogIsOpen true)

let closeDialog () = mobx.runInAction(fun _ -> mobx.set dialogIsOpen false)