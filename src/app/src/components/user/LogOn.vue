<template lang="pug">
article.mpj-main-content(role='main')
  pageTitle(title='Logging On')
  p Logging you on...
</template>

<script>
'use strict'

export default {
  name: 'log-on',
  inject: ['progress'],
  async created () {
    this.progress.$emit('show', 'indeterminate')
    await this.$auth.handleAuthentication(this.$store)
  },
  methods: {
    handleLoginEvent (data) {
      this.$router.push(data.state.target || '/journal')
    }
  }
}
</script>
