import { apiClient } from '../../../infrastructure/http'

function pickStr(...vals: unknown[]): string | undefined {
  for (const v of vals) {
    if (v == null) continue
    const s = String(v).trim()
    if (s) return s
  }
  return undefined
}

function num(v: unknown): number | undefined {
  if (v == null) return undefined
  const n = Number(v)
  return Number.isFinite(n) ? n : undefined
}

export type FeeHeadRow = {
  id: string
  name?: string
  accountCode?: string
}

export type FeeInstallmentRow = {
  id: string
  branchId?: string
  classId?: string
  academicYearId?: string
  installmentNo?: number
  installmentName?: string
  dueDate?: string
  lateFeeAfter?: string
  createdAt?: string
}

export type FeeStructureRow = {
  id: string
  classId?: string
  divisionId?: string
  feeHeadId?: string
  amount?: number
}

export type ClassFeeMapRow = {
  id: string
  classId?: string
  divisionId?: string
  academicYearId?: string
  feeHeadId?: string
  amount?: number
  installmentNo?: number
  academicYear?: string
  createdAt?: string
}

function parseFeeHead(raw: unknown): FeeHeadRow | null {
  if (raw == null || typeof raw !== 'object') return null
  const o = raw as Record<string, unknown>
  const id = pickStr(o.id, o.Id)
  if (!id) return null
  return {
    id,
    name: pickStr(o.name, o.Name),
    accountCode: pickStr(o.accountCode, o.AccountCode),
  }
}

function parseFeeInstallment(raw: unknown): FeeInstallmentRow | null {
  if (raw == null || typeof raw !== 'object') return null
  const o = raw as Record<string, unknown>
  const id = pickStr(o.id, o.Id)
  if (!id) return null
  const no = num(o.installmentNo ?? o.InstallmentNo)
  return {
    id,
    branchId: pickStr(o.branchId, o.BranchId),
    classId: pickStr(o.classId, o.ClassId),
    academicYearId: pickStr(o.academicYearId, o.AcademicYearId),
    installmentNo: no,
    installmentName: pickStr(o.installmentName, o.InstallmentName),
    dueDate: pickStr(o.dueDate, o.DueDate),
    lateFeeAfter: pickStr(o.lateFeeAfter, o.LateFeeAfter),
    createdAt: pickStr(o.createdAt, o.CreatedAt),
  }
}

function parseFeeStructure(raw: unknown): FeeStructureRow | null {
  if (raw == null || typeof raw !== 'object') return null
  const o = raw as Record<string, unknown>
  const id = pickStr(o.id, o.Id)
  if (!id) return null
  return {
    id,
    classId: pickStr(o.classId, o.ClassId),
    divisionId: pickStr(o.divisionId, o.DivisionId),
    feeHeadId: pickStr(o.feeHeadId, o.FeeHeadId),
    amount: num(o.amount ?? o.Amount),
  }
}

function parseClassFeeMap(raw: unknown): ClassFeeMapRow | null {
  if (raw == null || typeof raw !== 'object') return null
  const o = raw as Record<string, unknown>
  const id = pickStr(o.id, o.Id)
  if (!id) return null
  const instNo = num(o.installmentNo ?? o.InstallmentNo)
  return {
    id,
    classId: pickStr(o.classId, o.ClassId),
    divisionId: pickStr(o.divisionId, o.DivisionId),
    academicYearId: pickStr(o.academicYearId, o.AcademicYearId),
    feeHeadId: pickStr(o.feeHeadId, o.FeeHeadId),
    amount: num(o.amount ?? o.Amount),
    installmentNo: instNo !== undefined ? Math.trunc(instNo) : undefined,
    academicYear: pickStr(o.academicYear, o.AcademicYear),
    createdAt: pickStr(o.createdAt, o.CreatedAt),
  }
}

function asArray(data: unknown): unknown[] {
  return Array.isArray(data) ? data : []
}

export const feeHeadsMasterApi = {
  getAll: async (): Promise<FeeHeadRow[]> => {
    const { data } = await apiClient.get<unknown>('/FeeHead')
    return asArray(data).map(parseFeeHead).filter((x): x is FeeHeadRow => x != null)
  },
  create: async (body: { name: string; accountCode?: string | null }): Promise<FeeHeadRow> => {
    const { data } = await apiClient.post<unknown>('/FeeHead', {
      name: body.name.trim(),
      accountCode: body.accountCode?.trim() || null,
    })
    return parseFeeHead(data) ?? { id: '', name: body.name }
  },
  update: async (
    id: string,
    body: { name: string; accountCode?: string | null },
  ): Promise<FeeHeadRow> => {
    const { data } = await apiClient.put<unknown>(`/FeeHead/${id}`, {
      name: body.name.trim(),
      accountCode: body.accountCode?.trim() || null,
    })
    return parseFeeHead(data) ?? { id, name: body.name }
  },
  delete: async (id: string): Promise<void> => {
    await apiClient.delete(`/FeeHead/${id}`)
  },
}

