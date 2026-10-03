import { apiClient } from '../../../infrastructure/http'
import type {
  BranchFeeDashboardDto,
  BranchOutstandingRow,
  ClassFeeDashboardDto,
  ClassOutstandingRow,
  DivisionFeeDashboardDto,
  DivisionOutstandingRow,
  FeeCollectionTrendPoint,
  FeeHeadAnalyticsRow,
  InstallmentAnalyticsRow,
  StudentOutstandingRow,
} from '../types'

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

function parseBranchDash(raw: Record<string, unknown>): BranchFeeDashboardDto {
  return {
    branchId: pickStr(raw.branchId, raw.BranchId) ?? '',
    totalFees: num(raw.totalFees ?? raw.TotalFees),
    collectedFees: num(raw.collectedFees ?? raw.CollectedFees),
    pendingFees: num(raw.pendingFees ?? raw.PendingFees),
    collectionRate: num(raw.collectionRate ?? raw.CollectionRate),
    totalStudents: num(raw.totalStudents ?? raw.TotalStudents),
  }
}

function parseClassDash(raw: Record<string, unknown>): ClassFeeDashboardDto {
  return {
    classId: pickStr(raw.classId, raw.ClassId) ?? '',
    totalFees: num(raw.totalFees ?? raw.TotalFees),
    collectedFees: num(raw.collectedFees ?? raw.CollectedFees),
    pendingFees: num(raw.pendingFees ?? raw.PendingFees),
    collectionRate: num(raw.collectionRate ?? raw.CollectionRate),
    totalStudents: num(raw.totalStudents ?? raw.TotalStudents),
  }
}

function parseDivisionDash(raw: Record<string, unknown>): DivisionFeeDashboardDto {
  return {
    divisionId: pickStr(raw.divisionId, raw.DivisionId) ?? '',
    totalFees: num(raw.totalFees ?? raw.TotalFees),
    collectedFees: num(raw.collectedFees ?? raw.CollectedFees),
    pendingFees: num(raw.pendingFees ?? raw.PendingFees),
    collectionRate: num(raw.collectionRate ?? raw.CollectionRate),
    totalStudents: num(raw.totalStudents ?? raw.TotalStudents),
  }
}

function parseTrend(raw: Record<string, unknown>): FeeCollectionTrendPoint {
  return {
    date: pickStr(raw.date, raw.Date) ?? '',
    amount: num(raw.amount ?? raw.Amount),
  }
}

function parseFeeHead(raw: Record<string, unknown>): FeeHeadAnalyticsRow {
  return {
    feeHeadId: pickStr(raw.feeHeadId, raw.FeeHeadId) ?? '',
    feeHeadName: pickStr(raw.feeHeadName, raw.FeeHeadName) ?? '—',
    totalAmount: num(raw.totalAmount ?? raw.TotalAmount),
  }
}

function parseInstallment(raw: Record<string, unknown>): InstallmentAnalyticsRow {
  return {
    installmentId: pickStr(raw.installmentId, raw.InstallmentId) ?? '',
    installmentName: pickStr(raw.installmentName, raw.InstallmentName) ?? '—',
    expectedAmount: num(raw.expectedAmount ?? raw.ExpectedAmount),
    collectedAmount: num(raw.collectedAmount ?? raw.CollectedAmount),
    pendingAmount: num(raw.pendingAmount ?? raw.PendingAmount),
    collectionPercent: num(raw.collectionPercent ?? raw.CollectionPercent),
    branchName: pickStr(raw.branchName, raw.BranchName),
    className: pickStr(raw.className, raw.ClassName),
    divisionName: pickStr(raw.divisionName, raw.DivisionName),
  }
}

function feeAnalyticsScopeParams(classId?: string, divisionId?: string): Record<string, string> {
  const p: Record<string, string> = {}
  if (classId?.trim()) p.classId = classId.trim()
  if (divisionId?.trim()) p.divisionId = divisionId.trim()
  return p
}

function parseStudentOut(raw: Record<string, unknown>): StudentOutstandingRow {
  return {
    studentId: pickStr(raw.studentId, raw.StudentId) ?? '',
    rollNumber: pickStr(raw.rollNumber, raw.RollNumber),
    classId: pickStr(raw.classId, raw.ClassId),
    divisionId: pickStr(raw.divisionId, raw.DivisionId),
    remainingAmount: num(raw.remainingAmount ?? raw.RemainingAmount),
  }
}

function parseClassOut(raw: Record<string, unknown>): ClassOutstandingRow {
  return {
    classId: pickStr(raw.classId, raw.ClassId) ?? '',
    className: pickStr(raw.className, raw.ClassName) ?? '—',
    remainingAmount: num(raw.remainingAmount ?? raw.RemainingAmount),
  }
}

