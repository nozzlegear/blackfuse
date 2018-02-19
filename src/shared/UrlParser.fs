/// Source for almost all code in this module is Fable-Elmish
/// https://github.com/fable-elmish/browser/tree/02b498520aa34592689e402ef31b3cbfffe177ef/src

(** UrlParser
------
This port of the Elm library helps you turn URLs into nicely structured data.
It is designed to be used with `Navigation` module to help folks create
single-page applications (SPAs) where you manage browser navigation yourself.
*)

module UrlParser


(** 
#### Types
*)

type State<'v> =
  { visited : string list
    unvisited : string list
    args : Map<string,string>
    value : 'v }

[<RequireQualifiedAccess>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module internal State =
  let mkState visited unvisited args value =
        { visited = visited
          unvisited = unvisited
          args = args
          value = value }
          
  let map f { visited = visited; unvisited = unvisited; args = args; value = value } =
        { visited = visited
          unvisited = unvisited
          args = args
          value = f value }


/// Turn URLs like `/blog/42/cat-herding-techniques` into nice data.
type Parser<'a,'b> = State<'a> -> State<'b> list

let internal trimSlashes (str: string) = str.Trim '/'

(** 
#### Parse segments
Create a custom path segment parser. You can use it to define something like “only CSS files” like this:
```
    let css =
      custom "CSS_FILE" <| fun segment ->
        if String.EndsWith ".css" then
          Ok segment
        else
          Error "Does not end with .css"
```
*)
let custom stringToSomething : Parser<_,_> =
    let inner { visited = visited; unvisited = unvisited; args = args; value = value } =
        match unvisited with
        | [] -> []
        | next :: rest ->
            match stringToSomething next with
            | Ok nextValue ->
                [ State.mkState (next :: visited) rest args (value nextValue) ]

            | Error _ ->
                []
    inner


(** Parse a segment of the path as a `string`.
```
    parse str location
```
<pre>
    /alice/  ==>  Some "alice"
    /bob     ==>  Some "bob"
    /42/     ==>  Some "42"
</pre>
*)
let str state =
    custom Ok state


(** Parse a segment of the path as an `int`.
```
    parse i32 location
```
<pre>
    /alice/  ==>  None
    /bob     ==>  None
    /42/     ==>  Some 42
</pre>
*)
let i32 state =
    custom (System.Int32.TryParse >> function true, value -> Ok value | _ -> Error "Can't parse int" ) state

/// Parse the entire string as is, with a value type of unit.
let hardcoded (path: string): Parser<unit -> 'a, 'a> = 
    let path = trimSlashes path

    let inner { visited = visited; unvisited = unvisited; args = args; value = value } =
        let currentPath = System.String.Join("/", unvisited) |> trimSlashes

        match path = currentPath with 
        | true -> [ State.mkState (unvisited@visited) [] args (value ())]
        | false -> []

    inner

(** Parse a segment of the path if it matches a given string.
```
    s "blog"  // can parse /blog/
              // but not /glob/ or /42/ or anything else
```
*)
let s str : Parser<_,_> =
    let str = trimSlashes str 
    let segmentCount = 
        str.ToCharArray() 
        |> Array.filter (fun c -> c = '/')
        |> Array.length
        |> fun i -> i + 1 // Add one because e.g. 2 slashes means 3 segments (api/v1/something)

    let inner { visited = visited; unvisited = unvisited; args = args; value = value } =
        if List.isEmpty unvisited || List.length unvisited < segmentCount
        then [] 
        else 
            let segments, remaining = List.take segmentCount unvisited, List.skip segmentCount unvisited
            let next = System.String.Join("/", segments)

            if next = str
            then [ State.mkState (segments@visited) remaining args value ]
            else []
    inner

(** 
#### Combining parsers
Parse a path with multiple segments.

```
    parse (s "blog" </> i32) location
```
<pre>
    /blog/35/  ==>  Some 35
    /blog/42   ==>  Some 42
    /blog/     ==>  None
    /42/       ==>  None
</pre>
```
    parse (s "search" </> str) location
```
<pre>
    /search/cats/  ==>  Some "cats"
    /search/frog   ==>  Some "frog"
    /search/       ==>  None
    /cats/         ==>  None
</pre>
*)
let inline (</>) (parseBefore:Parser<_,_>) (parseAfter:Parser<_,_>) =
  fun state ->
    List.collect parseAfter (parseBefore state)

(** Transform a path parser.
```
    type Comment = { author : string; id : int }
    rawComment =
      s "user" </> str </> s "comments" </> i32
    comment =
      map (fun a id -> { author = a; id = id }) rawComment
    parse comment location
```
<pre>
    /user/bob/comments/42  ==>  Some { author = "bob"; id = 42 }
    /user/tom/comments/35  ==>  Some { author = "tom"; id = 35 }
    /user/sam/             ==>  None
</pre>
*)
let map (subValue:'a) (parse:Parser<'a,'b>) : Parser<'b->'c,'c> =
    let inner { visited = visited; unvisited = unvisited; args = args; value = value } =
        List.map (State.map value) 
        <| parse { visited = visited
                   unvisited = unvisited
                   args = args
                   value = subValue }
    inner
  


(** Try a bunch of different path parsers.
```
    type Route
      = Search of string
      | Blog of int
      | User of string
      | Comment of string*int
    route =
      oneOf
        [ map Search  (s "search" </> str)
          map Blog    (s "blog" </> i32)
          map User    (s "user" </> str)
          map Comment (s "user" </> str </> "comments" </> i32) ]
    parse route location
```
<pre>
    /search/cats           ==>  Some (Search "cats")
    /search/               ==>  None
    /blog/42               ==>  Some (Blog 42)
    /blog/cats             ==>  None
    /user/sam/             ==>  Some (User "sam")
    /user/bob/comments/42  ==>  Some (Comment "bob" 42)
    /user/tom/comments/35  ==>  Some (Comment "tom" 35)
    /user/                 ==>  None
</pre>
*)
let oneOf parsers state =
    List.collect (fun parser -> parser state) parsers


(** A parser that does not consume any path segments.
```
    type BlogRoute = Overview | Post of int
    blogRoute =
      oneOf
        [ map Overview top
          map Post  (s "post" </> i32) ]
    parse (s "blog" </> blogRoute) location
```
<pre>
    /blog/         ==>  Some Overview
    /blog/post/42  ==>  Some (Post 42)
</pre>
*)
let top state=
    [state]

(** 
#### Query parameters
Turn query parameters like `?name=tom&age=42` into nice data.

*)

type QueryParser<'a,'b> = State<'a> -> State<'b> list


(** Parse some query parameters.
```
    type Route = BlogList (Option string) | BlogPost Int
    route =
      oneOf
        [ map BlogList (s "blog" <?> stringParam "search")
          map BlogPost (s "blog" </> i32) ]
    parse route location
```
<pre>
    /blog/              ==>  Some (BlogList None)
    /blog/?search=cats  ==>  Some (BlogList (Some "cats"))
    /blog/42            ==>  Some (BlogPost 42)
</pre>
*)
let inline (<?>) (parser:Parser<_,_>) (queryParser:QueryParser<_,_>) : Parser<_,_> =
    fun state ->
        List.collect queryParser (parser state)

(** Create a custom query parser. You could create parsers like these:
```
    val jsonParam : string -> Decoder a -> QueryParser (Option a -> b) b
    val enumParam : string -> Map<string,a> -> QueryParser (Option a -> b) b
```
*)
let customParam (key: string) (func:string option -> _) : QueryParser<_,_> =
    let inner { visited = visited; unvisited = unvisited; args = args; value = value } =
        [ State.mkState visited unvisited args (value (func (Map.tryFind key args))) ]
    inner


(** Parse a query parameter as a `string`.
```
    parse (s "blog" <?> stringParam "search") location
```
<pre>
    /blog/              ==>  Some (Overview None)
    /blog/?search=cats  ==>  Some (Overview (Some "cats"))
</pre>
*)
let stringParam name =
    customParam name id

let internal intParamHelp =
    Option.bind 
        (fun value ->
            match System.Int32.TryParse value with
            | (true,x) -> Some x
            | _ -> None)

(** Parse a query parameter as an `int`. Option you want to show paginated
search results. You could have a `start` query parameter to say which result
should appear first.
```
    parse (s "results" <?> intParam "start") location
```
<pre>
    /results           ==>  Some None
    /results?start=10  ==>  Some (Some 10)
</pre>
*)
let intParam name =
    customParam name intParamHelp


// PARSER HELPERS

let rec internal parseHelp states =
    match states with
    | [] ->
        None
    | state :: rest ->
        match state.unvisited with
        | [] ->
            Some state.value
        | [""] ->
            Some state.value
        | _ ->
            parseHelp rest

let internal splitUrl (url:string) =
    match List.ofArray <| url.Split([|'/'|]) with
    | "" :: segments ->
        segments
    | segments ->
        segments

let internal toKeyValuePair (segment:string) =
    match segment.Split('=') with
    | [| key; value |] ->
        Option.tuple (Option.ofFunc System.Uri.UnescapeDataString key) (Option.ofFunc System.Uri.UnescapeDataString value)
    | _ -> None

let internal parseParams (querystring:string) =
    if querystring.StartsWith "?" 
    then querystring.Substring(1).Split('&')
    else querystring.Split('&')
    |> Seq.map toKeyValuePair
    |> Seq.choose id
    |> Map.ofSeq

(** 
#### Parsers
*)

/// Parse based on path and querystring. This parser ignores the hash entirely.
let parse (parser:Parser<_,_>) (path: string) (qs: string) =
    { visited = []
      unvisited = splitUrl path
      args = (parseParams qs)
      value = id }
    |> parser
    |> parseHelp