module Domain

open Fable.Validation.Core

type ErrorResponse =
    { statusCode: int
      statusDescription: string
      message: string }

type User =
  { id: string
    email: string
    /// The date the user was created, in unix seconds (not JS milliseconds).
    created: int64
    hashedPassword: string }

/// A pared-down User object, containing only the data needed by the client. Should NEVER contain sensitive data like the user's password.
type SessionToken =
  { id: string
    email: string
    /// The date the user was created, in unix seconds (not JS milliseconds).
    created: int64
    /// The date the SessionToken expires, in unix seconds (not JS milliseconds).
    exp: int64 }
  with
  static member FromUser exp (user: User) =
    { id = user.id
      email = user.email
      created = user.created
      exp = exp }

type SessionTokenResponse =
  { token: string }
  with
  static member FromToken token =
    { token = token }

module Requests =
  module Auth =
    type CreateSession =
      { username: string
        password: string }
      with
      member x.Validate () =
        fast <| fun t ->
          { username =
              t.Test "Username" x.username
              |> t.NotBlank "cannot be empty"
              |> t.End
            password =
              t.Test "Password" x.password
              |> t.NotBlank "cannot be empty"
              |> t.End }
      static member Validate (data: CreateSession) =
        data.Validate()