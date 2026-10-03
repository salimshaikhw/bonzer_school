import { useCallback, useEffect, useMemo, useState, type CSSProperties } from 'react'

/** Bucket id for schools without a management group (must match server-agnostic clients). */
export const ORG_TREE_UNGROUPED_KEY = '__ungrouped__' as const

const UNGROUPED_KEY = ORG_TREE_UNGROUPED_KEY

export type OrgTreeGroup = { id: string; name?: string }
export type OrgTreeBranch = { id: string; name?: string; groupId?: string | null }
export type OrgTreeClass = { id: string; name?: string; branchId?: string }
export type OrgTreeDivision = { id: string; name?: string; classId?: string }

export type OrgTreeSelection = {
  /** All schools in this group bucket (including {@link ORG_TREE_UNGROUPED_KEY}). Empty when a school/class/division is selected. */
  groupScopeId: string
  branchId: string
  classId: string
  divisionId: string
}

type Props = {
  title?: string
  groups: OrgTreeGroup[]
  branches: OrgTreeBranch[]
  classes: OrgTreeClass[]
  divisions: OrgTreeDivision[]
  /** When set and branch is empty, user selected a school group row only. */
  groupScopeId: string
  branchId: string
  classId: string
  divisionId: string
  onChange: (next: OrgTreeSelection) => void
}

function normalizeId(id: string | undefined | null): string {
  return String(id ?? '')
    .trim()
    .toLowerCase()
}

function getBranchGroupId(b: OrgTreeBranch | undefined): string | undefined {
  if (!b) return undefined
  const g = b.groupId ?? (b as unknown as { GroupId?: string }).GroupId
  return g ? String(g) : undefined
}

function getClassBranchId(c: OrgTreeClass): string | undefined {
  return c.branchId ?? (c as unknown as { BranchId?: string }).BranchId
}

function getDivisionClassId(d: OrgTreeDivision): string | undefined {
  return d.classId ?? (d as unknown as { ClassId?: string }).ClassId
}

function sortByName<T extends { name?: string; id: string }>(items: T[]): T[] {
  return [...items].sort((a, b) =>
    String(a.name ?? a.id).localeCompare(String(b.name ?? b.id), undefined, { sensitivity: 'base' }),
  )
}

function toggleSetMember(set: Set<string>, id: string): Set<string> {
  const next = new Set(set)
  if (next.has(id)) next.delete(id)
  else next.add(id)
  return next
}

