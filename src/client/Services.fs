module Services

open Fable.Core
open Fable.PowerPack
open JsInterop
open Domain
open Fable.PowerPack.Fetch

let getResponse (request: Fable.Import.JS.Promise<Response>) = promise {
    let! result = Promise.result request

    match result with
    | Error e ->
        printfn "Problem with request: %A" e

        let message =
            try
                let ex = e :> System.Exception
                ex.Message
            with
            | _ -> "There was a problem with the request and it could not be completed."

        let resp: ErrorResponse =
            { message = message
              statusCode = -1
              statusDescription = "Request Error" }

        return Error resp
    | Ok result ->
        let! text = result.text()

        return
            if result.Ok then Ok text
            else ofJson<ErrorResponse> text |> Error
}

[<PassGenerics>]
let parseResponseText<'a> (response: Fable.Import.JS.Promise<Result<string, Domain.ErrorResponse>>) =
    response
    |> Promise.map (fun response ->
        match response with
        | Ok s -> ofJson<'a> s |> Ok
        | Error s -> Error s
    )

[<PassGenerics>]
let sendRequest (url: string) (method: HttpMethod) (record: 'T option) =
    let reqProps =
        match record with
        | Some t ->
            [
                Body !^(toJson t)
                requestHeaders [ContentType "application/json"]
            ]
        | None -> []
        |> List.append [
            Method method
            // Must set fetch Credentials to SameOrigin or Include, else fetch won't set or send cookies.
            Credentials RequestCredentials.Sameorigin ]

    GlobalFetch.fetch(RequestInfo.Url url, requestProps reqProps)

module Auth =
    open Domain.Requests.OAuth
    let createOauthUrl (myShopifyDomain: string) =
        let apiUrl = 
            Paths.Api.Auth.createUrl.ToString()
            |> fun s -> sprintf "%s?domain=%s" s myShopifyDomain

        sendRequest apiUrl HttpMethod.POST None
        |> getResponse
        |> Promise.map (Result.map ofJson<GetShopifyOauthUrlResult>)

    let loginOrRegister rawQueryString =
        { rawQueryString = rawQueryString }
        |> Some
        |> sendRequest (Paths.Api.Auth.loginOrRegister.ToString()) HttpMethod.PUT
        |> getResponse

module Billing =
    open Domain.Requests.OAuth
    let createChargeUrl () =
        sendRequest (Paths.Api.Billing.createUrl.ToString()) HttpMethod.POST None 
        |> getResponse
        |> Promise.map (Result.map ofJson<GetShopifyOauthUrlResult>)

    let createOrUpdateCharge rawQueryString =
        { rawQueryString = rawQueryString }
        |> Some
        |> sendRequest (Paths.Api.Billing.createOrUpdateCharge.ToString()) HttpMethod.PUT
        |> getResponse

module Orders = 
    open Domain.Requests.Orders

    let listOrders page limit = 
        let url = sprintf "%s?limit=%i&page=%i" (Paths.Api.Orders.list.ToString()) limit page
        sendRequest url HttpMethod.GET None 
        |> getResponse 
        |> Promise.map (Result.map ofJson<ListOrdersResponse>) 