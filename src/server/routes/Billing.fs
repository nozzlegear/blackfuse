module Routes.Billing

open System
open Domain
open Suave
open Filters
open Operators
open ShopifySharp
open Errors

let createChargeUrl = withUser <| fun user req ctx -> async {
    if Option.isSome user.shopifyChargeId
    then raise <| HttpException("Your account is already subscribed!", Status.UnprocessableEntity)

    let service = RecurringChargeService(user.myShopifyUrl, user.shopifyAccessToken)
    let charge = RecurringCharge()
    charge.Name <- sprintf "%s monthly subscription" Constants.AppName
    charge.Price <- Option.toNullable <| Some Constants.SubscriptionPrice
    charge.TrialDays <- Option.toNullable <| Some Constants.FreeTrialDays
    charge.Test <- Option.toNullable <| Some (not ServerConstants.isLive)
    charge.ReturnUrl <- Utils.toAbsoluteUrl req Paths.Billing.result |> string

    let! newCharge =
        service.CreateAsync charge
        |> Async.AwaitTask

    return!
        Successful.OK <| sprintf """{"url":"%s"}""" newCharge.ConfirmationUrl
        >=> Writers.setMimeType Json.MimeType
        <| ctx
}

let updateSubscriptionCharge = withUser <| fun user req ctx -> async {
    let body =
        req.rawForm
        |> Json.parseFromBody<Requests.OAuth.CompleteShopifyOauth>
        |> fun t -> t.Validate()
        |> function
        | Ok b -> b
        | Error e -> raise <| fromValidation e

    if not <| AuthorizationService.IsAuthenticRequest(body.rawQueryString, ServerConstants.shopifySecretKey)
    then raise <| HttpException("Request did not pass Shopify's validation scheme.", Status.Forbidden)

    raise <| HttpException("Not yet implemented", Status.InternalServerError)

    return! Successful.OK "" ctx
}

let routes = [
    POST >=> choose [
        path "/api/v1/billing/create-charge-url" >=> createChargeUrl
        path "/api/v1/billing/update" >=> updateSubscriptionCharge
    ]
]