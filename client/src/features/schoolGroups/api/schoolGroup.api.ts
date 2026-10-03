import { apiClient } from '../../../infrastructure/http'
import type { SchoolGroup } from '../schoolGroup.types'

function mapGroup(raw: unknown): SchoolGroup | null {
  if (!raw || typeof raw !== 'object') return null
  const r = raw as Record<string, unknown>
  const id = String(r.id ?? r.Id ?? '')
  if (!id) return null
  return {
    id,
    name: (r.name ?? r.Name) as string | undefined,
    code: (r.code ?? r.Code) as string | undefined,
  }
}

export type SchoolGroupUpsert = {
  name: string
  code: string
  address?: string
  phone?: string
  email?: string
}

export const schoolGroupApi = {
  getAll: async (): Promise<SchoolGroup[]> => {
    const { data } = await apiClient.get<unknown>('/SchoolGroup')
    const list = Array.isArray(data) ? data : []
    return list.map(mapGroup).filter((g): g is SchoolGroup => !!g)
  },

  create: async (payload: SchoolGroupUpsert): Promise<SchoolGroup> => {
    const body = {
      id: crypto.randomUUID(),
      name: payload.name,
      code: payload.code,
      address: payload.address?.trim() || null,
      phone: payload.phone?.trim() || null,
      email: payload.email?.trim() || null,
    }
    const { data } = await apiClient.post<unknown>('/SchoolGroup', body)
    const mapped = mapGroup(data)
    if (!mapped) throw new Error('Invalid school group response from server.')
    return mapped
  },

  update: async (id: string, payload: SchoolGroupUpsert): Promise<SchoolGroup> => {
    const body = {
      name: payload.name,
      code: payload.code,
      address: payload.address?.trim() || null,
      phone: payload.phone?.trim() || null,
      email: payload.email?.trim() || null,
    }
    const { data } = await apiClient.put<unknown>(`/SchoolGroup/${id}`, body)
    const mapped = mapGroup(data)
    if (!mapped) throw new Error('Invalid school group response from server.')
    return mapped
  },

  remove: async (id: string): Promise<void> => {
    await apiClient.delete(`/SchoolGroup/${id}`)
  },
}
