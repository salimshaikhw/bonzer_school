import { apiClient } from '../../../infrastructure/http'
import { parseStudentFeeDashboardDto, type StudentFeeDashboardDto } from '../../admission/api/fees.api'

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

export type ParentLinkedStudent = {
  studentId: string
  firstName?: string
  lastName?: string
  admissionNo?: string
  rollNumber?: string
  branchId?: string
  classId?: string
  academicYearId?: string
}

export type OpenInvoiceLine = {
  invoiceItemId: string
  feeHeadId: string
  feeHeadName: string
  totalAmount: number
  paidAmount: number
  remainingAmount: number
}

export type OpenInvoice = {
  id: string
  invoiceNo?: string
  dueDate?: string
  totalAmount: number
  paidAmount: number
  remainingAmount: number
  lines: OpenInvoiceLine[]
}

export type PaymentHistoryRow = {
  paymentId: string
  invoiceId?: string
  paymentDate?: string
  amount?: number
  paymentMethod?: string
  transactionNumber?: string
}

function parseStudent(raw: Record<string, unknown>): ParentLinkedStudent {
  return {
    studentId: pickStr(raw.studentId, raw.StudentId) ?? '',
    firstName: pickStr(raw.firstName, raw.FirstName),
    lastName: pickStr(raw.lastName, raw.LastName),
    admissionNo: pickStr(raw.admissionNo, raw.AdmissionNo),
    rollNumber: pickStr(raw.rollNumber, raw.RollNumber),
    branchId: pickStr(raw.branchId, raw.BranchId),
    classId: pickStr(raw.classId, raw.ClassId),
    academicYearId: pickStr(raw.academicYearId, raw.AcademicYearId),
  }
}

function parseOpenInvoice(raw: Record<string, unknown>): OpenInvoice {
  const linesRaw = (raw.lines ?? raw.Lines) as unknown[] | undefined
  return {
    id: pickStr(raw.id, raw.Id) ?? '',
    invoiceNo: pickStr(raw.invoiceNo, raw.InvoiceNo),
    dueDate: pickStr(raw.dueDate, raw.DueDate),
    totalAmount: num(raw.totalAmount ?? raw.TotalAmount),
    paidAmount: num(raw.paidAmount ?? raw.PaidAmount),
    remainingAmount: num(raw.remainingAmount ?? raw.RemainingAmount),
    lines: Array.isArray(linesRaw)
      ? linesRaw.map((x) => {
          const o = x as Record<string, unknown>
          return {
            invoiceItemId: pickStr(o.invoiceItemId, o.InvoiceItemId) ?? '',
            feeHeadId: pickStr(o.feeHeadId, o.FeeHeadId) ?? '',
            feeHeadName: pickStr(o.feeHeadName, o.FeeHeadName) ?? '—',
            totalAmount: num(o.totalAmount ?? o.TotalAmount),
            paidAmount: num(o.paidAmount ?? o.PaidAmount),
            remainingAmount: num(o.remainingAmount ?? o.RemainingAmount),
          }
        })
      : [],
  }
}

function parsePaymentRow(raw: Record<string, unknown>): PaymentHistoryRow {
  return {
    paymentId: pickStr(raw.paymentId, raw.PaymentId) ?? '',
    invoiceId: pickStr(raw.invoiceId, raw.InvoiceId),
    paymentDate: pickStr(raw.paymentDate, raw.PaymentDate),
    amount: raw.amount != null ? num(raw.amount) : raw.Amount != null ? num(raw.Amount) : undefined,
    paymentMethod: pickStr(raw.paymentMethod, raw.PaymentMethod),
    transactionNumber: pickStr(raw.transactionNumber, raw.TransactionNumber),
  }
}

export const parentFeesApi = {
  getMyStudents: async (): Promise<ParentLinkedStudent[]> => {
    const { data } = await apiClient.get<unknown[]>('/ParentFees/my-students')
    if (!Array.isArray(data)) return []
    return data.map((x) => parseStudent(x as Record<string, unknown>))
  },

  getFeeSummary: async (studentId: string, academicYearId: string): Promise<StudentFeeDashboardDto> => {
    const { data } = await apiClient.get<Record<string, unknown>>(
      `/ParentFees/students/${studentId}/fee-summary`,
      { params: { academicYearId } },
    )
    return parseStudentFeeDashboardDto(data)
  },

  getOpenInvoices: async (studentId: string, academicYearId: string): Promise<OpenInvoice[]> => {
    const { data } = await apiClient.get<unknown[]>(
      `/ParentFees/students/${studentId}/open-invoices`,
      { params: { academicYearId } },
    )
    if (!Array.isArray(data)) return []
    return data.map((x) => parseOpenInvoice(x as Record<string, unknown>))
  },

  getPayments: async (studentId: string, academicYearId: string): Promise<PaymentHistoryRow[]> => {
    const { data } = await apiClient.get<unknown[]>(`/ParentFees/students/${studentId}/payments`, {
      params: { academicYearId },
    })
    if (!Array.isArray(data)) return []
    return data.map((x) => parsePaymentRow(x as Record<string, unknown>))
  },

  pay: async (
    studentId: string,
    body: {
      invoiceId: string
      academicYearId: string
      paymentMethod?: string
      transactionReference?: string
      feeLines: {
        invoiceItemId?: string | null
        feeHeadId?: string | null
        customFeeName?: string | null
        amount: number
      }[]
    },
  ): Promise<{ id?: string }> => {
    const { data } = await apiClient.post<Record<string, unknown>>(
      `/ParentFees/students/${studentId}/pay`,
      {
        invoiceId: body.invoiceId,
        academicYearId: body.academicYearId,
        paymentMethod: body.paymentMethod,
        transactionReference: body.transactionReference,
        feeLines: body.feeLines.map((f) => ({
          invoiceItemId: f.invoiceItemId?.trim() || null,
          feeHeadId: f.feeHeadId?.trim() || null,
          customFeeName: f.customFeeName?.trim() || null,
          amount: f.amount,
        })),
      },
    )
    return data && typeof data === 'object' ? { id: pickStr(data.id, data.Id) } : {}
  },

  /** Creates an open invoice from class fee map when needed, then records payment (staff collect flow). */
  collectPay: async (
    studentId: string,
    body: {
      academicYearId: string
      invoiceId?: string | null
      installmentId?: string | null
      paymentMethod?: string
      transactionReference?: string
      feeLines: {
        invoiceItemId?: string | null
        feeHeadId?: string | null
        customFeeName?: string | null
        amount: number
      }[]
    },
  ): Promise<{ id?: string }> => {
    const { data } = await apiClient.post<Record<string, unknown>>(
      `/ParentFees/students/${studentId}/collect-pay`,
      {
        academicYearId: body.academicYearId,
        invoiceId: body.invoiceId?.trim() || null,
        installmentId: body.installmentId?.trim() || null,
        paymentMethod: body.paymentMethod,
        transactionReference: body.transactionReference,
        feeLines: body.feeLines.map((f) => ({
          invoiceItemId: f.invoiceItemId?.trim() || null,
          feeHeadId: f.feeHeadId?.trim() || null,
          customFeeName: f.customFeeName?.trim() || null,
          amount: f.amount,
        })),
      },
    )
    return data && typeof data === 'object' ? { id: pickStr(data.id, data.Id) } : {}
  },
}
