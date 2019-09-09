<template lang="pug">
md-dialog(:md-active.sync='snoozeVisible').mpj-skinny
  md-dialog-title Snooze Prayer Request
  md-content.mpj-dialog-content
    span.mpj-text-muted Until
    md-datepicker(v-model='form.snoozedUntil'
                  :md-disabled-dates='datesInPast'
                  md-immediately)
  md-dialog-actions
    md-button(:disabled='!isValid'
              @click='snoozeRequest()').md-primary #[md-icon snooze] Snooze
    md-button(@click='closeDialog()') #[md-icon undo] Cancel
</template>

<script>
'use strict'

import { Actions } from '@/store'

export default {
  name: 'snooze-request',
  inject: [
    'journalEvents',
    'messages',
    'progress'
  ],
  props: {
    events: { required: true }
  },
  data () {
    return {
      snoozeVisible: false,
      datesInPast: date => date < new Date(),
      form: {
        requestId: '',
        snoozedUntil: ''
      }
    }
  },
  created () {
    this.journalEvents.$on('snooze', this.openDialog)
  },
  computed: {
    isValid () {
      return !isNaN(Date.parse(this.form.snoozedUntil))
    }
  },
  methods: {
    closeDialog () {
      this.form.requestId = ''
      this.form.snoozedUntil = ''
      this.snoozeVisible = false
    },
    openDialog (requestId) {
      this.form.requestId = requestId
      this.snoozeVisible = true
    },
    async snoozeRequest () {
      await this.$store.dispatch(Actions.SnoozeRequest, {
        progress: this.progress,
        requestId: this.form.requestId,
        until: Date.parse(this.form.snoozedUntil)
      })
      this.messages.$emit('info', `Request snoozed until ${this.form.snoozedUntil}`)
      this.closeDialog()
    }
  }
}
</script>
