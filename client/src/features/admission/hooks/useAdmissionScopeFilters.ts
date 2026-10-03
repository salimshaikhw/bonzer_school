import { useCallback, useEffect, useMemo, useState } from 'react'
import { useOrgHierarchy } from '../../../layouts/OrgHierarchyProvider'
import { referenceDataApi } from '../api/referenceData.api'
import { branchesApi, type Branch } from '../../branches'
import { schoolGroupApi, type SchoolGroup } from '../../schoolGroups'
import type { DivisionItem } from '../api/referenceData.api'
import type { ClassItem } from '../api/referenceData.api'

function norm(s: string | undefined | null): string {
  return String(s ?? '')
    .trim()
    .toLowerCase()
}

export type AdmissionScope = {
  groupId: string
  branchId: string
  classId: string
  divisionId: string
}

export function useAdmissionScopeFilters() {
  const org = useOrgHierarchy()
  const [groups, setGroups] = useState<SchoolGroup[]>([])
  const [branches, setBranches] = useState<Branch[]>([])
  const [classes, setClasses] = useState<ClassItem[]>([])
  const [divisions, setDivisions] = useState<DivisionItem[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const [groupId, setGroupId] = useState('')
  const [branchId, setBranchId] = useState('')
  const [classId, setClassId] = useState('')
  const [divisionId, setDivisionId] = useState('')

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    setError(null)
    Promise.all([
      schoolGroupApi.getAll().catch(() => [] as SchoolGroup[]),
      branchesApi.getAll().catch(() => [] as Branch[]),
      referenceDataApi.getClasses().catch(() => [] as ClassItem[]),
      referenceDataApi.getDivisions().catch(() => [] as DivisionItem[]),
    ])
      .then(([g, b, c, d]) => {
        if (cancelled) return
        setGroups(g)
        setBranches(b)
        setClasses(c)
        setDivisions(d)
      })
      .catch(() => {
        if (!cancelled) setError('Failed to load organization lists.')
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [])

  const branchesForGroup = useMemo(() => {
    if (!groupId) return branches
    return branches.filter((b) => norm(b.groupId) === norm(groupId))
  }, [groupId, branches])

  const classesForBranch = useMemo(() => {
    if (!branchId) return []
    return classes.filter((c) => norm(c.branchId) === norm(branchId))
  }, [branchId, classes])

  const divisionsForClass = useMemo(() => {
    if (!classId) return []
    return divisions.filter((d) => norm(d.classId) === norm(classId))
  }, [classId, divisions])

  const setGroupIdCascade = useCallback((id: string) => {
    setGroupId(id)
    setBranchId('')
    setClassId('')
    setDivisionId('')
  }, [])

  const setBranchIdCascade = useCallback((id: string) => {
    setBranchId(id)
    setClassId('')
    setDivisionId('')
  }, [])

  const setClassIdCascade = useCallback((id: string) => {
    setClassId(id)
    setDivisionId('')
  }, [])

  const applyOrgContext = useCallback(() => {
    const br = org.branches.find((b) => norm(b.id) === norm(org.branchId))
    if (br?.groupId) setGroupId(String(br.groupId))
    else setGroupId('')
    setBranchId(org.branchId)
    setClassId(org.classId)
    setDivisionId(org.divisionId)
  }, [org.branchId, org.classId, org.divisionId, org.branches])

  /** Set all scope fields at once (e.g. deep-link / URL) without cascade resets. */
  const setScopeRaw = useCallback((next: Partial<AdmissionScope>) => {
    setGroupId(next.groupId ?? '')
    setBranchId(next.branchId ?? '')
    setClassId(next.classId ?? '')
    setDivisionId(next.divisionId ?? '')
  }, [])

  const scope: AdmissionScope = useMemo(
    () => ({ groupId, branchId, classId, divisionId }),
    [groupId, branchId, classId, divisionId],
  )

  return {
    groups,
    branches,
    classes,
    divisions,
    branchesForGroup,
    classesForBranch,
    divisionsForClass,
    groupId,
    branchId,
    classId,
    divisionId,
    setGroupId: setGroupIdCascade,
    setBranchId: setBranchIdCascade,
    setClassId: setClassIdCascade,
    setDivisionId,
    setScopeRaw,
    applyOrgContext,
    scope,
    loading,
    error,
  }
}
