import { defineConfig, devices } from '@playwright/test'

// E2E test of the standard sale (M2.1). Not part of CI yet.
// Requires the API running against a seeded database:
//   make db-migrate && make db-seed && make run-db   (other terminal)
//   npm run test:e2e
// Browser: installed Microsoft Edge by default (PW_CHANNEL=chrome to use Chrome,
// or PW_CHANNEL= with `npx playwright install chromium` for the bundled one).
const channel = process.env.PW_CHANNEL ?? 'msedge'

export default defineConfig({
  testDir: './e2e',
  fullyParallel: false,
  workers: 1,
  reporter: [['list'], ['html', { open: 'never' }]],
  use: {
    baseURL: 'http://localhost:5173',
    trace: 'retain-on-failure',
    ...devices['Desktop Chrome'],
    channel: channel || undefined,
  },
  webServer: {
    command: 'npm run dev',
    url: 'http://localhost:5173',
    reuseExistingServer: true,
  },
})
