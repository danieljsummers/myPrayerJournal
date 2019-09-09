'use strict'

import Vue from 'vue'
import Router from 'vue-router'

import auth from './auth/AuthService'
import Home from '@/components/Home.vue'

Vue.use(Router)


const router = new Router({
  mode: 'history',
  base: process.env.BASE_URL,
  scrollBehavior: (to, from, savedPosition) => savedPosition ? savedPosition: { x: 0, y: 0 },
  routes: [
    {
      path: '/',
      name: 'Home',
      component: Home
    },
    {
      path: '/journal',
      name: 'Journal',
      component: () => import('@/components/Journal.vue')
    },
    {
      path: '/legal/privacy-policy',
      name: 'PrivacyPolicy',
      component: () => import('@/components/legal/PrivacyPolicy.vue')
    },
    {
      path: '/legal/terms-of-service',
      name: 'TermsOfService',
      component: () => import('@/components/legal/TermsOfService.vue')
    },
    {
      path: '/request/:id/edit',
      name: 'EditRequest',
      component: () => import('@/components/request/EditRequest.vue'),
      props: true
    },
    {
      path: '/request/:id/full',
      name: 'FullRequest',
      component: () => import('@/components/request/FullRequest.vue'),
      props: true
    },
    {
      path: '/requests/active',
      name: 'ActiveRequests',
      component: () => import('@/components/request/ActiveRequests.vue')
    },
    {
      path: '/requests/answered',
      name: 'AnsweredRequests',
      component: () => import('@/components/request/AnsweredRequests.vue')
    },
    {
      path: '/requests/snoozed',
      name: 'SnoozedRequests',
      component: () => import('@/components/request/SnoozedRequests.vue')
    },
    {
      path: '/user/log-on',
      name: 'LogOn',
      component: () => import('@/components/user/LogOn.vue')
    }
  ]
})

router.beforeEach((to, from, next) => {
  if (to.path === '/' || to.path === '/user/log-on' || auth.isAuthenticated()) {
    return next()
  }
  auth.login({ target: to.path })
})

export default router
