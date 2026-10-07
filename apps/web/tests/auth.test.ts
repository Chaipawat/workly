import { describe, expect, it } from 'vitest'
import { getAuthErrorMessage, getInitials, safeAuthRedirect } from '~/utils/auth'

describe('auth helpers', () => {
  it('creates initials from one or two name parts', () => {
    expect(getInitials('Ada Lovelace')).toBe('AL')
    expect(getInitials('  Prince  ')).toBe('P')
    expect(getInitials()).toBe('?')
  })

  it('only redirects to a local application path', () => {
    expect(safeAuthRedirect('/projects?filter=mine')).toBe('/projects?filter=mine')
    expect(safeAuthRedirect('https://example.com')).toBe('/dashboard')
    expect(safeAuthRedirect('//example.com')).toBe('/dashboard')
  })

  it('reads API problem details and validation errors', () => {
    expect(getAuthErrorMessage({ data: { title: 'Invalid email or password.' } })).toBe('Invalid email or password.')
    expect(getAuthErrorMessage({ data: { errors: { Password: ['Password is too short.'] } } })).toBe('Password is too short.')
    expect(getAuthErrorMessage({ statusCode: 429 })).toContain('Too many attempts')
  })
})
