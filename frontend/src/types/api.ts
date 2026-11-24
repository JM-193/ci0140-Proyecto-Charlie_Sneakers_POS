// DTOs returned by the C# API (backend/CharliesSneakersPOS.Api/Contracts).
// Amounts are always calculated by the backend; the UI only displays them
// or sends back a value the backend returned (for example "remaining").

export type PaymentMethod = 'Cash' | 'Card' | 'SinpeMobile'

export interface ProductVariant {
  id: string
  sku: string
  name: string
  barcode: string | null
  size: string
  color: string
  unitPrice: number
  stock: number
}

export interface CashSession {
  id: string
  status: 'Open' | 'Closed'
  openingAmount: number
  openedAt: string
  closedAt: string | null
  expectedAmount: number | null
  countedAmount: number | null
}

export interface CashClosing {
  cashSessionId: string
  expected: number
  counted: number
  difference: number
  surplus: number
  shortage: number
  isBalanced: boolean
}

export interface SaleItemRequest {
  productVariantId: string
  quantity: number
}

export interface SalePaymentRequest {
  method: PaymentMethod
  amount: number
}

export interface SalePreview {
  subtotal: number
  tax: number
  total: number
  totalReceived: number
  remaining: number
  change: number
}

export interface RegisterSaleRequest {
  saleId: string
  cashSessionId: string
  items: SaleItemRequest[]
  payments: SalePaymentRequest[]
}

export interface RegisterSaleResponse {
  saleId: string
  subtotal: number
  tax: number
  total: number
  change: number
}
