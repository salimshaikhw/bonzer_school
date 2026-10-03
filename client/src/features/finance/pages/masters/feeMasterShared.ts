import type { CSSProperties } from 'react'
import { financeTheme as t } from '../../ui/financeTheme'

export function apiErrMessage(e: unknown): string {
  if (e && typeof e === 'object' && 'response' in e) {
    const d = (e as { response?: { data?: { message?: string } } }).response?.data?.message
    if (d) return String(d)
  }
  if (e instanceof Error) return e.message
  return 'Request failed.'
}

export function money(n: number | undefined): string {
  if (n == null || !Number.isFinite(n)) return '—'
  return n.toLocaleString(undefined, { minimumFractionDigits: 0, maximumFractionDigits: 2 })
}

export const ms: Record<string, CSSProperties> = {
  card: {
    background: t.surface,
    border: `1px solid ${t.border}`,
    borderRadius: t.radius,
    padding: 20,
    marginBottom: 20,
  },
  h2: { margin: '0 0 8px', fontSize: 18, fontWeight: 700, color: t.text },
  desc: { margin: '0 0 16px', fontSize: 13, color: t.textMuted, lineHeight: 1.5 },
  err: { color: t.danger, fontSize: 14, margin: '0 0 12px' },
  ok: { color: t.success, fontSize: 14, margin: '0 0 12px' },
  formGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(200px, 1fr))',
    gap: 12,
    alignItems: 'end',
    marginBottom: 12,
  },
  lab: { display: 'flex', flexDirection: 'column', gap: 6, fontSize: 13, fontWeight: 600, color: t.textSecondary },
  input: {
    padding: '8px 10px',
    borderRadius: 8,
    border: `1px solid ${t.borderStrong}`,
    fontSize: 14,
    fontFamily: t.fontSans,
  },
  select: {
    padding: '8px 10px',
    borderRadius: 8,
    border: `1px solid ${t.borderStrong}`,
    fontSize: 14,
    fontFamily: t.fontSans,
    background: t.surface,
  },
  tableWrap: { overflowX: 'auto', border: `1px solid ${t.border}`, borderRadius: t.radius, background: t.surface },
  table: { width: '100%', borderCollapse: 'collapse', fontSize: 13 },
  th: {
    textAlign: 'left',
    padding: '10px 12px',
    borderBottom: `1px solid ${t.border}`,
    background: t.bgPage,
    color: t.textSecondary,
    fontWeight: 600,
  },
  td: { padding: '10px 12px', borderBottom: `1px solid ${t.border}`, color: t.text },
  rowActions: { display: 'flex', gap: 8, flexWrap: 'wrap' },
  subNav: {
    display: 'flex',
    flexWrap: 'wrap',
    gap: 4,
    padding: 4,
    background: t.bgPage,
    borderRadius: t.radius,
    width: 'fit-content',
    maxWidth: '100%',
    marginBottom: 8,
  },
  subTab: {
    padding: '8px 14px',
    fontSize: 13,
    fontWeight: 600,
    color: t.textSecondary,
    textDecoration: 'none',
    borderRadius: 8,
    border: '1px solid transparent',
  },
  subTabActive: {
    color: t.text,
    background: t.surface,
    boxShadow: t.shadow,
  },
}
