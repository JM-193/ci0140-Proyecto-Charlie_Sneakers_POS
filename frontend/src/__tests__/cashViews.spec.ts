import { afterEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { createMemoryHistory, createRouter } from 'vue-router'
import App from '@/App.vue'
import CashOpenView from '@/views/CashOpenView.vue'
import CashCloseView from '@/views/CashCloseView.vue'
import { requireOpenCash, routes } from '@/router'
import { MESSAGES } from '@/constants/messages'
import { createFakeBackend, openSession } from './fakeBackend'
import type { Component } from 'vue'

async function mountView(component: Component, path: string) {
  const pinia = createPinia()
  setActivePinia(pinia)
  const router = createRouter({ history: createMemoryHistory(), routes })
  router.beforeEach(requireOpenCash)
  await router.push(path)
  const wrapper = mount(component, { global: { plugins: [pinia, router] } })
  await flushPromises()
  return { wrapper, router }
}

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('CashOpenView (HU-09)', () => {
  it('shows the guard notice and opens the cash register', async () => {
    const backend = createFakeBackend()
    const { wrapper, router } = await mountView(CashOpenView, '/venta')

    expect(router.currentRoute.value.name).toBe('cash-open')
    expect(wrapper.text()).toContain(MESSAGES.cashRequiredToSell)

    await wrapper.get('[data-testid="opening-amount"]').setValue('50000')
    await wrapper.get('form').trigger('submit')
    await flushPromises()

    expect(backend.state.session?.openingAmount).toBe(50000)
    expect(router.currentRoute.value.name).toBe('sale')
  })

  it('shows the backend validation as a Spanish message', async () => {
    createFakeBackend()
    const { wrapper } = await mountView(CashOpenView, '/caja/abrir')

    await wrapper.get('[data-testid="opening-amount"]').setValue('0')
    await wrapper.get('form').trigger('submit')
    await flushPromises()

    expect(wrapper.get('[role="alert"]').text()).toBe(MESSAGES.invalidOpeningAmount)
  })

  it('does not offer a second opening when the cash is already open', async () => {
    createFakeBackend({ session: openSession() })
    const { wrapper } = await mountView(CashOpenView, '/caja/abrir')

    expect(wrapper.find('[data-testid="cash-already-open"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="open-cash"]').exists()).toBe(false)
  })
})

describe('CashCloseView (HU-10)', () => {
  async function closeWith(counted: string) {
    createFakeBackend({ session: openSession() })
    const { wrapper } = await mountView(CashCloseView, '/caja/cerrar')

    await wrapper.get('[data-testid="counted-amount"]').setValue(counted)
    await wrapper.get('form').trigger('submit')
    await flushPromises()
    expect(wrapper.text()).toContain('Confirmar cierre')
    await wrapper.get('form').trigger('submit')
    await flushPromises()
    return wrapper
  }

  it.each([
    ['50000', 'Cuadrada'],
    ['50500', 'Sobrante de'],
    ['49700', 'Faltante de'],
  ])('counted %s shows "%s"', async (counted, label) => {
    const wrapper = await closeWith(counted)

    expect(wrapper.get('[data-testid="closing-status"]').text()).toContain(label)
    expect(wrapper.get('[data-testid="closing-expected"]').text()).toMatch(/50.000/)
  })

  it('asks to open a cash register when none is open', async () => {
    createFakeBackend()
    const { wrapper } = await mountView(CashCloseView, '/caja/cerrar')

    expect(wrapper.text()).toContain('No hay una caja abierta')
    expect(wrapper.find('[data-testid="close-cash"]').exists()).toBe(false)
  })
})

describe('App header', () => {
  it.each([
    [openSession(), 'Caja abierta'],
    [null, 'Caja cerrada'],
  ])('shows the cash status', async (session, label) => {
    createFakeBackend({ session })
    const { wrapper } = await mountView(App, '/caja/cerrar')

    expect(wrapper.get('[data-testid="cash-status"]').text()).toContain(label)
    expect(wrapper.text()).toContain("Charlie's Sneakers POS")
  })
})
