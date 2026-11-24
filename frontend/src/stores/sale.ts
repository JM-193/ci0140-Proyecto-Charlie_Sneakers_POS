import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import { previewSale, registerSale, searchProducts } from '@/services/posApi'
import { toUserMessage } from '@/services/errorMessages'
import { MESSAGES } from '@/constants/messages'
import type {
  PaymentMethod,
  ProductVariant,
  RegisterSaleResponse,
  SaleItemRequest,
  SalePaymentRequest,
  SalePreview,
} from '@/types/api'

export interface SaleLine {
  product: ProductVariant
  quantity: number
}

function sameCode(a: string | null, b: string): boolean {
  return a !== null && a.trim().toLowerCase() === b.toLowerCase()
}

// HU-01, HU-02, HU-03, HU-05.
// The store only keeps what the cashier entered (lines, quantities, payments).
// Subtotal, tax, total, remaining and change always come from the backend
// (POST /api/sales/preview); a change is applied only if the backend accepts it.
export const useSaleStore = defineStore('sale', () => {
  const saleId = ref(crypto.randomUUID())
  const lines = ref<SaleLine[]>([])
  const payments = ref<SalePaymentRequest[]>([])
  const preview = ref<SalePreview | null>(null)
  const searchResults = ref<ProductVariant[]>([])
  const lastSale = ref<RegisterSaleResponse | null>(null)
  const busy = ref(false)
  const error = ref<string | null>(null)

  const hasItems = computed(() => lines.value.length > 0)
  const isFullyPaid = computed(
    () => payments.value.length > 0 && preview.value !== null && preview.value.remaining === 0,
  )

  // Cart and payment changes run one after another, so fast scans never
  // overwrite each other with stale previews.
  let queue: Promise<unknown> = Promise.resolve()
  function enqueue<T>(task: () => Promise<T>): Promise<T> {
    const run = queue.then(task, task)
    queue = run.catch(() => undefined)
    return run
  }

  // Searches can overlap (typing + scanner); only the latest one is applied.
  let searchToken = 0

  function toItems(source: SaleLine[]): SaleItemRequest[] {
    return source.map((line) => ({ productVariantId: line.product.id, quantity: line.quantity }))
  }

  function clearError() {
    error.value = null
  }

  async function applyCart(nextLines: SaleLine[], changed?: ProductVariant): Promise<boolean> {
    if (nextLines.length === 0) {
      lines.value = []
      payments.value = []
      preview.value = null
      error.value = null
      return true
    }

    busy.value = true
    try {
      preview.value = await previewSale(toItems(nextLines), [])
      lines.value = nextLines
      // The total changed, so previous payments no longer apply.
      payments.value = []
      error.value = null
      return true
    } catch (err) {
      error.value = toUserMessage(err, { availableStock: changed?.stock })
      return false
    } finally {
      busy.value = false
    }
  }

  async function applyPayments(nextPayments: SalePaymentRequest[]): Promise<boolean> {
    busy.value = true
    try {
      preview.value = await previewSale(toItems(lines.value), nextPayments)
      payments.value = nextPayments
      error.value = null
      return true
    } catch (err) {
      error.value = toUserMessage(err)
      return false
    } finally {
      busy.value = false
    }
  }

  function addProduct(product: ProductVariant): Promise<boolean> {
    lastSale.value = null
    return enqueue(() => {
      const exists = lines.value.some((line) => line.product.id === product.id)
      const next = exists
        ? lines.value.map((line) =>
            line.product.id === product.id ? { ...line, quantity: line.quantity + 1 } : line,
          )
        : [...lines.value, { product, quantity: 1 }]
      return applyCart(next, product)
    })
  }

  function changeQuantity(productId: string, quantity: number): Promise<boolean> {
    return enqueue(() => {
      const line = lines.value.find((l) => l.product.id === productId)
      if (!line || quantity < 1) {
        return Promise.resolve(false)
      }
      const next = lines.value.map((l) => (l.product.id === productId ? { ...l, quantity } : l))
      return applyCart(next, line.product)
    })
  }

  function increment(productId: string): Promise<boolean> {
    const line = lines.value.find((l) => l.product.id === productId)
    return line ? changeQuantity(productId, line.quantity + 1) : Promise.resolve(false)
  }

  function decrement(productId: string): Promise<boolean> {
    const line = lines.value.find((l) => l.product.id === productId)
    return line && line.quantity > 1
      ? changeQuantity(productId, line.quantity - 1)
      : Promise.resolve(false)
  }

  function removeLine(productId: string): Promise<boolean> {
    return enqueue(() => applyCart(lines.value.filter((l) => l.product.id !== productId)))
  }

  function addPayment(method: PaymentMethod, amount: number | null): Promise<boolean> {
    if (amount === null) {
      error.value = MESSAGES.invalidPaymentAmount
      return Promise.resolve(false)
    }
    return enqueue(() => applyPayments([...payments.value, { method, amount }]))
  }

  // "Monto exacto": pays exactly what the backend reports as remaining.
  function payExact(method: PaymentMethod): Promise<boolean> {
    return enqueue(() => {
      const remaining = preview.value?.remaining ?? 0
      if (!hasItems.value || remaining <= 0) {
        return Promise.resolve(false)
      }
      return applyPayments([...payments.value, { method, amount: remaining }])
    })
  }

  function removePayment(index: number): Promise<boolean> {
    return enqueue(() => applyPayments(payments.value.filter((_, i) => i !== index)))
  }

  function clearPayments(): Promise<boolean> {
    return payments.value.length === 0 ? Promise.resolve(true) : enqueue(() => applyPayments([]))
  }

  function reset() {
    saleId.value = crypto.randomUUID()
    lines.value = []
    payments.value = []
    preview.value = null
    searchResults.value = []
    error.value = null
  }

  function confirm(cashSessionId: string): Promise<boolean> {
    return enqueue(async () => {
      if (!isFullyPaid.value) {
        error.value = MESSAGES.paymentInsufficient
        return false
      }

      busy.value = true
      try {
        lastSale.value = await registerSale({
          saleId: saleId.value,
          cashSessionId,
          items: toItems(lines.value),
          payments: payments.value,
        })
        reset()
        return true
      } catch (err) {
        error.value = toUserMessage(err)
        return false
      } finally {
        busy.value = false
      }
    })
  }

  async function runSearch(term: string): Promise<ProductVariant[] | null> {
    const token = ++searchToken
    try {
      const results = await searchProducts(term)
      return token === searchToken ? results : null
    } catch (err) {
      if (token === searchToken) {
        error.value = toUserMessage(err)
      }
      return null
    }
  }

  function showResults(results: ProductVariant[]) {
    searchResults.value = results
    error.value = results.length === 0 ? MESSAGES.productNotFound : null
  }

  // Search by name, SKU or barcode while typing.
  async function search(term: string): Promise<void> {
    const trimmed = term.trim()
    if (!trimmed) {
      searchToken++
      searchResults.value = []
      return
    }
    const results = await runSearch(trimmed)
    if (results) {
      showResults(results)
    }
  }

  // Enter in the search box (barcode scanner): adds the product directly
  // when the code matches a barcode or SKU exactly. Returns true if added.
  async function scan(term: string): Promise<boolean> {
    const trimmed = term.trim()
    if (!trimmed) {
      return false
    }
    const results = await runSearch(trimmed)
    if (!results) {
      return false
    }
    const exact = results.find((p) => sameCode(p.barcode, trimmed) || sameCode(p.sku, trimmed))
    if (!exact) {
      showResults(results)
      return false
    }
    searchResults.value = []
    return addProduct(exact)
  }

  return {
    saleId,
    lines,
    payments,
    preview,
    searchResults,
    lastSale,
    busy,
    error,
    hasItems,
    isFullyPaid,
    clearError,
    addProduct,
    changeQuantity,
    increment,
    decrement,
    removeLine,
    addPayment,
    payExact,
    removePayment,
    clearPayments,
    confirm,
    reset,
    search,
    scan,
  }
})
