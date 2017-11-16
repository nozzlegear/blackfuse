module Constants

type Constant =
    | Version of string
    | AppName of string

type Language =
    | FSharp
    | TypeScript

let version = "1.0.0"

let all = [
    Version version
    AppName "Blackfuse"
]

let stringify lang constant =
    let name, value =
        match constant with
        | Version s -> s
        | AppName s -> s
        |> fun s -> constant.GetType().Name, s

    match lang with
    | FSharp -> "let"
    | TypeScript -> "export const"
    |> fun s -> sprintf "%s %s = \"%s\"" s name value

let fsConstants =
    all
    |> Seq.map (stringify FSharp)
    |> String.concat System.Environment.NewLine

let tsConstants =
    all
    |> Seq.map (stringify TypeScript)
    |> String.concat System.Environment.NewLine