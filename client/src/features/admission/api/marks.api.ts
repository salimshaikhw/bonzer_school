import { apiClient } from '../../../infrastructure/http'

function pickStr(...vals: unknown[]): string | undefined {
  for (const v of vals) {
    if (v == null) continue
    const s = String(v).trim()
    if (s) return s
  }
  return undefined
}

export interface MarkRow {
  id: string
  examSubjectComponentId?: string
  studentId?: string
  marksObtained?: number
  isAbsent?: boolean
  isMedical?: boolean
  isExempt?: boolean
  remark?: string
  enteredAt?: string
}

export function normalizeMarkRow(raw: Record<string, unknown>): MarkRow | null {
  const id = pickStr(raw.id, raw.Id)
  if (!id) return null
  return {
    id,
    examSubjectComponentId: pickStr(raw.examSubjectComponentId, raw.ExamSubjectComponentId),
    studentId: pickStr(raw.studentId, raw.StudentId),
    marksObtained:
      raw.marksObtained != null
        ? Number(raw.marksObtained)
        : raw.MarksObtained != null
          ? Number(raw.MarksObtained)
          : undefined,
    isAbsent: Boolean(raw.isAbsent ?? raw.IsAbsent),
    isMedical: Boolean(raw.isMedical ?? raw.IsMedical),
    isExempt: Boolean(raw.isExempt ?? raw.IsExempt),
    remark: pickStr(raw.remark, raw.Remark),
    enteredAt: pickStr(raw.enteredAt, raw.EnteredAt),
  }
}

export function parseMarkRows(data: unknown): MarkRow[] {
  const arr = Array.isArray(data) ? data : []
  return arr
    .map((x) => (x && typeof x === 'object' ? normalizeMarkRow(x as Record<string, unknown>) : null))
    .filter((x): x is MarkRow => x != null)
}

export const admissionMarksApi = {
  getByStudentId: async (studentId: string): Promise<MarkRow[]> => {
    const { data } = await apiClient.get<unknown>(`/Mark/student/${studentId}`)
    return parseMarkRows(data)
  },
}
