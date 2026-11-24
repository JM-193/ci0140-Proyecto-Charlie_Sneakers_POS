<script setup lang="ts">
import { computed, ref } from 'vue'
import { useSaleStore } from '@/stores/sale'
import { formatColones, parseAmount } from '@/utils/format'
import type { PaymentMethod } from '@/types/api'

// HU-05: payment with cash, card or SINPE Móvil. Received, remaining and
// change are the values returned by the backend for the entered payments.
const emit = defineEmits<{ back: []; confirm: [] }>()
const sale = useSaleStore()

const METHODS: { value: PaymentMethod; label: string }[] = [
  { value: 'Cash', label: 'Efectivo' },
  { value: 'Card', label: 'Tarjeta' },
  { value: 'SinpeMobile', label: 'SINPE Móvil' },
]

// Quick buttons for common Costa Rican bills (amount received, not calculated).
const BILLS = [1000, 2000, 5000, 10000, 20000, 50000]

const method = ref<PaymentMethod | null>(null)
const customAmount = ref('')

const preview = computed(() => sale.preview)
const remaining = computed(() => preview.value?.remaining ?? 0)
const hasPending = computed(() => remaining.value > 0)

function methodLabel(value: PaymentMethod): string {
  return METHODS.find((m) => m.value === value)?.label ?? value
}

async function payExact() {
  if (method.value) {
    await sale.payExact(method.value)
  }
}

async function payBill(amount: number) {
  await sale.addPayment('Cash', amount)
}

async function payCustom() {
  if (method.value && (await sale.addPayment(method.value, parseAmount(customAmount.value)))) {
    customAmount.value = ''
  }
}
</script>

<template>
  <div class="stack" data-testid="payment-panel">
    <h2>Cobro</h2>

    <div>
      <div class="amount-row total first">
        <span>Total</span>
        <span data-testid="pay-total">{{ formatColones(preview?.total) }}</span>
      </div>
      <div class="amount-row">
        <span>Pagado</span>
        <span data-testid="pay-received">{{ formatColones(preview?.totalReceived) }}</span>
      </div>
      <div class="amount-row" :class="{ pending: hasPending }">
        <span>Pendiente</span>
        <strong data-testid="pay-remaining">{{ formatColones(preview?.remaining) }}</strong>
      </div>
      <div v-if="(preview?.change ?? 0) > 0" class="amount-row change">
        <span>Vuelto (efectivo)</span>
        <strong data-testid="pay-change">{{ formatColones(preview?.change) }}</strong>
      </div>
    </div>

    <ul v-if="sale.payments.length" class="payments" data-testid="payments">
      <li v-for="(payment, index) in sale.payments" :key="index">
        <span>{{ methodLabel(payment.method) }}: {{ formatColones(payment.amount) }}</span>
        <button
          type="button"
          class="btn btn-danger"
          :disabled="sale.busy"
          :aria-label="`Quitar pago ${index + 1}`"
          @click="sale.removePayment(index)"
        >
          Quitar
        </button>
      </li>
    </ul>

    <template v-if="hasPending">
      <div class="method-grid" role="group" aria-label="Método de pago">
        <button
          v-for="option in METHODS"
          :key="option.value"
          type="button"
          class="btn"
          :class="{ 'btn-selected': method === option.value }"
          :aria-pressed="method === option.value"
          :data-testid="`method-${option.value}`"
          @click="method = option.value"
        >
          {{ option.label }}
        </button>
      </div>

      <template v-if="method">
        <button
          type="button"
          class="btn btn-primary btn-large"
          :disabled="sale.busy"
          data-testid="pay-exact"
          @click="payExact"
        >
          Monto exacto ({{ formatColones(remaining) }})
        </button>

        <div v-if="method === 'Cash'" class="bill-grid" role="group" aria-label="Billetes">
          <button
            v-for="bill in BILLS"
            :key="bill"
            type="button"
            class="btn"
            :disabled="sale.busy"
            :data-testid="`bill-${bill}`"
            @click="payBill(bill)"
          >
            {{ formatColones(bill) }}
          </button>
        </div>

        <form class="custom" @submit.prevent="payCustom">
          <label for="custom-amount">
            {{ method === 'Cash' ? 'Monto recibido (₡)' : 'Monto del pago (₡)' }}
          </label>
          <div class="custom-row">
            <input
              id="custom-amount"
              v-model="customAmount"
              class="input"
              type="text"
              inputmode="decimal"
              autocomplete="off"
              data-testid="custom-amount"
            />
            <button type="submit" class="btn" :disabled="sale.busy" data-testid="add-payment">
              Agregar pago
            </button>
          </div>
        </form>
      </template>
    </template>

    <button
      type="button"
      class="btn btn-primary btn-large"
      :disabled="sale.busy"
      data-testid="confirm-sale"
      @click="emit('confirm')"
    >
      Confirmar venta
    </button>
    <button
      type="button"
      class="btn"
      :disabled="sale.busy"
      data-testid="back-to-cart"
      @click="emit('back')"
    >
      Volver a la venta
    </button>
  </div>
</template>

<style scoped>
.method-grid,
.bill-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 0.5rem;
}

.payments {
  list-style: none;
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
}

.payments li {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 0.5rem;
}

.pending {
  color: var(--danger-color);
}

.change {
  font-size: 1.3rem;
  color: var(--success-color);
}

.custom-row {
  display: flex;
  gap: 0.5rem;
}

.custom-row .btn {
  min-height: 56px;
  flex-shrink: 0;
  padding: 0 1rem;
  white-space: nowrap;
}

.custom-row .input {
  min-width: 0;
}

.amount-row.total.first {
  border-top: none;
  margin-top: 0;
  padding-top: 0;
}
</style>
