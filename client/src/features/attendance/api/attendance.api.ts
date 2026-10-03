import axios from 'axios'
import { apiClient } from '../../../infrastructure/http'

function parseApiErrorJsonText(text: string): string | null {
  try {
    const j = JSON.parse(text) as Record<string, unknown>
    const vm = j.validationMessage ?? j.ValidationMessage
    const um = j.userMessage ?? j.UserMessage
    if (typeof vm === 'string' && vm.trim()) return vm.trim()
    if (typeof um === 'string' && um.trim()) return um.trim()
    const message = j.message ?? j.Message
    if (typeof message === 'string' && message.trim()) return message.trim()
  } catch {
    /* not JSON */
  }
  const t = text.trim()
  return t ? t.slice(0, 500) : null
}

async function ensureExcelBlob(blob: Blob): Promise<void> {
  if (!blob || blob.size < 32) {
    const t = blob ? await blob.text() : ''
    throw new Error(parseApiErrorJsonText(t) ?? 'Empty or invalid file from server.')
  }
  const head = new Uint8Array(await blob.slice(0, 4).arrayBuffer())
  if (head[0] === 0x50 && head[1] === 0x4b) return
  const t = await blob.text()
  throw new Error(parseApiErrorJsonText(t) ?? 'Server did not return an Excel file (.xlsx).')
}
import type {
  AttendanceDashboard,
  AttendanceSummary,
  AttendanceReport,
  AttendanceTrendPoint,
  AttendancePeriodType,
  AttendanceRecord,
} from '../attendance.types'

function toCamelCase<T>(obj: Record<string, unknown> | null | undefined): T {
  if (obj == null || typeof obj !== 'object') return {} as T
  const result: Record<string, unknown> = {}
  for (const [k, v] of Object.entries(obj)) {
    const camel = k.charAt(0).toLowerCase() + k.slice(1)
    if (v != null && typeof v === 'object' && !Array.isArray(v)) {
      result[camel] = toCamelCase(v as Record<string, unknown>)
    } else {
      result[camel] = v
    }
  }
  return result as T
}

function normalizeBulkImportPreview(data: Record<string, unknown>): BulkAttendanceImportPreview {
  const rowArr = (data.rows ?? data.Rows) as unknown[] | undefined
  const rows = Array.isArray(rowArr)
    ? rowArr.map((r) => toCamelCase((r ?? {}) as Record<string, unknown>) as BulkAttendanceImportRowResult)
    : []
  return {
    rows,
    totalRows: Number(data.totalRows ?? data.TotalRows ?? rows.length),
    resolvedCount: Number(data.resolvedCount ?? data.ResolvedCount ?? 0),
    errorCount: Number(data.errorCount ?? data.ErrorCount ?? 0),
    wouldImportCount: Number(data.wouldImportCount ?? data.WouldImportCount ?? 0),
    holidayAwareRowCount: Number(data.holidayAwareRowCount ?? data.HolidayAwareRowCount ?? 0),
  }
}

function normalizeBulkImportApply(data: Record<string, unknown>): BulkAttendanceImportApplyResult {
  const base = normalizeBulkImportPreview(data)
  return {
    ...base,
    savedCount: Number(data.savedCount ?? data.SavedCount ?? 0),
  }
}

/** API returns raw JSON number for several * /percentage routes, not { percentage: n }. */
function parsePercentageResponse(data: unknown): number {
  if (typeof data === 'number' && Number.isFinite(data)) return data
  if (data != null && typeof data === 'object') {
    const o = data as Record<string, unknown>
    const p = o.percentage ?? o.Percentage
    if (typeof p === 'number' && Number.isFinite(p)) return p
  }
  return 0
}

