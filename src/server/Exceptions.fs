module Exceptions
open Domain

type HttpException (message, status) =
    inherit System.Exception(message)
    member x.status: Status.Code = status
    member x.toErrorResponse () =
        ErrorResponse.FromStatus message status