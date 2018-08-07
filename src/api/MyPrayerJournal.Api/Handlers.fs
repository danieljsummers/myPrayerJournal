﻿/// HTTP handlers for the myPrayerJournal API
[<RequireQualifiedAccess>]
module MyPrayerJournal.Api.Handlers

open Giraffe
open MyPrayerJournal
open System

module Error =

  open Microsoft.Extensions.Logging

  /// Handle errors
  let error (ex : Exception) (log : ILogger) =
    log.LogError (EventId(), ex, "An unhandled exception has occurred while executing the request.")
    clearResponse >=> setStatusCode 500 >=> json ex.Message

  /// Handle 404s from the API, sending known URL paths to the Vue app so that they can be handled there
  let notFound : HttpHandler =
    fun next ctx ->
      [ "/answered"; "/journal"; "/snoozed"; "/user" ]
      |> List.filter ctx.Request.Path.Value.StartsWith
      |> List.length
      |> function
      | 0 -> (setStatusCode 404 >=> json ([ "error", "not found" ] |> dict)) next ctx
      | _ -> htmlFile "wwwroot/index.html" next ctx


/// Handler helpers
[<AutoOpen>]
module private Helpers =
  
  open Microsoft.AspNetCore.Http
  open System.Threading.Tasks
  open System.Security.Claims

  /// Get the database context from DI
  let db (ctx : HttpContext) =
    ctx.GetService<AppDbContext> ()

  /// Get the user's "sub" claim
  let user (ctx : HttpContext) =
    ctx.User.Claims |> Seq.tryFind (fun u -> u.Type = ClaimTypes.NameIdentifier)

  /// Get the current user's ID
  //  NOTE: this may raise if you don't run the request through the authorize handler first
  let userId ctx =
    ((user >> Option.get) ctx).Value

  /// Return a 201 CREATED response
  let created next ctx =
    setStatusCode 201 next ctx

  /// The "now" time in JavaScript
  let jsNow () =
    DateTime.UtcNow.Subtract(DateTime (1970, 1, 1, 0, 0, 0)).TotalSeconds |> int64 |> (*) 1000L
  
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
  
  /// A prayer request
  [<CLIMutable>]
  type Request =
    { /// The text of the request
      requestText : string
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
      userId ctx
      |> (db ctx).JournalByUserId
      |> asJson next ctx


/// /api/request URLs
module Request =
  
  open NCuid
  
  /// POST /api/request
  let add : HttpHandler =
    authorize
    >=> fun next ctx ->
      task {
        let! r     = ctx.BindJsonAsync<Models.Request> ()
        let  db    = db ctx
        let  reqId = Cuid.Generate ()
        let  usrId = userId ctx
        let  now   = jsNow ()
        { Request.empty with
            requestId    = reqId
            userId       = usrId
            enteredOn    = now
            snoozedUntil = 0L
          }
        |> db.AddEntry
        { History.empty with
            requestId = reqId
            asOf      = now
            status    = "Created"
            text      = Some r.requestText
            }
        |> db.AddEntry
        let! _   = db.SaveChangesAsync ()
        let! req = db.TryJournalById reqId usrId
        match req with
        | Some rqst -> return! (setStatusCode 201 >=> json rqst) next ctx
        | None -> return! Error.notFound next ctx
        }

  /// POST /api/request/[req-id]/history
  let addHistory reqId : HttpHandler =
    authorize
    >=> fun next ctx ->
      task {
        let  db  = db ctx
        let! req = db.TryRequestById reqId (userId ctx)
        match req with
        | Some _ ->
            let! hist = ctx.BindJsonAsync<Models.HistoryEntry> ()
            { History.empty with
                requestId = reqId
                asOf      = jsNow ()
                status    = hist.status
                text      = match hist.updateText with null | "" -> None | x -> Some x
              }
            |> db.AddEntry
            let! _ = db.SaveChangesAsync ()
            return! created next ctx
        | None -> return! Error.notFound next ctx
        }
  
  /// POST /api/request/[req-id]/note
  let addNote reqId : HttpHandler =
    authorize
    >=> fun next ctx ->
      task {
        let  db  = db ctx
        let! req = db.TryRequestById reqId (userId ctx)
        match req with
        | Some _ ->
            let! notes = ctx.BindJsonAsync<Models.NoteEntry> ()
            { Note.empty with
                requestId = reqId
                asOf = jsNow ()
                notes = notes.notes
              }
            |> db.AddEntry
            let! _ = db.SaveChangesAsync ()
            return! created next ctx
        | None -> return! Error.notFound next ctx
        }
          
  /// GET /api/requests/answered
  let answered : HttpHandler =
    authorize
    >=> fun next ctx ->
      userId ctx
      |> (db ctx).AnsweredRequests
      |> asJson next ctx
  
  /// GET /api/request/[req-id]
  let get reqId : HttpHandler =
    authorize
    >=> fun next ctx ->
      task {
        let! req = (db ctx).TryJournalById reqId (userId ctx)
        match req with
        | Some r -> return! json r next ctx
        | None -> return! Error.notFound next ctx
        }
  
  /// GET /api/request/[req-id]/complete
  let getComplete reqId : HttpHandler =
    authorize
    >=> fun next ctx ->
      task {
        let! req = (db ctx).TryCompleteRequestById reqId (userId ctx)
        match req with
        | Some r -> return! json r next ctx
        | None -> return! Error.notFound next ctx
        }
  
  /// GET /api/request/[req-id]/full
  let getFull reqId : HttpHandler =
    authorize
    >=> fun next ctx ->
      task {
        let! req = (db ctx).TryFullRequestById reqId (userId ctx)
        match req with
        | Some r -> return! json r next ctx
        | None -> return! Error.notFound next ctx
        }
  
  /// GET /api/request/[req-id]/notes
  let getNotes reqId : HttpHandler =
    authorize
    >=> fun next ctx ->
      task {
        let! notes = (db ctx).NotesById reqId (userId ctx)
        return! json notes next ctx
        }
  
  /// POST /api/request/[req-id]/snooze
  let snooze reqId : HttpHandler =
    authorize
    >=> fun next ctx ->
      task {
        let  db  = db ctx
        let! req = db.TryRequestById reqId (userId ctx)
        match req with
        | Some r ->
            let! until = ctx.BindJsonAsync<Models.SnoozeUntil> ()
            { r with snoozedUntil = until.until }
            |> db.UpdateEntry
            let! _ = db.SaveChangesAsync ()
            return! setStatusCode 204 next ctx
        | None -> return! Error.notFound next ctx
        }