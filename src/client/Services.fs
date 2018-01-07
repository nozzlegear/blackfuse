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
    open Domain.Requests.Auth
    let getShopifyOauthUrl (myShopifyDomain: string) =
        let apiUrl = sprintf "/api/v1/auth/oauth/shopify?domain=%s" myShopifyDomain

        sendRequest apiUrl HttpMethod.GET None
        |> getResponse
        |> Promise.map (fun r ->
            match r with
            | Ok s -> ofJson<GetShopifyOauthUrlResult> s |> Ok
            | Error e -> Error e
        )

    let completeOauth(rawQueryString: string) =
        { rawQueryString = rawQueryString }
        |> Some
        |> sendRequest "/api/v1/auth/oauth/shopify" HttpMethod.POST
        |> getResponse