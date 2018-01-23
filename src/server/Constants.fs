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

let authScopes: ShopifySharp.Enums.AuthorizationScope list =
    [
        ShopifySharp.Enums.AuthorizationScope.ReadOrders
        ShopifySharp.Enums.AuthorizationScope.WriteOrders
    ]

let couchdbUrl = envVarDefault "COUCHDB_URL" "localhost:5984"

let couchdbUsername = envVar "COUCHDB_USERNAME"

let couchdbPassword = envVar "COUCHDB_PASSWORD"

/// The app's domain, e.g. google.com or facebook.com (without protocol). This is used when creating webhooks and redirect URLs.
/// Due to that, it should not be set to anything but localhost during development.
let domain = envVarDefault "APP_DOMAIN" "localhost:8000"