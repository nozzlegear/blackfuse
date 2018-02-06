module Box 

open Fable.Import.React
module R = Fable.Helpers.React
module P = R.Props

type BoxProps = private {
    title: string 
    description: string option 
    error: string option 
    footer: ReactElement option
}

let title s = { title = s; description = None; error = None; footer = None; }

let description s props = { props with description = s }

let error s props = { props with error = s }

let footer f props = { props with footer = f }

let make children props = 
    R.div [P.ClassName "box theme"] [
        R.div [P.ClassName "panel active"] [
            R.div [P.ClassName "header"] [
                R.h4 [] [R.str props.title]

                match props.description with
                | Some s -> Some <| R.p [] [R.str s]
                | None -> None
                |> R.opt
            ]
            R.div [P.ClassName "body"] children
            R.div [P.ClassName "footer"] [
                match props.error with
                | Some s -> Some <| R.p [P.ClassName "error"] [R.str s]
                | None -> None
                |> R.opt

                R.opt props.footer
            ]
        ]
    ]    