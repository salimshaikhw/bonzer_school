import { apiClient } from '../../../infrastructure/http'
import type { Branch } from '../branches.types'

function mapBranch(raw: unknown): Branch | null {
  if (!raw || typeof raw !== 'object') return null
  const r = raw as Record<string, unknown>
  const id = String(r.id ?? r.Id ?? '')
  if (!id) return null
  return {
    id,
    groupId: (r.groupId ?? r.GroupId ?? null) as string | null,
    name: (r.name ?? r.Name) as string | undefined,
    city: (r.city ?? r.City) as string | undefined,
    state: (r.state ?? r.State) as string | undefined,
    phone: (r.phone ?? r.Phone) as string | undefined,
    email: (r.email ?? r.Email) as string | undefined,
  }
}

export type BranchUpsert = {
  groupId?: string | null
  name?: string
  address?: string
  city?: string
  state?: string
  phone?: string
  email?: string
}

export const branchesApi = {
  getAll: async (): Promise<Branch[]> => {
    const { data } = await apiClient.get<unknown>('/Branch')
    const list = Array.isArray(data) ? data : []
    return list.map(mapBranch).filter((b): b is Branch => !!b)
  },

  create: async (payload: BranchUpsert): Promise<Branch> => {
    const body = {
      groupId: payload.groupId || null,
      name: payload.name?.trim() || null,
      address: payload.address?.trim() || null,
      city: payload.city?.trim() || null,
      state: payload.state?.trim() || null,
      phone: payload.phone?.trim() || null,
      email: payload.email?.trim() || null,
    }
    const { data } = await apiClient.post<unknown>('/Branch', body)
    const mapped = mapBranch(data)
    if (!mapped) throw new Error('Invalid branch response from server.')
    return mapped
  },

  update: async (id: string, payload: BranchUpsert): Promise<Branch> => {
    const body = {
      groupId: payload.groupId || null,
      name: payload.name?.trim() || null,
      address: payload.address?.trim() || null,
      city: payload.city?.trim() || null,
      state: payload.state?.trim() || null,
      phone: payload.phone?.trim() || null,
      email: payload.email?.trim() || null,
    }
    const { data } = await apiClient.put<unknown>(`/Branch/${id}`, body)
    const mapped = mapBranch(data)
    if (!mapped) throw new Error('Invalid branch response from server.')
    return mapped
  },

  remove: async (id: string): Promise<void> => {
    await apiClient.delete(`/Branch/${id}`)
  },
}
