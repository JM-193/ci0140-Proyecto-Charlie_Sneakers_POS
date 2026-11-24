import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError, NetworkError } from '@/services/api'
import { getCurrentCashSession, previewSale, searchProducts } from '@/services/posApi'
import { createFakeBackend, openSession } from './fakeBackend'

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('api client', () => {
  it('uses same-origin /api paths (Vite proxy) and encodes the search term', async () => {
    const backend = createFakeBackend()
    await searchProducts('air force 1')
    expect(String(backend.fetchMock.mock.calls[0]?.[0])).toBe('/api/products?q=air%20force%201')
  })

  it('returns null when there is no open cash session (404)', async () => {
    createFakeBackend({ session: null })
    await expect(getCurrentCashSession()).resolves.toBeNull()
  })

  it('returns the open cash session', async () => {
    createFakeBackend({ session: openSession('abc') })
    await expect(getCurrentCashSession()).resolves.toMatchObject({ id: 'abc', status: 'Open' })
  })

  it('reads the error code from the response body', async () => {
    createFakeBackend()
    const error = await previewSale([{ productVariantId: 'p3', quantity: 5 }], []).catch(
      (e: unknown) => e,
    )
    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({
      endpoint: 'sales.preview',
      status: 409,
      code: 'insufficient_stock',
    })
  })

  it('ignores bodies that are not JSON', async () => {
    createFakeBackend({ failNext: { path: '/api/products', status: 500 } })
    const error = await searchProducts('x').catch((e: unknown) => e)
    expect(error).toMatchObject({ status: 500, code: undefined })
  })

  it('throws NetworkError when the server cannot be reached', async () => {
    createFakeBackend({ offline: true })
    await expect(searchProducts('x')).rejects.toBeInstanceOf(NetworkError)
  })
})
