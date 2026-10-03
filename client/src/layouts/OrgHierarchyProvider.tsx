import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from 'react'
import { referenceDataApi } from '../features/admission/api/referenceData.api'
import { branchesApi, type Branch } from '../features/branches'
import { schoolGroupApi, type SchoolGroup } from '../features/schoolGroups'
import type { OrgTreeSelection } from '../shared/components/OrganizationHierarchyTree'

type ClassRow = { id: string; name?: string; branchId?: string }
type DivRow = { id: string; name?: string; classId?: string }

export type OrgHierarchyContextValue = {
  /** Initial organization load attempt finished. */
  ready: boolean
  loading: boolean
  /** Set when branches could not be loaded (sidebar needs branches). */
  error: string | null
  /** Non-fatal issues (e.g. groups endpoint failed but branches ok). */
  loadWarnings: string[]
  schoolGroups: SchoolGroup[]
  branches: Branch[]
  /** Classes/divisions for the current branch & class selection (used across ERP pages). */
  classes: ClassRow[]
  divisions: DivRow[]
  /** Full org lists for the sidebar tree (all branches / classes / divisions). */
  treeClasses: ClassRow[]
  treeDivisions: DivRow[]
  /** School group bucket when user clicked a group row (all schools in that group). Empty when a school is selected. */
  groupScopeId: string
  branchId: string
  classId: string
  divisionId: string
  setOrgSelection: (next: OrgTreeSelection) => void
  refreshHierarchy: () => Promise<void>
  /** True while re-fetching after the initial load (tree can stay visible). */
  hierarchyRefreshing: boolean
}

const OrgHierarchyContext = createContext<OrgHierarchyContextValue | null>(null)

