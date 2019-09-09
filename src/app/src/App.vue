<template lang="pug">
#app.page-container
  md-app(md-waterfall md-mode='fixed-last' role='application')
    md-app-toolbar.md-large.md-dense.md-primary
      .md-toolbar-row
        .md-toolbar-section-start
          router-link(to='/').md-title
            span(style='font-weight:100;') my
            span(style='font-weight:400;') Prayer
            span(style='font-weight:700;') Journal
      navigation
    md-app-content
      md-progress-bar(v-if='progress.visible'
                      :md-mode='progress.mode')
      router-view
      md-snackbar(:md-active.sync='snackbar.visible'
                  md-position='center'
                  :md-duration='snackbar.interval'
                  ref='snackbar') {{ snackbar.message }}
      footer
        p.mpj-muted-text.mpj-text-right
          | myPrayerJournal v{{ version }}
          br
          em: small.
            #[router-link(to='/legal/privacy-policy') Privacy Policy] &bull;
            #[router-link(to='/legal/terms-of-service') Terms of Service] &bull;
            #[a(href='https://github.com/bit-badger/myprayerjournal' target='_blank') Developed] and hosted by
            #[a(href='https://bitbadger.solutions' target='_blank') Bit Badger Solutions]
</template>

<script lang="ts">
'use strict'

import Vue from 'vue'
import { Component } from 'vue-property-decorator'

import Navigation from '@/components/common/Navigation.vue'

import { Actions } from './store'
import { version } from '../package.json'

interface ProgressOptions {
  /** A Vue instance to use as an event bus */
  events: Vue
  /** Whether the progress bar is visible */
  visible: boolean
  /** The mode for the progress bar */
  mode: string
}

interface SnackbarOptions {
  /** A Vue instance to use as an event bus */
  events: Vue
  /** Whether the snackbar is visible */
  visible: boolean
  /** The message to display in the snackbar */
  message: string
  /** The duration for which the snackbar will be visible */
  interval: number
}

@Component({ components: { Navigation } })
export default class App extends Vue {
  
  /** Properties to support the global progress bar */
  progress: ProgressOptions = {
    events: new Vue(),
    visible: false,
    mode: 'query'
  }
  
  /** Properties to support the global snackbar */
  snackbar: SnackbarOptions = {
    events: new Vue(),
    visible: false,
    message: '',
    interval: 4000
  }

  /** Register event listeners */
  async mounted () {
    this.progress.events.$on('show', this.showProgress)
    this.progress.events.$on('done', this.hideProgress)
    this.snackbar.events.$on('info', this.showInfo)
    this.snackbar.events.$on('error', this.showError)
    await this.$store.dispatch(Actions.CheckAuthentication)
  }

  /** myPrayerJournal version string */
  get version () {
    return version.endsWith('.0')
      ? version.endsWith('.0.0')
        ? version.substr(0, version.length - 4)
        : version.substr(0, version.length - 2)
      : version
  }

  /**
   * Show the snackbar
   * @param message The message to be displayed
   */
  showSnackbar (message: string) {
    this.snackbar.message = message
    this.snackbar.visible = true
  }

  /**
   * Show an informational message in the snackbar
   * @param message The message to be displayed
   */
  showInfo (message: string) {
    this.snackbar.interval = 4000
    this.showSnackbar(message)
  }

  /**
   * Show an error message in the snackbar
   * @param message The message to be displayec
   */
  showError (message: string) {
    this.snackbar.interval = Infinity
    this.showSnackbar(message)
  }

  /**
   * Show the progress bar
   * @param mode The mode of progress bar to display
   */
  showProgress (mode: string) {
    this.progress.mode = mode
    this.progress.visible = true
  }

  /**
   * Hide the progress bar
   */
  hideProgress () {
    this.progress.visible = false
  }

  /**
   * Event listener to display a message when the user logs off
   */
  handleLoginEvent (data) {
    if (!data.loggedIn) {
      this.showInfo('Logged out successfully')
    }
  }

  provide () {
    return {
      messages: this.snackbar.events,
      progress: this.progress.events
    }
  }
}
</script>

<style lang="sass">
@import "~vue-material/dist/theme/engine"
@include md-register-theme("default", (primary: md-get-palette-color(green, 800), accent: md-get-palette-color(gray, 700)))
@import "~vue-material/dist/theme/all"

html, body
  font-family: -apple-system,BlinkMacSystemFont,"Segoe UI",Roboto,Oxygen-Sans,Ubuntu,Cantarell,"Helvetica Neue",sans-serif;
  font-size: 1rem
p
  margin-bottom: 0
footer
  border-top: solid 1px lightgray
  margin: 1rem -1rem 0
  padding: 0 1rem
footer p
  margin: 0
.mpj-full-page-card
  font-size: 1rem
  line-height: 1.25rem
.mpj-main-content
  max-width: 60rem
  margin: auto
.mpj-request-text
  white-space: pre-line
p.mpj-request-text
  margin-top: 0
.mpj-text-center
  text-align: center
.mpj-text-nowrap
  white-space: nowrap
.mpj-text-right
  text-align: right
.mpj-muted-text
  color: rgba(0, 0, 0, .6)
.mpj-valign-top
  vertical-align: top
.mpj-narrow
  max-width: 40rem
  margin: auto
.mpj-skinny
  max-width: 20rem
  margin: auto
.mpj-full-width
  width: 100%
.md-progress-bar
  margin: 24px
</style>
