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