export function OrgHierarchyProvider({ children }: { children: ReactNode }) {
  const [schoolGroups, setSchoolGroups] = useState<SchoolGroup[]>([])
  const [branches, setBranches] = useState<Branch[]>([])
  const [classes, setClasses] = useState<ClassRow[]>([])
  const [divisions, setDivisions] = useState<DivRow[]>([])
  const [treeClasses, setTreeClasses] = useState<ClassRow[]>([])
  const [treeDivisions, setTreeDivisions] = useState<DivRow[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [loadWarnings, setLoadWarnings] = useState<string[]>([])

  const [groupScopeId, setGroupScopeId] = useState('')
  const [branchId, setBranchId] = useState('')
  const [classId, setClassId] = useState('')
  const [divisionId, setDivisionId] = useState('')
  const [refreshTick, setRefreshTick] = useState(0)
  const [hierarchyRefreshing, setHierarchyRefreshing] = useState(false)
  const initialHierarchyFetchDoneRef = useRef(false)

  const refreshHierarchy = useCallback(async () => {
    setRefreshTick((x) => x + 1)
  }, [])

  useEffect(() => {
    let cancelled = false
    if (!initialHierarchyFetchDoneRef.current) {
      setLoading(true)
    } else {
      setHierarchyRefreshing(true)
    }
    setError(null)
    setLoadWarnings([])

    const run = async () => {
      const warnings: string[] = []

      const rGroups = await Promise.resolve(schoolGroupApi.getAll()).then(
        (v) => ({ ok: true as const, v }),
        () => {
          warnings.push('School groups could not be loaded (continuing with schools list).')
          return { ok: false as const, v: [] as SchoolGroup[] }
        },
      )

      const rBranches = await Promise.resolve(branchesApi.getAll()).then(
        (v) => ({ ok: true as const, v }),
        () => ({ ok: false as const, v: [] as Branch[] }),
      )

      const rTreeClasses = await Promise.resolve(referenceDataApi.getClasses()).then(
        (v) => ({ ok: true as const, v }),
        () => {
          warnings.push('Classes could not be loaded for hierarchy tree.')
          return { ok: false as const, v: [] as ClassRow[] }
        },
      )

      const rTreeDivisions = await Promise.resolve(referenceDataApi.getDivisions()).then(
        (v) =>
          ({
            ok: true as const,
            v: v.map((d) => ({
              id: d.id,
              name: d.name,
              classId: d.classId,
            })),
          }),
        () => {
          warnings.push('Divisions could not be loaded for hierarchy tree.')
          return { ok: false as const, v: [] as DivRow[] }
        },
      )

      if (cancelled) return

      if (!rGroups.ok) setSchoolGroups([])
      else setSchoolGroups(rGroups.v)

      if (!rBranches.ok) {
        setBranches([])
        setError(
          'Could not load schools (branches). Start the API, PostgreSQL, and sign in — then refresh the page.',
        )
      } else {
        setBranches(rBranches.v)
        setError(null)
        /** SaaS default: no school auto-selected — user must choose scope in the tree. */
        if (rBranches.v.length > 0) {
          setBranchId((prev) => (prev.trim() ? prev : ''))
        }
      }

      setTreeClasses(rTreeClasses.ok ? rTreeClasses.v : [])
      setTreeDivisions(rTreeDivisions.ok ? rTreeDivisions.v : [])

      if (!cancelled && warnings.length > 0) setLoadWarnings(warnings)
    }

    void run().finally(() => {
      if (!cancelled) {
        setLoading(false)
        setHierarchyRefreshing(false)
        initialHierarchyFetchDoneRef.current = true
      }
    })

    return () => {
      cancelled = true
    }
  }, [refreshTick])

  useEffect(() => {
    let cancelled = false
    if (!branchId.trim()) {
      setClasses([])
      setClassId('')
      setDivisionId('')
      return
    }
    void (async () => {
      try {
        const list = await referenceDataApi.getClassesByBranch(branchId)
        if (cancelled) return
        setClasses(list)
        const norm = (s: string) => s.trim().toLowerCase()
        setClassId((prev) => {
          if (!prev.trim()) return ''
          if (list.some((c) => norm(c.id) === norm(prev))) return prev
          return list.length > 0 ? list[0]!.id : ''
        })
      } catch {
        if (!cancelled) {
          setClasses([])
          setClassId('')
        }
      }
    })()
    return () => {
      cancelled = true
    }
  }, [branchId, refreshTick])

  useEffect(() => {
    let cancelled = false
    if (!classId.trim()) {
      setDivisions([])
      setDivisionId('')
      return
    }
    void (async () => {
      try {
        const list = await referenceDataApi.getDivisionsByClass(classId)
        if (cancelled) return
        setDivisions(list)
        const norm = (s: string) => s.trim().toLowerCase()
        setDivisionId((prev) => {
          if (!prev.trim()) return ''
          if (list.some((d) => norm(d.id) === norm(prev))) return prev
          return list.length > 0 ? list[0]!.id : ''
        })
      } catch {
        if (!cancelled) {
          setDivisions([])
          setDivisionId('')
        }
      }
    })()
    return () => {
      cancelled = true
    }
  }, [classId, refreshTick])

  const setOrgSelection = useCallback((next: OrgTreeSelection) => {
    setGroupScopeId(next.groupScopeId ?? '')
    setBranchId(next.branchId)
    setClassId(next.classId)
    setDivisionId(next.divisionId)
  }, [])

  const value = useMemo<OrgHierarchyContextValue>(
    () => ({
      ready: !loading,
      loading,
      error,
      loadWarnings,
      schoolGroups,
      branches,
      classes,
      divisions,
      treeClasses,
      treeDivisions,
      groupScopeId,
      branchId,
      classId,
      divisionId,
      setOrgSelection,
      refreshHierarchy,
      hierarchyRefreshing,
    }),
    [
      loading,
      error,
      loadWarnings,
      schoolGroups,
      branches,
      classes,
      divisions,
      treeClasses,
      treeDivisions,
      groupScopeId,
      branchId,
      classId,
      divisionId,
      setOrgSelection,
      refreshHierarchy,
      hierarchyRefreshing,
    ],
  )

  return <OrgHierarchyContext.Provider value={value}>{children}</OrgHierarchyContext.Provider>
}

export function useOrgHierarchy(): OrgHierarchyContextValue {
  const ctx = useContext(OrgHierarchyContext)
  if (!ctx) throw new Error('useOrgHierarchy must be used within OrgHierarchyProvider')
  return ctx
}
