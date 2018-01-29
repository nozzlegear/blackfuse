module Domain

open System
open Validation

type ErrorResponse =
    { statusCode: int
      statusDescription: string
      message: string }

type User = {
  /// A unique identifier used by CouchDB to lookup this record.
  id: string
  /// A unique identifier used by CouchDB to version this record.
  rev: string
  /// The date the user was created, in unix seconds (not JS milliseconds).
  created: int64
  shopId: int64
  /// Will be None if the user is unsubscribed.
  shopifyAccessToken: string option
  myShopifyUrl: string option
  shopName: string option
  shopifyChargeId: int64 option
}

/// A pared-down User object, containing only the data needed by the client. Should NEVER contain sensitive data like the user's password.
type SessionToken =
  { id: string
    rev: string
    /// The date the user was created, in unix seconds (not JS milliseconds).
    created: int64
    shopId: int64
    myShopifyUrl: string option
    shopName: string option
    shopifyChargeId: int64 option
    /// The date the SessionToken expires, in unix seconds (not JS milliseconds).
    exp: int64 }
  with
  static member FromUser exp (user: User) =
    { id = user.id
      rev = user.rev
      myShopifyUrl = user.myShopifyUrl
      shopId = user.shopId
      shopName = user.shopName
      shopifyChargeId = user.shopifyChargeId
      created = user.created
      exp = exp }

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
    customer: Customer
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
        orders: Order list }