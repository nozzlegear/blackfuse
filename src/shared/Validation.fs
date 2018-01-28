/// Utilities for validating types. 
module Validation
open Fable

type ValidationErrors = Map<string, string list>

type ValidationResult = (string * string list) option

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

type ValidationInstance<'a> = {
    propertyName: string 
    value: 'a
    errors: string list 
}

let onProperty propertyName value = 
    { propertyName = propertyName 
      value = value
      errors = [] }

/// Finishes validation on a property, turning the chain into a ValidationResult.
let validate props: ValidationResult = 
    match Seq.isEmpty props.errors with 
    | true -> None 
    | false -> Some (props.propertyName, props.errors)

/// Turns a list of validations into a Result. If any validation fails, this function returns `Error` with a value of `Map<propertyName: string, propertyErrors: string list>`. 
let toResult onSubject (validators: ValidationResult list) =
    validators 
    |> Seq.filter Option.isSome 
    |> Seq.map Option.get
    |> Seq.fold (fun (state: ValidationErrors) (propertyName, errorMsgs) -> 
        if Map.containsKey propertyName state 
        then state.[propertyName]@errorMsgs
        else errorMsgs
        |> fun newVal -> Map.add propertyName newVal state
    ) Map.empty
    |> fun e -> if Map.isEmpty e then Ok onSubject else Error e

let maybeAddErrorMessage props = function 
    | Some s -> { props with errors = props.errors@[s] }
    | None -> props

let notBlank message props = 
    match props.value with 
    | null
    | "" -> message |> Option.defaultValue "cannot be blank or empty." |> Some
    | _ -> None 
    |> maybeAddErrorMessage props

let inline contains expected message props = 
    match props.value |> Seq.contains expected with
    | false -> message |> Option.defaultValue (sprintf "must contain '%A'." expected) |> Some
    | true -> None 
    |> maybeAddErrorMessage props

let inline notcontains value message props = 
    match props.value |> Seq.contains value with 
    | true -> message |> Option.defaultValue (sprintf "must not contain '%A'." value) |> Some
    | false -> None 
    |> maybeAddErrorMessage props

let inline minLength len message props = 
    match props.value |> Seq.length with
    | i when i < len -> message |> Option.defaultValue (sprintf "minimum length must be at least %i." len) |> Some
    | _ -> None    
    |> maybeAddErrorMessage props

let inline maxLength len message props = 
    match props.value |> Seq.length with
    | i when i > len -> message |> Option.defaultValue (sprintf "maximum length must not exceed %i." len) |> Some
    | _ -> None     
    |> maybeAddErrorMessage props

let inline length len message props = 
    match props.value |> Seq.length with
    | i when i <> len -> message |> Option.defaultValue (sprintf "length must be %i." len) |> Some
    | _ -> None 
    |> maybeAddErrorMessage props

let isTrue message props = 
    match props.value with 
    | false -> message |> Option.defaultValue "must be true." |> Some
    | true -> None
    |> maybeAddErrorMessage props 

let isFalse message props = 
    match props.value with 
    | true -> message |> Option.defaultValue "must be false." |> Some
    | false -> None
    |> maybeAddErrorMessage props 

let inline equals expected message props =
    match expected = props.value with 
    | false -> message |> Option.defaultValue (sprintf "must equal '%A'." expected) |> Some
    | true -> None 
    |> maybeAddErrorMessage props

let inline notequals expected message props =
    match expected = props.value with 
    | true -> message |> Option.defaultValue (sprintf "must not equal '%A'." expected) |> Some
    | false -> None
    |> maybeAddErrorMessage props