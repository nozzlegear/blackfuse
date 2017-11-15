module Domain

open Status

type ErrorResponse =
    { statusCode: int
      statusDescription: string
      message: string }
    with
    static member FromStatus message status =
        let statusCode, statusDescription = toInt status, getDescription status

        { statusCode = statusCode
          statusDescription = statusDescription
          message = message }

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