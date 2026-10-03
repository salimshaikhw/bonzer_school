import type { CSSProperties } from 'react'
import { NavLink, Outlet } from 'react-router-dom'
import { ROUTES } from '../../../config/routes'
import { useOrgHierarchy } from '../../../layouts/OrgHierarchyProvider'

const TABS: { label: string; to: string; end?: boolean }[] = [
  { label: 'Analytics', to: ROUTES.ADMISSION_ANALYTICS, end: true },
  { label: 'Overview', to: ROUTES.ADMISSION_OVERVIEW, end: true },
  { label: 'Students', to: ROUTES.STUDENTS, end: false },
]

export function AdmissionModuleLayout() {
  const org = useOrgHierarchy()
  const studentsListDisabled = !!org.divisionId.trim()

  return (
    <div style={styles.page}>
      <div style={styles.shell}>
        <header style={styles.header}>
          <div style={styles.headerTop}>
            <div>
              <div style={styles.badge}>Enrollment</div>
              <h1 style={styles.title}>Admission desk</h1>
              <p style={styles.subtitle}>
                Intake, rosters, and student 360° views — scope is set from the organization tree on the left.
              </p>
            </div>
          </div>
          <nav style={styles.tabs} aria-label="Admission sections">
            {TABS.map((t) => {
              const isStudents = t.to === ROUTES.STUDENTS
              if (isStudents && studentsListDisabled) {
                return (
                  <span
                    key={t.to}
                    style={{ ...styles.tab, ...styles.tabDisabled }}
                    aria-disabled="true"
                    title="Student list is not available when a division is selected. Choose class or higher in the tree."
                  >
                    {t.label}
                  </span>
                )
              }
              return (
                <NavLink
                  key={t.to}
                  to={t.to}
                  end={t.end}
                  style={({ isActive }) => ({
                    ...styles.tab,
                    ...(isActive ? styles.tabActive : {}),
                  })}
                >
                  {t.label}
                </NavLink>
              )
            })}
          </nav>
        </header>
        <div style={styles.body}>
          <Outlet />
        </div>
      </div>
    </div>
  )
}

const styles: Record<string, CSSProperties> = {
  page: { maxWidth: 1280, margin: '0 auto' },
  shell: {
    background: '#fff',
    borderRadius: 14,
    boxShadow: '0 4px 24px rgba(15, 23, 42, 0.07)',
    border: '1px solid #e2e8f0',
    overflow: 'hidden',
  },
  header: {
    padding: '22px 26px 0',
    borderBottom: '1px solid #e2e8f0',
    background: 'linear-gradient(145deg, #0f172a 0%, #1e3a5f 42%, #1e293b 100%)',
    color: '#fff',
  },
  headerTop: {
    display: 'flex',
    alignItems: 'flex-start',
    justifyContent: 'space-between',
    gap: 16,
    marginBottom: 18,
  },
  badge: {
    display: 'inline-block',
    fontSize: 10,
    fontWeight: 800,
    letterSpacing: '0.12em',
    textTransform: 'uppercase',
    color: '#93c5fd',
    marginBottom: 8,
  },
  title: { margin: 0, fontSize: 24, fontWeight: 800, letterSpacing: '-0.03em', color: '#fff' },
  subtitle: { margin: '10px 0 0', fontSize: 14, color: 'rgba(248, 250, 252, 0.82)', maxWidth: 720, lineHeight: 1.55 },
  tabs: {
    display: 'flex',
    flexWrap: 'wrap',
    gap: 6,
    marginTop: 4,
    paddingBottom: 0,
  },
  tab: {
    padding: '11px 18px',
    fontSize: 14,
    fontWeight: 600,
    color: 'rgba(248, 250, 252, 0.65)',
    textDecoration: 'none',
    borderRadius: '10px 10px 0 0',
    background: 'transparent',
    border: '1px solid transparent',
    marginBottom: -1,
  },
  tabActive: {
    color: '#0f172a',
    background: '#f8fafc',
    borderColor: '#e2e8f0',
    borderBottomColor: '#f8fafc',
  },
  tabDisabled: {
    opacity: 0.45,
    cursor: 'not-allowed',
    pointerEvents: 'none',
  },
  body: { padding: 26, background: '#f8fafc', minHeight: 320 },
}
