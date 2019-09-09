'use strict'

import { Store } from 'vuex'
import { Auth0DecodedHash, WebAuth } from 'auth0-js'
import { EventEmitter } from 'events'

import { AppState, Mutations } from '@/store'
import AUTH_CONFIG from './auth0-variables'

/** Auth0 web authentication instance to use for our calls */
const webAuth = new WebAuth({
  domain: AUTH_CONFIG.domain,
  clientID: AUTH_CONFIG.clientId,
  redirectUri: AUTH_CONFIG.appDomain + AUTH_CONFIG.callbackUrl,
  audience: `https://${AUTH_CONFIG.domain}/userinfo`,
  responseType: 'token id_token',
  scope: 'openid profile email'
})

/** A token and its expiration */
class Token {
  
  /** The token */
  token: string = ''
  
  /** The expiration (in ticks) */
  expiry: number = 0

  /** Check to see if this token has passed its expiration */
  isValid (): boolean {
    return this.token !== '' && this.expiry !== 0 && Date.now() < this.expiry
  }
}

/** An authenticated user session */
class UserSession {

  /** The ID token */
  id: Token = new Token()

  /** The access token */
  access: Token = new Token()
  
  /** The complete user profile returned from Auth0 */
  profile: any
}

/**
 * A class to handle all authentication calls and determinations
 */
class AuthService extends EventEmitter {
  
  // Local storage key for our session data
  AUTH_SESSION = 'auth-session'

  // Received and calculated values for our ssesion (initially loaded from local storage if present)
  session = new UserSession()

  constructor() {
    super()
    this.refreshSession()
  }

  /**
   * Starts the user log in flow
   */
  login (customState: any) {
    webAuth.authorize({
      appState: customState
    })
  }

  /**
   * Promisified parseHash function
   */
  parseHash () : Promise<Auth0DecodedHash> {
    return new Promise((resolve, reject) => {
      webAuth.parseHash((err, authResult) => {
        if (err || authResult === null) {
          reject(err)
        } else {
          resolve(authResult)
        }
      })
    })
  }

  /**
   * Handle authentication replies from Auth0
   * @param store The Vuex store
   */
  async handleAuthentication (store: Store<AppState>) {
    try {
      const authResult = await this.parseHash()
      if (authResult && authResult.accessToken && authResult.idToken) {
        this.setSession(authResult)
        store.commit(Mutations.UserLoggedOn, this.session.profile)
      }
    } catch(err) {
      console.error(err)
      alert(`Error: ${err.error}. Check the console for further details.`)
    }
  }

  /**
   * Set up the session and commit it to local storage
   * @param authResult The authorization result
   */
  setSession (authResult: Auth0DecodedHash) {
    this.session.profile = authResult.idTokenPayload
    this.session.id.token = authResult.idToken!
    this.session.id.expiry = this.session.profile.exp * 1000
    this.session.access.token = authResult.accessToken!
    this.session.access.expiry = authResult.expiresIn! * 1000 + Date.now()

    localStorage.setItem(this.AUTH_SESSION, JSON.stringify(this.session))

    this.emit('loginEvent', {
      loggedIn: true,
      profile: authResult.idTokenPayload,
      state: authResult.appState || {}
    })
  }

  /**
   * Refresh this instance's session from the one in local storage
   */
  refreshSession () {
    this.session = 
      localStorage.getItem(this.AUTH_SESSION)
      ? JSON.parse(localStorage.getItem(this.AUTH_SESSION) || '{}')
      : { profile: {},
          id: {
            token: null,
            expiry: null
          },
          access: {
            token: null,
            expiry: null
          }
        }
  }

  /**
   * Renew authorzation tokens with Auth0
   */
  renewTokens (): Promise<Auth0DecodedHash> {
    return new Promise((resolve, reject) => {
      this.refreshSession()
      if (this.session.id.token !== null) {
        webAuth.checkSession({}, (err, authResult) => {
          if (err) {
            reject(err)
          } else {
            let result = authResult as Auth0DecodedHash
            this.setSession(result)
            resolve(result)
          }
        })
      } else {
        reject('Not logged in')
      }
    })
  }

  /**
   * Log out of myPrayerJournal
   * @param store The Vuex store
   */
  logout (store: Store<AppState>) {
    // Clear access token and ID token from local storage
    localStorage.removeItem(this.AUTH_SESSION)
    this.refreshSession()

    store.commit(Mutations.UserLoggedOff)

    webAuth.logout({
      returnTo: `${AUTH_CONFIG.appDomain}/`,
      clientID: AUTH_CONFIG.clientId
    })
    this.emit('loginEvent', { loggedIn: false })
  }

  /**
   * Is there a user authenticated?
   */
  isAuthenticated () {
    return this.session.id.isValid()
  }

  /**
   * Is the current access token valid?
   */
  isAccessTokenValid () {
    return this.session.access.isValid()
  }

  /**
   * Get the user's access token, renewing it if required
   */
  async getAccessToken () {
    if (this.isAccessTokenValid()) {
      return this.session.access.token
    } else {
      try {
        const authResult = await this.renewTokens()
        return authResult.accessToken!
      } catch (reject) {
        throw reject
      }
    }
  }
}

export default new AuthService()
