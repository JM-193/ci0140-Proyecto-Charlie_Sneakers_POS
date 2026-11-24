import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import { closeCashSession, getCurrentCashSession, openCashSession } from '@/services/posApi'
import { toUserMessage } from '@/services/errorMessages'
import { ApiError } from '@/services/api'
import { MESSAGES } from '@/constants/messages'
import type { CashClosing, CashSession } from '@/types/api'

// HU-09 / HU-10. The backend decides whether a session can be opened or
// closed and calculates expected amount, difference, surplus and shortage.
export const useCashSessionStore = defineStore('cashSession', () => {
  const session = ref<CashSession | null>(null)
  const lastClosing = ref<CashClosing | null>(null)
  const loaded = ref(false)
  const busy = ref(false)
  const error = ref<string | null>(null)

  const isOpen = computed(() => session.value?.status === 'Open')

  function clearError() {
    error.value = null
  }

  async function fetchCurrent(): Promise<void> {
    try {
      session.value = await getCurrentCashSession()
      error.value = null
    } catch (err) {
      session.value = null
      error.value = toUserMessage(err)
    } finally {
      loaded.value = true
    }
  }

  async function open(openingAmount: number | null): Promise<boolean> {
    if (openingAmount === null) {
      error.value = MESSAGES.invalidOpeningAmount
      return false
    }

    busy.value = true
    try {
      session.value = await openCashSession(openingAmount)
      lastClosing.value = null
      error.value = null
      return true
    } catch (err) {
      error.value = toUserMessage(err)
      return false
    } finally {
      busy.value = false
    }
  }

  async function close(countedAmount: number | null): Promise<boolean> {
    if (!session.value) {
      error.value = MESSAGES.noOpenCashToClose
      return false
    }
    if (countedAmount === null) {
      error.value = MESSAGES.invalidCountedAmount
      return false
    }

    busy.value = true
    try {
      lastClosing.value = await closeCashSession(session.value.id, countedAmount)
      session.value = null
      error.value = null
      return true
    } catch (err) {
      error.value = toUserMessage(err)
      // The session no longer exists or was already closed elsewhere.
      if (err instanceof ApiError && (err.status === 404 || err.status === 409)) {
        session.value = null
      }
      return false
    } finally {
      busy.value = false
    }
  }

  return {
    session,
    lastClosing,
    loaded,
    busy,
    error,
    isOpen,
    clearError,
    fetchCurrent,
    open,
    close,
  }
})
