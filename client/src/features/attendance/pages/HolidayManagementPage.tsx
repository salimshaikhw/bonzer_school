import axios from 'axios'
import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { ROUTES } from '../../../config/routes'
import { Spinner } from '../../../shared'
import { useOrgHierarchy } from '../../../layouts/OrgHierarchyProvider'
import { holidayManagementApi } from '../api/holidayManagement.api'
import type { SaturdayPattern, SchoolHolidayDto, WeeklyOffSettings } from '../api/holidayManagement.api'

const SATURDAY_PATTERN_OPTIONS: { value: SaturdayPattern; label: string }[] = [
  { value: 'none', label: 'All Saturdays working' },
  { value: 'allSaturdays', label: 'Every Saturday off' },
  { value: 'secondFourth', label: '2nd & 4th Saturday of month off' },
  { value: 'firstThirdFifth', label: '1st, 3rd & 5th Saturday of month off' },
]

function normalizeSaturdayPattern(raw: string | undefined): SaturdayPattern {
  if (raw === 'allSaturdays' || raw === 'secondFourth' || raw === 'firstThirdFifth') return raw
  return 'none'
}

function saturdayPatternSummary(p: SaturdayPattern): string {
  const o = SATURDAY_PATTERN_OPTIONS.find((x) => x.value === p)
  return o?.label ?? p
}

/** Parse API date string (YYYY-MM-DD or ISO) as local calendar date for weekday display. */
function parseHolidayDate(isoOrYmd: string): Date | null {
  const m = isoOrYmd.trim().match(/^(\d{4})-(\d{2})-(\d{2})/)
  if (m) {
    const y = Number(m[1])
    const mo = Number(m[2]) - 1
    const day = Number(m[3])
    const d = new Date(y, mo, day)
    return Number.isNaN(d.getTime()) ? null : d
  }
  const t = Date.parse(isoOrYmd)
  if (Number.isNaN(t)) return null
  return new Date(t)
}

function weekdayLong(d: Date | null): string {
  if (!d) return '—'
  return d.toLocaleDateString(undefined, { weekday: 'long' })
}

function apiErrorMessage(err: unknown, fallback: string): string {
  if (axios.isAxiosError(err)) {
    const status = err.response?.status
    const d = err.response?.data
    if (status === 403) return 'Forbidden (403). Your account may need a different role, or the server rejected the token.'
    if (typeof d === 'string' && d.trim()) return d
    if (d && typeof d === 'object') {
      const o = d as Record<string, unknown>
      for (const key of ['userMessage', 'title', 'detail', 'message'] as const) {
        const v = o[key]
        if (typeof v === 'string' && v.trim()) return v
      }
    }
    if (err.message) return err.message
  }
  return fallback
}

