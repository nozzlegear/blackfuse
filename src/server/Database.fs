module Database

open Npgsql.FSharp
open Domain

let connString = ServerConstants.databaseConnectionString

type ListOrder =
    | Descending of string
    | Ascending of string

let toUserOption: SqlRow -> Domain.User option =
    function
    | [ "id", Sql.Int id
        "email", Sql.String email
        "created", Sql.Long created
        "hashedPassword", Sql.String password
        "shopifyAccessToken", Sql.String token
        "myShopifyUrl", Sql.String myShopifyUrl
        "shopId", Sql.Long shopId
        "shopName", Sql.String shopName ] ->
        { id = id
          email = email
          created = created
          hashedPassword = password
          shopifyAccessToken = token
          myShopifyUrl = myShopifyUrl
          shopId = shopId
          shopName = shopName }
        |> Some
    | _ -> None

let paramsFromUser (user: Domain.User) =
    [
        "id", Sql.Int user.id
        "email", Sql.String user.email
        "created", Sql.Long user.created
        "hashedPassword", Sql.String user.hashedPassword
        "shopifyAccessToken", Sql.String user.shopifyAccessToken
        "myShopifyUrl", Sql.String user.myShopifyUrl
        "shopId", Sql.Long user.shopId
        "shopName", Sql.String user.shopName
    ]

let withoutIdProp = List.filter (fun (key, _) -> key <> "id")

let getUserById (id: int): Async<Domain.User option> = async {
    let! result =
        connString
        |> Sql.connect
        |> Sql.query "SELECT * FROM Users WHERE id = @id"
        |> Sql.parameters ["id", Sql.Int id]
        |> Sql.executeTableAsync

    return
        result
        |> Sql.mapEachRow toUserOption
        |> Seq.tryHead
}

let getUserByShopId (id: int64): Async<Domain.User option> = async {
    let! result =
        connString
        |> Sql.connect
        |> Sql.query "SELECT * FROM Users WHERE shopId = @shopId"
        |> Sql.parameters ["shopId", Sql.Long id]
        |> Sql.executeTableAsync

    return
        result
        |> Sql.mapEachRow toUserOption
        |> Seq.tryHead
}

let listUsers (order: ListOrder option) (limit: int option) = async {
    let limitSql =
        limit
        |> Option.map (fun _ -> "LIMIT @limit")
        |> Option.defaultValue ""

    let orderBySql =
        match order with
        | Some (Ascending s) -> sprintf "%s ASC" s
        | Some (Descending s) -> sprintf "%s DESC" s
        | None -> "created DESC"
        |> sprintf "ORDER BY %s"

    let sql = sprintf "SELECT * FROM Users %s %s" limitSql orderBySql

    let! result =
        connString
        |> Sql.connect
        |> Sql.query sql
        |> fun query -> match limit with | Some l -> Sql.parameters ["limit", Sql.Int l] query | None -> query
        |> Sql.executeTableAsync

    return
        result
        |> Sql.mapEachRow toUserOption
}

let createUser (user: Domain.User) =
    let sqlParams =
        paramsFromUser user
        |> withoutIdProp
    let fields =
        sqlParams
        |> Seq.map (fun (key, _) -> key)
        |> String.concat ", "
    let values =
        sqlParams
        |> Seq.map (fun (key, _) -> sprintf "@%s" key)
        |> String.concat ", "
    let sql = sprintf "INSERT INTO Users FIELDS (%s) VALUES (%s)" fields values

    connString
    |> Sql.connect
    |> Sql.query sql
    |> Sql.parameters sqlParams
    |> Sql.executeNonQuerySafeAsync

let updateUser (id: int) (user: Domain.User) =
    let sqlParams =
        paramsFromUser user
        |> withoutIdProp
    let sql =
        sqlParams
        |> withoutIdProp
        |> Seq.map (fun (key, _) -> sprintf "%s = @%s" key key)
        |> String.concat ", "
        |> sprintf "UPDATE Users SET %s WHERE id = @id"

    connString
    |> Sql.connect
    |> Sql.query sql
    // Use the id passed to the function to ensure we get the intended one
    |> Sql.parameters (sqlParams@["id", Sql.Int id])
    |> Sql.executeNonQuerySafeAsync