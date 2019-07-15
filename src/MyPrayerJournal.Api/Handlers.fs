﻿/// HTTP handlers for the myPrayerJournal API
[<RequireQualifiedAccess>]
module MyPrayerJournal.Api.Handlers

open FSharp.Control.Tasks.V2.ContextInsensitive
open Giraffe
open MyPrayerJournal
open System

/// Handler to return Vue files
module Vue =
  
  /// The application index page
  let app : HttpHandler = htmlFile "wwwroot/index.html"


/// Handlers for error conditions
module Error =

  open Microsoft.Extensions.Logging

  /// Handle errors
  let error (ex : Exception) (log : ILogger) =
    log.LogError (EventId(), ex, "An unhandled exception has occurred while executing the request.")
    clearResponse >=> setStatusCode 500 >=> json ex.Message

  /// Handle 404s from the API, sending known URL paths to the Vue app so that they can be handled there
  let notFound : HttpHandler =
    fun next ctx ->
      [ "/journal"; "/legal"; "/request"; "/user" ]
      |> List.filter ctx.Request.Path.Value.StartsWith
      |> List.length
      |> function
      | 0 -> (setStatusCode 404 >=> json ([ "error", "not found" ] |> dict)) next ctx
      | _ -> Vue.app next ctx


/// Handler helpers
[<AutoOpen>]
module private Helpers =
  
  open Microsoft.AspNetCore.Http
  open Raven.Client.Documents
  open System.Threading.Tasks
  open System.Security.Claims

  /// Get the database context from DI
//  let db (ctx : HttpContext) =
  //  ctx.GetService<AppDbContext> ()

  /// Create a RavenDB session
  let session (ctx : HttpContext) =
    ctx.GetService<IDocumentStore>().OpenAsyncSession ()

  /// Get the user's "sub" claim
  let user (ctx : HttpContext) =
    ctx.User.Claims |> Seq.tryFind (fun u -> u.Type = ClaimTypes.NameIdentifier)

  /// Get the current user's ID
  //  NOTE: this may raise if you don't run the request through the authorize handler first
  let userId ctx =
    ((user >> Option.get) ctx).Value |> UserId

  /// Create a request ID from a string
  let toReqId = Domain.Cuid >> RequestId

  /// Return a 201 CREATED response
  let created next ctx =
    setStatusCode 201 next ctx

  /// The "now" time in JavaScript as Ticks
  let jsNow () =
    (int64 >> (*) 1000L >> Ticks) <| DateTime.UtcNow.Subtract(DateTime (1970, 1, 1, 0, 0, 0)).TotalSeconds
  
  /// Handler to return a 403 Not Authorized reponse
  let notAuthorized : HttpHandler =
    setStatusCode 403 >=> fun _ _ -> Task.FromResult<HttpContext option> None

  /// Handler to require authorization
  let authorize : HttpHandler =
    fun next ctx -> match user ctx with Some _ -> next ctx | None -> notAuthorized next ctx
  
  /// Flip JSON result so we can pipe into it
  let asJson<'T> next ctx (o : 'T) =
    json o next ctx


/// Strongly-typed models for post requests
module Models =
  
  /// A history entry addition (AKA request update)
  [<CLIMutable>]
  type HistoryEntry =
    { /// The status of the history update
      status     : string
      /// The text of the update
      updateText : string
      }
  
  /// An additional note
  [<CLIMutable>]
  type NoteEntry =
    { /// The notes being added
      notes : string
      }
  
  /// Recurrence update
  [<CLIMutable>]
  type Recurrence =
    { /// The recurrence type
      recurType  : string
      /// The recurrence cound
      recurCount : int16
      }

  /// A prayer request
  [<CLIMutable>]
  type Request =
    { /// The text of the request
      requestText : string
      /// The recurrence type
      recurType   : string
      /// The recurrence count
      recurCount  : int16
      }
  
  /// Reset the "showAfter" property on a request
  [<CLIMutable>]
  type Show =
    { /// The time after which the request should appear
      showAfter : int64
      }

  /// The time until which a request should not appear in the journal
  [<CLIMutable>]
  type SnoozeUntil =
    { /// The time at which the request should reappear
      until : int64
      }


/// /api/journal URLs
module Journal =
  
  /// GET /api/journal
  let journal : HttpHandler =
    authorize
    >=> fun next ctx ->
      task {
        use  sess = session ctx
        let! jrnl = ((userId >> sess.JournalByUserId) ctx).ToListAsync ()
        return! json jrnl next ctx
        }


