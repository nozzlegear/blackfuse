module Router

open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
module R = Fable.Helpers.React
module P = R.Props
module mobx = Fable.Import.Mobx

type GroupContainerRenderer = React.ReactElement -> React.ReactElement

type RouteRenderer = unit -> React.ReactElement

type RouteMatcher = string -> RouteRenderer option

type GuardFunc = Browser.Location -> string option

// Group (fun child -> R.div [] [child]) [
//   Route (fun path -> if path = "/" then Some <| fun _ -> R.div [] [R.str "home page"] else None)
//   Route (fun path -> if path = "/test" then Some <| fun _ -> R.div [] [R.str "/test page"] else None)
// ]
type Route =
    | Route of RouteMatcher * GuardFunc option
    | Group of GroupContainerRenderer * GuardFunc option * Route list

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

let rec tryMatchRoute currentPath (route: Route): (RouteRenderer * GuardFunc) option =
    match route with
    | Group (renderGroupContainer, groupGuard, childRoutes) ->
        let matchedRoutes =
            childRoutes
            |> Seq.map (tryMatchRoute currentPath)
            |> Seq.filter Option.isSome
            |> Seq.map Option.get

        match Seq.tryHead matchedRoutes with
        | Some (render, onBeforeEnterChild) ->
            let onBeforeEnterGroup = Option.defaultValue (fun _ -> None) groupGuard

            //Combine the two guard functions
            let onBeforeEnter: GuardFunc = fun loc ->
                match onBeforeEnterGroup loc with
                | Some newLoc -> Some newLoc
                | None ->
                    match onBeforeEnterChild loc with
                    | Some newLoc -> Some newLoc
                    | None -> None

            Some (render >> renderGroupContainer, onBeforeEnter)
        | None -> None
    | Route (routeMatcher, guard) ->
        routeMatcher currentPath
        |> Option.map (fun renderer -> 
            let onBeforeEnter: GuardFunc = Option.defaultValue (fun _ -> None) guard

            renderer, onBeforeEnter
        )

let router (routes: Route list) (notFound: RouteRenderer) =
    let matchedRoute = Mobx.boxedObservable<RouteRenderer> (fun _ -> R.noscript [] [])
    let getChildMatch (loc: Browser.Location): RouteRenderer =
        routes
        |> Seq.map (tryMatchRoute loc.pathname)
        |> Seq.filter Option.isSome
        |> Seq.map Option.get
        |> Seq.tryHead
        |> Option.map (fun (render, guard) -> 
            guard loc 
            // if the guard function returns Some we need to short-circuit this route and replace it with the given one
            |> Option.map (fun newLoc -> (fun _ -> R.noscript [P.Ref (fun _ -> replace newLoc)] []))
            |> Option.defaultValue render 
        )
        |> Option.defaultValue notFound
    
    // Match the current location, then let the observable handle changes
    Mobx.get location |> getChildMatch |> Mobx.set matchedRoute
    Mobx.observe location (fun loc -> getChildMatch loc.newValue |> Mobx.set matchedRoute)

    fun _ ->
        let render = Mobx.get matchedRoute
        render()
    |> MobxReact.Observer

let trimTrailingSlash (s: string) = 
    if s.EndsWith "/" 
    then s.TrimEnd (char "/") 
    else s

let private stringMatch routePath handler currentPath = 
    match System.String.Equals(trimTrailingSlash routePath, trimTrailingSlash currentPath, System.StringComparison.OrdinalIgnoreCase) with 
    | true -> Some handler 
    | false -> None

[<PassGenerics>]
let private scanMatch routePath handler currentPath =
    let makeRenderer i = fun () -> handler i

    // TODO: what happens when the pathscan ends with a slash but the currentpath does not?
    PathScan.scan routePath currentPath 
    // Try again, this time trimming the trailing slash 
    |> Option.orElseWith (fun _ -> PathScan.scan routePath (trimTrailingSlash currentPath))
    |> Option.map makeRenderer

let route routePath handler = 
    Route(stringMatch routePath handler, None)

[<PassGenerics>]
let routeScan routePath handler = 
    Route(scanMatch routePath handler, None)

let routeWithGuard routePath guard handler = 
    Route (stringMatch routePath handler, Some guard)

[<PassGenerics>]
let routeScanWithGuard routePath guard handler = 
    Route(scanMatch routePath handler, Some guard)

let group handler routes = Group (handler, None, routes)

let groupWithGuard handler guard routes = Group (handler, Some guard, routes)

let link href (props: P.IHTMLProp list) =
    let onClick (e: React.MouseEvent) =
        e.preventDefault()
        history.push href

    R.a (props@[P.Href href; P.OnClick onClick])