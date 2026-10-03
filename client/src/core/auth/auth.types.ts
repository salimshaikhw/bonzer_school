/**
 * Auth domain types (core layer).
 */

import type { AppPersona } from './appPersona'

export interface LoginCredentials {
  email: string
  password: string
}

export interface AuthTokens {
  accessToken: string
  refreshToken: string
}

export interface AuthState {
  accessToken: string | null
  isAuthenticated: boolean
}

export interface AuthContextValue extends AuthState {
  logout: () => void
  setTokens: (access: string, refresh: string) => void
  /** Raw Identity role names from the access token. */
  roles: string[]
  /** Derived UX role for SaaS navigation and home layout. */
  appPersona: AppPersona
  /** Email from token when present. */
  userEmail: string | null
}
