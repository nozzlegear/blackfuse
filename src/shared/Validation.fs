/// Utilities for validating types. 
module Validation

type ValidationErrors = Map<string, string list>

let messageIsEmpty (propName, errorList) =
    let propNameEmpty = System.String.IsNullOrEmpty propName
    let errorListEmpty = Seq.isEmpty errorList

    propNameEmpty || errorListEmpty

let getMessage (errorMap: ValidationErrors) =
    Map.toSeq errorMap
    |> Seq.filter (messageIsEmpty >> not)
    |> Seq.map (fun (propName, errorList) ->
        errorList
        |> Seq.map (fun e -> sprintf "%s %s" propName e)
        |> String.concat "; ")
    |> String.concatAndAppend ". "

type Validator = 
    | NotBlank of string * string * string

let validate onSubject validators =
    if true
    then Ok onSubject 
    else Error Map.empty    
