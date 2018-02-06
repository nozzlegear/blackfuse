module Session

open Domain
open System
open System.IO
open System.Security.Cryptography

// Source: SAFE-BookStore https://github.com/SAFE-Stack/SAFE-BookStore/blob/c37a71f802f176a712ac803e4372d255bc0a225a/src/Server/JsonWebToken.fs
let private createPassPhrase() =
    let crypto = RandomNumberGenerator.Create()
    let randomNumber = Array.init 32 byte
    crypto.GetBytes(randomNumber)
    randomNumber

let private passPhrase =
    let fi = FileInfo("./temp/token.txt")
    
    if not fi.Exists 
    then
        let passPhrase = createPassPhrase()
        if not fi.Directory.Exists 
        then fi.Directory.Create()

        File.WriteAllBytes(fi.FullName, passPhrase)

    File.ReadAllBytes(fi.FullName)

/// Takes a Session record and signs it with HMACSHA256 and your application's secret key, returning the Session with its `signature` label set to the result.
let sign (session: Session) =
    use hasher = new System.Security.Cryptography.HMACSHA256(passPhrase)
    let hash = 
        session.ToEncodable()
        |> Json.stringify
        |> Text.Encoding.UTF8.GetBytes
        |> hasher.ComputeHash
        |> Text.Encoding.UTF8.GetString

    { session with signature = hash }

/// Tries to decode a JSON-encoded Session record and verify that it is authentic. It does this by hashing the decoded session with your application's secret key, and comparing that hash to the signature value.
let tryDecode (sessionStr: string) : Session option =
    try
        let session = Json.parse<Session> sessionStr
        let signed = sign session 
        
        if session.signature = signed.signature 
        then Some session
        else None 
    with
    | _ -> None 