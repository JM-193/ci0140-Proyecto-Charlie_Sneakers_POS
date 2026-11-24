import { ApiError, NetworkError } from '@/services/api'
import { formatMessage, MESSAGES } from '@/constants/messages'

// Extra data the UI already has and can add to a message (never from the
// backend error text). For stock errors: units of the product being changed.
export interface ErrorContext {
  availableStock?: number
}

type StatusMap = Partial<Record<number, string>>

// Status -> message for the endpoints whose error body has no code.
const BY_STATUS: Record<string, StatusMap> = {
  'cash.open': { 400: MESSAGES.invalidOpeningAmount, 409: MESSAGES.cashAlreadyOpen },
  'cash.close': {
    400: MESSAGES.invalidCountedAmount,
    404: MESSAGES.cashSessionNotFound,
    409: MESSAGES.cashAlreadyClosed,
  },
  'products.search': { 400: MESSAGES.searchTooLong },
  'sales.register': {
    400: MESSAGES.saleRejected,
    404: MESSAGES.productUnavailable,
    409: MESSAGES.saleConflict,
  },
}

// Codes returned by POST /api/sales/preview (PreviewSaleErrorCodes in the API).
const PREVIEW_BY_CODE: Record<string, string> = {
  invalid_request: MESSAGES.saleTooLarge,
  product_not_found: MESSAGES.productUnavailable,
  invalid_quantity: MESSAGES.invalidQuantity,
  invalid_payment: MESSAGES.invalidPaymentAmount,
  payment_not_allowed: MESSAGES.paymentNotAllowed,
}

function stockMessage(context: ErrorContext): string {
  const available = context.availableStock
  if (available === undefined) {
    return MESSAGES.insufficientStockGeneric
  }
  return available <= 0
    ? MESSAGES.outOfStock
    : formatMessage(MESSAGES.insufficientStock, { n: available })
}

function previewMessage(error: ApiError, context: ErrorContext): string {
  if (error.code === 'insufficient_stock') {
    return stockMessage(context)
  }
  return (error.code && PREVIEW_BY_CODE[error.code]) || MESSAGES.unexpectedError
}

// Converts any error thrown by the API layer into a message from MESSAGES.
export function toUserMessage(error: unknown, context: ErrorContext = {}): string {
  if (error instanceof NetworkError) {
    return MESSAGES.networkError
  }

  if (!(error instanceof ApiError)) {
    return MESSAGES.unexpectedError
  }

  // 502/503/504: the Vite proxy (or a gateway) could not reach the API.
  if (error.status >= 502 && error.status <= 504) {
    return MESSAGES.networkError
  }

  if (error.status >= 500) {
    return MESSAGES.serverError
  }

  if (error.endpoint === 'sales.preview') {
    return previewMessage(error, context)
  }

  return BY_STATUS[error.endpoint]?.[error.status] ?? MESSAGES.unexpectedError
}
