module Option

let ofBool a b =
    if a then Some b else None

let ofBoolBack b a =
    ofBool a b

let ofNullable a =
    if isNull a then None else Some a