export function OrganizationHierarchyTree({
  title = 'Organization',
  groups,
  branches,
  classes,
  divisions,
  groupScopeId,
  branchId,
  classId,
  divisionId,
  onChange,
}: Props) {
  const [expandedGroups, setExpandedGroups] = useState<Set<string>>(() => new Set())
  const [expandedBranches, setExpandedBranches] = useState<Set<string>>(() => new Set())
  const [expandedClasses, setExpandedClasses] = useState<Set<string>>(() => new Set())

  const groupBuckets = useMemo(() => {
    const map = new Map<string, OrgTreeGroup>()
    for (const g of groups) map.set(g.id, g)
    const byGroup = new Map<string, OrgTreeBranch[]>()
    for (const b of branches) {
      const gid = getBranchGroupId(b) ?? UNGROUPED_KEY
      const list = byGroup.get(gid) ?? []
      list.push(b)
      byGroup.set(gid, list)
    }
    for (const [, list] of byGroup) sortByName(list)
    const orderedGroupIds: string[] = []
    const seen = new Set<string>()
    for (const g of sortByName(groups)) {
      if (byGroup.has(g.id)) {
        orderedGroupIds.push(g.id)
        seen.add(g.id)
      }
    }
    const extra = [...byGroup.keys()]
      .filter((k) => k !== UNGROUPED_KEY && !seen.has(k))
      .sort((a, b) => {
        const na = map.get(a)?.name ?? a
        const nb = map.get(b)?.name ?? b
        return String(na).localeCompare(String(nb), undefined, { sensitivity: 'base' })
      })
    orderedGroupIds.push(...extra)
    if (byGroup.has(UNGROUPED_KEY)) orderedGroupIds.push(UNGROUPED_KEY)
    return { map, byGroup, orderedGroupIds }
  }, [groups, branches])

  const syncExpandedFromSelection = useCallback(() => {
    if (groupScopeId.trim() && !branchId.trim()) {
      setExpandedGroups((prev) => new Set(prev).add(groupScopeId))
      return
    }
    if (!branchId) return
    const br = branches.find((x) => normalizeId(x.id) === normalizeId(branchId))
    if (!br) return
    const gid = getBranchGroupId(br) ?? UNGROUPED_KEY
    setExpandedGroups((prev) => new Set(prev).add(gid))
    setExpandedBranches((prev) => new Set(prev).add(branchId))
    if (classId.trim()) {
      setExpandedClasses((prev) => new Set(prev).add(classId))
    }
  }, [branchId, classId, branches, groupScopeId])

  useEffect(() => {
    syncExpandedFromSelection()
  }, [syncExpandedFromSelection])

  const selectedBranchNorm = useMemo(() => normalizeId(branchId), [branchId])
  const selectedClassNorm = useMemo(() => normalizeId(classId), [classId])

  function groupLabel(gid: string): string {
    if (gid === UNGROUPED_KEY) return 'Schools (no group)'
    return groupBuckets.map.get(gid)?.name ?? groupBuckets.map.get(gid)?.id ?? 'School group'
  }

  function classesForBranchId(brId: string) {
    const bn = normalizeId(brId)
    return sortByName(classes.filter((c) => normalizeId(getClassBranchId(c)) === bn))
  }

  function divisionsForClassId(clId: string) {
    const cn = normalizeId(clId)
    return sortByName(divisions.filter((d) => normalizeId(getDivisionClassId(d)) === cn))
  }

  const panelStyle: CSSProperties = {
    border: '1px solid #e2e8f0',
    borderRadius: 10,
    background: '#fff',
    padding: 12,
    maxHeight: 'min(72vh, 720px)',
    overflow: 'auto',
    boxShadow: '0 1px 2px rgba(15, 23, 42, 0.06)',
  }

  const showTitle = Boolean(title?.trim())

  return (
    <div style={panelStyle}>
      {showTitle ? <h3 style={sectionTitle}>{title}</h3> : null}
      <p style={flowHint}>Expand nodes to reach school, class, and division. Your selection applies across the app.</p>

      {groupBuckets.orderedGroupIds.length === 0 && branches.length === 0 ? (
        <p style={muted}>No schools loaded yet.</p>
      ) : (
        <div style={treeRoot}>
          {groupBuckets.orderedGroupIds.map((gid) => {
            const gList = groupBuckets.byGroup.get(gid)
            if (!gList || gList.length === 0) return null
            const groupOpen = expandedGroups.has(gid)
            const groupRowSelected =
              !branchId.trim() && groupScopeId.trim() && normalizeId(groupScopeId) === normalizeId(gid)
            return (
              <div key={gid} style={treeGroup}>
                <div style={rowDepth0}>
                  <button
                    type="button"
                    onClick={() => setExpandedGroups((s) => toggleSetMember(s, gid))}
                    style={groupChevronBtn}
                    aria-expanded={groupOpen}
                    aria-label={groupOpen ? 'Collapse group' : 'Expand group'}
                  >
                    <span style={chevron}>{groupOpen ? '▼' : '▶'}</span>
                  </button>
                  <button
                    type="button"
                    onClick={() =>
                      onChange({
                        groupScopeId: gid,
                        branchId: '',
                        classId: '',
                        divisionId: '',
                      })
                    }
                    style={{
                      ...groupSelectBtn,
                      ...(groupRowSelected ? groupSelectBtnActive : {}),
                    }}
                  >
                    {groupLabel(gid)}
                  </button>
                </div>
                {groupOpen &&
                  gList.map((br) => {
                    const branchOpen = expandedBranches.has(br.id)
                    const clsList = classesForBranchId(br.id)
                    const branchSelected = selectedBranchNorm && normalizeId(br.id) === selectedBranchNorm
                    return (
                      <div key={br.id} style={nestBranch}>
                        <div style={rowDepth1}>
                          <button
                            type="button"
                            onClick={() => setExpandedBranches((s) => toggleSetMember(s, br.id))}
                            style={chevOnlyBtn}
                            aria-expanded={branchOpen}
                            aria-label={branchOpen ? 'Collapse' : 'Expand'}
                          >
                            <span style={chevronSm}>{branchOpen ? '▼' : '▶'}</span>
                          </button>
                          <button
                            type="button"
                            onClick={() =>
                              onChange({
                                groupScopeId: '',
                                branchId: br.id,
                                classId: '',
                                divisionId: '',
                              })
                            }
                            style={{
                              ...branchNameBtn,
                              ...(branchSelected ? branchNameActive : {}),
                            }}
                          >
                            {br.name ?? br.id}
                          </button>
                        </div>
                        {branchOpen &&
                          clsList.map((cl) => {
                            const classOpen = expandedClasses.has(cl.id)
                            const divList = divisionsForClassId(cl.id)
                            const classSelected =
                              branchSelected && selectedClassNorm && normalizeId(cl.id) === selectedClassNorm
                            return (
                              <div key={cl.id} style={nestClass}>
                                <div style={rowDepth2}>
                                  <button
                                    type="button"
                                    onClick={() => setExpandedClasses((s) => toggleSetMember(s, cl.id))}
                                    style={chevOnlyBtn}
                                    aria-expanded={classOpen}
                                    aria-label={classOpen ? 'Collapse' : 'Expand'}
                                  >
                                    <span style={chevronSm}>{classOpen ? '▼' : '▶'}</span>
                                  </button>
                                  <button
                                    type="button"
                                    onClick={() =>
                                      onChange({
                                        groupScopeId: '',
                                        branchId: br.id,
                                        classId: cl.id,
                                        divisionId: '',
                                      })
                                    }
                                    style={{
                                      ...classNameBtn,
                                      ...(classSelected ? classNameActive : {}),
                                    }}
                                  >
                                    {cl.name ?? cl.id}
                                  </button>
                                </div>
                                {classOpen &&
                                  (divList.length === 0 ? (
                                    <p style={mutedLeaf}>No divisions</p>
                                  ) : (
                                    <div style={nestDivision}>
                                      {divList.map((dv) => {
                                        const divSelected =
                                          classSelected &&
                                          divisionId &&
                                          normalizeId(dv.id) === normalizeId(divisionId)
                                        return (
                                          <button
                                            key={dv.id}
                                            type="button"
                                            onClick={() =>
                                              onChange({
                                                groupScopeId: '',
                                                branchId: br.id,
                                                classId: cl.id,
                                                divisionId: dv.id,
                                              })
                                            }
                                            style={{
                                              ...divisionLeafBtn,
                                              ...(divSelected ? divisionLeafActive : {}),
                                            }}
                                          >
                                            {dv.name ?? dv.id}
                                          </button>
                                        )
                                      })}
                                    </div>
                                  ))}
                              </div>
                            )
                          })}
                        {branchOpen && clsList.length === 0 && (
                          <p style={mutedLeaf}>No classes for this school</p>
                        )}
                      </div>
                    )
                  })}
              </div>
            )
          })}
        </div>
      )}
    </div>
  )
}

