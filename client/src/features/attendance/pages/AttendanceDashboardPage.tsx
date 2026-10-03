import axios from 'axios'
import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { ROUTES } from '../../../config/routes'
import { downloadBlobAsFile, Spinner } from '../../../shared'
import { useOrgHierarchy } from '../../../layouts/OrgHierarchyProvider'
import { studentsApi } from '../../admission/api/students.api'
import { attendanceApi } from '../api/attendance.api'
import { holidayManagementApi } from '../api/holidayManagement.api'
import { DivisionTemplateExcelImportPanel } from '../components/DivisionTemplateExcelImportPanel'
import type { AttendanceSummary, AttendanceTrendPoint, AttendancePeriodType, AttendanceRecord } from '../attendance.types'

type Level = 'branch' | 'class' | 'division' | 'student'
type StudentPick = { id: string; firstName?: string; lastName?: string; admissionNo?: string; rollNumber?: string }
type MonthlyAnalyticsItem = { monthKey: string; monthLabel: string; presentDays: number; workingDays: number; percentage: number }
type ChartMode = 'Week' | 'Month' | 'Custom'
type ChartBar = { key: string; label: string; value: number; status: string; color: string }
type CustomAggregate = 'Daily' | 'Weekly' | 'Monthly'

const PERIOD_OPTIONS: { value: AttendancePeriodType; label: string }[] = [
  { value: 'Week', label: 'Weekly' },
  { value: 'Month', label: 'Monthly' },
  { value: 'Custom', label: 'Custom' },
]

const DATE_QUICK_OPTIONS: { value: string; label: string }[] = [
  { value: 'manual', label: 'Manual (edit dates below)' },
  { value: 'today', label: 'Today' },
  { value: 'yesterday', label: 'Yesterday' },
  { value: 'last7', label: 'Last 7 days' },
  { value: 'last14', label: 'Last 14 days' },
  { value: 'thisMonth', label: 'This month (1st → today)' },
]

function applyQuickDateRange(value: string, currentStart: string, currentEnd: string): { start: string; end: string } {
  const today = new Date()
  today.setHours(0, 0, 0, 0)
  if (value === 'manual') {
    return { start: currentStart || toDateInputValue(today), end: currentEnd || toDateInputValue(today) }
  }
  if (value === 'today') {
    const y = toDateInputValue(today)
    return { start: y, end: y }
  }
  if (value === 'yesterday') {
    const d = new Date(today)
    d.setDate(d.getDate() - 1)
    const y = toDateInputValue(d)
    return { start: y, end: y }
  }
  if (value === 'last7') {
    const end = new Date(today)
    const start = new Date(today)
    start.setDate(start.getDate() - 6)
    return { start: toDateInputValue(start), end: toDateInputValue(end) }
  }
  if (value === 'last14') {
    const end = new Date(today)
    const start = new Date(today)
    start.setDate(start.getDate() - 13)
    return { start: toDateInputValue(start), end: toDateInputValue(end) }
  }
  if (value === 'thisMonth') {
    const start = new Date(today.getFullYear(), today.getMonth(), 1)
    return { start: toDateInputValue(start), end: toDateInputValue(today) }
  }
  return { start: currentStart, end: currentEnd }
}

function toDateInputValue(date: Date): string { return date.toISOString().slice(0, 10) }

