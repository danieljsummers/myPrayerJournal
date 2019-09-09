'use strict'

import authService from '../auth/AuthService'

export default {
  install (Vue: any) {
    Vue.prototype.$auth = authService

    Vue.mixin({
      created () {
        if (this.handleLoginEvent) {
          authService.addListener('loginEvent', this.handleLoginEvent)
        }
      },
      destroyed () {
        if (this.handleLoginEvent) {
          authService.removeListener('loginEvent', this.handleLoginEvent)
        }
      }
    })
  }
}
