import axios from 'axios'
import { useState, type CSSProperties } from 'react'
import { Link } from 'react-router-dom'
import readXlsxFile from 'read-excel-file/browser'
import { ROUTES } from '../../../config/routes'
import { Spinner } from '../../../shared'
import { useOrgHierarchy } from '../../../layouts/OrgHierarchyProvider'
import {
  attendanceApi,
  type BulkAttendanceImportPreview,
  type BulkAttendanceImportFilePayload,
  type BulkAttendanceImportRequest,
  type BulkAttendanceImportRowInput,
} from '../api/attendance.api'

function normHeader(h: unknown): string {
  return String(h ?? '')
    .trim()
    .toLowerCase()
    .replace(/\s+/g, '')
}

type ColMap = {
  roll?: number
  admission?: number
  enrollment?: number
  studentId?: number
  date?: number
  status?: number
  classId?: number
  divisionName?: number
  leaveType?: number
  isHoliday?: number
  remark?: number
}

function detectColumns(headers: unknown[]): ColMap {
  const idx = (preds: string[]) => {
    for (let i = 0; i < headers.length; i++) {
      const h = normHeader(headers[i])
      if (preds.some((p) => h === p || h.includes(p))) return i
    }
    return undefined
  }
  return {
    roll: idx(['roll', 'rollno', 'rollnumber', 'roll_no', 'rollno.']),
    admission: idx(['admission', 'admissionno', 'admission_no', 'admno', 'admissionnumber']),
    enrollment: idx(['enrollment', 'enrollmentno', 'enrollment_no', 'enrolment', 'machineid', 'userid', 'empcode', 'deviceid']),
    studentId: idx(['studentid', 'student_id', 'guid', 'uuid']),
    date: idx(['attendancedate', 'date', 'punchdate', 'logdate', 'indate', 'time']),
    status: idx(['status', 'present', 'state', 'attendance', 'inout', 'p/a']),
    classId: idx(['classid', 'class_id']),
    divisionName: idx(['divisionname', 'division_name', 'divname']),
    leaveType: idx(['leavetype', 'leave_type', 'leave']),
    isHoliday: idx(['isholiday', 'is_holiday', 'holidayflag']),
    remark: idx(['remark', 'note', 'comments', 'comment']),
  }
}

function cellToYmd(cell: unknown): string | undefined {
  if (cell == null || cell === '') return undefined
  if (cell instanceof Date && !Number.isNaN(cell.getTime())) {
    const y = cell.getFullYear()
    const m = String(cell.getMonth() + 1).padStart(2, '0')
    const d = String(cell.getDate()).padStart(2, '0')
    return `${y}-${m}-${d}`
  }
  const s = String(cell).trim()
  const m = s.match(/^(\d{4})-(\d{2})-(\d{2})/)
  if (m) return `${m[1]}-${m[2]}-${m[3]}`
  const slash = s.match(/^(\d{1,2})[/.-](\d{1,2})[/.-](\d{2,4})$/)
  if (slash) {
    let dd = Number(slash[1])
    let mo = Number(slash[2])
    let yy = Number(slash[3])
    if (yy < 100) yy += 2000
    if (dd > 12 && mo <= 12) {
      /* likely DD/MM */
    } else if (mo > 12 && dd <= 12) {
      const t = dd
      dd = mo
      mo = t
    }
    return `${yy}-${String(mo).padStart(2, '0')}-${String(dd).padStart(2, '0')}`
  }
  return undefined
}

function cellStr(cell: unknown): string | undefined {
  if (cell == null || cell === '') return undefined
  if (cell instanceof Date) return cell.toISOString()
  const t = String(cell).trim()
  return t.length ? t : undefined
}

function cellParseGuid(cell: unknown): string | undefined {
  const raw = cellStr(cell)
  if (!raw) return undefined
  const t = raw.replace(/[{}]/g, '').trim()
  const dashed = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(t)
  if (dashed) return t.toLowerCase()
  if (/^[0-9a-f]{32}$/i.test(t)) {
    const x = t.toLowerCase()
    return `${x.slice(0, 8)}-${x.slice(8, 12)}-${x.slice(12, 16)}-${x.slice(16, 20)}-${x.slice(20)}`
  }
  return undefined
}

