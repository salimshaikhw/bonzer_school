import type { CSSProperties } from 'react'
import { useEffect, useMemo, useState } from 'react'
import { AdmissionOrgScopeBanner } from '../../admission/components/AdmissionOrgScopeBanner'
import { useOrgHierarchy } from '../../../layouts/OrgHierarchyProvider'
import { Spinner } from '../../../shared'
import { examsApi, type ExamRow } from '../api/exams.api'

function fmtDate(s: string | undefined): string {
  if (!s?.trim()) return '—'
  const d = s.slice(0, 10)
  return d || '—'
}

export function ExamsPage() {
  const org = useOrgHierarchy()
  const [rows, setRows] = useState<ExamRow[]>([])
  const [loading, setLoading] = useState(true)
  const [err, setErr] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    setErr(null)
    void examsApi
      .getAll()
      .then((r) => {
        if (!cancelled) setRows(r)
      })
      .catch((e) => {
        if (!cancelled) setErr(e instanceof Error ? e.message : 'Could not load exams.')
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [])

  const filtered = useMemo(() => {
    const bid = org.branchId.trim()
    const cid = org.classId.trim()
    const did = org.divisionId.trim()
    if (!bid && !cid && !did) return rows
    return rows.filter((e) => {
      if (!bid && !cid && !did) return true
      if (did) return (e.divisionId ?? '') === did
      if (cid) return (e.classId ?? '') === cid
      if (bid) return (e.branchId ?? '') === bid || !(e.branchId ?? '').trim()
      return true
    })
  }, [rows, org.branchId, org.classId, org.divisionId])

  const scopeHint = org.branchId.trim()
    ? org.divisionId.trim()
      ? 'Filtered to exams linked to the selected division where data exists.'
      : org.classId.trim()
        ? 'Filtered to the selected class (and matching branch).'
        : 'Filtered to the selected school when exam rows specify a branch.'
    : org.groupScopeId.trim()
      ? 'Pick a school in the tree to narrow exams by branch/class.'
      : 'Select a school (or group then school) to filter this list.'

  return (
    <div style={styles.page}>
      <AdmissionOrgScopeBanner />

      <header style={styles.header}>
        <h1 style={styles.h1}>Exams</h1>
        <p style={styles.sub}>
          Scheduled exams from the catalog ({filtered.length} shown). Marks and analytics use the same exam records.
        </p>
        <p style={styles.hint}>{scopeHint}</p>
      </header>

      {loading ? (
        <div style={styles.spin}>
          <Spinner />
        </div>
      ) : err ? (
        <p style={styles.err}>{err}</p>
      ) : filtered.length === 0 ? (
        <p style={styles.muted}>
          {rows.length === 0
            ? 'No exams in the database yet. Create exams via the API or seed data.'
            : 'No exams match the current tree selection — choose another scope or clear class/division filters.'}
        </p>
      ) : (
        <div style={styles.card}>
          <table style={styles.table}>
            <thead>
              <tr>
                <th style={styles.th}>Name</th>
                <th style={styles.th}>Status</th>
                <th style={styles.th}>Start</th>
                <th style={styles.th}>End</th>
                <th style={styles.th}>Branch / class</th>
              </tr>
            </thead>
            <tbody>
              {filtered.map((e) => (
                <tr key={e.id}>
                  <td style={styles.td}>{e.name ?? e.id.slice(0, 8)}</td>
                  <td style={styles.td}>{e.status ?? '—'}</td>
                  <td style={styles.td}>{fmtDate(e.startDate)}</td>
                  <td style={styles.td}>{fmtDate(e.endDate)}</td>
                  <td style={{ ...styles.td, fontSize: 12, color: '#64748b' }}>
                    {[e.branchId?.slice(0, 8), e.classId?.slice(0, 8), e.divisionId?.slice(0, 8)]
                      .filter(Boolean)
                      .join(' · ') || '—'}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}

const styles: Record<string, CSSProperties> = {
  page: { maxWidth: 960 },
  header: { marginBottom: 20 },
  h1: { margin: '0 0 8px', fontSize: 22, fontWeight: 800, color: '#0f172a' },
  sub: { margin: 0, color: '#64748b', fontSize: 14, lineHeight: 1.45 },
  hint: { margin: '10px 0 0', fontSize: 13, color: '#0369a1', lineHeight: 1.4 },
  spin: { padding: 32, display: 'flex', justifyContent: 'center' },
  err: { color: '#b91c1c', fontSize: 14 },
  muted: { color: '#64748b', fontSize: 14, lineHeight: 1.5 },
  card: {
    background: '#fff',
    border: '1px solid #e2e8f0',
    borderRadius: 12,
    overflow: 'hidden',
  },
  table: { width: '100%', borderCollapse: 'collapse', fontSize: 14 },
  th: {
    textAlign: 'left',
    padding: '12px 14px',
    background: '#f8fafc',
    color: '#64748b',
    fontSize: 11,
    fontWeight: 600,
    textTransform: 'uppercase',
    letterSpacing: '0.05em',
    borderBottom: '1px solid #e2e8f0',
  },
  td: {
    padding: '12px 14px',
    borderBottom: '1px solid #f1f5f9',
    color: '#0f172a',
  },
}
