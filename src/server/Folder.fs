module Folder
open System.IO

let publicFolder =
    [
        "../client/public"
        "./src/client/public"
        "./deploy/public"
        "./public"
    ]
    |> Seq.tryFind Directory.Exists
    |> function
    | None -> failwithf "Could not find path to public folder."
    | Some s -> Path.GetFullPath s