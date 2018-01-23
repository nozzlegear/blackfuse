module Errors
open Domain
open Status


type HttpException (message, status) =
    inherit System.Exception(message)
    member x.status: Status.Code = status
    member x.toErrorResponse () =
        let statusCode, statusDescription = toInt status, getDescription status

        { statusCode = statusCode
          statusDescription = statusDescription
          message = message }

let fromValidation (errorMap: Map<string, string list>) =
    let message = Validation.getMessage errorMap

    HttpException (message, UnprocessableEntity)

let badData msg = HttpException(msg, Status.UnprocessableEntity)

let forbidden msg = HttpException(msg, Status.Forbidden)

let notFound msg = HttpException (msg, Status.NotFound)

let serverError msg = HttpException(msg, Status.InternalServerError)