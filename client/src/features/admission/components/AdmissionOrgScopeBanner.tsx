import type { CSSProperties } from 'react'
import { ORG_TREE_UNGROUPED_KEY } from '../../../shared/components/OrganizationHierarchyTree'
import { useOrgHierarchy } from '../../../layouts/OrgHierarchyProvider'
import { branchesInGroupScope } from '../utils/loadStudentsForOrgTree'

function norm(s: string | undefined | null): string {
  return String(s ?? '')
    .trim()
    .toLowerCase()
}

/**
 * Common ERP scope line: reflects the organization tree (group → school → class → division).
 * Selection is read-only here — users change scope in the left hierarchy panel.
 */
export function AdmissionOrgScopeBanner() {
  const org = useOrgHierarchy()

  const groupOnly = org.groupScopeId.trim() && !org.branchId.trim()
  if (groupOnly) {
    const inBucket = branchesInGroupScope(org.groupScopeId, org.branches)
    const label =
      org.groupScopeId === ORG_TREE_UNGROUPED_KEY
        ? 'Schools (no group)'
        : org.schoolGroups.find((g) => norm(g.id) === norm(org.groupScopeId))?.name ?? 'School group'
    return (
      <div style={styles.box}>
        <strong style={styles.k}>Current scope (from organization tree)</strong>
        <p style={styles.line}>
          {label} — <strong>all {inBucket.length} school(s)</strong> in this group
        </p>
        <p style={styles.hint}>Dashboard and analytics include every school listed under this group. Pick a school to enroll pupils or narrow the list.</p>
      </div>
    )
  }

  const br = org.branches.find((b) => norm(b.id) === norm(org.branchId))
  const groupId = br?.groupId != null && String(br.groupId).trim() ? String(br.groupId) : ''
  const grp = groupId ? org.schoolGroups.find((g) => norm(g.id) === norm(groupId)) : undefined

  const cl =
    org.treeClasses.find((c) => norm(c.id) === norm(org.classId)) ??
    org.classes.find((c) => norm(c.id) === norm(org.classId))
  const dv =
    org.treeDivisions.find((d) => norm(d.id) === norm(org.divisionId)) ??
    org.divisions.find((d) => norm(d.id) === norm(org.divisionId))

  const parts: string[] = []
  if (grp) parts.push(grp.name ?? 'School group')
  else if (groupId) parts.push('School group')
  if (br) parts.push(br.name ?? 'School')
  if (cl) parts.push(`Class: ${cl.name ?? cl.id}`)
  if (dv) parts.push(`Division: ${dv.name ?? dv.id}`)

  if (parts.length === 0) {
    return (
      <div style={styles.box}>
        <strong style={styles.k}>Current scope</strong>
        <p style={styles.muted}>Click a school group or a school in the organization tree on the left.</p>
      </div>
    )
  }

  return (
    <div style={styles.box}>
      <strong style={styles.k}>Current scope (from organization tree)</strong>
      <p style={styles.line}>{parts.join(' · ')}</p>
      {!org.classId.trim() && org.branchId.trim() ? (
        <p style={styles.hint}>Choose class/division in the tree to narrow students and analytics.</p>
      ) : null}
    </div>
  )
}

const styles: Record<string, CSSProperties> = {
  box: {
    marginBottom: 20,
    padding: '14px 16px',
    borderRadius: 10,
    border: '1px solid #c7d2fe',
    background: 'linear-gradient(135deg, #eef2ff 0%, #f8fafc 100%)',
  },
  k: { display: 'block', fontSize: 11, fontWeight: 700, color: '#3730a3', textTransform: 'uppercase', letterSpacing: '0.06em' },
  line: { margin: '8px 0 0', fontSize: 15, fontWeight: 600, color: '#0f172a' },
  muted: { margin: '8px 0 0', fontSize: 14, color: '#64748b' },
  hint: { margin: '8px 0 0', fontSize: 12, color: '#64748b', lineHeight: 1.4 },
}
