/// SOURCE: Suave (https://github.com/SuaveIO/suave/blob/56c061821abc7d55f462080e5c9bd28e5cf0e26b/src/Suave/Sscanf.fs)
/// Copyright 2014 Ademar Gonzalez, Henrik Feldt and contributors
/// Licensed under the Apache License, Version 2.0 (the "License");
/// you may not use this file except in compliance with the License.
/// You may obtain a copy of the License at
///      http://www.apache.org/licenses/LICENSE-2.0
/// Unless required by applicable law or agreed to in writing, software
/// distributed under the License is distributed on an "AS IS" BASIS,
/// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
/// See the License for the specific language governing permissions and
/// limitations under the License.
module PathScan

open Fable.Core
open System
open System.Text.RegularExpressions
open Microsoft.FSharp.Reflection

/// Verify that f x, and then return x, otherwise fail witha 'format failure' message
let private check f x = if f x then x else failwithf "format failure \"%s\"" x

let private parseDecimal x = Decimal.Parse(x, System.Globalization.CultureInfo.InvariantCulture)

let parseBoolean (s: string) = 
  printfn "Parsing bool from string %s" s
  match s.ToLower() with 
  | "true" -> true 
  | _ -> false

/// The supported characters for the formatter
let parsers =
  dict [
    'b', parseBoolean >> box
    'd', int64 >> box
    'i', int64 >> box
    's', box
    'u', uint32 >> int64 >> box
    'x', check (String.forall Char.IsLower) >> ((+) "0x") >> int64 >> box
    'X', check (String.forall Char.IsUpper) >> ((+) "0x") >> int64 >> box
    'o', ((+) "0o") >> int64 >> box
    'e', float >> box // no check for correct format for floats
    'E', float >> box
    'f', float >> box
    'F', float >> box
    'g', float >> box
    'G', float >> box
    'M', parseDecimal >> box
    'c', char >> box
  ]

// array of all possible formatters, i.e. [|"%b"; "%d"; ...|]
let separators =
  parsers.Keys
  |> Seq.map (fun c -> "%" + c.ToString())
  |> Seq.toArray

// Creates a list of formatter characters from a format string,
// for example "(%s,%d)" -> ['s', 'd']
let rec getFormatters xs =
  match xs with
  | '%' :: '%' :: xr -> getFormatters xr
  | '%' :: x :: xr   ->
    if parsers.ContainsKey x then x :: getFormatters xr
    else failwithf "Unknown formatter %%%c" x
  | x :: xr          -> getFormatters xr
  | []               -> []

// Coerce integer types from int64
let coerce o = function
  | v when v = typeof<int32> ->
    int32(unbox<int64> o) |> box
  | v when v = typeof<uint32> ->
    uint32(unbox<int64> o) |> box
  | _ -> o

/// Parse the format in 'pf' from the string 's' regardless of casing, failing and raising an exception
/// otherwise
[<PassGenerics>]
let sscanf (pf:PrintfFormat<_,_,_,_,'t>) s : 't =
  let formatStr  = pf.Value
  let constants  = formatStr.Split([|"%%"|], StringSplitOptions.None) 
                   |> Array.map (fun x -> x.Split(separators, StringSplitOptions.None))
  let regexStr   = constants 
                   |> Array.map (fun c -> c |> Array.map Regex.Escape |> String.concat "(.*?)")
                   |> String.concat "%"
  let regex      = Regex("^" + regexStr + "$", RegexOptions.IgnoreCase)
  let formatters = formatStr.ToCharArray() // need original string here (possibly with "%%"s)
                   |> Array.toList |> getFormatters
  let groups =
    regex.Match(s).Groups
    |> Seq.cast<Group>
    |> Seq.skip 1

  let matches =
    (groups, formatters)
    ||> Seq.map2 (fun g f -> g.Value |> parsers.[f])
    |> Seq.toArray

  if matches.Length = 1 then
    coerce matches.[0] typeof<'t> :?> 't
  else
    let tupleTypes = FSharpType.GetTupleElements(typeof<'t>)
    let matches =
      (matches,tupleTypes)
      ||> Array.map2 coerce
    FSharpValue.MakeTuple(matches, typeof<'t>) :?> 't

[<PassGenerics>]
let scan (pf : PrintfFormat<_,_,_,_,'t>) (path: string) =
    try
        let r = sscanf pf path
        Some r
    with _ -> None