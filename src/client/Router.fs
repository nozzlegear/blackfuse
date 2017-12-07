module Router

open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
module R = Fable.Helpers.React
module P = R.Props
module mobx = Fable.Import.Mobx

type RouteResult = obj -> React.ReactElement

type OnBeforeEnter = Browser.Location -> string option

// Group (fun child -> R.div [] [child]) [
//   Route ("/", fun _ -> R.div [] [R.str "home page"])
//   Route ("/test", fun _ -> R.div [] [R.str "/test page"])
// ]
type Route =
    | Route of string * OnBeforeEnter option * RouteResult
    | Group of (React.ReactElement -> React.ReactElement) * OnBeforeEnter option * Route list

let location = mobx.boxedObservable Browser.window.location

let private changeLocation value _ = mobx.runInAction(fun _ -> mobx.set location value)

let private history = History.createBrowserHistory()

let private unlisten = history.listen changeLocation

let push = history.push

let replace = history.replace

let forward = history.goForward

let back = history.goBack

let go = history.go

let canGo = history.canGo

let rec tryMatchRoute currentPath (route: Route): (obj * OnBeforeEnter * (obj -> React.ReactElement)) option =
    match route with
    | Group (getContainerElement, onBeforeEnterGroup', childRoutes) ->
        let matchedRoutes =
            childRoutes
            |> Seq.map (tryMatchRoute currentPath)
            |> Seq.filter Option.isSome
            |> Seq.map Option.get

        match Seq.tryHead matchedRoutes with
        | Some (dict, onBeforeEnterChild, getChildElement) ->
            let onBeforeEnterGroup = onBeforeEnterGroup' |> Option.defaultValue (fun _ -> None)

            //Combine the two onBeforeEnter functions
            let onBeforeEnter: OnBeforeEnter = fun loc ->
                match onBeforeEnterGroup loc with
                | Some newLoc -> Some newLoc
                | None ->
                    match onBeforeEnterChild loc with
                    | Some newLoc -> Some newLoc
                    | None -> None

            Some (createEmpty, onBeforeEnter, (fun _ -> getChildElement dict |> getContainerElement))
        | None -> None
    | Route (routePath, onBeforeEnter', getReactElement) ->
        let routeMatcher = RouteMatcher.create routePath

        match routeMatcher.parse currentPath with
        | Some dict ->
            let onBeforeEnter: OnBeforeEnter = onBeforeEnter' |> Option.defaultValue (fun _ -> None)
            Some (dict, onBeforeEnter, getReactElement)
        | None -> None

type MatchedRoute = obj * (obj -> React.ReactElement)

let router (routes: Route list) (notFound: RouteResult) =
    let matchedRoute = Mobx.boxedObservable<MatchedRoute> (createEmpty, fun _ -> R.noscript [] [])
    let getChildMatch (loc: Browser.Location): MatchedRoute =
        routes
        |> Seq.map (tryMatchRoute loc.pathname)
        |> Seq.filter Option.isSome
        |> Seq.map Option.get
        |> Seq.tryHead
        |> function
            | Some (dict, onBeforeEnter, routeResult) ->
                match onBeforeEnter loc with
                | None -> dict, routeResult
                | Some newLoc ->
                    // if the beforeEnter function returns Some we need to short-circuit this route and replace it with the given one
                    createEmpty, fun _ -> R.noscript [P.Ref (fun _ -> replace newLoc |> ignore)] []
            | None -> createEmpty, notFound

    // Match the current location, then let the observable handle changes
    Mobx.get location |> getChildMatch |> Mobx.set matchedRoute
    Mobx.observe location (fun loc -> getChildMatch loc.newValue |> Mobx.set matchedRoute)

    fun _ ->
        let routeDict, getRouteChild = Mobx.get matchedRoute

        getRouteChild routeDict
    |> MobxReact.Observer

let route path handler = Route (path, None, handler)

let routeWithGuard path guard handler = Route (path, Some guard, handler)

let group handler routes = Group (handler, None, routes)

let groupWithGuard handler guard routes = Group (handler, Some guard, routes)

let link href (props: P.IHTMLProp list) =
    let onClick (e: React.MouseEvent) =
        e.preventDefault()
        history.push href

    R.a (props@[P.Href href; P.OnClick onClick])