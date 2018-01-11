module Database

open Npgsql

let connString = ""

type ListOrder =
    | Descending of string
    | Ascending of string

let createCommand connection sqlText (cmdParams: Map<string, _> option) =
    let cmd = new NpgsqlCommand(sqlText, connection)

    match cmdParams with
    | Some cmdParams ->
        cmdParams
        |> Map.iter (fun key value -> cmd.Parameters.AddWithValue(key, value) |> ignore)
    | None -> ()

    cmd

let executeInsertOrUpdate (cmd: NpgsqlCommand) =
    cmd.ExecuteNonQueryAsync()
    |> Async.AwaitTask

let executeQuery (cmd: NpgsqlCommand) =
    cmd.ExecuteReaderAsync()
    |> Async.AwaitTask

let mapFieldNamesToColumns (reader: System.Data.Common.DbDataReader) =
    let keysAndColumns: (string * obj) seq = seq {
        for i = 0 to reader.FieldCount - 1 do
            let name = reader.GetName i
            let value = reader.GetValue i

            yield (name, value)
    }

    Map.ofSeq keysAndColumns

let getColumn<'T> key (map: Map<string, obj>) =
    let value = Map.find key map

    value :?> 'T

let userPropsFromMap (m: Map<string, obj>): Domain.User =
    { id = getColumn<string> "id" m
      email = getColumn<string> "email" m
      created = getColumn<int64> "created" m
      hashedPassword = getColumn<string> "hashedPassword" m
      shopifyAccessToken = getColumn<string> "shopifyAccessToken" m
      myShopifyUrl = getColumn<string> "myShopifyUrl" m
      shopId = getColumn<int64> "shopId" m
      shopName = getColumn<string> "shopName" m }

let userPropsToMap (user: Domain.User): Map<string, obj> =
    Map.empty<string, obj>
    |> Map.add "email" (user.email :> obj)
    |> Map.add "created" (user.created :> obj)
    |> Map.add "hashedPassword" (user.hashedPassword :> obj)
    |> Map.add "shopifyAccessToken" (user.shopifyAccessToken :> obj)
    |> Map.add "myShopifyUrl" (user.myShopifyUrl :> obj)
    |> Map.add "shopId" (user.shopId :> obj)
    |> Map.add "shopName" (user.shopName :> obj)

let withoutIdProp = Map.filter (fun key _ -> key <> "id")

let getUserById (id: string): Async<Domain.User option> = async {
    use conn = new NpgsqlConnection(connString)

    let sql = "SELECT * FROM Users WHERE id = @id"

    use! reader =
        Map.ofList ["id", id]
        |> Some
        |> createCommand conn sql
        |> executeQuery

    let! readable =
        reader.ReadAsync()
        |> Async.AwaitTask;

    return
        if not reader.HasRows || not readable then
            None
        else
            mapFieldNamesToColumns reader
            |> userPropsFromMap
            |> Some
}

let getUserByShopId (id: int64): Async<Domain.User option> = async {
    use conn = new NpgsqlConnection(connString)
    let sql = "SELECT * From Users WHERE shopId = @shopId"

    use! reader =
        Map.ofList ["shopId", id]
        |> Some
        |> createCommand conn sql
        |> executeQuery

    let! readable =
        reader.ReadAsync()
        |> Async.AwaitTask;

    return
        if not reader.HasRows || not readable then
            None
        else
            mapFieldNamesToColumns reader
            |> userPropsFromMap
            |> Some
}

let listUsers (order: ListOrder option) (limit: int option): Async<Domain.User seq> = async {
    use conn = new NpgsqlConnection(connString)

    let limitSql =
        match limit with
        | Some _ -> "LIMIT @limit"
        | None -> ""

    let orderBySql =
        match order with
        | Some (Ascending s) -> sprintf "%s ASC" s
        | Some (Descending s) -> sprintf "%s DESC" s
        | None -> "created DESC"
        |> sprintf "ORDER BY %s"

    let sql = sprintf "SELECT * FROM Users %s %s" limitSql orderBySql

    use! reader =
        match limit with
        | Some l -> ["limit", l] |> Map.ofList |> Some
        | None -> None
        |> createCommand conn sql
        |> executeQuery

    let users = seq {
        while reader.Read() do
            let user =
                mapFieldNamesToColumns reader
                |> userPropsFromMap

            yield user
    }

    return users
}

let createUser (user: Domain.User) =
    use conn = new NpgsqlConnection(connString)
    let userMap =
        userPropsToMap user
        |> withoutIdProp
    let fields =
        userMap
        |> Seq.map (fun kvp -> kvp.Key)
        |> String.concat ", "
    let values =
        userMap
        |> Seq.map (fun kvp -> sprintf "@%s" kvp.Key)
        |> String.concat ", "

    let text = sprintf "INSERT INTO Users FIELDS (%s) VALUES (%s)" fields values

    Some userMap
    |> createCommand conn text
    |> executeInsertOrUpdate

let updateUser (id: string) (user: Domain.User) =
    use conn = new NpgsqlConnection(connString)
    let userMap = userPropsToMap user
    let columnSql =
        userMap
        |> withoutIdProp
        |> Seq.map (fun kvp -> sprintf "%s = @%s" kvp.Key kvp.Key)
        |> String.concat ", "
        |> sprintf "UPDATE Users SET %s WHERE id = @id"

    userMap
    |> Map.add "id" (id :> obj) // Use the id passed to the function to ensure we get the intended one.
    |> Some
    |> createCommand conn columnSql
    |> executeInsertOrUpdate