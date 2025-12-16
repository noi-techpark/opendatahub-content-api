<template>
  <div id="app">
    <nav class="navbar" v-if="authStore.isAuthenticated">
      <div class="container">
        <div class="navbar-content">
          <router-link to="/" class="logo">
            <h1>Open Data Hub Discovery</h1>
          </router-link>
          <div class="nav-links">
            <router-link to="/datasets" class="nav-link">Datasets</router-link>
            <router-link to="/timeseries" class="nav-link">Timeseries</router-link>
            <span class="nav-divider">|</span>
            <span class="user-info">{{ authStore.user?.username }}</span>
            <button @click="handleLogout" class="nav-link logout-btn">Logout</button>
          </div>
        </div>
      </div>
    </nav>
    <main class="main-content">
      <router-view v-slot="{ Component, route }">
        <transition name="page" mode="out-in">
          <component :is="Component" :key="getRouteKey(route)" />
        </transition>
      </router-view>
    </main>

    <!-- Chatbot Component (only show when authenticated) -->
    <ChatBot v-if="authStore.isAuthenticated" />
  </div>
</template>

<script setup>
import { useRouter } from 'vue-router'
import ChatBot from './components/ChatBot.vue'
import { useAuthStore } from './stores/authStore'

const router = useRouter()
const authStore = useAuthStore()

function handleLogout() {
  authStore.logout()
  router.push('/login')
}

// Get route key - use fullPath only when bot is navigating
function getRouteKey(route) {
  // Check if bot is currently navigating
  if (window.__botNavigating) {
    return route.fullPath
  }
  // For manual navigation, only use path (no query params)
  // This prevents transitions on filter changes
  return route.path
}
</script>

<style>
/* Global page transition styles (must be unscoped) */
.page-enter-active,
.page-leave-active {
  transition: all 0.3s ease;
}

.page-enter-from {
  opacity: 0;
  transform: translateY(20px);
}

.page-enter-to {
  opacity: 1;
  transform: translateY(0);
}

.page-leave-from {
  opacity: 1;
  transform: translateY(0);
}

.page-leave-to {
  opacity: 0;
  transform: translateY(-20px);
}
</style>

<style scoped>
.navbar {
  background: var(--surface-color);
  border-bottom: 1px solid var(--border-color);
  padding: 1rem 0;
  position: sticky;
  top: 0;
  z-index: 100;
  box-shadow: var(--shadow-sm);
}

.navbar-content {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.logo {
  text-decoration: none;
  color: var(--text-primary);
}

.logo h1 {
  font-size: 1.5rem;
  font-weight: 700;
  background: linear-gradient(135deg, var(--primary-color), #7c3aed);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
}

.nav-links {
  display: flex;
  align-items: center;
  gap: 1.5rem;
}

.nav-link {
  text-decoration: none;
  color: var(--text-secondary);
  font-weight: 500;
  transition: color 0.2s;
  position: relative;
}

.nav-link:hover {
  color: var(--primary-color);
}

.nav-link.router-link-active {
  color: var(--primary-color);
}

.nav-link.router-link-active::after {
  content: '';
  position: absolute;
  bottom: -1.125rem;
  left: 0;
  right: 0;
  height: 2px;
  background: var(--primary-color);
}

.nav-divider {
  color: var(--border-color);
}

.user-info {
  color: var(--text-secondary);
  font-size: 0.875rem;
}

.logout-btn {
  background: none;
  border: none;
  cursor: pointer;
  font-family: inherit;
  font-size: inherit;
  padding: 0;
}

.main-content {
  min-height: calc(100vh - 4rem);
  padding: 2rem 0;
  position: relative;
}
</style>
