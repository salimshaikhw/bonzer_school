import axios from 'axios'
import { useEffect, useState, type CSSProperties } from 'react'
import { Link } from 'react-router-dom'
import { ROUTES } from '../../../config/routes'
import { downloadBlobAsFile, Spinner } from '../../../shared'
import { useOrgHierarchy } from '../../../layouts/OrgHierarchyProvider'
import { attendanceApi } from '../api/attendance.api'
import { DivisionTemplateExcelImportPanel } from '../components/DivisionTemplateExcelImportPanel'

function toYmd(d: Date): string {
  const y = d.getFullYear()
  const m = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  return `${y}-${m}-${day}`
}

function applyDatePreset(preset: string, customStart: string, customEnd: string): { start: string; end: string } {
  const today = new Date()
  today.setHours(0, 0, 0, 0)
  if (preset === 'custom') {
    return { start: customStart || toYmd(today), end: customEnd || toYmd(today) }
  }
  if (preset === 'today') {
    const y = toYmd(today)
    return { start: y, end: y }
  }
  if (preset === 'yesterday') {
    const d = new Date(today)
    d.setDate(d.getDate() - 1)
    const y = toYmd(d)
    return { start: y, end: y }
  }
  if (preset === 'last7') {
    const end = new Date(today)
    const start = new Date(today)
    start.setDate(start.getDate() - 6)
    return { start: toYmd(start), end: toYmd(end) }
  }
  if (preset === 'last14') {
    const end = new Date(today)
    const start = new Date(today)
    start.setDate(start.getDate() - 13)
    return { start: toYmd(start), end: toYmd(end) }
  }
  if (preset === 'thisMonth') {
    const start = new Date(today.getFullYear(), today.getMonth(), 1)
    return { start: toYmd(start), end: toYmd(today) }
  }
  const end = new Date(today)
  const start = new Date(today)
  start.setDate(start.getDate() - 6)
  return { start: toYmd(start), end: toYmd(end) }
}

