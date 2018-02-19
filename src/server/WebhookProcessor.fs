module WebhookProcessor

open ShopifySharp

type WebhookMessage =
    | CreateAll of Domain.User
    | HandleAppUninstalled of byte[]
    | HandleShopUpdated of byte[]

type private WebhookTopic =
    | AppUninstalled
    | ShopUpdated

let private createWebhook myShopifyUrl accessToken topic = async {
    let topicStr, path =
        match topic with
        | AppUninstalled -> "app/uninstalled", Paths.Api.Webhooks.appUninstalled.ToString()
        | ShopUpdated -> "shop/update", Paths.Api.Webhooks.shopUpdated.ToString()

    let hook = Webhook()
    hook.Topic <- topicStr
    hook.Address <- Utils.toAbsoluteUrl path |> string

    let! result =
        WebhookService(myShopifyUrl, accessToken).CreateAsync hook
        |> Async.AwaitTask
        |> Async.Catch

    match result with
    | Choice1Of2 _ -> printfn "Created %A webhook!" topic
    | Choice2Of2 exn ->
        match exn with
        | :? ShopifyException as exn when exn.Message.ToLower().Contains "this topic has already been taken" ->
            // Webhook has already been created.
            printfn "Webhook %A has already been created." topic
            ()
        | _ -> raise exn
}

let private createAllWebhooks (user: Domain.User) = async {
    let shopifyUrl, accessToken =
        match user.myShopifyUrl, user.shopifyAccessToken with
        | None, None
        | None, Some _
        | Some _, None ->
            sprintf "Failed to create webhooks with WebhookProcessor. User with id '%s' Shopify Access Token or MyShopifyUrl was None." user.id
            |> System.ArgumentException
            |> raise
        | Some url, Some token -> url, token

    do!
        [
            AppUninstalled
            ShopUpdated
        ]
        |> Seq.map (createWebhook shopifyUrl accessToken)
        |> Async.Parallel
        |> Async.Ignore
}

let private handleAppUninstalled = Json.parseFromBody<Shop> >> fun shop -> async {
    let! getUser = Database.getUserByShopId shop.Id.Value

    match getUser with
    | None -> 
        () // Do nothing, there's no user that has that shop id.
    | Some user ->
        // Erase the user's access token, shopify charge, shop name and shop url, but keep their shop id so we can restore their
        // account if they ever reinstall the app.
        do!
            ({ user with shopifyAccessToken = None; myShopifyUrl = None; subscription = None; shopName = None })
            |> Database.updateUser user.id user.rev
            |> Async.Ignore

        // Invalidate any of the user's auth sessions by deleting them
        do! Database.deleteSessionsForUser user.id
}

let private handleShopUpdated = Json.parseFromBody<Shop> >> fun shop -> async {
    let! getUser = Database.getUserByShopId shop.Id.Value

    match getUser with
    | None -> () // Do nothing, there's no user that has that shop id.
    | Some user ->
        do!
            ({ user with myShopifyUrl = Some shop.MyShopifyDomain; shopName = Some shop.Name })
            |> Database.updateUser user.id user.rev
            |> Async.Ignore
}

let private agent =
    MailboxProcessor<WebhookMessage>.Start (fun inbox ->
        let rec readMsg () = async {
            let! msg = inbox.Receive()

            do!
                match msg with
                | CreateAll user -> createAllWebhooks user
                | HandleAppUninstalled body -> handleAppUninstalled body
                | HandleShopUpdated body -> handleShopUpdated body

            return! readMsg()
        }

        readMsg()
    )

let post = agent.Post