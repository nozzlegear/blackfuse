module Routes.Orders 

open Suave 
open Filters 
open Suave.Operators
open ShopifySharp
open Domain.Requests.Orders
open ShopifySharp.Filters
open Davenport.Fsharp

let listOrders = withUserAndSession <| fun user _ req ctx -> async {
    let limit = 
        match req.queryParam "limit" with 
        | Choice1Of2 header -> 
            try int header with | _ -> 1
        | Choice2Of2 _ -> 1

    let page = 
        match req.queryParam "page" with 
        | Choice1Of2 header ->
            try int header with | _ -> 1
        | Choice2Of2 _ -> 1

    let url, token = 
        match user.myShopifyUrl, user.shopifyAccessToken with 
        | Some url, Some token -> url, token
        | _, _ -> raise <| Errors.forbidden "User does not have a valid MyShopifyUrl or ShopifyAccessToken. Please connect your Shopify account."

    let service = OrderService(url, token)
    let filter = OrderFilter()
    filter.Limit <- System.Nullable limit 
    filter.Page <- System.Nullable page
    filter.Status <- "all"

    let! totalOrders = 
        service.CountAsync filter 
        |> Async.AwaitTask 

    let! orders = 
        service.ListAsync filter 
        |> Async.AwaitTask
        |> Wrapper.asyncMapSeq (fun order ->
            let lineItems: Domain.LineItem list = 
                order.LineItems
                |> List.ofSeq
                |> List.map (fun li -> 
                    { id = li.Id.Value
                      quantity = if li.Quantity.HasValue then li.Quantity.Value else 0 
                      name = li.Name })

            let customer: Domain.Customer option = 
                Option.ofNullable order.Customer 
                |> Option.map (fun cust ->
                    { id = cust.Id.Value 
                      firstName = cust.FirstName
                      lastName = cust.LastName })

            let output: Domain.Order = 
                { id = order.Id.Value
                  dateCreated = order.CreatedAt.Value.DateTime
                  name = order.Name 
                  status = if order.ClosedAt.HasValue then Domain.Closed order.ClosedAt.Value.DateTime else Domain.Open 
                  totalPrice = if order.TotalPrice.HasValue then order.TotalPrice.Value else 0M
                  lineItems = lineItems
                  customer = customer }

            output
        )

    let totalPages = if totalOrders % limit > 0 then (totalOrders / limit) + 1 else totalOrders / limit

    return!
        { limit = limit 
          page = page 
          orders = List.ofSeq orders 
          totalOrders = totalOrders
          totalPages = totalPages }
        |> Writers.writeJson 200
        <| ctx
}

let routes = [
    GET >=> choose [
        Paths.Api.Orders.list >@-> listOrders
    ]
]