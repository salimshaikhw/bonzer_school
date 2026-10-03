import { apiClient } from '../../../infrastructure/http'
import type { InvoiceRequestBody, InvoiceRow } from '../types'

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

function parseInvoice(raw: unknown): InvoiceRow | null {
  if (raw == null || typeof raw !== 'object') return null
  const o = raw as Record<string, unknown>
  const id = pickStr(o.id, o.Id)
  if (!id) return null
  return {
    id,
    studentId: pickStr(o.studentId, o.StudentId),
    branchId: pickStr(o.branchId, o.BranchId),
    classId: pickStr(o.classId, o.ClassId),
    divisionId: pickStr(o.divisionId, o.DivisionId),
    installmentId: pickStr(o.installmentId, o.InstallmentId),
    nextInstallmentDate: pickStr(o.nextInstallmentDate, o.NextInstallmentDate),
    invoiceNo: pickStr(o.invoiceNo, o.InvoiceNo),
    invoiceDate: pickStr(o.invoiceDate, o.InvoiceDate),
    totalAmount: num(o.totalAmount ?? o.TotalAmount),
    paidAmount: num(o.paidAmount ?? o.PaidAmount),
    remainingAmount: num(o.remainingAmount ?? o.RemainingAmount),
    dueDate: pickStr(o.dueDate, o.DueDate),
    status: pickStr(o.status, o.Status),
    month: num(o.month ?? o.Month) as number | undefined,
    year: num(o.year ?? o.Year) as number | undefined,
    createdAt: pickStr(o.createdAt, o.CreatedAt),
  }
}

export const invoicesApi = {
  getAll: async (): Promise<InvoiceRow[]> => {
    const { data } = await apiClient.get<unknown>('/Invoice')
    const raw = Array.isArray(data) ? data : []
    return raw.map(parseInvoice).filter((x): x is InvoiceRow => x != null)
  },

  getById: async (id: string): Promise<InvoiceRow | null> => {
    const { data } = await apiClient.get<unknown>(`/Invoice/${id}`)
    return parseInvoice(data)
  },

  create: async (body: InvoiceRequestBody): Promise<InvoiceRow | null> => {
    const { data } = await apiClient.post<unknown>('/Invoice', body)
    return parseInvoice(data)
  },

  update: async (id: string, body: InvoiceRequestBody): Promise<InvoiceRow | null> => {
    const { data } = await apiClient.put<unknown>(`/Invoice/${id}`, body)
    return parseInvoice(data)
  },

  delete: async (id: string): Promise<boolean> => {
    await apiClient.delete(`/Invoice/${id}`)
    return true
  },

  /**
   * Creates an open invoice + lines from the student fee dashboard when they have no open invoice for the year.
   */
  createFromStudentSchedule: async (body: {
    studentId: string
    academicYearId: string
    installmentId?: string | null
  }): Promise<InvoiceRow | null> => {
    const { data } = await apiClient.post<unknown>('/Invoice/from-student-schedule', {
      studentId: body.studentId,
      academicYearId: body.academicYearId,
      ...(body.installmentId?.trim() ? { installmentId: body.installmentId.trim() } : {}),
    })
    return parseInvoice(data)
  },

  getStatement: async (id: string): Promise<InvoiceStatementDto | null> => {
    const { data } = await apiClient.get<unknown>(`/Invoice/${id}/statement`)
    if (data == null || typeof data !== 'object') return null
    const o = data as Record<string, unknown>
    const inv = parseInvoice(o.invoice ?? o.Invoice)
    if (!inv) return null
    const linesRaw = (o.lines ?? o.Lines) as unknown[] | undefined
    const lines: InvoiceStatementLineDto[] = Array.isArray(linesRaw)
      ? linesRaw.map((x) => {
          const r = x as Record<string, unknown>
          return {
            feeHeadId: pickStr(r.feeHeadId, r.FeeHeadId) ?? '',
            feeHeadName: pickStr(r.feeHeadName, r.FeeHeadName) ?? '—',
            totalAmount: num(r.totalAmount ?? r.TotalAmount) ?? 0,
            paidAmount: num(r.paidAmount ?? r.PaidAmount) ?? 0,
            remainingAmount: num(r.remainingAmount ?? r.RemainingAmount) ?? 0,
          }
        })
      : []
    const lpRaw = (o.lastPayment ?? o.LastPayment) as Record<string, unknown> | null | undefined
    let lastPayment: InvoiceLastPaymentDto | null = null
    if (lpRaw && typeof lpRaw === 'object') {
      const flRaw = (lpRaw.feeLines ?? lpRaw.FeeLines) as unknown[] | undefined
      lastPayment = {
        paymentId: pickStr(lpRaw.paymentId, lpRaw.PaymentId) ?? '',
        paymentDate: pickStr(lpRaw.paymentDate, lpRaw.PaymentDate),
        amount: num(lpRaw.amount ?? lpRaw.Amount),
        paymentMethod: pickStr(lpRaw.paymentMethod, lpRaw.PaymentMethod),
        transactionNumber: pickStr(lpRaw.transactionNumber, lpRaw.TransactionNumber),
        feeLines: Array.isArray(flRaw)
          ? flRaw.map((z) => {
              const f = z as Record<string, unknown>
              return {
                particulars: pickStr(f.particulars, f.Particulars) ?? '—',
                amount: num(f.amount ?? f.Amount) ?? 0,
              }
            })
          : [],
      }
    }
    return { invoice: inv, lines, lastPayment }
  },
}

export type InvoiceStatementLineDto = {
  feeHeadId: string
  feeHeadName: string
  totalAmount: number
  paidAmount: number
  remainingAmount: number
}

export type InvoiceLastPaymentDto = {
  paymentId: string
  paymentDate?: string
  amount?: number
  paymentMethod?: string
  transactionNumber?: string
  feeLines: { particulars: string; amount: number }[]
}

export type InvoiceStatementDto = {
  invoice: InvoiceRow
  lines: InvoiceStatementLineDto[]
  lastPayment: InvoiceLastPaymentDto | null
}
