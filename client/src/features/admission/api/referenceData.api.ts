import axios from 'axios'
import { apiClient } from '../../../infrastructure/http'

export interface ClassItem {
  id: string
  name?: string
  branchId?: string
}

export interface DivisionItem {
  id: string
  name?: string
  classId?: string
  branchId?: string
  code?: string
  availableSeats?: number
}

function extractRows(payload: unknown): Record<string, unknown>[] {
  if (Array.isArray(payload)) {
    return payload.filter((row): row is Record<string, unknown> => !!row && typeof row === 'object')
  }
  if (!payload || typeof payload !== 'object') return []

  const o = payload as Record<string, unknown>
  const nested =
    (Array.isArray(o.data) && o.data) ||
    (Array.isArray(o.items) && o.items) ||
    (Array.isArray(o.value) && o.value) ||
    (Array.isArray(o.$values) && o.$values)

  if (Array.isArray(nested)) {
    return nested.filter((row): row is Record<string, unknown> => !!row && typeof row === 'object')
  }
  return []
}

function normId(value: string | undefined | null): string {
  return String(value ?? '')
    .trim()
    .toLowerCase()
}

export const referenceDataApi = {
  getClasses: async (): Promise<ClassItem[]> => {
    const { data } = await apiClient.get<unknown>('/Class')
    return extractRows(data)
      .map((o) => ({
        id: String(o.id ?? o.Id ?? ''),
        name: o.name != null ? String(o.name) : o.Name != null ? String(o.Name) : undefined,
        branchId:
          o.branchId != null ? String(o.branchId) : o.BranchId != null ? String(o.BranchId) : undefined,
      }))
      .filter((x) => x.id.length > 0)
  },

  getDivisions: async (): Promise<DivisionItem[]> => {
    const { data } = await apiClient.get<unknown>('/Division')
    return extractRows(data)
      .map((o) => ({
        id: String(o.id ?? o.Id ?? ''),
        name: o.name != null ? String(o.name) : o.Name != null ? String(o.Name) : undefined,
        classId: o.classId != null ? String(o.classId) : o.ClassId != null ? String(o.ClassId) : undefined,
        branchId:
          o.branchId != null ? String(o.branchId) : o.BranchId != null ? String(o.BranchId) : undefined,
        code: o.code != null ? String(o.code) : o.Code != null ? String(o.Code) : undefined,
        availableSeats:
          o.availableSeats != null
            ? Number(o.availableSeats)
            : o.AvailableSeats != null
              ? Number(o.AvailableSeats)
              : undefined,
      }))
      .filter((x) => x.id.length > 0)
  },

  /** Classes for a single branch (sidebar + hierarchy). */
  getClassesByBranch: async (branchId: string): Promise<ClassItem[]> => {
    const id = branchId.trim()
    if (!id) return []
    const mapRows = (data: unknown): ClassItem[] =>
      extractRows(data)
        .map((o) => ({
          id: String(o.id ?? o.Id ?? ''),
          name: o.name != null ? String(o.name) : o.Name != null ? String(o.Name) : undefined,
          branchId:
            o.branchId != null ? String(o.branchId) : o.BranchId != null ? String(o.BranchId) : id,
        }))
        .filter((x) => x.id.length > 0)

    try {
      const { data } = await apiClient.get<unknown>(`/Class/by-branch/${encodeURIComponent(id)}`)
      return mapRows(data)
    } catch (e) {
      if (axios.isAxiosError(e) && e.response?.status === 404) {
        const all = await referenceDataApi.getClasses()
        return all.filter((c) => normId(c.branchId) === normId(id))
      }
      throw e
    }
  },

  /** Divisions for a single class. */
  getDivisionsByClass: async (classId: string): Promise<DivisionItem[]> => {
    const id = classId.trim()
    if (!id) return []
    const mapRows = (data: unknown): DivisionItem[] =>
      extractRows(data)
        .map((o) => ({
          id: String(o.id ?? o.Id ?? ''),
          name: o.name != null ? String(o.name) : o.Name != null ? String(o.Name) : undefined,
          classId: o.classId != null ? String(o.classId) : o.ClassId != null ? String(o.ClassId) : id,
          branchId:
            o.branchId != null ? String(o.branchId) : o.BranchId != null ? String(o.BranchId) : undefined,
          code: o.code != null ? String(o.code) : o.Code != null ? String(o.Code) : undefined,
          availableSeats:
            o.availableSeats != null
              ? Number(o.availableSeats)
              : o.AvailableSeats != null
                ? Number(o.AvailableSeats)
                : undefined,
        }))
        .filter((x) => x.id.length > 0)

    try {
      const { data } = await apiClient.get<unknown>(`/Division/by-class/${encodeURIComponent(id)}`)
      return mapRows(data)
    } catch (e) {
      if (axios.isAxiosError(e) && e.response?.status === 404) {
        const all = await referenceDataApi.getDivisions()
        return all.filter((d) => normId(d.classId) === normId(id))
      }
      throw e
    }
  },

  /** Use when the all-divisions list omits classId or uses different casing. */
  getDivisionById: async (id: string): Promise<DivisionItem | null> => {
    try {
      const { data } = await apiClient.get<unknown>(`/Division/${id}`)
      if (data == null || typeof data !== 'object' || Array.isArray(data)) return null
      const o = data as Record<string, unknown>
      return {
        id: String(o.id ?? o.Id ?? id),
        name: o.name != null ? String(o.name) : o.Name != null ? String(o.Name) : undefined,
        classId:
          o.classId != null
            ? String(o.classId)
            : o.ClassId != null
              ? String(o.ClassId)
              : undefined,
        branchId:
          o.branchId != null
            ? String(o.branchId)
            : o.BranchId != null
              ? String(o.BranchId)
              : undefined,
        availableSeats:
          o.availableSeats != null
            ? Number(o.availableSeats)
            : o.AvailableSeats != null
              ? Number(o.AvailableSeats)
              : undefined,
      }
    } catch {
      return null
    }
  },

  createClass: async (payload: { branchId: string; name: string }): Promise<ClassItem> => {
    const body = { branchId: payload.branchId, name: payload.name.trim() }
    const { data } = await apiClient.post<unknown>('/Class', body)
    const o = (data ?? {}) as Record<string, unknown>
    return {
      id: String(o.id ?? o.Id ?? ''),
      name: (o.name ?? o.Name) as string | undefined,
      branchId: (o.branchId ?? o.BranchId) as string | undefined,
    }
  },

  updateClass: async (id: string, payload: { branchId: string; name: string }): Promise<ClassItem> => {
    const body = { branchId: payload.branchId, name: payload.name.trim() }
    const { data } = await apiClient.put<unknown>(`/Class/${id}`, body)
    const o = (data ?? {}) as Record<string, unknown>
    return {
      id: String(o.id ?? o.Id ?? id),
      name: (o.name ?? o.Name) as string | undefined,
      branchId: (o.branchId ?? o.BranchId) as string | undefined,
    }
  },

  deleteClass: async (id: string): Promise<void> => {
    await apiClient.delete(`/Class/${id}`)
  },

  createDivision: async (payload: {
    classId: string
    branchId?: string | null
    name: string
    code?: string
    availableSeats?: number | null
  }): Promise<DivisionItem> => {
    const body = {
      classId: payload.classId,
      branchId: payload.branchId || null,
      name: payload.name.trim(),
      code: payload.code?.trim() || null,
      availableSeats: payload.availableSeats ?? null,
    }
    const { data } = await apiClient.post<unknown>('/Division', body)
    const o = (data ?? {}) as Record<string, unknown>
    return {
      id: String(o.id ?? o.Id ?? ''),
      name: (o.name ?? o.Name) as string | undefined,
      classId: (o.classId ?? o.ClassId) as string | undefined,
      branchId: (o.branchId ?? o.BranchId) as string | undefined,
      availableSeats: (o.availableSeats ?? o.AvailableSeats) as number | undefined,
    }
  },

  updateDivision: async (
    id: string,
    payload: {
      classId: string
      branchId?: string | null
      name: string
      code?: string
      availableSeats?: number | null
    }
  ): Promise<DivisionItem> => {
    const body = {
      classId: payload.classId,
      branchId: payload.branchId || null,
      name: payload.name.trim(),
      code: payload.code?.trim() || null,
      availableSeats: payload.availableSeats ?? null,
    }
    const { data } = await apiClient.put<unknown>(`/Division/${id}`, body)
    const o = (data ?? {}) as Record<string, unknown>
    return {
      id: String(o.id ?? o.Id ?? id),
      name: (o.name ?? o.Name) as string | undefined,
      classId: (o.classId ?? o.ClassId) as string | undefined,
      branchId: (o.branchId ?? o.BranchId) as string | undefined,
      availableSeats: (o.availableSeats ?? o.AvailableSeats) as number | undefined,
    }
  },

  deleteDivision: async (id: string): Promise<void> => {
    await apiClient.delete(`/Division/${id}`)
  },
}
