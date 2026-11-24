<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref } from 'vue'
import { useSaleStore } from '@/stores/sale'
import { formatColones } from '@/utils/format'
import type { ProductVariant } from '@/types/api'

// HU-01: search by name, SKU or barcode. Enter (barcode scanner) adds the
// product directly when the code matches exactly; scanning is not a tap.
defineProps<{ disabled?: boolean }>()

const sale = useSaleStore()
const term = ref('')
const input = ref<HTMLInputElement | null>(null)
let timer: ReturnType<typeof setTimeout> | undefined

const SEARCH_DELAY_MS = 300

function focus() {
  input.value?.focus()
  input.value?.select()
}

function onInput() {
  clearTimeout(timer)
  timer = setTimeout(() => void sale.search(term.value), SEARCH_DELAY_MS)
}

async function onEnter() {
  clearTimeout(timer)
  if (await sale.scan(term.value)) {
    term.value = ''
  }
  focus()
}

async function add(product: ProductVariant) {
  await sale.addProduct(product)
  focus()
}

onMounted(focus)
onBeforeUnmount(() => clearTimeout(timer))

defineExpose({ focus })
</script>

<template>
  <div class="stack">
    <div>
      <label for="product-search">Escanee o busque por nombre, SKU o código de barras</label>
      <input
        id="product-search"
        ref="input"
        v-model="term"
        class="input"
        type="search"
        autocomplete="off"
        enterkeyhint="search"
        placeholder="Ejemplo: Air Force 1 o 7441000000042"
        :disabled="disabled"
        data-testid="product-search"
        @input="onInput"
        @keydown.enter.prevent="onEnter"
      />
    </div>

    <ul v-if="sale.searchResults.length" class="results" data-testid="search-results">
      <li v-for="product in sale.searchResults" :key="product.id">
        <button
          type="button"
          class="result"
          :disabled="disabled || sale.busy"
          :data-testid="`add-${product.sku}`"
          @click="add(product)"
        >
          <span class="result-main">
            <strong>{{ product.name }}</strong>
            <span class="muted">
              Talla {{ product.size }} · {{ product.color }} · {{ product.sku }}
            </span>
          </span>
          <span class="result-side">
            <strong>{{ formatColones(product.unitPrice) }}</strong>
            <span :class="product.stock > 0 ? 'muted' : 'no-stock'">
              {{ product.stock > 0 ? `Disponibles: ${product.stock}` : 'Sin unidades' }}
            </span>
          </span>
        </button>
      </li>
    </ul>
  </div>
</template>

<style scoped>
.results {
  list-style: none;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.result {
  width: 100%;
  min-height: 64px;
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 1rem;
  padding: 0.6rem 1rem;
  text-align: left;
  font: inherit;
  color: inherit;
  background: var(--card-and-section-background);
  border: 2px solid var(--border-color);
  border-radius: var(--radius);
  cursor: pointer;
  touch-action: manipulation;
}

.result:active {
  border-color: var(--primary-color);
}

.result:disabled {
  opacity: 0.5;
}

.result-main,
.result-side {
  display: flex;
  flex-direction: column;
}

.result-side {
  text-align: right;
  white-space: nowrap;
}

.no-stock {
  color: var(--danger-color);
  font-weight: 700;
}
</style>
