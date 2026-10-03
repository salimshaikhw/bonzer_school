import type { AppPersona } from '../core/auth/appPersona'
import { getAppTrayModules, type AppModule } from './appModules'

const TRAY_ALLOW: Record<AppPersona, Set<string>> = {
  admin: new Set(['dashboard', 'admin', 'admission', 'attendance', 'exams', 'finance', 'users']),
  principal: new Set(['dashboard', 'admin', 'admission', 'attendance', 'exams', 'finance']),
  class_teacher: new Set(['dashboard', 'admission', 'attendance', 'exams', 'finance']),
  staff: new Set(['dashboard', 'admission', 'attendance', 'exams', 'finance']),
  parent: new Set(['dashboard', 'fees']),
}

/** App tray entries visible for this SaaS persona (multi-tenant school roles). */
export function getAppTrayModulesForPersona(persona: AppPersona): AppModule[] {
  const allow = TRAY_ALLOW[persona]
  return getAppTrayModules().filter((m) => allow.has(m.id))
}

/** Whether this persona should see fee / collection KPIs on the home dashboard. */
export function showFinancialKpisOnHome(persona: AppPersona): boolean {
  return persona === 'admin' || persona === 'principal' || persona === 'class_teacher' || persona === 'staff'
}