/** Normalize API/ISO date to yyyy-MM-dd for holiday map lookup */
function normalizeYmd(value: string | undefined): string {
  if (!value) return ''
  const s = String(value).trim()
  const m = s.match(/^(\d{4}-\d{2}-\d{2})/)
  return m ? m[1]! : s.length >= 10 ? s.slice(0, 10) : ''
}
function buildDefaultCustomRange() {
  const end = new Date(); const start = new Date(); start.setDate(end.getDate() - 29)
  return { start: toDateInputValue(start), end: toDateInputValue(end) }
}
function normalizeRoll(value: string | undefined): string {
  if (!value) return ''
  const t = value.trim(); if (!t) return ''
  const n = Number(t); if (Number.isFinite(n) && n >= 0) return String(n)
  return t.toLowerCase()
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
function buildMonthlyAnalytics(trend: AttendanceTrendPoint[]): MonthlyAnalyticsItem[] {
  const monthMap = new Map<string, { monthLabel: string; presentDays: number; workingDays: number }>()
  const presentStatuses = new Set(['Present', 'Late', 'HalfDay'])
  const nonWorkingStatuses = new Set(['Sunday', 'Holiday', 'WeeklyOff', 'Off'])
  for (const row of trend) {
    if (!row.date) continue
    const d = new Date(row.date); if (Number.isNaN(d.getTime())) continue
    const monthKey = `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}`
    const monthLabel = d.toLocaleString(undefined, { month: 'short', year: 'numeric' })
    const status = (row.status ?? '').trim()
    const agg = monthMap.get(monthKey) ?? { monthLabel, presentDays: 0, workingDays: 0 }
    if (!nonWorkingStatuses.has(status)) { agg.workingDays += 1; if (presentStatuses.has(status)) agg.presentDays += 1 }
    monthMap.set(monthKey, agg)
  }
  return Array.from(monthMap.entries())
    .map(([monthKey, value]) => ({
      monthKey, monthLabel: value.monthLabel, presentDays: value.presentDays, workingDays: value.workingDays,
      percentage: value.workingDays > 0 ? (value.presentDays / value.workingDays) * 100 : 0,
    }))
    .sort((a, b) => b.monthKey.localeCompare(a.monthKey))
}
function getColor(p: number): string { if (p >= 90) return '#16a34a'; if (p >= 75) return '#f59e0b'; return '#dc2626' }

function filenameSafeSegment(value: string): string {
  const t = value.replace(/[\\/:*?"<>|]+/g, '_').replace(/\s+/g, '_').trim()
  return (t || 'export').slice(0, 48)
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

function getPresenceWeight(status: string | undefined): number {
  const s = (status ?? '').trim().toLowerCase()
  if (s === 'present' || s === 'late') return 1
  if (s === 'halfday') return 0.5
  return 0
}

function isWorkingStatus(status: string | undefined): boolean {
  const s = (status ?? '').trim().toLowerCase()
  return !new Set(['sunday', 'holiday', 'weeklyoff', 'off']).has(s)
}

function getLastWeekRange(): { startDate: string; endDate: string } {
  const today = new Date()
  const day = today.getDay()
  const daysFromMonday = (day + 6) % 7
  const currentMonday = new Date(today)
  currentMonday.setDate(today.getDate() - daysFromMonday)
  const lastMonday = new Date(currentMonday)
  lastMonday.setDate(currentMonday.getDate() - 7)
  const lastSunday = new Date(lastMonday)
  lastSunday.setDate(lastMonday.getDate() + 6)
  return { startDate: toDateInputValue(lastMonday), endDate: toDateInputValue(lastSunday) }
}

function getLastMonthRange(): { startDate: string; endDate: string } {
  const today = new Date()
  const firstDayCurrentMonth = new Date(today.getFullYear(), today.getMonth(), 1)
  const lastDayLastMonth = new Date(firstDayCurrentMonth)
  lastDayLastMonth.setDate(0)
  const firstDayLastMonth = new Date(lastDayLastMonth.getFullYear(), lastDayLastMonth.getMonth(), 1)
  return { startDate: toDateInputValue(firstDayLastMonth), endDate: toDateInputValue(lastDayLastMonth) }
}

function getWeekKey(date: Date): { key: string; label: string } {
  const d = new Date(date)
  const day = (d.getDay() + 6) % 7
  d.setDate(d.getDate() - day)
  const year = d.getFullYear()
  const start = new Date(year, 0, 1)
  const week = Math.ceil((((d.getTime() - start.getTime()) / 86400000) + 1) / 7)
  return { key: `${year}-W${String(week).padStart(2, '0')}`, label: `W${week} ${year}` }
}

function buildBarsFromRecords(records: AttendanceRecord[], mode: ChartMode, customAggregate: CustomAggregate): ChartBar[] {
  const sorted = [...records].sort((a, b) => String(a.attendanceDate ?? '').localeCompare(String(b.attendanceDate ?? '')))
  if (mode !== 'Custom' || customAggregate === 'Daily') {
    const cap = mode === 'Week' ? sorted.slice(-7) : mode === 'Month' ? sorted.slice(-31) : sorted
    return cap.map((r, i) => {
      const d = r.attendanceDate ? new Date(r.attendanceDate) : null
      const label = d && !Number.isNaN(d.getTime())
        ? `${String(d.getDate()).padStart(2, '0')}/${String(d.getMonth() + 1).padStart(2, '0')}`
        : String(r.attendanceDate ?? i)
      return {
        key: `${r.id}-${i}`,
        label,
        value: Math.max(1, getStatusScore(r.status)),
        status: r.status ?? 'Absent',
        color: getStatusColor(r.status),
      }
    })
  }

  const grouped = new Map<string, { label: string; present: number; working: number }>()
  for (const r of sorted) {
    if (!r.attendanceDate) continue
    const d = new Date(r.attendanceDate)
    if (Number.isNaN(d.getTime())) continue
    const bucket = customAggregate === 'Weekly'
      ? getWeekKey(d)
      : { key: `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}`, label: d.toLocaleString(undefined, { month: 'short', year: 'numeric' }) }
    const row = grouped.get(bucket.key) ?? { label: bucket.label, present: 0, working: 0 }
    if (isWorkingStatus(r.status)) {
      row.working += 1
      row.present += getPresenceWeight(r.status)
    }
    grouped.set(bucket.key, row)
  }

  return Array.from(grouped.entries())
    .sort((a, b) => a[0].localeCompare(b[0]))
    .map(([key, row]) => {
      const pct = row.working > 0 ? (row.present / row.working) * 100 : 0
      return {
        key,
        label: row.label,
        value: Math.max(1, Math.min(100, pct)),
        status: `${Math.round(row.present * 10) / 10}/${row.working} present`,
        color: getColor(pct),
      }
    })
}

export function AttendanceDashboardPage() {
  const {
    ready: orgReady,
    branchId,
    classId,
    divisionId,
    branches,
    classes,
    divisions,
    setOrgSelection,
  } = useOrgHierarchy()
  const [students, setStudents] = useState<StudentPick[]>([])
  const [studentId, setStudentId] = useState('')
  const [rollNo, setRollNo] = useState('')

  const [periodType, setPeriodType] = useState<AttendancePeriodType>('Week')
  const [customRange, setCustomRange] = useState(buildDefaultCustomRange())
  const [dateQuickFill, setDateQuickFill] = useState('last7')

  const [summary, setSummary] = useState<AttendanceSummary | null>(null)
  const [percentage, setPercentage] = useState<number | null>(null)
  const [trend, setTrend] = useState<AttendanceTrendPoint[]>([])
  const [records, setRecords] = useState<AttendanceRecord[]>([])
  const [monthlyAnalytics, setMonthlyAnalytics] = useState<MonthlyAnalyticsItem[]>([])
  const [chartMode, setChartMode] = useState<ChartMode>('Week')
  const [chartCustomRange, setChartCustomRange] = useState(buildDefaultCustomRange())
  const [customAggregate, setCustomAggregate] = useState<CustomAggregate>('Daily')
  const [chartBars, setChartBars] = useState<ChartBar[]>([])
  const [chartOverall, setChartOverall] = useState<number | null>(null)
  const [chartLoading, setChartLoading] = useState(false)

  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [excelBusy, setExcelBusy] = useState(false)
  const [excelInfo, setExcelInfo] = useState<string | null>(null)
  const [attendanceRefreshKey, setAttendanceRefreshKey] = useState(0)
  const [holidayByDate, setHolidayByDate] = useState<Map<string, string>>(() => new Map())

  const level: Level = studentId ? 'student' : divisionId ? 'division' : classId ? 'class' : 'branch'

  const selectedStudent = useMemo(() => students.find((s) => s.id === studentId) ?? null, [students, studentId])
  const selectedStudentName = selectedStudent ? [selectedStudent.firstName, selectedStudent.lastName].filter(Boolean).join(' ') || selectedStudent.id.slice(0, 8) : '—'
  const selectedBranchName = branches.find((b) => b.id === branchId)?.name ?? branchId
  const selectedClassName = classes.find((c) => c.id === classId)?.name ?? classId
  const selectedDivisionName = divisions.find((d) => d.id === divisionId)?.name ?? divisionId

  const holidayFetchRange = useMemo(() => {
    if (!branchId || !customRange.start || !customRange.end) return null
    let from = customRange.start
    let to = customRange.end
    const pool: string[] = []
    for (const t of trend) {
      const y = normalizeYmd(t.date)
      if (y) pool.push(y)
    }
    for (const r of records) {
      const y = normalizeYmd(r.attendanceDate)
      if (y) pool.push(y)
    }
    if (pool.length > 0) {
      const sorted = [...new Set(pool)].sort()
      if (sorted[0]! < from) from = sorted[0]!
      const last = sorted[sorted.length - 1]!
      if (last > to) to = last
    }
    return { from, to }
  }, [branchId, customRange.start, customRange.end, trend, records])

  useEffect(() => {
    if (!holidayFetchRange || !branchId) {
      setHolidayByDate(new Map())
      return
    }
    let cancelled = false
    holidayManagementApi
      .listHolidays(branchId, { from: holidayFetchRange.from, to: holidayFetchRange.to })
      .then((list) => {
        if (cancelled) return
        const m = new Map<string, string>()
        for (const h of list) {
          const d = normalizeYmd(h.holidayDate)
          if (!d) continue
          const label = h.name?.trim() ? h.name.trim() : 'Holiday'
          m.set(d, label)
        }
        setHolidayByDate(m)
      })
      .catch(() => {
        if (!cancelled) setHolidayByDate(new Map())
      })
    return () => {
      cancelled = true
    }
  }, [branchId, holidayFetchRange?.from, holidayFetchRange?.to])

  const isSingleSelectedDay =
    !!customRange.start && !!customRange.end && customRange.start === customRange.end
  const singleDayHolidayName = isSingleSelectedDay ? holidayByDate.get(customRange.start) : undefined

  useEffect(() => {
    setStudentId('')
    setRollNo('')
    setStudents([])
    if (!branchId) return
    studentsApi
      .getByBranch(branchId)
      .then((list) => {
        const mapped = (Array.isArray(list) ? list : []).map(mapStudentPick).filter((s): s is StudentPick => !!s)
        setStudents(mapped)
      })
      .catch(() => setError('Failed to load students for branch'))
  }, [branchId])

  useEffect(() => {
    setStudentId('')
    setRollNo('')
  }, [classId, divisionId])

  useEffect(() => {
    if (dateQuickFill === 'manual') return
    const { start, end } = applyQuickDateRange(dateQuickFill, customRange.start, customRange.end)
    setCustomRange({ start, end })
  }, [dateQuickFill])

  async function downloadDivisionExcel() {
    if (!branchId || !classId || !divisionId) {
      setError('Select branch, class, and division to download the Excel template.')
      return
    }
    if (!customRange.start || !customRange.end) {
      setError('Set start and end dates.')
      return
    }
    setExcelBusy(true)
    setError(null)
    setExcelInfo(null)
    try {
      const blob = await attendanceApi.downloadDivisionAttendanceTemplate({
        branchId,
        classId,
        divisionId,
        startDate: customRange.start,
        endDate: customRange.end,
      })
      const fn = `attendance_${filenameSafeSegment(selectedClassName)}_${filenameSafeSegment(selectedDivisionName)}_${customRange.start}_${customRange.end}.xlsx`
      downloadBlobAsFile(blob, fn)
      setExcelInfo('Excel downloaded. Fill Status in the sheet, then upload below (Preview → Import).')
    } catch (e) {
      if (axios.isAxiosError(e)) {
        const d = e.response?.data
        let msg = 'Download failed.'
        if (typeof d === 'string' && d.trim()) msg = d
        else if (d && typeof d === 'object' && !(d instanceof Blob)) {
          const o = d as Record<string, unknown>
          const m = o.message ?? o.detail ?? o.title ?? o.validationMessage ?? o.ValidationMessage
          if (typeof m === 'string' && m.trim()) msg = m
        }
        if (msg === 'Download failed.' && e.message) msg = e.message
        setError(msg)
      } else if (e instanceof Error && e.message) {
        setError(e.message)
      } else setError('Download failed.')
    } finally {
      setExcelBusy(false)
    }
  }

  function handleFindByRoll() {
    const q = normalizeRoll(rollNo); if (!q) return
    const found = students.find((s) => normalizeRoll(s.rollNumber) === q) ?? students.find((s) => normalizeRoll(s.rollNumber).includes(q))
    if (found) { setStudentId(found.id); setError(null) }
    else setError('No student found for this roll number in selected branch.')
  }

  useEffect(() => {
    if (!branchId) return
    if (periodType === 'Custom' && (!customRange.start || !customRange.end)) return
    setLoading(true); setError(null)
    setSummary(null); setPercentage(null); setTrend([]); setRecords([]); setMonthlyAnalytics([])

    const startDate = periodType === 'Custom' ? customRange.start : undefined
    const endDate = periodType === 'Custom' ? customRange.end : undefined

    let run: Promise<void>
    if (level === 'student' && studentId) {
      run = Promise.all([
        attendanceApi.getStudentSummary(studentId, periodType, startDate, endDate),
        periodType === 'Custom' ? Promise.resolve<number | null>(null) : attendanceApi.getStudentPercentage(studentId, periodType),
        attendanceApi.getStudentTrend(studentId, periodType),
        attendanceApi.getStudentReport(studentId, periodType, startDate, endDate),
        attendanceApi.getStudentTrend(studentId, 'AcademicYear'),
      ]).then(([sum, pct, tr, rep, yearTrend]) => {
        setSummary(sum); setPercentage(pct ?? sum.percentage ?? null); setTrend(tr)
        setRecords(rep.detailedList ?? []); setMonthlyAnalytics(buildMonthlyAnalytics(yearTrend))
      })
    } else if (level === 'division' && divisionId) {
      run = Promise.all([
        attendanceApi.getDivisionSummary(divisionId, periodType, startDate, endDate),
        periodType === 'Custom' ? Promise.resolve<number | null>(null) : attendanceApi.getDivisionPercentage(divisionId, periodType),
        attendanceApi.getDivisionTrend(divisionId, periodType),
      ]).then(([sum, pct, tr]) => { setSummary(sum); setPercentage(pct ?? sum.percentage ?? null); setTrend(tr) })
    } else if (level === 'class' && classId) {
      run = Promise.all([
        attendanceApi.getClassSummary(classId, periodType, startDate, endDate),
        periodType === 'Custom' ? Promise.resolve<number | null>(null) : attendanceApi.getClassPercentage(classId, periodType),
        attendanceApi.getClassTrend(classId, periodType),
      ]).then(([sum, pct, tr]) => { setSummary(sum); setPercentage(pct ?? sum.percentage ?? null); setTrend(tr) })
    } else {
      run = Promise.all([
        attendanceApi.getBranchSummary(branchId, periodType),
        attendanceApi.getBranchTrend(branchId, periodType),
      ]).then(([sum, tr]) => { setSummary(sum); setPercentage(sum.percentage ?? null); setTrend(tr) })
    }

    run.catch(() => setError('Failed to load attendance data')).finally(() => setLoading(false))
  }, [branchId, classId, divisionId, studentId, level, periodType, customRange.start, customRange.end, attendanceRefreshKey])

  useEffect(() => {
    if (level !== 'student' || !studentId) return
    if (chartMode === 'Custom' && (!chartCustomRange.start || !chartCustomRange.end)) return
    setChartLoading(true)
    const range = chartMode === 'Week'
      ? getLastWeekRange()
      : chartMode === 'Month'
        ? getLastMonthRange()
        : { startDate: chartCustomRange.start, endDate: chartCustomRange.end }

    Promise.all([
      attendanceApi.getStudentReport(studentId, 'Custom', range.startDate, range.endDate),
      attendanceApi.getStudentSummary(studentId, 'Custom', range.startDate, range.endDate).then((s) => s.percentage),
    ])
      .then(([report, pct]) => {
        const list = report?.detailedList ?? []
        setChartBars(buildBarsFromRecords(list, chartMode, customAggregate))
        setChartOverall(pct ?? null)
      })
      .catch(() => {
        setChartBars([])
        setChartOverall(null)
      })
      .finally(() => setChartLoading(false))
  }, [level, studentId, chartMode, chartCustomRange.start, chartCustomRange.end, customAggregate])

  if (!orgReady) return <p><Spinner /> Loading…</p>

  const headerLabel = level === 'student'
    ? `Student Attendance (${selectedStudentName})`
    : level === 'division'
      ? `Division Attendance`
      : level === 'class'
        ? `Class Attendance`
        : 'Branch Attendance'

  const chips: string[] = []
  if (selectedBranchName) chips.push(`Branch: ${selectedBranchName}`)
  if (classId) chips.push(`Class: ${selectedClassName}`)
  if (divisionId) chips.push(`Division: ${selectedDivisionName}`)
  if (studentId) chips.push(`Student: ${selectedStudentName}`)

  function resetToBranchView() {
    setOrgSelection({ groupScopeId: '', branchId, classId: '', divisionId: '' })
    setStudentId('')
    setRollNo('')
    setError(null)
  }

  return (
    <div style={styles.wrap}>
      <h2 style={styles.title}>Attendance ERP Dashboard</h2>
      <p style={styles.subtitle}>
        Use the <strong>organization tree</strong> on the left to choose branch, class, and division. Then set dates, optional student, and period for analytics.
      </p>
      <p style={styles.links}>
        <Link to={ROUTES.ATTENDANCE_DIVISION}>
          <strong>Mark attendance (class / division grid)</strong>
        </Link>
        {' · '}
        <Link to={ROUTES.ATTENDANCE_HOLIDAYS}>Holiday management</Link>
        {' · '}
        <Link to={ROUTES.ATTENDANCE_BULK_IMPORT}>Bulk import (Excel)</Link>
        {' · '}
        <Link to={ROUTES.ATTENDANCE_DIVISION_EXCEL}>Division Excel (full-page)</Link>
      </p>

      <div style={styles.levelRow}>
        <div style={styles.levelChips}>
          {chips.length === 0 && <span style={styles.levelChip}>Branch level</span>}
          {chips.map((c) => (
            <span key={c} style={styles.levelChip}>{c}</span>
          ))}
        </div>
        {(classId || divisionId || studentId) && (
          <button type="button" style={styles.resetBtn} onClick={resetToBranchView}>
            Reset to Branch View
          </button>
        )}
      </div>

      <div style={styles.filtersCard}>
        <div style={styles.filters}>
          <label style={styles.label}>Date range (quick)
            <select
              value={dateQuickFill}
              onChange={(e) => setDateQuickFill(e.target.value)}
              style={styles.select}
            >
              {DATE_QUICK_OPTIONS.map((o) => (
                <option key={o.value} value={o.value}>{o.label}</option>
              ))}
            </select>
          </label>
          <label style={styles.label}>Start date
            <input
              type="date"
              value={customRange.start}
              onChange={(e) => {
                setDateQuickFill('manual')
                setCustomRange((r) => ({ ...r, start: e.target.value }))
              }}
              style={styles.select}
            />
          </label>
          <label style={styles.label}>End date
            <input
              type="date"
              value={customRange.end}
              onChange={(e) => {
                setDateQuickFill('manual')
                setCustomRange((r) => ({ ...r, end: e.target.value }))
              }}
              style={styles.select}
            />
          </label>
          <label style={styles.label}>Student
            <select value={studentId} onChange={(e) => setStudentId(e.target.value)} style={styles.select} disabled={!branchId}>
              <option value="">— Optional student —</option>
              {students.map((s) => <option key={s.id} value={s.id}>{[s.firstName, s.lastName].filter(Boolean).join(' ') || s.admissionNo || s.id.slice(0, 8)}</option>)}
            </select>
          </label>
          <label style={styles.label}>Roll no search
            <div style={styles.rollRow}>
              <input value={rollNo} onChange={(e) => setRollNo(e.target.value)} placeholder="e.g. 0012" style={styles.input} />
              <button type="button" style={styles.searchBtn} onClick={handleFindByRoll}>Find</button>
            </div>
          </label>
          <label style={styles.label}>Period (dashboard)
            <select value={periodType} onChange={(e) => setPeriodType(e.target.value as AttendancePeriodType)} style={styles.select}>
              {PERIOD_OPTIONS.map((p) => <option key={p.value} value={p.value}>{p.label}</option>)}
            </select>
          </label>
        </div>
        <p style={styles.filterHint}>
          <strong>Start / end dates</strong> are always used for the division Excel export below. Choose <strong>Period → Custom</strong> to drive the dashboard summaries from the same dates; <strong>Weekly / Monthly</strong> use the API’s calendar periods instead.
        </p>
        <div style={styles.excelRow}>
          <button
            type="button"
            style={styles.excelBtn}
            onClick={() => void downloadDivisionExcel()}
            disabled={excelBusy || !branchId || !classId || !divisionId}
          >
            {excelBusy ? '…' : 'Download division attendance Excel'}
          </button>
          <Link
            to={ROUTES.ATTENDANCE_DIVISION}
            style={styles.manualMarkLink}
            title="Open the daily grid to mark present / absent / leave for each student"
          >
            Mark attendance manually
          </Link>
          {excelInfo && <span style={styles.excelOk}>{excelInfo}</span>}
        </div>
        {branchId && classId && divisionId && (
          <DivisionTemplateExcelImportPanel
            branchId={branchId}
            classId={classId}
            divisionId={divisionId}
            disabled={excelBusy}
            onImportSuccess={() => {
              setAttendanceRefreshKey((k: number) => k + 1)
              setExcelInfo(null)
            }}
          />
        )}
      </div>

      {branchId && singleDayHolidayName && (
        <div style={styles.holidayBanner} role="status">
          Calendar holiday for selected date ({customRange.start}):{' '}
          <strong>{singleDayHolidayName}</strong>
        </div>
      )}

      {error && <p style={styles.error}>{error}</p>}
      {loading && <p><Spinner /> Loading attendance…</p>}

      {!loading && summary && (
        <section style={styles.section}>
          <h3 style={styles.sectionTitle}>{headerLabel}</h3>
          <div style={styles.analyticsRow}>
            <div style={styles.analyticsCard}>
              <div style={styles.analyticsTitle}>Overall Attendance</div>
              {level === 'student' ? (
                <div style={{ ...styles.roundPct, borderColor: getColor(percentage ?? summary.percentage ?? 0), color: getColor(percentage ?? summary.percentage ?? 0) }}>
                  {(percentage ?? summary.percentage ?? 0).toFixed(1)}%
                </div>
              ) : (
                <div style={{ ...styles.analyticsValue, color: getColor(percentage ?? summary.percentage ?? 0) }}>{(percentage ?? summary.percentage ?? 0).toFixed(1)}%</div>
              )}
            </div>
            <div style={styles.analyticsCard}><div style={styles.analyticsTitle}>Present</div><div style={styles.analyticsValue}>{summary.presentCount}</div></div>
            <div style={styles.analyticsCard}><div style={styles.analyticsTitle}>Absent</div><div style={styles.analyticsValue}>{summary.absentCount}</div></div>
            <div style={styles.analyticsCard}><div style={styles.analyticsTitle}>Leave</div><div style={styles.analyticsValue}>{summary.leaveCount}</div></div>
            <div style={styles.analyticsCard}><div style={styles.analyticsTitle}>Total Days</div><div style={styles.analyticsValue}>{summary.totalDays}</div></div>
          </div>

          {trend.length > 0 && (
            <table style={styles.table}>
              <thead>
                <tr>
                  <th style={styles.th}>Date</th>
                  <th style={styles.th}>Status</th>
                  <th style={styles.th}>Calendar holiday</th>
                </tr>
              </thead>
              <tbody>
                {trend.slice(0, 20).map((t, i) => {
                  const y = normalizeYmd(t.date)
                  const hol = y ? holidayByDate.get(y) : undefined
                  return (
                    <tr key={`${t.date}-${i}`}>
                      <td style={styles.td}>{t.date}</td>
                      <td style={styles.td}>{t.status}</td>
                      <td style={styles.td}>
                        {hol ? <span style={styles.holidayBadge}>{hol}</span> : '—'}
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          )}
        </section>
      )}

      {!loading && level === 'student' && (
        <>
          <section style={styles.section}>
            <h3 style={styles.sectionTitle}>Student Records</h3>
            {records.length === 0 ? <p style={styles.muted}>No detailed records for selected period.</p> : (
              <table style={styles.table}>
                <thead>
                  <tr>
                    <th style={styles.th}>Date</th>
                    <th style={styles.th}>Status</th>
                    <th style={styles.th}>Leave Type</th>
                    <th style={styles.th}>Remark</th>
                    <th style={styles.th}>Calendar holiday</th>
                  </tr>
                </thead>
                <tbody>
                  {records.slice(0, 40).map((r) => {
                    const y = normalizeYmd(r.attendanceDate)
                    const hol = y ? holidayByDate.get(y) : undefined
                    return (
                      <tr key={r.id}>
                        <td style={styles.td}>{r.attendanceDate ?? '—'}</td>
                        <td style={styles.td}>{r.status ?? '—'}</td>
                        <td style={styles.td}>{r.leaveType ?? '—'}</td>
                        <td style={styles.td}>{r.remark ?? '—'}</td>
                        <td style={styles.td}>{hol ? <span style={styles.holidayBadge}>{hol}</span> : '—'}</td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            )}
          </section>

          <section style={styles.section}>
            <h3 style={styles.sectionTitle}>Student Attendance Chart</h3>
            <div style={styles.chartTopRow}>
              <div style={styles.chartModeButtons}>
                {(['Week', 'Month', 'Custom'] as ChartMode[]).map((m) => (
                  <button
                    key={m}
                    type="button"
                    onClick={() => setChartMode(m)}
                    style={{ ...styles.chartModeBtn, ...(chartMode === m ? styles.chartModeBtnActive : {}) }}
                  >
                    {m === 'Week' ? 'Weekly' : m === 'Month' ? 'Monthly' : 'Custom'}
                  </button>
                ))}
              </div>
              <div style={styles.chartHint}>Chart score: {chartOverall != null ? `${chartOverall.toFixed(1)}%` : '—'}</div>
            </div>
            {chartMode === 'Custom' && (
              <div style={styles.chartCustomRow}>
                <label style={styles.label}>From
                  <input type="date" value={chartCustomRange.start} onChange={(e) => setChartCustomRange((r) => ({ ...r, start: e.target.value }))} style={styles.select} />
                </label>
                <label style={styles.label}>To
                  <input type="date" value={chartCustomRange.end} onChange={(e) => setChartCustomRange((r) => ({ ...r, end: e.target.value }))} style={styles.select} />
                </label>
              </div>
            )}
            {chartMode === 'Custom' && (
              <div style={styles.chartModeButtons}>
                {(['Daily', 'Weekly', 'Monthly'] as CustomAggregate[]).map((g) => (
                  <button
                    key={g}
                    type="button"
                    onClick={() => setCustomAggregate(g)}
                    style={{ ...styles.chartModeBtn, ...(customAggregate === g ? styles.chartModeBtnActive : {}) }}
                  >
                    {g}
                  </button>
                ))}
              </div>
            )}
            {chartLoading && <p><Spinner /> Loading chart…</p>}
            {!chartLoading && chartBars.length > 0 && (
              <div style={styles.chartWrap}>
                {chartBars.map((b) => (
                  <div key={b.key} style={styles.barItem} title={`${b.label}: ${b.status}`}>
                    <div style={styles.barTrack}>
                      <div style={{ ...styles.barFill, height: `${Math.max(4, b.value)}%`, background: b.color }} />
                    </div>
                    <div style={styles.barLabel}>{b.label}</div>
                  </div>
                ))}
              </div>
            )}
            {!chartLoading && chartBars.length === 0 && <p style={styles.muted}>No chart data available for selected range.</p>}
          </section>

          <section style={styles.section}>
            <h3 style={styles.sectionTitle}>Student Monthly Analytics</h3>
            {monthlyAnalytics.length === 0 ? <p style={styles.muted}>No monthly analytics available.</p> : (
              <table style={styles.table}>
                <thead><tr><th style={styles.th}>Month</th><th style={styles.th}>Present Days</th><th style={styles.th}>Working Days</th><th style={styles.th}>Attendance %</th></tr></thead>
                <tbody>
                  {monthlyAnalytics.slice(0, 12).map((m) => (
                    <tr key={m.monthKey}><td style={styles.td}>{m.monthLabel}</td><td style={styles.td}>{m.presentDays}</td><td style={styles.td}>{m.workingDays}</td><td style={styles.td}><span style={{ ...styles.percentPill, color: getColor(m.percentage), borderColor: getColor(m.percentage) }}>{m.percentage.toFixed(1)}%</span></td></tr>
                  ))}
                </tbody>
              </table>
            )}
          </section>
        </>
      )}
    </div>
  )
}

const styles: Record<string, React.CSSProperties> = {
  wrap: { padding: 0 },
  title: { margin: 0, color: '#1e3a5f', fontSize: 24, fontWeight: 700 },
  subtitle: { margin: '8px 0 8px', color: '#64748b', fontSize: 14 },
  links: { marginBottom: 16 },
  levelRow: { display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: 10, flexWrap: 'wrap', marginBottom: 12 },
  levelChips: { display: 'flex', gap: 8, flexWrap: 'wrap' },
  levelChip: {
    display: 'inline-flex',
    alignItems: 'center',
    padding: '4px 10px',
    borderRadius: 999,
    border: '1px solid #cbd5e1',
    background: '#fff',
    fontSize: 12,
    fontWeight: 600,
    color: '#334155',
  },
  resetBtn: {
    border: '1px solid #1e3a5f',
    background: '#fff',
    color: '#1e3a5f',
    borderRadius: 8,
    padding: '6px 12px',
    fontSize: 12,
    fontWeight: 700,
    cursor: 'pointer',
  },
  muted: { color: '#666', fontSize: 14, marginBottom: 12 },
  filtersCard: { border: '1px solid #e2e8f0', background: '#f8fafc', borderRadius: 10, padding: 12, marginBottom: 16 },
  filters: { display: 'flex', gap: 16, flexWrap: 'wrap' },
  filterHint: { margin: '10px 0 0', fontSize: 12, color: '#64748b', lineHeight: 1.5 },
  excelRow: { marginTop: 12, display: 'flex', alignItems: 'center', gap: 12, flexWrap: 'wrap' },
  excelBtn: {
    background: '#166534',
    color: '#fff',
    border: 'none',
    borderRadius: 8,
    padding: '10px 16px',
    fontSize: 14,
    fontWeight: 600,
    cursor: 'pointer',
  },
  /** Link styled like a button — navigation always works (a disabled button was ignoring clicks). */
  manualMarkLink: {
    background: '#1e3a5f',
    color: '#fff',
    border: 'none',
    borderRadius: 8,
    padding: '10px 16px',
    fontSize: 14,
    fontWeight: 600,
    cursor: 'pointer',
    textDecoration: 'none',
    display: 'inline-block',
    boxSizing: 'border-box',
  },
  excelOk: { fontSize: 13, color: '#166534' },
  holidayBanner: {
    marginBottom: 16,
    padding: '12px 14px',
    borderRadius: 8,
    border: '1px solid #fca5a5',
    background: '#fff1f2',
    color: '#9f1239',
    fontSize: 14,
  },
  holidayBadge: {
    display: 'inline-block',
    padding: '2px 8px',
    borderRadius: 6,
    background: '#fff1f2',
    color: '#9f1239',
    fontSize: 12,
    fontWeight: 600,
    border: '1px solid #fecdd3',
  },
  label: { display: 'flex', flexDirection: 'column', gap: 4, fontSize: 14, fontWeight: 500 },
  select: { padding: 8, border: '1px solid #ccc', borderRadius: 6, minWidth: 170 },
  input: { padding: 8, border: '1px solid #ccc', borderRadius: 6, minWidth: 120 },
  rollRow: { display: 'flex', alignItems: 'center', gap: 8 },
  searchBtn: { background: '#1e3a5f', color: '#fff', border: 'none', borderRadius: 6, padding: '8px 12px', cursor: 'pointer' },
  section: { marginBottom: 24 },
  sectionTitle: { margin: '0 0 12px', fontSize: 18, color: '#0f172a' },
  analyticsRow: { display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(160px, 1fr))', gap: 12, marginBottom: 12 },
  analyticsCard: { border: '1px solid #e2e8f0', background: '#fff', borderRadius: 10, padding: '10px 12px' },
  analyticsTitle: { fontSize: 12, color: '#64748b', marginBottom: 6 },
  analyticsValue: { fontSize: 22, fontWeight: 800, lineHeight: 1.2, color: '#1e3a5f' },
  roundPct: {
    width: 76,
    height: 76,
    borderRadius: '50%',
    border: '4px solid #1e3a5f',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    fontSize: 16,
    fontWeight: 800,
    marginTop: 4,
    background: '#fff',
  },
  chartTopRow: { display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: 12, flexWrap: 'wrap', marginBottom: 10 },
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
  chartCustomRow: { display: 'flex', gap: 12, flexWrap: 'wrap', marginBottom: 10 },
  chartWrap: {
    display: 'flex',
    alignItems: 'flex-end',
    gap: 8,
    padding: '12px 14px',
    border: '1px solid #e2e8f0',
    borderRadius: 8,
    background: '#fff',
    marginBottom: 12,
    overflowX: 'auto',
  },
  barItem: { minWidth: 34, display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 6 },
  barTrack: {
    width: 16,
    height: 110,
    borderRadius: 6,
    background: '#eef2f7',
    display: 'flex',
    alignItems: 'flex-end',
    overflow: 'hidden',
  },
  barFill: { width: '100%', borderRadius: 6 },
  barLabel: { fontSize: 10, color: '#64748b', textAlign: 'center' },
  percentPill: { display: 'inline-flex', alignItems: 'center', justifyContent: 'center', minWidth: 64, padding: '4px 10px', borderRadius: 999, border: '1px solid #cbd5e1', fontSize: 12, fontWeight: 700 },
  table: { borderCollapse: 'collapse', background: '#fff', borderRadius: 8, overflow: 'hidden', boxShadow: '0 1px 2px rgba(0,0,0,0.08)', width: '100%' },
  th: { textAlign: 'left', padding: '10px 12px', background: '#1e3a5f', color: '#fff', fontSize: 14 },
  td: { padding: '10px 12px', borderBottom: '1px solid #eee' },
  error: { color: '#c00' },
}

