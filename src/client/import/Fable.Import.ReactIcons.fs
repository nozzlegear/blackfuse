module rec Fable.Import.ReactIcons

open System
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import.JS
module R = Fable.Helpers.React

type IconProps =
    | Size of int
    | Color of string
    | ClassName of string
    | OnClick of (React.SyntheticEvent -> unit)

let private makeIcon icon (propList: IconProps list) =
    let props = keyValueList CaseRules.LowerFirst propList
    R.from icon props []

let hamburgerIcon =
    import "*" "react-icons/lib/md/menu"
    |> makeIcon

let errorIcon =
    import "*" "react-icons/lib/md/error-outline"
    |> makeIcon

let rightChevronIcon =
    import "*" "react-icons/lib/md/chevron-right"
    |> makeIcon

let leftChevronIcon =
    import "*" "react-icons/lib/md/chevron-left"
    |> makeIcon

let descriptionIcon =
    import "*" "react-icons/lib/md/description"
    |> makeIcon

let checkBox =
    import "*" "react-icons/lib/md/check-box"
    |> makeIcon