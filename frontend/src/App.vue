<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { RouterLink, RouterView } from 'vue-router'
import { useCashSessionStore } from '@/stores/cashSession'

const cash = useCashSessionStore()

const statusLabel = computed(() => (cash.isOpen ? 'Caja abierta' : 'Caja cerrada'))

onMounted(() => {
  if (!cash.loaded) {
    void cash.fetchCurrent()
  }
})
</script>

<template>
  <header class="app-header">
    <RouterLink :to="{ name: 'sale' }" class="brand">
      <img alt="Logo de Charlie's Sneakers" class="logo-img" src="@/assets/logo.png" />
      <span class="brand-name">Charlie's Sneakers POS</span>
    </RouterLink>

    <nav class="app-nav" aria-label="Navegación principal">
      <RouterLink v-if="!cash.isOpen" :to="{ name: 'cash-open' }">Abrir caja</RouterLink>
      <RouterLink :to="{ name: 'sale' }">Venta</RouterLink>
      <RouterLink :to="{ name: 'cash-close' }">Cerrar caja</RouterLink>
      <span
        class="cash-status"
        :class="cash.isOpen ? 'open' : 'closed'"
        role="status"
        data-testid="cash-status"
      >
        <span aria-hidden="true">{{ cash.isOpen ? '●' : '○' }}</span>
        {{ statusLabel }}
      </span>
    </nav>
  </header>

  <main>
    <RouterView />
  </main>
</template>
