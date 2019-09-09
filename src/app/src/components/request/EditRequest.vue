<template lang="pug">
md-content(role='main').mpj-narrow
  page-title(:title='title')
  md-field
    label(for='request_text') Prayer Request
    md-textarea(v-model='form.requestText'
                @blur='trimText()'
                md-autogrow
                autofocus).mpj-full-width
  br
  template(v-if='!isNew')
    label Also Mark As
    br
    md-radio(v-model='form.status'
             value='Updated') Updated
    md-radio(v-model='form.status'
             value='Prayed') Prayed
    md-radio(v-model='form.status'
             value='Answered') Answered
    br
  label Recurrence
  | &nbsp; &nbsp;
  em.mpj-muted-text After prayer, request reappears...
  br
  .md-layout
    .md-layout-item.md-size-30
      md-radio(v-model='form.recur.typ'
              value='Immediate') Immediately
    .md-layout-item.md-size-20
      md-radio(v-model='form.recur.typ'
              value='other') Every...
    .md-layout-item.md-size-10
      md-field(md-inline)
        label Count
        md-input(v-model='form.recur.count'
                type='number'
                :disabled='!showRecurrence')
    .md-layout-item.md-size-20
      md-field
        label Interval
        md-select(v-model='form.recur.other'
                  :disabled='!showRecurrence')
          md-option(value='Hours') hours
          md-option(value='Days') days
          md-option(value='Weeks') weeks
  .mpj-text-right
    md-button(:disabled='!isValidRecurrence'
              @click.stop='saveRequest()').md-primary.md-raised #[md-icon save] Save
    md-button(@click.stop='goBack()').md-raised #[md-icon arrow_back] Cancel
</template>

<script>
'use strict'

import { mapState } from 'vuex'

import { Actions } from '@/store'

export default {
  name: 'edit-request',
  inject: [
    'messages',
    'progress'
  ],
  props: {
    id: {
      type: String,
      required: true
    }
  },
  data () {
    return {
      title: 'Edit Prayer Request',
      isNew: false,
      form: {
        requestId: '',
        requestText: '',
        status: 'Updated',
        recur: {
          typ: 'Immediate',
          other: '',
          count: ''
        }
      }
    }
  },
  computed: {
    isValidRecurrence () {
      if (this.form.recur.typ === 'Immediate') return true
      const count = Number.parseInt(this.form.recur.count)
      if (isNaN(count) || this.form.recur.other === '') return false
      if (this.form.recur.other === 'Hours' && count > (365 * 24)) return false
      if (this.form.recur.other === 'Days' && count > 365) return false
      if (this.form.recur.other === 'Weeks' && count > 52) return false
      return true
    },
    showRecurrence () {
      return this.form.recur.typ !== 'Immediate'
    },
    ...mapState(['journal'])
  },
  async mounted () {
    await this.ensureJournal()
    if (this.id === 'new') {
      this.title = 'Add Prayer Request'
      this.isNew = true
      this.form.requestId = ''
      this.form.requestText = ''
      this.form.status = 'Created'
      this.form.recur.typ = 'Immediate'
      this.form.recur.other = ''
      this.form.recur.count = ''
    } else {
      this.title = 'Edit Prayer Request'
      this.isNew = false
      if (this.journal.length === 0) {
        await this.$store.dispatch(Actions.LoadJournal, this.progress)
      }
      const req = this.journal.filter(r => r.requestId === this.id)[0]
      this.form.requestId = this.id
      this.form.requestText = req.text
      this.form.status = 'Updated'
      if (req.recurType === 'Immediate') {
        this.form.recur.typ = 'Immediate'
        this.form.recur.other = ''
        this.form.recur.count = ''
      } else {
        this.form.recur.typ = 'other'
        this.form.recur.other = req.recurType
        this.form.recur.count = req.recurCount
      }
    }
  },
  methods: {
    goBack () {
      this.$router.go(-1)
    },
    trimText () {
      this.form.requestText = this.form.requestText.trim()
    },
    async ensureJournal () {
      if (!Array.isArray(this.journal)) {
        await this.$store.dispatch(actions.LOAD_JOURNAL, this.progress)
      }
    },
    async saveRequest () {
      if (this.isNew) {
        await this.$store.dispatch(Actions.AddRequest, {
          progress: this.progress,
          requestText: this.form.requestText,
          recurType: this.form.recur.typ === 'Immediate' ? 'Immediate' : this.form.recur.other,
          recurCount: this.form.recur.typ === 'Immediate' ? 0 : Number.parseInt(this.form.recur.count)
        })
        this.messages.$emit('info', 'New prayer request added')
      } else {
        await this.$store.dispatch(Actions.UpdateRequest, {
          progress: this.progress,
          requestId: this.form.requestId,
          updateText: this.form.requestText,
          status: this.form.status,
          recurType: this.form.recur.typ === 'Immediate' ? 'Immediate' : this.form.recur.other,
          recurCount: this.form.recur.typ === 'Immediate' ? 0 : Number.parseInt(this.form.recur.count)
        })
        if (this.form.status === 'Answered') {
          this.messages.$emit('info', 'Request updated and removed from active journal')
        } else {
          this.messages.$emit('info', 'Request updated')
        }
      }
      this.goBack()
    }
  }
}
</script>
