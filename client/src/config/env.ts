/**
 * Application environment configuration.
 * All env vars used by the app should be read through this module.
 */
export const env = {
  apiBaseUrl: import.meta.env.VITE_API_BASE_URL ?? '/api',
  isDev: import.meta.env.DEV,
} as const
