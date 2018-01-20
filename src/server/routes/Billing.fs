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

    // The charge result redirection does not include values to verify that the request is authentic.
    // You're instead expected to load the charge id via the Shopify API.

    let qs =
        AuthorizationService.ParseRawQuerystring body.rawQueryString
        |> Seq.map (|KeyValue|)
        |> Map.ofSeq

    let chargeId =
        try qs.Item "charge_id" |> Int64.Parse
        with _ -> raise <| badData "Missing charge_id value in Shopify redirected querystring."

    let service = RecurringChargeService(user.myShopifyUrl, user.shopifyAccessToken)
    let! charge = service.GetAsync chargeId |> Async.AwaitTask

    do!
        match charge.Status with
        | "accepted" -> service.ActivateAsync chargeId |> Async.AwaitTask
        | "active" -> async { () } // Charge has already been activated. No reason to throw an exception.
        | "expired" -> raise <| HttpException("It looks like your request has timed out. Please try again.", Status.PreconditionFailed)
        | s -> raise <| HttpException(sprintf "You must accept the subscription charge to use %s. Charge status: %s." Constants.AppName s, Status.PreconditionFailed)

    // Update the user's database model
    let! user = Database.updateUser user.id user.rev ({user with shopifyChargeId = Some chargeId})

    return!
        Successful.OK "{}"
        >=> Writers.setMimeType Json.MimeType
        >=> Cookie.setCookie (Routes.Auth.createSessionCookie user)
        <| ctx
}

let routes = [
    POST >=> path "/api/v1/billing/create-charge-url" >=> createChargeUrl
    PUT >=> path "/api/v1/billing/update" >=> updateSubscriptionCharge
]