function parseDivisionOut(raw: Record<string, unknown>): DivisionOutstandingRow {
  return {
    divisionId: pickStr(raw.divisionId, raw.DivisionId) ?? '',
    divisionName: pickStr(raw.divisionName, raw.DivisionName) ?? '—',
    remainingAmount: num(raw.remainingAmount ?? raw.RemainingAmount),
  }
}

function parseBranchOut(raw: Record<string, unknown>): BranchOutstandingRow {
  return {
    branchId: pickStr(raw.branchId, raw.BranchId) ?? '',
    branchName: pickStr(raw.branchName, raw.BranchName) ?? '—',
    remainingAmount: num(raw.remainingAmount ?? raw.RemainingAmount),
  }
}

export const adminFeesApi = {
  getBranchDashboard: async (branchId: string, academicYearId: string): Promise<BranchFeeDashboardDto> => {
    const { data } = await apiClient.get<Record<string, unknown>>('/Fees/branch-dashboard', {
      params: { branchId, academicYearId },
    })
    return parseBranchDash(data)
  },

  getClassDashboard: async (
    branchId: string,
    classId: string,
    academicYearId: string,
  ): Promise<ClassFeeDashboardDto> => {
    const { data } = await apiClient.get<Record<string, unknown>>('/Fees/class-dashboard', {
      params: { branchId, classId, academicYearId },
    })
    return parseClassDash(data)
  },

  getDivisionDashboard: async (
    branchId: string,
    divisionId: string,
    academicYearId: string,
  ): Promise<DivisionFeeDashboardDto> => {
    const { data } = await apiClient.get<Record<string, unknown>>('/Fees/division-dashboard', {
      params: { branchId, divisionId, academicYearId },
    })
    return parseDivisionDash(data)
  },

  getCollectionTrend: async (
    branchId: string,
    startDate: string,
    endDate: string,
  ): Promise<FeeCollectionTrendPoint[]> => {
    const { data } = await apiClient.get<unknown[]>('/Fees/collection-trend', {
      params: { branchId, startDate, endDate },
    })
    return Array.isArray(data) ? data.map((x) => parseTrend(x as Record<string, unknown>)) : []
  },

  getFeeHeadAnalytics: async (
    branchId: string,
    academicYearId: string,
    opts?: { classId?: string; divisionId?: string },
  ): Promise<FeeHeadAnalyticsRow[]> => {
    const { data } = await apiClient.get<unknown[]>('/Fees/feehead-analytics', {
      params: { branchId, academicYearId, ...feeAnalyticsScopeParams(opts?.classId, opts?.divisionId) },
    })
    return Array.isArray(data) ? data.map((x) => parseFeeHead(x as Record<string, unknown>)) : []
  },

  getInstallmentAnalytics: async (
    branchId: string,
    academicYearId: string,
    opts?: { classId?: string; divisionId?: string },
  ): Promise<InstallmentAnalyticsRow[]> => {
    const { data } = await apiClient.get<unknown[]>('/Fees/installment-analytics', {
      params: { branchId, academicYearId, ...feeAnalyticsScopeParams(opts?.classId, opts?.divisionId) },
    })
    return Array.isArray(data) ? data.map((x) => parseInstallment(x as Record<string, unknown>)) : []
  },

  getOutstandingByStudent: async (
    branchId: string,
    academicYearId: string,
    classId?: string,
    divisionId?: string,
  ): Promise<StudentOutstandingRow[]> => {
    const { data } = await apiClient.get<unknown[]>('/Fees/outstanding-by-student', {
      params: {
        branchId,
        academicYearId,
        ...(classId?.trim() ? { classId: classId.trim() } : {}),
        ...(divisionId?.trim() ? { divisionId: divisionId.trim() } : {}),
      },
    })
    return Array.isArray(data) ? data.map((x) => parseStudentOut(x as Record<string, unknown>)) : []
  },

  getOutstandingByClass: async (branchId: string, academicYearId: string): Promise<ClassOutstandingRow[]> => {
    const { data } = await apiClient.get<unknown[]>('/Fees/outstanding-by-class', {
      params: { branchId, academicYearId },
    })
    return Array.isArray(data) ? data.map((x) => parseClassOut(x as Record<string, unknown>)) : []
  },

  getOutstandingByDivision: async (
    branchId: string,
    academicYearId: string,
  ): Promise<DivisionOutstandingRow[]> => {
    const { data } = await apiClient.get<unknown[]>('/Fees/outstanding-by-division', {
      params: { branchId, academicYearId },
    })
    return Array.isArray(data) ? data.map((x) => parseDivisionOut(x as Record<string, unknown>)) : []
  },

  getOutstandingByBranch: async (academicYearId: string): Promise<BranchOutstandingRow[]> => {
    const { data } = await apiClient.get<unknown[]>('/Fees/outstanding-by-branch', {
      params: { academicYearId },
    })
    return Array.isArray(data) ? data.map((x) => parseBranchOut(x as Record<string, unknown>)) : []
  },
}
