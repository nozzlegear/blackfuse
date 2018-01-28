module AppEntry

open Fable.Core.JsInterop
open Fable.Import
open Fable.Helpers.React.Props
open Fable.Core
module R = Fable.Helpers.React
module Nav = Fable.Import.ReactMaterialNavbar
module Icon = Fable.Import.ReactIcons
module Sidebar = Fable.Import.ReactSidebar
module NavStore = Stores.Nav
module AuthStore = Stores.Auth

// Import CSS bundle
importSideEffects "./public/css/all.styl"

type RequiredAuth =
    | WithSubscription
    | WithoutSubscription

let nav =
    let hamburger = Icon.hamburgerIcon [Icon.Size 25; Icon.Color "#fff"; Icon.ClassName "pointer"; Icon.OnClick (ignore >> NavStore.toggleNavIsOpen)]
    Nav.navbar [Nav.Title Constants.AppName; Nav.Background BrowserConstants.ThemeColor; Nav.LeftAction hamburger]

let navMenu () =
    let link href text =
        R.div [] [
            Router.link href [] [
                R.str text
            ]
        ]
    let linebreak = R.hr []

    R.div [] [
        R.menu [ClassName "nav-menu"] [
            link Paths.Client.home "Open Orders"
            link "#" "Closed Orders"
            link "#" "Tracking Widget"
            link "#" "Automation Rules"
            link "#" "Help & Support"
            linebreak
            link "#" "Account Settings"
            link "#" "My Stages"
            linebreak
            match Mobx.get AuthStore.isAuthenticated with
            | true -> Paths.Client.Auth.logout, "Sign out"
            | false -> Paths.Client.Auth.login, "Sign in"
            |> fun (path, text) -> link path text
        ]
    ]

let sidebar () =
    Sidebar.sidebar [
        Sidebar.SidebarClassName "react-sidebar"
        Sidebar.ContentClassName "react-sidebar-content"
        Sidebar.Sidebar <| navMenu ()
        Sidebar.Open <| Mobx.get NavStore.navIsOpen
        Sidebar.Docked false
        Sidebar.OnSetOpen NavStore.setNavIsOpen
    ]

let body = R.div [Id "body"]

let withNav child =
    R.div [] [
        nav
        MobxReact.Observer (fun _ ->
            sidebar () [
                body [child]
            ]
        )
    ]

let withoutNav child =
    R.div [Id "minimal"] [
        body [child]
    ]

let appRoutes: Router.Route list =
    let requireAuth kind _ =
        match kind, Mobx.get Stores.Auth.isAuthenticated, Mobx.get Stores.Auth.hasSubscription with
        | WithSubscription, true, true
        | WithoutSubscription, true, _ -> None
        | WithoutSubscription, false, _
        | WithSubscription, false, _ -> Some Paths.Client.Auth.login
        | WithSubscription, true, false -> Some Paths.Client.Billing.index

    let logout _ =
        Stores.Auth.logOut()
        JsCookie.remove Constants.CookieName
        Some Paths.Client.Auth.login

    let emptyPage _ = R.noscript [] []        

    [
        // Redirect requests to the / page to /dashboard instead. This allows using route variables on the dashboard because it doesn't think all URL segments are just variables.
        Router.routeWithGuard "/" (fun _ -> Some Paths.Client.home) emptyPage
        Router.routeWithGuard Paths.Client.Auth.logout logout emptyPage

        Router.groupWithGuard withNav (requireAuth WithSubscription) [
            Router.route Paths.Client.home Pages.Home.Index.PageZero
            Router.routeScan Paths.Client.homeWithPageScan Pages.Home.Index.Page
        ]

        Router.group withoutNav [
            Router.route Paths.Client.Auth.login <| Pages.Auth.LoginOrRegister.Page Pages.Auth.LoginOrRegister.Login
            Router.route Paths.Client.Auth.register <| Pages.Auth.LoginOrRegister.Page Pages.Auth.LoginOrRegister.Register
            Router.route Paths.Client.Auth.completeOAuth <| Pages.Auth.CompleteOauth.Page
        ]

        Router.groupWithGuard withoutNav (requireAuth WithoutSubscription) [
            Router.route Paths.Client.Billing.index <| Pages.Billing.GetUrl.Page
            Router.route Paths.Client.Billing.result <| Pages.Billing.Result.Page
        ]
    ]

let notFoundPage _ =
    R.div [Id "minimal"; ClassName "not-found-container"] [
        body [
            ReactIcons.errorIcon [ReactIcons.Size 400; ReactIcons.ClassName "error-icon"; ReactIcons.Color BrowserConstants.ThemeColor]
            R.div [Id "not-found"] [
                R.div [Id "text"] [
                    R.h3 [] [
                        R.str "Error 404: This page is unknown or does not exist."
                    ]
                    R.p [] [
                        R.str "Sorry about that, but the page you are looking for doesn't exist."
                    ]
                    R.div [Id "rescue-button-container"] [
                        Router.link Paths.Client.home [Id "rescue-button"; ClassName "btn blue"] [R.str "Go to Dashboard"]
                    ]
                ]
            ]
        ]
    ]

let app =
    MobxReact.Provider [
        Router.router appRoutes notFoundPage
    ]

ReactDom.render(app, Browser.document.getElementById "contenthost")