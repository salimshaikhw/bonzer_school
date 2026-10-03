/**
 * Core layer: app-wide auth and bootstrap.
 * No UI, no API implementation details — only types and context.
 */
export { AuthProvider, useAuth } from './auth/AuthProvider'
export type { AuthContextValue, AuthState, LoginCredentials } from './auth/auth.types'
export type { AppPersona } from './auth/appPersona'
export {
  resolveAppPersona,
  appPersonaLabel,
  appPersonaDescription,
} from './auth/appPersona'
