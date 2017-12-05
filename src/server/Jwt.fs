// Source: SAFE-BookStore https://github.com/SAFE-Stack/SAFE-BookStore/blob/c37a71f802f176a712ac803e4372d255bc0a225a/src/Server/JsonWebToken.fs
module Jwt

//  Learn about JWT https://jwt.io/introduction/
//  This module uses the JOSE-JWT library https://github.com/dvsekhvalnov/jose-jwt

open Domain
open System
open System.IO
open System.Security.Cryptography

type Expiration =
    | DefaultExpiration
    | CustomExpiration of DateTimeOffset

let private createPassPhrase() =
    let crypto = RandomNumberGenerator.Create()
    let randomNumber = Array.init 32 byte
    crypto.GetBytes(randomNumber)
    randomNumber

let private passPhrase =
    let fi = FileInfo("./temp/token.txt")
    if not fi.Exists then
        let passPhrase = createPassPhrase()
        if not fi.Directory.Exists then
            fi.Directory.Create()
        File.WriteAllBytes(fi.FullName,passPhrase)
    File.ReadAllBytes(fi.FullName)

let private encodeString (payload:string) =
    Jose.JWT.Encode(payload, passPhrase, Jose.JwsAlgorithm.HS256)

let private decodeString (jwt:string) =
    Jose.JWT.Decode(jwt, passPhrase, Jose.JwsAlgorithm.HS256)

let timestampExpired unixTimestamp =
    DateTimeOffset.UtcNow.ToUnixTimeSeconds() > unixTimestamp

let encode expiration (user: User) =
    let expTicks =
        match expiration with
        | DefaultExpiration -> DateTimeOffset.UtcNow.AddDays 90.
        | CustomExpiration d -> d
        |> fun d -> d.ToUnixTimeSeconds()

    SessionToken.FromUser expTicks user
    |> Json.stringify
    |> encodeString

let tryDecode (jwt:string) : SessionToken option =
    try
        let token = decodeString jwt |> Json.parse<SessionToken>

        match timestampExpired token.exp with
        | false -> Some token
        | true -> None
    with
    | _ -> None