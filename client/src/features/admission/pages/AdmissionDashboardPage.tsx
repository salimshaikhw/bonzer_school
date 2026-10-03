import type { CSSProperties } from 'react'
import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { ROUTES } from '../../../config/routes'
import { Button, Spinner } from '../../../shared'
import { useOrgHierarchy } from '../../../layouts/OrgHierarchyProvider'
import type { Student } from '../students.types'
import { AdmissionOrgScopeBanner } from '../components/AdmissionOrgScopeBanner'
import {
  branchesInGroupScope,
  canStartNewAdmission,
  loadStudentsForBranchGroup,
  loadStudentsForGroupScopeKey,
  loadStudentsForOrgTree,
} from '../utils/loadStudentsForOrgTree'

export function AdmissionDashboardPage() {
  const org = useOrgHierarchy()
  const [scopeRows, setScopeRows] = useState<Student[]>([])
  const [groupRows, setGroupRows] = useState<Student[]>([])
  const [busy, setBusy] = useState(false)

  const scopeKey = `${org.groupScopeId}|${org.branchId}|${org.classId}|${org.divisionId}`
  const groupOnly = org.groupScopeId.trim() && !org.branchId.trim()

  useEffect(() => {
    if (!org.ready || org.loading) {
      setScopeRows([])
      setGroupRows([])
      return
    }
    if (!org.branchId.trim() && !org.groupScopeId.trim()) {
      setScopeRows([])
      setGroupRows([])
      return
    }

    setBusy(true)
    void (async () => {
      try {
        if (groupOnly) {
          const merged = await loadStudentsForGroupScopeKey(org.groupScopeId, org.branches)
          setScopeRows(merged)
          setGroupRows(merged)
        } else {
          const [scoped, group] = await Promise.all([
            loadStudentsForOrgTree({
              branchId: org.branchId,
              classId: org.classId,
              divisionId: org.divisionId,
            }),
            loadStudentsForBranchGroup(org.branchId, org.branches),
          ])
          setScopeRows(scoped)
          setGroupRows(group)
        }
      } catch {
        setScopeRows([])
        setGroupRows([])
      } finally {
        setBusy(false)
      }
    })()
  }, [scopeKey, org.ready, org.loading, org.groupScopeId, org.branchId, org.classId, org.divisionId, org.branches, groupOnly])

  const scopeStats = useMemo(() => {
    const total = scopeRows.length
    const active = scopeRows.filter((r) => String(r.studentStatus ?? '').toUpperCase() === 'ACTIVE').length
    const draft = scopeRows.filter((r) => String(r.studentStatus ?? '').toUpperCase() === 'DRAFT').length
    const pendingDivision = scopeRows.filter((r) => {
      const hasClass = !!(r.classId && String(r.classId).trim())
      const hasDiv = !!(r.divisionId && String(r.divisionId).trim())
      return hasClass && !hasDiv
    }).length
    return { total, active, draft, pendingDivision }
  }, [scopeRows])

  const groupStats = useMemo(() => {
    const total = groupRows.length
    if (groupOnly) {
      const schoolsInGroup = branchesInGroupScope(org.groupScopeId, org.branches).length
      return { total, schoolsInGroup, hasGroup: true, isGroupOnly: true as const }
    }
    const br = org.branches.find((b) => b.id === org.branchId)
    const gid = br?.groupId != null && String(br.groupId).trim() ? String(br.groupId) : ''
    const schoolsInGroup = gid ? org.branches.filter((b) => String(b.groupId ?? '') === gid).length : 1
    return { total, schoolsInGroup, hasGroup: !!gid, isGroupOnly: false as const }
  }, [groupRows, groupOnly, org.branchId, org.groupScopeId, org.branches])

  const canEnroll = canStartNewAdmission(org)

  const hasScope = !!(org.branchId.trim() || org.groupScopeId.trim())

  return (
    <div style={styles.page}>
      <AdmissionOrgScopeBanner />

      <header style={styles.header}>
        <div>
          <h2 style={styles.h2}>Overview</h2>
          <p style={styles.lead}>
            {groupOnly
              ? 'Enrollment snapshot for every school in the selected group.'
              : 'Key enrollment figures for the current tree selection and for the whole school group.'}
          </p>
        </div>
        <div style={styles.headerActions}>
          <Link to={ROUTES.ADMISSION_ANALYTICS}>
            <Button type="button" variant="secondary">
              Analytics
            </Button>
          </Link>
          {canEnroll ? (
            <Link to={ROUTES.STUDENTS_NEW}>
              <Button type="button">New admission</Button>
            </Link>
          ) : (
            <Button
              type="button"
              disabled
              title="Select a school row only (not a group, class, or division) to add a student"
            >
              New admission
            </Button>
          )}
        </div>
      </header>

      {!hasScope ? (
        <p style={styles.muted}>Choose a school group or a school in the organization tree to load admission metrics.</p>
      ) : busy ? (
        <p style={styles.muted}>
          <Spinner /> Loading…
        </p>
      ) : (
        <>
          <section style={styles.card}>
            <h3 style={styles.sectionTitle}>{groupOnly ? 'School group (selected)' : 'This scope (tree)'}</h3>
            {!groupOnly && <p style={styles.cardHint}>Respects class and division if selected in the hierarchy.</p>}
            <div style={styles.kpiGrid}>
              <div style={styles.kpi}>
                <div style={styles.kpiValue}>{scopeStats.total}</div>
                <div style={styles.kpiLabel}>Students</div>
              </div>
              <div style={styles.kpi}>
                <div style={styles.kpiValue}>{scopeStats.active}</div>
                <div style={styles.kpiLabel}>Active</div>
              </div>
              <div style={styles.kpi}>
                <div style={styles.kpiValue}>{scopeStats.draft}</div>
                <div style={styles.kpiLabel}>Drafts</div>
              </div>
              <div style={styles.kpi}>
                <div style={styles.kpiValue}>{scopeStats.pendingDivision}</div>
                <div style={styles.kpiLabel}>Class set, division pending</div>
              </div>
            </div>
          </section>

          {!groupOnly && (
            <section style={styles.card}>
              <h3 style={styles.sectionTitle}>School group (aggregated)</h3>
              <p style={styles.cardHint}>
                {groupStats.hasGroup
                  ? `All ${groupStats.schoolsInGroup} school(s) in the same group as the current selection.`
                  : 'Ungrouped school — totals match this school only.'}
              </p>
              <div style={styles.kpiGrid}>
                <div style={styles.kpi}>
                  <div style={styles.kpiValue}>{groupStats.total}</div>
                  <div style={styles.kpiLabel}>Students in group</div>
                </div>
              </div>
            </section>
          )}
        </>
      )}
    </div>
  )
}

