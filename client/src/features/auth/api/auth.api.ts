import { apiClient } from '../../../infrastructure/http'
import type { LoginCredentials } from '../../../core/auth/auth.types'
import type { AuthTokens } from '../auth.types'

/** API may return camelCase or PascalCase property names */
function normalizeTokens(data: unknown): AuthTokens {
  if (data == null || typeof data !== 'object') {
    throw new Error('The server returned an empty login response.')
  }
  const d = data as Record<string, unknown>
  const accessToken = d.accessToken ?? d.AccessToken
  const refreshToken = d.refreshToken ?? d.RefreshToken
  if (typeof accessToken !== 'string' || !accessToken) {
    throw new Error('The server response is missing accessToken.')
  }
  if (typeof refreshToken !== 'string' || !refreshToken) {
    throw new Error('The server response is missing refreshToken.')
  }
  return { accessToken, refreshToken }
}

export const authApi = {
  login: async (credentials: LoginCredentials): Promise<AuthTokens> => {
    const { data } = await apiClient.post<unknown>('/Auth/login', credentials)
    return normalizeTokens(data)
  },

  refresh: async (refreshToken: string): Promise<AuthTokens> => {
    const { data } = await apiClient.post<unknown>('/Auth/refresh', { refreshToken })
    return normalizeTokens(data)
  },

  revoke: async (refreshToken: string): Promise<void> => {
    await apiClient.post('/Auth/revoke', { refreshToken })
  },
}
