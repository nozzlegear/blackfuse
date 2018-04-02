module Database

open Davenport.Fsharp
open Davenport.Types
open Domain
open Suave.Logging

let private asyncTryHead (a: Async<'a seq>) = async {
    let! result = a

    return Seq.tryHead result
}

let private logger = Suave.Logging.Log.create "Davenport"

let private printWarning m = 
    Message.eventX m
    |> logger.log Warn
    |> Async.Start

let private addUsernameAndPassword client =
    match ServerConstants.couchdbUsername, ServerConstants.couchdbPassword with
    | Some u, Some p -> client |> username u |> password p
    | Some u, None -> client |> username u
    | None, Some p -> client |> password p
    | None, None -> client

/// For the users database, the type must always be "user" (case-sensitive). CouchDB will reject any other value.
let [<Literal>] UserType = "user"

let [<Literal>] SessionType = "session"

let private defaultFields = "id", "rev"

let private fieldMapping: FieldMapping = 
    Map.empty 
    |> Map.add UserType defaultFields
    |> Map.add SessionType defaultFields

/// The user database is separate from the app's own database, because it uses CouchDB's built-in users database.
let private userDb =
    ServerConstants.couchdbUrl
    |> database "_users"
    |> addUsernameAndPassword
    // Map the User record's id/rev fields to CouchDB's fields. 
    |> mapFields fieldMapping
    |> warning printWarning

type CouchPerUser = 
    | UserId of string

let private db = function 
    | CouchPerUser.UserId userId ->
        let dbName = sprintf "blackfuse_%s" userId

        ServerConstants.couchdbUrl
        |> database dbName
        |> addUsernameAndPassword
        |> mapFields fieldMapping
        |> warning printWarning

let createDefaultUserDatabase() = 
    userDb 
    |> createDatabase

/// Configures indexes of the user database, creating them if they don't already exist.
let configureIndexes() = 
    // Makes searching for users by their ShopId faster
    let indexes = ["shopId"] 

    userDb 
    |> createIndexes [] indexes

let configureDatabaseForUser = db >> fun db -> async {
    let! createResult = createDatabase db 

    // Create any necessary database indexes here
    do! 
        [
            // Make it faster to look up a doc by its type
            "type"
        ] 
        |> createIndexes []
        <| db
        |> Async.Ignore

    return createResult
}

type DatabaseDoc = 
    | Session of Session
    | User of User

let private insertable d: InsertedDocument<obj> = 
    match d with 
    | DatabaseDoc.Session s -> Some SessionType, s :> obj 
    | DatabaseDoc.User u -> Some UserType, u :> obj

let private toDoc (d: Document) = 
    match d.TypeName with 
    | Some UserType -> 
        d.To<User>() 
        |> DatabaseDoc.User
        |> Some 
    | Some SessionType -> 
        d.To<Session>()
        |> DatabaseDoc.Session
        |> Some 
    | _ ->
        None

let private toUser = function 
    | Some (DatabaseDoc.User d) -> Some d
    | _ -> None

let private toSession = function 
    | Some (DatabaseDoc.Session d) -> Some d 
    | _ -> None

let getUserById id rev =
    userDb
    |> get id rev
    |> Async.Map (toDoc >> toUser)

let getUserByShopId (id: int64) =
    Map.empty 
    |> Map.add "shopId" [FindOperator.EqualTo id]
    |> find [FindOption.FindLimit 1]
    <| userDb 
    |> Async.Map Seq.ofList
    |> Async.TryHead
    |> Async.Map (Option.bind (toDoc >> toUser))

/// Returns a tuple of (totalRows * User seq).
let listUsers limit =
    limit 
    |> Option.map (fun limit -> [ListOption.ListLimit limit])
    |> Option.defaultValue []
    |> List.append [ListOption.IncludeDocs true]
    |> listAll
    <| userDb
    |> Async.Map (fun v -> 
        let rows = 
            v.Rows 
            |> List.map (fun r -> r.Doc |> Option.bind (toDoc >> toUser))

        v.TotalRows, rows)

/// Creates a user, returning a new user record with the Id and Rev labels filled by CouchDB.
let createUser user = async {
    let! result =
        user
        |> DatabaseDoc.User 
        |> insertable 
        |> create
        <| userDb

    assert result.Okay

    return { user with id = result.Id; rev = result.Rev }
}

/// Updates the user with the given id and revision, returning a new user record with the Id and Rev labels updated by CouchDB.
let updateUser id rev user = async {
    let! result = 
        user
        |> DatabaseDoc.User 
        |> insertable 
        |> update id rev 
        <| userDb

    assert result.Okay

    return { user with id = result.Id; rev = result.Rev }
}

let createSession = db >> fun db session -> async {
    let! result = 
        session 
        |> DatabaseDoc.Session 
        |> insertable 
        |> create
        <| db

    assert result.Okay

    return { session with id = result.Id; rev = result.Rev }
}

let updateSession = db >> fun db id rev session -> async {
    let! result = 
        session 
        |> DatabaseDoc.Session 
        |> insertable 
        |> update id rev
        <| db 

    assert result.Okay

    return { session with id = result.Id; rev = result.Rev }
}

let getSession = db >> fun db id rev -> 
    db 
    |> get id rev 
    |> Async.Map (toDoc >> toSession) 

let deleteSession = db >> fun db id rev -> 
    db
    |> delete id rev

let deleteSessionsForUser = db >> fun db -> async {
    let! sessions = 
        Map.empty 
        |> Map.add "type" [FindOperator.EqualTo SessionType]
        |> find []
        <| db
        |> Async.MapSeq (toDoc >> toSession)

    do! 
        sessions
        |> Seq.filter Option.isSome
        |> Seq.map (Option.get >> fun s -> delete s.id s.rev db)
        |> Async.Parallel
        |> Async.Ignore
}