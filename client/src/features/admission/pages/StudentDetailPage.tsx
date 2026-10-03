import type { CSSProperties, ReactNode } from 'react'
import { useEffect, useMemo, useState } from 'react'
import { useParams, Link, useNavigate } from 'react-router-dom'
import { ROUTES } from '../../../config/routes'
import { Button, Input, Spinner } from '../../../shared'
import { studentsApi, type StudentAdmissionPortfolio } from '../api/students.api'
import { attendanceApi } from '../../attendance/api/attendance.api'
import type { AttendancePeriodType, AttendanceSummary } from '../../attendance/attendance.types'
import { referenceDataApi, type ClassItem, type DivisionItem } from '../api/referenceData.api'
import { useOrgHierarchy } from '../../../layouts/OrgHierarchyProvider'

function CardShell({
  title,
  subtitle,
  children,
  accent,
}: {
  title: string
  subtitle?: string
  children: ReactNode
  accent: string
}) {
  return (
    <section style={{ ...cardShell, borderTopColor: accent }}>
      <div style={cardHead}>
        <h3 style={cardTitle}>{title}</h3>
        {subtitle ? <p style={cardSub}>{subtitle}</p> : null}
      </div>
      <div style={cardBody}>{children}</div>
    </section>
  )
}

function norm(s: string | undefined | null): string {
  return String(s ?? '')
    .trim()
    .toLowerCase()
}

type AttendanceUiPeriod = 'week' | 'month' | 'academicYear' | 'custom'

function mapUiPeriodToApi(ui: AttendanceUiPeriod): AttendancePeriodType {
  switch (ui) {
    case 'week':
      return 'Week'
    case 'month':
      return 'Month'
    case 'academicYear':
      return 'AcademicYear'
    case 'custom':
      return 'Custom'
    default:
      return 'Month'
  }
}

