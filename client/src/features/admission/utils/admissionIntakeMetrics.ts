import type { DivisionItem } from '../api/referenceData.api'
import type { Student } from '../students.types'

export type IntakeScopeLevel = 'group' | 'school' | 'class' | 'division'

export type IntakeKpi = {
  level: IntakeScopeLevel
  title: string
  enrolled: number | null
  capacity: number | null
  remaining: number | null
  /** Shown when enrolled/capacity are not applicable until user drills down */
  placeholder?: string
}

function norm(s: string | undefined | null): string {
  return String(s ?? '')
    .trim()
    .toLowerCase()
}

export function getDivisionSeats(d: DivisionItem): number {
  const n = d.availableSeats ?? (d as unknown as { AvailableSeats?: number }).AvailableSeats
  return typeof n === 'number' && n >= 0 ? n : 0
}

function getBranchIdFromDivision(d: DivisionItem): string | undefined {
  return d.branchId ?? (d as unknown as { BranchId?: string }).BranchId
}

function getClassIdFromDivision(d: DivisionItem): string | undefined {
  return d.classId ?? (d as unknown as { ClassId?: string }).ClassId
}

function remainingSeatsForDivisions(
  divisions: DivisionItem[],
  strengthByDivision: { id?: string; count: number }[],
): number | null {
  if (divisions.length === 0) return null
  let remaining = 0
  let anyCap = false
  for (const d of divisions) {
    const seats = getDivisionSeats(d)
    if (seats <= 0) continue
    anyCap = true
    const count = strengthByDivision.find((x) => norm(x.id) === norm(d.id))?.count ?? 0
    remaining += Math.max(0, seats - count)
  }
  return anyCap ? remaining : null
}

/** One KPI row for the organization level currently selected in the tree (no mixing school/class/division in one strip). */
export function buildIntakeKpiForScopeLevel(
  level: IntakeScopeLevel,
  input: {
    title: string
    effectiveBranchIds: string[]
    branchId: string
    classId: string
    divisionId: string
    allDivisions: DivisionItem[]
    scopedStudents: Student[]
    strengthByDivision: { id?: string; count: number }[]
  },
): IntakeKpi {
  const { title, effectiveBranchIds, branchId, classId, divisionId, allDivisions, scopedStudents, strengthByDivision } =
    input

  if (level === 'group') {
    if (effectiveBranchIds.length === 0) {
      return {
        level: 'group',
        title,
        enrolled: null,
        capacity: null,
        remaining: null,
        placeholder: 'Select a school group in the organization tree.',
      }
    }
    const branchSet = new Set(effectiveBranchIds.map(norm))
    const divisionsInScope = allDivisions.filter((d) => branchSet.has(norm(getBranchIdFromDivision(d))))
    const capacity = divisionsInScope.reduce((s, d) => s + getDivisionSeats(d), 0)
    const enrolled = scopedStudents.length
    const hasCap = capacity > 0
    return {
      level: 'group',
      title,
      enrolled,
      capacity: hasCap ? capacity : null,
      remaining: hasCap ? remainingSeatsForDivisions(divisionsInScope, strengthByDivision) : null,
    }
  }

  if (level === 'school') {
    if (!branchId.trim()) {
      return {
        level: 'school',
        title,
        enrolled: null,
        capacity: null,
        remaining: null,
        placeholder: 'Select a school (branch) in the organization tree.',
      }
    }
    const divisionsInScope = allDivisions.filter((d) => norm(getBranchIdFromDivision(d)) === norm(branchId))
    const capacity = divisionsInScope.reduce((s, d) => s + getDivisionSeats(d), 0)
    const enrolled = scopedStudents.length
    const hasCap = capacity > 0
    return {
      level: 'school',
      title,
      enrolled,
      capacity: hasCap ? capacity : null,
      remaining: hasCap ? remainingSeatsForDivisions(divisionsInScope, strengthByDivision) : null,
    }
  }

  if (level === 'class') {
    if (!branchId.trim() || !classId.trim()) {
      return {
        level: 'class',
        title,
        enrolled: null,
        capacity: null,
        remaining: null,
        placeholder: 'Pick a school, then a class. Class intake is always for a single branch.',
      }
    }
    const divisionsInScope = allDivisions.filter(
      (d) => norm(getBranchIdFromDivision(d)) === norm(branchId) && norm(getClassIdFromDivision(d)) === norm(classId),
    )
    const capacity = divisionsInScope.reduce((s, d) => s + getDivisionSeats(d), 0)
    const enrolled = scopedStudents.length
    const hasCap = capacity > 0
    return {
      level: 'class',
      title,
      enrolled,
      capacity: hasCap ? capacity : null,
      remaining: hasCap ? remainingSeatsForDivisions(divisionsInScope, strengthByDivision) : null,
    }
  }

  if (!branchId.trim() || !divisionId.trim()) {
    return {
      level: 'division',
      title,
      enrolled: null,
      capacity: null,
      remaining: null,
      placeholder: 'Select a division under a school.',
    }
  }
  const d = allDivisions.find((x) => norm(x.id) === norm(divisionId))
  const cap = d ? getDivisionSeats(d) : 0
  const fromApi = strengthByDivision.find((x) => norm(x.id) === norm(divisionId))?.count
  const enrolled = fromApi ?? scopedStudents.length
  const hasCap = cap > 0
  return {
    level: 'division',
    title,
    enrolled,
    capacity: hasCap ? cap : null,
    remaining: hasCap ? Math.max(0, cap - enrolled) : null,
  }
}
