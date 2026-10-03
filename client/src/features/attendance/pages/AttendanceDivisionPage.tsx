import axios from 'axios'
import { useCallback, useEffect, useMemo, useState, type CSSProperties } from 'react'
import { Link } from 'react-router-dom'
import { ROUTES } from '../../../config/routes'
import { Button, Spinner } from '../../../shared'
import { useOrgHierarchy } from '../../../layouts/OrgHierarchyProvider'
import { referenceDataApi } from '../../admission/api/referenceData.api'
import { studentsApi } from '../../admission/api/students.api'
import { attendanceApi } from '../api/attendance.api'
import { holidayManagementApi } from '../api/holidayManagement.api'
import type { AttendanceSummary, AttendanceTrendPoint, AttendancePeriodType } from '../attendance.types'

const PERIOD_OPTIONS: { value: AttendancePeriodType; label: string }[] = [
  { value: 'Week', label: 'Week' },
  { value: 'Month', label: 'Month' },
  { value: 'AcademicYear', label: 'Academic Year' },
]

/** Status saved per student; server also marks calendar weekly-off / school holidays on save. */
const STATUS_OPTIONS = ['Present', 'Absent', 'Leave', 'HalfDay', 'Late', 'Holiday'] as const

const LEAVE_TYPE_OPTIONS = ['', 'Sick', 'Casual', 'Medical', 'Earned', 'Maternity', 'Emergency'] as const

function ymdLocal(d: Date): string {
  const y = d.getFullYear()
  const m = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  return `${y}-${m}-${day}`
}

type ManualRow = {
  studentId: string
  rollNumber: string
  displayName: string
  status: string
  leaveType: string
  remark: string
}

function formatAttendanceDateHint(ymd: string): string {
  if (!ymd || ymd.length < 10) return ''
  const [y, m, d] = ymd.split('-').map(Number)
  if (!y || !m || !d) return ymd
  const dt = new Date(y, m - 1, d)
  if (Number.isNaN(dt.getTime())) return ymd
  return dt.toLocaleDateString(undefined, { weekday: 'long', year: 'numeric', month: 'short', day: 'numeric' })
}

