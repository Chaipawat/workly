export interface AuthUser {
  id: string
  email: string
  displayName: string
}

export interface AuthResponse {
  accessToken: string
  accessTokenExpiresAt: string
  user: AuthUser
}

export interface LoginInput {
  email: string
  password: string
}

export interface RegisterInput extends LoginInput {
  displayName: string
}

export interface ApiProblem {
  title?: string
  detail?: string
  errors?: Record<string, string[]>
}
