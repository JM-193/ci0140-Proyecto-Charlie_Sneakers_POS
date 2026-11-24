import {
  createRouter,
  createWebHistory,
  type RouteLocationNormalized,
  type RouteLocationRaw,
  type RouteRecordRaw,
} from 'vue-router'
import { useCashSessionStore } from '@/stores/cashSession'
import { CASH_REQUIRED_REASON } from '@/constants/routes'
import CashOpenView from '@/views/CashOpenView.vue'
import SaleView from '@/views/SaleView.vue'
import CashCloseView from '@/views/CashCloseView.vue'

declare module 'vue-router' {
  interface RouteMeta {
    requiresOpenCash?: boolean
  }
}

export { CASH_REQUIRED_REASON }

export const routes: RouteRecordRaw[] = [
  { path: '/', redirect: { name: 'sale' } },
  { path: '/caja/abrir', name: 'cash-open', component: CashOpenView },
  { path: '/venta', name: 'sale', component: SaleView, meta: { requiresOpenCash: true } },
  { path: '/caja/cerrar', name: 'cash-close', component: CashCloseView },
  { path: '/:pathMatch(.*)*', redirect: { name: 'sale' } },
]

// HU-09: selling requires an open cash session, as reported by the backend
// (GET /api/cash-sessions/current returns 404 when there is none).
export async function requireOpenCash(
  to: RouteLocationNormalized,
): Promise<true | RouteLocationRaw> {
  if (!to.meta.requiresOpenCash) {
    return true
  }

  const cash = useCashSessionStore()
  await cash.fetchCurrent()

  return cash.isOpen ? true : { name: 'cash-open', query: { motivo: CASH_REQUIRED_REASON } }
}

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes,
})

router.beforeEach(requireOpenCash)

export default router
