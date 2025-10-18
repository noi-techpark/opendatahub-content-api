import { createRouter, createWebHistory } from 'vue-router'
import Home from '../views/Home.vue'
import DatasetBrowser from '../views/DatasetBrowser.vue'
import DatasetInspector from '../views/DatasetInspector.vue'
import TimeseriesBrowser from '../views/TimeseriesBrowser.vue'
import TimeseriesInspector from '../views/TimeseriesInspector.vue'
import BulkTimeseriesInspector from '../views/BulkTimeseriesInspector.vue'
import BulkMeasurementsInspector from '../views/BulkMeasurementsInspector.vue'

const routes = [
  {
    path: '/',
    name: 'Home',
    component: Home
  },
  {
    path: '/datasets',
    name: 'DatasetBrowser',
    component: DatasetBrowser
  },
  {
    path: '/datasets/:datasetName',
    name: 'DatasetInspector',
    component: DatasetInspector,
    props: true
  },
  {
    path: '/timeseries',
    name: 'TimeseriesBrowser',
    component: TimeseriesBrowser
  },
  {
    path: '/timeseries/:typeName',
    name: 'TimeseriesInspector',
    component: TimeseriesInspector,
    props: true
  },
  {
    path: '/bulk-timeseries',
    name: 'BulkTimeseriesInspector',
    component: BulkTimeseriesInspector
  },
  {
    path: '/bulk-measurements',
    name: 'BulkMeasurementsInspector',
    component: BulkMeasurementsInspector
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

export default router
