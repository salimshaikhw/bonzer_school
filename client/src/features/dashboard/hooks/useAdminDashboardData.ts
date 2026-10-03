import { useEffect, useState, useCallback, useMemo } from 'react'
import { useOrgHierarchy } from '../../../layouts/OrgHierarchyProvider'
import { branchesInGroupScope, loadStudentsForOrgScope } from '../../admission/utils/loadStudentsForOrgTree'
import { studentsApi, normalizeStudentFromApi } from '../../admission/api/students.api'
import { referenceDataApi } from '../../admission/api/referenceData.api'
import type { Branch } from '../../branches'
import type { Student } from '../../admission/students.types'

export interface StrengthItem {
  id?: string
  count: number
}

export interface DistributionItem {
  key: string
  count: number
}

export interface YearCount {
  year: number
  count: number
}

function norm(s: string | undefined | null): string {
  return String(s ?? '')
    .trim()
    .toLowerCase()
}

function mergeDistRows(lists: { key: string; count: number }[][]): DistributionItem[] {
  const map: Record<string, number> = {}
  for (const arr of lists) {
    for (const r of arr) {
      const k = r.key ?? '—'
      map[k] = (map[k] ?? 0) + r.count
    }
  }
  return Object.entries(map)
    .map(([key, count]) => ({ key, count }))
    .sort((a, b) => b.count - a.count)
}

