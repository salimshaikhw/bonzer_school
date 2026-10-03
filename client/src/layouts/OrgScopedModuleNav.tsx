import type { CSSProperties } from 'react'
import { NavLink } from 'react-router-dom'
import type { AppModule } from '../config/appModules'
import { ORG_TREE_UNGROUPED_KEY } from '../shared/components/OrganizationHierarchyTree'
import type { OrgHierarchyContextValue } from './OrgHierarchyProvider'

type Props = {
  org: OrgHierarchyContextValue
  activeModule: AppModule | null
  disabledTos?: readonly string[]
  /** Primary + module tabs visible (tree node picked, or admin). */
  showModules: boolean
  modules: AppModule[]
}

function isDisabledTo(disabledTos: readonly string[] | undefined, to: string) {
  return disabledTos?.includes(to) ?? false
}

export function hasOrgTreeSelection(org: OrgHierarchyContextValue): boolean {
  return !!(
    org.groupScopeId.trim() ||
    org.branchId.trim() ||
    org.classId.trim() ||
    org.divisionId.trim()
  )
}

/** Human-readable path for the current tree selection (broad → narrow). */
export function formatOrgSelectionSummary(org: OrgHierarchyContextValue): string {
  const bits: string[] = []
  const gid = org.groupScopeId.trim()
  const bid = org.branchId.trim()

  if (gid && !bid) {
    if (gid === ORG_TREE_UNGROUPED_KEY) {
      bits.push('Schools (no group)')
    } else {
      const g = org.schoolGroups.find((x) => x.id === gid)
      bits.push(g?.name ?? g?.code ?? 'Group')
    }
  }
  if (bid) {
    const b = org.branches.find((x) => x.id === bid)
    bits.push(b?.name ?? 'School')
  }
  if (org.classId.trim()) {
    const c = org.treeClasses.find((x) => x.id === org.classId) ?? org.classes.find((x) => x.id === org.classId)
    bits.push(c?.name ?? 'Class')
  }
  if (org.divisionId.trim()) {
    const d =
      org.treeDivisions.find((x) => x.id === org.divisionId) ?? org.divisions.find((x) => x.id === org.divisionId)
    bits.push(d?.name ?? 'Division')
  }
  return bits.join(' → ')
}

export function OrgScopedModuleNav({ org, activeModule, disabledTos, showModules, modules }: Props) {
  if (!showModules) {
    return (
      <div style={styles.hintBar} role="status">
        <span style={styles.hintText}>
          Select a <strong>group</strong>, <strong>school</strong>, <strong>class</strong>, or <strong>division</strong>{' '}
          in the school tree to open modules. Your choice sets the scope for admission, attendance, finance, and more.
        </span>
      </div>
    )
  }

  const summary = formatOrgSelectionSummary(org)

  return (
    <div style={styles.wrap}>
      {summary ? (
        <div style={styles.scopeRow} title="Current scope from the school tree">
          <span style={styles.scopeLabel}>Scope</span>
          <span style={styles.scopeValue}>{summary}</span>
        </div>
      ) : null}
      <nav style={styles.moduleTrack} aria-label="Modules">
        {modules.map((m) => (
          <NavLink
            key={m.id}
            to={m.basePath}
            end={m.basePath === '/'}
            style={({ isActive }) => ({
              ...styles.moduleTab,
              ...(isActive ? styles.moduleTabActive : {}),
            })}
          >
            <span title={m.label}>{m.label}</span>
          </NavLink>
        ))}
      </nav>
      {activeModule && activeModule.children.length > 0 ? (
        <nav style={styles.subTrack} aria-label={`${activeModule.label} pages`}>
          {activeModule.children.map((c) => {
            const off = isDisabledTo(disabledTos, c.to)
            if (off) {
              return (
                <span
                  key={c.to}
                  style={{ ...styles.subTab, ...styles.subTabDisabled }}
                  title="Not available when a division is selected in the school tree."
                >
                  {c.label}
                </span>
              )
            }
            return (
              <NavLink
                key={c.to}
                to={c.to}
                end={c.end}
                style={({ isActive }) => ({
                  ...styles.subTab,
                  ...(isActive ? styles.subTabActive : {}),
                })}
              >
                {c.label}
              </NavLink>
            )
          })}
        </nav>
      ) : null}
    </div>
  )
}

const styles: Record<string, CSSProperties> = {
  hintBar: {
    marginBottom: 18,
    padding: '12px 16px',
    background: '#fffbeb',
    border: '1px solid #fcd34d',
    borderRadius: 10,
    fontSize: 13,
    color: '#78350f',
    lineHeight: 1.5,
  },
  hintText: {},
  wrap: {
    marginBottom: 20,
    padding: '12px 16px 14px',
    background: '#fff',
    borderRadius: 12,
    border: '1px solid #e2e8f0',
    boxShadow: '0 1px 3px rgba(15, 23, 42, 0.06)',
  },
  scopeRow: {
    display: 'flex',
    flexWrap: 'wrap',
    alignItems: 'baseline',
    gap: 8,
    marginBottom: 12,
    paddingBottom: 10,
    borderBottom: '1px solid #f1f5f9',
  },
  scopeLabel: {
    fontSize: 10,
    fontWeight: 800,
    letterSpacing: '0.08em',
    textTransform: 'uppercase',
    color: '#94a3b8',
  },
  scopeValue: {
    fontSize: 13,
    fontWeight: 600,
    color: '#0f172a',
    flex: 1,
    minWidth: 0,
  },
  moduleTrack: {
    display: 'flex',
    flexWrap: 'wrap',
    gap: 6,
    marginBottom: 8,
  },
  moduleTab: {
    padding: '8px 14px',
    fontSize: 13,
    fontWeight: 600,
    color: '#64748b',
    textDecoration: 'none',
    borderRadius: 8,
    border: '1px solid transparent',
    background: '#f8fafc',
  },
  moduleTabActive: {
    color: '#1e40af',
    background: '#eff6ff',
    borderColor: '#bfdbfe',
  },
  subTrack: {
    display: 'flex',
    flexWrap: 'wrap',
    gap: 4,
    paddingTop: 4,
  },
  subTab: {
    padding: '6px 12px',
    fontSize: 12,
    fontWeight: 600,
    color: '#64748b',
    textDecoration: 'none',
    borderRadius: 6,
    border: '1px solid transparent',
  },
  subTabActive: {
    color: '#0f172a',
    background: '#f1f5f9',
    borderColor: '#e2e8f0',
  },
  subTabDisabled: {
    opacity: 0.45,
    cursor: 'not-allowed',
    pointerEvents: 'none',
  },
}
