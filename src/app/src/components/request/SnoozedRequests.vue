<template lang="pug">
article.mpj-main-content(role='main')
  page-title(title='Snoozed Requests'
             hide-on-page=true)
  template(v-if='loaded')
    md-empty-state(v-if='requests.length === 0'
                   md-icon='sentiment_dissatisfied'
                   md-label='No Snoozed Requests'
                   md-description='Your prayer journal has no snoozed requests')
      md-button(to='/journal').md-primary.md-raised Return to your journal
    request-list(v-if='requests.length !== 0'
                 title='Snoozed Requests'
                 :requests='requests')
  p(v-else) Loading journal...
</template>

<script>
'use strict'

import { mapState } from 'vuex'

import { Actions } from '@/store'

import RequestList from '@/components/request/RequestList'

export default {
  name: 'snoozed-requests',
  inject: ['progress'],
  components: {
    RequestList
  },
  data () {
    return {
      requests: [],
      loaded: false
    }
  },
  computed: {
    ...mapState(['journal', 'isLoadingJournal'])
  },
  created () {
    this.$on('requestUnsnoozed', this.ensureJournal)
  },
  methods: {
    async ensureJournal () {
      if (!Array.isArray(this.journal)) {
        this.loaded = false
        await this.$store.dispatch(Actions.LoadJournal, this.progress)
      }
      this.requests = this.journal
        .filter(req => req.snoozedUntil > Date.now())
        .sort((a, b) => a.snoozedUntil - b.snoozedUntil)
      this.loaded = true
    }
  },
  async mounted () {
    await this.ensureJournal()
  }
}
</script>
