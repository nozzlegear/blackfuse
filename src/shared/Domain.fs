module Domain

open System
open Validation

type ErrorResponse =
    { statusCode: int
      statusDescription: string
      message: string }

type Subscription = 
  { chargeId: int64 
    planName: string 
    price: decimal }

type User = {
  /// A unique identifier used by CouchDB to lookup this record.
  id: string
  /// A unique identifier used by CouchDB to version this record.
  rev: string
  /// The user's username. Required by CouchDB.
  name: string
  /// The date the user was created.
  created: DateTime
  shopId: int64
  /// Will be None if the user is unsubscribed.
  shopifyAccessToken: string option
  myShopifyUrl: string option
  shopName: string option
  subscription: Subscription option
}

/// A pared-down User object, containing only the data needed by the client. Should NEVER contain sensitive data like the user's password.
type ParedUser = 
  {
    id: string
    rev: string
    name: string
    created: DateTime
    shopId: int64
    myShopifyUrl: string option
    shopName: string option
    subscription: Subscription option
  }
  with 
  static member FromUser (user: User) = 
   { id = user.id
     rev = user.rev
     name = user.name
     myShopifyUrl = user.myShopifyUrl
     shopId = user.shopId
     shopName = user.shopName
     subscription = user.subscription
     created = user.created }

/// Represents a logged in user. 
type Session =
  { id: string
    rev: string
    signature: string
    created: DateTime
    user: ParedUser }
  with 
  /// Converts the record to an encodable version that can be stringified to JSON and hashed with a secret key. 
  member x.ToEncodable() = 
    if x.created = System.DateTime() 
    then raise <| System.Exception "Session.created must not be the default DateTime value."
    else { x with id = ""; rev = ""; signature = "" }

/// Represent's a Shopify order's status.
type OrderStatus =
  | Open
  | Closed of DateTime

/// Represent's a Shopify order's line item.
type LineItem =
  { id: int64
    quantity: int
    name: string }

/// Represents a Shopify customer.
type Customer =
  { id: int64
    firstName: string
    lastName: string }

/// Represents a Shopify order. ShopifySharp can't be used on the frontend, and we wouldn't want to send the entire order object anyway, so this record holds just the order data we need.
type Order =
  { id: int64
    name: string
    lineItems: LineItem list
    customer: Customer option
    dateCreated: DateTime
    status: OrderStatus
    totalPrice: decimal }

type SessionTokenResponse =
  { token: string }
  with
  static member FromToken token =
    { token = token }

module Requests =
  module OAuth =
    type CompleteShopifyOauth  =
      { rawQueryString: string }
      with
      member x.Validate () =
        [
          onProperty "rawQueryString" x.rawQueryString
          |> notBlank None
          |> validate
        ]
        |> toResult x
      static member Validate (data: CompleteShopifyOauth) =
        data.Validate()

    type GetShopifyOauthUrlResult =
      { url: string }
  
  module Orders =
    type ListOrdersResponse = 
      { page: int
        limit: int
        totalPages: int
        totalOrders: int
        orders: Order list }