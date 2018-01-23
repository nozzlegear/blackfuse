module Paths

module Api =
    module Auth =
        let createUrl = "/api/v1/oauth/create-url"
        let loginOrRegister = "/api/v1/oauth"

    module Billing =
        let createUrl = "/api/v1/billing/create-url"
        let createOrUpdateCharge = "/api/v1/billing"

    module User =
        let getInfo = "/api/v1/user"

    module Webhooks =
        let appUninstalled = "/api/v1/webhooks/app/uninstalled"
        let shopUpdated = "/api/v1/webhooks/shop/update"

module Client =
    let home = "/"

    module Billing =
        let index = "/billing"
        let result = "/billing/result"

    module Auth =
        let login = "/auth/login"
        let logout = "/auth/logout"
        let register = "/auth/register"
        let completeOAuth = "/auth/complete-oauth"