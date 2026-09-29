import { expect, test } from '@playwright/test'

test('renders the organization dashboard', async ({ page }) => {
  await page.goto('/dashboard')
  await expect(page.getByRole('heading', { level: 1 })).toContainText('Good morning, Ryu')
  await expect(page.getByRole('region', { name: 'Organization snapshot' })).toBeVisible()
  await expect(page.getByRole('heading', { name: 'Active projects' })).toBeVisible()
})

test('exposes a recoverable error state', async ({ page }) => {
  await page.goto('/dashboard?state=error')
  await expect(page.getByRole('alert')).toContainText('couldn’t load your dashboard')
  await expect(page.getByRole('button', { name: 'Try again' })).toBeVisible()
})

test('uses compact navigation on mobile', async ({ page }, testInfo) => {
  test.skip(!testInfo.project.name.includes('mobile'), 'Mobile-only behavior')
  await page.goto('/dashboard')
  await page.getByRole('button', { name: 'Open navigation' }).click()
  await expect(page.getByRole('navigation', { name: 'Primary navigation' })).toBeVisible()
})
