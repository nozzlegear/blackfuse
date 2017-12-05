module rec Fable.Import.ReactMaterialNavbar
open System
open Fable.Core
open Fable.Import.JS
open Fable.Core.JsInterop
module R = Fable.Helpers.React

type IProps =
    | Title of string
    | Color of string
    | Background of string
    | LeftAction of React.ReactElement
    | RightAction of React.ReactElement
    | Style of React.CSSProperties
    | Key of U2<string, float>

let private navbar': React.ComponentClass<_> =
    import "Navbar" "react-material-navbar"

let navbar (props: IProps list) =
    let propList = keyValueList CaseRules.LowerFirst props
    R.from navbar' propList []

type IButtonProps =
  | Text of string

let DefaultButton: React.ComponentClass<_> =
  import "DefaultButton" "office-ui-fabric-react/lib/components/button"

let inline defaultButton (props: IButtonProps list) c =
  R.from DefaultButton (keyValueList CaseRules.LowerFirst props) c