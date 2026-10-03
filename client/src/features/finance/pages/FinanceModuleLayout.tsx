import type { CSSProperties } from 'react'
import { useMemo } from 'react'
import { NavLink, Outlet } from 'react-router-dom'
import { ROUTES } from '../../../config/routes'
import { useOrgHierarchy } from '../../../layouts/OrgHierarchyProvider'
import { financeModuleScopeHint } from '../utils/financeOrgScope'
import { financeTheme as t } from '../ui/financeTheme'

const TABS: { label: string; to: string; end?: boolean }[] = [
  { label: 'Collect fees', to: ROUTES.FINANCE_COLLECT, end: true },
  { label: 'Analytics', to: ROUTES.FINANCE_ANALYTICS, end: true },
  { label: 'Invoices', to: ROUTES.FINANCE_INVOICES, end: true },
  { label: 'Payments', to: ROUTES.FINANCE_PAYMENTS, end: true },
  { label: 'Masters', to: ROUTES.FINANCE_MASTERS, end: false },
]

export function FinanceModuleLayout() {
  const org = useOrgHierarchy()
  const scopeHint = useMemo(() => financeModuleScopeHint(org), [
    org.branchId,
    org.groupScopeId,
    org.classId,
    org.divisionId,
  ])

  return (
    <div style={styles.page}>
      <div style={styles.shell}>
        <header style={styles.header}>
          <div style={styles.headerTop}>
            <div style={styles.headerLeft}>
              <div style={styles.productRow}>
                <span style={styles.productDot} aria-hidden />
                <span style={styles.productLabel}>BonzerSchool</span>
                <span style={styles.productSlash}>/</span>
                <span style={styles.productModule}>Finance</span>
              </div>
              <h1 style={styles.title}>Fees & billing</h1>
              <p style={styles.subtitle}>{scopeHint}</p>
            </div>
          </div>
          <nav style={styles.tabTrack} aria-label="Finance sections">
            {TABS.map((tab) => (
              <NavLink
                key={tab.to}
                to={tab.to}
                end={tab.end}
                style={({ isActive }) => ({
                  ...styles.tab,
                  ...(isActive ? styles.tabActive : {}),
                })}
              >
                {tab.label}
              </NavLink>
            ))}
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
  page: {
    maxWidth: 1320,
    margin: '0 auto',
    padding: '0 12px 32px',
    fontFamily: t.fontSans,
  },
  shell: {
    background: t.surface,
    borderRadius: t.radiusLg,
    boxShadow: t.shadowMd,
    border: `1px solid ${t.border}`,
    /* Allow nested pages (e.g. collect fees) to use full width without clipping negative margins / shadows */
    overflow: 'visible',
  },
  header: {
    padding: '24px 28px 16px',
    background: t.surface,
    borderBottom: `1px solid ${t.border}`,
  },
  headerTop: {
    display: 'flex',
    alignItems: 'flex-start',
    justifyContent: 'space-between',
    gap: 16,
    marginBottom: 20,
  },
  headerLeft: { minWidth: 0 },
  productRow: {
    display: 'flex',
    alignItems: 'center',
    gap: 8,
    marginBottom: 10,
    flexWrap: 'wrap',
  },
  productDot: {
    width: 8,
    height: 8,
    borderRadius: t.radiusPill,
    background: t.accent,
    flexShrink: 0,
  },
  productLabel: {
    fontSize: 12,
    fontWeight: 600,
    color: t.textMuted,
    letterSpacing: '0.02em',
  },
  productSlash: { color: t.borderStrong, fontSize: 13, fontWeight: 300 },
  productModule: {
    fontSize: 12,
    fontWeight: 700,
    color: t.accent,
    textTransform: 'uppercase',
    letterSpacing: '0.06em',
  },
  title: {
    margin: 0,
    fontSize: 26,
    fontWeight: 700,
    letterSpacing: '-0.03em',
    color: t.text,
    lineHeight: 1.2,
  },
  subtitle: {
    margin: '10px 0 0',
    fontSize: 14,
    color: t.textMuted,
    maxWidth: 720,
    lineHeight: 1.55,
  },
  tabTrack: {
    display: 'flex',
    flexWrap: 'wrap',
    gap: 4,
    padding: 4,
    background: t.bgPage,
    borderRadius: t.radius,
    marginTop: 4,
    width: 'fit-content',
    maxWidth: '100%',
  },
  tab: {
    padding: '10px 20px',
    fontSize: 14,
    fontWeight: 600,
    color: t.textSecondary,
    textDecoration: 'none',
    borderRadius: 8,
    border: '1px solid transparent',
    background: 'transparent',
    display: 'inline-block',
    transition: 'background 0.15s ease, color 0.15s ease, box-shadow 0.15s ease',
  },
  tabActive: {
    color: t.text,
    background: t.surface,
    boxShadow: t.shadow,
  },
  body: { padding: '28px 28px 36px', background: t.bgPage, minHeight: 360 },
}
