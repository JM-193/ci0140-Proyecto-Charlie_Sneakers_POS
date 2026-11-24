<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useCashSessionStore } from '@/stores/cashSession'
import { formatColones, parseAmount } from '@/utils/format'

// HU-10: count the cash, close the register and show the result of the
// reconciliation exactly as calculated by the backend.
const cash = useCashSessionStore()

const countedAmount = ref('')
const confirming = ref(false)

onMounted(async () => {
  cash.clearError()
  await cash.fetchCurrent()
})

const result = computed(() => {
  const closing = cash.lastClosing
  if (!closing) {
    return null
  }
  if (closing.isBalanced) {
    return { label: 'Cuadrada', css: 'alert-success', amount: null }
  }
  return closing.surplus > 0
    ? { label: 'Sobrante', css: 'alert-info', amount: closing.surplus }
    : { label: 'Faltante', css: 'alert-error', amount: closing.shortage }
})

async function closeCash() {
  if (!confirming.value) {
    confirming.value = true
    return
  }
  confirming.value = false
  if (await cash.close(parseAmount(countedAmount.value))) {
    countedAmount.value = ''
  }
}
</script>

<template>
  <section class="narrow stack">
    <h1>Cerrar y cuadrar caja</h1>

    <div v-if="cash.lastClosing && result" class="card stack" data-testid="closing-result">
      <h2>Resultado del cuadre</h2>
      <div class="amount-row">
        <span>Efectivo esperado</span>
        <strong data-testid="closing-expected">{{
          formatColones(cash.lastClosing.expected)
        }}</strong>
      </div>
      <div class="amount-row">
        <span>Efectivo contado</span>
        <strong data-testid="closing-counted">{{ formatColones(cash.lastClosing.counted) }}</strong>
      </div>
      <div class="amount-row">
        <span>Diferencia</span>
        <strong>{{ formatColones(cash.lastClosing.difference) }}</strong>
      </div>
      <p class="alert" :class="result.css" role="status" data-testid="closing-status">
        {{ result.label
        }}<template v-if="result.amount !== null"> de {{ formatColones(result.amount) }}</template>
      </p>
      <p class="muted">La caja quedó cerrada. Para volver a vender debe abrir una caja nueva.</p>
      <RouterLink :to="{ name: 'cash-open' }" class="btn btn-primary btn-large">
        Abrir caja nueva
      </RouterLink>
    </div>

    <form v-else-if="cash.isOpen && cash.session" class="card stack" @submit.prevent="closeCash">
      <p>
        Monto inicial: <strong>{{ formatColones(cash.session.openingAmount) }}</strong>
      </p>
      <div>
        <label for="counted-amount">Efectivo contado en caja (₡)</label>
        <input
          id="counted-amount"
          v-model="countedAmount"
          class="input"
          type="text"
          inputmode="decimal"
          autocomplete="off"
          placeholder="Ejemplo: 80000"
          data-testid="counted-amount"
          @input="confirming = false"
        />
      </div>

      <p v-if="cash.error" class="alert alert-error" role="alert">{{ cash.error }}</p>
      <p v-if="confirming" class="alert alert-info" role="alert">
        Después de cerrar no se podrá vender hasta abrir otra caja. Presione de nuevo para
        confirmar.
      </p>

      <button
        type="submit"
        class="btn btn-primary btn-large"
        :disabled="cash.busy"
        data-testid="close-cash"
      >
        {{ confirming ? 'Confirmar cierre' : 'Cerrar caja' }}
      </button>
    </form>

    <div v-else class="card stack">
      <p v-if="cash.error" class="alert alert-error" role="alert">{{ cash.error }}</p>
      <p v-else>No hay una caja abierta en este momento.</p>
      <RouterLink :to="{ name: 'cash-open' }" class="btn btn-primary btn-large">
        Abrir caja
      </RouterLink>
    </div>
  </section>
</template>
