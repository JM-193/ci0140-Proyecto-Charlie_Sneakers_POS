<script setup lang="ts">
import { useSaleStore } from '@/stores/sale'
import { formatColones } from '@/utils/format'
import SaleTotals from '@/components/SaleTotals.vue'

// HU-02: change quantities and remove lines before charging.
const emit = defineEmits<{ charge: [] }>()
const sale = useSaleStore()
</script>

<template>
  <div class="stack">
    <h2>Venta actual</h2>

    <p v-if="!sale.hasItems" class="muted">Escanee un producto para empezar la venta.</p>

    <ul v-else class="lines" data-testid="cart-lines">
      <li v-for="line in sale.lines" :key="line.product.id" class="line">
        <div class="line-info">
          <strong>{{ line.product.name }}</strong>
          <span class="muted">
            Talla {{ line.product.size }} · {{ line.product.color }} ·
            {{ formatColones(line.product.unitPrice) }} c/u
          </span>
          <span class="muted">Disponibles: {{ line.product.stock }}</span>
        </div>
        <div class="line-actions">
          <button
            type="button"
            class="btn"
            aria-label="Restar una unidad"
            :disabled="sale.busy || line.quantity <= 1"
            :data-testid="`decrement-${line.product.sku}`"
            @click="sale.decrement(line.product.id)"
          >
            −
          </button>
          <span class="qty" :data-testid="`qty-${line.product.sku}`">{{ line.quantity }}</span>
          <button
            type="button"
            class="btn"
            aria-label="Sumar una unidad"
            :disabled="sale.busy"
            :data-testid="`increment-${line.product.sku}`"
            @click="sale.increment(line.product.id)"
          >
            +
          </button>
          <button
            type="button"
            class="btn btn-danger"
            :disabled="sale.busy"
            :data-testid="`remove-${line.product.sku}`"
            @click="sale.removeLine(line.product.id)"
          >
            Quitar
          </button>
        </div>
      </li>
    </ul>

    <SaleTotals :preview="sale.preview" />

    <button
      type="button"
      class="btn btn-primary btn-large"
      :disabled="!sale.hasItems || sale.busy"
      data-testid="charge"
      @click="emit('charge')"
    >
      Cobrar
    </button>
  </div>
</template>

<style scoped>
.lines {
  list-style: none;
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.line {
  display: flex;
  flex-wrap: wrap;
  justify-content: space-between;
  gap: 0.5rem;
  padding-bottom: 0.75rem;
  border-bottom: 1px solid var(--border-color);
}

.line-info {
  display: flex;
  flex-direction: column;
}

.line-actions {
  display: flex;
  align-items: center;
  gap: 0.4rem;
}

.qty {
  min-width: 2.5rem;
  text-align: center;
  font-size: 1.3rem;
  font-weight: 700;
}
</style>
