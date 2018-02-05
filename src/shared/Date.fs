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

/// Formats a DateTime object to a MMM dd, yyyy string. This function is used on the frontend because Fable does not implement all .NET datetime formats:
/// https://github.com/fable-compiler/Fable/blob/5a2605d53010d2c3c1074bbf68ad56f2d33a0a46/src/js/fable-core/Date.ts#L66
let toMediumDateString (d: DateTime) = 
    let month = 
        match d.Month with 
        | 1 -> "Jan"
        | 2 -> "Feb"
        | 3 -> "Mar"
        | 4 -> "Apr"
        | 5 -> "May"
        | 6 -> "Jun"
        | 7 -> "Jul"
        | 8 -> "Aug"
        | 9 -> "Sep"
        | 10 -> "Oct"
        | 11 -> "Nov"
        | _ -> "Dec"

    d.ToString("dd, yyyy")
    |> sprintf "%s %s" month