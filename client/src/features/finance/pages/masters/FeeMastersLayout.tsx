import type { CSSProperties } from 'react'
import { NavLink, Outlet } from 'react-router-dom'
import { ROUTES } from '../../../../config/routes'
import { financeTheme as t } from '../../ui/financeTheme'
import { ms } from './feeMasterShared'

const SUB: { label: string; to: string }[] = [
  { label: 'Academic years', to: ROUTES.FINANCE_MASTERS_ACADEMIC_YEARS },
  { label: 'Fee heads', to: ROUTES.FINANCE_MASTERS_FEE_HEADS },
  { label: 'Installments & dates', to: ROUTES.FINANCE_MASTERS_INSTALLMENTS },
  { label: 'Class fee map', to: ROUTES.FINANCE_MASTERS_CLASS_FEE_MAP },
  { label: 'Fee structure', to: ROUTES.FINANCE_MASTERS_FEE_STRUCTURE },
]

export function FeeMastersLayout() {
  return (
    <div>
      <p style={intro}>
        Start with academic years, then fee heads, installments (due dates), class fee mapping, and optional fee
        structure rows used by billing and dashboards.
      </p>
      <nav style={ms.subNav} aria-label="Fee master data">
        {SUB.map((s) => (
          <NavLink
            key={s.to}
            to={s.to}
            end
            style={({ isActive }) => ({
              ...ms.subTab,
              ...(isActive ? ms.subTabActive : {}),
            })}
          >
            {s.label}
          </NavLink>
        ))}
      </nav>
      <Outlet />
    </div>
  )
}

const intro: CSSProperties = {
  margin: '0 0 16px',
  fontSize: 14,
  color: t.textMuted,
  lineHeight: 1.55,
  maxWidth: 800,
}
