import type { CSSProperties } from 'react'
import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { ROUTES } from '../../../config/routes'
import { Spinner } from '../../../shared'
import { useOrgHierarchy } from '../../../layouts/OrgHierarchyProvider'
import { AdmissionOrgScopeBanner } from '../../admission/components/AdmissionOrgScopeBanner'
import { academicYearsApi } from '../api/academicYears.api'
import {
  financeScopeTitle,
  getFinanceEffectiveBranchIds,
  getFinanceScopeLevel,
  type FinanceScopeLevel,
} from '../utils/financeOrgScope'
import { adminFeesApi } from '../api/adminFees.api'
import { FinancePageHeader } from '../ui/FinancePageHeader'
import { financeTheme as t } from '../ui/financeTheme'
import type {
  BranchFeeDashboardDto,
  BranchOutstandingRow,
  ClassFeeDashboardDto,
  DivisionFeeDashboardDto,
  FeeCollectionTrendPoint,
  FeeHeadAnalyticsRow,
  InstallmentAnalyticsRow,
  StudentOutstandingRow,
} from '../types'

function norm(s: string | undefined | null): string {
  return String(s ?? '')
    .trim()
    .toLowerCase()
}

function money(n: number): string {
  return n.toLocaleString(undefined, { minimumFractionDigits: 0, maximumFractionDigits: 0 })
}

