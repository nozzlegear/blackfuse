module Errors
open Domain
open Status

let fromStatus message status =
    let statusCode, statusDescription = toInt status, getDescription status

    { statusCode = statusCode
      statusDescription = statusDescription
      message = message }


type HttpException (message, status) =
    inherit System.Exception(message)
    member x.status: Status.Code = status
    member x.toErrorResponse () =
        fromStatus message status

let fromValidation (errorMap: Map<string, string list>) =
    let message = Validation.getMessage errorMap

    HttpException (message, UnprocessableEntity)

let badData msg = HttpException(msg, Status.UnprocessableEntity)