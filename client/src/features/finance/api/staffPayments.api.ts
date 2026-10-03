import { apiClient } from '../../../infrastructure/http'
import type { PaymentRequestBody, PaymentRow } from '../types'

function num(v: unknown): number | undefined {
  if (v == null) return undefined
  const n = Number(v)
  return Number.isFinite(n) ? n : undefined
}

function pickStr(...vals: unknown[]): string | undefined {
  for (const v of vals) {
    if (v == null) continue
    const s = String(v).trim()
    if (s) return s
  }
  return undefined
}

function parsePayment(raw: unknown): PaymentRow | null {
  if (raw == null || typeof raw !== 'object') return null
  const o = raw as Record<string, unknown>
  const id = pickStr(o.id, o.Id)
  if (!id) return null
  return {
    id,
    invoiceId: pickStr(o.invoiceId, o.InvoiceId),
    studentId: pickStr(o.studentId, o.StudentId),
    branchId: pickStr(o.branchId, o.BranchId),
    divisionId: pickStr(o.divisionId, o.DivisionId),
    installmentId: pickStr(o.installmentId, o.InstallmentId),
    collectedByStaffId: pickStr(o.collectedByStaffId, o.CollectedByStaffId),
    paymentDate: pickStr(o.paymentDate, o.PaymentDate),
    amount: num(o.amount ?? o.Amount),
    paymentMethod: pickStr(o.paymentMethod, o.PaymentMethod),
    paymentType: pickStr(o.paymentType, o.PaymentType),
    transactionNumber: pickStr(o.transactionNumber, o.TransactionNumber),
    bankName: pickStr(o.bankName, o.BankName),
    remarks: pickStr(o.remarks, o.Remarks),
    createdAt: pickStr(o.createdAt, o.CreatedAt),
  }
}

function unwrapPayload<T>(raw: unknown): T | null {
  if (raw == null) return null
  if (typeof raw === 'object' && raw !== null && 'success' in raw && 'data' in raw) {
    const o = raw as { success?: boolean; data?: T }
    if (o.success && o.data !== undefined) return o.data
    if (o.data !== undefined) return o.data
  }
  return raw as T
}

export const staffPaymentsApi = {
  getAll: async (): Promise<PaymentRow[]> => {
    const { data } = await apiClient.get<unknown>('/Payment')
    const inner = unwrapPayload<unknown>(data)
    const raw = Array.isArray(inner) ? inner : []
    return raw.map(parsePayment).filter((x): x is PaymentRow => x != null)
  },

  getById: async (id: string): Promise<PaymentRow | null> => {
    const { data } = await apiClient.get<unknown>(`/Payment/${id}`)
    const inner = unwrapPayload<unknown>(data)
    return parsePayment(inner)
  },

  create: async (body: PaymentRequestBody): Promise<PaymentRow | null> => {
    const { data } = await apiClient.post<unknown>('/Payment', body)
    const inner = unwrapPayload<unknown>(data)
    return parsePayment(inner)
  },

  /** Download receipt PDF using current auth token. */
  downloadReceiptPdf: async (paymentId: string, disposition: 'inline' | 'attachment' = 'attachment'): Promise<Blob> => {
    const { data } = await apiClient.get<Blob>(`/Payment/${paymentId}/receipt/pdf`, {
      params: { disposition },
      responseType: 'blob',
    })
    return data
  },
}
