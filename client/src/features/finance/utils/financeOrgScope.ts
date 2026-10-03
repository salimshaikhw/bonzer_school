import type { OrgHierarchyContextValue } from '../../../layouts/OrgHierarchyProvider'
import { ORG_TREE_UNGROUPED_KEY } from '../../../shared/components/OrganizationHierarchyTree'
import { branchesInGroupScope } from '../../admission/utils/loadStudentsForOrgTree'

/** Deepest level selected in the org tree — mirrors admission analytics. */
export type FinanceScopeLevel = 'group' | 'school' | 'class' | 'division' | null

function norm(s: string | undefined | null): string {
  return String(s ?? '')
    .trim()
    .toLowerCase()
}

export function getFinanceScopeLevel(
  org: Pick<OrgHierarchyContextValue, 'branchId' | 'groupScopeId' | 'classId' | 'divisionId'>,
): FinanceScopeLevel {
  if (!org.branchId.trim() && !org.groupScopeId.trim()) return null
  if (org.divisionId.trim()) return 'division'
  if (org.classId.trim()) return 'class'
  if (org.branchId.trim()) return 'school'
  if (org.groupScopeId.trim()) return 'group'
  return null
}

export function getFinanceEffectiveBranchIds(
  org: Pick<OrgHierarchyContextValue, 'branchId' | 'groupScopeId' | 'branches'>,
): string[] {
  if (org.branchId.trim()) return [org.branchId.trim()]
  if (org.groupScopeId.trim()) {
    return branchesInGroupScope(org.groupScopeId, org.branches).map((b) => b.id)
  }
  return []
}

export function financeScopeTitle(
  level: FinanceScopeLevel,
  org: OrgHierarchyContextValue,
  effectiveBranchCount: number,
): string | null {
  if (!level) return null
  if (level === 'group') {
    if (norm(org.groupScopeId) === norm(ORG_TREE_UNGROUPED_KEY)) {
      return `School group — Ungrouped schools (${effectiveBranchCount} school${effectiveBranchCount === 1 ? '' : 's'})`
    }
    const g = org.schoolGroups.find((x) => norm(x.id) === norm(org.groupScopeId))
    const n = g?.name?.trim()
    const suffix =
      effectiveBranchCount > 0
        ? ` · ${effectiveBranchCount} school${effectiveBranchCount === 1 ? '' : 's'}`
        : ''
    return `School group — ${n || 'Selected group'}${suffix}`
  }
  if (level === 'school') {
    const name = org.branches.find((b) => norm(b.id) === norm(org.branchId))?.name ?? org.branchId
    return `School-wide — ${name || 'School'}`
  }
  if (level === 'class') {
    const c =
      org.treeClasses.find((x) => norm(x.id) === norm(org.classId)) ??
      org.classes.find((x) => norm(x.id) === norm(org.classId))
    return `Class — ${c?.name ?? org.classId}`
  }
  const d =
    org.treeDivisions.find((x) => norm(x.id) === norm(org.divisionId)) ??
    org.divisions.find((x) => norm(x.id) === norm(org.divisionId))
  return `Division — ${d?.name ?? org.divisionId}`
}

export function financeModuleScopeHint(
  org: Pick<OrgHierarchyContextValue, 'branchId' | 'groupScopeId' | 'classId' | 'divisionId'>,
): string {
  if (!org.groupScopeId.trim() && !org.branchId.trim()) {
    return 'Select a school group or school in the tree on the left — Finance lists and tools use that scope.'
  }
  if (org.divisionId.trim()) {
    return 'Division selected — tables show this division only. Move up the tree (class or school) to widen.'
  }
  if (org.classId.trim()) {
    return 'Class selected — drill-down views focus on this class. Choose only the school row to see all classes.'
  }
  if (org.branchId.trim()) {
    return 'School selected — open class or division under this school to narrow students, invoices, and balances.'
  }
  return 'Group selected — see rolled-up fees by branch; pick a school to record payments and branch dashboards.'
}
