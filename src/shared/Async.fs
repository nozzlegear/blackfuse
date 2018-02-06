module Async 

let Map fn computation = async {
    let! result = computation

    return fn result
}

let Bind fn computation = async {
    let! result = computation 

    return! fn result
}