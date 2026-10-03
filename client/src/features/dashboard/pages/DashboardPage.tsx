import type { CSSProperties } from 'react'
import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { ROUTES } from '../../../config/routes'
import { showFinancialKpisOnHome } from '../../../config/roleAccess'
import { useAuth } from '../../../core'
import { userApi } from '../../user/api/user.api'
import { ORG_TREE_UNGROUPED_KEY, Spinner } from '../../../shared'
import { useOrgHierarchy } from '../../../layouts/OrgHierarchyProvider'
import { attendanceApi } from '../../attendance/api/attendance.api'
import type { AttendanceSummary } from '../../attendance/attendance.types'
import { admissionFeesApi, type BranchFeeDashboardDto } from '../../admission/api/fees.api'
import { studentsApi } from '../../admission/api/students.api'
import { useDashboardData } from '../hooks/useDashboardData'
import { useAdminDashboardData } from '../hooks/useAdminDashboardData'

function examMatchesSelection(
  e: { branchId?: string; classId?: string; divisionId?: string },
  branchId: string,
  classId: string,
  divisionId: string,
): boolean {
  if (!branchId && !classId && !divisionId) return true
  if (divisionId) return (e.divisionId ?? '') === divisionId
  if (classId) return (e.classId ?? '') === classId
  if (branchId) return (e.branchId ?? '') === branchId
  return true
}

type GroupEnrollmentRow = { groupKey: string; name: string; schools: number; students: number }

