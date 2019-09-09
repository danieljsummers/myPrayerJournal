'use strict'

import axios from 'axios'

const http = axios.create({
  baseURL: `${location.protocol}//${location.host}/api/`
})

/**
 * API access for myPrayerJournal
 */
export default {

  /**
   * Set the bearer token for all future requests
   * @param token The token to use to identify the user to the server
   */
  setBearer: (token: string) => { http.defaults.headers.common['Authorization'] = `Bearer ${token}` },

  /**
   * Remove the bearer token
   */
  removeBearer: () => delete http.defaults.headers.common['Authorization'],

  /**
   * Add a note for a prayer request
   * @param requestId The Id of the request to which the note applies
   * @param notes The notes to be added
   */
  addNote: (requestId: string, notes: string) => http.post(`request/${requestId}/note`, { notes }),

  /**
   * Add a new prayer request
   * @param requestText The text of the request to be added
   * @param recurType The type of recurrence for this request
   * @param recurCount The number of intervals of recurrence
   */
  addRequest: (requestText: string, recurType: string, recurCount: number) =>
    http.post('request', { requestText, recurType, recurCount }),

  /**
   * Get all answered requests, along with the text they had when it was answered
   */
  getAnsweredRequests: () => http.get('requests/answered'),

  /**
   * Get a prayer request (full; includes all history and notes)
   * @param requestId The Id of the request to retrieve
   */
  getFullRequest: (requestId: string) => http.get(`request/${requestId}/full`),

  /**
   * Get past notes for a prayer request
   * @param requestId The Id of the request for which notes should be retrieved
   */
  getNotes: (requestId: string) => http.get(`request/${requestId}/notes`),

  /**
   * Get a prayer request (journal-style; only latest update)
   * @param requestId The Id of the request to retrieve
   */
  getRequest: (requestId: string) => http.get(`request/${requestId}`),

  /**
   * Get all prayer requests and their most recent updates
   */
  journal: () => http.get('journal'),

  /**
   * Show a request after the given date (used for "show now")
   * @param requestId The ID of the request which should be shown
   * @param showAfter The ticks after which the request should be shown
   */
  showRequest: (requestId: string, showAfter: number) => http.patch(`request/${requestId}/show`, { showAfter }),

  /**
   * Snooze a request until the given time
   * @param requestId The ID of the prayer request to be snoozed
   * @param until The ticks until which the request should be snoozed
   */
  snoozeRequest: (requestId: string, until: number) => http.patch(`request/${requestId}/snooze`, { until }),

  /**
   * Update recurrence for a prayer request
   * @param requestId The ID of the prayer request for which recurrence is being updated
   * @param recurType The type of recurrence to set
   * @param recurCount The number of recurrence intervals to set
   */
  updateRecurrence: (requestId: string, recurType: string, recurCount: number) =>
    http.patch(`request/${requestId}/recurrence`, { recurType, recurCount }),

  /**
   * Update a prayer request
   * @param requestId The ID of the request to be updated
   * @param status The status of the update
   * @param updateText The text of the update (optional)
   */
  updateRequest: (requestId: string, status: string, updateText: string) =>
    http.post(`request/${requestId}/history`, { status, updateText })
}
