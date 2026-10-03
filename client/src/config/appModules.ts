import { ROUTES } from './routes'

export type AppModuleChild = {
  label: string
  to: string
  /** Use for index routes so parent is not always active. */
  end?: boolean
}

export type AppModule = {
  id: string
  label: string
  /** Short label for narrow app tray */
  shortLabel: string
  basePath: string
  children: AppModuleChild[]
}

/** Ordered by specificity — longer paths must appear before shorter ones (e.g. /admin-dashboard before /). */
export const APP_MODULES: AppModule[] = [
  {
    id: 'admin',
    label: 'Administration',
    shortLabel: 'Admin',
    basePath: '/admin-dashboard',
    children: [
      { label: 'Overview', to: ROUTES.ADMIN_DASHBOARD, end: true },
      { label: 'School groups', to: ROUTES.ADMIN_ORG_GROUPS },
      { label: 'Schools', to: ROUTES.ADMIN_ORG_SCHOOLS },
      { label: 'Classes', to: ROUTES.ADMIN_ORG_CLASSES },
      { label: 'Divisions', to: ROUTES.ADMIN_ORG_DIVISIONS },
    ],
  },
  {
    id: 'admission',
    label: 'Admission',
    shortLabel: 'Adm',
    basePath: ROUTES.ADMISSION,
    children: [
      { label: 'Analytics', to: ROUTES.ADMISSION_ANALYTICS, end: true },
      { label: 'Overview', to: ROUTES.ADMISSION_OVERVIEW, end: true },
      { label: 'Students', to: ROUTES.STUDENTS, end: true },
    ],
  },
  {
    id: 'attendance',
    label: 'Attendance',
    shortLabel: 'Att',
    basePath: ROUTES.ATTENDANCE,
    children: [
      { label: 'Dashboard', to: ROUTES.ATTENDANCE, end: true },
      { label: 'Mark attendance', to: ROUTES.ATTENDANCE_DIVISION },
      { label: 'Holidays', to: ROUTES.ATTENDANCE_HOLIDAYS },
      { label: 'Bulk import', to: ROUTES.ATTENDANCE_BULK_IMPORT },
      { label: 'Division Excel', to: ROUTES.ATTENDANCE_DIVISION_EXCEL },
    ],
  },
  {
    id: 'exams',
    label: 'Exams',
    shortLabel: 'Exm',
    basePath: ROUTES.EXAMS,
    children: [{ label: 'Catalog', to: ROUTES.EXAMS, end: true }],
  },
  {
    id: 'finance',
    label: 'Finance',
    shortLabel: 'Fin',
    basePath: ROUTES.FINANCE,
    children: [
      { label: 'Collect fees', to: ROUTES.FINANCE_COLLECT, end: true },
      { label: 'Analytics', to: ROUTES.FINANCE_ANALYTICS, end: true },
      { label: 'Invoices', to: ROUTES.FINANCE_INVOICES, end: true },
      { label: 'Payments', to: ROUTES.FINANCE_PAYMENTS, end: true },
      { label: 'Fee masters', to: ROUTES.FINANCE_MASTERS, end: false },
    ],
  },
  {
    id: 'users',
    label: 'Users',
    shortLabel: 'Usr',
    basePath: ROUTES.USERS,
    children: [],
  },
  {
    id: 'fees',
    label: 'Fees',
    shortLabel: 'Pay',
    basePath: '/fees',
    children: [{ label: 'Pay fees', to: ROUTES.PARENT_FEES, end: true }],
  },
  {
    id: 'dashboard',
    label: 'Dashboard',
    shortLabel: 'Home',
    basePath: ROUTES.HOME,
    children: [],
  },
]

export function matchAppModule(pathname: string): AppModule | null {
  const normalized = pathname.startsWith('/') ? pathname : `/${pathname}`
  const ordered = [...APP_MODULES].sort((a, b) => b.basePath.length - a.basePath.length)
  for (const m of ordered) {
    if (m.basePath === ROUTES.HOME) {
      if (normalized === '/' || normalized === '') return m
      continue
    }
    if (normalized === m.basePath || normalized.startsWith(`${m.basePath}/`)) {
      return m
    }
  }
  return null
}

/** Home last so primary work modules (Admin, Admission, …) lead the rail — logo still links home. */
const TRAY_ORDER = ['admin', 'admission', 'attendance', 'exams', 'finance', 'users', 'fees', 'dashboard'] as const

/** Modules in left app tray order. */
export function getAppTrayModules(): AppModule[] {
  const map = new Map(APP_MODULES.map((m) => [m.id, m] as const))
  return TRAY_ORDER.map((id) => map.get(id)).filter((m): m is AppModule => m != null)
}
