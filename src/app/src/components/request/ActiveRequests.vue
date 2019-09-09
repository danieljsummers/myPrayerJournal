<template lang="pug">
md-content(role='main').mpj-main-content
  page-title(title='Active Requests'
             hide-on-page=true)
  template(v-if='loaded')
    md-empty-state(v-if='requests.length === 0'
                   md-icon='sentiment_dissatisfied'
                   md-label='No Active Requests'
                   md-description='Your prayer journal has no active requests')
      md-button(to='/journal').md-primary.md-raised Return to your journal
    request-list(v-if='requests.length !== 0'
                 title='Active Requests'
                 :requests='requests')
  p(v-else) Loading journal...
</template>

<script>
'use strict'

import { mapState } from 'vuex'

import RequestList from '@/components/request/RequestList'

import { Actions } from '@/store'

export default {
  name: 'active-requests',
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
    this.$on('requestNowShown', this.ensureJournal)
  },
  methods: {
    async ensureJournal () {
      if (!Array.isArray(this.journal)) {
        this.loaded = false
        await this.$store.dispatch(Actions.LoadJournal, this.progress)
      }
      this.requests = this.journal
        .sort((a, b) => a.showAfter - b.showAfter)
      this.loaded = true
    }
  },
  async mounted () {
    await this.ensureJournal()
  }
}
</script>
