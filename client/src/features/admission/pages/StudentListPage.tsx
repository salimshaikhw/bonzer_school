import type { CSSProperties } from 'react'
import { useCallback, useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { ROUTES } from '../../../config/routes'
import { Button, Spinner } from '../../../shared'
import { useOrgHierarchy } from '../../../layouts/OrgHierarchyProvider'
import type { Student } from '../students.types'
import { AdmissionOrgScopeBanner } from '../components/AdmissionOrgScopeBanner'
import { canStartNewAdmission, loadStudentsForOrgScope } from '../utils/loadStudentsForOrgTree'

function studentDisplayName(s: { firstName?: string; middleName?: string; lastName?: string }) {
  return [s.firstName, s.middleName, s.lastName].filter(Boolean).join(' ') || '—'
}

const PAGE_OPTIONS = [10, 20, 50, 100] as const

export function StudentListPage() {
  const org = useOrgHierarchy()
  const [allRows, setAllRows] = useState<Student[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(20)

  const scopeKey = `${org.groupScopeId}|${org.branchId}|${org.classId}|${org.divisionId}`

  const load = useCallback(() => {
    if ((!org.branchId.trim() && !org.groupScopeId.trim()) || !org.ready || org.loading) {
      setAllRows([])
      setError(null)
      return
    }
    setLoading(true)
    setError(null)
    void loadStudentsForOrgScope(
      {
        groupScopeId: org.groupScopeId,
        branchId: org.branchId,
        classId: org.classId,
        divisionId: org.divisionId,
      },
      org.branches,
    )
      .then((rows: Student[]) => {
        setAllRows(rows)
        setPage(1)
      })
      .catch(() => setError('Failed to load students'))
      .finally(() => setLoading(false))
  }, [org.groupScopeId, org.branchId, org.classId, org.divisionId, org.branches, org.ready, org.loading])

  useEffect(() => {
    load()
  }, [load, scopeKey])

  const totalCount = allRows.length
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize))
  const safePage = Math.min(page, totalPages)
  const students = useMemo(() => {
    const start = (safePage - 1) * pageSize
    return allRows.slice(start, start + pageSize)
  }, [allRows, safePage, pageSize])

  const from = totalCount === 0 ? 0 : (safePage - 1) * pageSize + 1
  const to = Math.min(safePage * pageSize, totalCount)

  const hasScope = !!(org.branchId.trim() || org.groupScopeId.trim())

  if (!org.ready || org.loading) {
    return (
      <p style={styles.muted}>
        <Spinner /> Loading…
      </p>
    )
  }

  if (org.divisionId.trim()) {
    return (
      <div style={styles.wrap}>
        <AdmissionOrgScopeBanner />
        <p style={styles.muted}>
          Student list is not available when a <strong>division</strong> is selected in the organization tree. Select a{' '}
          <strong>class</strong>, <strong>school</strong>, or <strong>group</strong> to browse students, or open{' '}
          <Link to={ROUTES.ADMISSION_ANALYTICS} style={styles.inTextLink}>
            Admission analytics
          </Link>
          .
        </p>
      </div>
    )
  }

  if (!hasScope) {
    return (
      <div style={styles.wrap}>
        <AdmissionOrgScopeBanner />
        <h2 style={styles.h2}>Students</h2>
        <p style={styles.muted}>Select a school group or a school in the organization tree to view students.</p>
      </div>
    )
  }

  if (loading && allRows.length === 0) {
    return (
      <div style={styles.wrap}>
        <AdmissionOrgScopeBanner />
        <p style={styles.muted}>
          <Spinner /> Loading students…
        </p>
      </div>
    )
  }
  if (error && allRows.length === 0)
    return (
      <div style={styles.wrap}>
        <AdmissionOrgScopeBanner />
        <p style={{ color: '#c00' }}>{error}</p>
      </div>
    )

  return (
    <div style={styles.wrap}>
      <AdmissionOrgScopeBanner />
      <div style={styles.header}>
        <h2 style={styles.h2}>Students</h2>
        <div style={styles.actions}>
          <Button variant="secondary" onClick={() => load()} disabled={loading}>
            {loading ? 'Refreshing…' : 'Refresh'}
          </Button>
          {canStartNewAdmission(org) ? (
            <Link to={ROUTES.STUDENTS_NEW}>
              <Button>Add student</Button>
            </Link>
          ) : (
            <Button
              type="button"
              disabled
              title="Pick a school (branch row only — not class or division) to register a student"
            >
              Add student
            </Button>
          )}
        </div>
      </div>

      <div style={styles.tableScroll}>
        <table style={styles.table}>
          <thead>
            <tr>
              <th style={styles.th}>Name</th>
              <th style={styles.th}>Admission no</th>
              <th style={styles.th}>Roll no</th>
              <th style={styles.th}>Status</th>
              <th style={{ ...styles.th, width: 88 }}> </th>
            </tr>
          </thead>
          <tbody>
            {students.map((s, i) => (
              <tr key={s.id} style={{ background: i % 2 === 0 ? '#fff' : '#f8fafc' }}>
                <td style={styles.td}>{studentDisplayName(s)}</td>
                <td style={styles.td}>{s.admissionNo ?? '—'}</td>
                <td style={styles.td}>{s.rollNumber ?? '—'}</td>
                <td style={styles.td}>
                  <span style={styles.statusPill}>{s.studentStatus ?? '—'}</span>
                </td>
                <td style={styles.td}>
                  <Link to={ROUTES.STUDENT_DETAIL(s.id)} style={styles.viewLink}>
                    View
                  </Link>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {students.length === 0 && !loading && <p style={styles.muted}>No students in this scope.</p>}
      {error && allRows.length > 0 && <p style={styles.errorInline}>{error}</p>}

      <div style={styles.footer}>
        <div style={styles.footerLeft}>
          <span style={styles.muted}>
            Showing {from}-{to} of {totalCount}
          </span>
          <label style={styles.pageSizeLabel}>
            Rows per page
            <select
              value={pageSize}
              onChange={(e) => {
                setPageSize(Number(e.target.value))
                setPage(1)
              }}
              style={styles.select}
              disabled={loading}
            >
              {PAGE_OPTIONS.map((n) => (
                <option key={n} value={n}>
                  {n}
                </option>
              ))}
            </select>
          </label>
        </div>
        <div style={styles.pager}>
          <Button variant="secondary" disabled={loading || safePage <= 1} onClick={() => setPage(1)}>
            First
          </Button>
          <Button variant="secondary" disabled={loading || safePage <= 1} onClick={() => setPage((p) => Math.max(1, p - 1))}>
            Previous Page
          </Button>
          <span style={styles.pageIndicator}>
            Page {totalPages === 0 ? 0 : safePage} of {totalPages}
          </span>
          <Button
            variant="secondary"
            disabled={loading || totalPages === 0 || safePage >= totalPages}
            onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
          >
            Next Page
          </Button>
          <Button
            variant="secondary"
            disabled={loading || totalPages === 0 || safePage >= totalPages}
            onClick={() => setPage(totalPages)}
          >
            Last Page
          </Button>
        </div>
      </div>
    </div>
  )
}

const styles: Record<string, CSSProperties> = {
  wrap: { padding: 0 },
  h2: { margin: 0, fontSize: 18, fontWeight: 600, color: '#1e293b' },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 16,
    flexWrap: 'wrap',
    gap: 12,
  },
  actions: { display: 'flex', gap: 8, flexWrap: 'wrap' },
  tableScroll: {
    width: '100%',
    overflowX: 'auto',
    borderRadius: 8,
    border: '1px solid #e2e8f0',
    background: '#fff',
    boxShadow: '0 1px 3px rgba(0,0,0,0.06)',
  },
  table: {
    width: '100%',
    minWidth: 640,
    borderCollapse: 'collapse',
    fontSize: 14,
  },
  th: {
    textAlign: 'left',
    padding: '12px 14px',
    background: '#1e3a5f',
    color: '#fff',
    fontWeight: 600,
    whiteSpace: 'nowrap',
  },
  td: {
    padding: '12px 14px',
    borderBottom: '1px solid #e2e8f0',
    verticalAlign: 'middle',
    color: '#334155',
  },
  viewLink: { color: '#1e3a5f', fontWeight: 600, textDecoration: 'none' },
  statusPill: {
    display: 'inline-block',
    padding: '2px 10px',
    borderRadius: 999,
    background: '#f1f5f9',
    fontSize: 12,
    fontWeight: 500,
    color: '#475569',
  },
  muted: { color: '#64748b', fontSize: 14, lineHeight: 1.55, maxWidth: 640 },
  inTextLink: { color: '#1e3a5f', fontWeight: 700, textDecoration: 'underline' },
  errorInline: { color: '#c00', fontSize: 14, marginTop: 8 },
  footer: {
    marginTop: 20,
    display: 'flex',
    flexWrap: 'wrap',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: 16,
  },
  footerLeft: { display: 'flex', alignItems: 'center', gap: 20, flexWrap: 'wrap' },
  pageSizeLabel: {
    display: 'flex',
    alignItems: 'center',
    gap: 8,
    fontSize: 14,
    color: '#64748b',
  },
  select: {
    padding: '6px 10px',
    borderRadius: 6,
    border: '1px solid #cbd5e1',
    fontSize: 14,
    background: '#fff',
  },
  pager: { display: 'flex', alignItems: 'center', gap: 8, flexWrap: 'wrap' },
  pageIndicator: { fontSize: 14, color: '#334155', padding: '0 8px' },
}
