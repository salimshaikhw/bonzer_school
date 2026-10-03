import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { ROUTES } from '../../../config/routes'
import { Spinner } from '../../../shared'
import { studentsApi } from '../../admission/api/students.api'
import { branchesApi } from '../../branches'
import { attendanceApi } from '../api/attendance.api'
import type { AttendanceSummary, AttendanceTrendPoint, AttendancePeriodType, AttendanceRecord } from '../attendance.types'

const PERIOD_OPTIONS: { value: AttendancePeriodType; label: string }[] = [
  { value: 'Week', label: 'Week' },
  { value: 'Month', label: 'Month' },
  { value: 'Custom', label: 'Custom Range' },
]

type StudentPick = {
  id: string
  firstName?: string
  lastName?: string
  admissionNo?: string
  rollNumber?: string
}

type MonthlyAnalyticsItem = {
  monthKey: string
  monthLabel: string
  presentDays: number
  workingDays: number
  percentage: number
}

type ChartMode = 'Week' | 'Month' | 'Custom'
type DayChartBar = { key: string; label: string; value: number; status: string; color: string }
type CustomPlotGranularity = 'Daily' | 'Weekly' | 'Monthly'

function formatStudentName(student: StudentPick): string {
  return [student.firstName, student.lastName].filter(Boolean).join(' ') || student.admissionNo || student.id.slice(0, 8)
}

function toDateInputValue(date: Date): string {
  return date.toISOString().slice(0, 10)
}

function buildDefaultCustomRange() {
  const end = new Date()
  const start = new Date()
  start.setDate(end.getDate() - 29)
  return { start: toDateInputValue(start), end: toDateInputValue(end) }
}

function buildMonthlyAnalytics(trend: AttendanceTrendPoint[]): MonthlyAnalyticsItem[] {
  const monthMap = new Map<string, { monthLabel: string; presentDays: number; workingDays: number }>()
  const presentStatuses = new Set(['Present', 'Late', 'HalfDay'])
  const nonWorkingStatuses = new Set(['Sunday', 'Holiday', 'WeeklyOff', 'Off'])

  for (const row of trend) {
    if (!row.date) continue
    const d = new Date(row.date)
    if (Number.isNaN(d.getTime())) continue
    const monthKey = `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}`
    const monthLabel = d.toLocaleString(undefined, { month: 'short', year: 'numeric' })
    const status = (row.status ?? '').trim()
    const agg = monthMap.get(monthKey) ?? { monthLabel, presentDays: 0, workingDays: 0 }

    if (!nonWorkingStatuses.has(status)) {
      agg.workingDays += 1
      if (presentStatuses.has(status)) agg.presentDays += 1
    }

    monthMap.set(monthKey, agg)
  }

  return Array.from(monthMap.entries())
    .map(([monthKey, value]) => ({
      monthKey,
      monthLabel: value.monthLabel,
      presentDays: value.presentDays,
      workingDays: value.workingDays,
      percentage: value.workingDays > 0 ? (value.presentDays / value.workingDays) * 100 : 0,
    }))
    .sort((a, b) => b.monthKey.localeCompare(a.monthKey))
}

function getBarColor(percentage: number): string {
  if (percentage >= 90) return '#16a34a'
  if (percentage >= 75) return '#f59e0b'
  return '#dc2626'
}

function getStatusScore(status: string | undefined): number {
  const s = (status ?? '').trim().toLowerCase()
  if (s === 'present' || s === 'late') return 100
  if (s === 'halfday') return 50
  if (s === 'leave') return 25
  return 0
}

function getStatusColor(status: string | undefined): string {
  const s = (status ?? '').trim().toLowerCase()
  if (s === 'present' || s === 'late') return '#16a34a'
  if (s === 'halfday') return '#f59e0b'
  if (s === 'leave') return '#a855f7'
  return '#dc2626'
}

