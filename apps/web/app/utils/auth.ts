import type { ApiProblem } from '~/types/auth'

export const getInitials = (name?: string) => {
  if (!name?.trim()) return '?'
  return name.trim().split(/\s+/).slice(0, 2).map(part => part[0]?.toUpperCase()).join('')
}

export const getAuthErrorMessage = (error: unknown) => {
  if (!error || typeof error !== 'object') return 'Something went wrong. Please try again.'
  const candidate = error as { data?: ApiProblem, status?: number, statusCode?: number }
  const validationMessage = candidate.data?.errors
    ? Object.values(candidate.data.errors).flat()[0]
    : undefined
  if (validationMessage) return validationMessage
  if (candidate.data?.title) return candidate.data.title
  if ((candidate.statusCode ?? candidate.status) === 429) return 'Too many attempts. Please wait a minute and try again.'
  return 'Unable to reach Workly. Check that the API is running and try again.'
}

export const safeAuthRedirect = (value: unknown) => {
  const path = Array.isArray(value) ? value[0] : value
  return typeof path === 'string' && path.startsWith('/') && !path.startsWith('//')
    ? path
    : '/dashboard'
}
