import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useSaleStore } from '@/stores/sale'
import { formatMessage, MESSAGES } from '@/constants/messages'
import { createFakeBackend, openSession, PRODUCTS } from './fakeBackend'
import type { ProductVariant } from '@/types/api'

const [airForce, samba, nb550, soldOut] = PRODUCTS as [
  ProductVariant,
  ProductVariant,
  ProductVariant,
  ProductVariant,
]

beforeEach(() => {
  setActivePinia(createPinia())
})

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('useSaleStore: cart', () => {
  it('adds a product and shows the totals returned by the backend', async () => {
    const backend = createFakeBackend()
    const sale = useSaleStore()

    await expect(sale.addProduct(airForce)).resolves.toBe(true)

    expect(sale.lines).toEqual([{ product: airForce, quantity: 1 }])
    expect(backend.callsTo('/api/sales/preview')[0]?.body).toEqual({
      items: [{ productVariantId: 'p1', quantity: 1 }],
      payments: [],
    })
    expect(sale.preview).toMatchObject({ subtotal: 65000, tax: 8450, total: 73450 })
  })

  it('adding the same product again increments its quantity', async () => {
    createFakeBackend()
    const sale = useSaleStore()

    await sale.addProduct(airForce)
    await sale.addProduct(airForce)

    expect(sale.lines).toHaveLength(1)
    expect(sale.lines[0]?.quantity).toBe(2)
    expect(sale.preview?.total).toBe(146900)
  })

  it('applies fast consecutive scans in order', async () => {
    const backend = createFakeBackend()
    const sale = useSaleStore()

    await Promise.all([
      sale.addProduct(airForce),
      sale.addProduct(samba),
      sale.addProduct(airForce),
    ])

    expect(sale.lines.map((l) => [l.product.id, l.quantity])).toEqual([
      ['p1', 2],
      ['p2', 1],
    ])
    expect(backend.callsTo('/api/sales/preview')).toHaveLength(3)
  })

  it('increments, decrements and removes lines', async () => {
    createFakeBackend()
    const sale = useSaleStore()
    await sale.addProduct(airForce)
    await sale.addProduct(samba)

    await sale.increment('p1')
    expect(sale.lines[0]?.quantity).toBe(2)

    await sale.decrement('p1')
    expect(sale.lines[0]?.quantity).toBe(1)

    await expect(sale.decrement('p1')).resolves.toBe(false)
    expect(sale.lines[0]?.quantity).toBe(1)

    await sale.removeLine('p1')
    expect(sale.lines.map((l) => l.product.id)).toEqual(['p2'])
    expect(sale.preview?.subtotal).toBe(58000)
  })

  it('removing the last line clears the totals without calling the API', async () => {
    const backend = createFakeBackend()
    const sale = useSaleStore()
    await sale.addProduct(samba)
    const calls = backend.fetchMock.mock.calls.length

    await sale.removeLine('p2')

    expect(sale.hasItems).toBe(false)
    expect(sale.preview).toBeNull()
    expect(backend.fetchMock.mock.calls.length).toBe(calls)
  })

  it('keeps the previous quantity and shows the available stock when it is exceeded', async () => {
    createFakeBackend()
    const sale = useSaleStore()
    await sale.addProduct(nb550)

    await expect(sale.increment('p3')).resolves.toBe(false)

    expect(sale.lines[0]?.quantity).toBe(1)
    expect(sale.error).toBe(formatMessage(MESSAGES.insufficientStock, { n: 1 }))
    expect(sale.preview?.subtotal).toBe(69000)
  })

  it('does not add a product without stock (V-02)', async () => {
    createFakeBackend()
    const sale = useSaleStore()

    await expect(sale.addProduct(soldOut)).resolves.toBe(false)

    expect(sale.lines).toEqual([])
    expect(sale.error).toBe(MESSAGES.outOfStock)
  })

  it('changing the cart discards previous payments', async () => {
    createFakeBackend()
    const sale = useSaleStore()
    await sale.addProduct(samba)
    await sale.addPayment('Card', 1000)

    await sale.addProduct(airForce)

    expect(sale.payments).toEqual([])
    expect(sale.preview?.totalReceived).toBe(0)
  })
})

describe('useSaleStore: search and scanner', () => {
  it('Enter with an exact barcode adds the product directly', async () => {
    createFakeBackend()
    const sale = useSaleStore()

    await expect(sale.scan('7441000000080')).resolves.toBe(true)

    expect(sale.lines[0]?.product.id).toBe('p2')
    expect(sale.searchResults).toEqual([])
  })

  it('Enter with an exact SKU (any case) adds the product directly', async () => {
    createFakeBackend()
    const sale = useSaleStore()

    await expect(sale.scan('  pv-af1-wht-40 ')).resolves.toBe(true)

    expect(sale.lines[0]?.product.id).toBe('p1')
  })

  it('Enter with a partial match shows results instead of adding', async () => {
    createFakeBackend()
    const sale = useSaleStore()

    await expect(sale.scan('Nike')).resolves.toBe(false)

    expect(sale.lines).toEqual([])
    expect(sale.searchResults.map((p) => p.id)).toEqual(['p1'])
  })

  it('shows "product not found" when nothing matches', async () => {
    createFakeBackend()
    const sale = useSaleStore()

    await expect(sale.scan('0000000000000')).resolves.toBe(false)

    expect(sale.error).toBe(MESSAGES.productNotFound)
  })

  it('searches by name while typing and clears with an empty term', async () => {
    createFakeBackend()
    const sale = useSaleStore()

    await sale.search('samba')
    expect(sale.searchResults.map((p) => p.id)).toEqual(['p2'])

    await sale.search('   ')
    expect(sale.searchResults).toEqual([])
  })

  it('maps search failures to a user message', async () => {
    createFakeBackend({ offline: true })
    const sale = useSaleStore()

    await sale.search('samba')

    expect(sale.error).toBe(MESSAGES.networkError)
  })
})

