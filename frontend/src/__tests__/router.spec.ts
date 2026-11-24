import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { createMemoryHistory, createRouter } from 'vue-router'
import { requireOpenCash, routes } from '@/router'
import { CASH_REQUIRED_REASON } from '@/constants/routes'
import { createFakeBackend, openSession } from './fakeBackend'

function makeRouter() {
  const router = createRouter({ history: createMemoryHistory(), routes })
  router.beforeEach(requireOpenCash)
  return router
}

beforeEach(() => {
  setActivePinia(createPinia())
})

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('cash guard (HU-09)', () => {
  it('redirects /venta to /caja/abrir with a notice when the cash is closed', async () => {
    createFakeBackend({ session: null })
    const router = makeRouter()

    await router.push('/venta')

    expect(router.currentRoute.value.name).toBe('cash-open')
    expect(router.currentRoute.value.query.motivo).toBe(CASH_REQUIRED_REASON)
  })

  it('also redirects when the API cannot be reached', async () => {
    createFakeBackend({ offline: true })
    const router = makeRouter()

    await router.push('/venta')

    expect(router.currentRoute.value.name).toBe('cash-open')
  })

  it('allows selling when the cash is open', async () => {
    createFakeBackend({ session: openSession() })
    const router = makeRouter()

    await router.push('/venta')

    expect(router.currentRoute.value.name).toBe('sale')
  })

  it('sends "/" and unknown paths to the sale view', async () => {
    createFakeBackend({ session: openSession() })
    const router = makeRouter()

    await router.push('/')
    expect(router.currentRoute.value.name).toBe('sale')

    await router.push('/no-existe')
    expect(router.currentRoute.value.name).toBe('sale')
  })

  it('does not check the cash for the open and close views', async () => {
    const backend = createFakeBackend({ session: null })
    const router = makeRouter()

    await router.push('/caja/cerrar')

    expect(router.currentRoute.value.name).toBe('cash-close')
    expect(backend.callsTo('/api/cash-sessions/current')).toHaveLength(0)
  })

  it('blocks selling after the cash register is closed (HU-10)', async () => {
    const backend = createFakeBackend({ session: openSession() })
    const router = makeRouter()
    await router.push('/venta')

    backend.state.session = null
    await router.push('/caja/cerrar')
    await router.push('/venta')

    expect(router.currentRoute.value.name).toBe('cash-open')
  })
})
