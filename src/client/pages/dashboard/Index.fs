module Pages.Dashboard.Index

open Fable
open Fable.Core
open Fable.Import
open Fable.PowerPack
module R = Fable.Helpers.React
module P = R.Props
module C = Components
module S = Stores.Dashboard

let loadPage bypassLoadingCheck page =
    if Mobx.get S.loading && not bypassLoadingCheck
    then ()
    else 
    promise {
        let! result = Services.Orders.listOrders page 100

        match result with 
        | Ok r -> 
            S.receivedOrders r
        | Error e ->
            Fable.Import.Browser.console.error e 
            S.receivedError e.message
    }
    |> Promise.start

let summarizeLineItems (li: Domain.LineItem list) = 
    match Seq.length li with 
    | 0 -> "(No line items in this order.)"
    | 1 -> 
        let item = Seq.head li
        sprintf "%s x%i" item.name item.quantity
    | 2 -> 
        let item = Seq.head li 
        sprintf "%s x%i and 1 other item." item.name item.quantity
    | count -> 
        let item = Seq.head li 
        sprintf "%s x%i and %i other items." item.name item.quantity (count - 1)

let formatCustomerName (customer: Domain.Customer option) = 
    match customer with 
    | Some cust -> sprintf "%s %s" cust.firstName cust.lastName 
    | None -> "(No customer for this order. Was it created by API?)"

let selectPage pageStr = 
    try int pageStr
    with _ -> 1
    |> Paths.Client.homeWithPage
    |> Router.push

let Page (page: int) =
    let page = if page < 1 then 1 else page
    
    let loadAfterMount() = 
        R.div [P.ClassName "loading"] [
            R.h1 [] [R.str <| sprintf "Loading Page %i of Shopify orders, please wait." page]
            R.progress [] []
            C.AfterMount "load-after-mount" (fun _ -> 
                loadPage true page
            )
        ]

    fun _ ->
        let Error = sprintf "Error getting orders: %s" >> C.ErrorCentered

        let body = 
            match Mobx.get S.error, Mobx.get S.orders with 
            | Some e, None -> Error e
            | None, None -> loadAfterMount()
            | _, Some o when o.page <> page -> loadAfterMount()
            | _, Some o -> 
                let pageSelector = 
                    R.div [P.ClassName "form-control"] [
                        [1..o.totalPages]
                        |> List.map (fun p -> 
                            R.option [P.Value <| string p; P.Key <| string p] [
                                R.str <| sprintf "Page %i of %i" p o.totalPages
                            ]
                        )
                        |> R.select [P.Value <| string o.page; P.OnChange (Utils.getValueFromEvent >> selectPage)]
                    ]

                R.div [] [
                    C.PageHeader (sprintf "%i Orders" o.totalOrders) (Some pageSelector)

                    Mobx.get S.error 
                    |> Option.map Error
                    |> R.opt

                    R.table [P.ClassName "pure-table pure-table-horizontal pure-table-striped"] [
                        R.thead [] [
                            R.tr [] [
                                R.th [] [R.str "Order ID"]
                                R.th [] [R.str "Customer Name"]
                                R.th [] [R.str "Line Item Summary"]
                                R.th [] [R.str "Order Status"]
                            ]
                        ]

                        o.orders
                        |> List.map (fun order -> 
                            R.tr [P.Key <| string order.id] [
                                R.td [] [R.str order.name]
                                R.td [] [R.str <| formatCustomerName order.customer]
                                R.td [] [R.str <| summarizeLineItems order.lineItems]
                                R.td [] [R.str <| order.status.ToString()]
                            ]
                        )
                        |> R.tbody []
                    ]
                    R.hr []
                    Pure.Grid "" [] [
                        Pure.Unit "" [Pure.Medium 16; Pure.Base 24] [] []
                        Pure.Unit "" [Pure.Medium 8; Pure.Base 24] [] [pageSelector]
                    ]
                ]

        R.div [] [ 
            body
        ]
    |> MobxReact.Observer

let PageOne () = Page 1