module Domain

open Fable.Validation.Core

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
  shopifyAccessToken: string
  myShopifyUrl: string
  shopId: int64
  shopName: string
  shopifyChargeId: int64 option
}

/// A pared-down User object, containing only the data needed by the client. Should NEVER contain sensitive data like the user's password.
type SessionToken =
  { id: string
    rev: string
    /// The date the user was created, in unix seconds (not JS milliseconds).
    created: int64
    myShopifyUrl: string
    shopId: int64
    shopName: string
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

type SessionTokenResponse =
  { token: string }
  with
  static member FromToken token =
    { token = token }

module Requests =
  module Auth =
    type CompleteShopifyOauth  =
      { rawQueryString: string }
      with
      member x.Validate () =
        fast <| fun t ->
          { rawQueryString =
              t.Test "rawQueryString" x.rawQueryString
              |> t.NotBlank "cannot be empty"
              |> t.End }
      static member Validate (data: CompleteShopifyOauth) =
        data.Validate()

    type GetShopifyOauthUrlResult =
      { url: string }