import type { CSSProperties } from 'react'
import { useEffect, useMemo, useState } from 'react'
import { Spinner } from '../../../shared'
import { useOrgHierarchy } from '../../../layouts/OrgHierarchyProvider'
import { studentsApi, normalizeStudentFullFromApi } from '../api/students.api'
import { referenceDataApi } from '../api/referenceData.api'
import type { Student } from '../students.types'
import type { DivisionItem } from '../api/referenceData.api'
import { AdmissionOrgScopeBanner } from '../components/AdmissionOrgScopeBanner'
import { AdmissionIntakeKpiStrip } from '../components/AdmissionIntakeKpiStrip'
import { ORG_TREE_UNGROUPED_KEY } from '../../../shared/components/OrganizationHierarchyTree'
import { branchesInGroupScope } from '../utils/loadStudentsForOrgTree'
import { buildIntakeKpiForScopeLevel } from '../utils/admissionIntakeMetrics'

/** Deepest level selected in the org tree drives which analytics block is shown. */
type AnalyticsLevel = 'group' | 'school' | 'class' | 'division'

type DistRow = { key: string; count: number }

function norm(s: string | undefined | null): string {
  return String(s ?? '')
    .trim()
    .toLowerCase()
}

function groupBy(students: Student[], field: keyof Student): DistRow[] {
  const map: Record<string, number> = {}
  for (const s of students) {
    const v = s[field]
    const key = v != null && String(v).trim() !== '' ? String(v) : '—'
    map[key] = (map[key] ?? 0) + 1
  }
  return Object.entries(map)
    .map(([key, count]) => ({ key, count }))
    .sort((a, b) => b.count - a.count)
}

function getDivisionSeats(d: DivisionItem): number {
  const n = d.availableSeats ?? (d as unknown as { AvailableSeats?: number }).AvailableSeats
  return typeof n === 'number' ? n : 0
}

function getClassId(d: DivisionItem): string | undefined {
  return d.classId ?? (d as unknown as { ClassId?: string }).ClassId
}

function getBranchIdFromDivision(d: DivisionItem): string | undefined {
  return d.branchId ?? (d as unknown as { BranchId?: string }).BranchId
}

function mergeDistRows(lists: { key?: string; Key?: string; count: number }[][]): DistRow[] {
  const map: Record<string, number> = {}
  for (const arr of lists) {
    for (const r of arr) {
      const k = r.key ?? r.Key ?? '—'
      map[k] = (map[k] ?? 0) + r.count
    }
  }
  return Object.entries(map)
    .map(([key, count]) => ({ key, count }))
    .sort((a, b) => b.count - a.count)
}

function mergeStrength(lists: { id?: string; count: number }[][]): { id?: string; count: number }[] {
  const map: Record<string, number> = {}
  for (const arr of lists) {
    for (const r of arr) {
      const id = r.id?.trim() ? r.id : ''
      const k = id || '__none__'
      map[k] = (map[k] ?? 0) + r.count
    }
  }
  return Object.entries(map).map(([k, count]) => ({
    id: k === '__none__' ? undefined : k,
    count,
  }))
}

function mergeStudents(lists: Student[][]): Student[] {
  const seen = new Set<string>()
  const out: Student[] = []
  for (const list of lists) {
    for (const s of list) {
      if (seen.has(s.id)) continue
      seen.add(s.id)
      out.push(s)
    }
  }
  return out
}

function normalizeSearchStudents(raw: Student[]): Student[] {
  return raw
    .map((r) => normalizeStudentFullFromApi(r as unknown))
    .filter((s): s is Student => s != null)
}

type SchoolRow = { branchId: string; name: string; total: number; casteTop: string }

