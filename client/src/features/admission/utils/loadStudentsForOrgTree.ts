import type { Branch } from '../../branches'
import { ORG_TREE_UNGROUPED_KEY } from '../../../shared/components/OrganizationHierarchyTree'
import { studentsApi } from '../api/students.api'
import type { Student } from '../students.types'

function norm(s: string | undefined | null): string {
  return String(s ?? '')
    .trim()
    .toLowerCase()
}

export type OrgTreeScope = {
  groupScopeId: string
  branchId: string
  classId: string
  divisionId: string
}

/** New admissions / add-student: only when a school (branch) row is selected, not group-only or class/division drill-down. */
export function canStartNewAdmission(scope: Pick<OrgTreeScope, 'branchId' | 'classId' | 'divisionId'>): boolean {
  return !!scope.branchId?.trim() && !scope.classId?.trim() && !scope.divisionId?.trim()
}

/** Same bucket key as the organization tree (group id or {@link ORG_TREE_UNGROUPED_KEY}). */
export function branchBucketKey(b: Branch): string {
  const raw = b.groupId ?? (b as { GroupId?: string | null }).GroupId
  if (raw != null && String(raw).trim() !== '') return String(raw)
  return ORG_TREE_UNGROUPED_KEY
}

/** All branches in the tree bucket selected by `groupScopeId`. */
export function branchesInGroupScope(groupScopeId: string, branches: Branch[]): Branch[] {
  if (!groupScopeId.trim()) return []
  const want = norm(groupScopeId)
  return branches.filter((b) => norm(branchBucketKey(b)) === want)
}

export async function loadStudentsForGroupScopeKey(groupScopeId: string, branches: Branch[]): Promise<Student[]> {
  const inGroup = branchesInGroupScope(groupScopeId, branches)
  if (inGroup.length === 0) return []
  const lists = await Promise.all(inGroup.map((b) => studentsApi.getByBranch(b.id)))
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

/** Students for current tree: school group bucket, or branch/class/division. */
export async function loadStudentsForOrgScope(scope: OrgTreeScope, branches: Branch[]): Promise<Student[]> {
  if (scope.branchId.trim()) {
    if (scope.divisionId.trim()) {
      return studentsApi.search({
        branchId: scope.branchId.trim(),
        classId: scope.classId.trim() || undefined,
        divisionId: scope.divisionId.trim(),
      })
    }
    if (scope.classId.trim()) {
      return studentsApi.search({
        branchId: scope.branchId.trim(),
        classId: scope.classId.trim(),
      })
    }
    return studentsApi.search({ branchId: scope.branchId.trim() })
  }
  if (scope.groupScopeId.trim()) {
    return loadStudentsForGroupScopeKey(scope.groupScopeId, branches)
  }
  return []
}

/** Students matching the hierarchy when a school (branch) is selected. */
export async function loadStudentsForOrgTree(
  scope: Pick<OrgTreeScope, 'branchId' | 'classId' | 'divisionId'>,
): Promise<Student[]> {
  const { branchId, classId, divisionId } = scope
  if (!branchId.trim()) return []
  if (divisionId.trim()) {
    return studentsApi.search({
      branchId: branchId.trim(),
      classId: classId.trim() || undefined,
      divisionId: divisionId.trim(),
    })
  }
  if (classId.trim()) {
    return studentsApi.search({
      branchId: branchId.trim(),
      classId: classId.trim(),
    })
  }
  return studentsApi.search({ branchId: branchId.trim() })
}

/** All students in every branch in the same management group as `branchId`. */
export async function loadStudentsForBranchGroup(branchId: string, branches: Branch[]): Promise<Student[]> {
  if (!branchId.trim()) return []
  const br = branches.find((b) => norm(b.id) === norm(branchId))
  const key = br ? branchBucketKey(br) : ''
  if (!key) return loadStudentsForOrgTree({ branchId, classId: '', divisionId: '' })
  return loadStudentsForGroupScopeKey(key, branches)
}