function filenameSafeSegment(value: string): string {
  const t = value.replace(/[\\/:*?"<>|]+/g, '_').replace(/\s+/g, '_').trim()
  return (t || 'export').slice(0, 48)
}

function apiErrorMessage(err: unknown, fallback: string): string {
  if (axios.isAxiosError(err)) {
    const d = err.response?.data
    if (typeof d === 'string' && d.trim()) return d
    if (d && typeof d === 'object') {
      const o = d as Record<string, unknown>
      for (const key of ['userMessage', 'title', 'message', 'detail'] as const) {
        const v = o[key]
        if (typeof v === 'string' && v.trim()) return v
      }
    }
    if (err.message) return err.message
  }
  if (err instanceof Error && err.message) return err.message
  return fallback
}

const PRESET_OPTIONS: { value: string; label: string }[] = [
  { value: 'today', label: 'Today only' },
  { value: 'yesterday', label: 'Yesterday only' },
  { value: 'last7', label: 'Last 7 days' },
  { value: 'last14', label: 'Last 14 days' },
  { value: 'thisMonth', label: 'This month (start → today)' },
  { value: 'custom', label: 'Custom (use dates below)' },
]

export function AttendanceDivisionExcelPage() {
  const { ready: orgReady, branchId, classId, divisionId, branches, classes, divisions } = useOrgHierarchy()
  const [preset, setPreset] = useState('last7')
  const [startDate, setStartDate] = useState('')
  const [endDate, setEndDate] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [info, setInfo] = useState<string | null>(null)

  useEffect(() => {
    const { start, end } = applyDatePreset(preset, startDate, endDate)
    if (preset !== 'custom') {
      setStartDate(start)
      setEndDate(end)
    }
  }, [preset])

  async function downloadExcel() {
    if (!branchId || !classId || !divisionId) {
      setError('Select branch, class, and division.')
      return
    }
    const { start, end } = applyDatePreset(preset, startDate, endDate)
    if (!start || !end) {
      setError('Choose a valid date range.')
      return
    }
    setBusy(true)
    setError(null)
    setInfo(null)
    try {
      const blob = await attendanceApi.downloadDivisionAttendanceTemplate({
        branchId,
        classId,
        divisionId,
        startDate: start,
        endDate: end,
      })
      const bName = branches.find((x) => x.id === branchId)?.name ?? 'branch'
      const cName = classes.find((x) => x.id === classId)?.name ?? 'class'
      const dName = divisions.find((x) => x.id === divisionId)?.name ?? 'division'
      const fn = `attendance_${filenameSafeSegment(bName)}_${filenameSafeSegment(cName)}_${filenameSafeSegment(dName)}_${start}_${end}.xlsx`
      downloadBlobAsFile(blob, fn)
      setInfo('Workbook downloaded. Set Status (e.g. Present/Absent) for each row, then use Upload filled workbook below.')
    } catch (e) {
      setError(apiErrorMessage(e, 'Download failed.'))
    } finally {
      setBusy(false)
    }
  }

  if (!orgReady) {
    return (
      <p>
        <Spinner /> Loading…
      </p>
    )
  }

  return (
    <div style={styles.wrap}>
      <h2>Division attendance Excel</h2>
      <p style={styles.muted}>
        Use the <strong>organization tree</strong> on the left to choose school, class, and division, then pick a date range. The API builds an Excel file with class name/id, division name/id,
        class teacher, roll number, student name, <strong>StudentId</strong> (for import resolution), one row per day per student, plus{' '}
        <strong>Status</strong> (required to mark), <strong>LeaveType</strong>, <strong>IsHoliday</strong>, <strong>Remark</strong>. Approved
        leaves in that range are pre-filled as Status <code>Leave</code> with leave type. Fill remaining statuses, then use{' '}
        <strong>Upload filled workbook</strong> below, or open <Link to={ROUTES.ATTENDANCE_BULK_IMPORT}>Bulk import</Link> for other spreadsheets.
      </p>
      <p style={styles.links}>
        <Link to={ROUTES.ATTENDANCE}>Attendance</Link>
        {' · '}
        <Link to={ROUTES.ATTENDANCE_BULK_IMPORT}>Bulk import</Link>
      </p>

      <div style={styles.filters}>
        <label style={styles.label}>
          Date range preset
          <select value={preset} onChange={(e) => setPreset(e.target.value)} style={styles.select}>
            {PRESET_OPTIONS.map((o) => (
              <option key={o.value} value={o.value}>
                {o.label}
              </option>
            ))}
          </select>
        </label>
        <label style={styles.label}>
          Start date
          <input
            type="date"
            value={startDate}
            onChange={(e) => {
              setPreset('custom')
              setStartDate(e.target.value)
            }}
            style={styles.select}
            disabled={preset !== 'custom'}
          />
        </label>
        <label style={styles.label}>
          End date
          <input
            type="date"
            value={endDate}
            onChange={(e) => {
              setPreset('custom')
              setEndDate(e.target.value)
            }}
            style={styles.select}
            disabled={preset !== 'custom'}
          />
        </label>
      </div>
      {preset !== 'custom' && (
        <p style={styles.muted}>
          Using <strong>{startDate}</strong> → <strong>{endDate}</strong> from preset. Choose “Custom” to edit dates manually.
        </p>
      )}

      <button type="button" style={styles.btn} onClick={() => void downloadExcel()} disabled={busy || !branchId || !classId || !divisionId}>
        {busy ? '…' : 'Download Excel'}
      </button>

      {branchId && classId && divisionId && (
        <DivisionTemplateExcelImportPanel branchId={branchId} classId={classId} divisionId={divisionId} disabled={busy} />
      )}

      {error && <p style={styles.error}>{error}</p>}
      {info && <p style={styles.ok}>{info}</p>}
    </div>
  )
}

const styles: Record<string, CSSProperties> = {
  wrap: { padding: 0, maxWidth: 1200 },
  muted: { color: '#666', fontSize: 14, marginBottom: 12 },
  links: { marginBottom: 16 },
  filters: { display: 'flex', gap: 24, flexWrap: 'wrap', marginBottom: 16 },
  label: { display: 'flex', flexDirection: 'column', gap: 4, fontSize: 14, fontWeight: 500 },
  select: { padding: 8, border: '1px solid #ccc', borderRadius: 6, minWidth: 200 },
  btn: {
    padding: '10px 20px',
    background: '#1e3a5f',
    color: '#fff',
    border: 'none',
    borderRadius: 6,
    cursor: 'pointer',
    marginTop: 8,
  },
  error: { color: '#c00', marginTop: 12 },
  ok: { color: '#063', marginTop: 12 },
}
