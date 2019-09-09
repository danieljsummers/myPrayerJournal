'use strict'

export default {
  /** Action to add a prayer request (pass request text) */
  ADD_REQUEST: 'add-request',
  /** Action to check if a user is authenticated, refreshing the session first if it exists */
  CHECK_AUTHENTICATION: 'check-authentication',
  /** Action to load the user's prayer journal */
  LOAD_JOURNAL: 'load-journal',
  /** Action to update a request */
  UPDATE_REQUEST: 'update-request',
  /** Action to skip the remaining recurrence period */
  SHOW_REQUEST_NOW: 'show-request-now',
  /** Action to snooze a request */
  SNOOZE_REQUEST: 'snooze-request'
}
