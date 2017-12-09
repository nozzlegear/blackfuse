module Fable.Import.ReactWinPivot

open System
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import.JS
module R = Fable.Helpers.React

[<Pojo>]
type ITab =
    { name: string
      selected: bool }

type PivotProp =
    | Title of string
    | Actions of React.ReactElement
    | Tabs of ITab array
    | OnChange of (ITab -> unit)

type IReactWinPivot =
    abstract member Header: React.ComponentClass<obj>
    abstract member PivotTabs: React.ComponentClass<obj>

let private imported: IReactWinPivot = import "*" "react-win-pivot"

let header (propList: PivotProp list) =
    let props = keyValueList CaseRules.LowerFirst propList

    R.from imported.Header props []

let tab name selected = { name = name; selected = selected }

let pivot (propList: PivotProp list) (tabList: ITab list) =
    let props =
        propList
        |> List.append [Tabs <| List.toArray tabList]
        |> keyValueList CaseRules.LowerFirst
    R.from imported.PivotTabs props []