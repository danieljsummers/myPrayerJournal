﻿/// Main server module for myPrayerJournal
module MyPrayerJournal.App

open Auth0.AuthenticationApi
open Auth0.AuthenticationApi.Models
open Microsoft.EntityFrameworkCore
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open Reader
open System
open System.IO
open Suave
open Suave.Filters
open Suave.Operators
open Suave.Redirection
open Suave.RequestErrors
open Suave.State.CookieStateStore
open Suave.Successful

let utf8 = System.Text.Encoding.UTF8

type JsonNetCookieSerializer () =
  interface CookieSerialiser with
    member x.serialise m =
      utf8.GetBytes (JsonConvert.SerializeObject m)
    member x.deserialise m =
      JsonConvert.DeserializeObject<Map<string, obj>> (utf8.GetString m)

type Auth0Config = {
  Domain : string
  ClientId : string
  ClientSecret : string
}
with
  static member empty =
    { Domain = ""
      ClientId = ""
      ClientSecret = ""
      }

type Config = {
  Conn : string
  Auth0 : Auth0Config
}
with
  static member empty =
    { Conn = ""
      Auth0 = Auth0Config.empty
      }

let cfg =
  try
    use sr = File.OpenText "appsettings.json"
    let settings = JToken.ReadFrom(new JsonTextReader(sr)) :?> JObject
    { Conn = settings.["conn"].ToObject<string>()
      Auth0 =
        { Domain = settings.["auth0"].["domain"].ToObject<string>()
          ClientId = settings.["auth0"].["client-id"].ToObject<string>()
          ClientSecret = settings.["auth0"].["client-secret"].ToObject<string>()
          }
      }
  with _ -> Config.empty

/// Data Configuration singleton 
//let lazyCfg = lazy (DataConfig.FromJson <| try File.ReadAllText "data-config.json" with _ -> "{}")
/// RethinkDB connection singleton
//let lazyConn = lazy lazyCfg.Force().CreateConnection ()
/// Application dependencies
//let deps = {
//  new IDependencies with
//    member __.Conn with get () = lazyConn.Force ()
//  }

/// Get the scheme, host, and port of the URL
let schemeHostPort (req : HttpRequest) =
  sprintf "%s://%s" req.url.Scheme (req.headers |> List.filter (fun x -> fst x = "host") |> List.head |> snd)

/// Authorization functions
module Auth =

(*
  let exchangeCodeForToken code = context (fun ctx ->
      async {
        let client = AuthenticationApiClient (Uri (sprintf "https://%s" cfg.Auth0.Domain))
        let! req =
          client.ExchangeCodeForAccessTokenAsync
            (ExchangeCodeRequest
              (AuthorizationCode = code,
              ClientId = cfg.Auth0.ClientId,
              ClientSecret = cfg.Auth0.ClientSecret,
              RedirectUri = sprintf "%s/user/log-on" (schemeHostPort ctx.request)))
        let! user = client.GetUserInfoAsync ((req : AccessToken).AccessToken)
        return
          ctx
          |> HttpContext.state
          |> function
          | None -> FORBIDDEN "Cannot sign in without state"
          | Some state ->
              state.set "auth-token" req.IdToken
              >=> Writers.setUserData "user" user
        }
      |> Async.RunSynchronously
    )

  /// Handle the sign-in callback from Auth0
  let handleSignIn =
    context (fun ctx ->
      GET
      >=> match ctx.request.queryParam "code" with
          | Choice1Of2 authCode ->
              exchangeCodeForToken authCode
              >=> FOUND (sprintf "%s/journal" (schemeHostPort ctx.request))
          | Choice2Of2 msg -> BAD_REQUEST msg
    )
  
  /// Handle signing out a user
  let handleSignOut =
    context (fun ctx ->
      match ctx |> HttpContext.state with
      | Some state -> state.set "auth-key" null
      | _ -> succeed
      >=> FOUND (sprintf "%s/" (schemeHostPort ctx.request))) *)

  let cw (x : string) = Console.WriteLine x

  /// Convert microtime to ticks, add difference from 1/1/1 to 1/1/1970
  let jsDate jsTicks =
    DateTime(jsTicks * 10000000L).AddTicks(DateTime(1970, 1, 1).Ticks)
  
  let getIdFromToken token =
    match token with
    | Some jwt ->
        try
          let key = Convert.FromBase64String(cfg.Auth0.ClientSecret.Replace("-", "+").Replace("_", "/"))
          let payload = Jose.JWT.Decode<JObject>(jwt, key)
          let tokenExpires = jsDate (payload.["exp"].ToObject<int64>())
          match tokenExpires > DateTime.UtcNow with
          | true -> Some (payload.["sub"].ToObject<string>())
          | _ -> None
        with ex ->
          sprintf "Token Deserialization Exception - %s" (ex.GetType().FullName) |> cw
          sprintf "Message - %s" ex.Message |> cw
          ex.StackTrace |> cw
          None
    | _ -> None
  
  /// Add the logged on user Id to the context if it exists
  let loggedOn = warbler (fun ctx ->
      match HttpContext.state ctx with
      | Some state -> Writers.setUserData "user" (state.get "auth-token" |> getIdFromToken)
      | _ -> Writers.setUserData "user" None)

  /// Create a user context for the currently assigned user
  //let userCtx ctx = { Id = ctx.userState.["user"] :?> string option }

/// Read an item from the user state, downcast to the expected type
let read ctx key : 'value =
  ctx.userState |> Map.tryFind key |> Option.map (fun x -> x :?> 'value) |> Option.get
    
/// Create a new data context
let dataCtx () =
  new DataContext (((DbContextOptionsBuilder<DataContext>()).UseNpgsql cfg.Conn).Options)

/// Return an HTML page
let html ctx content =
  ""//Views.page (Auth.userCtx ctx) content

/// Home page
let viewHome = warbler (fun ctx -> OK ("" (*Views.home*) |> html ctx))

/// Journal page
let viewJournal =
  context (fun ctx ->
    use dataCtx = dataCtx ()
    let reqs = Data.Requests.allForUser (defaultArg (read ctx "user") "") dataCtx
    OK ("" (*Views.journal reqs*) |> html ctx))

let idx =
  context (fun ctx ->
    Console.WriteLine "serving index"
    succeed)
/// Suave application
let app =
  statefulForSession
  >=> Auth.loggedOn
  >=> choose [
        path Route.home >=> Files.browseFileHome "index.html"
        path Route.journal >=> viewJournal
        //path Route.User.logOn >=> Auth.handleSignIn
        //path Route.User.logOff >=> Auth.handleSignOut
        Writers.setHeader "Cache-Control" "no-cache" >=> Files.browseHome
        NOT_FOUND "Page not found." 
        ]

/// Ensure the EF context is created in the right format
let ensureDatabase () =
  async {
    use data = dataCtx ()
    do! data.Database.MigrateAsync ()
    }
  |> Async.RunSynchronously

let suaveCfg =
  { defaultConfig with
      homeFolder = Some (Path.GetFullPath "./wwwroot/")
      serverKey = Text.Encoding.UTF8.GetBytes("12345678901234567890123456789012")
      cookieSerialiser = JsonNetCookieSerializer ()
    }
open Suave.Utils

[<EntryPoint>]
let main argv = 
  // Establish the data environment
  //liftDep getConn (Data.establishEnvironment >> Async.RunSynchronously)
  //|> run deps
  
  ensureDatabase ()
  startWebServer suaveCfg app
  0 
