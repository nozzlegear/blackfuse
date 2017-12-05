/// Utilities for working with Fable.Validation. This module does not contain the validation logic itself, such logic is typically attached to the type being validated.
module Validation

let messageIsEmpty (propName, errorList) =
    let propNameEmpty = System.String.IsNullOrEmpty propName
    let errorListEmpty = Seq.isEmpty errorList

    propNameEmpty || errorListEmpty

let getMessage (errorMap: Map<string, string list>) =
    Map.toSeq errorMap
    |> Seq.filter (messageIsEmpty >> not)
    |> Seq.map (fun (propName, errorList) ->
        errorList
        |> Seq.map (fun e -> sprintf "%s %s" propName e)
        |> String.concat "; ")
    |> String.concatAndAppend ". "