function mergeStrength(lists: { id?: string; count: number }[][]): StrengthItem[] {
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

function mergeYearCounts(lists: YearCount[][]): YearCount[] {
  const map: Record<number, number> = {}
  for (const arr of lists) {
    for (const { year, count } of arr) {
      map[year] = (map[year] ?? 0) + count
    }
  }
  return Object.entries(map)
    .map(([y, c]) => ({ year: Number(y), count: c }))
    .sort((a, b) => b.year - a.year)
}

function strengthByClassFromStudents(students: Student[]): StrengthItem[] {
  const map: Record<string, number> = {}
  for (const s of students) {
    const id = s.classId?.trim() ? s.classId : ''
    const k = id || '__none__'
    map[k] = (map[k] ?? 0) + 1
  }
  return Object.entries(map).map(([k, count]) => ({
    id: k === '__none__' ? undefined : k,
    count,
  }))
}

function strengthByDivisionFromStudents(students: Student[]): StrengthItem[] {
  const map: Record<string, number> = {}
  for (const s of students) {
    const id = s.divisionId?.trim() ? s.divisionId : ''
    const k = id || '__none__'
    map[k] = (map[k] ?? 0) + 1
  }
  return Object.entries(map).map(([k, count]) => ({
    id: k === '__none__' ? undefined : k,
    count,
  }))
}

function genderFromStudents(students: Student[]): DistributionItem[] {
  const map: Record<string, number> = {}
  for (const s of students) {
    const g = s.gender?.trim() ? String(s.gender) : '—'
    map[g] = (map[g] ?? 0) + 1
  }
  return Object.entries(map)
    .map(([key, count]) => ({ key, count }))
    .sort((a, b) => b.count - a.count)
}

function admissionTrendFromStudents(students: Student[]): YearCount[] {
  const map: Record<number, number> = {}
  for (const s of students) {
    const d = s.admissionDate?.trim()
    if (!d) continue
    const y = new Date(d).getFullYear()
    if (!Number.isFinite(y)) continue
    map[y] = (map[y] ?? 0) + 1
  }
  return Object.entries(map)
    .map(([y, c]) => ({ year: Number(y), count: c }))
    .sort((a, b) => b.year - a.year)
}

function studentMatchesTree(
  s: Student,
  effectiveBranchIds: string[],
  classId: string,
  divisionId: string,
): boolean {
  const bid = s.branchId ?? ''
  if (effectiveBranchIds.length > 0) {
    if (!effectiveBranchIds.some((id) => norm(id) === norm(bid))) return false
  }
  if (classId.trim() && norm(s.classId) !== norm(classId)) return false
  if (divisionId.trim() && norm(s.divisionId) !== norm(divisionId)) return false
  return true
}

function effectiveBranchIdsFromOrg(
  branchId: string,
  groupScopeId: string,
  branches: Branch[],
): string[] {
  if (branchId.trim()) return [branchId.trim()]
  if (groupScopeId.trim()) return branchesInGroupScope(groupScopeId, branches).map((b) => b.id)
  return []
}

export function useAdminDashboardData() {
  const org = useOrgHierarchy()
  const [classes, setClasses] = useState<{ id: string; name?: string }[]>([])
  const [divisions, setDivisions] = useState<{ id: string; name?: string }[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const [scopedStudents, setScopedStudents] = useState<Student[]>([])
  const [todayAdmissions, setTodayAdmissions] = useState<number>(0)
  const [branchCounts, setBranchCounts] = useState<{ branchId: string; branchName: string; count: number }[]>([])

  const [strengthByClass, setStrengthByClass] = useState<StrengthItem[]>([])
  const [strengthByDivision, setStrengthByDivision] = useState<StrengthItem[]>([])
  const [genderDistribution, setGenderDistribution] = useState<DistributionItem[]>([])
  const [admissionTrend, setAdmissionTrend] = useState<YearCount[]>([])
  const [analyticsLoading, setAnalyticsLoading] = useState(false)

  const hasOrgScope = !!(org.groupScopeId.trim() || org.branchId.trim())
  const effectiveBranchIds = useMemo(
    () => effectiveBranchIdsFromOrg(org.branchId, org.groupScopeId, org.branches),
    [org.branchId, org.groupScopeId, org.branches],
  )

  const scopeKey = `${org.groupScopeId}|${org.branchId}|${org.classId}|${org.divisionId}`

  useEffect(() => {
    referenceDataApi
      .getClasses()
      .then(setClasses)
      .catch(() => setClasses([]))
    referenceDataApi
      .getDivisions()
      .then(setDivisions)
      .catch(() => setDivisions([]))
  }, [])

  useEffect(() => {
    if (!org.ready || org.loading) {
      setLoading(true)
      return
    }
    if (!hasOrgScope) {
      setScopedStudents([])
      setTodayAdmissions(0)
      setBranchCounts([])
      setLoading(false)
      setError(null)
      return
    }

    setLoading(true)
    setError(null)
    void Promise.all([
      loadStudentsForOrgScope(
        {
          branchId: org.branchId,
          classId: org.classId,
          divisionId: org.divisionId,
          groupScopeId: org.groupScopeId,
        },
        org.branches,
      ),
      studentsApi.getTodayAdmissions(),
    ])
      .then(([students, todayList]) => {
        setScopedStudents(students)
        const todayNorm = todayList
          .map((raw) => normalizeStudentFromApi(raw) ?? (raw as Student))
          .filter((x): x is Student => x != null && !!x.id)
        const eff = effectiveBranchIdsFromOrg(org.branchId, org.groupScopeId, org.branches)
        const filtered = todayNorm.filter((s) =>
          studentMatchesTree(s, eff, org.classId, org.divisionId),
        )
        setTodayAdmissions(filtered.length)

        const byBranch: Record<string, number> = {}
        for (const s of students) {
          const b = s.branchId?.trim()
          if (!b) continue
          byBranch[b] = (byBranch[b] ?? 0) + 1
        }
        const rows: { branchId: string; branchName: string; count: number }[] = []
        for (const b of org.branches) {
          if (eff.length > 0 && !eff.some((id) => norm(id) === norm(b.id))) continue
          rows.push({
            branchId: b.id,
            branchName: b.name ?? b.id,
            count: byBranch[b.id] ?? 0,
          })
        }
        setBranchCounts(rows)
      })
      .catch(() => {
        setError('Failed to load dashboard data.')
        setScopedStudents([])
        setTodayAdmissions(0)
        setBranchCounts([])
      })
      .finally(() => setLoading(false))
  }, [org.ready, org.loading, hasOrgScope, scopeKey, org.branches, org.branchId, org.classId, org.divisionId, org.groupScopeId])

  useEffect(() => {
    if (!org.ready || org.loading || !hasOrgScope || effectiveBranchIds.length === 0) {
      setStrengthByClass([])
      setStrengthByDivision([])
      setGenderDistribution([])
      setAdmissionTrend([])
      return
    }

    const drillDown = !!(org.classId.trim() || org.divisionId.trim())
    if (!drillDown && loading) {
      setAnalyticsLoading(true)
      return
    }

    if (drillDown && loading) {
      setStrengthByClass([])
      setStrengthByDivision([])
      setGenderDistribution([])
      setAdmissionTrend([])
      return
    }
    if (drillDown) {
      setAnalyticsLoading(false)
      setStrengthByClass(strengthByClassFromStudents(scopedStudents))
      setStrengthByDivision(strengthByDivisionFromStudents(scopedStudents))
      setGenderDistribution(genderFromStudents(scopedStudents))
      setAdmissionTrend(admissionTrendFromStudents(scopedStudents))
      return
    }

    setAnalyticsLoading(true)
    Promise.all(
      effectiveBranchIds.map((bid) =>
        Promise.all([
          studentsApi.getStrengthByClass(bid),
          studentsApi.getStrengthByDivision(bid),
          studentsApi.getGenderDistribution(bid),
          studentsApi.getAdmissionTrend(bid),
        ]),
      ),
    )
      .then((results) => {
        const cls = results.map((r) => r[0])
        const div = results.map((r) => r[1])
        const gen = results.map((r) => r[2])
        const tr = results.map((r) => r[3])
        setStrengthByClass(mergeStrength(cls))
        setStrengthByDivision(mergeStrength(div))
        setGenderDistribution(mergeDistRows(gen))
        setAdmissionTrend(mergeYearCounts(tr))
      })
      .catch(() => {
        setStrengthByClass([])
        setStrengthByDivision([])
        setGenderDistribution([])
        setAdmissionTrend([])
      })
      .finally(() => setAnalyticsLoading(false))
  }, [
    org.ready,
    org.loading,
    hasOrgScope,
    effectiveBranchIds.join('|'),
    org.classId,
    org.divisionId,
    scopedStudents,
    loading,
  ])

  const getName = useCallback(
    (id: string, list: { id: string; name?: string }[]) =>
      list.find((x) => norm(x.id) === norm(id))?.name ?? id.slice(0, 8),
    [],
  )

  const branchesInScopeCount = effectiveBranchIds.length
  const totalStudents = scopedStudents.length

  return {
    hasOrgScope,
    branchesInScopeCount,
    totalStudents,
    todayAdmissions,
    branchCounts,
    classes,
    divisions,
    strengthByClass,
    strengthByDivision,
    genderDistribution,
    admissionTrend,
    loading,
    analyticsLoading,
    error,
    getName,
  }
}
