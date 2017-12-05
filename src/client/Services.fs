module Services

open Fable.Core
open Fable.PowerPack
open JsInterop
open Domain
open Fable.PowerPack.Fetch

let getResponse (request: Fable.Import.JS.Promise<Response>) = promise {
    let! result = request
    let! text = result.text()

    return
        match result.Ok with
            | true ->
                Ok text
            | false ->
                ofJson<ErrorResponse> text
                |> Error
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
    let login (data: CreateSession) =
        Some data
        |> sendRequest "/api/v1/auth" HttpMethod.POST
        |> getResponse