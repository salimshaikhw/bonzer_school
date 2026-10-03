import { apiClient } from '../../../infrastructure/http'
import type { AttendanceSummary } from '../../attendance/attendance.types'
import type { Student, StudentRequest, StudentSearchFilter, PagedStudentsResponse } from '../students.types'
import { parseStudentFeeDashboardDto, type StudentFeeDashboardDto } from './fees.api'
import { parseMarkRows, type MarkRow } from './marks.api'

function pickStr(...vals: unknown[]): string | undefined {
  for (const v of vals) {
    if (v == null) continue
    const s = String(v).trim()
    if (s) return s
  }
  return undefined
}

function normEq(a: string | undefined, b: string | undefined): boolean {
  return String(a ?? '')
    .trim()
    .toLowerCase() === String(b ?? '').trim().toLowerCase()
}

/** Normalize one student row from API (camelCase or PascalCase). */
export function normalizeStudentFromApi(raw: unknown): Student | null {
  if (raw == null || typeof raw !== 'object') return null
  const o = raw as Record<string, unknown>
  const id = o.id ?? o.Id
  if (id == null) return null
  const sid = String(id).trim()
  if (!sid) return null
  return {
    id: sid,
    branchId: pickStr(o.branchId, o.BranchId),
    academicYearId: pickStr(o.academicYearId, o.AcademicYearId),
    classId: pickStr(o.classId, o.ClassId),
    divisionId: pickStr(o.divisionId, o.DivisionId),
    rollNumber: pickStr(o.rollNumber, o.RollNumber),
    firstName: pickStr(o.firstName, o.FirstName),
    middleName: pickStr(o.middleName, o.MiddleName),
    lastName: pickStr(o.lastName, o.LastName),
    admissionNo: pickStr(o.admissionNo, o.AdmissionNo),
    studentStatus: pickStr(o.studentStatus, o.StudentStatus),
    enrollmentNo: pickStr(o.enrollmentNo, o.EnrollmentNo),
  }
}

/** Full student row for profile / portfolio (extends list normalization with common detail fields). */
export function normalizeStudentFullFromApi(raw: unknown): Student | null {
  const base = normalizeStudentFromApi(raw)
  if (!base || !raw || typeof raw !== 'object') return null
  const o = raw as Record<string, unknown>
  return {
    ...base,
    gender: pickStr(o.gender, o.Gender) ?? base.gender,
    dob: pickStr(o.dob == null ? undefined : String(o.dob), o.Dob == null ? undefined : String(o.Dob)),
    email: pickStr(o.email, o.Email),
    phone: pickStr(o.phone, o.Phone),
    fatherName: pickStr(o.fatherName, o.FatherName),
    fatherPhone: pickStr(o.fatherPhone, o.FatherPhone),
    motherName: pickStr(o.motherName, o.MotherName),
    motherPhone: pickStr(o.motherPhone, o.MotherPhone),
    admissionDate: pickStr(o.admissionDate, o.AdmissionDate),
    hasScholarship:
      o.hasScholarship === true || o.HasScholarship === true
        ? true
        : o.hasScholarship === false || o.HasScholarship === false
          ? false
          : undefined,
    scholarshipType: pickStr(o.scholarshipType, o.ScholarshipType),
    ewsDgCategory: pickStr(o.ewsDgCategory, o.EwsDgCategory),
    medicalConditions: pickStr(o.medicalConditions, o.MedicalConditions),
  }
}

function normPortfolioAttendance(raw: unknown): AttendanceSummary | null {
  if (!raw || typeof raw !== 'object') return null
  const r = raw as Record<string, unknown>
  return {
    presentCount: Number(r.presentCount ?? r.PresentCount ?? 0),
    absentCount: Number(r.absentCount ?? r.AbsentCount ?? 0),
    leaveCount: Number(r.leaveCount ?? r.LeaveCount ?? 0),
    halfDayCount: Number(r.halfDayCount ?? r.HalfDayCount ?? 0),
    lateCount: Number(r.lateCount ?? r.LateCount ?? 0),
    totalDays: Number(r.totalDays ?? r.TotalDays ?? 0),
    percentage: Number(r.percentage ?? r.Percentage ?? 0),
  }
}

export type StudentAdmissionPortfolio = {
  student: Student
  attendance: AttendanceSummary | null
  fees: StudentFeeDashboardDto | null
  marks: MarkRow[]
}