describe('useSaleStore: payment (remaining and change come from the backend)', () => {
  it('"Monto exacto" pays the remaining amount reported by the backend', async () => {
    const backend = createFakeBackend()
    const sale = useSaleStore()
    await sale.addProduct(airForce)

    await sale.payExact('SinpeMobile')

    const lastPreview = backend.callsTo('/api/sales/preview').slice(-1)[0]
    expect(lastPreview?.body).toMatchObject({
      payments: [{ method: 'SinpeMobile', amount: 73450 }],
    })
    expect(sale.preview).toMatchObject({ remaining: 0, change: 0 })
    expect(sale.isFullyPaid).toBe(true)
  })

  it('shows the remaining amount after a partial payment', async () => {
    createFakeBackend()
    const sale = useSaleStore()
    await sale.addProduct(airForce)

    await sale.addPayment('Card', 50000)

    expect(sale.preview).toMatchObject({ totalReceived: 50000, remaining: 23450, change: 0 })
    expect(sale.isFullyPaid).toBe(false)
  })

  it('shows the change for cash payments above the total', async () => {
    createFakeBackend()
    const sale = useSaleStore()
    await sale.addProduct(samba)

    await sale.addPayment('Cash', 70000)

    expect(sale.preview).toMatchObject({ remaining: 0, change: 4460 })
  })

  it('rejects a card payment above the remaining amount', async () => {
    createFakeBackend()
    const sale = useSaleStore()
    await sale.addProduct(samba)

    await expect(sale.addPayment('Card', 100000)).resolves.toBe(false)

    expect(sale.payments).toEqual([])
    expect(sale.error).toBe(MESSAGES.paymentNotAllowed)
  })

  it('rejects an amount that is not a number without calling the API', async () => {
    const backend = createFakeBackend()
    const sale = useSaleStore()
    await sale.addProduct(samba)
    const calls = backend.fetchMock.mock.calls.length

    await expect(sale.addPayment('Cash', null)).resolves.toBe(false)

    expect(sale.error).toBe(MESSAGES.invalidPaymentAmount)
    expect(backend.fetchMock.mock.calls.length).toBe(calls)
  })

  it('removes a payment and clears all payments', async () => {
    createFakeBackend()
    const sale = useSaleStore()
    await sale.addProduct(airForce)
    await sale.addPayment('Card', 10000)
    await sale.addPayment('Cash', 5000)

    await sale.removePayment(0)
    expect(sale.payments).toEqual([{ method: 'Cash', amount: 5000 }])

    await sale.clearPayments()
    expect(sale.payments).toEqual([])
    expect(sale.preview?.remaining).toBe(73450)
  })

  it('"Monto exacto" does nothing when nothing is pending', async () => {
    createFakeBackend()
    const sale = useSaleStore()

    await expect(sale.payExact('Cash')).resolves.toBe(false)
  })
})

describe('useSaleStore: confirm', () => {
  it('registers the sale with a client generated saleId and starts a new one', async () => {
    const backend = createFakeBackend({ session: openSession('s-1') })
    const sale = useSaleStore()
    await sale.addProduct(samba)
    await sale.addPayment('Cash', 70000)
    const saleId = sale.saleId

    await expect(sale.confirm('s-1')).resolves.toBe(true)

    expect(backend.state.registered[0]).toEqual({
      saleId,
      cashSessionId: 's-1',
      items: [{ productVariantId: 'p2', quantity: 1 }],
      payments: [{ method: 'Cash', amount: 70000 }],
    })
    expect(sale.lastSale).toMatchObject({ saleId, change: 4460 })
    expect(sale.saleId).not.toBe(saleId)
    expect(sale.lines).toEqual([])
  })

  it('does not register while there is a pending balance', async () => {
    const backend = createFakeBackend({ session: openSession('s-1') })
    const sale = useSaleStore()
    await sale.addProduct(samba)
    await sale.addPayment('Card', 1000)

    await expect(sale.confirm('s-1')).resolves.toBe(false)

    expect(sale.error).toBe(MESSAGES.paymentInsufficient)
    expect(backend.callsTo('/api/sales')).toHaveLength(0)
  })

  it('shows a conflict message when the cash register was closed meanwhile', async () => {
    const backend = createFakeBackend({ session: openSession('s-1') })
    const sale = useSaleStore()
    await sale.addProduct(samba)
    await sale.payExact('Card')
    backend.state.session = null

    await expect(sale.confirm('s-1')).resolves.toBe(false)

    expect(sale.error).toBe(MESSAGES.saleConflict)
    expect(sale.lines).toHaveLength(1)
  })
})
