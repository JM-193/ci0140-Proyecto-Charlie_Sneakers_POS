// Display-only formatting. No amounts are calculated here.
const colones = new Intl.NumberFormat('es-CR', {
  style: 'currency',
  currency: 'CRC',
  minimumFractionDigits: 0,
  maximumFractionDigits: 2,
})

export function formatColones(amount: number | null | undefined): string {
  return amount === null || amount === undefined ? '—' : colones.format(amount)
}

// Parses what the cashier typed ("15000", "15 000", "15000,50").
// Returns null when it is not a number; range and decimals are validated by the API.
export function parseAmount(input: string): number | null {
  const normalized = input.replace(/[\s₡]/g, '').replace(',', '.')
  if (normalized === '' || !/^-?\d+(\.\d+)?$/.test(normalized)) {
    return null
  }
  return Number(normalized)
}
