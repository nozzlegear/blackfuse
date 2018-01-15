module Domain

open Fable.Validation.Core

type ErrorResponse =
    { statusCode: int
      statusDescription: string
      message: string }

type User =
  { id: int
    email: string
    /// The date the user was created, in unix seconds (not JS milliseconds).
    created: int64
    hashedPassword: string
    shopifyAccessToken: string
    myShopifyUrl: string
    shopId: int64
    shopName: string }

/// A pared-down User object, containing only the data needed by the client. Should NEVER contain sensitive data like the user's password.
type SessionToken =
  { id: string
    email: string
    /// The date the user was created, in unix seconds (not JS milliseconds).
    created: int64
    myShopifyUrl: string
    shopId: int64
    shopName: string
    /// The date the SessionToken expires, in unix seconds (not JS milliseconds).
    exp: int64 }
  with
  static member FromUser exp (user: User) =
    { id = user.id
      email = user.email
      myShopifyUrl = user.myShopifyUrl
      shopId = user.shopId
      shopName = user.shopName
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