function buildDayChartBars(records: AttendanceRecord[], mode: ChartMode): DayChartBar[] {
  const sorted = [...records].sort((a, b) => String(a.attendanceDate ?? '').localeCompare(String(b.attendanceDate ?? '')))
  const capped = mode === 'Week' ? sorted.slice(-7) : mode === 'Month' ? sorted.slice(-31) : sorted.slice(-62)
  return capped.map((r, idx) => {
    const date = r.attendanceDate ?? `row-${idx}`
    const status = r.status ?? 'Absent'
    const d = new Date(date)
    const label = Number.isNaN(d.getTime())
      ? date
      : mode === 'Month'
        ? `${d.getDate()}`
        : `${String(d.getDate()).padStart(2, '0')}/${String(d.getMonth() + 1).padStart(2, '0')}`
    return {
      key: `${date}-${idx}`,
      label,
      value: getStatusScore(status),
      status,
      color: getStatusColor(status),
    }
  })
}

function buildMonthlyBarsFromTrend(trend: AttendanceTrendPoint[]): DayChartBar[] {
  const monthly = buildMonthlyAnalytics(trend).slice(0, 12).reverse()
  return monthly.map((m) => ({
    key: m.monthKey,
    label: m.monthLabel,
    value: Math.max(0, Math.min(100, m.percentage)),
    status: `${m.presentDays}/${m.workingDays} days`,
    color: getBarColor(m.percentage),
  }))
}

function getWeekBucket(d: Date): { key: string; label: string } {
  const date = new Date(d)
  date.setHours(0, 0, 0, 0)
  const day = (date.getDay() + 6) % 7
  date.setDate(date.getDate() - day)
  const year = date.getFullYear()
  const start = new Date(year, 0, 1)
  const week = Math.ceil((((date.getTime() - start.getTime()) / 86400000) + 1) / 7)
  return { key: `${year}-W${String(week).padStart(2, '0')}`, label: `W${week} ${year}` }
}

function buildCustomAggregatedBars(records: AttendanceRecord[], granularity: CustomPlotGranularity): DayChartBar[] {
  if (granularity === 'Daily') return buildDayChartBars(records, 'Custom')
  const presentStatuses = new Set(['present', 'late', 'halfday'])
  const nonWorkingStatuses = new Set(['sunday', 'holiday', 'weeklyoff', 'off'])
  const map = new Map<string, { label: string; present: number; working: number }>()

  for (const r of records) {
    if (!r.attendanceDate) continue
    const d = new Date(r.attendanceDate)
    if (Number.isNaN(d.getTime())) continue
    const status = (r.status ?? '').trim().toLowerCase()
    let key = ''
    let label = ''
    if (granularity === 'Weekly') {
      const b = getWeekBucket(d)
      key = b.key
      label = b.label
    } else {
      key = `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}`
      label = d.toLocaleString(undefined, { month: 'short', year: 'numeric' })
    }
    const agg = map.get(key) ?? { label, present: 0, working: 0 }
    if (!nonWorkingStatuses.has(status)) {
      agg.working += 1
      if (presentStatuses.has(status)) agg.present += 1
    }
    map.set(key, agg)
  }

  return Array.from(map.entries())
    .sort((a, b) => a[0].localeCompare(b[0]))
    .map(([key, v]) => {
      const pct = v.working > 0 ? (v.present / v.working) * 100 : 0
      return {
        key,
        label: v.label,
        value: Math.max(0, Math.min(100, pct)),
        status: `${v.present}/${v.working} days`,
        color: getBarColor(pct),
      }
    })
}

function normalizeRoll(value: string | undefined): string {
  if (!value) return ''
  const trimmed = value.trim()
  if (trimmed === '') return ''
  const asNumber = Number(trimmed)
  if (Number.isFinite(asNumber) && asNumber >= 0) return String(asNumber)
  return trimmed.toLowerCase()
}

