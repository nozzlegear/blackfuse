module Date

open System

let toUnixTimestamp date =
    let offset = DateTimeOffset date
    offset.ToUnixTimeSeconds()

let toJsTimestamp date =
    let offset = DateTimeOffset date
    offset.ToUnixTimeMilliseconds()

let fromUnixTimestamp ts =
    let offset = DateTimeOffset.FromUnixTimeSeconds ts
    offset.DateTime

let fromJsTimestamp ts =
    let offset = DateTimeOffset.FromUnixTimeMilliseconds ts
    offset.DateTime