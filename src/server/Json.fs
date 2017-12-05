// Source/credit: SAFE-Stack https://github.com/SAFE-Stack/SAFE-BookStore/blob/65bb1b9049ee6b6c32c457e8ed1d876e6b96796d/src/Server/FableJson.fs
module Json
open Newtonsoft.Json

// The Fable.JsonConverter serializes F# types so they can be deserialized on the
// client side by Fable into full type instances, see http://fable.io/blog/Introducing-0-7.html#JSON-Serialization
// The converter includes a cache to improve serialization performance. Because of this,
// it's better to keep a single instance during the server lifetime.
let private jsonConverter = Fable.JsonConverter() :> JsonConverter

let stringify value =
    JsonConvert.SerializeObject(value, [|jsonConverter|])

let parse<'a> (json:string) : 'a =
    JsonConvert.DeserializeObject<'a>(json, [|jsonConverter|])

let parseFromBody<'a> (body: byte[]) =
    let json = System.Text.Encoding.UTF8.GetString body
    parse<'a> json

let MimeType = "application/json"