const styles: Record<string, CSSProperties> = {
  page: { maxWidth: 1100, margin: '0 auto' },
  header: {
    display: 'flex',
    flexWrap: 'wrap',
    alignItems: 'flex-start',
    justifyContent: 'space-between',
    gap: 16,
    marginBottom: 20,
  },
  h2: { margin: 0, fontSize: 18, fontWeight: 700, color: '#0f172a' },
  lead: { margin: '8px 0 0', fontSize: 14, color: '#64748b', maxWidth: 560 },
  headerActions: { display: 'flex', flexWrap: 'wrap', gap: 10, alignItems: 'center' },
  card: {
    background: '#fff',
    borderRadius: 12,
    border: '1px solid #e2e8f0',
    padding: 20,
    marginBottom: 16,
    boxShadow: '0 1px 3px rgba(15, 23, 42, 0.06)',
  },
  sectionTitle: { margin: '0 0 6px', fontSize: 15, fontWeight: 700, color: '#1e3a5f' },
  cardHint: { margin: '0 0 14px', fontSize: 13, color: '#64748b' },
  kpiGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(160px, 1fr))',
    gap: 14,
  },
  kpi: {
    background: 'linear-gradient(180deg, #f8fafc 0%, #fff 100%)',
    border: '1px solid #e2e8f0',
    borderRadius: 10,
    padding: 16,
  },
  kpiValue: { fontSize: 26, fontWeight: 800, color: '#1e3a5f' },
  kpiLabel: { fontSize: 12, fontWeight: 600, color: '#64748b', marginTop: 6, lineHeight: 1.35 },
  muted: { color: '#64748b', fontSize: 14 },
}
