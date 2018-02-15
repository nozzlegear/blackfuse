module Session

open Domain
open System
open System.Text
open System.Security.Cryptography

/// Takes a Session record and signs it with HMACSHA256 and your application's secret key, returning the Session with its `signature` label set to the result.
let sign (session: Session) =
    use hasher = new HMACSHA256(Encoding.UTF8.GetBytes ServerConstants.sessionTokenSecretKey)

    // This is a gross hack, but I was running into an issue (bug?) where signing a session from the `tryDecode` function below would
    // produce a different hash than one that was just created as a record. The problem was, when stringified to JSON, the parsed
    // json's .price label would be 9.00 (two zeroes) and the one created from F# would be 9.0 (one zero). So to hack around that I'm
    // just stringifying, parsing, then stringifying again to get them to the same "state" and be equal. 
    let hash = 
        session.ToEncodable()
        |> Json.stringify
        |> Json.parse<Session>
        |> Json.stringify
        |> Text.Encoding.UTF8.GetBytes
        |> hasher.ComputeHash
        |> Convert.ToBase64String

    { session with signature = hash }

/// Tries to decode a JSON-encoded Session record and verify that it is authentic. It does this by hashing the decoded session with your application's secret key, and comparing that hash to the signature value.
let tryDecode (sessionStr: string) : Session option =
    try
        let session = Json.parse<Session> sessionStr
        let signed = sign session 

        session.signature = signed.signature
        |> Option.ofBoolBack session
    with
    | _ -> None 