import { useCallback } from 'react'
import { useAuth } from '../../../core'
import type { LoginCredentials } from '../../../core/auth/auth.types'
import { authApi } from '../api/auth.api'

export function useLogin() {
  const { setTokens } = useAuth()

  const login = useCallback(
    async (credentials: LoginCredentials) => {
      const tokens = await authApi.login(credentials)
      setTokens(tokens.accessToken, tokens.refreshToken)
    },
    [setTokens]
  )

  return { login }
}