export const attendanceApi = {
  getDashboard: async (branchId: string): Promise<AttendanceDashboard> => {
    const { data } = await apiClient.get<Record<string, unknown>>(`/Attendance/branch/${branchId}/dashboard`)
    return toCamelCase(data) as AttendanceDashboard
  },

  getTodaySummary: async (branchId: string): Promise<AttendanceSummary> => {
    const { data } = await apiClient.get<Record<string, unknown>>(`/Attendance/branch/${branchId}/today/summary`)
    return toCamelCase(data) as AttendanceSummary
  },

  getBranchSummary: async (branchId: string, periodType: AttendancePeriodType): Promise<AttendanceSummary> => {
    const { data } = await apiClient.get<Record<string, unknown>>(`/Attendance/branch/${branchId}/summary`, {
      params: { periodType },
    })
    return toCamelCase(data) as AttendanceSummary
  },

  getBranchTrend: async (branchId: string, periodType: AttendancePeriodType): Promise<AttendanceTrendPoint[]> => {
    const { data } = await apiClient.get<unknown[]>(`/Attendance/branch/${branchId}/trend`, {
      params: { periodType },
    })
    return (Array.isArray(data) ? data : []).map((d) => toCamelCase(d as Record<string, unknown>) as AttendanceTrendPoint)
  },

  getClassSummary: async (
    classId: string,
    periodType: AttendancePeriodType,
    startDate?: string,
    endDate?: string
  ): Promise<AttendanceSummary> => {
    const params: Record<string, string> = { periodType }
    if (startDate) params.startDate = startDate
    if (endDate) params.endDate = endDate
    const { data } = await apiClient.get<Record<string, unknown>>(`/Attendance/class/${classId}/summary`, {
      params,
    })
    return toCamelCase(data) as AttendanceSummary
  },

  getClassPercentage: async (classId: string, periodType: AttendancePeriodType): Promise<number> => {
    const { data } = await apiClient.get<unknown>(`/Attendance/class/${classId}/percentage`, {
      params: { periodType },
    })
    return parsePercentageResponse(data)
  },

  getClassTrend: async (classId: string, periodType: AttendancePeriodType): Promise<AttendanceTrendPoint[]> => {
    const { data } = await apiClient.get<unknown[]>(`/Attendance/class/${classId}/trend`, {
      params: { periodType },
    })
    return (Array.isArray(data) ? data : []).map((d) => toCamelCase(d as Record<string, unknown>) as AttendanceTrendPoint)
  },

  getDivisionReport: async (
    divisionId: string,
    periodType: AttendancePeriodType,
    startDate?: string,
    endDate?: string
  ): Promise<AttendanceReport> => {
    const params: Record<string, string> = { periodType }
    if (startDate) params.startDate = startDate
    if (endDate) params.endDate = endDate
    const { data } = await apiClient.get<Record<string, unknown>>(`/Attendance/division/${divisionId}/report`, {
      params,
    })
    const report = toCamelCase(data) as AttendanceReport
    if (report.summary && typeof (report.summary as unknown as Record<string, unknown>).presentCount === 'number') {
      report.summary = report.summary as AttendanceSummary
    }
    return report
  },

  getDivisionSummary: async (
    divisionId: string,
    periodType: AttendancePeriodType,
    startDate?: string,
    endDate?: string
  ): Promise<AttendanceSummary> => {
    const params: Record<string, string> = { periodType }
    if (startDate) params.startDate = startDate
    if (endDate) params.endDate = endDate
    const { data } = await apiClient.get<unknown>(`/Attendance/division/${divisionId}/summary`, {
      params,
    })
    if (data != null && typeof data === 'object' && !Array.isArray(data)) {
      return toCamelCase(data as Record<string, unknown>) as AttendanceSummary
    }
    return {
      presentCount: 0,
      absentCount: 0,
      leaveCount: 0,
      halfDayCount: 0,
      lateCount: 0,
      totalDays: 0,
      percentage: 0,
    }
  },

  getDivisionPercentage: async (divisionId: string, periodType: AttendancePeriodType): Promise<number> => {
    const { data } = await apiClient.get<unknown>(`/Attendance/division/${divisionId}/percentage`, {
      params: { periodType },
    })
    return parsePercentageResponse(data)
  },

  getDivisionTrend: async (divisionId: string, periodType: AttendancePeriodType): Promise<AttendanceTrendPoint[]> => {
    const { data } = await apiClient.get<unknown>(`/Attendance/division/${divisionId}/trend`, {
      params: { periodType },
    })
    const arr = Array.isArray(data) ? data : []
    return arr
      .filter((d): d is Record<string, unknown> => d != null && typeof d === 'object' && !Array.isArray(d))
      .map((d) => toCamelCase(d) as AttendanceTrendPoint)
  },

  /** Single calendar day (server uses periodType Day + startDate). */
  getDivisionDetailedForDay: async (divisionId: string, dateYmd: string): Promise<AttendanceRecord[]> => {
    const { data } = await apiClient.get<unknown[]>(`/Attendance/division/${divisionId}/detailed`, {
      params: { periodType: 'Day', startDate: dateYmd },
    })
    const arr = Array.isArray(data) ? data : []
    return arr
      .filter((raw): raw is Record<string, unknown> => raw != null && typeof raw === 'object' && !Array.isArray(raw))
      .map((raw) => {
      const c = toCamelCase(raw) as Record<string, unknown>
      return {
        id: String(c.id ?? ''),
        studentId: c.studentId != null ? String(c.studentId) : undefined,
        attendanceDate: c.attendanceDate != null ? String(c.attendanceDate) : undefined,
        status: c.status != null ? String(c.status) : undefined,
        leaveType: c.leaveType != null ? String(c.leaveType) : undefined,
        remark: c.remark != null ? String(c.remark) : undefined,
      }
    })
  },

  /**
   * Mark attendance for every row in one request (same as POST /Attendance/class/{classId}/{divisionId}?date=…).
   */
  markClassAttendance: async (
    classId: string,
    divisionId: string,
    dateYmd: string,
    students: { studentId: string; status?: string | null; leaveType?: string | null; remark?: string | null }[],
  ): Promise<unknown> => {
    const { data } = await apiClient.post<unknown>(
      `/Attendance/class/${classId}/${divisionId}`,
      students.map((s) => ({
        studentId: s.studentId,
        status: s.status,
        leaveType: s.leaveType || null,
        remark: s.remark || null,
      })),
      { params: { date: dateYmd } },
    )
    return data
  },

  getStudentReport: async (
    studentId: string,
    periodType: AttendancePeriodType,
    startDate?: string,
    endDate?: string
  ): Promise<AttendanceReport> => {
    const params: Record<string, string> = { periodType }
    if (startDate) params.startDate = startDate
    if (endDate) params.endDate = endDate
    const { data } = await apiClient.get<Record<string, unknown>>(`/Attendance/student/${studentId}/report`, {
      params,
    })
    return toCamelCase(data) as AttendanceReport
  },

  getStudentSummary: async (
    studentId: string,
    periodType: AttendancePeriodType,
    startDate?: string,
    endDate?: string
  ): Promise<AttendanceSummary> => {
    const params: Record<string, string> = { periodType }
    if (startDate) params.startDate = startDate
    if (endDate) params.endDate = endDate
    const { data } = await apiClient.get<Record<string, unknown>>(`/Attendance/student/${studentId}/summary`, {
      params,
    })
    return toCamelCase(data) as AttendanceSummary
  },

  getStudentPercentage: async (studentId: string, periodType: AttendancePeriodType): Promise<number> => {
    const { data } = await apiClient.get<unknown>(`/Attendance/student/${studentId}/percentage`, {
      params: { periodType },
    })
    return parsePercentageResponse(data)
  },

  getStudentTrend: async (studentId: string, periodType: AttendancePeriodType): Promise<AttendanceTrendPoint[]> => {
    const { data } = await apiClient.get<unknown[]>(`/Attendance/student/${studentId}/trend`, {
      params: { periodType },
    })
    return (Array.isArray(data) ? data : []).map((d) => toCamelCase(d as Record<string, unknown>) as AttendanceTrendPoint)
  },

  previewBulkImport: async (body: BulkAttendanceImportRequest): Promise<BulkAttendanceImportPreview> => {
    const { data } = await apiClient.post<Record<string, unknown>>('/Attendance/bulk-import/preview', body)
    return normalizeBulkImportPreview(data)
  },

  importBulkFromMachine: async (body: BulkAttendanceImportRequest): Promise<BulkAttendanceImportApplyResult> => {
    const { data } = await apiClient.post<Record<string, unknown>>('/Attendance/bulk-import', body)
    return normalizeBulkImportApply(data)
  },

  /** GET — official .xlsx template from API (RollNumber, ClassId, DivisionName, AttendanceDate, Status, LeaveType, IsHoliday). */
  downloadBulkImportSampleXlsx: async (): Promise<Blob> => {
    const { data } = await apiClient.get<Blob>('/Attendance/bulk-import/sample', { responseType: 'blob' })
    return data
  },

  /** POST — Excel with class/division/teacher/students × date range; approved leaves pre-filled. */
  downloadDivisionAttendanceTemplate: async (body: {
    branchId: string
    classId: string
    divisionId: string
    startDate: string
    endDate: string
  }): Promise<Blob> => {
    try {
      const res = await apiClient.post<Blob>('/Attendance/bulk-import/division-template', body, {
        responseType: 'blob',
      })
      const data = res.data
      const ct = (res.headers['content-type'] ?? '').toLowerCase()
      if (ct.includes('application/json') || ct.includes('text/')) {
        const t = await data.text()
        throw new Error(parseApiErrorJsonText(t) ?? 'Server returned an error instead of Excel.')
      }
      await ensureExcelBlob(data)
      return data
    } catch (e) {
      if (axios.isAxiosError(e) && e.response?.data instanceof Blob) {
        const t = await e.response.data.text()
        throw new Error(parseApiErrorJsonText(t) ?? `Download failed (${e.response.status}).`)
      }
      throw e
    }
  },

  previewBulkImportFromFile: async (payload: BulkAttendanceImportFilePayload): Promise<BulkAttendanceImportPreview> => {
    const fd = buildBulkImportFormData(payload)
    const { data } = await apiClient.post<Record<string, unknown>>('/Attendance/bulk-import/preview/file', fd)
    return normalizeBulkImportPreview(data)
  },

  importBulkFromFile: async (payload: BulkAttendanceImportFilePayload): Promise<BulkAttendanceImportApplyResult> => {
    const fd = buildBulkImportFormData(payload)
    const { data } = await apiClient.post<Record<string, unknown>>('/Attendance/bulk-import/file', fd)
    return normalizeBulkImportApply(data)
  },

  /** Preview filled workbook from division-template export (server requires class + division on form). */
  previewDivisionTemplateImportFromFile: async (payload: BulkAttendanceImportFilePayload): Promise<BulkAttendanceImportPreview> => {
    const fd = buildBulkImportFormData(payload)
    const { data } = await apiClient.post<Record<string, unknown>>('/Attendance/bulk-import/division-template/preview-file', fd)
    return normalizeBulkImportPreview(data)
  },

  /** Save filled workbook from division-template export. */
  importDivisionTemplateFromFile: async (payload: BulkAttendanceImportFilePayload): Promise<BulkAttendanceImportApplyResult> => {
    const fd = buildBulkImportFormData(payload)
    const { data } = await apiClient.post<Record<string, unknown>>('/Attendance/bulk-import/division-template/import-file', fd)
    return normalizeBulkImportApply(data)
  },
}

