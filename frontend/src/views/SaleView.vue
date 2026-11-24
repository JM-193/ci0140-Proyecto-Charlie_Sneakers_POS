<script setup lang="ts">
import { nextTick, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useSaleStore } from '@/stores/sale'
import { useCashSessionStore } from '@/stores/cashSession'
import { formatColones } from '@/utils/format'
import ProductSearch from '@/components/ProductSearch.vue'
import CartPanel from '@/components/CartPanel.vue'
import PaymentPanel from '@/components/PaymentPanel.vue'

// Register a sale (HU-01, 02, 03, 05). Standard sale (M2.1), after scanning:
// Cobrar -> payment method -> Monto exacto (or a bill) -> Confirmar venta = 4 taps.
const sale = useSaleStore()
const cash = useCashSessionStore()
const router = useRouter()

const paying = ref(false)
const search = ref<InstanceType<typeof ProductSearch> | null>(null)

watch(
  () => sale.hasItems,
  (hasItems) => {
    if (!hasItems) {
      paying.value = false
    }
  },
)

async function focusSearch() {
  await nextTick()
  search.value?.focus()
}

function startPayment() {
  sale.clearError()
  paying.value = true
}

async function backToCart() {
  await sale.clearPayments()
  paying.value = false
  await focusSearch()
}

async function confirmSale() {
  if (!cash.session) {
    await router.push({ name: 'cash-open' })
    return
  }
  if (await sale.confirm(cash.session.id)) {
    paying.value = false
    await focusSearch()
  }
}
</script>

<template>
  <div class="sale-layout">
    <div class="sale-messages" aria-live="polite">
      <p v-if="sale.error" class="alert alert-error" role="alert" data-testid="sale-error">
        {{ sale.error }}
      </p>
      <p v-else-if="sale.lastSale" class="alert alert-success" data-testid="sale-success">
        Venta registrada. Total {{ formatColones(sale.lastSale.total) }}.
        <template v-if="sale.lastSale.change > 0">
          Vuelto: {{ formatColones(sale.lastSale.change) }}.
        </template>
      </p>
    </div>

    <section class="card sale-search">
      <h1>Registrar venta</h1>
      <ProductSearch ref="search" :disabled="paying" />
    </section>

    <aside class="card sale-side">
      <CartPanel v-if="!paying" @charge="startPayment" />
      <PaymentPanel v-else @back="backToCart" @confirm="confirmSale" />
    </aside>
  </div>
</template>

<style scoped>
.sale-layout {
  display: grid;
  grid-template-columns: minmax(0, 3fr) minmax(320px, 2fr);
  grid-template-areas:
    'messages messages'
    'search side';
  gap: 1rem;
  align-items: start;
}

.sale-messages {
  grid-area: messages;
}

.sale-messages:empty {
  display: none;
}

.sale-search {
  grid-area: search;
}

.sale-side {
  grid-area: side;
  position: sticky;
  top: 90px;
}

@media (max-width: 860px) {
  .sale-layout {
    grid-template-columns: 1fr;
    grid-template-areas:
      'messages'
      'search'
      'side';
  }

  .sale-side {
    position: static;
  }
}
</style>
