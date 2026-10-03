/**
 * Central route path constants.
 * Use these for navigation and route definitions to avoid magic strings.
 */
export const ROUTES = {
  HOME: '/',
  LOGIN: '/login',
  /** Parent portal: pay fees against school invoices */
  PARENT_FEES: '/fees/parent',
  /** Staff: fees, invoices, payments (does not overlap with {@link ROUTES.PARENT_FEES}). */
  FINANCE: '/finance',
  FINANCE_ANALYTICS: '/finance/analytics',
  FINANCE_INVOICES: '/finance/invoices',
  FINANCE_PAYMENTS: '/finance/payments',
  FINANCE_COLLECT: '/finance/collect',
  FINANCE_MASTERS: '/finance/masters',
  FINANCE_MASTERS_FEE_HEADS: '/finance/masters/fee-heads',
  FINANCE_MASTERS_INSTALLMENTS: '/finance/masters/installments',
  FINANCE_MASTERS_CLASS_FEE_MAP: '/finance/masters/class-fee-map',
  FINANCE_MASTERS_FEE_STRUCTURE: '/finance/masters/fee-structure',
  FINANCE_MASTERS_ACADEMIC_YEARS: '/finance/masters/academic-years',
  REGISTER: '/register',
  FORGOT_PASSWORD: '/forgot-password',
  USERS: '/users',
  ADMISSION: '/admission',
  /** Legacy admission overview path. */
  ADMISSION_OVERVIEW: '/admission/overview',
  STUDENTS: '/admission/students',
  STUDENTS_NEW: '/admission/students/new',
  STUDENT_DETAIL: (id: string) => `/admission/students/${id}` as const,
  STUDENT_EDIT: (id: string) => `/admission/students/${id}/edit` as const,
  ADMIN_DASHBOARD: '/admin-dashboard',
  ADMIN_ORG_GROUPS: '/admin-dashboard/org/school-groups',
  ADMIN_ORG_SCHOOLS: '/admin-dashboard/org/schools',
  ADMIN_ORG_CLASSES: '/admin-dashboard/org/classes',
  ADMIN_ORG_DIVISIONS: '/admin-dashboard/org/divisions',
  ADMISSION_ANALYTICS: '/admission/analytics',
  ATTENDANCE: '/attendance',
  ATTENDANCE_DIVISION: '/attendance/division',
  ATTENDANCE_STUDENT: '/attendance/student',
  ATTENDANCE_HOLIDAYS: '/attendance/holidays',
  ATTENDANCE_BULK_IMPORT: '/attendance/bulk-import',
  ATTENDANCE_DIVISION_EXCEL: '/attendance/division-excel',
  /** Staff: exam catalog (lists GET /api/Exam). */
  EXAMS: '/exams',
} as const

export type RoutePath = (typeof ROUTES)[keyof typeof ROUTES]
