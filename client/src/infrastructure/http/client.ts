/**
 * Infrastructure: HTTP client (Axios) with base URL and auth interceptor.
 * Single place for all outbound API configuration.
 */
import axios, { type AxiosInstance } from 'axios'
import { env } from '../../config/env'

const TOKEN_ACCESS = 'accessToken'
const TOKEN_REFRESH = 'refreshToken'

export function createApiClient(): AxiosInstance {
  const client = axios.create({
    baseURL: env.apiBaseUrl,
    // Do not set Content-Type globally: with application/json, axios stringifies FormData and file uploads break.
    // JSON requests still get application/json from axios transformRequest for plain object bodies.
  })

  client.interceptors.request.use((config) => {
    const token = localStorage.getItem(TOKEN_ACCESS)
    if (token) config.headers.Authorization = `Bearer ${token}`
    if (typeof FormData !== 'undefined' && config.data instanceof FormData) {
      config.headers.delete('Content-Type')
    }
    return config
  })

  client.interceptors.response.use(
    (res) => res,
    (err) => {
      const status = err.response?.status
      const base = String(err.config?.baseURL ?? '')
      const path = String(err.config?.url ?? '')
      const full = `${base}${path}`.toLowerCase()
      const isAuthLogin = full.includes('auth/login')
      // Failed login returns 401 — do not treat as "session expired" or force a full reload.
      if (status === 401 && !isAuthLogin) {
        localStorage.removeItem(TOKEN_ACCESS)
        localStorage.removeItem(TOKEN_REFRESH)
        const path = window.location.pathname || ''
        if (!path.endsWith('/login') && path !== '/login') {
          window.location.href = '/login'
        }
      }
      return Promise.reject(err)
    }
  )

  return client
}

export const apiClient = createApiClient()
