import axios from 'axios'
import { useRef, useState, type CSSProperties } from 'react'
import { attendanceApi, type BulkAttendanceImportFilePayload } from '../api/attendance.api'

export type DivisionTemplateExcelImportPanelProps = {
  branchId: string
  classId: string
  divisionId: string
  disabled?: boolean
  onImportSuccess?: (savedCount: number) => void
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

export function DivisionTemplateExcelImportPanel({
  branchId,
  classId,
  divisionId,
  disabled,
  onImportSuccess,
}: DivisionTemplateExcelImportPanelProps) {
  const fileRef = useRef<HTMLInputElement>(null)
  const [file, setFile] = useState<File | null>(null)
  const [defaultPresent, setDefaultPresent] = useState(true)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [info, setInfo] = useState<string | null>(null)
  /**
   * Preview does NOT write to the database. Only POST …/import-file persists rows.
   * This runs preview then import so one click saves to the attendance table.
   */
  async function runPreviewThenImport() {
    setBusy(true)
    setError(null)
    setInfo(null)
    try {
      const payload = buildPayload()
      const p = await attendanceApi.previewDivisionTemplateImportFromFile(payload)
      if (p.wouldImportCount === 0) {
        setError(
          p.errorCount > 0
            ? `Nothing was saved: ${p.errorCount} row error(s) in the file. Fix the sheet and try again.`
            : 'Nothing to save: no rows qualify for import. Add Status values (or keep "Empty status = Present" checked) and valid dates.',
        )
        return
      }
      const r = await attendanceApi.importDivisionTemplateFromFile(payload)
      setInfo(`Saved ${r.savedCount} row(s) to the attendance table.`)
      onImportSuccess?.(r.savedCount)
    } catch (e) {
      setError(apiErrorMessage(e, 'Save failed. Ensure you are logged in and assigned as class teacher for this division.'))
    } finally {
      setBusy(false)
    }
  }

  function buildPayload(): BulkAttendanceImportFilePayload {
    if (!branchId || !classId || !divisionId) throw new Error('Select branch, class, and division.')
    if (!file) throw new Error('Choose a filled Excel file.')
    return {
      branchId,
      classId,
      divisionId,
      classTeacherId: null,
      defaultDate: null,
      defaultPresentWhenStatusEmpty: defaultPresent,
      file,
    }
  }

  function onPickFile(list: FileList | null) {
    const next = list?.[0] ?? null
    setFile(next)
    setInfo(null)
    setError(null)
  }

  async function runPreview() {
    setBusy(true)
    setError(null)
    setInfo(null)
    try {
      const payload = buildPayload()
      const p = await attendanceApi.previewDivisionTemplateImportFromFile(payload)
      setInfo(
        `Preview: ${p.wouldImportCount} row(s) ready to save, ${p.errorCount} error(s), ${p.holidayAwareRowCount} on weekly off or calendar holiday.`
      )
    } catch (e) {
      setError(apiErrorMessage(e, 'Preview failed.'))
    } finally {
      setBusy(false)
    }
  }

  return (
    <div style={styles.wrap}>
      <p style={styles.heading}>Upload filled workbook</p>
      <p style={styles.hint}>
        Use the same branch, class, and division as the download. Sign in as class teacher (or admin).{' '}
        <strong>Preview</strong> only checks the file; it does not save. Use <strong>Save to attendance</strong> to write rows
        to the database (<code style={{ fontSize: 11 }}>…/import-file</code>).
      </p>
      <input
        ref={fileRef}
        type="file"
        accept=".xlsx,.xls,.csv"
        style={{ display: 'none' }}
        onChange={(e) => onPickFile(e.target.files)}
      />
      <label style={styles.check}>
        <input
          type="checkbox"
          checked={defaultPresent}
          onChange={(e) => setDefaultPresent(e.target.checked)}
          disabled={busy || disabled}
        />
        Empty status = Present
      </label>
      <div style={styles.row}>
        <button
          type="button"
          style={styles.btnSecondary}
          onClick={() => fileRef.current?.click()}
          disabled={busy || disabled}
        >
          Choose file…
        </button>
        {file ? <span style={styles.fileName}>{file.name}</span> : <span style={styles.muted}>No file chosen</span>}
        <button
          type="button"
          style={styles.btnImport}
          onClick={() => void runPreviewThenImport()}
          disabled={!file || busy || disabled}
        >
          {busy ? '…' : 'Save to attendance'}
        </button>
        <button type="button" style={styles.btnPreview} onClick={() => void runPreview()} disabled={!file || busy || disabled}>
          {busy ? '…' : 'Preview only'}
        </button>
      </div>
      {error && <p style={styles.error}>{error}</p>}
      {info && !error && <p style={styles.ok}>{info}</p>}
    </div>
  )
}

const styles: Record<string, CSSProperties> = {
  wrap: { marginTop: 14, paddingTop: 14, borderTop: '1px solid #e2e8f0' },
  heading: { margin: '0 0 6px', fontSize: 14, fontWeight: 700, color: '#0f172a' },
  hint: { margin: '0 0 10px', fontSize: 12, color: '#64748b', lineHeight: 1.45 },
  check: { display: 'flex', alignItems: 'center', gap: 8, fontSize: 13, marginBottom: 10, cursor: 'pointer' },
  row: { display: 'flex', flexWrap: 'wrap', alignItems: 'center', gap: 10 },
  btnSecondary: {
    padding: '8px 14px',
    borderRadius: 8,
    border: '1px solid #94a3b8',
    background: '#fff',
    color: '#334155',
    fontWeight: 600,
    fontSize: 13,
    cursor: 'pointer',
  },
  btnPreview: {
    padding: '8px 14px',
    borderRadius: 8,
    border: 'none',
    background: '#1e3a5f',
    color: '#fff',
    fontWeight: 600,
    fontSize: 13,
    cursor: 'pointer',
  },
  btnImport: {
    padding: '8px 14px',
    borderRadius: 8,
    border: 'none',
    background: '#166534',
    color: '#fff',
    fontWeight: 600,
    fontSize: 13,
    cursor: 'pointer',
  },
  fileName: { fontSize: 13, color: '#334155', maxWidth: 240, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' },
  muted: { fontSize: 13, color: '#94a3b8' },
  error: { margin: '10px 0 0', fontSize: 13, color: '#b91c1c' },
  ok: { margin: '10px 0 0', fontSize: 13, color: '#166534' },
}
