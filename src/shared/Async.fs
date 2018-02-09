module Async 

let Map fn computation = async {
    let! result = computation

    return fn result
}

let Bind fn computation = async {
    let! result = computation 

    return! fn result
}

let Filter fn (computation: Async<seq<'a>>) = async {
    let! result = computation 

    return Seq.filter fn result
}