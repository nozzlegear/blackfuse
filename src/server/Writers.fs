module Writers 

open Suave 
open Operators
open Suave.Logging

let private logger = Log.create "Writers"

let writeStatusInt status = 
    match HttpCode.tryParse status with 
    | Choice1Of2 code -> Writers.setStatus code 
    | Choice2Of2 msg -> 
        Message.eventX "Failed to parse status code {status} to Suave HttpCode. Error: {msg}"
        >> Message.setField "status" status 
        >> Message.setField "msg" msg 
        |> logger.error

        failwith "Failed to parse status code to Suave HttpCode"

let writeJson status a = 
    Json.stringify a 
    |> Successful.OK 
    >=> writeStatusInt status
    >=> Writers.setMimeType Json.MimeType

let writeEmpty status = 
    Successful.OK ""    
    >=> writeStatusInt status 