function cellParseBoolOptional(cell: unknown): boolean | undefined {
  if (cell == null || cell === '') return undefined
  if (typeof cell === 'boolean') return cell
  const s = String(cell).trim().toLowerCase()
  if (s === 'true' || s === '1' || s === 'yes' || s === 'y') return true
  if (s === 'false' || s === '0' || s === 'no' || s === 'n') return false
  return undefined
}

function buildRowsFromMatrix(matrix: unknown[][], defaultDate: string | undefined): BulkAttendanceImportRowInput[] {
  if (matrix.length < 2) return []
  const headers = matrix[0] ?? []
  const map = detectColumns(headers)
  if (map.roll == null && map.admission == null && map.enrollment == null && map.studentId == null) {
    throw new Error(
      'Could not find a student column. Add a header like RollNumber, AdmissionNo, EnrollmentNo, or StudentId in row 1.'
    )
  }
  if (map.date == null && !defaultDate) {
    throw new Error(
      'No AttendanceDate/Date column found and no default date set. Add AttendanceDate (or Date) or pick a default date for the file.'
    )
  }
  const out: BulkAttendanceImportRowInput[] = []
  for (let i = 1; i < matrix.length; i++) {
    const row = matrix[i] ?? []
    const roll = map.roll != null ? cellStr(row[map.roll]) : undefined
    const admission = map.admission != null ? cellStr(row[map.admission]) : undefined
    const enrollment = map.enrollment != null ? cellStr(row[map.enrollment]) : undefined
    const sidRaw = map.studentId != null ? cellStr(row[map.studentId]) : undefined
    const sid = sidRaw ? cellParseGuid(sidRaw) : undefined
    const dateRaw = map.date != null ? cellToYmd(row[map.date]) : undefined
    const status = map.status != null ? cellStr(row[map.status]) : undefined
    const rowClassId = map.classId != null ? cellParseGuid(row[map.classId]) : undefined
    const divisionName = map.divisionName != null ? cellStr(row[map.divisionName]) : undefined
    const leaveType = map.leaveType != null ? cellStr(row[map.leaveType]) : undefined
    const isHoliday = map.isHoliday != null ? cellParseBoolOptional(row[map.isHoliday]) : undefined
    const remark = map.remark != null ? cellStr(row[map.remark]) : undefined
    const hasId = !!sid || !!roll || !!admission || !!enrollment
    if (!hasId) continue
    const ymd = dateRaw ?? defaultDate ?? undefined
    const payload: BulkAttendanceImportRowInput = {
      rowIndex: i + 1,
      studentId: sid ?? undefined,
      rollNumber: roll,
      admissionNo: admission,
      enrollmentNo: enrollment,
      date: ymd,
      classId: rowClassId,
      divisionName,
      status,
      leaveType,
      isHoliday: isHoliday ?? null,
      remark,
    }
    out.push(payload)
  }
  return out
}

async function parseAttendanceFile(file: File, defaultDate: string | undefined): Promise<BulkAttendanceImportRowInput[]> {
  const name = file.name.toLowerCase()
  if (name.endsWith('.csv')) {
    const text = await file.text()
    const lines = text.split(/\r?\n/).filter((l) => l.trim().length > 0)
    const matrix = lines.map((line) => {
      const cells: string[] = []
      let cur = ''
      let inQ = false
      for (let j = 0; j < line.length; j++) {
        const ch = line[j]
        if (ch === '"') {
          inQ = !inQ
        } else if ((ch === ',' && !inQ) || ch === ';') {
          cells.push(cur.trim())
          cur = ''
        } else cur += ch
      }
      cells.push(cur.trim())
      return cells
    })
    return buildRowsFromMatrix(matrix, defaultDate)
  }
  const matrix = (await readXlsxFile(file)) as unknown[][]
  return buildRowsFromMatrix(matrix, defaultDate)
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
  return fallback
}

