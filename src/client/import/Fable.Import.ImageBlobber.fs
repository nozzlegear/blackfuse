module Fable.Import.ImageBlobber

open Fable.Core
open Fable.Core.JsInterop
open Fable.Import.JS
open Fable.Import.Browser

[<Pojo>]
type Dimensions =
    { height: int
      widthh: int }

[<Pojo>]
type BlobDetails =
    { filename: string
      base64: string
      dimensions: Dimensions }

[<Pojo>]
type ScaleResult =
    { scaledBase64: string
      scaledDimensions: Dimensions }

[<Pojo>]
type ScaleOptions =
    { height: int
      width: int
      preserveRatio: bool }

type IImageBlobber =
    abstract member Supported: bool
    abstract member getFileBlobs: fileInput: Element -> Promise<File list>
    abstract member getBase64: file: File -> Promise<BlobDetails>
    abstract member scaleBase64: base64: string -> options: ScaleOptions -> Promise<ScaleResult>

let private imported: IImageBlobber = import "*" "image-blobber"

let supported = imported.Supported

let getFileBlobs = imported.getFileBlobs

let getBase64 = imported.getBase64

let scaleBase64 = imported.scaleBase64