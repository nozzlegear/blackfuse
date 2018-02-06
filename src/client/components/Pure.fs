module Pure 

open Fable.Import.React
module R = Fable.Helpers.React
module P = R.Props

type Size = 
    | Small of int 
    | Medium of int 
    | Large of int 
    | ExtraLarge of int 
    /// The default unit size. Any other size takes precedence over this size.
    | Base of int 

let getClassNames sizes = 
    let rec getClassName sizes className = 
        match sizes with 
        | Small i::sizes -> 
            sprintf "%s pure-u-sm-%i-24" className i
            |> getClassName sizes
        | Medium i::sizes -> 
            sprintf "%s pure-u-md-%i-24" className i
            |> getClassName sizes
        | Large i::sizes ->
            sprintf "%s pure-u-lg-%i-24" className i
            |> getClassName sizes
        | ExtraLarge i::sizes ->
            sprintf "%s pure-u-xl-%i-24" className i
            |> getClassName sizes
        | Base i::sizes ->
            sprintf "%s pure-u-%i-24" className i
            |> getClassName sizes
        | [] -> className

    getClassName sizes "" 

let Grid classNames (attributes: Fable.Helpers.React.Props.IHTMLProp list) = 
    let atts = attributes@[P.ClassName <| sprintf "pure-g %s" classNames]    
    R.div atts

let Unit classNames sizes (attributes: Fable.Helpers.React.Props.IHTMLProp list) = 
    let className = 
        getClassNames sizes
        |> sprintf "%s %s" classNames
    R.div (attributes@[P.ClassName className])