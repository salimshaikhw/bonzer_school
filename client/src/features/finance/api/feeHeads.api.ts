import { apiClient } from '../../../infrastructure/http'

export type FeeHeadRow = { id: string; name?: string; accountCode?: string }

function pickStr(...vals: unknown[]): string | undefined {
  for (const v of vals) {
    if (v == null) continue
    const s = String(v).trim()
    if (s) return s
  }
  return undefined
}

export const feeHeadsApi = {
  getAll: async (): Promise<FeeHeadRow[]> => {
    const { data } = await apiClient.get<unknown>('/FeeHead')
    const raw = Array.isArray(data) ? data : []
    return raw
      .map((x) => {
        const o = x as Record<string, unknown>
        const id = pickStr(o.id, o.Id)
        if (!id) return null
        return {
          id,
          name: pickStr(o.name, o.Name),
          accountCode: pickStr(o.accountCode, o.AccountCode),
        } as FeeHeadRow
      })
      .filter((x): x is FeeHeadRow => x != null)
  },
}
