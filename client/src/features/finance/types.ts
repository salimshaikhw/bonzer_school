export type AcademicYearRow = {
  id: string
  yearName?: string
  startDate?: string
  endDate?: string
  isActive?: boolean
}

export type ClassFeeDashboardDto = {
  classId: string
  totalFees: number
  collectedFees: number
  pendingFees: number
  totalStudents: number
  collectionRate: number
}

export type DivisionFeeDashboardDto = {
  divisionId: string
  totalFees: number
  collectedFees: number
  pendingFees: number
  totalStudents: number
  collectionRate: number
}

export type BranchFeeDashboardDto = {
  branchId: string
  totalFees: number
  collectedFees: number
  pendingFees: number
  totalStudents: number
  collectionRate: number
}

export type FeeCollectionTrendPoint = { date: string; amount: number }

export type FeeHeadAnalyticsRow = {
  feeHeadId: string
  feeHeadName: string
  totalAmount: number
}

export type InstallmentAnalyticsRow = {
  installmentId: string
  installmentName: string
  expectedAmount: number
  collectedAmount: number
  pendingAmount?: number
  collectionPercent?: number
  branchName?: string
  className?: string
  divisionName?: string
}

export type StudentOutstandingRow = {
  studentId: string
  rollNumber?: string
  classId?: string
  divisionId?: string
  remainingAmount: number
}

export type ClassOutstandingRow = {
  classId: string
  className: string
  remainingAmount: number
}

export type DivisionOutstandingRow = {
  divisionId: string
  divisionName: string
  remainingAmount: number
}

export type BranchOutstandingRow = {
  branchId: string
  branchName: string
  remainingAmount: number
}

export type InvoiceRow = {
  id: string
  studentId?: string
  branchId?: string
  classId?: string
  divisionId?: string
  installmentId?: string
  nextInstallmentDate?: string
  invoiceNo?: string
  invoiceDate?: string
  totalAmount?: number
  paidAmount?: number
  remainingAmount?: number
  dueDate?: string
  status?: string
  month?: number
  year?: number
  createdAt?: string
}

export type InvoiceRequestBody = {
  id: string
  studentId?: string | null
  branchId?: string | null
  classId?: string | null
  divisionId?: string | null
  installmentId?: string | null
  nextInstallmentDate?: string | null
  invoiceNo?: string | null
  invoiceDate?: string | null
  totalAmount?: number | null
  paidAmount?: number | null
  remainingAmount?: number | null
  dueDate?: string | null
  status?: string | null
  month?: number | null
  year?: number | null
  createdAt?: string | null
}

export type PaymentRow = {
  id: string
  invoiceId?: string
  studentId?: string
  branchId?: string
  divisionId?: string
  installmentId?: string
  collectedByStaffId?: string
  paymentDate?: string
  amount?: number
  paymentMethod?: string
  paymentType?: string
  transactionNumber?: string
  bankName?: string
  remarks?: string
  createdAt?: string
}

export type PaymentRequestBody = {
  invoiceId?: string | null
  studentId?: string | null
  branchId?: string | null
  divisionId?: string | null
  installmentId?: string | null
  collectedByStaffId?: string | null
  paymentDate?: string | null
  amount?: number | null
  paymentMethod?: string | null
  paymentType?: string | null
  transactionNumber?: string | null
  bankName?: string | null
  remarks?: string | null
  createdAt?: string | null
  feeLines?: { feeHeadId: string; amount: number }[] | null
}
