<template lang="pug">
md-content(role='main').mpj-main-content-wide
  page-title(:title='title')
  p(v-if='isLoadingJournal') Loading your prayer journal...
  template(v-else)
    md-empty-state(v-if='journal.length === 0'
                   md-icon='done_all'
                   md-label='No Requests to Show'
                   md-description='You have no requests to be shown; see the “Active” link above for snoozed/deferred requests, and the “Answered” link for answered requests')
      md-button(:to="{ name: 'EditRequest', params: { id: 'new' } }").md-primary.md-raised Add a New Request
    template(v-else)
      .mpj-text-center
        md-button(:to="{ name: 'EditRequest', params: { id: 'new' } }"
                  role='button').md-raised.md-accent #[md-icon add_box] Add a New Request
      br
      .mpj-journal
        request-card(v-for='request in journal'
                    :key='request.requestId'
                    :request='request')
    notes-edit
    snooze-request
</template>

<script>
'use strict'

import Vue from 'vue'
import { mapState } from 'vuex'

import NotesEdit from './request/NotesEdit'
import RequestCard from './request/RequestCard'
import SnoozeRequest from './request/SnoozeRequest'

import { Actions } from '@/store'

export default {
  name: 'journal',
  inject: [
    'messages',
    'progress'
  ],
  components: {
    NotesEdit,
    RequestCard,
    SnoozeRequest
  },
  data () {
    return {
      eventBus: new Vue()
    }
  },
  computed: {
    title () {
      return `${this.user.given_name}&rsquo;s Prayer Journal`
    },
    snackbar () {
      return this.$parent.$refs.snackbar
    },
    ...mapState(['user', 'journal', 'isLoadingJournal'])
  },
  async created () {
    await this.$store.dispatch(Actions.LoadJournal, this.progress)
    this.messages.$emit('info', `Loaded ${this.journal.length} prayer requests`)
  },
  provide () {
    return {
      journalEvents: this.eventBus
    }
  }
}
</script>

<style lang="sass">
.mpj-journal
  display: flex
  flex-flow: row wrap
  justify-content: center
  align-items: flex-start
.mpj-dialog-content
  padding: 0 1rem
</style>
