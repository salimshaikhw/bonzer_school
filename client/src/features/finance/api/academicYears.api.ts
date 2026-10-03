import { apiClient } from '../../../infrastructure/http'
import type { AcademicYearRow } from '../types'

function pickStr(...vals: unknown[]): string | undefined {
  for (const v of vals) {
    if (v == null) continue
    const s = String(v).trim()
    if (s) return s
  }
  return undefined
}

function parseBool(v: unknown): boolean | undefined {
  if (typeof v === 'boolean') return v
  if (v === 'true' || v === 'True') return true
  if (v === 'false' || v === 'False') return false
  return undefined
}

function parseRow(raw: unknown): AcademicYearRow | null {
  if (raw == null || typeof raw !== 'object') return null
  const o = raw as Record<string, unknown>
  const id = pickStr(o.id, o.Id)
  if (!id) return null
  const isActiveRaw = o.isActive ?? o.IsActive
  return {
    id,
    yearName: pickStr(o.yearName, o.YearName),
    startDate: pickStr(o.startDate, o.StartDate),
    endDate: pickStr(o.endDate, o.EndDate),
    isActive: typeof isActiveRaw === 'boolean' ? isActiveRaw : parseBool(isActiveRaw),
  }
}

export type AcademicYearUpsert = {
  yearName: string
  startDate: string
  endDate: string
  isActive?: boolean | null
}

export const academicYearsApi = {
  getAll: async (): Promise<AcademicYearRow[]> => {
    const { data } = await apiClient.get<unknown>('/AcademicYear')
    const raw = Array.isArray(data) ? data : []
    return raw.map(parseRow).filter((x): x is AcademicYearRow => x != null)
  },

  create: async (body: AcademicYearUpsert): Promise<AcademicYearRow> => {
    const { data } = await apiClient.post<unknown>('/AcademicYear', {
      yearName: body.yearName.trim(),
      startDate: body.startDate.trim(),
      endDate: body.endDate.trim(),
      isActive: body.isActive ?? null,
    })
    return parseRow(data) ?? { id: '', yearName: body.yearName }
  },

  update: async (id: string, body: AcademicYearUpsert): Promise<AcademicYearRow> => {
    const { data } = await apiClient.put<unknown>(`/AcademicYear/${id}`, {
      yearName: body.yearName.trim(),
      startDate: body.startDate.trim(),
      endDate: body.endDate.trim(),
      isActive: body.isActive ?? null,
    })
    return parseRow(data) ?? { id, yearName: body.yearName }
  },

  delete: async (id: string): Promise<void> => {
    await apiClient.delete(`/AcademicYear/${id}`)
  },
}
