/* eslint-disable */

// Vue packages and components
import Vue from 'vue'
import { MdApp,
         MdButton,
         MdCard,
         MdContent,
         MdDatepicker,
         MdDialog,
         MdEmptyState,
         MdField,
         MdIcon,
         MdLayout,
         MdProgress,
         MdRadio,
         MdSnackbar,
         MdTable,
         MdTabs,
         MdToolbar,
         MdTooltip } from 'vue-material/dist/components'

// myPrayerJournal components
import App         from '@/App.vue'
import router      from '@/router'
import store       from '@/store'
import DateFromNow from '@/components/common/DateFromNow.vue'
import PageTitle   from '@/components/common/PageTitle.vue'
import AuthPlugin  from '@/plugins/auth'

/* eslint-enable */

// Styles
import 'vue-material/dist/vue-material.min.css'
import 'vue-material/dist/theme/default.css'

Vue.config.productionTip = false

Vue.use(MdApp)
Vue.use(MdButton)
Vue.use(MdCard)
Vue.use(MdContent)
Vue.use(MdDatepicker)
Vue.use(MdDialog)
Vue.use(MdEmptyState)
Vue.use(MdField)
Vue.use(MdIcon)
Vue.use(MdLayout)
Vue.use(MdProgress)
Vue.use(MdRadio)
Vue.use(MdSnackbar)
Vue.use(MdTable)
Vue.use(MdTabs)
Vue.use(MdToolbar)
Vue.use(MdTooltip)
Vue.use(AuthPlugin)
Vue.component('date-from-now', DateFromNow)
Vue.component('page-title', PageTitle)

new Vue({
  router,
  store,
  render: h => h(App)
}).$mount('#app')
