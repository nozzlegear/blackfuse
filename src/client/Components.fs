module Components

open Fable.Core
open Fable.Import
module R = Fable.Helpers.React
module P = R.Props

let Box title description error footer children =
    R.div [P.ClassName "box theme"] [
        R.div [P.ClassName "panel active"] [
            R.div [P.ClassName "header"] [
                R.h4 [] [R.str title]

                match description with
                | Some s -> Some <| R.p [] [R.str s]
                | None -> None
                |> R.opt
            ]
            R.div [P.ClassName "body"] children
            R.div [P.ClassName "footer"] [
                match error with
                | Some s -> Some <| R.p [P.ClassName "error"] [R.str s]
                | None -> None
                |> R.opt
                R.opt footer
            ]
        ]
    ]

let ControlGroup label control =
    R.div [P.ClassName "control-group"] [
        R.label [] [R.str label]
        control
    ]

let TextboxWithLabel label value onChange =
    ControlGroup label <| R.input [P.Type "text"; P.OnChange (Utils.getValueFromEvent >> onChange); P.Value value]

let PasswordWithLabel label value onChange =
    ControlGroup label <| R.input [P.Type "password"; P.OnChange (Utils.getValueFromEvent >> onChange); P.Value value]

let DateInputWithLabel label (value: System.DateTime option) onChange =
    let formattedValue =
        value
        |> Option.map (fun d -> d.ToString "YYYY-MM-DD")
        |> Option.defaultValue ""

    R.input [P.Type "date"; P.OnChange (Utils.getDateFromEvent >> onChange); P.Value formattedValue]
    |> ControlGroup label

let Checkbox label value onChange =
    R.div [P.ClassName "control-group checkbox"] [
        R.label [] [
            R.input [P.Type "checkbox"; P.OnChange (Utils.getCheckedFromEvent >> onChange); P.Checked value]
            R.str <| " " + label
        ]
    ]

let TextareaWithLabel label rows value onChange =
    ControlGroup label <| R.textarea [P.Value value; P.Rows rows; P.OnChange (Utils.getValueFromEvent >> onChange)] []

let Instruction text =
    R.div [P.ClassName "instruction"] [
        R.div [P.ClassName "icon"] [
            ReactIcons.descriptionIcon [ReactIcons.Size 35; ReactIcons.Color "#fff"]
        ]
        R.p [] [R.str text]
    ]

let PureGrid classNames (attributes: Fable.Helpers.React.Props.IHTMLProp list) = 
    let atts = attributes@[P.ClassName <| sprintf "pure-g %s" classNames]    
    R.div atts

let PureUnit fractionOf24 classNames (attributes: Fable.Helpers.React.Props.IHTMLProp list) = 
    let atts = attributes@[P.ClassName <| sprintf "pure-u-%i-24 %s" fractionOf24 classNames]
    R.div atts

let LeftRightSplit leftSide rightSide = 
    let leftSideSize, leftSideChild = leftSide
    let rightSideSize, rightSideChild = rightSide 

    PureGrid "left-right-split" [] [
        PureUnit leftSideSize "left" [] [leftSideChild]
        PureUnit rightSideSize "right" [] [rightSideChild]
    ]

let Error msg = R.p [P.ClassName "error red"] [R.str msg]

/// Same as the `Error` function, but centers the error message in a div with .text-center class.
let ErrorCentered msg = R.div [P.ClassName "text-center"] [Error msg]

/// Runs the given function after each React render cycle, whether mounting or updating.
let AfterRender key f =
    let func (el: Browser.Element) =
        match Option.ofNullable el with
        | Some _ -> f ()
        | None -> ()

    R.noscript [P.Key key; P.Ref func] []

/// Runs the given function after the first mount of the parent React component or element.
/// NOTE: This must be created *outside* of the parent's render cycle, i.e. it should not be created inside a render method or MobxReact.Observer function.
let AfterMount key f =
    let mutable mounted = false
    let func el =
        match Option.ofNullable el with
        | None -> ()
        | Some _ ->
            if not mounted then f()
            mounted <- true

    R.noscript [P.Key key; P.Ref func] []