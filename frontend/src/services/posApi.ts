import { ApiError, apiGet, apiPost } from '@/services/api'
import type {
  CashClosing,
  CashSession,
  ProductVariant,
  RegisterSaleRequest,
  RegisterSaleResponse,
  SaleItemRequest,
  SalePaymentRequest,
  SalePreview,
} from '@/types/api'

export function searchProducts(term: string): Promise<ProductVariant[]> {
  return apiGet('products.search', `/api/products?q=${encodeURIComponent(term)}`)
}

// 404 means "no open cash session", which is a normal state, not an error.
export async function getCurrentCashSession(): Promise<CashSession | null> {
  try {
    return await apiGet<CashSession>('cash.current', '/api/cash-sessions/current')
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) {
      return null
    }
    throw error
  }
}

export function openCashSession(openingAmount: number): Promise<CashSession> {
  return apiPost('cash.open', '/api/cash-sessions', { openingAmount })
}

export function closeCashSession(id: string, countedAmount: number): Promise<CashClosing> {
  return apiPost('cash.close', `/api/cash-sessions/${encodeURIComponent(id)}/close`, {
    countedAmount,
  })
}

export function previewSale(
  items: SaleItemRequest[],
  payments: SalePaymentRequest[],
): Promise<SalePreview> {
  return apiPost('sales.preview', '/api/sales/preview', { items, payments })
}

export function registerSale(request: RegisterSaleRequest): Promise<RegisterSaleResponse> {
  return apiPost('sales.register', '/api/sales', request)
}
