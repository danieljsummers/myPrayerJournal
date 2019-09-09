<script lang="ts">
'use strict'

import Vue from 'vue'
import { Component, Prop, Watch } from 'vue-property-decorator'
import moment from 'moment'

@Component({ name: 'date-from-now' })
export default class DateFromNow extends Vue {

  /** The tag used to render the relative date (defaults to "span") */
  @Prop({ default: 'span' })
  tag: string

  /** The ticks of the date to be rendered */
  @Prop({ default: 0 })
  value: number

  /** How frequently to check for updates to this relative date (defaults to 10s) */
  @Prop({ default: 10000 })
  interval: number

  /** The calculated relative date/time */
  fromNow: string = moment(this.value).fromNow()

  /** The ID of the setTimeout interval that updates the value */
  intervalId: number = null

  /** The non-relative date/time */
  get actual () {
    return moment(this.value).format('LLLL')
  }

  mounted () {
    this.intervalId = setInterval(this.updateFromNow, this.interval)
  }
  
  beforeDestroy () {
    clearInterval(this.intervalId)
  }
  
  /** Update the relative date/time */
  @Watch('value')
  updateFromNow () {
    let newFromNow = moment(this.value).fromNow()
    if (newFromNow !== this.fromNow) this.fromNow = newFromNow
  }
  
  render (createElement) {
    return createElement(this.tag, {
      domProps: {
        title: this.actual,
        innerText: this.fromNow
      }
    })
  }
}
</script>