const sectionTitle: CSSProperties = {
  margin: '0 0 6px',
  fontSize: 14,
  fontWeight: 700,
  color: '#0f172a',
}

const flowHint: CSSProperties = {
  margin: '0 0 12px',
  fontSize: 11,
  color: '#64748b',
  lineHeight: 1.45,
}

const muted: CSSProperties = { margin: 0, fontSize: 12, color: '#94a3b8' }
const mutedLeaf: CSSProperties = {
  margin: '4px 0 4px 28px',
  fontSize: 12,
  color: '#94a3b8',
  fontStyle: 'italic',
}

const treeRoot: CSSProperties = { display: 'flex', flexDirection: 'column', gap: 2 }
const treeGroup: CSSProperties = { marginBottom: 4 }

const rowDepth0: CSSProperties = { display: 'flex', alignItems: 'stretch' }
const rowDepth1: CSSProperties = {
  display: 'flex',
  alignItems: 'stretch',
  marginLeft: 4,
  marginTop: 2,
}
const rowDepth2: CSSProperties = {
  display: 'flex',
  alignItems: 'stretch',
  marginLeft: 20,
  marginTop: 2,
}

const nestBranch: CSSProperties = { marginLeft: 6, borderLeft: '2px solid #e2e8f0', paddingLeft: 6 }
const nestClass: CSSProperties = { marginLeft: 0 }
const nestDivision: CSSProperties = {
  marginLeft: 44,
  marginTop: 2,
  display: 'flex',
  flexDirection: 'column',
  gap: 3,
}

