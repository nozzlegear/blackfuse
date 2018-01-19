module AppEntry

open Fable.Core.JsInterop
open Fable.Import
open Fable.Helpers.React.Props
open Fable.Core
module R = Fable.Helpers.React
module Nav = Fable.Import.ReactMaterialNavbar
module Icon = Fable.Import.ReactIcons
module Sidebar = Fable.Import.ReactSidebar
module Dialog = Fable.Import.ReactWinDialog
module NavStore = Stores.Nav
module AuthStore = Stores.Auth

// Import CSS bundle
importSideEffects "./public/css/all.styl"

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
            link Paths.home "Open Orders"
            link "#" "Closed Orders"
            link "#" "Tracking Widget"
            link "#" "Automation Rules"
            link "#" "Help & Support"
            linebreak
            link "#" "Account Settings"
            link "#" "My Stages"
            linebreak
            match Mobx.get AuthStore.isAuthenticated with
            | true -> Paths.Auth.logout, "Sign out"
            | false -> Paths.Auth.login, "Sign in"
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

let dialog () =
    let props = [
        Dialog.Title "Henlo World"
        Dialog.PrimaryText "This doesn't do anything"
        Dialog.SecondaryText "Close"
        Dialog.Open <| Mobx.get NavStore.dialogIsOpen
        Dialog.OnSecondaryClick (ignore >> NavStore.closeDialog)
    ]
    let children = [
        R.div [] [
            R.h1 [] [R.str "Hello world, you're inside the dialog"]
        ]
    ]
    Dialog.dialog props children

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

let DashboardPage dict =
    // User can only reach this page if logged in
    let session = Mobx.get Stores.Auth.session |> Option.get

    R.div [] [
        dialog ()
        R.h1 [] [
            R.str <| sprintf "Hello. You're currently logged in. Your shop is %s at %s." session.shopName session.myShopifyUrl
        ]
        R.str "You're on the dashboard page."
        R.button [Type "button"; OnClick (ignore >> NavStore.openDialog)] [
            R.str "Click to open the dialog"
        ]
    ]

let appRoutes: Router.Route list =
    let requireAuth _ =
        match Mobx.get Stores.Auth.isAuthenticated with
        | true -> None
        | false -> Some Paths.Auth.login
    let logout _ =
        Stores.Auth.logOut()
        JsCookie.remove Constants.CookieName
        Some Paths.Auth.login

    [
        Router.groupWithGuard withNav requireAuth [
            Router.route Paths.home DashboardPage
        ]
        Router.group withoutNav [
            Router.route Paths.Auth.login <| Pages.Auth.LoginOrRegister.Page Pages.Auth.LoginOrRegister.Login
            Router.route Paths.Auth.register <| Pages.Auth.LoginOrRegister.Page Pages.Auth.LoginOrRegister.Register
            Router.route Paths.Auth.completeOAuth <| Pages.Auth.CompleteOauth.Page
        ]
        Router.routeWithGuard Paths.Auth.logout logout (fun _ -> R.noscript [] [])
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
                        Router.link Paths.home [Id "rescue-button"; ClassName "btn blue"] [R.str "Go to Dashboard"]
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