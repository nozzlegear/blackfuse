module Folder
open System.IO

let publicFolder =
    [
        "../client/public"
        "./src/client/public"
        "./public"
    ]
    |> Seq.tryFind Directory.Exists
    |> function
    | None -> failwithf "Could not find path to public folder."
    | Some s -> s