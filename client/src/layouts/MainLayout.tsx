import type { CSSProperties } from 'react'
import { useMemo } from 'react'
import { Outlet, Link, useLocation, useNavigate } from 'react-router-dom'
import { appPersonaLabel, useAuth } from '../core'
import { ROUTES } from '../config/routes'
import { matchAppModule } from '../config/appModules'
import { getAppTrayModulesForPersona } from '../config/roleAccess'
import { Button, useMediaQuery } from '../shared'
import { hasOrgTreeSelection, OrgScopedModuleNav } from './OrgScopedModuleNav'
import { UnifiedSidebar } from './UnifiedSidebar'
import { OrgHierarchyProvider, useOrgHierarchy } from './OrgHierarchyProvider'

export function MainLayout() {
  return (
    <OrgHierarchyProvider>
      <MainLayoutShell />
    </OrgHierarchyProvider>
  )
}

function MainLayoutShell() {
  const { logout, userEmail, appPersona } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const wide = useMediaQuery('(min-width: 1024px)')
  const org = useOrgHierarchy()
  const activeModule = matchAppModule(location.pathname)

  const admissionStudentsDisabledTos =
    activeModule?.id === 'admission' && org.divisionId.trim() ? ([ROUTES.STUDENTS] as const) : undefined

  const trayModules = useMemo(() => {
    const list = getAppTrayModulesForPersona(appPersona)
    return [...list].sort((a, b) => {
      if (a.id === 'dashboard') return -1
      if (b.id === 'dashboard') return 1
      return 0
    })
  }, [appPersona])

  const showModuleNav =
    hasOrgTreeSelection(org) ||
    appPersona === 'admin' ||
    location.pathname.startsWith('/finance')

  function handleLogout() {
    logout()
    navigate(ROUTES.LOGIN)
  }

  return (
    <div style={styles.layout}>
      <header style={styles.header}>
        <div style={styles.headerLeft}>
          <Link to={ROUTES.HOME} style={styles.headerBrand}>
            <span style={styles.headerBrandMark} aria-hidden>
              B
            </span>
            <span>BonzerSchool</span>
          </Link>
          <span style={styles.headerDivider}>|</span>
          <Link to={ROUTES.HOME} style={styles.headerHomeLink}>
            Home
          </Link>
          {userEmail ? (
            <>
              <span style={styles.headerDivider}>|</span>
              <span style={styles.headerUser} title={userEmail}>
                {userEmail}
              </span>
            </>
          ) : null}
          <span style={styles.rolePill} title="Role from your account">
            {appPersonaLabel(appPersona)}
          </span>
        </div>
        <Button variant="secondary" onClick={handleLogout} style={styles.logout}>
          Sign out
        </Button>
      </header>

      <div style={styles.workspace}>
        <div
          style={{
            ...styles.bodyRow,
            flexDirection: wide ? 'row' : 'column',
          }}
        >
          <UnifiedSidebar org={org} activeModule={activeModule} layoutWide={wide} />

          <main style={styles.main}>
            <OrgScopedModuleNav
              org={org}
              activeModule={activeModule}
              disabledTos={admissionStudentsDisabledTos}
              showModules={showModuleNav}
              modules={trayModules}
            />
            <Outlet />
          </main>
        </div>
      </div>
    </div>
  )
}

const styles: Record<string, CSSProperties> = {
  layout: {
    minHeight: '100vh',
    background: '#e8edf3',
    display: 'flex',
    flexDirection: 'column',
  },
  header: {
    background: 'linear-gradient(90deg, #0f172a 0%, #1e3a5f 100%)',
    color: '#fff',
    padding: '0 20px',
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    height: 52,
    flexShrink: 0,
    zIndex: 4,
    boxShadow: '0 2px 8px rgba(15, 23, 42, 0.12)',
  },
  headerLeft: {
    display: 'flex',
    alignItems: 'center',
    gap: 10,
    minWidth: 0,
    flexWrap: 'wrap',
    rowGap: 6,
  },
  headerBrand: {
    display: 'flex',
    alignItems: 'center',
    gap: 8,
    fontSize: 15,
    fontWeight: 800,
    letterSpacing: '-0.02em',
    color: '#fff',
    textDecoration: 'none',
  },
  headerBrandMark: {
    width: 28,
    height: 28,
    borderRadius: 8,
    background: 'linear-gradient(135deg, #38bdf8 0%, #2563eb 100%)',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    fontSize: 13,
    fontWeight: 900,
    flexShrink: 0,
  },
  headerHomeLink: {
    fontSize: 13,
    fontWeight: 600,
    color: 'rgba(248, 250, 252, 0.95)',
    textDecoration: 'none',
  },
  headerDivider: { color: 'rgba(255,255,255,0.25)', fontSize: 14, userSelect: 'none' },
  headerUser: {
    fontSize: 12,
    fontWeight: 500,
    color: 'rgba(248, 250, 252, 0.88)',
    maxWidth: 200,
    overflow: 'hidden',
    textOverflow: 'ellipsis',
    whiteSpace: 'nowrap',
  },
  rolePill: {
    marginLeft: 10,
    fontSize: 10,
    fontWeight: 800,
    letterSpacing: '0.06em',
    textTransform: 'uppercase',
    padding: '4px 10px',
    borderRadius: 999,
    background: 'rgba(59, 130, 246, 0.35)',
    border: '1px solid rgba(147, 197, 253, 0.45)',
    color: '#e0f2fe',
    flexShrink: 0,
  },
  logout: { borderColor: 'rgba(255,255,255,0.45)', color: '#fff', fontSize: 13, padding: '7px 14px' },
  workspace: {
    display: 'flex',
    flex: 1,
    flexDirection: 'column',
    minHeight: 0,
  },
  bodyRow: {
    display: 'flex',
    flex: 1,
    minHeight: 0,
    alignItems: 'stretch',
    minWidth: 0,
  },
  main: {
    flex: 1,
    minWidth: 0,
    padding: '20px 24px 28px',
    background: '#f1f5f9',
    overflowY: 'auto',
  },
}
