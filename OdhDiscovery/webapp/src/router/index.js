import { createRouter, createWebHistory } from 'vue-router'
import Home from '../views/Home.vue'
import Login from '../views/Login.vue'
import DatasetBrowser from '../views/DatasetBrowser.vue'
import DatasetInspector from '../views/DatasetInspector.vue'
import TimeseriesBrowser from '../views/TimeseriesBrowser.vue'
import TimeseriesInspector from '../views/TimeseriesInspector.vue'
import BulkMeasurementsInspector from '../views/BulkMeasurementsInspector.vue'
import { useAuthStore } from '../stores/authStore'

const routes = [
  {
    path: '/login',
    name: 'Login',
    component: Login,
    meta: { requiresAuth: false }
  },
  {
    path: '/',
    name: 'Home',
    component: Home,
    meta: { requiresAuth: true }
  },
  {
    path: '/datasets',
    name: 'DatasetBrowser',
    component: DatasetBrowser,
    meta: { requiresAuth: true }
  },
  {
    path: '/datasets/:datasetName',
    name: 'DatasetInspector',
    component: DatasetInspector,
    props: true,
    meta: { requiresAuth: true }
  },
  {
    path: '/timeseries',
    name: 'TimeseriesBrowser',
    component: TimeseriesBrowser,
    meta: { requiresAuth: true }
  },
  {
    path: '/timeseries/:typeName',
    name: 'TimeseriesInspector',
    component: TimeseriesInspector,
    props: true,
    meta: { requiresAuth: true }
  },
  {
    path: '/bulk-measurements',
    name: 'BulkMeasurementsInspector',
    component: BulkMeasurementsInspector,
    meta: { requiresAuth: true }
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

// Navigation guard
router.beforeEach(async (to, from, next) => {
  const authStore = useAuthStore()

  // Check if route requires authentication
  const requiresAuth = to.meta.requiresAuth !== false

  if (requiresAuth) {
    // Check if user has a token
    if (!authStore.isAuthenticated) {
      next({ name: 'Login', query: { redirect: to.fullPath } })
      return
    }

    // Verify token is still valid with backend
    const isValid = await authStore.verifyToken()
    if (!isValid) {
      next({ name: 'Login', query: { redirect: to.fullPath } })
      return
    }
  }

  // If already authenticated and going to login, redirect to home
  if (to.name === 'Login' && authStore.isAuthenticated) {
    next({ name: 'Home' })
    return
  }

  next()
})

export default router
