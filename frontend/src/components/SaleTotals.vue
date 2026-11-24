<script setup lang="ts">
import type { SalePreview } from '@/types/api'
import { formatColones } from '@/utils/format'

// HU-03: amounts calculated by the backend (POST /api/sales/preview).
defineProps<{ preview: SalePreview | null }>()
</script>

<template>
  <div data-testid="sale-totals">
    <div class="amount-row">
      <span>Subtotal</span>
      <span data-testid="subtotal">{{ formatColones(preview?.subtotal ?? 0) }}</span>
    </div>
    <div class="amount-row">
      <span>IVA (13 %)</span>
      <span data-testid="tax">{{ formatColones(preview?.tax ?? 0) }}</span>
    </div>
    <div class="amount-row total">
      <span>Total</span>
      <span data-testid="total">{{ formatColones(preview?.total ?? 0) }}</span>
    </div>
  </div>
</template>