export function AttendanceDivisionPage() {
  const { ready: orgReady, branchId, divisionId, divisions, classId } = useOrgHierarchy()
  const [periodType, setPeriodType] = useState<AttendancePeriodType>('AcademicYear')
  const [summary, setSummary] = useState<AttendanceSummary | null>(null)
  const [percentage, setPercentage] = useState<number | null>(null)
  const [trend, setTrend] = useState<AttendanceTrendPoint[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const [markDate, setMarkDate] = useState(() => ymdLocal(new Date()))
  const [manualRows, setManualRows] = useState<ManualRow[]>([])
  const [manualLoading, setManualLoading] = useState(false)
  const [manualError, setManualError] = useState<string | null>(null)
  const [manualSaved, setManualSaved] = useState<string | null>(null)
  const [savingManual, setSavingManual] = useState(false)
  /** Filled from GET /Division/{id} when tree + list do not expose ClassId (PascalCase / incomplete list). */
  const [apiClassId, setApiClassId] = useState('')
  const [schoolHolidayLabel, setSchoolHolidayLabel] = useState<string | null>(null)

  const effectiveClassId = useMemo(() => {
    if (classId) return classId
    if (!divisionId) return ''
    const dv = divisions.find((d) => d.id === divisionId)
    return dv?.classId ?? (dv as unknown as { ClassId?: string })?.ClassId ?? ''
  }, [classId, divisionId, divisions])

  const classIdForAttendance = effectiveClassId || apiClassId

  useEffect(() => {
    let cancelled = false
    if (!divisionId) {
      setApiClassId('')
      return
    }
    if (effectiveClassId) {
      setApiClassId('')
      return
    }
    setApiClassId('')
    void referenceDataApi.getDivisionById(divisionId).then((d) => {
      if (cancelled || !d?.classId) return
      setApiClassId(d.classId)
    })
    return () => {
      cancelled = true
    }
  }, [divisionId, effectiveClassId])

  useEffect(() => {
    if (!branchId || !markDate) {
      setSchoolHolidayLabel(null)
      return
    }
    let cancelled = false
    holidayManagementApi
      .listHolidays(branchId, { from: markDate, to: markDate })
      .then((list) => {
        if (cancelled) return
        const h = list[0]
        setSchoolHolidayLabel(h?.name?.trim() ? h.name.trim() : list.length > 0 ? 'School holiday' : null)
      })
      .catch(() => {
        if (!cancelled) setSchoolHolidayLabel(null)
      })
    return () => {
      cancelled = true
    }
  }, [branchId, markDate])

  const loadManualGrid = useCallback(async () => {
    if (!divisionId || !classIdForAttendance) {
      setManualRows([])
      setManualLoading(false)
      return
    }
    setManualLoading(true)
    setManualError(null)
    setManualSaved(null)
    try {
      const [students, dayRecords] = await Promise.all([
        studentsApi.getStudentsForDivisionManualMark(divisionId, classIdForAttendance),
        attendanceApi.getDivisionDetailedForDay(divisionId, markDate),
      ])
      const byStudent = new Map<string, (typeof dayRecords)[0]>()
      for (const r of dayRecords) {
        if (r.studentId) byStudent.set(r.studentId, r)
      }
      const rows: ManualRow[] = students
        .map((s) => {
          const sid = s.id?.trim()
          if (!sid) return null
          const ex = byStudent.get(sid)
          const nameParts = [s.firstName, s.middleName, s.lastName].filter(
            (x) => x != null && String(x).trim() !== '',
          )
          const displayName = nameParts.length ? nameParts.map((x) => String(x)).join(' ') : '—'
          return {
            studentId: sid,
            rollNumber: s.rollNumber?.trim() ? String(s.rollNumber) : '',
            displayName,
            status: ex?.status?.trim() ? ex.status : 'Present',
            leaveType: ex?.leaveType ?? '',
            remark: ex?.remark ?? '',
          }
        })
        .filter((x): x is ManualRow => x != null)
        .sort((a, b) =>
          a.rollNumber.localeCompare(b.rollNumber, undefined, { numeric: true, sensitivity: 'base' }),
        )
      setManualRows(rows)
    } catch (e) {
      let msg = 'Could not load students or saved attendance for this date.'
      if (axios.isAxiosError(e)) {
        const st = e.response?.status
        if (st === 401 || st === 403) {
          msg = 'Sign in again — loading students requires authentication.'
        } else if (!e.response) {
          msg = 'Cannot reach the API. Is it running and is the Vite proxy correct?'
        } else if (typeof e.response.data === 'string' && e.response.data.trim()) {
          msg = e.response.data.trim().slice(0, 200)
        } else {
          msg = `Request failed (HTTP ${String(st)}). Check students exist for this division in the database.`
        }
      }
      setManualError(msg)
      setManualRows([])
    } finally {
      setManualLoading(false)
    }
  }, [divisionId, classIdForAttendance, markDate])

  useEffect(() => {
    if (!divisionId) return
    let cancelled = false
    setSummary(null)
    setPercentage(null)
    setTrend([])
    setError(null)
    setLoading(true)
    Promise.all([
      attendanceApi.getDivisionSummary(divisionId, periodType),
      attendanceApi.getDivisionPercentage(divisionId, periodType),
      attendanceApi.getDivisionTrend(divisionId, periodType),
    ])
      .then(([sum, pct, t]) => {
        if (cancelled) return
        setSummary(sum)
        setPercentage(pct)
        setTrend(t)
      })
      .catch(() => {
        if (!cancelled) setError('Failed to load division attendance. Check API, login, and PostgreSQL.')
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [divisionId, periodType])

  useEffect(() => {
    void loadManualGrid()
  }, [loadManualGrid])

  if (!orgReady) return <p><Spinner /> Loading…</p>
  if (!divisionId) {
    return (
      <div style={styles.wrap}>
        <h2>Division-wise Attendance</h2>
        <p style={styles.links}>
          <Link to={ROUTES.ATTENDANCE}>Dashboard</Link>
          {' · '}
          <Link to={ROUTES.ATTENDANCE_STUDENT}>Student-wise</Link>
        </p>
        <p style={styles.muted}>
          Select a <strong>division</strong> in the organization tree on the left (expand school → class → division).
        </p>
      </div>
    )
  }

  const selectedDivision = divisions.find((d) => d.id === divisionId)

  return (
    <div style={styles.wrap}>
      <h2>Division-wise Attendance</h2>
      <p style={styles.links}>
        <Link to={ROUTES.ATTENDANCE}>Dashboard</Link>
        {' · '}
        <Link to={ROUTES.ATTENDANCE_STUDENT}>Student-wise</Link>
      </p>
      <p style={styles.muted}>
        Division comes from the tree on the left. Weekly off days (Saturday/Sunday per branch holiday settings) and holiday-marked days are excluded from counts and percentage. Trend may show Saturday, Sunday, or Holiday.
      </p>

      {loading && (
        <p style={styles.banner}>
          <Spinner /> Loading division statistics…
        </p>
      )}
      {error && <p style={styles.error}>{error}</p>}

      <div style={styles.filters}>
        <label style={styles.label}>
          Period
          <select value={periodType} onChange={(e) => setPeriodType(e.target.value as AttendancePeriodType)} style={styles.select}>
            {PERIOD_OPTIONS.map((o) => (
              <option key={o.value} value={o.value}>{o.label}</option>
            ))}
          </select>
        </label>
      </div>

      {divisionId && !classIdForAttendance && (
        <p style={styles.muted}>
          Resolving class for this division… If this stays long, pick <strong>class</strong> (step 2) in the sidebar, or ensure the division has a ClassId in the database.
        </p>
      )}

      {divisionId && classIdForAttendance && (
        <section style={styles.section}>
          <h3>Mark attendance (manual)</h3>
          <p style={styles.muted}>
            For <strong>{selectedDivision?.name ?? 'this division'}</strong>: pick the calendar day you are marking,
            then set each student to Present, Absent, <strong>Leave</strong> (choose leave type), <strong>Holiday</strong>, etc., and save.
            Class id <code style={styles.code}>{classIdForAttendance.slice(0, 8)}…</code>
          </p>

          <div style={styles.dateCard}>
            <div style={styles.dateCardMain}>
              <label style={styles.labelBold}>
                Attendance date (which day?)
                <input
                  type="date"
                  value={markDate}
                  onChange={(e) => setMarkDate(e.target.value)}
                  style={styles.dateInputLarge}
                />
              </label>
              {markDate && (
                <p style={styles.dateHuman}>{formatAttendanceDateHint(markDate)}</p>
              )}
              <div style={styles.dateQuick}>
                <span style={styles.quickLabel}>Quick:</span>
                <button type="button" style={styles.chipBtn} onClick={() => setMarkDate(ymdLocal(new Date()))}>
                  Today
                </button>
                <button
                  type="button"
                  style={styles.chipBtn}
                  onClick={() => {
                    const d = new Date()
                    d.setDate(d.getDate() - 1)
                    setMarkDate(ymdLocal(d))
                  }}
                >
                  Yesterday
                </button>
              </div>
            </div>
            {schoolHolidayLabel && (
              <div style={styles.holidayCallout} role="status">
                <strong>School holiday on this date:</strong> {schoolHolidayLabel}. You can still save rows; the server
                treats calendar holidays / weekly off when storing.
              </div>
            )}
            {!schoolHolidayLabel && branchId && (
              <p style={styles.calendarHint}>No named school holiday on this date in Holiday management (branch may still have weekly off).</p>
            )}
          </div>

          <datalist id="manual-leave-type-presets">
            {LEAVE_TYPE_OPTIONS.filter((x) => x !== '').map((x) => (
              <option key={x} value={x} />
            ))}
          </datalist>

          <div style={styles.manualBar}>
            <Button type="button" variant="secondary" onClick={() => void loadManualGrid()} disabled={manualLoading}>
              Reload student list
            </Button>
            <Button
              type="button"
              variant="secondary"
              onClick={() =>
                setManualRows((prev) => prev.map((r) => ({ ...r, status: 'Present', leaveType: '', remark: '' })))
              }
              disabled={manualRows.length === 0 || manualLoading}
            >
              Mark all present
            </Button>
            <Button
              type="button"
              onClick={async () => {
                if (!classIdForAttendance || !divisionId || manualRows.length === 0) return
                setSavingManual(true)
                setManualError(null)
                setManualSaved(null)
                try {
                  await attendanceApi.markClassAttendance(classIdForAttendance, divisionId, markDate, manualRows)
                  setManualSaved(`Saved attendance for ${manualRows.length} student(s) on ${markDate}.`)
                  await loadManualGrid()
                } catch {
                  setManualError('Save failed (locked date, network, or server validation). Check API logs.')
                } finally {
                  setSavingManual(false)
                }
              }}
              disabled={manualLoading || savingManual || manualRows.length === 0}
            >
              {savingManual ? <Spinner size={18} /> : 'Save attendance'}
            </Button>
          </div>
          {manualError && <p style={styles.error}>{manualError}</p>}
          {manualSaved && <p style={styles.saved}>{manualSaved}</p>}
          {!manualLoading && manualRows.length > 0 && (
            <p style={styles.countLine}>
              <strong>{manualRows.length}</strong> student(s) in this division · roll &amp; name below · use{' '}
              <strong>Leave</strong> + leave type for approved / sick leave.
            </p>
          )}
          {manualLoading && manualRows.length === 0 ? (
            <p style={styles.banner}>
              <Spinner /> Loading students (division list, or class roster filtered to this division)…
            </p>
          ) : manualRows.length === 0 ? (
            <p style={styles.muted}>
              No students found for this division. In the database, students need <code style={styles.code}>division_id</code>{' '}
              matching this division (or same class with correct division). Add/admit students or check Admission.
            </p>
          ) : (
            <div style={styles.tableWrap}>
              <table style={styles.table}>
                <thead>
                  <tr>
                    <th style={styles.th}>#</th>
                    <th style={styles.th}>Roll no.</th>
                    <th style={styles.th}>Student name</th>
                    <th style={styles.th}>Attendance</th>
                    <th style={styles.th}>Leave type</th>
                    <th style={styles.th}>Remark</th>
                  </tr>
                </thead>
                <tbody>
                  {manualRows.map((row, idx) => (
                    <tr key={row.studentId}>
                      <td style={styles.td}>{idx + 1}</td>
                      <td style={styles.td}>{row.rollNumber || '—'}</td>
                      <td style={styles.td}>{row.displayName}</td>
                      <td style={styles.td}>
                        <select
                          value={row.status}
                          onChange={(e) => {
                            const v = e.target.value
                            setManualRows((prev) =>
                              prev.map((r, i) => (i === idx ? { ...r, status: v } : r)),
                            )
                          }}
                          style={styles.selectCompact}
                        >
                          {STATUS_OPTIONS.map((s) => (
                            <option key={s} value={s}>
                              {s}
                            </option>
                          ))}
                        </select>
                      </td>
                      <td style={styles.td}>
                        <input
                          type="text"
                          list="manual-leave-type-presets"
                          value={row.leaveType}
                          placeholder="e.g. Sick (or pick from list)"
                          title="Required when status is Leave; optional otherwise"
                          onChange={(e) => {
                            const v = e.target.value
                            setManualRows((prev) =>
                              prev.map((r, i) => (i === idx ? { ...r, leaveType: v } : r)),
                            )
                          }}
                          style={styles.textInput}
                        />
                      </td>
                      <td style={styles.td}>
                        <input
                          type="text"
                          value={row.remark}
                          onChange={(e) => {
                            const v = e.target.value
                            setManualRows((prev) =>
                              prev.map((r, i) => (i === idx ? { ...r, remark: v } : r)),
                            )
                          }}
                          style={styles.textInput}
                        />
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </section>
      )}

      {divisionId && (
        <>
          {summary && (
            <section style={styles.section}>
              <h3>{selectedDivision?.name ?? 'Division'} – Summary</h3>
              <div className="form-grid" style={styles.cards}>
                <div style={styles.card}>
                  <div style={styles.cardValue}>{summary.presentCount}</div>
                  <div style={styles.cardLabel}>Present</div>
                </div>
                <div style={styles.card}>
                  <div style={styles.cardValue}>{summary.absentCount}</div>
                  <div style={styles.cardLabel}>Absent</div>
                </div>
                <div style={styles.card}>
                  <div style={styles.cardValue}>{summary.leaveCount}</div>
                  <div style={styles.cardLabel}>Leave</div>
                </div>
                <div style={styles.card}>
                  <div style={styles.cardValue}>{percentage != null ? `${percentage.toFixed(1)}%` : '—'}</div>
                  <div style={styles.cardLabel}>Percentage</div>
                </div>
                <div style={styles.card}>
                  <div style={styles.cardValue}>{summary.totalDays}</div>
                  <div style={styles.cardLabel}>Total Days</div>
                </div>
              </div>
            </section>
          )}

          {trend.length > 0 && (
            <section style={styles.section}>
              <h3>Trend (last 10)</h3>
              <table style={styles.table}>
                <thead>
                  <tr>
                    <th style={styles.th}>Date</th>
                    <th style={styles.th}>Status</th>
                  </tr>
                </thead>
                <tbody>
                  {trend.slice(0, 10).map((t, i) => (
                    <tr key={`${t.date}-${i}`}>
                      <td style={styles.td}>{t.date}</td>
                      <td style={styles.td}>{t.status}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </section>
          )}
        </>
      )}
    </div>
  )
}

const styles: Record<string, CSSProperties> = {
  wrap: { padding: 0 },
  banner: { display: 'flex', alignItems: 'center', gap: 8, marginBottom: 12, fontSize: 14, color: '#334155' },
  muted: { color: '#666', fontSize: 14, marginBottom: 12 },
  links: { marginBottom: 16 },
  filters: { display: 'flex', gap: 24, flexWrap: 'wrap', marginBottom: 24 },
  label: { display: 'flex', flexDirection: 'column', gap: 4, fontSize: 14, fontWeight: 500 },
  select: { padding: 8, border: '1px solid #ccc', borderRadius: 6, minWidth: 160 },
  section: { marginBottom: 32 },
  cards: { display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(140px, 1fr))', gap: '16px', marginTop: 8 },
  card: { background: '#fff', padding: 20, borderRadius: 8, boxShadow: '0 1px 3px rgba(0,0,0,0.1)' },
  cardValue: { fontSize: 24, fontWeight: 700, color: '#1e3a5f' },
  cardLabel: { fontSize: 13, color: '#666', marginTop: 4 },
  table: { borderCollapse: 'collapse', background: '#fff', borderRadius: 8, overflow: 'hidden', boxShadow: '0 1px 3px rgba(0,0,0,0.1)' },
  th: { textAlign: 'left', padding: '10px 12px', background: '#1e3a5f', color: '#fff', fontSize: 14 },
  td: { padding: '10px 12px', borderBottom: '1px solid #eee' },
  error: { color: '#c00' },
  saved: { color: '#166534', fontSize: 14, marginTop: 8 },
  code: { fontSize: 12, background: '#f1f5f9', padding: '2px 6px', borderRadius: 4 },
  manualBar: {
    display: 'flex',
    flexWrap: 'wrap',
    gap: 12,
    alignItems: 'flex-end',
    marginBottom: 16,
  },
  dateCard: {
    border: '1px solid #cbd5e1',
    borderRadius: 10,
    padding: 16,
    marginBottom: 20,
    background: '#f8fafc',
  },
  dateCardMain: { marginBottom: 8 },
  labelBold: { display: 'flex', flexDirection: 'column', gap: 6, fontSize: 15, fontWeight: 700, color: '#0f172a' },
  dateInputLarge: { padding: '10px 12px', border: '1px solid #94a3b8', borderRadius: 8, fontSize: 16, maxWidth: 280 },
  dateHuman: { margin: '8px 0 0', fontSize: 14, color: '#475569', fontWeight: 500 },
  dateQuick: { display: 'flex', flexWrap: 'wrap', alignItems: 'center', gap: 8, marginTop: 12 },
  quickLabel: { fontSize: 13, color: '#64748b' },
  chipBtn: {
    padding: '6px 12px',
    fontSize: 13,
    fontWeight: 600,
    border: '1px solid #1e3a5f',
    borderRadius: 8,
    background: '#fff',
    color: '#1e3a5f',
    cursor: 'pointer',
  },
  holidayCallout: {
    marginTop: 12,
    padding: '10px 12px',
    borderRadius: 8,
    background: '#fff7ed',
    border: '1px solid #fdba74',
    color: '#9a3412',
    fontSize: 14,
    lineHeight: 1.45,
  },
  calendarHint: { margin: '8px 0 0', fontSize: 12, color: '#64748b' },
  countLine: { fontSize: 14, color: '#334155', marginBottom: 12 },
  selectCompact: { padding: 6, border: '1px solid #ccc', borderRadius: 6, minWidth: 120, fontSize: 13 },
  dateInput: { padding: 8, border: '1px solid #ccc', borderRadius: 6, fontSize: 14 },
  tableWrap: { overflowX: 'auto' },
  textInput: { width: '100%', minWidth: 120, padding: 6, border: '1px solid #ccc', borderRadius: 6, fontSize: 13 },
}
