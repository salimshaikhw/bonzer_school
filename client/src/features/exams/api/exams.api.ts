import { apiClient } from '../../../infrastructure/http'

function pickStr(...vals: unknown[]): string | undefined {
  for (const v of vals) {
    if (v == null) continue
    const s = String(v).trim()
    if (s) return s
  }
  return undefined
}

export type ExamRow = {
  id: string
  name?: string
  examTypeId?: string
  academicYearId?: string
  branchId?: string
  classId?: string
  divisionId?: string
  startDate?: string
  endDate?: string
  status?: string
  isLocked?: boolean
  createdAt?: string
}

function parseExam(raw: unknown): ExamRow | null {
  if (raw == null || typeof raw !== 'object') return null
  const o = raw as Record<string, unknown>
  const id = pickStr(o.id, o.Id)
  if (!id) return null
  return {
    id,
    name: pickStr(o.name, o.Name),
    examTypeId: pickStr(o.examTypeId, o.ExamTypeId),
    academicYearId: pickStr(o.academicYearId, o.AcademicYearId),
    branchId: pickStr(o.branchId, o.BranchId),
    classId: pickStr(o.classId, o.ClassId),
    divisionId: pickStr(o.divisionId, o.DivisionId),
    startDate: pickStr(o.startDate, o.StartDate),
    endDate: pickStr(o.endDate, o.EndDate),
    status: pickStr(o.status, o.Status),
    isLocked: o.isLocked === true || o.IsLocked === true,
    createdAt: pickStr(o.createdAt, o.CreatedAt),
  }
}

export const examsApi = {
  getAll: async (): Promise<ExamRow[]> => {
    const { data } = await apiClient.get<unknown>('/Exam')
    const raw = Array.isArray(data) ? data : []
    return raw.map(parseExam).filter((x): x is ExamRow => x != null)
  },
}