function normalizePagedStudents(raw: unknown): PagedStudentsResponse {
  const d = raw as Record<string, unknown>
  const items = (d.items ?? d.Items ?? d.source ?? d.Source) as Student[] | undefined
  const list = Array.isArray(items) ? items : []
  const totalCount = Number(d.totalCount ?? d.TotalCount ?? 0)
  const page = Number(d.page ?? d.Page ?? 1)
  const pageSize = Number(d.pageSize ?? d.PageSize ?? 20)
  let totalPages = Number(d.totalPages ?? d.TotalPages ?? 0)
  if (totalPages <= 0 && pageSize > 0) {
    totalPages = Math.max(1, Math.ceil(totalCount / pageSize))
  }
  return { items: list, totalCount, page, pageSize, totalPages }
}

export const studentsApi = {
  getAll: async (): Promise<Student[]> => {
    const { data } = await apiClient.get<Student[]>('/Student')
    return data
  },

  getPaged: async (page: number, pageSize: number): Promise<PagedStudentsResponse> => {
    try {
      const { data } = await apiClient.get<unknown>('/Student/paged', {
        params: { page, pageSize },
      })
      return normalizePagedStudents(data)
    } catch {
      // Fallback for API instances that don't expose /Student/paged yet.
      const all = await studentsApi.getAll()
      const safePageSize = Math.max(1, pageSize)
      const safePage = Math.max(1, page)
      const start = (safePage - 1) * safePageSize
      const items = all.slice(start, start + safePageSize)
      return {
        items,
        totalCount: all.length,
        page: safePage,
        pageSize: safePageSize,
        totalPages: Math.max(1, Math.ceil(all.length / safePageSize)),
      }
    }
  },

  getTotal: async (): Promise<number> => {
    const { data } = await apiClient.get<{ total: number }>('/Student/total')
    return data.total
  },

  getById: async (id: string): Promise<Student> => {
    const { data } = await apiClient.get<Student>(`/Student/${id}`)
    return data
  },

  getByAdmission: async (branchId: string, admissionNo: string): Promise<Student | null> => {
    try {
      const { data } = await apiClient.get<Student>('/Student/by-admission', {
        params: { branchId, admissionNo },
      })
      return data
    } catch {
      return null
    }
  },

  getByRoll: async (classId: string, rollNo: string): Promise<Student | null> => {
    try {
      const { data } = await apiClient.get<Student>('/Student/by-roll', {
        params: { classId, rollNo },
      })
      return data
    } catch {
      return null
    }
  },

  getByClass: async (classId: string): Promise<Student[]> => {
    const { data } = await apiClient.get<unknown>(`/Student/class/${classId}`)
    const arr = Array.isArray(data) ? data : []
    return arr.map(normalizeStudentFromApi).filter((x): x is Student => x != null)
  },

  getByDivision: async (divisionId: string): Promise<Student[]> => {
    const { data } = await apiClient.get<unknown>(`/Student/division/${divisionId}`)
    const arr = Array.isArray(data) ? data : []
    return arr.map(normalizeStudentFromApi).filter((x): x is Student => x != null)
  },

  /**
   * Division endpoint first; if empty (stale cache or mapping issue), load class roster and keep rows for this division.
   */
  getStudentsForDivisionManualMark: async (divisionId: string, classId: string): Promise<Student[]> => {
    const fromDiv = await studentsApi.getByDivision(divisionId)
    if (fromDiv.length > 0) return fromDiv
    if (!classId) return []
    const inClass = await studentsApi.getByClass(classId)
    const want = divisionId.trim().toLowerCase()
    return inClass.filter((s) => {
      const d = pickStr(s.divisionId, (s as Record<string, unknown>).DivisionId)
      return d != null && d.trim().toLowerCase() === want
    })
  },

  getByBranch: async (branchId: string): Promise<Student[]> => {
    const { data } = await apiClient.get<Student[]>(`/Student/branch/${branchId}`)
    return data
  },

  search: async (filter: StudentSearchFilter): Promise<Student[]> => {
    const { searchTerm, keyword, ...rest } = filter
    const kw = (keyword ?? searchTerm)?.trim()
    const body: Record<string, unknown> = { ...rest }
    if (kw) body.keyword = kw
    const { data } = await apiClient.post<unknown>('/Student/search', body)
    const arr = Array.isArray(data) ? data : []
    return arr.map(normalizeStudentFromApi).filter((x): x is Student => x != null)
  },

  /**
   * Fees desk: resolve by student Id (GUID) or roll / admission fragment within a branch.
   */
  findForFeesCollection: async (branchId: string, term: string): Promise<Student[]> => {
    const t = term.trim()
    if (!t) return []
    const guidRe =
      /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i
    if (guidRe.test(t)) {
      try {
        const { data: raw } = await apiClient.get<unknown>(`/Student/${t}`)
        const s = normalizeStudentFullFromApi(raw)
        if (!s) return []
        if (branchId.trim() && s.branchId && normEq(s.branchId, branchId)) return [s]
        if (!branchId.trim()) return [s]
        return []
      } catch {
        return []
      }
    }
    if (!branchId.trim()) return []
    return studentsApi.search({ branchId, keyword: t })
  },

  create: async (payload: StudentRequest): Promise<Student> => {
    const { data } = await apiClient.post<Student>('/Student', payload)
    return data
  },

  update: async (id: string, payload: Partial<StudentRequest>): Promise<Student> => {
    const { data } = await apiClient.put<Student>(`/Student/${id}`, payload)
    return data
  },

  patch: async (id: string, payload: Partial<StudentRequest>): Promise<Student> => {
    const { data } = await apiClient.patch<Student>(`/Student/${id}`, payload)
    return data
  },

  delete: async (id: string): Promise<void> => {
    await apiClient.delete(`/Student/${id}`)
  },

  softDelete: async (id: string): Promise<void> => {
    await apiClient.delete(`/Student/${id}/soft`)
  },

  promote: async (
    id: string,
    newClassId: string,
    academicYearId: string
  ): Promise<void> => {
    await apiClient.post(`/Student/${id}/promote`, null, {
      params: { newClassId, academicYearId },
    })
  },

  transfer: async (id: string, newBranchId: string): Promise<void> => {
    await apiClient.post(`/Student/${id}/transfer`, null, {
      params: { newBranchId },
    })
  },

  issueTc: async (id: string): Promise<void> => {
    await apiClient.post(`/Student/${id}/issue-tc`)
  },

  restore: async (id: string): Promise<void> => {
    await apiClient.post(`/Student/${id}/restore`)
  },

  // Analytics / admission dashboard
  getTodayAdmissions: async (): Promise<Student[]> => {
    const { data } = await apiClient.get<Student[]>('/Student/today-admissions')
    return data
  },

  getStrengthByClass: async (branchId: string): Promise<{ id?: string; count: number }[]> => {
    const { data } = await apiClient.get<{ id?: string; count: number }[]>('/Student/analytics/strength-by-class', {
      params: { branchId },
    })
    return data
  },

  getStrengthByDivision: async (branchId: string): Promise<{ id?: string; count: number }[]> => {
    const { data } = await apiClient.get<{ id?: string; count: number }[]>('/Student/analytics/strength-by-division', {
      params: { branchId },
    })
    return data
  },

  getGenderDistribution: async (branchId: string): Promise<{ key: string; count: number }[]> => {
    const { data } = await apiClient.get<{ key: string; count: number }[]>('/Student/analytics/gender-distribution', {
      params: { branchId },
    })
    return data
  },

  getAdmissionTrend: async (branchId: string): Promise<{ year: number; count: number }[]> => {
    const { data } = await apiClient.get<{ year: number; count: number }[]>('/Student/analytics/admission-trend', {
      params: { branchId },
    })
    return data
  },

  getCasteWiseDistribution: async (branchId: string): Promise<{ key: string; count: number }[]> => {
    const { data } = await apiClient.get<{ key: string; count: number }[]>('/Student/analytics/caste-distribution', {
      params: { branchId },
    })
    return data
  },

  getCategoryWiseDistribution: async (branchId: string): Promise<{ key: string; count: number }[]> => {
    const { data } = await apiClient.get<{ key: string; count: number }[]>('/Student/analytics/category-distribution', {
      params: { branchId },
    })
    return data
  },

  /** One round-trip for student profile: student + attendance + fees + marks (best-effort partials on server). */
  getAdmissionPortfolio: async (id: string): Promise<StudentAdmissionPortfolio> => {
    const { data } = await apiClient.get<Record<string, unknown>>(`/Student/${id}/admission-portfolio`)
    const st = data.student ?? data.Student
    const student = normalizeStudentFullFromApi(st)
    if (!student) throw new Error('Student not found')
    const feesRaw = data.fees ?? data.Fees
    const marksRaw = data.marks ?? data.Marks
    const attRaw = data.attendanceAcademicYear ?? data.AttendanceAcademicYear
    return {
      student,
      attendance: normPortfolioAttendance(attRaw),
      fees:
        feesRaw != null && typeof feesRaw === 'object'
          ? parseStudentFeeDashboardDto(feesRaw as Record<string, unknown>)
          : null,
      marks: parseMarkRows(marksRaw),
    }
  },
}