export function AttendanceBulkImportPage() {
  const { ready: orgReady, branchId, classId, divisionId } = useOrgHierarchy()
  const [defaultDate, setDefaultDate] = useState('')
  const [defaultPresent, setDefaultPresent] = useState(true)
  const [file, setFile] = useState<File | null>(null)
  const [parsedRows, setParsedRows] = useState<BulkAttendanceImportRowInput[]>([])
  const [preview, setPreview] = useState<BulkAttendanceImportPreview | null>(null)
  const [lastRequest, setLastRequest] = useState<BulkAttendanceImportRequest | null>(null)
  /** After preview: import JSON body vs re-upload file to API */
  const [importMode, setImportMode] = useState<'none' | 'json' | 'server'>('none')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [info, setInfo] = useState<string | null>(null)

  async function handleParseFile(f: File | null) {
    setFile(f)
    setPreview(null)
    setLastRequest(null)
    setImportMode('none')
    setParsedRows([])
    setInfo(null)
    if (!f) return
    setBusy(true)
    setError(null)
    try {
      const rows = await parseAttendanceFile(f, defaultDate || undefined)
      if (rows.length === 0) {
        setInfo('No rows parsed in the browser. You can still use Preview (API) to parse the file on the server.')
      } else {
        setParsedRows(rows)
        setInfo(`Parsed ${rows.length} row(s) in browser, or use Preview (API) for server-side Excel/CSV parsing.`)
      }
    } catch {
      setParsedRows([])
      setInfo('Browser could not read this file. Use Preview (API) to parse .xlsx / .xls / .csv on the server.')
    } finally {
      setBusy(false)
    }
  }

  function buildRequest(): BulkAttendanceImportRequest {
    if (!branchId) throw new Error('Select a branch.')
    if (parsedRows.length === 0) throw new Error('Load a file first.')
    return {
      branchId,
      classId: classId || null,
      divisionId: divisionId || null,
      classTeacherId: null,
      defaultDate: defaultDate || null,
      defaultPresentWhenStatusEmpty: defaultPresent,
      rows: parsedRows,
    }
  }

  function buildFilePayload(): BulkAttendanceImportFilePayload {
    if (!branchId) throw new Error('Select a branch.')
    if (!file) throw new Error('Choose a file first.')
    return {
      branchId,
      classId: classId || null,
      divisionId: divisionId || null,
      classTeacherId: null,
      defaultDate: defaultDate || null,
      defaultPresentWhenStatusEmpty: defaultPresent,
      file,
    }
  }

  async function runPreview() {
    setBusy(true)
    setError(null)
    setInfo(null)
    try {
      const body = buildRequest()
      const p = await attendanceApi.previewBulkImport(body)
      setPreview(p)
      setLastRequest(body)
      setImportMode('json')
      setInfo(
        `Preview: ${p.wouldImportCount} row(s) ready to save, ${p.errorCount} error(s), ${p.holidayAwareRowCount} on weekly off or calendar holiday (still saved with holiday flag).`
      )
    } catch (e) {
      setError(apiErrorMessage(e, 'Preview failed.'))
    } finally {
      setBusy(false)
    }
  }

  async function runPreviewFromApi() {
    setBusy(true)
    setError(null)
    setInfo(null)
    try {
      const payload = buildFilePayload()
      const p = await attendanceApi.previewBulkImportFromFile(payload)
      setPreview(p)
      setLastRequest(null)
      setImportMode('server')
      setInfo(
        `API preview: ${p.wouldImportCount} row(s) ready to save, ${p.errorCount} error(s), ${p.holidayAwareRowCount} on weekly off or calendar holiday.`
      )
    } catch (e) {
      setError(apiErrorMessage(e, 'API preview failed.'))
    } finally {
      setBusy(false)
    }
  }

  async function runImport() {
    setBusy(true)
    setError(null)
    setInfo(null)
    try {
      if (importMode === 'server' && file) {
        const payload = buildFilePayload()
        const r = await attendanceApi.importBulkFromFile(payload)
        setPreview(r)
        setInfo(`Import finished: ${r.savedCount} attendance row(s) saved.`)
        return
      }
      if (importMode === 'json' && lastRequest) {
        const r = await attendanceApi.importBulkFromMachine(lastRequest)
        setPreview(r)
        setInfo(`Import finished: ${r.savedCount} attendance row(s) saved.`)
        return
      }
      setError('Run preview first (browser rows or API file).')
    } catch (e) {
      setError(apiErrorMessage(e, 'Import failed.'))
    } finally {
      setBusy(false)
    }
  }

  async function downloadApiSampleXlsx() {
    setBusy(true)
    setError(null)
    try {
      const blob = await attendanceApi.downloadBulkImportSampleXlsx()
      const url = URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = 'bulk-attendance-sample.xlsx'
      a.click()
      URL.revokeObjectURL(url)
      setInfo('Downloaded sample workbook from API.')
    } catch (e) {
      setError(apiErrorMessage(e, 'Could not download sample from API.'))
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
      <h2>Bulk attendance (Excel / CSV)</h2>
      <p style={styles.muted}>
        <strong>Sign in</strong> before preview/import — attendance is saved with your staff id as class teacher. Your login email should match{' '}
        <code>Staff.Email</code>, and you should be mapped in <strong>class teacher map</strong> for the chosen class/division (admins can pass{' '}
        <code>ClassTeacherId</code> in API form data). Layout: <strong>RollNumber</strong>, <strong>ClassId</strong> (class GUID),{' '}
        <strong>DivisionName</strong>,{' '}
        <strong>AttendanceDate</strong> (or Date), <strong>Status</strong>, optional <strong>LeaveType</strong>, optional{' '}
        <strong>IsHoliday</strong> (true/false, 1/0). You can still use <strong>AdmissionNo</strong> / <strong>EnrollmentNo</strong>{' '}
        / <strong>StudentId</strong> instead of roll. Class and division can be omitted from the file if you set the class
        and class/division from the <strong>organization tree</strong> on the left when those columns are omitted (rolls must be unique within the branch when using roll-only). Weekly off and calendar
        holidays match single marking; preview shows effective holiday.
      </p>
      <p style={styles.links}>
        <Link to={ROUTES.ATTENDANCE}>Attendance</Link>
        {' · '}
        <Link to={ROUTES.ATTENDANCE_HOLIDAYS}>Holiday management</Link>
        {' · '}
        <Link to={ROUTES.ATTENDANCE_DIVISION_EXCEL}>Division Excel builder</Link>
      </p>

      <section style={styles.sampleBox} aria-label="Download sample templates">
        <h3 style={styles.sampleTitle}>Download sample file (Excel-ready CSV)</h3>
        <p style={styles.muted}>
          Row 1 must be headers matching the import. Dates as <code>YYYY-MM-DD</code>. Status: Present, Absent, P, A,{' '}
          <code>1</code>/<code>0</code>, or leave blank if you use “empty = Present”. Open in Excel and use{' '}
          <strong>Save As → Excel Workbook (.xlsx)</strong> if you prefer a true Excel file; CSV and XLSX both upload.
        </p>
        <ul style={styles.sampleList}>
          <li>
            <button type="button" style={styles.btnLink} onClick={() => void downloadApiSampleXlsx()}>
              Download official .xlsx sample (from API)
            </button>{' '}
            — same columns as below; replace <code>REPLACE_WITH_YOUR_CLASS_GUID</code> with a real class id.
          </li>
          <li>
            <a
              href={`${import.meta.env.BASE_URL}bulk-attendance-sample-class-division.csv`}
              download="bulk-attendance-sample-class-division.csv"
            >
              RollNumber + ClassId + DivisionName + AttendanceDate + Status (+ optional LeaveType, IsHoliday)
            </a>{' '}
            — replace <code>REPLACE_WITH_CLASS_GUID</code> with your class id from the system.
          </li>
          <li>
            <a href={`${import.meta.env.BASE_URL}bulk-attendance-sample.csv`} download="bulk-attendance-sample.csv">
              RollNumber + Date + Status
            </a>{' '}
            — simple file; use class/division filters below if rolls repeat across classes.
          </li>
          <li>
            <a
              href={`${import.meta.env.BASE_URL}bulk-attendance-sample-admission.csv`}
              download="bulk-attendance-sample-admission.csv"
            >
              AdmissionNo + Date + Status
            </a>
          </li>
          <li>
            <a
              href={`${import.meta.env.BASE_URL}bulk-attendance-sample-enrollment.csv`}
              download="bulk-attendance-sample-enrollment.csv"
            >
              EnrollmentNo + Date + Status
            </a>{' '}
            — for machine / employee-style IDs mapped to enrollment numbers.
          </li>
          <li>
            <a
              href={`${import.meta.env.BASE_URL}bulk-attendance-sample-default-date.csv`}
              download="bulk-attendance-sample-default-date.csv"
            >
              RollNumber + Status only
            </a>{' '}
            — set <strong>Default date</strong> above, then choose this file (no Date column).
          </li>
        </ul>
      </section>

      <p style={styles.orgHint}>
        Import scope uses the <strong>organization tree</strong> (left): branch is required; class and division are optional defaults when the file omits ClassId / DivisionName (pick only a branch for broadest scope).
      </p>
      <div style={styles.filters}>
        <label style={styles.label}>
          Default date if file has no date column
          <input type="date" value={defaultDate} onChange={(e) => setDefaultDate(e.target.value)} style={styles.select} />
        </label>
      </div>

      <label style={styles.check}>
        <input type="checkbox" checked={defaultPresent} onChange={(e) => setDefaultPresent(e.target.checked)} />
        Treat empty status as <strong>Present</strong> (typical for punch logs)
      </label>

      <div style={{ marginTop: 16 }}>
        <label style={styles.label}>
          File (.xlsx, .xls, .csv)
          <input
            type="file"
            accept=".xlsx,.xls,.csv"
            onChange={(e) => void handleParseFile(e.target.files?.[0] ?? null)}
            style={{ marginTop: 6 }}
          />
        </label>
      </div>

      {file && (
        <p style={styles.muted}>
          Loaded: {file.name} — {parsedRows.length} row(s) parsed in browser
        </p>
      )}

      <p style={styles.muted}>
        <strong>Browser:</strong> preview uses rows parsed above. <strong>API:</strong> upload the same file; the server reads .xlsx, .xls, or .csv
        (no JSON body).
      </p>

      <div style={styles.row}>
        <button type="button" style={styles.btn} onClick={() => void runPreview()} disabled={busy || !branchId || parsedRows.length === 0}>
          {busy ? '…' : 'Preview (browser → JSON)'}
        </button>
        <button
          type="button"
          style={styles.btn}
          onClick={() => void runPreviewFromApi()}
          disabled={busy || !branchId || !file}
        >
          {busy ? '…' : 'Preview (API file upload)'}
        </button>
        <button
          type="button"
          style={styles.btnOk}
          onClick={() => void runImport()}
          disabled={
            busy ||
            !preview ||
            preview.wouldImportCount === 0 ||
            importMode === 'none' ||
            (importMode === 'json' && !lastRequest) ||
            (importMode === 'server' && !file)
          }
        >
          Import saved rows
        </button>
      </div>

      {preview && preview.rows.length > 0 && (
        <div style={{ marginTop: 24, overflowX: 'auto' }}>
          <h3>Preview</h3>
          <table style={styles.table}>
            <thead>
              <tr>
                <th style={styles.th}>Row</th>
                <th style={styles.th}>Student</th>
                <th style={styles.th}>Class / Div</th>
                <th style={styles.th}>Date</th>
                <th style={styles.th}>Status</th>
                <th style={styles.th}>Leave</th>
                <th style={styles.th}>Holiday / off</th>
                <th style={styles.th}>Error</th>
              </tr>
            </thead>
            <tbody>
              {preview.rows.map((r, i) => (
                <tr key={`${r.rowIndex}-${i}`}>
                  <td style={styles.td}>{r.rowIndex}</td>
                  <td style={styles.td}>{r.studentDisplayName ?? '—'}</td>
                  <td style={styles.td} title={r.resolvedClassId ?? ''}>
                    {r.resolvedClassId || r.resolvedDivisionId ? (
                      <span style={{ fontSize: 12 }}>
                        {r.resolvedClassId ? `${r.resolvedClassId.slice(0, 8)}…` : '—'}
                        <br />
                        {r.resolvedDivisionId ? `${r.resolvedDivisionId.slice(0, 8)}…` : '—'}
                      </span>
                    ) : (
                      '—'
                    )}
                  </td>
                  <td style={styles.td}>{r.date ?? '—'}</td>
                  <td style={styles.td}>{r.normalizedStatus ?? r.rawStatus ?? '—'}</td>
                  <td style={styles.td}>{r.leaveType ?? '—'}</td>
                  <td style={styles.td}>
                    {r.effectiveHoliday ? (
                      <span style={styles.badgeHol}>
                        {[
                          r.rowIsHoliday ? 'File IsHoliday' : null,
                          r.isCalendarHoliday ? 'Calendar' : null,
                          r.isWeeklyOff ? 'Weekly off' : null,
                        ]
                          .filter(Boolean)
                          .join(' · ')}
                      </span>
                    ) : (
                      '—'
                    )}
                  </td>
                  <td style={styles.tdErr}>{r.error ?? ''}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {error && <p style={styles.error}>{error}</p>}
      {info && <p style={styles.ok}>{info}</p>}
    </div>
  )
}

const styles: Record<string, CSSProperties> = {
  wrap: { padding: 0, maxWidth: 1100 },
  muted: { color: '#666', fontSize: 14, marginBottom: 12 },
  sampleBox: {
    background: '#f0f4f8',
    border: '1px solid #c5d4e3',
    borderRadius: 8,
    padding: '16px 20px',
    marginBottom: 24,
  },
  sampleTitle: { margin: '0 0 8px 0', fontSize: 16 },
  sampleList: { margin: '8px 0 0 0', paddingLeft: 20, lineHeight: 1.7 },
  links: { marginBottom: 16 },
  orgHint: { fontSize: 14, color: '#334155', marginBottom: 12, lineHeight: 1.5 },
  filters: { display: 'flex', gap: 24, flexWrap: 'wrap', marginBottom: 16 },
  label: { display: 'flex', flexDirection: 'column', gap: 4, fontSize: 14, fontWeight: 500 },
  select: { padding: 8, border: '1px solid #ccc', borderRadius: 6, minWidth: 200 },
  check: { display: 'flex', alignItems: 'center', gap: 8, marginTop: 8, cursor: 'pointer' },
  row: { display: 'flex', gap: 12, flexWrap: 'wrap', marginTop: 16 },
  btn: {
    padding: '8px 16px',
    background: '#1e3a5f',
    color: '#fff',
    border: 'none',
    borderRadius: 6,
    cursor: 'pointer',
  },
  btnOk: {
    padding: '8px 16px',
    background: '#166534',
    color: '#fff',
    border: 'none',
    borderRadius: 6,
    cursor: 'pointer',
  },
  btnLink: {
    padding: 0,
    border: 'none',
    background: 'none',
    color: '#1e3a5f',
    textDecoration: 'underline',
    cursor: 'pointer',
    font: 'inherit',
  },
  table: { borderCollapse: 'collapse', background: '#fff', width: '100%', fontSize: 13 },
  th: { textAlign: 'left', padding: '8px 10px', background: '#1e3a5f', color: '#fff' },
  td: { padding: '8px 10px', borderBottom: '1px solid #eee' },
  tdErr: { padding: '8px 10px', borderBottom: '1px solid #eee', color: '#a33', maxWidth: 280 },
  badgeHol: { color: '#92400e', fontWeight: 600 },
  error: { color: '#c00', marginTop: 12 },
  ok: { color: '#063', marginTop: 12 },
}
