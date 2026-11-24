import { describe, expect, it } from 'vitest'
import { ApiError, NetworkError, type ApiEndpoint } from '@/services/api'
import { toUserMessage } from '@/services/errorMessages'
import { formatMessage, MESSAGES } from '@/constants/messages'

describe('toUserMessage: HTTP error -> user message', () => {
  const cases: [ApiEndpoint, number, string | undefined, string][] = [
    ['cash.open', 409, undefined, MESSAGES.cashAlreadyOpen],
    ['cash.open', 400, undefined, MESSAGES.invalidOpeningAmount],
    ['cash.close', 400, undefined, MESSAGES.invalidCountedAmount],
    ['cash.close', 404, undefined, MESSAGES.cashSessionNotFound],
    ['cash.close', 409, undefined, MESSAGES.cashAlreadyClosed],
    ['products.search', 400, undefined, MESSAGES.searchTooLong],
    ['sales.register', 400, undefined, MESSAGES.saleRejected],
    ['sales.register', 404, undefined, MESSAGES.productUnavailable],
    ['sales.register', 409, undefined, MESSAGES.saleConflict],
    ['sales.preview', 400, 'invalid_request', MESSAGES.saleTooLarge],
    ['sales.preview', 404, 'product_not_found', MESSAGES.productUnavailable],
    ['sales.preview', 400, 'invalid_quantity', MESSAGES.invalidQuantity],
    ['sales.preview', 400, 'invalid_payment', MESSAGES.invalidPaymentAmount],
    ['sales.preview', 409, 'payment_not_allowed', MESSAGES.paymentNotAllowed],
    ['sales.preview', 409, 'insufficient_stock', MESSAGES.insufficientStockGeneric],
    ['sales.preview', 409, 'something_new', MESSAGES.unexpectedError],
    ['sales.preview', 409, undefined, MESSAGES.unexpectedError],
    ['cash.current', 400, undefined, MESSAGES.unexpectedError],
    ['cash.open', 418, undefined, MESSAGES.unexpectedError],
  ]

  it.each(cases)('%s HTTP %i (%s)', (endpoint, status, code, expected) => {
    expect(toUserMessage(new ApiError(endpoint, status, code))).toBe(expected)
  })

  it('adds the available units to the stock message', () => {
    const error = new ApiError('sales.preview', 409, 'insufficient_stock')
    expect(toUserMessage(error, { availableStock: 2 })).toBe(
      formatMessage(MESSAGES.insufficientStock, { n: 2 }),
    )
  })

  it('uses the out-of-stock message when no units are available', () => {
    const error = new ApiError('sales.preview', 409, 'insufficient_stock')
    expect(toUserMessage(error, { availableStock: 0 })).toBe(MESSAGES.outOfStock)
  })

  it.each([500, 501, 505])('server errors (%i) never show details', (status) => {
    expect(toUserMessage(new ApiError('sales.register', status))).toBe(MESSAGES.serverError)
  })

  it.each([502, 503, 504])('proxy cannot reach the API (%i)', (status) => {
    expect(toUserMessage(new ApiError('cash.current', status))).toBe(MESSAGES.networkError)
  })

  it('maps connection failures to the network message', () => {
    expect(toUserMessage(new NetworkError('cash.open'))).toBe(MESSAGES.networkError)
  })

  it('maps unknown errors to the generic message', () => {
    expect(toUserMessage(new Error('Insufficient stock for this product.'))).toBe(
      MESSAGES.unexpectedError,
    )
    expect(toUserMessage('boom')).toBe(MESSAGES.unexpectedError)
  })

  it('always returns a message from the inventory', () => {
    const allowed = new Set<string>([
      ...Object.values(MESSAGES),
      formatMessage(MESSAGES.insufficientStock, { n: 2 }),
    ])
    for (const [endpoint, status, code] of cases) {
      expect(allowed.has(toUserMessage(new ApiError(endpoint, status, code)))).toBe(true)
    }
  })
})