function buildBulkImportFormData(payload: BulkAttendanceImportFilePayload): FormData {
  const fd = new FormData()
  fd.append('branchId', payload.branchId)
  if (payload.classId) fd.append('classId', payload.classId)
  if (payload.divisionId) fd.append('divisionId', payload.divisionId)
  if (payload.classTeacherId) fd.append('classTeacherId', payload.classTeacherId)
  if (payload.defaultDate) fd.append('defaultDate', payload.defaultDate)
  fd.append('defaultPresentWhenStatusEmpty', String(payload.defaultPresentWhenStatusEmpty))
  fd.append('file', payload.file)
  return fd
}

export type BulkAttendanceImportRowInput = {
  rowIndex: number
  studentId?: string | null
  rollNumber?: string | null
  admissionNo?: string | null
  enrollmentNo?: string | null
  /** YYYY-MM-DD (maps to API Date) */
  date?: string | null
  /** Same as date; preferred column name RollNumber + ClassId + DivisionName layout */
  attendanceDate?: string | null
  classId?: string | null
  divisionName?: string | null
  status?: string | null
  leaveType?: string | null
  isHoliday?: boolean | null
  remark?: string | null
}

export type BulkAttendanceImportRequest = {
  branchId: string
  classId?: string | null
  divisionId?: string | null
  classTeacherId?: string | null
  defaultDate?: string | null
  defaultPresentWhenStatusEmpty: boolean
  rows: BulkAttendanceImportRowInput[]
}

/** Multipart upload for POST …/bulk-import/preview/file and …/bulk-import/file */
export type BulkAttendanceImportFilePayload = {
  branchId: string
  classId?: string | null
  divisionId?: string | null
  classTeacherId?: string | null
  defaultDate?: string | null
  defaultPresentWhenStatusEmpty: boolean
  file: File
}

export type BulkAttendanceImportRowResult = {
  rowIndex: number
  resolved: boolean
  studentId?: string | null
  studentDisplayName?: string | null
  error?: string | null
  date?: string | null
  rawStatus?: string | null
  normalizedStatus?: string | null
  resolvedClassId?: string | null
  resolvedDivisionId?: string | null
  leaveType?: string | null
  rowIsHoliday?: boolean | null
  isWeeklyOff: boolean
  isCalendarHoliday: boolean
  effectiveHoliday: boolean
  wouldImport: boolean
}

export type BulkAttendanceImportPreview = {
  rows: BulkAttendanceImportRowResult[]
  totalRows: number
  resolvedCount: number
  errorCount: number
  wouldImportCount: number
  holidayAwareRowCount: number
}

export type BulkAttendanceImportApplyResult = BulkAttendanceImportPreview & {
  savedCount: number
}
