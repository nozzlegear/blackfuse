module Fable.Import.ReactWinDialog

open System
open Fable.Core
open Fable.Import.JS
open Fable.Core.JsInterop
module R = Fable.Helpers.React

type DialogProps =
    | Title of string
    | Open of bool
    | Danger of bool
    | PrimaryText of string
    | SecondaryText of string
    | OnPrimaryClick of (React.FormEvent -> unit)
    | OnSecondaryClick of (React.FormEvent -> unit)
    | OverlayStyle of Map<string, React.CSSProperties>
    | ContainerStyle of Map<string, React.CSSProperties>
    | DialogStyle of Map<string, React.CSSProperties>
    | Id of string
    | ClassName of string

let private imported = importDefault<React.ComponentClass<obj>> "react-win-dialog"

let dialog (propList: DialogProps list) =
    let props = keyValueList CaseRules.LowerFirst propList
    R.from imported props