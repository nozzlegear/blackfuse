module Paths

open UrlParser

type PathParser<'a> = string -> string -> 'a option

type PlainPath<'a> = PathParser<'a> * string

type ComplexPath<'a> = PathParser<'a> * ('a -> string)

type Path<'a> = 
    | Plain of PlainPath<'a>
    | Complex of ComplexPath<'a>
    with 
    member x.ToString: 'a -> string = 
        match x with 
        | Plain (_, path) -> fun _ -> path
        | Complex (_, getPath) -> getPath
    member x.Parse = 
        match x with 
        | Complex (parse, _) -> parse
        | Plain (parse, _) -> parse

let plain s: Path<unit> = Plain (parse (hardcoded s), s) 

let complex url getUrl = Complex (parse url, getUrl)

module Api =
    module Auth =
        let createUrl = plain "/api/v1/oauth/create-url"
        let loginOrRegister = plain "/api/v1/oauth"

    module Billing =
        let createUrl = plain "/api/v1/billing/create-url"
        let createOrUpdateCharge = plain "/api/v1/billing"

    module Orders = 
        let list = plain "/api/v1/orders"

    module User =
        let getInfo = plain "/api/v1/user"

    module Webhooks =
        let appUninstalled = plain "/api/v1/webhooks/app/uninstalled"
        let shopUpdated = plain "/api/v1/webhooks/shop/update"

module Client =
    let home = plain "/dashboard"
    let homeWithPage = complex (s "/dashboard" </> i32) (sprintf "/dashboard/%i")

    module Auth =
        let login = plain "/auth/login"
        let logout = plain "/auth/logout"
        let register = plain "/auth/register"
        let completeOAuth = plain "/auth/complete-oauth"

    module Billing =
        let index = plain "/billing"
        let result = plain "/billing/result"
    
    module User = 
        let index = plain "/user"