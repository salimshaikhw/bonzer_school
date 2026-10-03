import type { CSSProperties } from 'react'
import { useNavigate } from 'react-router-dom'
import type { AppModule } from '../config/appModules'
import { ROUTES } from '../config/routes'
import { Button, OrganizationHierarchyTree, Spinner } from '../shared'
import type { OrgHierarchyContextValue } from './OrgHierarchyProvider'

type Props = {
  org: OrgHierarchyContextValue
  activeModule: AppModule | null
  /** Desktop: constrain height so the rail scrolls; mobile: stack and let the page scroll. */
  layoutWide?: boolean
}

/** Left rail: school tree only. Module tabs render in the main column after a tree selection. */
export function UnifiedSidebar({ org, activeModule, layoutWide = true }: Props) {
  const navigate = useNavigate()

  const orgTree = !org.ready && org.loading ? (
    <div style={styles.orgLoading}>
      <Spinner />
      <span style={styles.muted}>Loading…</span>
    </div>
  ) : (
    <>
      {org.error ? <p style={styles.err}>{org.error}</p> : null}
      {org.loadWarnings.length > 0 ? <p style={styles.warn}>{org.loadWarnings.join(' ')}</p> : null}
      <div style={styles.orgToolbar}>
        <Button
          type="button"
          variant="secondary"
          fullWidth
          disabled={org.loading || org.hierarchyRefreshing}
          onClick={() => {
            void org.refreshHierarchy()
          }}
          style={styles.refreshBtn}
        >
          {org.hierarchyRefreshing ? 'Refreshing…' : 'Refresh schools'}
        </Button>
      </div>
      {org.branches.length > 0 ? (
        <OrganizationHierarchyTree
          title=""
          groups={org.schoolGroups}
          branches={org.branches}
          classes={org.treeClasses}
          divisions={org.treeDivisions}
          groupScopeId={org.groupScopeId}
          branchId={org.branchId}
          classId={org.classId}
          divisionId={org.divisionId}
          onChange={(next) => {
            org.setOrgSelection(next)
            if (next.divisionId && activeModule?.id === 'attendance') {
              navigate(ROUTES.ATTENDANCE_DIVISION)
            }
          }}
        />
      ) : (
        <p style={styles.muted}>No schools loaded. Sign in and ensure the API is running.</p>
      )}
    </>
  )

  return (
    <aside
      style={{
        ...styles.aside,
        ...(layoutWide ? styles.asideDesktop : styles.asideMobile),
      }}
      aria-label="School organization"
    >
      <div style={styles.treeHeader}>
        <h2 style={styles.treeTitle}>School tree</h2>
        <p style={styles.treeSubtitle}>Select a node to unlock modules and scoped data.</p>
      </div>

      <div style={styles.orgBlock}>{orgTree}</div>
    </aside>
  )
}

const styles: Record<string, CSSProperties> = {
  aside: {
    flexShrink: 0,
    width: 280,
    maxWidth: '100%',
    boxSizing: 'border-box',
    background: '#fff',
    borderRight: '1px solid #e2e8f0',
    display: 'flex',
    flexDirection: 'column',
    minHeight: 0,
    overflow: 'hidden',
  },
  asideDesktop: {
    maxHeight: 'calc(100vh - 52px)',
  },
  asideMobile: {
    maxHeight: 'none',
    borderRight: 'none',
    borderBottom: '1px solid #e2e8f0',
    width: '100%',
  },
  treeHeader: {
    flexShrink: 0,
    padding: '14px 14px 10px',
    borderBottom: '1px solid #f1f5f9',
  },
  treeTitle: {
    margin: 0,
    fontSize: 13,
    fontWeight: 800,
    letterSpacing: '0.06em',
    textTransform: 'uppercase',
    color: '#0f172a',
  },
  treeSubtitle: {
    margin: '6px 0 0',
    fontSize: 11,
    fontWeight: 500,
    color: '#94a3b8',
    lineHeight: 1.4,
  },
  orgBlock: {
    flex: 1,
    minHeight: 0,
    overflowY: 'auto',
    overflowX: 'hidden',
    padding: '0 10px 16px',
  },
  orgToolbar: {
    padding: '0 12px 10px',
    flexShrink: 0,
  },
  refreshBtn: {
    padding: '8px 12px',
    fontSize: 13,
  },
  orgLoading: {
    display: 'flex',
    alignItems: 'center',
    gap: 10,
    padding: '12px 16px',
    fontSize: 13,
    color: '#64748b',
  },
  err: { color: '#b91c1c', fontSize: 12, margin: '0 12px 8px', lineHeight: 1.4 },
  warn: { color: '#a16207', fontSize: 11, margin: '0 12px 8px', lineHeight: 1.4 },
  muted: { color: '#94a3b8', fontSize: 12, margin: '0 12px 12px', lineHeight: 1.45 },
}