function mapStudentPick(raw: unknown): StudentPick | null {
  if (!raw || typeof raw !== 'object') return null
  const r = raw as Record<string, unknown>
  const id = String(r.id ?? r.Id ?? '')
  if (!id) return null
  return {
    id,
    firstName: String(r.firstName ?? r.FirstName ?? '') || undefined,
    lastName: String(r.lastName ?? r.LastName ?? '') || undefined,
    admissionNo: String(r.admissionNo ?? r.AdmissionNo ?? '') || undefined,
    rollNumber: String(r.rollNumber ?? r.RollNumber ?? r.roll_no ?? '') || undefined,
  }
}

export function AttendanceStudentPage() {
  const [branches, setBranches] = useState<{ id: string; name?: string }[]>([])
  const [branchId, setBranchId] = useState('')
  const [students, setStudents] = useState<StudentPick[]>([])
  const [studentId, setStudentId] = useState('')
  const [rollNo, setRollNo] = useState('')
  const [periodType, setPeriodType] = useState<AttendancePeriodType>('Week')
  const [customRange, setCustomRange] = useState(buildDefaultCustomRange())
  const [summary, setSummary] = useState<AttendanceSummary | null>(null)
  const [percentage, setPercentage] = useState<number | null>(null)
  const [trend, setTrend] = useState<AttendanceTrendPoint[]>([])
  const [records, setRecords] = useState<AttendanceRecord[]>([])
  const [monthlyAnalytics, setMonthlyAnalytics] = useState<MonthlyAnalyticsItem[]>([])
  const [chartMode, setChartMode] = useState<ChartMode>('Week')
  const [chartCustomRange, setChartCustomRange] = useState(buildDefaultCustomRange())
  const [customPlotBy, setCustomPlotBy] = useState<CustomPlotGranularity>('Daily')
  const [chartBars, setChartBars] = useState<DayChartBar[]>([])
  const [chartOverall, setChartOverall] = useState<number | null>(null)
  const [chartLoading, setChartLoading] = useState(false)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [rollingUp, setRollingUp] = useState(false)

  useEffect(() => {
    branchesApi
      .getAll()
      .then((list) => {
        setBranches(list)
        if (list.length > 0 && !branchId) setBranchId(list[0].id)
      })
      .catch(() => setError('Failed to load branches'))
  }, [])

  useEffect(() => {
    if (!branchId) return
    setError(null)
    studentsApi
      .getByBranch(branchId)
      .then((list) => {
        const mapped = (Array.isArray(list) ? list : [])
          .map(mapStudentPick)
          .filter((s): s is StudentPick => !!s)
        setStudents(mapped)
        if (mapped.length > 0 && !studentId) setStudentId(mapped[0].id)
        else if (!mapped.find((s) => s.id === studentId)) setStudentId('')
      })
      .catch(() => setError('Failed to load students'))
  }, [branchId])

  const selectedStudent = useMemo(
    () => students.find((s) => s.id === studentId) ?? null,
    [students, studentId]
  )

  const studentName = selectedStudent ? formatStudentName(selectedStudent) : '—'
  const totalDays = summary?.totalDays ?? 0
  const presentPct = totalDays > 0 ? ((summary?.presentCount ?? 0) / totalDays) * 100 : 0
  const absentPct = totalDays > 0 ? ((summary?.absentCount ?? 0) / totalDays) * 100 : 0
  const leavePct = totalDays > 0 ? ((summary?.leaveCount ?? 0) / totalDays) * 100 : 0

  const canLoadCustom = periodType !== 'Custom' || (!!customRange.start && !!customRange.end)

  function handleFindByRoll() {
    const q = normalizeRoll(rollNo)
    if (!q) return
    const found = students.find((s) => normalizeRoll(s.rollNumber) === q)
      ?? students.find((s) => normalizeRoll(s.rollNumber).includes(q))
    if (found) {
      setStudentId(found.id)
      setError(null)
      return
    }
    setError('No student found for the entered roll number in selected branch.')
  }

  useEffect(() => {
    if (!studentId || !canLoadCustom) return
    setLoading(true)
    setError(null)
    const startDate = periodType === 'Custom' ? customRange.start : undefined
    const endDate = periodType === 'Custom' ? customRange.end : undefined

    Promise.all([
      attendanceApi.getStudentSummary(studentId, periodType, startDate, endDate),
      attendanceApi.getStudentTrend(studentId, periodType),
      attendanceApi.getStudentReport(studentId, periodType, startDate, endDate),
      periodType === 'Custom'
        ? Promise.resolve<number | null>(null)
        : attendanceApi.getStudentPercentage(studentId, periodType),
    ])
      .then(([sum, t, report, pct]) => {
        setSummary(sum)
        setPercentage(pct ?? sum.percentage ?? null)
        setTrend(t)
        setRecords(report.detailedList ?? [])
      })
      .catch(() => setError('Failed to load student attendance'))
      .finally(() => setLoading(false))
  }, [studentId, periodType, customRange.start, customRange.end, canLoadCustom])

  useEffect(() => {
    if (!studentId) return
    setRollingUp(true)
    attendanceApi
      .getStudentTrend(studentId, 'AcademicYear')
      .then((yearTrend) => setMonthlyAnalytics(buildMonthlyAnalytics(yearTrend)))
      .catch(() => setMonthlyAnalytics([]))
      .finally(() => setRollingUp(false))
  }, [studentId])

  useEffect(() => {
    if (!studentId) return
    if (chartMode === 'Custom' && (!chartCustomRange.start || !chartCustomRange.end)) return

    setChartLoading(true)
    const startDate = chartMode === 'Custom' ? chartCustomRange.start : undefined
    const endDate = chartMode === 'Custom' ? chartCustomRange.end : undefined

    const dataPromise = chartMode === 'Month'
      ? Promise.all([
          attendanceApi.getStudentTrend(studentId, 'AcademicYear'),
          attendanceApi.getStudentPercentage(studentId, 'AcademicYear'),
        ]).then(([trendRows, pct]) => ({ bars: buildMonthlyBarsFromTrend(trendRows), pct }))
      : Promise.all([
          attendanceApi.getStudentReport(studentId, chartMode, startDate, endDate),
          chartMode === 'Custom'
            ? attendanceApi.getStudentSummary(studentId, 'Custom', startDate, endDate).then((s) => s.percentage)
            : attendanceApi.getStudentPercentage(studentId, chartMode),
        ]).then(([report, pct]) => {
          const detailed = report?.detailedList ?? []
          const bars = chartMode === 'Custom'
            ? buildCustomAggregatedBars(detailed, customPlotBy)
            : buildDayChartBars(detailed, chartMode)
          return { bars, pct }
        })

    dataPromise
      .then(({ bars, pct }) => {
        setChartBars(bars)
        setChartOverall(pct ?? null)
      })
      .catch(() => {
        setChartBars([])
        setChartOverall(null)
      })
      .finally(() => setChartLoading(false))
  }, [studentId, chartMode, chartCustomRange.start, chartCustomRange.end, customPlotBy])

  if (error && !summary && students.length === 0) return <p style={styles.error}>{error}</p>

  return (
    <div style={styles.wrap}>
      <div style={styles.erpShell}>
        <div style={styles.pageHead}>
          <div>
            <h2 style={styles.title}>Student Attendance ERP</h2>
            <p style={styles.subtitle}>Monitor student attendance dashboard and analytics with filters and trend insights.</p>
            <p style={styles.links}>
              <Link to={ROUTES.ATTENDANCE}>Attendance Dashboard</Link>
              {' · '}
              <Link to={ROUTES.ATTENDANCE_DIVISION}>Division-wise</Link>
            </p>
          </div>
          {selectedStudent && (
            <div style={styles.selectedBadge}>
              <div style={styles.selectedLabel}>Selected Student</div>
              <div style={styles.selectedName}>{studentName}</div>
              <div style={styles.selectedMeta}>Roll: {selectedStudent.rollNumber ?? '—'}</div>
            </div>
          )}
        </div>

        <p style={styles.muted}>
          Weekly off days (Saturday/Sunday per branch holiday settings) and holiday-marked days are excluded from counts and percentage.
        </p>

      <div style={styles.filtersCard}>
        <div style={styles.filters}>
        <label style={styles.label}>
          Branch
          <select value={branchId} onChange={(e) => setBranchId(e.target.value)} style={styles.select}>
            <option value="">— Select —</option>
            {branches.map((b) => (
              <option key={b.id} value={b.id}>{b.name ?? b.id}</option>
            ))}
          </select>
        </label>
        <label style={styles.label}>
          Student
          <select value={studentId} onChange={(e) => setStudentId(e.target.value)} style={styles.select}>
            <option value="">— Select —</option>
            {students.map((s) => (
              <option key={s.id} value={s.id}>
                {[s.firstName, s.lastName].filter(Boolean).join(' ') || s.admissionNo || s.id.slice(0, 8)}
              </option>
            ))}
          </select>
        </label>
        <label style={styles.label}>
          Roll no
          <div style={styles.rollRow}>
            <input
              value={rollNo}
              onChange={(e) => setRollNo(e.target.value)}
              placeholder="Enter roll number"
              style={styles.input}
            />
            <button type="button" style={styles.searchBtn} onClick={handleFindByRoll}>
              Find
            </button>
          </div>
        </label>
        <label style={styles.label}>
          Period
          <select value={periodType} onChange={(e) => setPeriodType(e.target.value as AttendancePeriodType)} style={styles.select}>
            {PERIOD_OPTIONS.map((o) => (
              <option key={o.value} value={o.value}>{o.label}</option>
            ))}
          </select>
        </label>
        {periodType === 'Custom' && (
          <>
            <label style={styles.label}>
              Start date
              <input
                type="date"
                value={customRange.start}
                onChange={(e) => setCustomRange((r) => ({ ...r, start: e.target.value }))}
                style={styles.select}
              />
            </label>
            <label style={styles.label}>
              End date
              <input
                type="date"
                value={customRange.end}
                onChange={(e) => setCustomRange((r) => ({ ...r, end: e.target.value }))}
                style={styles.select}
              />
            </label>
          </>
        )}
      </div>
      </div>

      {studentId && (
        <>
          {loading && !summary && <p><Spinner /> Loading…</p>}
          {summary && (
            <section style={styles.section}>
              <h3 style={styles.sectionTitle}>Attendance Dashboard</h3>
              <div style={styles.analyticsRow}>
                <div style={styles.analyticsCard}>
                  <div style={styles.analyticsTitle}>Overall Attendance</div>
                  <div style={{ ...styles.analyticsValue, color: getBarColor(percentage ?? presentPct) }}>
                    {(percentage ?? presentPct).toFixed(1)}%
                  </div>
                </div>
                <div style={styles.analyticsCard}>
                  <div style={styles.analyticsTitle}>Present Ratio</div>
                  <div style={{ ...styles.analyticsValue, color: getBarColor(presentPct) }}>{presentPct.toFixed(1)}%</div>
                </div>
                <div style={styles.analyticsCard}>
                  <div style={styles.analyticsTitle}>Absent Ratio</div>
                  <div style={{ ...styles.analyticsValue, color: getBarColor(100 - absentPct) }}>{absentPct.toFixed(1)}%</div>
                </div>
                <div style={styles.analyticsCard}>
                  <div style={styles.analyticsTitle}>Leave Ratio</div>
                  <div style={{ ...styles.analyticsValue, color: '#a855f7' }}>{leavePct.toFixed(1)}%</div>
                </div>
              </div>
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
                  <div style={styles.cardValue}>{summary.totalDays}</div>
                  <div style={styles.cardLabel}>Total Days</div>
                </div>
              </div>
            </section>
          )}

          {trend.length > 0 && (
            <section style={styles.section}>
              <h3 style={styles.sectionTitle}>{periodType} trend (last 15)</h3>
              <table style={styles.table}>
                <thead>
                  <tr>
                    <th style={styles.th}>Date</th>
                    <th style={styles.th}>Status</th>
                  </tr>
                </thead>
                <tbody>
                  {trend.slice(0, 15).map((t, i) => (
                    <tr key={`${t.date}-${i}`}>
                      <td style={styles.td}>{t.date}</td>
                      <td style={styles.td}>{t.status}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </section>
          )}

          <section style={styles.section}>
            <h3 style={styles.sectionTitle}>Attendance records ({periodType})</h3>
            {records.length === 0 && !loading && <p style={styles.muted}>No detailed records for selected filters.</p>}
            {records.length > 0 && (
              <table style={styles.table}>
                <thead>
                  <tr>
                    <th style={styles.th}>Date</th>
                    <th style={styles.th}>Status</th>
                    <th style={styles.th}>Leave Type</th>
                    <th style={styles.th}>Remark</th>
                  </tr>
                </thead>
                <tbody>
                  {records.slice(0, 31).map((r) => (
                    <tr key={r.id}>
                      <td style={styles.td}>{r.attendanceDate ?? '—'}</td>
                      <td style={styles.td}>{r.status ?? '—'}</td>
                      <td style={styles.td}>{r.leaveType ?? '—'}</td>
                      <td style={styles.td}>{r.remark ?? '—'}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </section>

          <section style={styles.section}>
            <h3 style={styles.sectionTitle}>Attendance analytics chart</h3>
            <div style={styles.chartTopRow}>
              <div style={styles.chartModeButtons}>
                {(['Week', 'Month', 'Custom'] as ChartMode[]).map((m) => (
                  <button
                    key={m}
                    type="button"
                    onClick={() => setChartMode(m)}
                    style={{
                      ...styles.chartModeBtn,
                      ...(chartMode === m ? styles.chartModeBtnActive : {}),
                    }}
                  >
                    {m === 'Week' ? 'Weekly' : m === 'Month' ? 'Monthly' : 'Custom'}
                  </button>
                ))}
              </div>
              <div style={styles.chartHint}>
                Chart score: {chartOverall != null ? `${chartOverall.toFixed(1)}%` : '—'}
              </div>
            </div>

            {chartMode === 'Custom' && (
              <div style={styles.chartCustomRow}>
                <label style={styles.label}>
                  From
                  <input
                    type="date"
                    value={chartCustomRange.start}
                    onChange={(e) => setChartCustomRange((r) => ({ ...r, start: e.target.value }))}
                    style={styles.select}
                  />
                </label>
                <label style={styles.label}>
                  To
                  <input
                    type="date"
                    value={chartCustomRange.end}
                    onChange={(e) => setChartCustomRange((r) => ({ ...r, end: e.target.value }))}
                    style={styles.select}
                  />
                </label>
              </div>
            )}
            {chartMode === 'Custom' && (
              <div style={styles.customPlotButtons}>
                {(['Daily', 'Weekly', 'Monthly'] as CustomPlotGranularity[]).map((g) => (
                  <button
                    key={g}
                    type="button"
                    onClick={() => setCustomPlotBy(g)}
                    style={{
                      ...styles.chartModeBtn,
                      ...(customPlotBy === g ? styles.chartModeBtnActive : {}),
                    }}
                  >
                    {g}
                  </button>
                ))}
              </div>
            )}

            {chartLoading && <p><Spinner /> Loading chart…</p>}
            {!chartLoading && chartBars.length > 0 && (
              <div style={styles.dailyChartWrap}>
                {chartBars.map((b) => (
                    <div key={b.key} style={styles.dailyBarItem} title={`${b.label}: ${b.status}`}>
                    <div style={styles.dailyBarTrack}>
                      <div style={{ ...styles.dailyBarFill, height: `${Math.max(4, b.value)}%`, background: b.color }} />
                    </div>
                      <div style={styles.dailyBarLabel}>{b.label}</div>
                  </div>
                ))}
              </div>
            )}
            {!chartLoading && chartBars.length === 0 && <p style={styles.muted}>No chart data for selected range.</p>}

            <h3 style={styles.subSectionTitle}>Monthly analytics (student)</h3>
            {rollingUp && <p><Spinner /> Loading monthly analytics…</p>}
            {!rollingUp && monthlyAnalytics.length === 0 && <p style={styles.muted}>No monthly analytics available.</p>}
            {monthlyAnalytics.length > 0 && (
              <table style={styles.table}>
                <thead>
                  <tr>
                    <th style={styles.th}>Month</th>
                    <th style={styles.th}>Present Days</th>
                    <th style={styles.th}>Working Days</th>
                    <th style={styles.th}>Attendance %</th>
                  </tr>
                </thead>
                <tbody>
                  {monthlyAnalytics.slice(0, 12).map((m) => (
                    <tr key={m.monthKey}>
                      <td style={styles.td}>{m.monthLabel}</td>
                      <td style={styles.td}>{m.presentDays}</td>
                      <td style={styles.td}>{m.workingDays}</td>
                      <td style={styles.td}>
                        <span
                          style={{
                            ...styles.percentPill,
                            color: getBarColor(m.percentage),
                            borderColor: getBarColor(m.percentage),
                            background: `${getBarColor(m.percentage)}15`,
                          }}
                        >
                          {m.percentage.toFixed(1)}%
                        </span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </section>
        </>
      )}
      {!studentId && students.length > 0 && (
        <p style={styles.muted}>Select a student or search by roll no to view attendance dashboard.</p>
      )}
      {!studentId && !loading && branchId && students.length === 0 && (
        <p style={styles.muted}>No students found for selected branch.</p>
      )}
      </div>
    </div>
  )
}

const styles: Record<string, React.CSSProperties> = {
  wrap: { padding: 0 },
  erpShell: {
    background: '#fff',
    border: '1px solid #e2e8f0',
    borderRadius: 12,
    padding: 20,
    boxShadow: '0 1px 3px rgba(0,0,0,0.06)',
  },
  pageHead: {
    display: 'flex',
    alignItems: 'flex-start',
    justifyContent: 'space-between',
    gap: 12,
    flexWrap: 'wrap',
    marginBottom: 8,
  },
  title: { margin: 0, color: '#1e3a5f', fontSize: 24, fontWeight: 700 },
  subtitle: { margin: '8px 0 8px', color: '#64748b', fontSize: 14 },
  selectedBadge: {
    border: '1px solid #dbeafe',
    background: '#f8fbff',
    borderRadius: 10,
    padding: '10px 12px',
    minWidth: 220,
  },
  selectedLabel: { fontSize: 12, color: '#64748b' },
  selectedName: { fontSize: 15, fontWeight: 700, color: '#1e3a5f', marginTop: 2 },
  selectedMeta: { fontSize: 12, color: '#64748b', marginTop: 4 },
  muted: { color: '#666', fontSize: 14, marginBottom: 12 },
  links: { marginBottom: 16 },
  filtersCard: {
    border: '1px solid #e2e8f0',
    background: '#f8fafc',
    borderRadius: 10,
    padding: 12,
    marginBottom: 16,
  },
  filters: { display: 'flex', gap: 24, flexWrap: 'wrap', marginBottom: 24 },
  label: { display: 'flex', flexDirection: 'column', gap: 4, fontSize: 14, fontWeight: 500 },
  select: { padding: 8, border: '1px solid #ccc', borderRadius: 6, minWidth: 200 },
  input: { padding: 8, border: '1px solid #ccc', borderRadius: 6, minWidth: 130 },
  rollRow: { display: 'flex', alignItems: 'center', gap: 8 },
  searchBtn: { background: '#1e3a5f', color: '#fff', border: 'none', borderRadius: 6, padding: '8px 12px', cursor: 'pointer' },
  section: { marginBottom: 28 },
  sectionTitle: { margin: '0 0 12px', fontSize: 18, color: '#0f172a' },
  subSectionTitle: { marginTop: 20, marginBottom: 12, fontSize: 17, color: '#0f172a' },
  cards: { display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(140px, 1fr))', gap: '16px', marginTop: 8 },
  analyticsRow: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(170px, 1fr))',
    gap: 12,
    marginBottom: 14,
  },
  analyticsCard: {
    border: '1px solid #e2e8f0',
    background: '#f8fafc',
    borderRadius: 10,
    padding: '12px 14px',
  },
  analyticsTitle: { fontSize: 12, color: '#64748b', marginBottom: 6 },
  analyticsValue: { fontSize: 22, fontWeight: 800, lineHeight: 1.2 },
  card: { background: '#fff', padding: 20, borderRadius: 10, border: '1px solid #e2e8f0', boxShadow: '0 1px 2px rgba(0,0,0,0.05)' },
  cardValue: { fontSize: 24, fontWeight: 700, color: '#1e3a5f' },
  cardLabel: { fontSize: 13, color: '#666', marginTop: 4 },
  chartWrap: {
    display: 'flex',
    alignItems: 'flex-end',
    gap: 10,
    padding: '12px 14px',
    border: '1px solid #e2e8f0',
    borderRadius: 8,
    background: '#fff',
    marginBottom: 16,
    overflowX: 'auto',
  },
  chartTopRow: { display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: 16, flexWrap: 'wrap', marginBottom: 12 },
  chartModeButtons: { display: 'flex', gap: 8, flexWrap: 'wrap' },
  chartModeBtn: {
    border: '1px solid #cbd5e1',
    background: '#fff',
    color: '#334155',
    borderRadius: 999,
    padding: '8px 14px',
    fontSize: 13,
    cursor: 'pointer',
  },
  chartModeBtnActive: { background: '#1e3a5f', color: '#fff', borderColor: '#1e3a5f' },
  chartHint: { fontSize: 12, color: '#64748b', fontWeight: 600 },
  chartCustomRow: { display: 'flex', gap: 12, flexWrap: 'wrap', marginBottom: 12 },
  customPlotButtons: { display: 'flex', gap: 8, flexWrap: 'wrap', marginBottom: 12 },
  dailyChartWrap: {
    display: 'flex',
    alignItems: 'flex-end',
    gap: 8,
    padding: '12px 14px',
    border: '1px solid #e2e8f0',
    borderRadius: 8,
    background: '#fff',
    marginBottom: 16,
    overflowX: 'auto',
  },
  dailyBarItem: { minWidth: 32, display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 6 },
  dailyBarTrack: {
    width: 16,
    height: 110,
    borderRadius: 6,
    background: '#eef2f7',
    display: 'flex',
    alignItems: 'flex-end',
    overflow: 'hidden',
  },
  dailyBarFill: { width: '100%', borderRadius: 6 },
  dailyBarLabel: { fontSize: 10, color: '#64748b', textAlign: 'center' },
  percentPill: {
    display: 'inline-flex',
    alignItems: 'center',
    justifyContent: 'center',
    minWidth: 64,
    padding: '4px 10px',
    borderRadius: 999,
    border: '1px solid #cbd5e1',
    fontSize: 12,
    fontWeight: 700,
  },
  table: { borderCollapse: 'collapse', background: '#fff', borderRadius: 8, overflow: 'hidden', boxShadow: '0 1px 2px rgba(0,0,0,0.08)', width: '100%' },
  th: { textAlign: 'left', padding: '10px 12px', background: '#1e3a5f', color: '#fff', fontSize: 14 },
  td: { padding: '10px 12px', borderBottom: '1px solid #eee' },
  error: { color: '#c00' },
}