const chevron: CSSProperties = { width: 18, fontSize: 10, color: '#64748b', flexShrink: 0 }
const chevronSm: CSSProperties = { fontSize: 9, color: '#64748b', lineHeight: 1 }

const groupChevronBtn: CSSProperties = {
  width: 36,
  minWidth: 36,
  padding: '8px 0',
  border: '1px solid #e2e8f0',
  borderRadius: 8,
  background: '#f8fafc',
  cursor: 'pointer',
  display: 'flex',
  alignItems: 'center',
  justifyContent: 'center',
  flexShrink: 0,
}

const groupSelectBtn: CSSProperties = {
  flex: 1,
  marginLeft: 6,
  display: 'flex',
  alignItems: 'center',
  padding: '8px 10px',
  border: '1px solid #e2e8f0',
  borderRadius: 8,
  background: '#f8fafc',
  cursor: 'pointer',
  fontSize: 13,
  fontWeight: 600,
  color: '#1e293b',
  textAlign: 'left',
}

const groupSelectBtnActive: CSSProperties = {
  background: '#e0e7ff',
  borderColor: '#818cf8',
  color: '#1e1b4b',
}

const chevOnlyBtn: CSSProperties = {
  width: 28,
  minWidth: 28,
  padding: '6px 0',
  border: '1px solid transparent',
  borderRadius: 6,
  background: '#f1f5f9',
  cursor: 'pointer',
  display: 'flex',
  alignItems: 'center',
  justifyContent: 'center',
  flexShrink: 0,
}

const branchNameBtn: CSSProperties = {
  flex: 1,
  marginLeft: 4,
  padding: '6px 10px',
  border: 'none',
  borderRadius: 6,
  background: 'transparent',
  cursor: 'pointer',
  fontSize: 13,
  color: '#1e3a5f',
  textAlign: 'left',
}

const branchNameActive: CSSProperties = {
  background: '#e0e7ff',
  fontWeight: 700,
}

const classNameBtn: CSSProperties = {
  flex: 1,
  marginLeft: 4,
  padding: '5px 10px',
  border: '1px solid #e2e8f0',
  borderRadius: 6,
  background: '#fff',
  cursor: 'pointer',
  fontSize: 12,
  color: '#334155',
  textAlign: 'left',
}

const classNameActive: CSSProperties = {
  background: '#1e3a5f',
  color: '#fff',
  borderColor: '#1e3a5f',
  fontWeight: 700,
}

const divisionLeafBtn: CSSProperties = {
  padding: '6px 10px',
  border: '1px solid #dbeafe',
  borderRadius: 6,
  background: '#fff',
  cursor: 'pointer',
  fontSize: 12,
  color: '#0f172a',
  textAlign: 'left',
}

const divisionLeafActive: CSSProperties = {
  background: '#dcfce7',
  borderColor: '#86efac',
  fontWeight: 700,
}
