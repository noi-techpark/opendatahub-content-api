<template>
  <div class="curl-display">
    <div class="curl-header">
      <h3>API Request</h3>
      <button @click="copyCurl" class="btn btn-outline btn-sm">
        {{ copied ? 'Copied!' : 'Copy' }}
      </button>
    </div>
    <div class="code-block">
      <code>{{ curlCommand }}</code>
    </div>
    <a :href="shareUrl" class="share-link" target="_blank" rel="noopener">
      Share this view
    </a>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'

const props = defineProps({
  curlCommand: {
    type: String,
    required: true
  }
})

const copied = ref(false)

const shareUrl = computed(() => {
  return window.location.href
})

async function copyCurl() {
  try {
    await navigator.clipboard.writeText(props.curlCommand)
    copied.value = true
    setTimeout(() => {
      copied.value = false
    }, 2000)
  } catch (err) {
    console.error('Failed to copy:', err)
  }
}
</script>

<style scoped>
.curl-display {
  margin-bottom: 1.5rem;
}

.curl-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 0.5rem;
}

.curl-header h3 {
  font-size: 1rem;
  font-weight: 600;
  color: var(--text-primary);
}

.btn-sm {
  padding: 0.375rem 0.75rem;
  font-size: 0.875rem;
}

.code-block {
  margin-bottom: 0.5rem;
}

.share-link {
  display: inline-block;
  font-size: 0.875rem;
  color: var(--primary-color);
  text-decoration: none;
  margin-top: 0.5rem;
}

.share-link:hover {
  text-decoration: underline;
}
</style>
