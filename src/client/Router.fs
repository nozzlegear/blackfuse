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

let router (routes: Route list) (notFound: RouteResult) =
    // TODO: Maybe add an interceptor on the location observable that will run the onBeforeEnter functions of each route?
    // That way the router function will never fire if the interceptor changes the location first?
    // Or maybe that won't work, because we would need to change both the interceptor value and the browser value at the same time.

    fun _ ->
        let loc = mobx.get location
        let matchedRoutes =
            routes
            |> Seq.map (tryMatchRoute loc.pathname)
            |> Seq.filter Option.isSome
            |> Seq.map Option.get

        match Seq.tryHead matchedRoutes with
        | Some (dict, onBeforeEnter, routeResult) ->
            // if the beforeEnter function returns Some we need some way of short-circuting this route and replacing it with the given one
            match onBeforeEnter loc with
            | None -> routeResult dict
            | Some newLoc ->
                R.noscript [P.Ref (fun _ -> replace newLoc |> ignore)] []
        | None -> notFound createEmpty
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