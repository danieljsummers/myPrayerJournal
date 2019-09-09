'use strict'

/* eslint-disable no-multi-spaces */
import Vue  from 'vue'
import Vuex from 'vuex'

import api  from '@/api'
import auth from '@/auth/AuthService'

import { Actions, AppState, JournalRequest, Mutations } from './Types'
/* eslint-enable no-multi-spaces */

Vue.use(Vuex)

/* eslint-disable no-console */
const logError = function (error: any) {
  if (error.response) {
    // The request was made and the server responded with a status code
    // that falls out of the range of 2xx
    console.error(error.response.data)
    console.error(error.response.status)
    console.error(error.response.headers)
  } else if (error.request) {
    // The request was made but no response was received
    // `error.request` is an instance of XMLHttpRequest in the browser and an instance of
    // http.ClientRequest in node.js
    console.error(error.request)
  } else {
    // Something happened in setting up the request that triggered an Error
    console.error('Error', error.message)
  }
  console.error(`config: ${error.config}`)
}

/**
 * Set the "Bearer" authorization header with the current access token
 */
const setBearer = async function () {
  try {
    await auth.getAccessToken()
    api.setBearer(auth.session.id.token)
  } catch (err) {
    if (err === 'Not logged in') {
      console.warn('API request attempted when user was not logged in')
    } else {
      console.error(err)
    }
  }
}
/* eslint-enable no-console */

/** The initial state of the store */
let state : AppState = {
  user: auth.session.profile,
  isAuthenticated: auth.isAuthenticated(),
  journal: [],
  isLoadingJournal: false
}

export default new Vuex.Store({
  state,
  mutations: {
    [Mutations.LoadingJournal] (state, flag: boolean) {
      state.isLoadingJournal = flag
    },
    [Mutations.LoadedJournal] (state, journal: JournalRequest[]) {
      state.journal = journal
    },
    [Mutations.RequestAdded] (state, newRequest: JournalRequest) {
      state.journal.push(newRequest)
    },
    [Mutations.RequestUpdated] (state, request: JournalRequest) {
      let jrnl = state.journal.filter(it => it.requestId !== request.requestId)
      if (request.lastStatus !== 'Answered') jrnl.push(request)
      state.journal = jrnl
    },
    [Mutations.SetAuthentication] (state, value: boolean) {
      state.isAuthenticated = value
    },
    [Mutations.UserLoggedOff] (state) {
      state.user = {}
      api.removeBearer()
      state.isAuthenticated = false
    },
    [Mutations.UserLoggedOn] (state, user: any) {
      state.user = user
      state.isAuthenticated = true
    }
  },
  actions: {
    async [Actions.AddRequest] ({ commit }, { progress, requestText, recurType, recurCount }) {
      progress.$emit('show', 'indeterminate')
      try {
        await setBearer()
        const newRequest = await api.addRequest(requestText, recurType, recurCount)
        commit(Mutations.RequestAdded, newRequest.data)
        progress.$emit('done')
      } catch (err) {
        logError(err)
        progress.$emit('done')
      }
    },
    async [Actions.CheckAuthentication] ({ commit }) {
      try {
        await auth.getAccessToken()
        commit(Mutations.SetAuthentication, auth.isAuthenticated())
      } catch (_) {
        commit(Mutations.SetAuthentication, false)
      }
    },
    async [Actions.LoadJournal] ({ commit }, progress) {
      commit(Mutations.LoadedJournal, [])
      progress.$emit('show', 'query')
      commit(Mutations.LoadingJournal, true)
      await setBearer()
      try {
        const jrnl = await api.journal()
        commit(Mutations.LoadedJournal, jrnl.data)
        progress.$emit('done')
      } catch (err) {
        logError(err)
        progress.$emit('done')
      } finally {
        commit(Mutations.LoadingJournal, false)
      }
    },
    async [Actions.UpdateRequest] ({ commit, state }, { progress, requestId, status, updateText, recurType, recurCount }) {
      progress.$emit('show', 'indeterminate')
      try {
        await setBearer()
        let oldReq = (state.journal.filter(req => req.requestId === requestId) || [])[0] || {}
        if (!(status === 'Prayed' && updateText === '')) {
          if (status !== 'Answered' && (oldReq.recurType !== recurType || oldReq.recurCount !== recurCount)) {
            await api.updateRecurrence(requestId, recurType, recurCount)
          }
        }
        if (status !== 'Updated' || oldReq.text !== updateText) {
          await api.updateRequest(requestId, status, oldReq.text !== updateText ? updateText : '')
        }
        const request = await api.getRequest(requestId)
        commit(Mutations.RequestUpdated, request.data)
        progress.$emit('done')
      } catch (err) {
        logError(err)
        progress.$emit('done')
      }
    },
    async [Actions.ShowRequestNow] ({ commit }, { progress, requestId, showAfter }) {
      progress.$emit('show', 'indeterminate')
      try {
        await setBearer()
        await api.showRequest(requestId, showAfter)
        const request = await api.getRequest(requestId)
        commit(Mutations.RequestUpdated, request.data)
        progress.$emit('done')
      } catch (err) {
        logError(err)
        progress.$emit('done')
      }
    },
    async [Actions.SnoozeRequest] ({ commit }, { progress, requestId, until }) {
      progress.$emit('show', 'indeterminate')
      try {
        await setBearer()
        await api.snoozeRequest(requestId, until)
        const request = await api.getRequest(requestId)
        commit(Mutations.RequestUpdated, request.data)
        progress.$emit('done')
      } catch (err) {
        logError(err)
        progress.$emit('done')
      }
    }
  },
  getters: {},
  modules: {}
})

export * from './Types'
