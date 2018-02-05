module Pages.User.Index

open Fable.Core
open Fable.Import
module R = Fable.Helpers.React
module P = R.Props 
module C = Components

let Page _ = 
    fun _ ->
        let user = Mobx.get Stores.Auth.session |> Option.get
        let sub = user.subscription |> Option.get
        let domain = Option.get user.myShopifyUrl

        R.div [] [
            R.h1 [] [R.str "User Account"]
            R.hr []

            R.div [] [
                R.h3 [] [R.str <| Option.get user.shopName]

                R.p [] [R.str domain]

                user.created
                |> Date.fromUnixTimestamp
                |> fun d -> R.p [] [R.str <| sprintf "Member since %s." (Date.toMediumDateString d)]

                sprintf "%s: $%.2f/month" sub.planName sub.price
                |> fun s -> R.p [] [R.str s]

                R.p [] [R.str <| sprintf "User ID: %s" user.id]

                R.a [P.ClassName "btn red"; P.Href <| sprintf "https://%s/admin/apps" domain ] [
                    R.str <| sprintf "Uninstall %s" Constants.AppName
                ]
            ]
        ]
    |> MobxReact.Observer