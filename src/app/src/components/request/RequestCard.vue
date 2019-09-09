<template lang="pug">
md-card(v-if='shouldDisplay'
        md-with-hover).mpj-request-card
  md-card-actions(md-alignment='space-between')
    md-button(@click='markPrayed()').md-icon-button.md-raised.md-primary
      md-icon done
      md-tooltip(md-direction='top'
                 md-delay=1000) Mark as Prayed
    span
      md-button(@click.stop='showEdit()').md-icon-button.md-raised
        md-icon edit
        md-tooltip(md-direction='top'
                   md-delay=1000) Edit Request
      md-button(@click.stop='showNotes()').md-icon-button.md-raised
        md-icon comment
        md-tooltip(md-direction='top'
                   md-delay=1000) Add Notes
      md-button(@click.stop='snooze()').md-icon-button.md-raised
        md-icon schedule
        md-tooltip(md-direction='top'
                   md-delay=1000) Snooze Request
  md-card-content
    p.mpj-request-text {{ request.text }}
    p.mpj-text-right: small.mpj-muted-text: em (last activity #[date-from-now(:value='request.asOf')])
</template>

<script>
'use strict'

import { Actions } from '@/store'

export default {
  name: 'request-card',
  inject: [
    'journalEvents',
    'messages',
    'progress'
  ],
  props: {
    request: { required: true }
  },
  computed: {
    shouldDisplay () {
      const now = Date.now()
      return Math.max(now, this.request.showAfter, this.request.snoozedUntil) === now
    }
  },
  methods: {
    async markPrayed () {
      await this.$store.dispatch(Actions.UpdateRequest, {
        progress: this.progress,
        requestId: this.request.requestId,
        status: 'Prayed',
        updateText: ''
      })
      this.messages.$emit('info', 'Request marked as prayed')
    },
    showEdit () {
      this.$router.push({ name: 'EditRequest', params: { id: this.request.requestId } })
    },
    showNotes () {
      this.journalEvents.$emit('notes', this.request)
    },
    snooze () {
      this.journalEvents.$emit('snooze', this.request.requestId)
    }
  }
}
</script>

<style lang="sass">
.mpj-request-card
  width: 20rem
  margin-bottom: 1rem
@media screen and (max-width: 20rem)
  .mpj-request-card
    width: 100%
</style>
