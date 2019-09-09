<template lang="pug">
h1(v-if='!hideOnPage'
   v-html='title').md-title
</template>

<script lang="ts">
'use strict'

import Vue from 'vue'
import { Component, Prop, Watch } from 'vue-property-decorator'

/** Update the brower's page title, optionally putting the title on the page */
@Component({ name: 'page-title' })
export default class PageTitle extends Vue {
  
  /** The page title to display */
  @Prop({ required: true })
  title: string

  /** Whether this should be hidden on the page (default false) */
  @Prop({ default: false })
  hideOnPage: boolean

  @Watch('title')
  updateTitle () {
    document.title = `${this.title.replace('&rsquo;', "'")} « myPrayerJournal`
  }
  
  created () {
    this.updateTitle()
  }
}
</script>