export function AdmissionAnalyticsPage() {
  const org = useOrgHierarchy()
  const [allDivisions, setAllDivisions] = useState<DivisionItem[]>([])
  const [branchStudents, setBranchStudents] = useState<Student[]>([])
  const [branchStrengthByDivision, setBranchStrengthByDivision] = useState<{ id?: string; count: number }[]>([])
  const [branchGender, setBranchGender] = useState<DistRow[]>([])
  const [branchCaste, setBranchCaste] = useState<DistRow[]>([])
  const [branchCategory, setBranchCategory] = useState<DistRow[]>([])
  const [branchLoading, setBranchLoading] = useState(false)
  const [schoolWise, setSchoolWise] = useState<SchoolRow[]>([])
  const [schoolWiseLoading, setSchoolWiseLoading] = useState(false)

  useEffect(() => {
    referenceDataApi.getDivisions().then(setAllDivisions).catch(() => setAllDivisions([]))
  }, [])

  const groupBranches = useMemo(() => {
    if (org.branchId.trim()) {
      const br = org.branches.find((b) => norm(b.id) === norm(org.branchId))
      const gid = br?.groupId != null && String(br.groupId).trim() ? String(br.groupId) : ''
      if (!gid) {
        const one = org.branches.find((b) => norm(b.id) === norm(org.branchId))
        return one ? [one] : []
      }
      return org.branches.filter((b) => norm(String(b.groupId ?? '')) === norm(gid))
    }
    if (org.groupScopeId.trim()) {
      return branchesInGroupScope(org.groupScopeId, org.branches)
    }
    return []
  }, [org.branchId, org.groupScopeId, org.branches])

  /** Combined analytics: one school, or every school in the selected group bucket. */
  const effectiveBranchIds = useMemo(() => {
    if (org.branchId.trim()) return [org.branchId.trim()]
    if (org.groupScopeId.trim()) {
      return branchesInGroupScope(org.groupScopeId, org.branches).map((b) => b.id)
    }
    return []
  }, [org.branchId, org.groupScopeId, org.branches])

  const analyticsLevel = useMemo((): AnalyticsLevel | null => {
    if (!org.branchId.trim() && !org.groupScopeId.trim()) return null
    if (org.divisionId.trim()) return 'division'
    if (org.classId.trim()) return 'class'
    if (org.branchId.trim()) return 'school'
    if (org.groupScopeId.trim()) return 'group'
    return null
  }, [org.branchId, org.groupScopeId, org.classId, org.divisionId])

  const divisionsInScope = useMemo(() => {
    if (effectiveBranchIds.length === 0) return []
    const set = new Set(effectiveBranchIds.map(norm))
    return allDivisions.filter((d) => set.has(norm(getBranchIdFromDivision(d))))
  }, [effectiveBranchIds, allDivisions])

  const divisionsForClass = useMemo(() => {
    if (!org.classId.trim() || !org.branchId.trim()) return []
    return allDivisions.filter(
      (d) =>
        norm(getBranchIdFromDivision(d)) === norm(org.branchId) && norm(getClassId(d)) === norm(org.classId),
    )
  }, [org.classId, org.branchId, allDivisions])

  const branchRemaining = useMemo(() => {
    let remaining = 0
    for (const d of divisionsInScope) {
      const seats = getDivisionSeats(d)
      const count = branchStrengthByDivision.find((x) => norm(x.id) === norm(d.id))?.count ?? 0
      remaining += Math.max(0, seats - count)
    }
    return remaining
  }, [divisionsInScope, branchStrengthByDivision])

  const classRemaining = useMemo(() => {
    let remaining = 0
    for (const d of divisionsForClass) {
      const seats = getDivisionSeats(d)
      const count = branchStrengthByDivision.find((x) => norm(x.id) === norm(d.id))?.count ?? 0
      remaining += Math.max(0, seats - count)
    }
    return remaining
  }, [divisionsForClass, branchStrengthByDivision])

  const classStudents = useMemo(() => {
    if (!org.classId.trim()) return []
    return branchStudents.filter((su) => norm(su.classId) === norm(org.classId))
  }, [branchStudents, org.classId])

  const divisionStudents = useMemo(() => {
    if (!org.divisionId.trim()) return []
    if (analyticsLevel === 'division') return branchStudents
    return classStudents.filter((su) => norm(su.divisionId) === norm(org.divisionId))
  }, [analyticsLevel, branchStudents, classStudents, org.divisionId])

  const divisionRemaining = useMemo(() => {
    const d = allDivisions.find((x) => x.id === org.divisionId)
    if (!d) return null
    const seats = getDivisionSeats(d)
    const fromApi = branchStrengthByDivision.find((x) => norm(x.id) === norm(d.id))?.count
    const count = fromApi ?? divisionStudents.length
    return Math.max(0, seats - count)
  }, [org.divisionId, allDivisions, branchStrengthByDivision, divisionStudents.length])

  const branchPrevSchool = useMemo(() => groupBy(branchStudents, 'previousSchool'), [branchStudents])
  const branchEws = useMemo(() => groupBy(branchStudents, 'ewsDgCategory'), [branchStudents])
  const classGender = useMemo(() => groupBy(classStudents, 'gender'), [classStudents])
  const classCaste = useMemo(() => groupBy(classStudents, 'caste'), [classStudents])
  const classCategory = useMemo(() => groupBy(classStudents, 'category'), [classStudents])
  const classPrevSchool = useMemo(() => groupBy(classStudents, 'previousSchool'), [classStudents])
  const classEws = useMemo(() => groupBy(classStudents, 'ewsDgCategory'), [classStudents])
  const divGender = useMemo(() => groupBy(divisionStudents, 'gender'), [divisionStudents])
  const divCaste = useMemo(() => groupBy(divisionStudents, 'caste'), [divisionStudents])
  const divCategory = useMemo(() => groupBy(divisionStudents, 'category'), [divisionStudents])
  const divPrevSchool = useMemo(() => groupBy(divisionStudents, 'previousSchool'), [divisionStudents])
  const divEws = useMemo(() => groupBy(divisionStudents, 'ewsDgCategory'), [divisionStudents])

  const scopeBranchKey = effectiveBranchIds.join('|')

  useEffect(() => {
    if (!org.ready || org.loading || analyticsLevel !== 'group' || groupBranches.length === 0) {
      setSchoolWise([])
      return
    }
    setSchoolWiseLoading(true)
    void Promise.all(
      groupBranches.map(async (b) => {
        try {
          const [students, caste] = await Promise.all([
            studentsApi.getByBranch(b.id),
            studentsApi.getCasteWiseDistribution(b.id),
          ])
          const rows = mergeDistRows([caste as { key?: string; Key?: string; count: number }[]])
          const casteTop = rows
            .slice(0, 3)
            .map((r) => `${r.key}: ${r.count}`)
            .join('; ')
          return {
            branchId: b.id,
            name: b.name ?? b.id,
            total: students.length,
            casteTop: casteTop || '—',
          } satisfies SchoolRow
        } catch {
          return {
            branchId: b.id,
            name: b.name ?? b.id,
            total: 0,
            casteTop: '—',
          } satisfies SchoolRow
        }
      }),
    )
      .then(setSchoolWise)
      .finally(() => setSchoolWiseLoading(false))
  }, [org.ready, org.loading, analyticsLevel, groupBranches])

  const scopedSearchKey = `${analyticsLevel ?? ''}|${org.branchId}|${org.classId}|${org.divisionId}`

  useEffect(() => {
    if (!org.ready || org.loading) {
      setBranchStudents([])
      setBranchStrengthByDivision([])
      setBranchGender([])
      setBranchCaste([])
      setBranchCategory([])
      setBranchLoading(false)
      return
    }

    if (!analyticsLevel) {
      setBranchStudents([])
      setBranchStrengthByDivision([])
      setBranchGender([])
      setBranchCaste([])
      setBranchCategory([])
      setBranchLoading(false)
      return
    }

    if (analyticsLevel === 'group' || analyticsLevel === 'school') {
      if (effectiveBranchIds.length === 0) {
        setBranchStudents([])
        setBranchStrengthByDivision([])
        setBranchGender([])
        setBranchCaste([])
        setBranchCategory([])
        setBranchLoading(false)
        return
      }
      setBranchLoading(true)
      Promise.all(
        effectiveBranchIds.map((bid) =>
          Promise.all([
            studentsApi.getByBranch(bid),
            studentsApi.getStrengthByDivision(bid),
            studentsApi.getGenderDistribution(bid),
            studentsApi.getCasteWiseDistribution(bid),
            studentsApi.getCategoryWiseDistribution(bid),
          ]),
        ),
      )
        .then((results) => {
          const studentLists = results.map((r) => r[0])
          const strengthLists = results.map((r) => r[1])
          const genderLists = results.map((r) => r[2])
          const casteLists = results.map((r) => r[3])
          const categoryLists = results.map((r) => r[4])
          const students = mergeStudents(studentLists.map((list) => normalizeSearchStudents(list)))
          setBranchStudents(students)
          setBranchStrengthByDivision(mergeStrength(strengthLists))
          setBranchGender(mergeDistRows(genderLists))
          setBranchCaste(mergeDistRows(casteLists))
          setBranchCategory(mergeDistRows(categoryLists))
        })
        .catch(() => {
          setBranchStudents([])
          setBranchStrengthByDivision([])
          setBranchGender([])
          setBranchCaste([])
          setBranchCategory([])
        })
        .finally(() => setBranchLoading(false))
      return
    }

    if (analyticsLevel === 'class') {
      if (!org.branchId.trim() || !org.classId.trim()) {
        setBranchStudents([])
        setBranchStrengthByDivision([])
        setBranchGender([])
        setBranchCaste([])
        setBranchCategory([])
        setBranchLoading(false)
        return
      }
      const bid = org.branchId.trim()
      const cid = org.classId.trim()
      setBranchLoading(true)
      Promise.all([studentsApi.search({ branchId: bid, classId: cid }), studentsApi.getStrengthByDivision(bid)])
        .then(([rawStudents, strength]) => {
          const students = normalizeSearchStudents(rawStudents)
          const divSet = new Set(
            allDivisions
              .filter((d) => norm(getBranchIdFromDivision(d)) === norm(bid) && norm(getClassId(d)) === norm(cid))
              .map((d) => norm(d.id)),
          )
          const filteredStrength = strength.filter((s) => s.id && divSet.has(norm(s.id)))
          setBranchStudents(students)
          setBranchStrengthByDivision(filteredStrength)
          setBranchGender(groupBy(students, 'gender'))
          setBranchCaste(groupBy(students, 'caste'))
          setBranchCategory(groupBy(students, 'category'))
        })
        .catch(() => {
          setBranchStudents([])
          setBranchStrengthByDivision([])
          setBranchGender([])
          setBranchCaste([])
          setBranchCategory([])
        })
        .finally(() => setBranchLoading(false))
      return
    }

    if (analyticsLevel === 'division') {
      if (!org.branchId.trim() || !org.divisionId.trim()) {
        setBranchStudents([])
        setBranchStrengthByDivision([])
        setBranchGender([])
        setBranchCaste([])
        setBranchCategory([])
        setBranchLoading(false)
        return
      }
      const bid = org.branchId.trim()
      const did = org.divisionId.trim()
      const searchFilter: {
        branchId: string
        divisionId: string
        classId?: string
      } = { branchId: bid, divisionId: did }
      if (org.classId.trim()) searchFilter.classId = org.classId.trim()
      setBranchLoading(true)
      Promise.all([studentsApi.search(searchFilter), studentsApi.getStrengthByDivision(bid)])
        .then(([rawStudents, strength]) => {
          const students = normalizeSearchStudents(rawStudents)
          const filteredStrength = strength.filter((s) => norm(s.id) === norm(did))
          setBranchStudents(students)
          setBranchStrengthByDivision(filteredStrength)
          setBranchGender(groupBy(students, 'gender'))
          setBranchCaste(groupBy(students, 'caste'))
          setBranchCategory(groupBy(students, 'category'))
        })
        .catch(() => {
          setBranchStudents([])
          setBranchStrengthByDivision([])
          setBranchGender([])
          setBranchCaste([])
          setBranchCategory([])
        })
        .finally(() => setBranchLoading(false))
    }
  }, [org.ready, org.loading, analyticsLevel, scopeBranchKey, scopedSearchKey, allDivisions, effectiveBranchIds])

  const selectedClass =
    org.treeClasses.find((c) => norm(c.id) === norm(org.classId)) ??
    org.classes.find((c) => norm(c.id) === norm(org.classId))
  const selectedDivision =
    org.treeDivisions.find((d) => norm(d.id) === norm(org.divisionId)) ??
    org.divisions.find((d) => norm(d.id) === norm(org.divisionId))

  const groupAnalyticsTitle = useMemo(() => {
    if (!org.groupScopeId.trim()) return 'School group'
    if (norm(org.groupScopeId) === norm(ORG_TREE_UNGROUPED_KEY)) {
      return 'School group — All ungrouped schools'
    }
    const g = org.schoolGroups.find((x) => norm(x.id) === norm(org.groupScopeId))
    const n = g?.name?.trim()
    const suffix =
      effectiveBranchIds.length > 0 ? ` (${effectiveBranchIds.length} school${effectiveBranchIds.length === 1 ? '' : 's'})` : ''
    return `School group — ${n || 'Selected group'}${suffix}`
  }, [org.groupScopeId, org.schoolGroups, effectiveBranchIds.length])

  const schoolAnalyticsTitle = useMemo(() => {
    const name = org.branches.find((b) => norm(b.id) === norm(org.branchId))?.name ?? org.branchId
    return `School-wide — ${name || 'School'}`
  }, [org.branchId, org.branches])

  const classAnalyticsTitle = `Class-wide — ${selectedClass?.name ?? org.classId}`
  const divisionAnalyticsTitle = `Division-wide — ${selectedDivision?.name ?? org.divisionId}`

  const intakeKpi = useMemo(() => {
    if (!analyticsLevel) return null
    const brName = org.branches.find((b) => norm(b.id) === norm(org.branchId))?.name
    const title =
      analyticsLevel === 'group'
        ? groupAnalyticsTitle
        : analyticsLevel === 'school'
          ? `School — ${brName ?? (org.branchId.trim() || 'School')}`
          : analyticsLevel === 'class'
            ? `Class — ${selectedClass?.name ?? org.classId}`
            : `Division — ${selectedDivision?.name ?? org.divisionId}`
    return buildIntakeKpiForScopeLevel(analyticsLevel, {
      title,
      effectiveBranchIds,
      branchId: org.branchId,
      classId: org.classId,
      divisionId: org.divisionId,
      allDivisions,
      scopedStudents: branchStudents,
      strengthByDivision: branchStrengthByDivision,
    })
  }, [
    analyticsLevel,
    groupAnalyticsTitle,
    org.branchId,
    org.classId,
    org.divisionId,
    org.branches,
    effectiveBranchIds,
    allDivisions,
    branchStudents,
    branchStrengthByDivision,
    selectedClass,
    selectedDivision,
  ])

  /** Per-school breakdown only when a school group is selected (never when drilling to a single branch). */
  const showSchoolWiseTable = analyticsLevel === 'group'

  const renderSection = (
    title: string,
    total: number,
    remaining: number | null,
    gender: DistRow[],
    caste: DistRow[],
    category: DistRow[],
    prevSchool: DistRow[],
    ews: DistRow[],
    loadingFlag: boolean,
  ) => (
    <section style={styles.section}>
      <h3>{title}</h3>
      {loadingFlag && (
        <p>
          <Spinner /> Loading…
        </p>
      )}
      {!loadingFlag && (
        <>
          <div className="form-grid" style={styles.cards}>
            <div style={styles.card}>
              <div style={styles.cardValue}>{total}</div>
              <div style={styles.cardLabel}>Total Admission</div>
            </div>
            <div style={styles.card}>
              <div style={styles.cardValue}>{remaining != null ? remaining : '—'}</div>
              <div style={styles.cardLabel}>Remaining Seats</div>
            </div>
          </div>
          <h4 style={styles.h4}>Male / Female</h4>
          <table style={styles.table}>
            <thead>
              <tr>
                <th style={styles.th}>Gender</th>
                <th style={styles.th}>Count</th>
              </tr>
            </thead>
            <tbody>
              {gender.map((r) => (
                <tr key={r.key}>
                  <td style={styles.td}>{r.key}</td>
                  <td style={styles.td}>{r.count}</td>
                </tr>
              ))}
              {gender.length === 0 && (
                <tr>
                  <td colSpan={2} style={styles.td}>
                    —
                  </td>
                </tr>
              )}
            </tbody>
          </table>
          <h4 style={styles.h4}>Caste-wise</h4>
          <table style={styles.table}>
            <thead>
              <tr>
                <th style={styles.th}>Caste</th>
                <th style={styles.th}>Count</th>
              </tr>
            </thead>
            <tbody>
              {caste.slice(0, 15).map((r) => (
                <tr key={r.key}>
                  <td style={styles.td}>{r.key}</td>
                  <td style={styles.td}>{r.count}</td>
                </tr>
              ))}
              {caste.length === 0 && (
                <tr>
                  <td colSpan={2} style={styles.td}>
                    —
                  </td>
                </tr>
              )}
            </tbody>
          </table>
          <h4 style={styles.h4}>Category-wise</h4>
          <table style={styles.table}>
            <thead>
              <tr>
                <th style={styles.th}>Category</th>
                <th style={styles.th}>Count</th>
              </tr>
            </thead>
            <tbody>
              {category.map((r) => (
                <tr key={r.key}>
                  <td style={styles.td}>{r.key}</td>
                  <td style={styles.td}>{r.count}</td>
                </tr>
              ))}
              {category.length === 0 && (
                <tr>
                  <td colSpan={2} style={styles.td}>
                    —
                  </td>
                </tr>
              )}
            </tbody>
          </table>
          <h4 style={styles.h4}>Previous School-wise</h4>
          <table style={styles.table}>
            <thead>
              <tr>
                <th style={styles.th}>Previous School</th>
                <th style={styles.th}>Count</th>
              </tr>
            </thead>
            <tbody>
              {prevSchool.slice(0, 10).map((r) => (
                <tr key={r.key}>
                  <td style={styles.td}>{r.key}</td>
                  <td style={styles.td}>{r.count}</td>
                </tr>
              ))}
              {prevSchool.length === 0 && (
                <tr>
                  <td colSpan={2} style={styles.td}>
                    —
                  </td>
                </tr>
              )}
            </tbody>
          </table>
          <h4 style={styles.h4}>EWS / DG Category</h4>
          <table style={styles.table}>
            <thead>
              <tr>
                <th style={styles.th}>EWS/DG</th>
                <th style={styles.th}>Count</th>
              </tr>
            </thead>
            <tbody>
              {ews.map((r) => (
                <tr key={r.key}>
                  <td style={styles.td}>{r.key}</td>
                  <td style={styles.td}>{r.count}</td>
                </tr>
              ))}
              {ews.length === 0 && (
                <tr>
                  <td colSpan={2} style={styles.td}>
                    —
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </>
      )}
    </section>
  )

  if (!org.ready || org.loading) {
    return (
      <p>
        <Spinner /> Loading…
      </p>
    )
  }

  return (
    <div style={styles.wrap}>
      <AdmissionOrgScopeBanner />
      <div style={styles.pageHead}>
        <h2 style={styles.h2}>Enrollment &amp; intake analytics</h2>
        <p style={styles.lead}>
          ERP-style summary driven by the organization tree. The strip below matches the <strong>selected node only</strong>{' '}
          (group, school, class, or division)—enrolled, capacity when configured on divisions, and remaining seats. Detail
          tables follow for that same scope.
        </p>
      </div>

      {!org.branchId.trim() && !org.groupScopeId.trim() ? (
        <p style={styles.muted}>Select a school group or a school in the organization tree to load analytics.</p>
      ) : (
        <>
          <AdmissionIntakeKpiStrip kpi={intakeKpi} loading={branchLoading} />
          {showSchoolWiseTable && (
            <section style={styles.section}>
              <h3>School group — per-school admissions</h3>
              <p style={styles.tableHint}>
                Compare intake across schools in this management group. The summary tables below are still for the whole group
                selection only.
              </p>
              {schoolWiseLoading ? (
                <p>
                  <Spinner /> Loading…
                </p>
              ) : (
                <table style={styles.table}>
                  <thead>
                    <tr>
                      <th style={styles.th}>School</th>
                      <th style={styles.th}>Admissions</th>
                      <th style={styles.th}>Caste (top)</th>
                    </tr>
                  </thead>
                  <tbody>
                    {schoolWise.map((r) => (
                      <tr key={r.branchId}>
                        <td style={styles.td}>{r.name}</td>
                        <td style={styles.td}>{r.total}</td>
                        <td style={{ ...styles.td, fontSize: 13 }}>{r.casteTop}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </section>
          )}

          {effectiveBranchIds.length > 0 &&
            analyticsLevel === 'group' &&
            renderSection(
              groupAnalyticsTitle,
              branchStudents.length,
              branchRemaining,
              branchGender,
              branchCaste,
              branchCategory,
              branchPrevSchool,
              branchEws,
              branchLoading,
            )}

          {effectiveBranchIds.length > 0 &&
            analyticsLevel === 'school' &&
            renderSection(
              schoolAnalyticsTitle,
              branchStudents.length,
              branchRemaining,
              branchGender,
              branchCaste,
              branchCategory,
              branchPrevSchool,
              branchEws,
              branchLoading,
            )}

          {analyticsLevel === 'class' && !org.branchId.trim() ? (
            <p style={styles.muted}>
              Select a school (branch) in the tree. Class-level analytics are always for a single branch—data from multiple
              schools is not combined here.
            </p>
          ) : null}

          {analyticsLevel === 'class' &&
            org.branchId.trim() &&
            renderSection(
              classAnalyticsTitle,
              classStudents.length,
              classRemaining,
              classGender,
              classCaste,
              classCategory,
              classPrevSchool,
              classEws,
              branchLoading,
            )}

          {analyticsLevel === 'division' && !org.branchId.trim() ? (
            <p style={styles.muted}>Select a school (branch) in the tree to load division-level analytics.</p>
          ) : null}

          {analyticsLevel === 'division' &&
            org.branchId.trim() &&
            renderSection(
              divisionAnalyticsTitle,
              divisionStudents.length,
              divisionRemaining,
              divGender,
              divCaste,
              divCategory,
              divPrevSchool,
              divEws,
              branchLoading,
            )}
        </>
      )}
    </div>
  )
}

const styles: Record<string, CSSProperties> = {
  wrap: { padding: 0 },
  pageHead: { marginBottom: 8 },
  h2: { margin: '0 0 8px', fontSize: 22, fontWeight: 800, color: '#0f172a', letterSpacing: '-0.02em' },
  lead: { margin: '0 0 16px', fontSize: 14, color: '#64748b', maxWidth: 820, lineHeight: 1.55 },
  section: { marginBottom: 32 },
  cards: { display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(160px, 1fr))', gap: '16px', marginTop: 8 },
  card: { background: '#fff', padding: 20, borderRadius: 8, boxShadow: '0 1px 3px rgba(0,0,0,0.1)' },
  cardValue: { fontSize: 24, fontWeight: 700, color: '#1e3a5f' },
  cardLabel: { fontSize: 13, color: '#666', marginTop: 4 },
  h4: { marginTop: 16, marginBottom: 8, fontSize: 16 },
  table: {
    borderCollapse: 'collapse',
    background: '#fff',
    borderRadius: 8,
    overflow: 'hidden',
    boxShadow: '0 1px 3px rgba(0,0,0,0.1)',
    marginBottom: 16,
  },
  th: { textAlign: 'left', padding: '10px 12px', background: '#1e3a5f', color: '#fff', fontSize: 14 },
  td: { padding: '10px 12px', borderBottom: '1px solid #eee' },
  muted: { color: '#666', fontSize: 14 },
  tableHint: { margin: '0 0 12px', fontSize: 13, color: '#64748b', maxWidth: 720 },
}
