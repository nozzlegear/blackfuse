module Paths

/// NOTE: There appears to be a bug where changing a pathscan and rebuilding with webpack watch will
/// make everything compile fine, but when the path is parsed and converted to a tuple by the PathScan
/// module the latest values will be undefined. The only fix right now appears to be stopping webpack
/// compilation and then starting it over.
/// 
/// Reproduction: 
/// 1. Start webpack watch compilation
/// 2. PathScan.scan "/hello/%s/%s" "/hello/world/foo" -> Some (string "world", string "foo")
/// 3. Change the above line to PathScan.scan "/hello/%s/%s/%i" "/hello/world/foo/5"
/// 4. Result is Some (string "world", string "foo", undefined)
/// 5. Stop webpack compilation and restart it.
/// 6. Result is now Some (string "world", string "foo", int 5)
let makeScan (path: PrintfFormat<_, System.IO.TextWriter, unit, unit, 't>) = path

module Api =
    module Auth =
        let createUrl = "/api/v1/oauth/create-url"
        let loginOrRegister = "/api/v1/oauth"

    module Billing =
        let createUrl = "/api/v1/billing/create-url"
        let createOrUpdateCharge = "/api/v1/billing"

    module Orders = 
        let list = "/api/v1/orders"

    module User =
        let getInfo = "/api/v1/user"

    module Webhooks =
        let appUninstalled = "/api/v1/webhooks/app/uninstalled"
        let shopUpdated = "/api/v1/webhooks/shop/update"

module Client =
    let home = "/dashboard"
    let homeWithPageScan = makeScan "/dashboard/%i" 

    module Billing =
        let index = "/billing"
        let result = "/billing/result"

    module Auth =
        let login = "/auth/login"
        let logout = "/auth/logout"
        let register = "/auth/register"
        let completeOAuth = "/auth/complete-oauth"