export function FinanceAnalyticsPage() {
  const org = useOrgHierarchy()
  const [years, setYears] = useState<{ id: string; label: string }[]>([])
  const [academicYearId, setAcademicYearId] = useState('')

  const [loading, setLoading] = useState(false)
  const [err, setErr] = useState<string | null>(null)

  const [groupOut, setGroupOut] = useState<BranchOutstandingRow[]>([])
  const [branchDash, setBranchDash] = useState<BranchFeeDashboardDto | null>(null)
  const [classDash, setClassDash] = useState<ClassFeeDashboardDto | null>(null)
  const [divisionDash, setDivisionDash] = useState<DivisionFeeDashboardDto | null>(null)
  const [feeHeads, setFeeHeads] = useState<FeeHeadAnalyticsRow[]>([])
  const [installments, setInstallments] = useState<InstallmentAnalyticsRow[]>([])
  const [outClass, setOutClass] = useState<{ classId: string; className: string; remainingAmount: number }[]>([])
  const [outDivision, setOutDivision] = useState<
    { divisionId: string; divisionName: string; remainingAmount: number }[]
  >([])
  const [studentsOut, setStudentsOut] = useState<StudentOutstandingRow[]>([])
  const [trend, setTrend] = useState<FeeCollectionTrendPoint[]>([])

  useEffect(() => {
    let cancelled = false
    void (async () => {
      try {
        const list = await academicYearsApi.getAll()
        if (cancelled) return
        const opts = list.map((y) => ({
          id: y.id,
          label: y.yearName?.trim() ? String(y.yearName) : y.id,
        }))
        setYears(opts)
        const active = list.find((y) => y.isActive === true)
        setAcademicYearId((prev) => {
          if (prev.trim()) return prev
          if (active?.id) return active.id
          return opts[0]?.id ?? ''
        })
      } catch {
        if (!cancelled) {
          setYears([])
          setErr('Could not load academic years.')
        }
      }
    })()
    return () => {
      cancelled = true
    }
  }, [])

  const analyticsLevel = useMemo((): FinanceScopeLevel => getFinanceScopeLevel(org), [
    org.branchId,
    org.groupScopeId,
    org.classId,
    org.divisionId,
  ])
  const effectiveBranchIds = useMemo(() => getFinanceEffectiveBranchIds(org), [
    org.branchId,
    org.groupScopeId,
    org.branches,
  ])
  const scopeTitle = useMemo(() => financeScopeTitle(analyticsLevel, org, effectiveBranchIds.length), [
    analyticsLevel,
    effectiveBranchIds.length,
    org.branchId,
    org.groupScopeId,
    org.classId,
    org.divisionId,
    org.branches,
    org.schoolGroups,
    org.treeClasses,
    org.treeDivisions,
    org.classes,
    org.divisions,
  ])
  const groupBranchNormSet = useMemo(
    () => new Set(effectiveBranchIds.map((id) => norm(id))),
    [effectiveBranchIds.join('|')],
  )

  const showSchoolRollups = analyticsLevel === 'school'

  useEffect(() => {
    if (!academicYearId.trim()) return

    if (!analyticsLevel) {
      setGroupOut([])
      setBranchDash(null)
      setClassDash(null)
      setDivisionDash(null)
      setFeeHeads([])
      setInstallments([])
      setOutClass([])
      setOutDivision([])
      setStudentsOut([])
      setTrend([])
      setLoading(false)
      setErr(null)
      return
    }

    if (analyticsLevel === 'group') {
      setLoading(true)
      setErr(null)
      let cancelled = false
      void (async () => {
        try {
          const allBranches = await adminFeesApi.getOutstandingByBranch(academicYearId.trim())
          if (cancelled) return
          const filtered =
            groupBranchNormSet.size > 0
              ? allBranches.filter((r) => groupBranchNormSet.has(norm(r.branchId)))
              : []
          setGroupOut(filtered.sort((a, b) => b.remainingAmount - a.remainingAmount))
          setBranchDash(null)
          setClassDash(null)
          setDivisionDash(null)
          setFeeHeads([])
          setInstallments([])
          setOutClass([])
          setOutDivision([])
          setStudentsOut([])
          setTrend([])
        } catch (e) {
          if (!cancelled) {
            setErr(e instanceof Error ? e.message : 'Failed to load group fee analytics.')
            setGroupOut([])
          }
        } finally {
          if (!cancelled) setLoading(false)
        }
      })()
      return () => {
        cancelled = true
      }
    }

    if (!org.branchId.trim()) {
      setGroupOut([])
      setBranchDash(null)
      setClassDash(null)
      setDivisionDash(null)
      setFeeHeads([])
      setInstallments([])
      setOutClass([])
      setOutDivision([])
      setStudentsOut([])
      setTrend([])
      setLoading(false)
      return
    }

    setLoading(true)
    setErr(null)
    let cancelled = false

    const branchId = org.branchId.trim()
    const ay = academicYearId.trim()
    const classId = org.classId.trim()
    const divisionId = org.divisionId.trim()

    const end = new Date()
    const start = new Date()
    start.setDate(start.getDate() - 90)
    const endStr = end.toISOString().slice(0, 10)
    const startStr = start.toISOString().slice(0, 10)

    void (async () => {
      try {
        const scopeOpts = { classId: classId || undefined, divisionId: divisionId || undefined }
        const promises: Promise<unknown>[] = [
          adminFeesApi.getBranchDashboard(branchId, ay),
          adminFeesApi.getFeeHeadAnalytics(branchId, ay, scopeOpts),
          adminFeesApi.getInstallmentAnalytics(branchId, ay, scopeOpts),
          adminFeesApi.getOutstandingByClass(branchId, ay),
          adminFeesApi.getOutstandingByDivision(branchId, ay),
          adminFeesApi.getOutstandingByStudent(branchId, ay, classId || undefined, divisionId || undefined),
          adminFeesApi.getCollectionTrend(branchId, startStr, endStr),
        ]

        if (classId) {
          promises.push(adminFeesApi.getClassDashboard(branchId, classId, ay))
        }
        if (divisionId) {
          promises.push(adminFeesApi.getDivisionDashboard(branchId, divisionId, ay))
        }

        const results = await Promise.all(promises)
        if (cancelled) return

        let i = 0
        setBranchDash(results[i++] as BranchFeeDashboardDto)
        setFeeHeads(results[i++] as FeeHeadAnalyticsRow[])
        setInstallments(results[i++] as InstallmentAnalyticsRow[])
        setOutClass(results[i++] as typeof outClass)
        setOutDivision(results[i++] as typeof outDivision)
        setStudentsOut(results[i++] as StudentOutstandingRow[])
        setTrend(results[i++] as FeeCollectionTrendPoint[])

        if (classId) {
          setClassDash(results[i++] as ClassFeeDashboardDto)
        } else {
          setClassDash(null)
        }
        if (divisionId) {
          setDivisionDash(results[i++] as DivisionFeeDashboardDto)
        } else {
          setDivisionDash(null)
        }

        setGroupOut([])
      } catch (e) {
        if (!cancelled) {
          setErr(e instanceof Error ? e.message : 'Failed to load fee analytics.')
          setBranchDash(null)
          setClassDash(null)
          setDivisionDash(null)
          setFeeHeads([])
          setInstallments([])
          setOutClass([])
          setOutDivision([])
          setStudentsOut([])
          setTrend([])
        }
      } finally {
        if (!cancelled) setLoading(false)
      }
    })()

    return () => {
      cancelled = true
    }
  }, [
    academicYearId,
    analyticsLevel,
    org.branchId,
    org.classId,
    org.divisionId,
    org.branches,
    effectiveBranchIds,
    groupBranchNormSet,
  ])

  const groupTotalOutstanding = useMemo(() => groupOut.reduce((s, r) => s + r.remainingAmount, 0), [groupOut])

  const trendMax = useMemo(() => trend.reduce((m, p) => Math.max(m, p.amount), 0), [trend])

  const feeHeadScopeNote = useMemo(() => {
    const cid = org.classId?.trim()
    const did = org.divisionId?.trim()
    if (did) {
      const d =
        org.treeDivisions.find((x) => x.id === did) ?? org.divisions.find((x) => x.id === did)
      let classSuffix = ''
      if (cid) {
        const c =
          org.treeClasses.find((x) => x.id === cid) ?? org.classes.find((x) => x.id === cid)
        if (c?.name) classSuffix = ` — class ${c.name}`
      }
      return `Fee-type totals follow the selected division${d?.name ? ` (${d.name})` : ''}${classSuffix}. They reflect class fee mapping for this scope.`
    }
    if (cid) {
      const c = org.treeClasses.find((x) => x.id === cid) ?? org.classes.find((x) => x.id === cid)
      return `Fee-type totals for class ${c?.name ?? 'selection'} only (from class fee mapping).`
    }
    return 'Fee-type totals for the whole school and academic year (all classes). Narrow the tree to a class or division to see that scope only.'
  }, [org.classId, org.divisionId, org.treeClasses, org.treeDivisions, org.classes, org.divisions])

  const installmentScopeNote = useMemo(() => {
    const cid = org.classId?.trim()
    const did = org.divisionId?.trim()
    if (did || cid) {
      return 'Expected and collected amounts come from invoice lines tagged with an installment, filtered by your tree selection.'
    }
    return 'Expected and collected amounts come from invoice lines tagged with an installment for this school and year.'
  }, [org.classId, org.divisionId])

  return (
    <div style={{ fontFamily: t.fontSans }}>
      <AdmissionOrgScopeBanner />

      <FinancePageHeader
        title="Revenue & collection"
        description="Outstanding balances, collection trends, and installment progress — scoped from the organization tree and academic year below."
      />

      <div style={styles.toolbarCard}>
        <label style={styles.lab}>
          Academic year
          <select
            style={styles.select}
            value={academicYearId}
            onChange={(e) => setAcademicYearId(e.target.value)}
            disabled={years.length === 0}
          >
            {years.length === 0 ? <option value="">—</option> : null}
            {years.map((y) => (
              <option key={y.id} value={y.id}>
                {y.label}
              </option>
            ))}
          </select>
        </label>
      </div>

      {err ? <p style={styles.error}>{err}</p> : null}

      {scopeTitle ? (
        <div style={styles.scopeStrip}>
          <strong style={styles.scopeK}>Active scope</strong>
          <span style={styles.scopeLine}>{scopeTitle}</span>
        </div>
      ) : null}

      {!org.ready || org.loading ? (
        <div style={styles.spin}>
          <Spinner />
        </div>
      ) : !academicYearId.trim() ? (
        <p style={styles.muted}>Select an academic year to load fee analytics.</p>
      ) : !analyticsLevel ? (
        <div style={styles.emptyBox}>
          <strong style={styles.emptyTitle}>Choose a scope in the organization tree</strong>
          <p style={styles.muted}>
            Click a school group (for multi-school rollups) or a school, then optionally a class or division — same as
            Admission analytics. Fee dashboards and student balances follow that selection.
          </p>
        </div>
      ) : loading ? (
        <div style={styles.spin}>
          <Spinner />
        </div>
      ) : analyticsLevel === 'group' ? (
        <section style={styles.section}>
          <h2 style={styles.h2}>Outstanding by branch (group)</h2>
          <p style={styles.muted}>
            Totals include every school in the selected group for this academic year. Pick a single school in the tree
            for collection trends, installments, and student balances.
          </p>
          <div style={styles.kpiRow}>
            <div style={styles.kpi}>
              <div style={styles.kpiLab}>Total outstanding</div>
              <div style={styles.kpiVal}>{money(groupTotalOutstanding)}</div>
            </div>
            <div style={styles.kpi}>
              <div style={styles.kpiLab}>Schools in scope</div>
              <div style={styles.kpiVal}>{groupOut.length}</div>
            </div>
          </div>
          {groupOut.length === 0 ? (
            <p style={styles.muted}>
              {effectiveBranchIds.length === 0
                ? 'This school group has no branches in the catalog yet, or schools are not assigned to the group. Use Admin → Schools to set each school’s group, or select a different group.'
                : 'No outstanding rows returned for the schools in this group for the selected academic year.'}
            </p>
          ) : (
            <table style={styles.table}>
              <thead>
                <tr>
                  <th style={styles.th}>Branch</th>
                  <th style={styles.th} colSpan={1}>
                    Remaining
                  </th>
                </tr>
              </thead>
              <tbody>
                {groupOut.map((r) => (
                  <tr key={r.branchId}>
                    <td style={styles.td}>{r.branchName || r.branchId}</td>
                    <td style={{ ...styles.td, textAlign: 'right' }}>{money(r.remainingAmount)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </section>
      ) : (
        <>
          {analyticsLevel === 'class' || analyticsLevel === 'division' ? (
            <p style={styles.drillHint}>
              {analyticsLevel === 'class'
                ? 'Class drill-down: branch KPIs and trend are school-wide; student balances and fee breakdowns follow the selected class.'
                : 'Division drill-down: student balances match this division; branch/collection metrics stay at school level.'}
            </p>
          ) : null}
          <div style={styles.kpiRow}>
            {branchDash ? (
              <>
                <div style={styles.kpi}>
                  <div style={styles.kpiLab}>Collection rate</div>
                  <div style={styles.kpiVal}>{branchDash.collectionRate.toFixed(1)}%</div>
                  <div style={styles.kpiSub}>
                    Collected {money(branchDash.collectedFees)} / {money(branchDash.totalFees)}
                  </div>
                </div>
                <div style={styles.kpi}>
                  <div style={styles.kpiLab}>Pending fees</div>
                  <div style={{ ...styles.kpiVal, color: '#b45309' }}>{money(branchDash.pendingFees)}</div>
                </div>
                <div style={styles.kpi}>
                  <div style={styles.kpiLab}>Students</div>
                  <div style={styles.kpiVal}>{branchDash.totalStudents}</div>
                </div>
              </>
            ) : null}
            {classDash ? (
              <div style={{ ...styles.kpi, borderTopColor: '#0369a1' }}>
                <div style={styles.kpiLab}>Class scope</div>
                <div style={styles.kpiVal}>{classDash.collectionRate.toFixed(1)}%</div>
                <div style={styles.kpiSub}>Pending {money(classDash.pendingFees)}</div>
              </div>
            ) : null}
            {divisionDash ? (
              <div style={{ ...styles.kpi, borderTopColor: '#7c3aed' }}>
                <div style={styles.kpiLab}>Division scope</div>
                <div style={styles.kpiVal}>{divisionDash.collectionRate.toFixed(1)}%</div>
                <div style={styles.kpiSub}>Pending {money(divisionDash.pendingFees)}</div>
              </div>
            ) : null}
          </div>

          {showSchoolRollups ? (
            <section style={styles.section}>
              <h2 style={styles.h2}>Outstanding by class</h2>
              {outClass.length === 0 ? (
                <p style={styles.muted}>No rows.</p>
              ) : (
                <table style={styles.table}>
                  <thead>
                    <tr>
                      <th style={styles.th}>Class</th>
                      <th style={styles.th}>Remaining</th>
                    </tr>
                  </thead>
                  <tbody>
                    {outClass.map((r) => (
                      <tr key={r.classId}>
                        <td style={styles.td}>{r.className}</td>
                        <td style={{ ...styles.td, textAlign: 'right' }}>{money(r.remainingAmount)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </section>
          ) : null}

          {showSchoolRollups ? (
            <section style={styles.section}>
            <h2 style={styles.h2}>Outstanding by division</h2>
            {outDivision.length === 0 ? (
              <p style={styles.muted}>No rows.</p>
            ) : (
              <table style={styles.table}>
                <thead>
                  <tr>
                    <th style={styles.th}>Division</th>
                    <th style={styles.th}>Remaining</th>
                  </tr>
                </thead>
                <tbody>
                  {outDivision.map((r) => (
                    <tr key={r.divisionId}>
                      <td style={styles.td}>{r.divisionName}</td>
                      <td style={{ ...styles.td, textAlign: 'right' }}>{money(r.remainingAmount)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
            </section>
          ) : null}

          <section style={styles.section}>
            <h2 style={styles.h2}>Fee heads (by type)</h2>
            <p style={styles.sectionHint}>{feeHeadScopeNote}</p>
            {feeHeads.length === 0 ? (
              <p style={styles.muted}>
                No fee-head rows for this scope. Map fee heads under Finance → Fee masters → Class fee map, or widen the
                tree to school level.
              </p>
            ) : (
              <table style={styles.table}>
                <thead>
                  <tr>
                    <th style={styles.th}>Fee head</th>
                    <th style={styles.th}>Total (mapped)</th>
                  </tr>
                </thead>
                <tbody>
                  {feeHeads.map((r) => (
                    <tr key={r.feeHeadId}>
                      <td style={styles.td}>{r.feeHeadName}</td>
                      <td style={{ ...styles.td, textAlign: 'right' }}>{money(r.totalAmount)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </section>

          <section style={styles.section}>
            <h2 style={styles.h2}>Installments</h2>
            <p style={styles.sectionHint}>{installmentScopeNote}</p>
            {installments.length === 0 ? (
              <p style={styles.muted}>
                No installment-tagged invoice lines in this scope yet. Ensure invoices include installment IDs on line
                items, or adjust class/division in the tree.
              </p>
            ) : (
              <table style={styles.table}>
                <thead>
                  <tr>
                    <th style={styles.th}>School</th>
                    <th style={styles.th}>Class</th>
                    <th style={styles.th}>Division</th>
                    <th style={styles.th}>Installment</th>
                    <th style={{ ...styles.th, textAlign: 'right' }}>Expected</th>
                    <th style={{ ...styles.th, textAlign: 'right' }}>Collected</th>
                    <th style={{ ...styles.th, textAlign: 'right' }}>Pending</th>
                    <th style={{ ...styles.th, textAlign: 'right' }}>%</th>
                  </tr>
                </thead>
                <tbody>
                  {installments.map((r) => {
                    const pend =
                      r.pendingAmount != null && Number.isFinite(r.pendingAmount)
                        ? r.pendingAmount
                        : Math.max(0, r.expectedAmount - r.collectedAmount)
                    const pct =
                      r.collectionPercent != null && Number.isFinite(r.collectionPercent)
                        ? r.collectionPercent
                        : r.expectedAmount > 0
                          ? Math.round((r.collectedAmount / r.expectedAmount) * 1000) / 10
                          : 0
                    return (
                      <tr key={r.installmentId}>
                        <td style={styles.td}>{r.branchName ?? '—'}</td>
                        <td style={styles.td}>{r.className ?? '—'}</td>
                        <td style={styles.td}>{r.divisionName ?? '—'}</td>
                        <td style={styles.td}>{r.installmentName}</td>
                        <td style={{ ...styles.td, textAlign: 'right' }}>{money(r.expectedAmount)}</td>
                        <td style={{ ...styles.td, textAlign: 'right' }}>{money(r.collectedAmount)}</td>
                        <td style={{ ...styles.td, textAlign: 'right', color: pend > 0 ? '#b45309' : '#047857' }}>
                          {money(pend)}
                        </td>
                        <td style={{ ...styles.td, textAlign: 'right' }}>{r.expectedAmount > 0 ? `${pct}%` : '—'}</td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            )}
          </section>

          <section style={styles.section}>
            <h2 style={styles.h2}>Collection trend (90 days)</h2>
            {trend.length === 0 ? (
              <p style={styles.muted}>No collection points in this range.</p>
            ) : (
              <div style={styles.trend}>
                {trend.map((p) => (
                  <div key={p.date} style={styles.trendRow}>
                    <span style={styles.trendDate}>{p.date || '—'}</span>
                    <div style={styles.trendBarTrack}>
                      <div
                        style={{
                          ...styles.trendBar,
                          width: `${trendMax > 0 ? Math.max(8, (p.amount / trendMax) * 100) : 0}%`,
                        }}
                      />
                    </div>
                    <span style={styles.trendAmt}>{money(p.amount)}</span>
                  </div>
                ))}
              </div>
            )}
          </section>

          <section style={styles.section}>
            <h2 style={styles.h2}>Students with balance</h2>
            <p style={styles.muted}>
              {analyticsLevel === 'division'
                ? 'Filtered to the selected division.'
                : analyticsLevel === 'class'
                  ? 'Filtered to the selected class.'
                  : 'School-wide unless you narrow class or division in the tree.'}{' '}
              Open a student record to post payments or review invoices.
            </p>
            {studentsOut.length === 0 ? (
              <p style={styles.muted}>No outstanding students in this filter.</p>
            ) : (
              <table style={styles.table}>
                <thead>
                  <tr>
                    <th style={styles.th}>Roll</th>
                    <th style={styles.th}>Remaining</th>
                    <th style={styles.th}>Student</th>
                  </tr>
                </thead>
                <tbody>
                  {studentsOut.map((r) => (
                    <tr key={r.studentId}>
                      <td style={styles.td}>{r.rollNumber ?? '—'}</td>
                      <td style={{ ...styles.td, textAlign: 'right', fontWeight: 700 }}>{money(r.remainingAmount)}</td>
                      <td style={styles.td}>
                        <Link to={ROUTES.STUDENT_DETAIL(r.studentId)} style={styles.linkBtn}>
                          Open student
                        </Link>
                      </td>
                    </tr>
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

const styles: Record<string, CSSProperties> = {
  toolbarCard: {
    display: 'flex',
    flexWrap: 'wrap',
    gap: 16,
    marginBottom: 20,
    padding: '16px 18px',
    background: t.surface,
    borderRadius: t.radius,
    border: `1px solid ${t.border}`,
    boxShadow: t.shadow,
    alignItems: 'flex-end',
  },
  scopeStrip: {
    marginBottom: 18,
    padding: '14px 18px',
    borderRadius: t.radius,
    background: t.accentSoft,
    border: `1px solid ${t.accentMuted}`,
    borderLeft: `4px solid ${t.accent}`,
    display: 'flex',
    flexDirection: 'column',
    gap: 6,
  },
  scopeK: {
    fontSize: 11,
    letterSpacing: '0.08em',
    textTransform: 'uppercase',
    color: t.accent,
    fontWeight: 700,
  },
  scopeLine: { fontSize: 15, fontWeight: 700, color: t.text },
  emptyBox: {
    padding: '28px 24px',
    borderRadius: t.radius,
    border: `1px dashed ${t.borderStrong}`,
    background: t.bgPage,
    marginBottom: 16,
    textAlign: 'center' as const,
    maxWidth: 520,
    marginLeft: 'auto',
    marginRight: 'auto',
  },
  emptyTitle: { display: 'block', marginBottom: 8, fontSize: 17, fontWeight: 700, color: t.text },
  drillHint: {
    margin: '0 0 18px',
    padding: '14px 16px',
    fontSize: 14,
    lineHeight: 1.55,
    color: t.textSecondary,
    background: t.accentSoft,
    border: `1px solid ${t.accentMuted}`,
    borderRadius: t.radius,
  },
  lab: { display: 'flex', flexDirection: 'column', gap: 8, fontSize: 12, fontWeight: 600, color: t.textSecondary },
  select: {
    padding: '10px 14px',
    borderRadius: 8,
    border: `1px solid ${t.borderStrong}`,
    minWidth: 240,
    fontSize: 14,
    fontFamily: t.fontSans,
    color: t.text,
    background: t.surface,
    boxShadow: 'inset 0 1px 1px rgba(0,0,0,0.04)',
  },
  error: {
    color: t.danger,
    marginBottom: 14,
    padding: '12px 14px',
    background: t.dangerSoft,
    borderRadius: 8,
    border: `1px solid #fecaca`,
    fontSize: 14,
  },
  muted: { color: t.textMuted, fontSize: 14, lineHeight: 1.55 },
  spin: { padding: 40, display: 'flex', justifyContent: 'center' },
  section: {
    marginBottom: 24,
    padding: '22px 22px',
    background: t.surface,
    borderRadius: t.radius,
    border: `1px solid ${t.border}`,
    boxShadow: t.shadow,
  },
  h2: {
    margin: '0 0 6px',
    fontSize: 15,
    fontWeight: 700,
    color: t.text,
    letterSpacing: '-0.02em',
  },
  sectionHint: {
    margin: '0 0 16px',
    fontSize: 13,
    lineHeight: 1.55,
    color: t.textMuted,
  },
  kpiRow: { display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(168px, 1fr))', gap: 14, marginBottom: 22 },
  kpi: {
    padding: '18px 16px',
    background: t.surface,
    borderRadius: t.radius,
    border: `1px solid ${t.border}`,
    boxShadow: t.shadow,
    borderTop: `3px solid ${t.success}`,
    transition: 'box-shadow 0.15s ease',
  },
  kpiLab: {
    fontSize: 11,
    fontWeight: 600,
    letterSpacing: '0.06em',
    textTransform: 'uppercase',
    color: t.textMuted,
  },
  kpiVal: { fontSize: 24, fontWeight: 700, color: t.text, letterSpacing: '-0.02em', marginTop: 8 },
  kpiSub: { marginTop: 8, fontSize: 13, color: t.textMuted, lineHeight: 1.4 },
  tableWrap: { borderRadius: 8, border: `1px solid ${t.border}`, overflow: 'hidden' },
  table: { width: '100%', borderCollapse: 'collapse', fontSize: 14 },
  th: {
    textAlign: 'left',
    padding: '12px 14px',
    background: t.bgPage,
    color: t.textSecondary,
    fontSize: 11,
    fontWeight: 600,
    textTransform: 'uppercase',
    letterSpacing: '0.05em',
    borderBottom: `1px solid ${t.border}`,
  },
  td: {
    padding: '12px 14px',
    borderBottom: `1px solid ${t.border}`,
    color: t.text,
  },
  trend: { display: 'flex', flexDirection: 'column', gap: 10 },
  trendRow: {
    display: 'grid',
    gridTemplateColumns: '108px 1fr 108px',
    alignItems: 'center',
    gap: 14,
    padding: '6px 0',
  },
  trendDate: { fontSize: 12, color: t.textMuted, fontVariantNumeric: 'tabular-nums' },
  trendBarTrack: { height: 12, background: t.bgPage, borderRadius: 6, overflow: 'hidden', border: `1px solid ${t.border}` },
  trendBar: {
    height: '100%',
    background: `linear-gradient(90deg, ${t.accent}, #818cf8)`,
    borderRadius: 5,
  },
  trendAmt: { textAlign: 'right', fontSize: 13, fontWeight: 600, fontVariantNumeric: 'tabular-nums' },
  linkBtn: {
    display: 'inline-block',
    padding: '6px 12px',
    borderRadius: 6,
    fontSize: 13,
    fontWeight: 600,
    color: t.accent,
    background: t.accentSoft,
    border: `1px solid ${t.accentMuted}`,
    textDecoration: 'none',
  },
}