export function StudentDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const org = useOrgHierarchy()
  const [portfolio, setPortfolio] = useState<StudentAdmissionPortfolio | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refClasses, setRefClasses] = useState<ClassItem[]>([])
  const [refDivs, setRefDivs] = useState<DivisionItem[]>([])
  const [attPeriod, setAttPeriod] = useState<AttendanceUiPeriod>('month')
  const [customStart, setCustomStart] = useState('')
  const [customEnd, setCustomEnd] = useState('')
  const [attSummary, setAttSummary] = useState<AttendanceSummary | null>(null)
  const [attLoading, setAttLoading] = useState(false)
  const [attError, setAttError] = useState<string | null>(null)

  useEffect(() => {
    referenceDataApi.getClasses().then(setRefClasses).catch(() => setRefClasses([]))
    referenceDataApi.getDivisions().then(setRefDivs).catch(() => setRefDivs([]))
  }, [])

  useEffect(() => {
    if (!id?.trim()) {
      setPortfolio(null)
      setLoading(false)
      return
    }
    setLoading(true)
    setError(null)
    studentsApi
      .getAdmissionPortfolio(id)
      .then(setPortfolio)
      .catch(() => {
        setPortfolio(null)
        setError('Failed to load student profile.')
      })
      .finally(() => setLoading(false))
  }, [id])

  useEffect(() => {
    if (!id?.trim()) {
      setAttSummary(null)
      return
    }
    if (attPeriod === 'custom' && (!customStart.trim() || !customEnd.trim())) {
      setAttSummary(null)
      setAttError(null)
      setAttLoading(false)
      return
    }
    const apiPeriod = mapUiPeriodToApi(attPeriod)
    setAttLoading(true)
    setAttError(null)
    attendanceApi
      .getStudentSummary(
        id,
        apiPeriod,
        attPeriod === 'custom' ? customStart.trim() : undefined,
        attPeriod === 'custom' ? customEnd.trim() : undefined,
      )
      .then(setAttSummary)
      .catch(() => {
        setAttSummary(null)
        setAttError('Could not load attendance for this period.')
      })
      .finally(() => setAttLoading(false))
  }, [id, attPeriod, customStart, customEnd])

  const student = portfolio?.student
  const attendance = attSummary
  const fees = portfolio?.fees ?? null
  const marks = portfolio?.marks ?? []

  const branchLabel = useMemo(() => {
    if (!student?.branchId?.trim()) return '—'
    const b =
      org.branches.find((x) => norm(x.id) === norm(student.branchId)) ??
      org.branches.find((x) => x.id === student.branchId)
    return b?.name ?? student.branchId
  }, [student?.branchId, org.branches])

  const classLabel = useMemo(() => {
    if (!student?.classId?.trim()) return '—'
    const fromTree =
      org.treeClasses.find((c) => norm(c.id) === norm(student.classId)) ??
      org.classes.find((c) => norm(c.id) === norm(student.classId))
    if (fromTree?.name) return fromTree.name
    const c = refClasses.find((x) => norm(x.id) === norm(student.classId))
    return c?.name ?? student.classId
  }, [student?.classId, org.treeClasses, org.classes, refClasses])

  const divisionLabel = useMemo(() => {
    if (!student?.divisionId?.trim()) return '—'
    const fromTree =
      org.treeDivisions.find((d) => norm(d.id) === norm(student.divisionId)) ??
      org.divisions.find((d) => norm(d.id) === norm(student.divisionId))
    if (fromTree?.name) return fromTree.name
    const d = refDivs.find((x) => norm(x.id) === norm(student.divisionId))
    return d?.name ?? student.divisionId
  }, [student?.divisionId, org.treeDivisions, org.divisions, refDivs])

  async function handleDelete() {
    if (!id || !window.confirm('Delete this student?')) return
    try {
      await studentsApi.delete(id)
      navigate(ROUTES.STUDENTS)
    } catch {
      alert('Delete failed')
    }
  }

  if (loading) {
    return (
      <p style={loadingTxt}>
        <Spinner /> Loading student…
      </p>
    )
  }
  if (error || !student) {
    return <p style={{ color: '#b91c1c' }}>{error ?? 'Student not found'}</p>
  }

  const fullName = [student.firstName, student.middleName, student.lastName].filter(Boolean).join(' ') || '—'
  const status = String(student.studentStatus ?? '—').toUpperCase()

  const marksWithScore = marks.filter((m) => !m.isAbsent && !m.isExempt && m.marksObtained != null)
  const markAvg =
    marksWithScore.length > 0
      ? marksWithScore.reduce((s, m) => s + Number(m.marksObtained), 0) / marksWithScore.length
      : null

  return (
    <div style={page}>
      <div style={topBar}>
        <Link to={ROUTES.STUDENTS} style={backLink}>
          ← Back to students
        </Link>
        <div style={actions}>
          <Link to={ROUTES.STUDENT_EDIT(student.id)}>
            <Button variant="secondary">Edit record</Button>
          </Link>
          <Button variant="ghost" onClick={handleDelete}>
            Delete
          </Button>
        </div>
      </div>

      <header style={hero}>
        <div style={heroMain}>
          <div style={avatar}>{(student.firstName?.[0] ?? '?').toUpperCase()}</div>
          <div>
            <div style={heroNameRow}>
              <h1 style={heroName}>{fullName}</h1>
              <span style={{ ...statusPill, ...(status === 'ACTIVE' ? statusOk : {}) }}>{status}</span>
            </div>
            <p style={heroMeta}>
              Admission <strong>{student.admissionNo ?? '—'}</strong>
              {student.rollNumber ? (
                <>
                  {' · '}
                  Roll <strong>{student.rollNumber}</strong>
                </>
              ) : null}
            </p>
            <p style={heroHint}>
              Profile loads student, fees, and marks together. Attendance below uses the attendance module with selectable
              periods (weekly through custom range).
            </p>
          </div>
        </div>
      </header>

      <div style={grid}>
        <CardShell title="Enrollment" subtitle="Placement and identifiers" accent="#1e3a5f">
          <dl style={dl}>
            <dt>Gender</dt>
            <dd>{student.gender ?? '—'}</dd>
            <dt>Date of birth</dt>
            <dd>{student.dob ?? '—'}</dd>
            <dt>School (branch)</dt>
            <dd>{branchLabel}</dd>
            <dt>Class</dt>
            <dd>{classLabel}</dd>
            <dt>Division</dt>
            <dd>{divisionLabel}</dd>
            <dt>Academic year</dt>
            <dd>{student.academicYearId ? <code style={code}>{student.academicYearId}</code> : '—'}</dd>
          </dl>
          <Link to={ROUTES.ATTENDANCE_DIVISION} style={{ fontSize: 13, fontWeight: 600, color: '#1e3a5f' }}>
            Open attendance module →
          </Link>
        </CardShell>

        <CardShell title="Attendance" subtitle="Period-wise summary (attendance module)" accent="#0369a1">
          <div style={periodToolbar}>
            {(
              [
                { key: 'week' as const, label: 'Week' },
                { key: 'month' as const, label: 'Month' },
                { key: 'academicYear' as const, label: 'Academic year' },
                { key: 'custom' as const, label: 'Custom' },
              ] as const
            ).map(({ key, label }) => (
              <button
                key={key}
                type="button"
                onClick={() => setAttPeriod(key)}
                style={{
                  ...periodTab,
                  ...(attPeriod === key ? periodTabActive : {}),
                }}
              >
                {label}
              </button>
            ))}
          </div>
          {attPeriod === 'custom' ? (
            <div style={customRangeRow}>
              <Input
                label="From"
                type="date"
                value={customStart}
                onChange={(e) => setCustomStart(e.target.value)}
                style={{ marginBottom: 0 }}
              />
              <Input
                label="To"
                type="date"
                value={customEnd}
                onChange={(e) => setCustomEnd(e.target.value)}
                style={{ marginBottom: 0 }}
              />
            </div>
          ) : null}
          {attError ? <p style={{ ...mutedSmall, color: '#b91c1c' }}>{attError}</p> : null}
          {attLoading ? (
            <p style={mutedSmall}>
              <Spinner /> Loading attendance…
            </p>
          ) : attendance ? (
            <div style={kpiRow}>
              <div style={kpiBox}>
                <div style={kpiVal}>{attendance.percentage.toFixed(1)}%</div>
                <div style={kpiLab}>Attendance</div>
              </div>
              <div style={kpiBox}>
                <div style={kpiVal}>{attendance.presentCount}</div>
                <div style={kpiLab}>Present</div>
              </div>
              <div style={kpiBox}>
                <div style={kpiVal}>{attendance.absentCount}</div>
                <div style={kpiLab}>Absent</div>
              </div>
              <div style={kpiBox}>
                <div style={kpiVal}>{attendance.leaveCount}</div>
                <div style={kpiLab}>Leave</div>
              </div>
              <div style={kpiBox}>
                <div style={kpiVal}>{attendance.totalDays}</div>
                <div style={kpiLab}>Days tracked</div>
              </div>
            </div>
          ) : (
            <p style={mutedSmall}>
              {attPeriod === 'custom'
                ? 'Choose start and end dates to load a custom range.'
                : 'No attendance summary for this period yet.'}
            </p>
          )}
        </CardShell>

        <CardShell title="Fees" subtitle="Current academic year" accent="#047857">
          {fees ? (
            <>
              <div style={kpiRow}>
                <div style={kpiBox}>
                  <div style={kpiVal}>{fees.totalFees.toLocaleString(undefined, { maximumFractionDigits: 0 })}</div>
                  <div style={kpiLab}>Total</div>
                </div>
                <div style={kpiBox}>
                  <div style={{ ...kpiVal, color: '#047857' }}>
                    {fees.paidFees.toLocaleString(undefined, { maximumFractionDigits: 0 })}
                  </div>
                  <div style={kpiLab}>Paid</div>
                </div>
                <div style={kpiBox}>
                  <div style={{ ...kpiVal, color: fees.remainingFees > 0 ? '#b45309' : '#0f172a' }}>
                    {fees.remainingFees.toLocaleString(undefined, { maximumFractionDigits: 0 })}
                  </div>
                  <div style={kpiLab}>Outstanding</div>
                </div>
              </div>
              {fees.feeHeads.length > 0 && (
                <table style={miniTable}>
                  <thead>
                    <tr>
                      <th style={th}>Fee head</th>
                      <th style={th}>Total</th>
                      <th style={th}>Paid</th>
                      <th style={th}>Due</th>
                    </tr>
                  </thead>
                  <tbody>
                    {fees.feeHeads.map((h) => (
                      <tr key={h.feeHeadId}>
                        <td style={td}>{h.feeHeadName}</td>
                        <td style={td}>{h.totalAmount.toLocaleString()}</td>
                        <td style={td}>{h.paidAmount.toLocaleString()}</td>
                        <td style={td}>{h.remainingAmount.toLocaleString()}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </>
          ) : (
            <p style={mutedSmall}>No fee dashboard — set academic year on the student or configure fees.</p>
          )}
        </CardShell>

        <CardShell title="Exams & marks" subtitle="Recorded mark entries" accent="#7c3aed">
          <>
            <div style={kpiRow}>
              <div style={kpiBox}>
                <div style={kpiVal}>{marks.length}</div>
                <div style={kpiLabMark}>Entries</div>
              </div>
              <div style={kpiBox}>
                <div style={kpiVal}>{markAvg != null ? markAvg.toFixed(1) : '—'}</div>
                <div style={kpiLabMark}>Avg (graded)</div>
              </div>
            </div>
            {marks.length > 0 ? (
              <div style={tableScroll}>
                <table style={miniTable}>
                  <thead>
                    <tr>
                      <th style={th}>Marks</th>
                      <th style={th}>Status</th>
                      <th style={th}>Entered</th>
                    </tr>
                  </thead>
                  <tbody>
                    {marks.slice(0, 12).map((m) => (
                      <tr key={m.id}>
                        <td style={td}>
                          {m.isAbsent ? 'Absent' : m.isExempt ? 'Exempt' : m.marksObtained ?? '—'}
                        </td>
                        <td style={td}>
                          {[m.isAbsent && 'A', m.isMedical && 'M', m.isExempt && 'E'].filter(Boolean).join(' ') || '—'}
                        </td>
                        <td style={td}>{m.enteredAt?.slice(0, 10) ?? '—'}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            ) : (
              <p style={mutedSmall}>No mark rows for this student yet.</p>
            )}
          </>
        </CardShell>

        <CardShell title="Scholarship & welfare" accent="#c2410c">
          <dl style={dl}>
            <dt>Scholarship</dt>
            <dd>{student.hasScholarship ? `Yes — ${student.scholarshipType ?? ''}` : 'No'}</dd>
            <dt>EWS / DG</dt>
            <dd>{student.ewsDgCategory ?? '—'}</dd>
            <dt>Medical</dt>
            <dd>{student.medicalConditions?.trim() || '—'}</dd>
          </dl>
        </CardShell>

        <CardShell title="Family & contact" accent="#0d9488">
          <dl style={dl}>
            <dt>Father</dt>
            <dd>{student.fatherName ?? '—'}</dd>
            <dt>Father phone</dt>
            <dd>{student.fatherPhone ?? '—'}</dd>
            <dt>Mother</dt>
            <dd>{student.motherName ?? '—'}</dd>
            <dt>Student email</dt>
            <dd>{student.email ?? '—'}</dd>
          </dl>
        </CardShell>
      </div>
    </div>
  )
}

const page: CSSProperties = { maxWidth: 1100, margin: '0 auto' }
const topBar: CSSProperties = {
  display: 'flex',
  justifyContent: 'space-between',
  alignItems: 'center',
  marginBottom: 20,
  flexWrap: 'wrap',
  gap: 12,
}
const backLink: CSSProperties = { color: '#1e3a5f', fontWeight: 600, textDecoration: 'none', fontSize: 14 }
const actions: CSSProperties = { display: 'flex', gap: 8 }
const hero: CSSProperties = {
  background: 'linear-gradient(120deg, #fff 0%, #f1f5f9 100%)',
  border: '1px solid #e2e8f0',
  borderRadius: 14,
  padding: 22,
  marginBottom: 22,
  boxShadow: '0 2px 12px rgba(15, 23, 42, 0.06)',
}
const heroMain: CSSProperties = { display: 'flex', gap: 18, alignItems: 'center' }
const avatar: CSSProperties = {
  width: 64,
  height: 64,
  borderRadius: 14,
  background: 'linear-gradient(145deg, #1e3a5f, #334155)',
  color: '#fff',
  display: 'flex',
  alignItems: 'center',
  justifyContent: 'center',
  fontSize: 26,
  fontWeight: 800,
  flexShrink: 0,
}
const heroNameRow: CSSProperties = { display: 'flex', flexWrap: 'wrap', alignItems: 'center', gap: 10 }
const heroName: CSSProperties = { margin: 0, fontSize: 24, fontWeight: 800, color: '#0f172a' }
const heroMeta: CSSProperties = { margin: '8px 0 0', fontSize: 14, color: '#64748b' }
const heroHint: CSSProperties = { margin: '10px 0 0', fontSize: 12, color: '#94a3b8', maxWidth: 640, lineHeight: 1.45 }
const statusPill: CSSProperties = {
  fontSize: 11,
  fontWeight: 800,
  letterSpacing: '0.06em',
  textTransform: 'uppercase',
  padding: '4px 10px',
  borderRadius: 999,
  background: '#e2e8f0',
  color: '#475569',
}
const statusOk: CSSProperties = { background: '#d1fae5', color: '#047857' }
const code: CSSProperties = { fontSize: 12, background: '#f1f5f9', padding: '2px 6px', borderRadius: 4 }
const grid: CSSProperties = {
  display: 'grid',
  gridTemplateColumns: 'repeat(auto-fill, minmax(320px, 1fr))',
  gap: 18,
}
const cardShell: CSSProperties = {
  background: '#fff',
  borderRadius: 12,
  border: '1px solid #e2e8f0',
  borderTop: '3px solid #1e3a5f',
  boxShadow: '0 1px 3px rgba(15, 23, 42, 0.06)',
  overflow: 'hidden',
}
const cardHead: CSSProperties = { padding: '16px 18px 0' }
const cardTitle: CSSProperties = { margin: 0, fontSize: 15, fontWeight: 800, color: '#0f172a' }
const cardSub: CSSProperties = { margin: '6px 0 0', fontSize: 12, color: '#64748b' }
const cardBody: CSSProperties = { padding: '14px 18px 18px' }
const dl: CSSProperties = {
  display: 'grid',
  gridTemplateColumns: '120px 1fr',
  gap: '8px 12px',
  margin: 0,
  fontSize: 13,
  alignItems: 'baseline',
}
const periodToolbar: CSSProperties = {
  display: 'flex',
  flexWrap: 'wrap',
  gap: 8,
  marginBottom: 14,
}
const periodTab: CSSProperties = {
  cursor: 'pointer',
  background: '#fff',
  color: '#475569',
  fontSize: 12,
  fontWeight: 700,
  padding: '8px 14px',
  borderRadius: 8,
  border: '1px solid #e2e8f0',
}
const periodTabActive: CSSProperties = {
  background: '#0369a1',
  color: '#fff',
  borderColor: '#0369a1',
}
const customRangeRow: CSSProperties = {
  display: 'grid',
  gridTemplateColumns: 'repeat(auto-fill, minmax(160px, 1fr))',
  gap: 12,
  marginBottom: 14,
}
const kpiRow: CSSProperties = { display: 'flex', flexWrap: 'wrap', gap: 12 }
const kpiBox: CSSProperties = {
  flex: '1 1 72px',
  minWidth: 72,
  background: '#f8fafc',
  borderRadius: 10,
  padding: 12,
  textAlign: 'center',
  border: '1px solid #e2e8f0',
}
const kpiVal: CSSProperties = { fontSize: 20, fontWeight: 800, color: '#0f172a' }
const kpiLab: CSSProperties = { fontSize: 10, fontWeight: 700, color: '#64748b', marginTop: 6, textTransform: 'uppercase' }
const kpiLabMark: CSSProperties = kpiLab
const miniTable: CSSProperties = {
  width: '100%',
  borderCollapse: 'collapse',
  fontSize: 13,
  marginTop: 12,
}
const th: CSSProperties = {
  textAlign: 'left',
  padding: '8px 10px',
  background: '#1e3a5f',
  color: '#fff',
  fontSize: 12,
}
const td: CSSProperties = { padding: '8px 10px', borderBottom: '1px solid #e2e8f0' }
const tableScroll: CSSProperties = { overflowX: 'auto', maxHeight: 220, overflowY: 'auto' }
const mutedSmall: CSSProperties = { margin: 0, fontSize: 13, color: '#64748b' }
const loadingTxt: CSSProperties = { fontSize: 14, color: '#475569' }
