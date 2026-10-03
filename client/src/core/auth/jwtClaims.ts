/**
 * Read non-sensitive claims from the access JWT (payload only; no signature verification).
 * Used for SaaS UI: role-based navigation and welcome copy.
 */

export function decodeJwtPayload(accessToken: string | null): Record<string, unknown> | null {
  if (!accessToken?.trim()) return null
  try {
    const parts = accessToken.split('.')
    if (parts.length < 2) return null
    let base64 = parts[1].replace(/-/g, '+').replace(/_/g, '/')
    while (base64.length % 4) base64 += '='
    const json = atob(base64)
    return JSON.parse(json) as Record<string, unknown>
  } catch {
    return null
  }
}

function mergeRoleValue(v: unknown, into: Set<string>) {
  if (v == null) return
  if (Array.isArray(v)) {
    for (const x of v) {
      if (x != null && String(x).trim()) into.add(String(x).trim())
    }
  } else if (typeof v === 'string' && v.trim()) {
    into.add(v.trim())
  }
}

export function extractRolesFromPayload(payload: Record<string, unknown> | null): string[] {
  if (!payload) return []
  const out = new Set<string>()
  mergeRoleValue(payload.role, out)
  mergeRoleValue(payload.Role, out)
  for (const k of Object.keys(payload)) {
    if (k.endsWith('/role') || k.toLowerCase().includes('identity/claims/role')) {
      mergeRoleValue(payload[k], out)
    }
  }
  return [...out]
}

export function extractEmailFromPayload(payload: Record<string, unknown> | null): string | null {
  if (!payload) return null
  const raw =
    payload.email ??
    payload.Email ??
    payload.unique_name ??
    payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress']
  return typeof raw === 'string' && raw.trim() ? raw.trim() : null
}

export function getRolesFromAccessToken(accessToken: string | null): string[] {
  return extractRolesFromPayload(decodeJwtPayload(accessToken))
}

export function getEmailFromAccessToken(accessToken: string | null): string | null {
  return extractEmailFromPayload(decodeJwtPayload(accessToken))
}
