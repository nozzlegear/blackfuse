module Database

open Davenport.Fsharp.Wrapper
open Domain

let private asyncTryHead (a: Async<'a seq>) = async {
    let! result = a

    return Seq.tryHead result
}

let private printWarning = printfn "%s"

let private addUsernameAndPassword client =
    match ServerConstants.couchdbUsername, ServerConstants.couchdbPassword with
    | Some u, Some p -> client |> username u |> password p
    | Some u, None -> client |> username u
    | None, Some p -> client |> password p
    | None, None -> client

let private userDb =
    ServerConstants.couchdbUrl
    |> database "blackfuse_users"
    |> addUsernameAndPassword
    |> idField "id" // Map the User record's id label to CouchDB's _id field
    |> revField "rev" // Map the User record's rev label to CouchDB's _rev field
    |> warning (Event.add printWarning)

let private sessionDb = 
    ServerConstants.couchdbUrl
    |> database "blackfuse_sessions"
    |> addUsernameAndPassword
    |> idField "id"
    |> revField "rev"
    |> warning (Event.add printWarning)    

/// Configures all couchdb databases used by this app by creating them (if they don't exist), creating indexes and creating/updating design docs.
let configureDatabases = async {
    let userDbIndexes = ["shopId"] // Makes searching for users by their ShopId faster
    let userDbDesignDocs = []

    // Not using Async.Ignore to make sure any errors thrown by database configuration bubble up to the app.
    let! _ =
        Async.Parallel [
            userDb |> configureDatabase userDbDesignDocs userDbIndexes
            sessionDb |> configureDatabase [] []
        ]

    ()
}

let getUserById id rev =
    userDb
    |> get<User> id rev

let getUserByShopId (id: int64) =
    let options = Davenport.Entities.FindOptions()
    options.Limit <- 1 |> System.Nullable

    userDb
    |> findByExpr <@ fun (u: User) -> u.shopId = id @> (Some options)
    |> asyncTryHead

/// Returns a tuple of (totalRows * User seq).
let listUsers limit =
    let options =
        match limit with
        | None -> None
        | Some l ->
            let o = Davenport.Entities.ListOptions()
            o.Limit <- Option.toNullable l
            Some o

    userDb
    |> listWithDocs<User> options
    |> asyncMap (fun d -> d.TotalRows, d.Rows |> Seq.map (fun r -> r.Doc))

/// Creates a user, returning a new user record with the Id and Rev labels filled by CouchDB.
let createUser user = async {
    let! result = userDb |> create<User> user

    assert result.Ok

    return { user with id = result.Id; rev = result.Rev }
}

/// Updates the user with the given id and revision, returning a new user record with the Id and Rev labels updated by CouchDB.
let updateUser id rev user = async {
    let! result = userDb |> update<User> id rev user

    assert result.Ok

    return { user with id = result.Id; rev = result.Rev }
}

let createSession session = async {
    let! result = sessionDb |> create<Session> session 

    assert result.Ok

    return { session with id = result.Id; rev = result.Rev }
}

let getSession id rev = sessionDb |> get<Session> id rev

let deleteSession id rev = sessionDb |> delete id rev