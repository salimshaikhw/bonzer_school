export type AppPersona = 'admin' | 'principal' | 'class_teacher' | 'parent' | 'staff'

/** Map Identity roles (from JWT) to a single UI persona. */
export function resolveAppPersona(roles: string[]): AppPersona {
  const n = roles.map((r) => String(r).trim().toLowerCase())
  if (n.some((r) => r === 'admin' || r === 'superadmin' || r === 'administrator')) return 'admin'
  if (n.some((r) => r === 'principal' || r === 'schooladmin' || r === 'headmaster')) return 'principal'
  if (n.some((r) => r === 'classteacher' || r === 'class_teacher' || r === 'teacher')) return 'class_teacher'
  if (n.some((r) => r === 'parent' || r === 'guardian')) return 'parent'
  return 'staff'
}

export function appPersonaLabel(p: AppPersona): string {
  switch (p) {
    case 'admin':
      return 'Administrator'
    case 'principal':
      return 'Principal'
    case 'class_teacher':
      return 'Class teacher'
    case 'parent':
      return 'Parent'
    case 'staff':
      return 'Staff'
    default:
      return 'Staff'
  }
}

export function appPersonaDescription(p: AppPersona): string {
  switch (p) {
    case 'admin':
      return 'Full multi-school organization, users, and modules.'
    case 'principal':
      return 'School-level oversight: enrollment, fees, and attendance analytics.'
    case 'class_teacher':
      return 'Class roster, admission views, and daily attendance workflows.'
    case 'parent':
      return 'Family portal: focused dashboard for your children.'
    case 'staff':
      return 'Standard staff access to admission and attendance.'
    default:
      return ''
  }
}