export function DashboardPage() {
  const { appPersona, userEmail, roles: jwtRoles } = useAuth()
  const { exams, loading: examsLoading, error: examsError } = useDashboardData()
  const org = useOrgHierarchy()
  const adminDash = useAdminDashboardData()
  const [groupEnrollment, setGroupEnrollment] = useState<GroupEnrollmentRow[]>([])
  const [enrollLoading, setEnrollLoading] = useState(false)
  const [feeDash, setFeeDash] = useState<BranchFeeDashboardDto | null>(null)
  const [feeLoading, setFeeLoading] = useState(false)
  const [attMonth, setAttMonth] = useState<AttendanceSummary | null>(null)
  const [attLoading, setAttLoading] = useState(false)
  const [userDirectoryCount, setUserDirectoryCount] = useState<number | null>(null)
  const [userDirectoryLoading, setUserDirectoryLoading] = useState(false)

  const showFees = showFinancialKpisOnHome(appPersona)
  const hasTreeScope = !!(org.groupScopeId.trim() || org.branchId.trim())

  const filteredExams = exams.filter((e) =>
    examMatchesSelection(e, org.branchId, org.classId, org.divisionId),
  )

  const ctxParts: string[] = []
  if (org.groupScopeId.trim() && !org.branchId.trim()) {
    const gName =
      org.groupScopeId === ORG_TREE_UNGROUPED_KEY
        ? 'Schools (no group)'
        : org.schoolGroups.find((g) => String(g.id) === String(org.groupScopeId))?.name ?? 'School group'
    ctxParts.push(`${gName} (whole group)`)
  }
  const br = org.branches.find((b) => b.id === org.branchId)
  if (br?.groupId && org.branchId.trim()) {
    const grp = org.schoolGroups.find((g) => String(g.id) === String(br.groupId))
    if (grp) ctxParts.push(grp.name ?? 'School group')
  }
  if (br) ctxParts.push(br.name ?? 'School')
  const cl = org.classes.find((c) => c.id === org.classId) ?? org.treeClasses.find((c) => c.id === org.classId)
  const dv = org.divisions.find((d) => d.id === org.divisionId) ?? org.treeDivisions.find((d) => d.id === org.divisionId)
  if (cl) ctxParts.push(cl.name ?? 'Class')
  if (dv) ctxParts.push(dv.name ?? 'Division')

  useEffect(() => {
    if (!org.ready || org.loading || org.branches.length === 0 || appPersona === 'parent') {
      setGroupEnrollment([])
      return
    }
    let cancelled = false
    setEnrollLoading(true)
    void (async () => {
      try {
        const counts = await Promise.all(
          org.branches.map((b) =>
            studentsApi.getByBranch(b.id).then((rows) => ({ bid: b.id, n: rows.length })),
          ),
        )
        if (cancelled) return
        const byGroup = new Map<string, { schools: Set<string>; students: number }>()
        for (const b of org.branches) {
          const n = counts.find((c) => c.bid === b.id)?.n ?? 0
          const gid =
            b.groupId != null && String(b.groupId).trim()
              ? String(b.groupId)
              : '__ungrouped__'
          const cur = byGroup.get(gid) ?? { schools: new Set<string>(), students: 0 }
          cur.schools.add(b.id)
          cur.students += n
          byGroup.set(gid, cur)
        }
        const rows: GroupEnrollmentRow[] = [...byGroup.entries()].map(([groupKey, v]) => {
          const name =
            groupKey === '__ungrouped__'
              ? 'Ungrouped schools'
              : org.schoolGroups.find((g) => g.id === groupKey)?.name ?? 'School group'
          return { groupKey, name, schools: v.schools.size, students: v.students }
        })
        setGroupEnrollment(rows.sort((a, b) => b.students - a.students))
      } catch {
        if (!cancelled) setGroupEnrollment([])
      } finally {
        if (!cancelled) setEnrollLoading(false)
      }
    })()
    return () => {
      cancelled = true
    }
  }, [org.ready, org.loading, org.branches, org.schoolGroups, appPersona])

  useEffect(() => {
    if (!org.branchId.trim() || !showFees) {
      setFeeDash(null)
      setAttMonth(null)
      return
    }
    let cancelled = false
    setFeeLoading(true)
    setAttLoading(true)
    void (async () => {
      try {
        const rows = await studentsApi.getByBranch(org.branchId)
        if (cancelled) return
        const ay = rows.map((s) => s.academicYearId).find((id) => id?.trim())
        if (ay?.trim()) {
          const f = await admissionFeesApi.getBranchDashboard(org.branchId, ay.trim())
          if (!cancelled) setFeeDash(f)
        } else {
          setFeeDash(null)
        }
      } catch {
        if (!cancelled) setFeeDash(null)
      } finally {
        if (!cancelled) setFeeLoading(false)
      }
    })()
    void (async () => {
      try {
        const a = await attendanceApi.getBranchSummary(org.branchId, 'Month')
        if (!cancelled) setAttMonth(a)
      } catch {
        if (!cancelled) setAttMonth(null)
      } finally {
        if (!cancelled) setAttLoading(false)
      }
    })()
    return () => {
      cancelled = true
    }
  }, [org.branchId, showFees])

  useEffect(() => {
    if (appPersona !== 'admin') {
      setUserDirectoryCount(null)
      return
    }
    let cancelled = false
    setUserDirectoryLoading(true)
    userApi
      .getAll()
      .then((rows) => {
        if (!cancelled) setUserDirectoryCount(rows.length)
      })
      .catch(() => {
        if (!cancelled) setUserDirectoryCount(null)
      })
      .finally(() => {
        if (!cancelled) setUserDirectoryLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [appPersona])

  const staffLike = appPersona !== 'parent'
  const scopedStudents = staffLike ? adminDash.totalStudents : null
  const todayAdm = staffLike ? adminDash.todayAdmissions : null
  const adminLoading = staffLike && adminDash.hasOrgScope ? adminDash.loading : false

  const roleLinks = useMemo(() => {
    switch (appPersona) {
      case 'admin':
        return [
          { to: ROUTES.ADMIN_DASHBOARD, label: 'Admin & org' },
          { to: ROUTES.ADMISSION_ANALYTICS, label: 'Admission analytics' },
          { to: ROUTES.EXAMS, label: 'Exams' },
          { to: ROUTES.FINANCE_ANALYTICS, label: 'Finance & fees' },
          { to: ROUTES.USERS, label: 'Users & roles' },
          { to: ROUTES.ATTENDANCE, label: 'Attendance' },
        ]
      case 'principal':
        return [
          { to: ROUTES.ADMIN_DASHBOARD, label: 'School dashboard' },
          { to: ROUTES.ADMISSION_ANALYTICS, label: 'Admission analytics' },
          { to: ROUTES.EXAMS, label: 'Exams' },
          { to: ROUTES.FINANCE_ANALYTICS, label: 'Finance & fees' },
          { to: ROUTES.ATTENDANCE, label: 'Attendance' },
        ]
      case 'class_teacher':
        return [
          { to: ROUTES.ADMISSION_ANALYTICS, label: 'Admission analytics' },
          { to: ROUTES.EXAMS, label: 'Exams' },
          { to: ROUTES.FINANCE_ANALYTICS, label: 'Finance & fees' },
          { to: ROUTES.STUDENTS, label: 'Students' },
          { to: ROUTES.ATTENDANCE_DIVISION, label: 'Mark attendance' },
        ]
      case 'staff':
        return [
          { to: ROUTES.ADMISSION_ANALYTICS, label: 'Admission analytics' },
          { to: ROUTES.EXAMS, label: 'Exams' },
          { to: ROUTES.FINANCE_ANALYTICS, label: 'Finance & fees' },
          { to: ROUTES.STUDENTS, label: 'Students' },
          { to: ROUTES.ATTENDANCE_DIVISION, label: 'Mark attendance' },
        ]
      case 'parent':
        return []
      default:
        return []
    }
  }, [appPersona])

  if (appPersona === 'parent') {
    return (
      <div style={styles.page}>
        <h1 style={styles.h1}>Home</h1>
        <p style={styles.scopeLine}>
          <Link to={ROUTES.PARENT_FEES} style={styles.inlineLink}>
            Pay fees
          </Link>
        </p>
      </div>
    )
  }

  return (
    <div style={styles.page}>
      <header style={styles.pageHead}>
        <h1 style={styles.h1}>Home</h1>
        {hasTreeScope && ctxParts.length > 0 ? (
          <p style={styles.scopeLine}>
            {ctxParts.join(' → ')}
            {org.divisionId ? (
              <>
                {' · '}
                <Link to={ROUTES.ATTENDANCE_DIVISION} style={styles.inlineLink}>
                  Attendance
                </Link>
              </>
            ) : null}
          </p>
        ) : null}
      </header>

      {appPersona === 'admin' && (
        <section style={styles.section} aria-label="Administration">
          <div style={styles.kpiGrid}>
            <div style={{ ...styles.kpiCard, borderTopColor: '#4f46e5' }}>
              <div style={styles.kpiLab}>Session</div>
              <div style={styles.kpiMeta}>{userEmail ?? '—'}</div>
              <div style={styles.kpiSub}>{jwtRoles.length ? jwtRoles.join(', ') : '—'}</div>
            </div>
            <div style={{ ...styles.kpiCard, borderTopColor: '#7c3aed' }}>
              <div style={styles.kpiLab}>Users</div>
              {userDirectoryLoading ? <Spinner /> : <div style={styles.kpiVal}>{userDirectoryCount ?? '—'}</div>}
              <Link to={ROUTES.USERS} style={styles.kpiLink}>
                Users →
              </Link>
            </div>
            <div style={{ ...styles.kpiCard, borderTopColor: '#0d9488' }}>
              <div style={styles.kpiLab}>Schools</div>
              <div style={styles.kpiVal}>{org.branches.length}</div>
              <Link to={ROUTES.ADMIN_ORG_SCHOOLS} style={styles.kpiLink}>
                Org →
              </Link>
            </div>
            <div style={{ ...styles.kpiCard, borderTopColor: '#047857' }}>
              <div style={styles.kpiLab}>Fees</div>
              <div style={styles.kpiValMuted}>—</div>
            </div>
          </div>
        </section>
      )}

      <section style={styles.kpiSection} aria-label="Metrics">
        <div style={styles.kpiGrid}>
          <div style={{ ...styles.kpiCard, borderTopColor: '#1e3a5f' }}>
            <div style={styles.kpiLab}>Enrollment</div>
            {!hasTreeScope ? (
              <div style={styles.kpiValMuted}>—</div>
            ) : adminLoading ? (
              <Spinner />
            ) : (
              <div style={styles.kpiVal}>{scopedStudents != null ? scopedStudents : '—'}</div>
            )}
            <Link to={ROUTES.ADMISSION_ANALYTICS} style={styles.kpiLink}>
              Analytics →
            </Link>
          </div>
          <div style={{ ...styles.kpiCard, borderTopColor: '#0369a1' }}>
            <div style={styles.kpiLab}>Admissions today</div>
            {!hasTreeScope ? (
              <div style={styles.kpiValMuted}>—</div>
            ) : adminLoading ? (
              <Spinner />
            ) : (
              <div style={styles.kpiVal}>{todayAdm != null ? todayAdm : '—'}</div>
            )}
            <Link to={ROUTES.ADMISSION} style={styles.kpiLink}>
              Admission →
            </Link>
          </div>
          <div style={{ ...styles.kpiCard, borderTopColor: '#047857' }}>
            <div style={styles.kpiLab}>Fees</div>
            {!org.branchId.trim() ? (
              <div style={styles.kpiValMuted}>—</div>
            ) : feeLoading ? (
              <Spinner />
            ) : feeDash ? (
              <>
                <div style={styles.kpiVal}>{feeDash.collectionRate.toFixed(1)}%</div>
                <div style={styles.kpiSub}>
                  {feeDash.collectedFees.toLocaleString(undefined, { maximumFractionDigits: 0 })} /{' '}
                  {feeDash.totalFees.toLocaleString(undefined, { maximumFractionDigits: 0 })}
                </div>
              </>
            ) : (
              <div style={styles.kpiValMuted}>—</div>
            )}
          </div>
          <div style={{ ...styles.kpiCard, borderTopColor: '#c2410c' }}>
            <div style={styles.kpiLab}>Attendance</div>
            {!org.branchId.trim() ? (
              <div style={styles.kpiValMuted}>—</div>
            ) : attLoading ? (
              <Spinner />
            ) : attMonth ? (
              <>
                <div style={styles.kpiVal}>{attMonth.percentage.toFixed(1)}%</div>
                <div style={styles.kpiSub}>
                  {attMonth.presentCount} / {attMonth.absentCount} · {attMonth.totalDays}d
                </div>
              </>
            ) : (
              <div style={styles.kpiValMuted}>—</div>
            )}
            <Link to={ROUTES.ATTENDANCE} style={styles.kpiLink}>
              Attendance →
            </Link>
          </div>
        </div>
      </section>

      {roleLinks.length > 0 && (
        <section style={styles.section}>
          <div style={styles.shortcuts}>
            {roleLinks.map((x) => (
              <Link key={x.to} to={x.to} style={styles.shortcut}>
                {x.label}
              </Link>
            ))}
          </div>
        </section>
      )}

      {(appPersona === 'admin' || appPersona === 'principal') && (enrollLoading || groupEnrollment.length > 0) && (
        <section style={styles.section}>
          {enrollLoading ? (
            <Spinner />
          ) : (
            <table style={styles.table}>
              <thead>
                <tr>
                  <th style={styles.th}>Group</th>
                  <th style={styles.th}>Schools</th>
                  <th style={styles.th}>Students</th>
                </tr>
              </thead>
              <tbody>
                {groupEnrollment.map((r) => (
                  <tr key={r.groupKey}>
                    <td style={styles.td}>{r.name}</td>
                    <td style={styles.td}>{r.schools}</td>
                    <td style={styles.td}>{r.students}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </section>
      )}

      {examsLoading ? (
        <div style={styles.spinnerRow}>
          <Spinner />
        </div>
      ) : null}
      {!examsLoading && examsError ? <p style={styles.error}>{examsError}</p> : null}
      {!examsLoading &&
        (appPersona === 'admin' || appPersona === 'principal') &&
        org.branches.length > 0 &&
        filteredExams.length > 0 && (
          <section style={styles.section}>
            <ul style={styles.list}>
              {filteredExams.map((e) => (
                <li key={e.id}>{e.name ?? e.id}</li>
              ))}
            </ul>
          </section>
        )}
    </div>
  )
}

const styles: Record<string, CSSProperties> = {
  page: { maxWidth: 1100 },
  pageHead: {
    marginBottom: 18,
    padding: '0 0 10px',
    borderBottom: '1px solid #e2e8f0',
  },
  h1: { margin: 0, fontSize: 20, fontWeight: 800, color: '#0f172a', letterSpacing: '-0.02em' },
  scopeLine: { margin: '8px 0 0', fontSize: 12, color: '#64748b', lineHeight: 1.4 },
  inlineLink: { color: '#1e3a5f', fontWeight: 700 },
  kpiMeta: { fontSize: 14, fontWeight: 600, color: '#334155', wordBreak: 'break-word' },
  kpiSub: { fontSize: 12, color: '#64748b', lineHeight: 1.35 },
  kpiSection: { marginBottom: 28 },
  kpiGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(220px, 1fr))',
    gap: 14,
  },
  kpiCard: {
    background: '#fff',
    borderRadius: 12,
    padding: 18,
    border: '1px solid #e2e8f0',
    borderTop: '4px solid #1e3a5f',
    boxShadow: '0 2px 10px rgba(15, 23, 42, 0.06)',
    display: 'flex',
    flexDirection: 'column',
    gap: 8,
    minHeight: 148,
  },
  kpiLab: {
    fontSize: 11,
    fontWeight: 800,
    letterSpacing: '0.06em',
    textTransform: 'uppercase',
    color: '#64748b',
  },
  kpiVal: { fontSize: 28, fontWeight: 800, color: '#0f172a', lineHeight: 1.1 },
  kpiValMuted: { fontSize: 22, fontWeight: 700, color: '#94a3b8' },
  kpiLink: { fontSize: 13, fontWeight: 700, color: '#1e3a5f', marginTop: 'auto', textDecoration: 'none' },
  spinnerRow: { padding: '8px 0' },
  shortcuts: { display: 'flex', flexWrap: 'wrap', gap: 10 },
  shortcut: {
    display: 'inline-block',
    padding: '10px 16px',
    borderRadius: 10,
    background: '#fff',
    border: '1px solid #e2e8f0',
    color: '#1e3a5f',
    fontWeight: 700,
    fontSize: 13,
    textDecoration: 'none',
    boxShadow: '0 1px 3px rgba(15, 23, 42, 0.06)',
  },
  section: { marginBottom: 24 },
  error: { color: '#c00', fontSize: 13, margin: 0 },
  list: { paddingLeft: 20, margin: 0, fontSize: 14, color: '#334155' },
  table: {
    width: '100%',
    maxWidth: 560,
    borderCollapse: 'collapse',
    marginTop: 0,
    fontSize: 14,
    background: '#fff',
    borderRadius: 8,
    overflow: 'hidden',
    boxShadow: '0 1px 3px rgba(15, 23, 42, 0.08)',
  },
  th: {
    textAlign: 'left',
    padding: '10px 12px',
    background: '#1e3a5f',
    color: '#fff',
    fontWeight: 600,
  },
  td: { padding: '10px 12px', borderBottom: '1px solid #e2e8f0', color: '#334155' },
}
