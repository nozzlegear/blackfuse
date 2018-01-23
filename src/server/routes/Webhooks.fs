module Routes.Webhooks

open Suave
open Filters
open Operators
open WebhookProcessor

let respondToWebhook (processorMessage: byte[] -> WebhookMessage) = request <| fun req ctx ->
    processorMessage req.rawForm
    |> WebhookProcessor.post

    // Always write success as error responses will cause Shopify to continuously retry.
    Successful.OK "" ctx

let routes = [
    POST >=> choose [
        path Paths.Api.Webhooks.appUninstalled >=> validShopifyWebhook (respondToWebhook HandleAppUninstalled)
        path Paths.Api.Webhooks.shopUpdated >=> validShopifyWebhook (respondToWebhook HandleShopUpdated)
    ]
]