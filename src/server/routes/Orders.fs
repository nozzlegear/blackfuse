module Routes.Orders 

open Suave 
open Filters 
open Suave.Filters
open Suave.Operators
open ShopifySharp
open Domain.Requests.Orders
open ShopifySharp.Filters
open Davenport.Fsharp

let listOrders = withUser <| fun user req ctx -> async {
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
                      name = li.Name }
                )

            let customer: Domain.Customer = 
                { id = order.Customer.Id.Value 
                  firstName = order.Customer.FirstName
                  lastName = order.Customer.LastName }

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

    return!
        { limit = limit; page = page; orders = List.ofSeq orders }
        |> Json.toJson
        |> Successful.ok
        >=> Writers.setMimeType Json.MimeType
        <| ctx
}

let routes = [
    GET >=> path Paths.Api.Orders.list >=> listOrders
]