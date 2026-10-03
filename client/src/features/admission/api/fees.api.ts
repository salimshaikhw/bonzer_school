import { apiClient } from '../../../infrastructure/http'

function num(v: unknown): number {
  const n = Number(v)
  return Number.isFinite(n) ? n : 0
}

function pickStr(...vals: unknown[]): string | undefined {
  for (const v of vals) {
    if (v == null) continue
    const s = String(v).trim()
    if (s) return s
  }
  return undefined
}

export interface StudentFeeDashboardDto {
  studentId: string
  rollNumber?: string
  totalFees: number
  paidFees: number
  remainingFees: number
  feeHeads: {
    feeHeadId: string
    feeHeadName: string
    totalAmount: number
    paidAmount: number
    remainingAmount: number
  }[]
  installments: {
    installmentId: string
    installmentName: string
    totalAmount: number
    paidAmount: number
    remainingAmount: number
    dueDate?: string
  }[]
}

export function parseStudentFeeDashboardDto(raw: Record<string, unknown>): StudentFeeDashboardDto {
  const feeHeadsRaw = (raw.feeHeads ?? raw.FeeHeads) as unknown[] | undefined
  const instRaw = (raw.installments ?? raw.Installments) as unknown[] | undefined

  return {
    studentId: pickStr(raw.studentId, raw.StudentId) ?? '',
    rollNumber: pickStr(raw.rollNumber, raw.RollNumber),
    totalFees: num(raw.totalFees ?? raw.TotalFees),
    paidFees: num(raw.paidFees ?? raw.PaidFees),
    remainingFees: num(raw.remainingFees ?? raw.RemainingFees),
    feeHeads: Array.isArray(feeHeadsRaw)
      ? feeHeadsRaw.map((x) => {
          const o = x as Record<string, unknown>
          return {
            feeHeadId: pickStr(o.feeHeadId, o.FeeHeadId) ?? '',
            feeHeadName: pickStr(o.feeHeadName, o.FeeHeadName) ?? '—',
            totalAmount: num(o.totalAmount ?? o.TotalAmount),
            paidAmount: num(o.paidAmount ?? o.PaidAmount),
            remainingAmount: num(o.remainingAmount ?? o.RemainingAmount),
          }
        })
      : [],
    installments: Array.isArray(instRaw)
      ? instRaw.map((x) => {
          const o = x as Record<string, unknown>
          return {
            installmentId: pickStr(o.installmentId, o.InstallmentId) ?? '',
            installmentName: pickStr(o.installmentName, o.InstallmentName) ?? '—',
            totalAmount: num(o.totalAmount ?? o.TotalAmount),
            paidAmount: num(o.paidAmount ?? o.PaidAmount),
            remainingAmount: num(o.remainingAmount ?? o.RemainingAmount),
            dueDate: pickStr(o.dueDate, o.DueDate),
          }
        })
      : [],
  }
}

export type BranchFeeDashboardDto = {
  totalFees: number
  collectedFees: number
  pendingFees: number
  collectionRate: number
  totalStudents: number
}

function parseBranchFeeDashboard(raw: Record<string, unknown>): BranchFeeDashboardDto {
  return {
    totalFees: num(raw.totalFees ?? raw.TotalFees),
    collectedFees: num(raw.collectedFees ?? raw.CollectedFees),
    pendingFees: num(raw.pendingFees ?? raw.PendingFees),
    collectionRate: num(raw.collectionRate ?? raw.CollectionRate),
    totalStudents: num(raw.totalStudents ?? raw.TotalStudents),
  }
}

export const admissionFeesApi = {
  getStudentDashboard: async (studentId: string, academicYearId: string): Promise<StudentFeeDashboardDto> => {
    const { data } = await apiClient.get<Record<string, unknown>>('/Fees/student-dashboard', {
      params: { studentId, academicYearId },
    })
    return parseStudentFeeDashboardDto(data)
  },

  getBranchDashboard: async (branchId: string, academicYearId: string): Promise<BranchFeeDashboardDto> => {
    const { data } = await apiClient.get<Record<string, unknown>>('/Fees/branch-dashboard', {
      params: { branchId, academicYearId },
    })
    return parseBranchFeeDashboard(data)
  },
}
