import { expect, test, type Page } from '@playwright/test'
import { dashboardMock } from '../app/data/dashboard.mock'

const session = {
  accessToken: 'test-access-token',
  accessTokenExpiresAt: '2099-01-01T00:00:00Z',
  user: { id: '019a0000-0000-7000-8000-000000000001', email: 'ada@example.com', displayName: 'Ada Lovelace' },
}

const organization = {
  id: '019a0000-0000-7000-8000-000000000002',
  name: 'Acme Studio',
  slug: 'acme-studio',
  role: 'owner',
}

const member = {
  id: '019a0000-0000-7000-8000-000000000003',
  name: 'Ada Lovelace',
  email: 'ada@example.com',
  jobTitle: 'Owner',
  role: 'owner',
  status: 'active',
  departmentId: null,
  departmentName: null,
}

const mockWorkspaceApi = async (page: Page) => {
  await page.route('**/api/organizations', async (route) => {
    if (route.request().method() === 'GET') {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify([organization]) })
      return
    }
    await route.fulfill({ status: 201, contentType: 'application/json', body: JSON.stringify(organization) })
  })
  await page.route('**/api/organizations/*/dashboard*', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify(dashboardMock),
  }))
  await page.route('**/api/organizations/*/people', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify([member]),
  }))
  for (const resource of ['departments', 'projects', 'tasks', 'leave', 'announcements']) {
    await page.route(`**/api/organizations/*/${resource}`, route => route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: '[]',
    }))
  }
}

const mockAuthenticatedSession = async (page: Page) => {
  await page.route('**/api/auth/refresh', route => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(session) }))
  await mockWorkspaceApi(page)
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
  await mockWorkspaceApi(page)
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

test('searches workspace data and opens notifications', async ({ page }, testInfo) => {
  test.skip(testInfo.project.name.includes('mobile'), 'The global search is intentionally compacted on mobile')
  await mockAuthenticatedSession(page)
  await page.goto('/dashboard')
  await page.getByRole('searchbox', { name: 'Search workspace' }).fill('Ada')
  await expect(page.getByRole('option', { name: /Ada Lovelace/ })).toBeVisible()
  await page.getByRole('button', { name: 'Notifications' }).click()
  await expect(page.getByText("You're all caught up.")).toBeVisible()
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

test('renders the people workspace and its member management tools', async ({ page }) => {
  await mockAuthenticatedSession(page)
  await page.goto('/people')
  await expect(page.getByRole('heading', { level: 1, name: 'People' })).toBeVisible()
  await expect(page.getByRole('heading', { name: 'Add an existing Workly user' })).toBeVisible()
  await expect(page.getByRole('row', { name: /Ada Lovelace ada@example\.com/ })).toBeVisible()
})

test('renames the selected workspace from organization settings', async ({ page }) => {
  await mockAuthenticatedSession(page)
  await page.route(`**/api/organizations/${organization.id}`, async (route) => {
    const body = route.request().postDataJSON() as { name: string, slug: string }
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ ...organization, name: body.name, slug: body.slug }),
    })
  })

  await page.goto('/settings/organization')
  await page.getByRole('textbox', { name: 'Name', exact: true }).fill('Northstar Studio')
  await page.getByRole('button', { name: 'Save changes' }).click()

  await expect(page.getByText('Workspace settings updated.')).toBeVisible()
  await expect(page.getByText('Northstar Studio').first()).toBeVisible()
})

test('updates the personal display name from settings', async ({ page }) => {
  await mockAuthenticatedSession(page)
  await page.route('**/api/auth/profile', async (route) => {
    const body = route.request().postDataJSON() as { displayName: string }
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ ...session.user, displayName: body.displayName }),
    })
  })
  await page.goto('/settings/organization')
  await page.getByRole('textbox', { name: 'Display name' }).fill('Ada Byron')
  await page.getByRole('button', { name: 'Save profile' }).click()
  await expect(page.getByText('Profile updated.')).toBeVisible()
  await expect(page.getByText('Ada Byron').first()).toBeVisible()
})
