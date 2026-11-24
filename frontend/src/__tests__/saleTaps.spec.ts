import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount, type VueWrapper } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { createMemoryHistory, createRouter } from 'vue-router'
import SaleView from '@/views/SaleView.vue'
import { routes } from '@/router'
import { useCashSessionStore } from '@/stores/cashSession'
import { createFakeBackend, openSession, PRODUCTS } from './fakeBackend'
import type { PaymentMethod } from '@/types/api'

// M2.1: a standard sale (1 to 3 scanned products, one payment method) must be
// completed in 5 taps or less. Scanning (typing the code + Enter) is not a tap.
const MAX_TAPS = 5
const METHODS: PaymentMethod[] = ['Cash', 'Card', 'SinpeMobile']
const PRODUCT_COUNTS = [1, 2, 3]

async function mountSaleView() {
  const pinia = createPinia()
  setActivePinia(pinia)
  const router = createRouter({ history: createMemoryHistory(), routes })
  const cash = useCashSessionStore()
  cash.session = openSession('s-1')
  cash.loaded = true
  await router.push('/caja/abrir')
  const wrapper = mount(SaleView, { global: { plugins: [pinia, router] } })
  await flushPromises()
  return wrapper
}

async function scan(wrapper: VueWrapper, code: string) {
  const input = wrapper.get('[data-testid="product-search"]')
  await input.setValue(code)
  await input.trigger('keydown', { key: 'Enter' })
  await flushPromises()
}

function tapper(wrapper: VueWrapper) {
  let taps = 0
  return {
    get count() {
      return taps
    },
    async tap(testId: string) {
      taps++
      await wrapper.get(`[data-testid="${testId}"]`).trigger('click')
      await flushPromises()
    },
  }
}

beforeEach(() => {
  vi.useRealTimers()
})

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('standard sale taps (M2.1)', () => {
  const scenarios = PRODUCT_COUNTS.flatMap((products) =>
    METHODS.map((method) => [products, method] as const),
  )

  it.each(scenarios)('%i product(s) paid with %s', async (productCount, method) => {
    const backend = createFakeBackend({ session: openSession('s-1') })
    const wrapper = await mountSaleView()
    const user = tapper(wrapper)

    for (const product of PRODUCTS.slice(0, productCount)) {
      await scan(wrapper, product.barcode ?? product.sku)
    }
    expect(wrapper.findAll('[data-testid="cart-lines"] li')).toHaveLength(productCount)

    await user.tap('charge')
    await user.tap(`method-${method}`)
    await user.tap('pay-exact')
    await user.tap('confirm-sale')

    expect(backend.state.registered).toHaveLength(1)
    expect(backend.state.registered[0]?.payments).toHaveLength(1)
    expect(backend.state.registered[0]?.payments[0]?.method).toBe(method)
    expect(wrapper.find('[data-testid="sale-success"]').exists()).toBe(true)
    expect(user.count).toBe(4)
    expect(user.count).toBeLessThanOrEqual(MAX_TAPS)
  })

  it('cash with a quick bill button is also 4 taps and shows the change', async () => {
    const backend = createFakeBackend({ session: openSession('s-1') })
    const wrapper = await mountSaleView()
    const user = tapper(wrapper)

    await scan(wrapper, PRODUCTS[1]?.barcode ?? '')
    await user.tap('charge')
    await user.tap('method-Cash')
    await user.tap('bill-50000')
    expect(wrapper.get('[data-testid="pay-remaining"]').text()).toContain('15')
    await user.tap('bill-20000')
    expect(wrapper.get('[data-testid="pay-change"]').text()).toContain('4')
    await user.tap('confirm-sale')

    expect(backend.state.registered[0]?.payments).toEqual([
      { method: 'Cash', amount: 50000 },
      { method: 'Cash', amount: 20000 },
    ])
    expect(user.count).toBe(5)
  })

  it('shows the not-found message when the scanned code does not exist', async () => {
    createFakeBackend({ session: openSession('s-1') })
    const wrapper = await mountSaleView()

    await scan(wrapper, '0000000000000')

    expect(wrapper.get('[data-testid="sale-error"]').text()).toBe(
      'No se encontró el producto. Revise el código o busque por nombre.',
    )
  })
})
