module Option

let ofBool a b =
    if a then Some b else None

let ofBoolBack b a =
    ofBool a b

let ofNullable a =
    if isNull a then None else Some a

/// Converts the string option to a string. `match opt with | Some o -> o | None -> ""`
let toString (a: string option) =
    match a with
    | Some a -> a
    | None -> ""