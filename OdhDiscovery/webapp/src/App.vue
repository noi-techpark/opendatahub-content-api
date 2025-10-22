<template>
  <div id="app">
    <nav class="navbar">
      <div class="container">
        <div class="navbar-content">
          <router-link to="/" class="logo">
            <h1>Open Data Hub Discovery</h1>
          </router-link>
          <div class="nav-links">
            <router-link to="/datasets" class="nav-link">Datasets</router-link>
            <router-link to="/timeseries" class="nav-link">Timeseries</router-link>
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

    <!-- Chatbot Component -->
    <ChatBot />
  </div>
</template>

<script setup>
import ChatBot from './components/ChatBot.vue'

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
  gap: 2rem;
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

.main-content {
  min-height: calc(100vh - 4rem);
  padding: 2rem 0;
  position: relative;
}
</style>
