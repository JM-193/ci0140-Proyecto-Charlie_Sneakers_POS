import { expect, test, type APIRequestContext, type Page } from '@playwright/test'

// M2.1: open cash -> standard sale with 3 scanned products -> close cash,
// counting the taps of the sale. Scanning (typing + Enter) is not a tap.
const MAX_SALE_TAPS = 5

interface Product {
  sku: string
  barcode: string | null
  stock: number
}

// Leaves the register closed so the test always starts from "Abrir caja".
async function closeOpenCash(request: APIRequestContext) {
  const current = await request.get('/api/cash-sessions/current')
  if (current.ok()) {
    const session = (await current.json()) as { id: string }
    await request.post(`/api/cash-sessions/${session.id}/close`, { data: { countedAmount: 0 } })
  }
}

async function productsWithStock(request: APIRequestContext, count: number): Promise<Product[]> {
  const response = await request.get('/api/products?q=PV-')
  const products = ((await response.json()) as Product[]).filter((p) => p.stock > 0)
  expect(products.length, 'run make db-seed first').toBeGreaterThanOrEqual(count)
  return products.slice(0, count)
}

function tapCounter(page: Page) {
  let taps = 0
  return {
    get count() {
      return taps
    },
    async tap(testId: string) {
      taps++
      await page.getByTestId(testId).click()
    },
  }
}

test('standard sale with 3 scanned products is completed in 5 taps or less', async ({
  page,
  request,
}) => {
  await closeOpenCash(request)
  const products = await productsWithStock(request, 3)

  // HU-09: selling with the cash closed redirects to "Abrir caja".
  await page.goto('/venta')
  await expect(page).toHaveURL(/\/caja\/abrir/)
  await expect(page.getByText('Debe abrir la caja antes de vender.')).toBeVisible()
  await page.getByTestId('opening-amount').fill('50000')
  await page.getByTestId('open-cash').click()
  await expect(page).toHaveURL(/\/venta/)
  await expect(page.getByTestId('cash-status')).toContainText('Caja abierta')

  // Scans: not counted as taps.
  const search = page.getByTestId('product-search')
  for (const product of products) {
    await search.fill(product.barcode ?? product.sku)
    await search.press('Enter')
    await expect(search).toHaveValue('')
  }
  await expect(page.getByTestId('cart-lines').locator('li')).toHaveCount(3)

  const user = tapCounter(page)
  await user.tap('charge')
  await user.tap('method-Cash')
  await user.tap('pay-exact')
  await expect(page.getByTestId('pay-remaining')).toContainText('0')
  await user.tap('confirm-sale')
  await expect(page.getByTestId('sale-success')).toBeVisible()

  console.log(`M2.1 taps for the standard sale: ${user.count}`)
  expect(user.count).toBeLessThanOrEqual(MAX_SALE_TAPS)

  // HU-10: close and reconcile, then selling is blocked again.
  await page.getByRole('link', { name: 'Cerrar caja' }).click()
  await page.getByTestId('counted-amount').fill('50000')
  await page.getByTestId('close-cash').click()
  await page.getByTestId('close-cash').click()
  await expect(page.getByTestId('closing-status')).toContainText('Faltante')
  await expect(page.getByTestId('cash-status')).toContainText('Caja cerrada')

  await page.goto('/venta')
  await expect(page).toHaveURL(/\/caja\/abrir/)
})
