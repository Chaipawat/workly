import type { AuthResponse, AuthUser, LoginInput, RegisterInput } from '~/types/auth'

type ApiRequestOptions = {
  method?: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE'
  body?: object | string | FormData
  query?: Record<string, string | number | boolean | undefined>
}

let refreshRequest: Promise<boolean> | null = null

const getStatus = (error: unknown) => {
  if (!error || typeof error !== 'object') return undefined
  const candidate = error as { status?: number, statusCode?: number, response?: { status?: number } }
  return candidate.statusCode ?? candidate.status ?? candidate.response?.status
}

export const useAuth = () => {
  const config = useRuntimeConfig()
  const accessToken = useState<string | null>('auth:access-token', () => null)
  const accessTokenExpiresAt = useState<string | null>('auth:access-token-expiry', () => null)
  const user = useState<AuthUser | null>('auth:user', () => null)
  const initialized = useState<boolean>('auth:initialized', () => false)

  const apiUrl = (path: string) => `${config.public.apiBase}${path.startsWith('/') ? path : `/${path}`}`

  const applySession = (response: AuthResponse) => {
    accessToken.value = response.accessToken
    accessTokenExpiresAt.value = response.accessTokenExpiresAt
    user.value = response.user
  }

  const clearSession = () => {
    accessToken.value = null
    accessTokenExpiresAt.value = null
    user.value = null
    useState('workspace:organizations', () => []).value = []
    useState<string | null>('workspace:current-id', () => null).value = null
    useState<boolean>('workspace:initialized', () => false).value = false
    if (import.meta.client) localStorage.removeItem('workly:organization-id')
  }

  const refresh = async () => {
    if (refreshRequest) return refreshRequest
    refreshRequest = (async () => {
      try {
        const response = await $fetch<AuthResponse>(apiUrl('/auth/refresh'), {
          method: 'POST',
          credentials: 'include',
        })
        applySession(response)
        return true
      }
      catch {
        clearSession()
        return false
      }
      finally {
        refreshRequest = null
      }
    })()
    return refreshRequest
  }

  const initialize = async () => {
    if (initialized.value) return Boolean(user.value && accessToken.value)
    await refresh()
    initialized.value = true
    return Boolean(user.value && accessToken.value)
  }

  const createSession = async (path: '/auth/login' | '/auth/register', body: LoginInput | RegisterInput) => {
    const response = await $fetch<AuthResponse>(apiUrl(path), {
      method: 'POST',
      body,
      credentials: 'include',
    })
    applySession(response)
    initialized.value = true
    return response.user
  }

  const login = (input: LoginInput) => createSession('/auth/login', input)
  const register = (input: RegisterInput) => createSession('/auth/register', input)

  const logout = async () => {
    try {
      await $fetch(apiUrl('/auth/logout'), { method: 'POST', credentials: 'include' })
    }
    finally {
      clearSession()
      initialized.value = true
    }
  }

  const changePassword = async (currentPassword: string, newPassword: string) => {
    await apiFetch<unknown>('/auth/password', { method: 'PUT', body: { currentPassword, newPassword } })
    clearSession()
  }

  const updateProfile = async (displayName: string) => {
    const updatedUser = await apiFetch<AuthUser>('/auth/profile', { method: 'PUT', body: { displayName } })
    user.value = updatedUser
    return updatedUser
  }

  const apiFetch = async <T>(path: string, options: ApiRequestOptions = {}, retry = true): Promise<T> => {
    try {
      return await $fetch<T>(apiUrl(path), {
        ...options,
        headers: accessToken.value ? { Authorization: `Bearer ${accessToken.value}` } : undefined,
        credentials: 'include',
      })
    }
    catch (error) {
      if (!retry || getStatus(error) !== 401 || !await refresh()) throw error
      return apiFetch<T>(path, options, false)
    }
  }

  return {
    accessToken: readonly(accessToken),
    accessTokenExpiresAt: readonly(accessTokenExpiresAt),
    user: readonly(user),
    initialized: readonly(initialized),
    isAuthenticated: computed(() => Boolean(user.value && accessToken.value)),
    initialize,
    login,
    register,
    logout,
    updateProfile,
    changePassword,
    refresh,
    apiFetch,
  }
}
