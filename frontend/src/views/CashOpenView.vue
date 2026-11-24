<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useCashSessionStore } from '@/stores/cashSession'
import { MESSAGES } from '@/constants/messages'
import { CASH_REQUIRED_REASON } from '@/constants/routes'
import { formatColones, parseAmount } from '@/utils/format'

// HU-09: open the cash register with an opening amount.
const cash = useCashSessionStore()
const route = useRoute()
const router = useRouter()

const openingAmount = ref('')
const amountInput = ref<HTMLInputElement | null>(null)

const redirectedToOpen = computed(() => route.query.motivo === CASH_REQUIRED_REASON)

onMounted(async () => {
  // Refresh the state (also shows the connection message if the API is down).
  await cash.fetchCurrent()
  amountInput.value?.focus()
})

async function submit() {
  if (await cash.open(parseAmount(openingAmount.value))) {
    openingAmount.value = ''
    await router.push({ name: 'sale' })
  }
}
</script>

<template>
  <section class="narrow stack">
    <h1>Abrir caja</h1>

    <p v-if="redirectedToOpen && !cash.isOpen" class="alert alert-info" role="alert">
      {{ MESSAGES.cashRequiredToSell }}
    </p>

    <div v-if="cash.isOpen && cash.session" class="card stack" data-testid="cash-already-open">
      <p>
        La caja ya está abierta con un monto inicial de
        <strong>{{ formatColones(cash.session.openingAmount) }}</strong>.
      </p>
      <RouterLink :to="{ name: 'sale' }" class="btn btn-primary btn-large">Ir a vender</RouterLink>
    </div>

    <form v-else class="card stack" @submit.prevent="submit">
      <div>
        <label for="opening-amount">Monto inicial en efectivo (₡)</label>
        <input
          id="opening-amount"
          ref="amountInput"
          v-model="openingAmount"
          class="input"
          type="text"
          inputmode="decimal"
          autocomplete="off"
          placeholder="Ejemplo: 50000"
          data-testid="opening-amount"
        />
      </div>

      <p v-if="cash.error" class="alert alert-error" role="alert">{{ cash.error }}</p>

      <button
        type="submit"
        class="btn btn-primary btn-large"
        :disabled="cash.busy"
        data-testid="open-cash"
      >
        Abrir caja
      </button>
    </form>
  </section>
</template>