export const feeInstallmentsMasterApi = {
  getAll: async (): Promise<FeeInstallmentRow[]> => {
    const { data } = await apiClient.get<unknown>('/FeeInstallment')
    return asArray(data).map(parseFeeInstallment).filter((x): x is FeeInstallmentRow => x != null)
  },
  create: async (body: {
    branchId: string
    classId: string
    academicYearId: string
    installmentNo: number
    installmentName: string
    dueDate: string
    lateFeeAfter?: string | null
  }): Promise<FeeInstallmentRow> => {
    const { data } = await apiClient.post<unknown>('/FeeInstallment', {
      branchId: body.branchId,
      classId: body.classId,
      academicYearId: body.academicYearId,
      installmentNo: body.installmentNo,
      installmentName: body.installmentName.trim(),
      dueDate: body.dueDate,
      lateFeeAfter: body.lateFeeAfter?.trim() || null,
    })
    return parseFeeInstallment(data) ?? { id: '' }
  },
  update: async (
    id: string,
    body: {
      branchId: string
      classId: string
      academicYearId: string
      installmentNo: number
      installmentName: string
      dueDate: string
      lateFeeAfter?: string | null
    },
  ): Promise<FeeInstallmentRow> => {
    const { data } = await apiClient.put<unknown>(`/FeeInstallment/${id}`, {
      branchId: body.branchId,
      classId: body.classId,
      academicYearId: body.academicYearId,
      installmentNo: body.installmentNo,
      installmentName: body.installmentName.trim(),
      dueDate: body.dueDate,
      lateFeeAfter: body.lateFeeAfter?.trim() || null,
    })
    return parseFeeInstallment(data) ?? { id }
  },
  delete: async (id: string): Promise<void> => {
    await apiClient.delete(`/FeeInstallment/${id}`)
  },
}

export const feeStructuresMasterApi = {
  getAll: async (): Promise<FeeStructureRow[]> => {
    const { data } = await apiClient.get<unknown>('/FeeStructure')
    return asArray(data).map(parseFeeStructure).filter((x): x is FeeStructureRow => x != null)
  },
  create: async (body: {
    classId: string
    divisionId?: string | null
    feeHeadId: string
    amount: number
  }): Promise<FeeStructureRow> => {
    const { data } = await apiClient.post<unknown>('/FeeStructure', {
      classId: body.classId,
      divisionId: body.divisionId?.trim() || null,
      feeHeadId: body.feeHeadId,
      amount: body.amount,
    })
    return parseFeeStructure(data) ?? { id: '' }
  },
  update: async (
    id: string,
    body: {
      classId: string
      divisionId?: string | null
      feeHeadId: string
      amount: number
    },
  ): Promise<FeeStructureRow> => {
    const { data } = await apiClient.put<unknown>(`/FeeStructure/${id}`, {
      classId: body.classId,
      divisionId: body.divisionId?.trim() || null,
      feeHeadId: body.feeHeadId,
      amount: body.amount,
    })
    return parseFeeStructure(data) ?? { id }
  },
  delete: async (id: string): Promise<void> => {
    await apiClient.delete(`/FeeStructure/${id}`)
  },
}

export const classFeeMapsMasterApi = {
  getAll: async (): Promise<ClassFeeMapRow[]> => {
    const { data } = await apiClient.get<unknown>('/ClassFeeMap')
    return asArray(data).map(parseClassFeeMap).filter((x): x is ClassFeeMapRow => x != null)
  },
  create: async (body: {
    classId: string
    divisionId?: string | null
    academicYearId: string
    feeHeadId: string
    amount: number
    installmentNo?: number | null
    academicYear?: string | null
  }): Promise<ClassFeeMapRow> => {
    const { data } = await apiClient.post<unknown>('/ClassFeeMap', {
      classId: body.classId,
      divisionId: body.divisionId?.trim() || null,
      academicYearId: body.academicYearId,
      feeHeadId: body.feeHeadId,
      amount: body.amount,
      installmentNo: body.installmentNo ?? null,
      academicYear: body.academicYear?.trim() || null,
    })
    return parseClassFeeMap(data) ?? { id: '' }
  },
  update: async (
    id: string,
    body: {
      classId: string
      divisionId?: string | null
      academicYearId: string
      feeHeadId: string
      amount: number
      installmentNo?: number | null
      academicYear?: string | null
    },
  ): Promise<ClassFeeMapRow> => {
    const { data } = await apiClient.put<unknown>(`/ClassFeeMap/${id}`, {
      classId: body.classId,
      divisionId: body.divisionId?.trim() || null,
      academicYearId: body.academicYearId,
      feeHeadId: body.feeHeadId,
      amount: body.amount,
      installmentNo: body.installmentNo ?? null,
      academicYear: body.academicYear?.trim() || null,
    })
    return parseClassFeeMap(data) ?? { id }
  },
  delete: async (id: string): Promise<void> => {
    await apiClient.delete(`/ClassFeeMap/${id}`)
  },
}
