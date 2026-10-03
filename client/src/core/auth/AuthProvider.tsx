import React, { createContext, useCallback, useContext, useMemo, useState } from 'react'
import type { AuthContextValue } from './auth.types'
import { resolveAppPersona } from './appPersona'
import { getEmailFromAccessToken, getRolesFromAccessToken } from './jwtClaims'

const AuthContext = createContext<AuthContextValue | null>(null)

const TOKEN_ACCESS = 'accessToken'
const TOKEN_REFRESH = 'refreshToken'

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [accessToken, setAccessToken] = useState<string | null>(() =>
    localStorage.getItem(TOKEN_ACCESS),
  )

  const setTokens = useCallback((access: string, refresh: string) => {
    localStorage.setItem(TOKEN_ACCESS, access)
    localStorage.setItem(TOKEN_REFRESH, refresh)
    setAccessToken(access)
  }, [])

  const logout = useCallback(() => {
    localStorage.removeItem(TOKEN_ACCESS)
    localStorage.removeItem(TOKEN_REFRESH)
    setAccessToken(null)
    window.location.href = '/login'
  }, [])

  const roles = useMemo(() => getRolesFromAccessToken(accessToken), [accessToken])
  const appPersona = useMemo(() => resolveAppPersona(roles), [roles])
  const userEmail = useMemo(() => getEmailFromAccessToken(accessToken), [accessToken])

  const value: AuthContextValue = {
    accessToken,
    isAuthenticated: !!accessToken,
    logout,
    setTokens,
    roles,
    appPersona,
    userEmail,
  }

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within AuthProvider')
  return ctx
}
