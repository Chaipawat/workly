import { expect, test, type Page } from '@playwright/test'

const session = {
  accessToken: 'test-access-token',
  accessTokenExpiresAt: '2099-01-01T00:00:00Z',
  user: { id: '019a0000-0000-7000-8000-000000000001', email: 'ada@example.com', displayName: 'Ada Lovelace' },
}

const mockAuthenticatedSession = async (page: Page) => {
  await page.route('**/api/auth/refresh', route => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(session) }))
}

test('redirects a guest from a protected page to login', async ({ page }) => {
  await page.route('**/api/auth/refresh', route => route.fulfill({ status: 401, contentType: 'application/problem+json', body: '{"title":"Missing refresh session."}' }))
  await page.goto('/dashboard')
  await expect(page).toHaveURL(/\/login\?redirect=\/dashboard/)
  await expect(page.getByRole('heading', { name: 'Sign in to Workly' })).toBeVisible()
})

test('signs in and signs out through the browser flow', async ({ page }) => {
  await page.route('**/api/auth/refresh', route => route.fulfill({ status: 401, contentType: 'application/problem+json', body: '{"title":"Missing refresh session."}' }))
  await page.route('**/api/auth/login', route => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(session) }))
  await page.route('**/api/auth/logout', route => route.fulfill({ status: 204 }))
  await page.goto('/login')
  await page.getByLabel('Email address').fill('ada@example.com')
  await page.getByLabel('Password').fill('a-long-learning-password')
  await page.getByRole('button', { name: 'Sign in' }).click()
  await expect(page).toHaveURL(/\/dashboard$/)
  await expect(page.getByRole('heading', { level: 1 })).toContainText('Ada Lovelace')
  await page.getByRole('button', { name: 'Sign out' }).click()
  await expect(page).toHaveURL(/\/login$/)
})

test('renders the organization dashboard for an authenticated user', async ({ page }) => {
  await mockAuthenticatedSession(page)
  await page.goto('/dashboard')
  await expect(page.getByRole('heading', { level: 1 })).toContainText('Good morning, Ada')
  await expect(page.getByRole('region', { name: 'Organization snapshot' })).toBeVisible()
  await expect(page.getByRole('heading', { name: 'Active projects' })).toBeVisible()
})

test('exposes a recoverable error state', async ({ page }) => {
  await mockAuthenticatedSession(page)
  await page.goto('/dashboard?state=error')
  await expect(page.getByRole('alert')).toContainText('couldn’t load your dashboard')
  await expect(page.getByRole('button', { name: 'Try again' })).toBeVisible()
})

test('uses compact navigation on mobile', async ({ page }, testInfo) => {
  test.skip(!testInfo.project.name.includes('mobile'), 'Mobile-only behavior')
  await mockAuthenticatedSession(page)
  await page.goto('/dashboard')
  await page.getByRole('button', { name: 'Open navigation' }).click()
  await expect(page.getByRole('navigation', { name: 'Primary navigation' })).toBeVisible()
})

test('handles navigation destinations that are not implemented yet', async ({ page }) => {
  await mockAuthenticatedSession(page)
  await page.goto('/people')
  await expect(page.getByRole('heading', { level: 1, name: 'People' })).toBeVisible()
  await expect(page.getByRole('link', { name: 'Back to dashboard' })).toHaveAttribute('href', '/dashboard')
})
