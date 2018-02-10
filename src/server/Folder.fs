module Folder
open System.IO

let publicFolder =
    [
        // If the server is running from src/server
        "../client/public"
        // If the server is running from src/server/dist
        "../../client/public"
        // If the server is running from the dist dir at the root project dir
        "../src/client/public"
        // If the server is running from root project dir
        "./src/client/public"
        // If the server is running from root project dir and public folder was copied into dist folder
        "./dist/public"
        // If the public folder was copied to wherever the server is running from
        "./public"
    ]
    |> Seq.tryFind Directory.Exists
    |> function
    | None -> failwithf "Could not find path to public folder."
    | Some s -> Path.GetFullPath s