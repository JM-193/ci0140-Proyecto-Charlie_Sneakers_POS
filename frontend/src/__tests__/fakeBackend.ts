import { vi } from 'vitest'
import type {
  CashClosing,
  CashSession,
  ProductVariant,
  RegisterSaleRequest,
  SaleItemRequest,
  SalePaymentRequest,
  SalePreview,
} from '@/types/api'

// Test double of the C# API used to simulate fetch in unit tests.
// It mimics the HTTP contract (status codes and error codes) only; the real
// business rules are verified by the backend unit and integration tests.

export const PRODUCTS: ProductVariant[] = [
  product('p1', 'PV-AF1-WHT-40', '7441000000042', 'Nike Air Force 1', 65000, 8),
  product('p2', 'PV-SMB-GRN-41', '7441000000080', 'Adidas Samba OG', 58000, 7),
  product('p3', 'PV-NB550-GRY-44', '7441000000097', 'New Balance 550', 69000, 1),
  product('p4', 'PV-AJ1-BRD-43', '7441000000035', 'Air Jordan 1 Bred', 98000, 0),
]

function product(
  id: string,
  sku: string,
  barcode: string,
  name: string,
  unitPrice: number,
  stock: number,
): ProductVariant {
  return { id, sku, barcode, name, size: '42', color: 'Blanco', unitPrice, stock }
}

export interface FakeBackendOptions {
  session?: CashSession | null
  offline?: boolean
  failNext?: { path: string; status: number; body?: unknown }
}

interface Call {
  method: string
  path: string
  body: unknown
}

function json(status: number, body?: unknown): Response {
  return new Response(body === undefined ? null : JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  })
}

export function openSession(id = 'session-1'): CashSession {
  return {
    id,
    status: 'Open',
    openingAmount: 50000,
    openedAt: '2026-11-24T08:00:00Z',
    closedAt: null,
    expectedAmount: null,
    countedAmount: null,
  }
}

export function createFakeBackend(options: FakeBackendOptions = {}) {
  const state = {
    session: options.session ?? null,
    offline: options.offline ?? false,
    failNext: options.failNext,
    calls: [] as Call[],
    registered: [] as RegisterSaleRequest[],
  }

  function previewOf(
    items: SaleItemRequest[],
    payments: SalePaymentRequest[],
  ): { status: number; body: unknown } {
    let subtotal = 0
    for (const item of items) {
      const found = PRODUCTS.find((p) => p.id === item.productVariantId)
      if (!found) return { status: 404, body: { error: 'Not found', code: 'product_not_found' } }
      if (item.quantity < 1) return { status: 400, body: { code: 'invalid_quantity' } }
      if (item.quantity > found.stock) return { status: 409, body: { code: 'insufficient_stock' } }
      subtotal += found.unitPrice * item.quantity
    }
    const tax = Math.round(subtotal * 13) / 100
    const total = subtotal + tax
    let remaining = total
    let received = 0
    let change = 0
    for (const payment of payments) {
      if (payment.amount <= 0) return { status: 400, body: { code: 'invalid_payment' } }
      if (remaining === 0 || (payment.method !== 'Cash' && payment.amount > remaining)) {
        return { status: 409, body: { code: 'payment_not_allowed' } }
      }
      received += payment.amount
      if (payment.amount > remaining) {
        change = payment.amount - remaining
        remaining = 0
      } else {
        remaining -= payment.amount
      }
    }
    const preview: SalePreview = {
      subtotal,
      tax,
      total,
      totalReceived: received,
      remaining,
      change,
    }
    return { status: 200, body: preview }
  }

  function handle(method: string, url: URL, body: unknown): Response {
    const path = url.pathname

    if (method === 'GET' && path === '/api/products') {
      const q = (url.searchParams.get('q') ?? '').toLowerCase()
      return json(
        200,
        PRODUCTS.filter((p) =>
          [p.name, p.sku, p.barcode ?? ''].some((v) => v.toLowerCase().includes(q)),
        ),
      )
    }

    if (method === 'GET' && path === '/api/cash-sessions/current') {
      return state.session ? json(200, state.session) : json(404)
    }

    if (method === 'POST' && path === '/api/cash-sessions') {
      const { openingAmount } = body as { openingAmount: number }
      if (state.session) return json(409, { title: 'Conflict' })
      if (!(openingAmount > 0)) return json(400, { title: 'Invalid request' })
      state.session = { ...openSession(), openingAmount }
      return json(201, state.session)
    }

    const close = /^\/api\/cash-sessions\/([^/]+)\/close$/.exec(path)
    if (method === 'POST' && close) {
      if (!state.session || state.session.id !== close[1]) return json(409, { title: 'Conflict' })
      const { countedAmount } = body as { countedAmount: number }
      const expected = state.session.openingAmount
      const difference = countedAmount - expected
      const closing: CashClosing = {
        cashSessionId: state.session.id,
        expected,
        counted: countedAmount,
        difference,
        surplus: Math.max(difference, 0),
        shortage: Math.max(-difference, 0),
        isBalanced: difference === 0,
      }
      state.session = null
      return json(200, closing)
    }

    if (method === 'POST' && path === '/api/sales/preview') {
      const request = body as { items: SaleItemRequest[]; payments: SalePaymentRequest[] }
      const result = previewOf(request.items, request.payments ?? [])
      return json(result.status, result.body)
    }

    if (method === 'POST' && path === '/api/sales') {
      const request = body as RegisterSaleRequest
      if (!state.session) return json(409, { error: 'The cash register must be open.' })
      const result = previewOf(request.items, request.payments)
      const preview = result.body as SalePreview
      if (result.status !== 200 || preview.remaining !== 0) {
        return json(409, { error: 'The sale has an outstanding balance.' })
      }
      state.registered.push(request)
      return json(200, {
        saleId: request.saleId,
        subtotal: preview.subtotal,
        tax: preview.tax,
        total: preview.total,
        change: preview.change,
      })
    }

    return json(404)
  }

  const fetchMock = vi.fn<typeof fetch>(async (input, init) => {
    const url = new URL(String(input), 'http://localhost')
    const method = init?.method ?? 'GET'
    const body = init?.body ? JSON.parse(String(init.body)) : undefined
    state.calls.push({ method, path: url.pathname, body })

    if (state.offline) {
      throw new TypeError('Failed to fetch')
    }
    if (state.failNext && state.failNext.path === url.pathname) {
      const { status, body: errorBody } = state.failNext
      state.failNext = undefined
      return json(status, errorBody)
    }
    return handle(method, url, body)
  })

  vi.stubGlobal('fetch', fetchMock)

  return {
    state,
    fetchMock,
    callsTo: (path: string) => state.calls.filter((c) => c.path === path),
  }
}