export function HolidayManagementPage() {
  const { ready: orgReady, branchId, branches } = useOrgHierarchy()
  const [settings, setSettings] = useState<WeeklyOffSettings | null>(null)
  const [saturdayPattern, setSaturdayPattern] = useState<SaturdayPattern>('none')
  const [sundayOff, setSundayOff] = useState(true)
  const [holidays, setHolidays] = useState<SchoolHolidayDto[]>([])
  const [year, setYear] = useState(new Date().getUTCFullYear())
  const [newDate, setNewDate] = useState('')
  const [newName, setNewName] = useState('')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [info, setInfo] = useState<string | null>(null)

  useEffect(() => {
    if (!branchId) return
    setError(null)
    setInfo(null)
    const from = `${year}-01-01`
    const to = `${year}-12-31`
    Promise.all([
      holidayManagementApi.getWeeklyOff(branchId),
      holidayManagementApi.listHolidays(branchId, { from, to }),
    ])
      .then(([s, h]) => {
        setSettings(s)
        setSaturdayPattern(normalizeSaturdayPattern(s.saturdayPattern))
        setSundayOff(s.sundayOff)
        setHolidays(h)
      })
      .catch(() => setError('Failed to load holiday settings'))
  }, [branchId, year])

  async function saveWeeklyOff() {
    if (!branchId) return
    setSaving(true)
    setError(null)
    setInfo(null)
    try {
      const s = await holidayManagementApi.updateWeeklyOff(branchId, { saturdayPattern, sundayOff })
      setSettings(s)
      setSaturdayPattern(normalizeSaturdayPattern(s.saturdayPattern))
      setInfo('Weekly off saved.')
    } catch (e) {
      setError(apiErrorMessage(e, 'Could not save weekly off.'))
    } finally {
      setSaving(false)
    }
  }

  async function addHoliday() {
    if (!branchId || !newDate) return
    setSaving(true)
    setError(null)
    setInfo(null)
    try {
      await holidayManagementApi.addHoliday(branchId, { holidayDate: newDate, name: newName || 'Holiday' })
      const list = await holidayManagementApi.listHolidays(branchId, {
        from: `${year}-01-01`,
        to: `${year}-12-31`,
      })
      setHolidays(list)
      setNewDate('')
      setNewName('')
      setInfo('Holiday added.')
    } catch {
      setError('Could not add holiday (duplicate date or Admin role required).')
    } finally {
      setSaving(false)
    }
  }

  async function removeHoliday(id: string) {
    setSaving(true)
    setError(null)
    setInfo(null)
    try {
      await holidayManagementApi.deleteHoliday(id)
      setHolidays((prev) => prev.filter((h) => h.id !== id))
      setInfo('Holiday removed.')
    } catch (e) {
      setError(apiErrorMessage(e, 'Could not delete holiday.'))
    } finally {
      setSaving(false)
    }
  }

  if (!orgReady) return <p><Spinner /> Loading…</p>

  return (
    <div style={styles.wrap}>
      <h2>Holiday management</h2>
      <p style={styles.muted}>
        Select the <strong>school (branch)</strong> in the organization tree on the left. Then choose how <strong>Saturdays</strong> and <strong>Sundays</strong> count as weekly off (some schools use
        2nd/4th Saturdays, others 1st/3rd/5th), and add <strong>named calendar holidays</strong>. Attendance % and dashboards
        use these rules together with per-row holiday flags.
      </p>
      <p style={styles.links}>
        <Link to={ROUTES.ATTENDANCE}>Attendance</Link>
        {' · '}
        <Link to={ROUTES.ATTENDANCE_STUDENT}>Student-wise</Link>
      </p>

      <p style={styles.branchHint}>
        Current branch:{' '}
        <strong>{branches.find((b) => b.id === branchId)?.name ?? (branchId || '—')}</strong>
      </p>
      <div style={styles.filters}>
        <label style={styles.label}>
          Holiday list year
          <input
            type="number"
            value={year}
            min={2000}
            max={2100}
            onChange={(e) => setYear(Number(e.target.value))}
            style={styles.select}
          />
        </label>
      </div>

      {branchId && (
        <>
          <section style={styles.section}>
            <h3>Weekly off (Saturday / Sunday)</h3>
            <p style={styles.muted}>
              Weekly off days are excluded from attendance percentages. Default: Sunday off; Saturday pattern is per branch.
            </p>
            <label style={styles.label}>
              Saturday rule
              <select
                value={saturdayPattern}
                onChange={(e) => setSaturdayPattern(normalizeSaturdayPattern(e.target.value))}
                style={styles.selectWide}
              >
                {SATURDAY_PATTERN_OPTIONS.map((o) => (
                  <option key={o.value} value={o.value}>{o.label}</option>
                ))}
              </select>
            </label>
            <label style={styles.check}>
              <input type="checkbox" checked={sundayOff} onChange={(e) => setSundayOff(e.target.checked)} />
              Treat <strong>Sunday</strong> as weekly off
            </label>
            <div style={{ marginTop: 12 }}>
              <button type="button" style={styles.btn} onClick={saveWeeklyOff} disabled={saving}>
                {saving ? 'Saving…' : 'Save weekly off'}
              </button>
            </div>
            {settings && (
              <p style={styles.muted}>
                Saved: Saturday — {saturdayPatternSummary(normalizeSaturdayPattern(settings.saturdayPattern))}; Sun=
                {settings.sundayOff ? 'off' : 'working'}
              </p>
            )}
          </section>

          <section style={styles.section}>
            <h3>Calendar holidays (this branch)</h3>
            <div style={styles.row}>
              <label style={styles.label}>
                Date
                <input type="date" value={newDate} onChange={(e) => setNewDate(e.target.value)} style={styles.select} />
                {newDate ? (
                  <span style={styles.dayHint}>{weekdayLong(parseHolidayDate(newDate))}</span>
                ) : null}
              </label>
              <label style={styles.label}>
                Name (optional)
                <input
                  type="text"
                  value={newName}
                  onChange={(e) => setNewName(e.target.value)}
                  placeholder="e.g. Diwali"
                  style={styles.select}
                />
              </label>
              <button type="button" style={styles.btn} onClick={addHoliday} disabled={saving || !newDate}>
                Add holiday
              </button>
            </div>
            <table style={styles.table}>
              <thead>
                <tr>
                  <th style={styles.th}>Date</th>
                  <th style={styles.th}>Day</th>
                  <th style={styles.th}>Name</th>
                  <th style={styles.th} />
                </tr>
              </thead>
              <tbody>
                {holidays.length === 0 ? (
                  <tr>
                    <td colSpan={4} style={styles.td}>No holidays for {year}.</td>
                  </tr>
                ) : (
                  holidays.map((h) => (
                    <tr key={h.id}>
                      <td style={styles.td}>{h.holidayDate}</td>
                      <td style={styles.td}>{weekdayLong(parseHolidayDate(h.holidayDate))}</td>
                      <td style={styles.td}>{h.name ?? '—'}</td>
                      <td style={styles.td}>
                        <button type="button" style={styles.btnDanger} onClick={() => removeHoliday(h.id)} disabled={saving}>
                          Remove
                        </button>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </section>
        </>
      )}

      {error && <p style={styles.error}>{error}</p>}
      {info && <p style={styles.ok}>{info}</p>}
    </div>
  )
}

const styles: Record<string, React.CSSProperties> = {
  wrap: { padding: 0, maxWidth: 900 },
  muted: { color: '#666', fontSize: 14, marginBottom: 12 },
  links: { marginBottom: 16 },
  branchHint: { fontSize: 14, color: '#334155', marginBottom: 12 },
  filters: { display: 'flex', gap: 24, flexWrap: 'wrap', marginBottom: 24 },
  label: { display: 'flex', flexDirection: 'column', gap: 4, fontSize: 14, fontWeight: 500 },
  dayHint: { fontSize: 13, fontWeight: 400, color: '#444', marginTop: 2 },
  select: { padding: 8, border: '1px solid #ccc', borderRadius: 6, minWidth: 200 },
  selectWide: { padding: 8, border: '1px solid #ccc', borderRadius: 6, minWidth: 280, maxWidth: '100%' },
  section: { marginBottom: 32 },
  check: { display: 'flex', alignItems: 'center', gap: 8, marginBottom: 8, cursor: 'pointer' },
  row: { display: 'flex', flexWrap: 'wrap', gap: 16, alignItems: 'flex-end', marginBottom: 16 },
  btn: {
    padding: '8px 16px',
    background: '#1e3a5f',
    color: '#fff',
    border: 'none',
    borderRadius: 6,
    cursor: 'pointer',
  },
  btnDanger: {
    padding: '6px 12px',
    background: '#a33',
    color: '#fff',
    border: 'none',
    borderRadius: 6,
    cursor: 'pointer',
    fontSize: 13,
  },
  table: { borderCollapse: 'collapse', background: '#fff', borderRadius: 8, overflow: 'hidden', boxShadow: '0 1px 3px rgba(0,0,0,0.1)', width: '100%' },
  th: { textAlign: 'left', padding: '10px 12px', background: '#1e3a5f', color: '#fff', fontSize: 14 },
  td: { padding: '10px 12px', borderBottom: '1px solid #eee' },
  error: { color: '#c00' },
  ok: { color: '#063' },
}
