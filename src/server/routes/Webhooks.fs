module Routes.Webhooks

open Suave
open Filters
open Operators
open ShopifySharp
open Errors

let appUninstalled = request <| fun req ctx -> async {
    // The app-uninstalled webhook sends the Shop object in the body
    let shop =
        req.rawForm
        |> Json.parseFromBody<Shop>

    let! getUser = Database.getUserByShopId shop.Id.Value

    if Option.isSome getUser
    then
        let user = Option.get getUser

        // Erase the user's access token and shopify charge, but keep their shopId so we can easily reinstate their account if they ever install the app again.
        do!
            ({ user with shopifyAccessToken = None; myShopifyUrl = None; shopifyChargeId = None; })
            |> Database.updateUser user.id user.rev
            |> Async.Ignore

    // Write success even if the user wasn't found, as error responses will cause Shopify to continuously retry.
    return! Successful.OK "" ctx
}

let shopUpdated = request <| fun req ctx -> async {
    // The shop-updated webhook sends the Shop object in the body
    let shop =
        req.rawForm
        |> Json.parseFromBody<Shop>

    let! getUser = Database.getUserByShopId shop.Id.Value

    match getUser with
    | None -> ()
    | Some user ->
        // Update the user's shop name and shop url
        do!
            ({user with myShopifyUrl = Some shop.MyShopifyDomain; shopName = Some shop.Name })
            |> Database.updateUser user.id user.rev
            |> Async.Ignore

    // Write success even if the user wasn't found, as error responses will cause Shopify to continuously retry.
    return! Successful.OK "" ctx
}

let routes = [
    POST >=> choose [
        path "/api/v1/webhooks/app-uninstalled" >=> validShopifyWebhook appUninstalled
        path "/api/v1/webhooks/shop-updated" >=> validShopifyWebhook shopUpdated
    ]
]