'use strict'

/** A prayer request that is part of the user's journal */
export interface JournalRequest {
  /** The ID of the request (just the CUID part) */
  requestId: string
  /** The ID of the user to whom the request belongs */
  userId: string
  /** The current text of the request */
  text: string
  /** The last time action was taken on the request */
  asOf: number
  /** The last status for the request */
  lastStatus: string // TODO string union?
  /** The time that this request should reappear in the user's journal */
  snoozedUntil: number
  /** The time after which this request should reappear in the user's journal by configured recurrence */
  showAfter: number
  /** The type of recurrence for this request */
  recurType: string // TODO Recurrence union?
  /** How many of the recurrence intervals should occur between appearances in the journal */
  recurCount: number
  /** History entries for the request */
  history: any[] // History list
  /** Note entries for the request */
  notes: any[] // Note list
}

export interface AppState {
  /** The profile of the user currently logged on */
  user: any
  /** Whether there is an authenticated user */
  isAuthenticated: boolean
  /** The user's current request journal */
  journal: JournalRequest[]
  /** Whether the journal is in the process of loading */
  isLoadingJournal: boolean
}

/** All actions supported by the store */
const actions = {
  /** Action to add a prayer request (pass request text) */
  AddRequest: 'add-request',
  /** Action to check if a user is authenticated, refreshing the session first if it exists */
  CheckAuthentication: 'check-authentication',
  /** Action to load the user's prayer journal */
  LoadJournal: 'load-journal',
  /** Action to update a request */
  UpdateRequest: 'update-request',
  /** Action to skip the remaining recurrence period */
  ShowRequestNow: 'show-request-now',
  /** Action to snooze a request */
  SnoozeRequest: 'snooze-request'
}

/** All mutations supported by the store */
const mutations = {
  /** Mutation for when the user's prayer journal is being loaded */
  LoadingJournal: 'loading-journal',
  /** Mutation for when the user's prayer journal has been loaded */
  LoadedJournal: 'journal-loaded',
  /** Mutation for adding a new prayer request (pass text) */
  RequestAdded: 'request-added',
  /** Mutation to replace a prayer request at the top of the current journal */
  RequestUpdated: 'request-updated',
  /** Mutation for setting the authentication state */
  SetAuthentication: 'set-authentication',
  /** Mutation for logging a user off */
  UserLoggedOff: 'user-logged-off',
  /** Mutation for logging a user on (pass user) */
  UserLoggedOn: 'user-logged-on'
}

export { actions as Actions }
export { mutations as Mutations }
