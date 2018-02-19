module Routes.Webhooks

open Suave
open Filters
open Operators
open WebhookProcessor

let respondToWebhook (processorMessage: byte[] -> WebhookMessage) = request <| fun req ctx ->
    processorMessage req.rawForm
    |> WebhookProcessor.post

    // Always write success as error responses will cause Shopify to continuously retry.
    Writers.writeEmpty 200 ctx

let routes = [
    POST >=> choose [
        Paths.Api.Webhooks.appUninstalled >@-> validShopifyWebhook (respondToWebhook HandleAppUninstalled)
        Paths.Api.Webhooks.shopUpdated >@-> validShopifyWebhook (respondToWebhook HandleShopUpdated)
    ]
]