module ServerConstants

open System

let private envVar key =
    let value = System.Environment.GetEnvironmentVariable key

    match String.IsNullOrEmpty value with
    | true -> None
    | false -> Some value

let private envVarRequired key =
    match envVar key with
    | Some s -> s
    | None -> failwithf "Required environment variable %s was null or empty." key

let private envVarDefault key defaultValue =
    match envVar key with
    | Some s -> s
    | None -> defaultValue
let shopifySecretKey = envVarRequired "SHOPIFY_SECRET_KEY"

let shopifyApiKey = envVarRequired "SHOPIFY_PUBLIC_KEY"