/// /api/request URLs
module Request =
  
  open NCuid
  
  /// POST /api/request
  let add : HttpHandler =
    authorize
    >=> fun next ctx ->
      task {
        let! r     = ctx.BindJsonAsync<Models.Request> ()
        use  sess  = session ctx
        let  reqId = (Cuid.Generate >> toReqId) ()
        let  usrId = userId ctx
        let  now   = jsNow ()
        do! sess.AddRequest
              { Request.empty with
                  Id         = string reqId
                  userId     = usrId
                  enteredOn  = now
                  showAfter  = now
                  recurType  = Recurrence.fromString r.recurType
                  recurCount = r.recurCount
                  history    = [
                    { History.empty with
                        asOf   = now
                        status = "Created"
                        text   = Some r.requestText
                      }      
                    ]
                }
        do! sess.SaveChangesAsync ()
        match! sess.TryJournalById reqId usrId with
        | Some req -> return! (setStatusCode 201 >=> json req) next ctx
        | None -> return! Error.notFound next ctx
        }

  /// POST /api/request/[req-id]/history
  let addHistory requestId : HttpHandler =
    authorize
    >=> fun next ctx ->
      task {
        use sess  = session ctx
        let reqId = toReqId requestId
        match! sess.TryRequestById reqId (userId ctx) with
        | Some req ->
            let! hist = ctx.BindJsonAsync<Models.HistoryEntry> ()
            let  now  = jsNow ()
            { History.empty with
                asOf   = now
                status = hist.status
                text   = match hist.updateText with null | "" -> None | x -> Some x
              }
            |> sess.AddHistory reqId
            match hist.status with
            | "Prayed" ->
                (Ticks >> sess.UpdateShowAfter reqId) <| now.toLong () + (req.recurType.duration * int64 req.recurCount)
            | _ -> ()
            do! sess.SaveChangesAsync ()
            return! created next ctx
        | None -> return! Error.notFound next ctx
        }
  
  /// POST /api/request/[req-id]/note
  let addNote requestId : HttpHandler =
    authorize
    >=> fun next ctx ->
      task {
        use sess  = session ctx
        let reqId = toReqId requestId
        match! sess.TryRequestById reqId (userId ctx) with
        | Some _ ->
            let! notes = ctx.BindJsonAsync<Models.NoteEntry> ()
            sess.AddNote reqId { asOf = jsNow (); notes = notes.notes }
            do! sess.SaveChangesAsync ()
            return! created next ctx
        | None -> return! Error.notFound next ctx
        }
          
  /// GET /api/requests/answered
  let answered : HttpHandler =
    authorize
    >=> fun next ctx ->
      task {
        use sess = session ctx
        let! reqs = ((userId >> sess.AnsweredRequests) ctx).ToListAsync ()
        return! json reqs next ctx
        }
  
  /// GET /api/request/[req-id]
  let get requestId : HttpHandler =
    authorize
    >=> fun next ctx ->
      task {
        use sess = session ctx
        match! sess.TryJournalById (toReqId requestId) (userId ctx) with
        | Some req -> return! json req next ctx
        | None -> return! Error.notFound next ctx
        }
  
  /// GET /api/request/[req-id]/full
  let getFull requestId : HttpHandler =
    authorize
    >=> fun next ctx ->
      task {
        use sess = session ctx
        match! sess.TryFullRequestById (toReqId requestId) (userId ctx) with
        | Some req -> return! json req next ctx
        | None -> return! Error.notFound next ctx
        }
  
  /// GET /api/request/[req-id]/notes
  let getNotes requestId : HttpHandler =
    authorize
    >=> fun next ctx ->
      task {
        use sess = session ctx
        let! notes = sess.NotesById (toReqId requestId) (userId ctx)
        return! json notes next ctx
        }
  
  /// PATCH /api/request/[req-id]/show
  let show requestId : HttpHandler =
    authorize
    >=> fun next ctx ->
      task {
        use sess  = session ctx
        let reqId = toReqId requestId
        match! sess.TryRequestById reqId (userId ctx) with
        | Some _ ->
            let! show = ctx.BindJsonAsync<Models.Show> ()
            sess.UpdateShowAfter reqId (Ticks show.showAfter)
            do! sess.SaveChangesAsync ()
            return! setStatusCode 204 next ctx
        | None -> return! Error.notFound next ctx
        }
  
  /// PATCH /api/request/[req-id]/snooze
  let snooze requestId : HttpHandler =
    authorize
    >=> fun next ctx ->
      task {
        use sess  = session ctx
        let reqId = toReqId requestId
        match! sess.TryRequestById reqId (userId ctx) with
        | Some _ ->
            let! until = ctx.BindJsonAsync<Models.SnoozeUntil> ()
            sess.UpdateSnoozed reqId (Ticks until.until)
            do! sess.SaveChangesAsync ()
            return! setStatusCode 204 next ctx
        | None -> return! Error.notFound next ctx
        }

  /// PATCH /api/request/[req-id]/recurrence
  let updateRecurrence requestId : HttpHandler =
    authorize
    >=> fun next ctx ->
      task {
        use sess  = session ctx
        let reqId = toReqId requestId
        match! sess.TryRequestById reqId (userId ctx) with
        | Some _ ->
            let! recur = ctx.BindJsonAsync<Models.Recurrence> ()
            sess.UpdateRecurrence reqId (Recurrence.fromString recur.recurType) recur.recurCount
            do! sess.SaveChangesAsync ()
            return! setStatusCode 204 next ctx
        | None -> return! Error.notFound next ctx
        }