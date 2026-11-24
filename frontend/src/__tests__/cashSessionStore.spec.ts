import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useCashSessionStore } from '@/stores/cashSession'
import { MESSAGES } from '@/constants/messages'
import { createFakeBackend, openSession } from './fakeBackend'

beforeEach(() => {
  setActivePinia(createPinia())
})

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('useCashSessionStore', () => {
  it('loads the current open session', async () => {
    createFakeBackend({ session: openSession() })
    const cash = useCashSessionStore()

    await cash.fetchCurrent()

    expect(cash.loaded).toBe(true)
    expect(cash.isOpen).toBe(true)
    expect(cash.error).toBeNull()
  })

  it('treats 404 as "cash closed"', async () => {
    createFakeBackend({ session: null })
    const cash = useCashSessionStore()

    await cash.fetchCurrent()

    expect(cash.isOpen).toBe(false)
    expect(cash.error).toBeNull()
  })

  it('shows the network message when the API is down', async () => {
    createFakeBackend({ offline: true })
    const cash = useCashSessionStore()

    await cash.fetchCurrent()

    expect(cash.isOpen).toBe(false)
    expect(cash.error).toBe(MESSAGES.networkError)
  })

  it('opens the cash register with the opening amount', async () => {
    const backend = createFakeBackend()
    const cash = useCashSessionStore()

    await expect(cash.open(50000)).resolves.toBe(true)

    expect(cash.isOpen).toBe(true)
    expect(backend.callsTo('/api/cash-sessions')[0]?.body).toEqual({ openingAmount: 50000 })
  })

  it('does not allow a second open cash register (409)', async () => {
    createFakeBackend({ session: openSession() })
    const cash = useCashSessionStore()

    await expect(cash.open(10000)).resolves.toBe(false)

    expect(cash.error).toBe(MESSAGES.cashAlreadyOpen)
  })

  it('maps an amount rejected by the backend (400)', async () => {
    createFakeBackend()
    const cash = useCashSessionStore()

    await expect(cash.open(0)).resolves.toBe(false)

    expect(cash.error).toBe(MESSAGES.invalidOpeningAmount)
  })

  it('does not call the API when the amount is not a number', async () => {
    const backend = createFakeBackend()
    const cash = useCashSessionStore()

    await expect(cash.open(null)).resolves.toBe(false)

    expect(cash.error).toBe(MESSAGES.invalidOpeningAmount)
    expect(backend.fetchMock).not.toHaveBeenCalled()
  })

  it('closes the cash register and keeps the backend reconciliation', async () => {
    const backend = createFakeBackend({ session: openSession('s-9') })
    const cash = useCashSessionStore()
    await cash.fetchCurrent()

    await expect(cash.close(49700)).resolves.toBe(true)

    expect(backend.callsTo('/api/cash-sessions/s-9/close')[0]?.body).toEqual({
      countedAmount: 49700,
    })
    expect(cash.isOpen).toBe(false)
    expect(cash.lastClosing).toMatchObject({ expected: 50000, shortage: 300, isBalanced: false })
  })

  it('reports a cash register already closed (409) and forgets the session', async () => {
    const backend = createFakeBackend({ session: openSession() })
    const cash = useCashSessionStore()
    await cash.fetchCurrent()
    backend.state.session = null

    await expect(cash.close(50000)).resolves.toBe(false)

    expect(cash.error).toBe(MESSAGES.cashAlreadyClosed)
    expect(cash.isOpen).toBe(false)
  })

  it('cannot close without an open session', async () => {
    createFakeBackend()
    const cash = useCashSessionStore()

    await expect(cash.close(100)).resolves.toBe(false)

    expect(cash.error).toBe(MESSAGES.noOpenCashToClose)
  })

  it('rejects a counted amount that is not a number', async () => {
    createFakeBackend({ session: openSession() })
    const cash = useCashSessionStore()
    await cash.fetchCurrent()

    await expect(cash.close(null)).resolves.toBe(false)

    expect(cash.error).toBe(MESSAGES.invalidCountedAmount)
  })
})
