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

/// Whether the application is considered to be running in "production" mode, as determined by the
/// NODE_ENV environment value. If true, oauth redirects will go to the current domain host on https.
/// If false, oauth redirects will go to localhost:8000.
let isLive = (envVarDefault "NODE_ENV" "development") = "production"

let shopifySecretKey = envVarRequired "SHOPIFY_SECRET_KEY"

let shopifyApiKey = envVarRequired "SHOPIFY_PUBLIC_KEY"

let shopifyChargeRedirectPath = "/shopify/activate-charge"

let authScopes: ShopifySharp.Enums.AuthorizationScope list =
    [
        ShopifySharp.Enums.AuthorizationScope.ReadOrders
        ShopifySharp.Enums.AuthorizationScope.WriteOrders
    ]

let databaseConnectionString = envVarRequired "DATABASE_CONNECTION_STRING"