<template>
  <div class="api-call-chain-snippets">
    <details class="snippets-accordion">
      <summary class="snippets-toggle">
        <span class="toggle-icon">📘</span>
        <span class="toggle-text">{{ title }}</span>
      </summary>

      <div class="snippets-content">
        <p v-if="description" class="snippets-description">
          {{ description }}
        </p>

        <div class="tabs">
          <button
            v-for="lang in availableLanguages"
            :key="lang"
            @click="selectedLanguage = lang"
            class="tab"
            :class="{ active: selectedLanguage === lang }"
          >
            {{ lang }}
          </button>
        </div>

        <div class="code-container">
          <button @click="copyCode" class="copy-btn" :class="{ copied: copied }">
            {{ copied ? '✓ Copied' : 'Copy' }}
          </button>
          <pre><code class="language-code" v-html="highlightedCode"></code></pre>
        </div>
      </div>
    </details>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import Prism from 'prismjs'
import 'prismjs/themes/prism-tomorrow.css'
import 'prismjs/components/prism-python'
import 'prismjs/components/prism-javascript'
import 'prismjs/components/prism-go'

const props = defineProps({
  title: {
    type: String,
    default: 'Show Code Examples - How to Reproduce This API Call Chain'
  },
  description: {
    type: String,
    default: ''
  },
  snippets: {
    type: Object,
    required: true,
    // Expected format: { Python: 'code...', JavaScript: 'code...', Go: 'code...' }
  }
})

const selectedLanguage = ref(Object.keys(props.snippets)[0] || 'Python')
const copied = ref(false)

const availableLanguages = computed(() => Object.keys(props.snippets))

const currentSnippet = computed(() => {
  return props.snippets[selectedLanguage.value] || ''
})

const languageMap = {
  'Python': 'python',
  'JavaScript': 'javascript',
  'Go': 'go'
}

const highlightedCode = computed(() => {
  const code = currentSnippet.value
  if (!code) return ''

  const lang = languageMap[selectedLanguage.value] || 'javascript'
  const grammar = Prism.languages[lang]

  if (!grammar) return Prism.util.encode(code)

  return Prism.highlight(code, grammar, lang)
})

async function copyCode() {
  try {
    await navigator.clipboard.writeText(currentSnippet.value)
    copied.value = true
    setTimeout(() => {
      copied.value = false
    }, 2000)
  } catch (err) {
    console.error('Failed to copy code:', err)
  }
}
</script>

<style scoped>
.api-call-chain-snippets {
  margin: 1rem 0;
}

.snippets-accordion {
  border: 1px solid var(--border-color);
  border-radius: 0.5rem;
  background: white;
  overflow: hidden;
}

.snippets-toggle {
  padding: 1rem;
  cursor: pointer;
  user-select: none;
  display: flex;
  align-items: center;
  gap: 0.75rem;
  font-weight: 600;
  color: var(--text-primary);
  background: var(--bg-color);
  list-style: none;
  transition: background-color 0.2s;
}

.snippets-toggle:hover {
  background: rgba(59, 130, 246, 0.05);
}

.snippets-toggle::-webkit-details-marker {
  display: none;
}

.toggle-icon {
  font-size: 1.25rem;
}

.toggle-text {
  flex: 1;
}

.snippets-content {
  padding: 1.5rem;
  border-top: 1px solid var(--border-color);
}

.snippets-description {
  color: var(--text-secondary);
  font-size: 0.9375rem;
  line-height: 1.6;
  margin-bottom: 1.5rem;
}

.tabs {
  display: flex;
  gap: 0.5rem;
  margin-bottom: 1rem;
  border-bottom: 2px solid var(--border-color);
  padding-bottom: 0;
}

.tab {
  padding: 0.625rem 1.25rem;
  background: transparent;
  border: none;
  border-bottom: 3px solid transparent;
  color: var(--text-secondary);
  font-weight: 600;
  font-size: 0.875rem;
  cursor: pointer;
  transition: all 0.2s;
  margin-bottom: -2px;
}

.tab:hover {
  color: var(--primary-color);
  background: rgba(59, 130, 246, 0.05);
}

.tab.active {
  color: var(--primary-color);
  border-bottom-color: var(--primary-color);
}

.code-container {
  position: relative;
  background: #1e1e1e;
  border-radius: 0.5rem;
  overflow: hidden;
}

.copy-btn {
  position: absolute;
  top: 0.75rem;
  right: 0.75rem;
  padding: 0.5rem 1rem;
  background: rgba(255, 255, 255, 0.1);
  color: white;
  border: 1px solid rgba(255, 255, 255, 0.2);
  border-radius: 0.375rem;
  font-size: 0.875rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
  z-index: 10;
}

.copy-btn:hover {
  background: rgba(255, 255, 255, 0.2);
  border-color: rgba(255, 255, 255, 0.3);
}

.copy-btn.copied {
  background: #10b981;
  border-color: #10b981;
}

pre {
  margin: 0;
  padding: 3rem 1.5rem 1.5rem 1.5rem;
  overflow-x: auto;
}

code {
  font-family: 'Courier New', Courier, monospace;
  font-size: 0.875rem;
  line-height: 1.6;
  white-space: pre;
}
</style>
