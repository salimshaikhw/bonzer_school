import type { CSSProperties, FormEvent } from 'react'
import { useCallback, useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { ROUTES } from '../../../config/routes'
import { useAuth } from '../../../core'
import { AdmissionOrgScopeBanner } from '../../admission/components/AdmissionOrgScopeBanner'
import {
  normalizeStudentFromApi,
  normalizeStudentFullFromApi,
  studentsApi,
} from '../../admission/api/students.api'
import type { Student } from '../../admission/students.types'
import { useOrgHierarchy } from '../../../layouts/OrgHierarchyProvider'
import { Button, Spinner } from '../../../shared'
import { academicYearsApi } from '../api/academicYears.api'
import { adminFeesApi } from '../api/adminFees.api'
import { staffPaymentsApi } from '../api/staffPayments.api'
import { invoicesApi } from '../api/invoices.api'
import { parentFeesApi, type OpenInvoice, type OpenInvoiceLine } from '../../fees/api/parentFees.api'
import type { StudentFeeDashboardDto } from '../../admission/api/fees.api'
import type { InstallmentAnalyticsRow } from '../types'
import { financeTheme as t } from '../ui/financeTheme'

/** Indian Rupee display (matches fee payment mockups). */
function money(n: number): string {
  return n.toLocaleString(undefined, { minimumFractionDigits: 0, maximumFractionDigits: 2 })
}

function rupee(n: number): string {
  return `₹ ${money(n)}`
}

function clampPay(raw: string, max: number): number {
  const n = Number(String(raw).replace(/[^0-9.]/g, ''))
  if (!Number.isFinite(n) || n < 0) return 0
  return Math.min(n, max)
}

/** Integer rupees to words (sufficient for school fees slips). */
function rupeesInWords(n: number): string {
  const num = Math.floor(Math.max(0, n))
  if (num === 0) return 'zero'
  const ones = [
    '',
    'one',
    'two',
    'three',
    'four',
    'five',
    'six',
    'seven',
    'eight',
    'nine',
    'ten',
    'eleven',
    'twelve',
    'thirteen',
    'fourteen',
    'fifteen',
    'sixteen',
    'seventeen',
    'eighteen',
    'nineteen',
  ]
  const tens = ['', '', 'twenty', 'thirty', 'forty', 'fifty', 'sixty', 'seventy', 'eighty', 'ninety']
  function chunkToWords(x: number): string {
    if (x < 20) return ones[x]
    if (x < 100) {
      const t = Math.floor(x / 10)
      const o = x % 10
      return tens[t] + (o ? ` ${ones[o]}` : '')
    }
    if (x < 1000) {
      const h = Math.floor(x / 100)
      const r = x % 100
      return `${ones[h]} hundred${r ? ` ${chunkToWords(r)}` : ''}`.trim()
    }
    return String(x)
  }
  let rest = num
  const parts: string[] = []
  const cr = Math.floor(rest / 10000000)
  if (cr) {
    parts.push(`${chunkToWords(cr)} crore`)
    rest %= 10000000
  }
  const lk = Math.floor(rest / 100000)
  if (lk) {
    parts.push(`${chunkToWords(lk)} lakh`)
    rest %= 100000
  }
  const th = Math.floor(rest / 1000)
  if (th) {
    parts.push(`${chunkToWords(th)} thousand`)
    rest %= 1000
  }
  if (rest > 0) parts.push(chunkToWords(rest))
  return parts.join(' ').replace(/\s+/g, ' ').trim()
}

function totalPayWords(n: number): string {
  const r = Math.round(Math.max(0, n))
  if (r === 0) return 'Zero Rupees Only'
  return `${rupeesInWords(r).replace(/\b\w/g, (c) => c.toUpperCase())} Rupees Only`
}

function studentName(s: Student): string {
  return [s.firstName, s.middleName, s.lastName].filter(Boolean).join(' ').trim() || '—'
}

/** Roster option: roll + full name (fees collection picker). */
function rosterStudentLabel(s: Student): string {
  const roll = (s.rollNumber ?? '').trim()
  const nm = studentName(s)
  return roll ? `Roll ${roll} — ${nm}` : nm
}

function normEq(a: string | undefined, b: string | undefined): boolean {
  return String(a ?? '')
    .trim()
    .toLowerCase() === String(b ?? '').trim().toLowerCase()
}

function apiErrMessage(e: unknown): string {
  if (e && typeof e === 'object' && 'response' in e) {
    const d = (e as { response?: { data?: { message?: string } } }).response?.data?.message
    if (d) return String(d)
  }
  if (e instanceof Error) return e.message
  return 'Request failed.'
}

export function FeesCollectionPage() {
  const { appPersona } = useAuth()
  const org = useOrgHierarchy()
  const branchId = org.branchId.trim()

  const [years, setYears] = useState<{ id: string; label: string }[]>([])
  const [academicYearId, setAcademicYearId] = useState('')

  const [searchQ, setSearchQ] = useState('')
  const [searching, setSearching] = useState(false)
  const [candidates, setCandidates] = useState<Student[]>([])
  const [student, setStudent] = useState<Student | null>(null)

  const divisionId = org.divisionId.trim()
  const treeClassId = org.classId.trim()
  const [divisionRoster, setDivisionRoster] = useState<Student[]>([])
  const [rosterLoading, setRosterLoading] = useState(false)
  const [rosterFilter, setRosterFilter] = useState('')
  const [rosterSelectId, setRosterSelectId] = useState('')

  const [invoices, setInvoices] = useState<OpenInvoice[]>([])
  const [invoiceId, setInvoiceId] = useState('')
  /** Split amounts per fee head (accountant allocates one payment across heads on this invoice). */
  const [payByFeeHead, setPayByFeeHead] = useState<Record<string, string>>({})
  const [loadInv, setLoadInv] = useState(false)

  const [feeDash, setFeeDash] = useState<StudentFeeDashboardDto | null>(null)
  const [scheduleInstallmentId, setScheduleInstallmentId] = useState('')
  const [creatingSchedule, setCreatingSchedule] = useState(false)

  const [installmentRows, setInstallmentRows] = useState<InstallmentAnalyticsRow[]>([])
  const [studentInstalls, setStudentInstalls] = useState<
    {
      installmentId: string
      installmentName: string
      totalAmount: number
      paidAmount: number
      remainingAmount: number
    }[]
  >([])

  const [paymentMethod, setPaymentMethod] = useState('Cash')
  const [txnRef, setTxnRef] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [msg, setMsg] = useState<string | null>(null)
  const [err, setErr] = useState<string | null>(null)
  const [lastPaymentId, setLastPaymentId] = useState<string | null>(null)

  const branch = useMemo(
    () => org.branches.find((b) => b.id === branchId),
    [org.branches, branchId],
  )
  const collectionScopeLabel = useMemo(() => {
    const parts: string[] = []
    if (branch?.name?.trim()) parts.push(branch.name.trim())
    if (treeClassId) {
      const c =
        org.treeClasses.find((x) => x.id === treeClassId) ?? org.classes.find((x) => x.id === treeClassId)
      if (c?.name?.trim()) parts.push(c.name.trim())
    }
    if (divisionId) {
      const d =
        org.treeDivisions.find((x) => x.id === divisionId) ??
        org.divisions.find((x) => x.id === divisionId)
      if (d?.name?.trim()) parts.push(d.name.trim())
    }
    return parts.length ? parts.join(' · ') : 'this school'
  }, [branch, treeClassId, divisionId, org.treeClasses, org.classes, org.treeDivisions, org.divisions])

  const classLabelFromTree = useMemo(() => {
    if (!treeClassId) return '—'
    const c =
      org.treeClasses.find((x) => x.id === treeClassId) ?? org.classes.find((x) => x.id === treeClassId)
    return c?.name?.trim() || '—'
  }, [treeClassId, org.treeClasses, org.classes])
  const className = useMemo(() => {
    if (!student?.classId) return '—'
    const c = org.treeClasses.find((x) => x.id === student.classId) ?? org.classes.find((x) => x.id === student.classId)
    return c?.name ?? student.classId.slice(0, 8)
  }, [student, org.treeClasses, org.classes])
  const divName = useMemo(() => {
    if (!student?.divisionId) return '—'
    const d =
      org.treeDivisions.find((x) => x.id === student.divisionId) ?? org.divisions.find((x) => x.id === student.divisionId)
    return d?.name ?? student.divisionId.slice(0, 8)
  }, [student, org.treeDivisions, org.divisions])

  useEffect(() => {
    let c = false
    void academicYearsApi.getAll().then((list) => {
      if (c) return
      const opts = list.map((y) => ({
        id: y.id,
        label: y.yearName?.trim() ? String(y.yearName) : y.id,
      }))
      setYears(opts)
      const active = list.find((y) => y.isActive === true)
      setAcademicYearId((prev) => {
        if (prev.trim()) return prev
        if (active?.id) return active.id
        return opts[0]?.id ?? ''
      })
    })
    return () => {
      c = true
    }
  }, [])

  useEffect(() => {
    if (!branchId || !academicYearId.trim()) {
      setInstallmentRows([])
      return
    }
    let c = false
    void adminFeesApi
      .getInstallmentAnalytics(branchId, academicYearId.trim(), {
        classId: treeClassId || undefined,
        divisionId: divisionId || undefined,
      })
      .then((rows) => {
        if (!c) setInstallmentRows(rows)
      })
      .catch(() => {
        if (!c) setInstallmentRows([])
      })
    return () => {
      c = true
    }
  }, [branchId, academicYearId, treeClassId, divisionId])

  useEffect(() => {
    if (!divisionId) {
      setDivisionRoster([])
      setRosterLoading(false)
      return
    }
    let cancelled = false
    setRosterLoading(true)
    void studentsApi
      .getStudentsForDivisionManualMark(divisionId, treeClassId)
      .then((list) => {
        if (cancelled) return
        const scoped = branchId ? list.filter((s) => !s.branchId || normEq(s.branchId, branchId)) : list
        scoped.sort((a, b) => {
          const ra = a.rollNumber?.trim() ?? ''
          const rb = b.rollNumber?.trim() ?? ''
          const na = Number(ra)
          const nb = Number(rb)
          if (Number.isFinite(na) && Number.isFinite(nb) && String(na) === ra && String(nb) === rb) {
            return na - nb
          }
          const byRoll = ra.localeCompare(rb, undefined, { numeric: true })
          if (byRoll !== 0) return byRoll
          return studentName(a).localeCompare(studentName(b))
        })
        setDivisionRoster(scoped)
      })
      .catch(() => {
        if (!cancelled) setDivisionRoster([])
      })
      .finally(() => {
        if (!cancelled) setRosterLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [branchId, divisionId, treeClassId])

  useEffect(() => {
    if (!divisionId) return
    setStudent(null)
    setCandidates([])
    setRosterSelectId('')
    setRosterFilter('')
    setLastPaymentId(null)
    setMsg(null)
  }, [divisionId, treeClassId])

  const rosterFiltered = useMemo(() => {
    const q = rosterFilter.trim().toLowerCase()
    if (!q) return divisionRoster
    return divisionRoster.filter((s) => {
      const roll = (s.rollNumber ?? '').toLowerCase()
      const nm = studentName(s).toLowerCase()
      return roll.includes(q) || nm.includes(q)
    })
  }, [divisionRoster, rosterFilter])

  const selectedInvoice = useMemo(
    () => invoices.find((i) => i.id === invoiceId) ?? null,
    [invoices, invoiceId],
  )

  /**
   * One row per fee head for the student’s structure (class / year dashboard), merged with
   * lines on the selected open invoice so staff can split one payment across tuition, uniform, etc.
   */
  const collectionRows = useMemo(() => {
    type Row = {
      feeHeadId: string
      feeHeadName: string
      invLines: OpenInvoiceLine[]
      displayTotal: number
      displayPaid: number
      displayRemaining: number
      /** Max allocatable on the selected invoice for this head (0 if not on this invoice). */
      maxPay: number
    }

    const byHead = new Map<string, Row>()

    for (const h of feeDash?.feeHeads ?? []) {
      if (!h.feeHeadId?.trim()) continue
      byHead.set(h.feeHeadId, {
        feeHeadId: h.feeHeadId,
        feeHeadName: h.feeHeadName?.trim() || '—',
        invLines: [],
        displayTotal: h.totalAmount,
        displayPaid: h.paidAmount,
        displayRemaining: h.remainingAmount,
        maxPay: 0,
      })
    }

    for (const line of selectedInvoice?.lines ?? []) {
      const id = line.feeHeadId?.trim()
      if (!id) continue
      let row = byHead.get(id)
      if (!row) {
        row = {
          feeHeadId: id,
          feeHeadName: line.feeHeadName?.trim() || '—',
          invLines: [],
          displayTotal: 0,
          displayPaid: 0,
          displayRemaining: 0,
          maxPay: 0,
        }
        byHead.set(id, row)
      }
      row.invLines.push(line)
      if (line.feeHeadName?.trim()) row.feeHeadName = line.feeHeadName.trim()
    }

    const rows: Row[] = []
    for (const r of byHead.values()) {
      if (r.invLines.length > 0) {
        const invTotal = r.invLines.reduce((s, l) => s + l.totalAmount, 0)
        const invPaid = r.invLines.reduce((s, l) => s + l.paidAmount, 0)
        const invRem = r.invLines.reduce((s, l) => s + l.remainingAmount, 0)
        r.displayTotal = invTotal
        r.displayPaid = invPaid
        r.displayRemaining = invRem
        r.maxPay = invRem
      } else {
        const due =
          r.displayRemaining > 0.009
            ? r.displayRemaining
            : Math.max(0, r.displayTotal - r.displayPaid)
        r.maxPay = due
      }
      rows.push(r)
    }

    rows.sort((a, b) => a.feeHeadName.localeCompare(b.feeHeadName, undefined, { sensitivity: 'base' }))
    return rows
  }, [feeDash, selectedInvoice])

  const splitLineCalcs = useMemo(
    () =>
      collectionRows.map((row) => {
        const paying = clampPay(payByFeeHead[row.feeHeadId] ?? '', row.maxPay)
        return { row, paying }
      }),
    [collectionRows, payByFeeHead],
  )

  const totalPaying = useMemo(() => splitLineCalcs.reduce((s, x) => s + x.paying, 0), [splitLineCalcs])

  const isCashPayment = paymentMethod === 'Cash'
  const txnFieldMeta = useMemo(() => {
    switch (paymentMethod) {
      case 'UPI':
        return { label: 'UPI ref / UTR', placeholder: 'UPI transaction ID or UTR' }
      case 'Card':
        return { label: 'Card / auth ref', placeholder: 'Auth code, RRN, or last 4 + ref' }
      case 'Bank':
        return { label: 'Bank transfer ref', placeholder: 'NEFT / IMPS / RTGS reference' }
      default:
        return { label: '', placeholder: '' }
    }
  }, [paymentMethod])

  /** Totals for fee heads that exist on the selected invoice (excludes structure-only rows not on this bill). */
  const invoiceGridTotals = useMemo(() => {
    const onInv = collectionRows.filter((r) => r.invLines.length > 0)
    if (onInv.length > 0) {
      return {
        sumLineTotal: onInv.reduce((s, r) => s + r.displayTotal, 0),
        sumPaidBefore: onInv.reduce((s, r) => s + r.displayPaid, 0),
        sumRemainingBefore: onInv.reduce((s, r) => s + r.displayRemaining, 0),
        sumPaying: totalPaying,
      }
    }
    const lines = selectedInvoice?.lines ?? []
    if (lines.length > 0) {
      return {
        sumLineTotal: lines.reduce((s, l) => s + l.totalAmount, 0),
        sumPaidBefore: lines.reduce((s, l) => s + l.paidAmount, 0),
        sumRemainingBefore: lines.reduce((s, l) => s + l.remainingAmount, 0),
        sumPaying: totalPaying,
      }
    }
    if (collectionRows.length > 0) {
      return {
        sumLineTotal: collectionRows.reduce((s, r) => s + r.displayTotal, 0),
        sumPaidBefore: collectionRows.reduce((s, r) => s + r.displayPaid, 0),
        sumRemainingBefore: collectionRows.reduce((s, r) => s + r.displayRemaining, 0),
        sumPaying: totalPaying,
      }
    }
    return null
  }, [collectionRows, totalPaying, selectedInvoice])

  const scheduleOutstandingHeads = useMemo(
    () => (feeDash?.feeHeads ?? []).filter((h) => h.remainingAmount > 0.009),
    [feeDash],
  )

  const scheduleTotals = useMemo(() => {
    const rows = scheduleOutstandingHeads
    if (!rows.length) return null
    return {
      sumTotal: rows.reduce((s, h) => s + h.totalAmount, 0),
      sumRemaining: rows.reduce((s, h) => s + h.remainingAmount, 0),
    }
  }, [scheduleOutstandingHeads])

  const loadStudentFees = useCallback(
    async (s: Student) => {
      const ay = academicYearId.trim() || s.academicYearId?.trim()
      if (!ay) {
        setInvoices([])
        setInvoiceId('')
        setFeeDash(null)
        setStudentInstalls([])
        return
      }
      setLoadInv(true)
      setErr(null)
      try {
        const [open, summary] = await Promise.all([
          parentFeesApi.getOpenInvoices(s.id, ay),
          parentFeesApi.getFeeSummary(s.id, ay).catch(() => null),
        ])
        setInvoices(open)
        setInvoiceId(open[0]?.id ?? '')
        setPayByFeeHead({})
        setFeeDash(summary)
        setStudentInstalls(summary?.installments ?? [])
        const inst = summary?.installments?.find((x) => x.remainingAmount > 0.009)
        setScheduleInstallmentId(inst?.installmentId ?? '')
      } catch {
        setErr('Could not load open invoices for this student and academic year.')
        setInvoices([])
        setInvoiceId('')
        setFeeDash(null)
        setStudentInstalls([])
      } finally {
        setLoadInv(false)
      }
    },
    [academicYearId],
  )

  const onCreateInvoiceFromSchedule = async () => {
    if (!student || !academicYearId.trim()) return
    setMsg(null)
    setErr(null)
    setCreatingSchedule(true)
    try {
      await invoicesApi.createFromStudentSchedule({
        studentId: student.id,
        academicYearId: academicYearId.trim(),
        installmentId: scheduleInstallmentId.trim() || null,
      })
      setMsg('Open invoice created from the fee schedule. You can collect payment below.')
      await loadStudentFees(student)
    } catch (e) {
      setErr(apiErrMessage(e))
    } finally {
      setCreatingSchedule(false)
    }
  }

  useEffect(() => {
    if (!student || !academicYearId.trim()) return
    void loadStudentFees(student)
  }, [student, academicYearId, loadStudentFees])

  const onSearch = async () => {
    setMsg(null)
    setErr(null)
    setCandidates([])
    setStudent(null)
    setLastPaymentId(null)
    if (!branchId) {
      setErr('Select a school in the organization tree first.')
      return
    }
    setSearching(true)
    try {
      const rows = await studentsApi.findForFeesCollection(branchId, searchQ)
      if (rows.length === 0) {
        setErr('No student found for this roll number / Id in this school.')
      } else if (rows.length === 1) {
        const raw = await studentsApi.getById(rows[0].id)
        const parsed = normalizeStudentFullFromApi(raw) ?? normalizeStudentFromApi(raw)
        setStudent((parsed ?? raw) as Student)
      } else {
        setCandidates(rows)
      }
    } catch {
      setErr('Search failed. Check you are signed in and the API is running.')
    } finally {
      setSearching(false)
    }
  }

  const pickCandidate = async (s: Student) => {
    setCandidates([])
    try {
      const full = await studentsApi.getById(s.id)
      const parsed = normalizeStudentFullFromApi(full) ?? normalizeStudentFromApi(full)
      setStudent((parsed ?? full) as Student)
    } catch {
      setStudent(s)
    }
  }

  const onRosterStudentChange = async (id: string) => {
    setRosterSelectId(id)
    setMsg(null)
    setErr(null)
    setLastPaymentId(null)
    setCandidates([])
    if (!id.trim()) {
      setStudent(null)
      return
    }
    try {
      const raw = await studentsApi.getById(id.trim())
      const parsed = normalizeStudentFullFromApi(raw) ?? normalizeStudentFromApi(raw)
      setStudent((parsed ?? raw) as Student)
    } catch {
      setErr('Could not load student details.')
      setStudent(null)
    }
  }

  const onErpSearch = () => {
    setMsg(null)
    setErr(null)
    if (!divisionId) {
      void onSearch()
      return
    }
    if (!rosterSelectId.trim()) {
      setErr('Choose a student from the list, then tap Search to load fee lines.')
      return
    }
    if (student?.id === rosterSelectId.trim()) {
      void loadStudentFees(student)
      return
    }
    void onRosterStudentChange(rosterSelectId.trim())
  }

  const onPay = async (e: FormEvent) => {
    e.preventDefault()
    setMsg(null)
    setErr(null)
    setLastPaymentId(null)
    if (!branchId || !student) {
      setErr('Select a school and student.')
      return
    }
    const ay = academicYearId.trim()
    if (!ay) {
      setErr('Select an academic year.')
      return
    }
    if (totalPaying <= 0) {
      setErr('Enter an amount for at least one fee head.')
      return
    }
    const feeLines: {
      invoiceItemId?: string | null
      feeHeadId?: string | null
      amount: number
    }[] = []
    for (const { row, paying } of splitLineCalcs) {
      if (paying <= 0) continue
      const inv = row.invLines
      if (inv.length === 0) {
        feeLines.push({ feeHeadId: row.feeHeadId, amount: paying })
        continue
      }
      if (inv.length === 1) {
        const itemId = inv[0].invoiceItemId?.trim()
        if (!itemId) {
          setErr('Invoice line is missing an id. Reload open invoices and try again.')
          return
        }
        feeLines.push({ invoiceItemId: itemId, feeHeadId: row.feeHeadId, amount: paying })
      } else {
        feeLines.push({ feeHeadId: row.feeHeadId, amount: paying })
      }
    }
    if (feeLines.length === 0) {
      setErr('No allocatable fee lines.')
      return
    }
    setSubmitting(true)
    try {
      const row = await parentFeesApi.collectPay(student.id, {
        academicYearId: ay,
        invoiceId: selectedInvoice?.id?.trim() || null,
        installmentId: scheduleInstallmentId.trim() || null,
        paymentMethod: paymentMethod.trim() || undefined,
        transactionReference: isCashPayment ? undefined : txnRef.trim() || undefined,
        feeLines,
      })
      if (row?.id) {
        setLastPaymentId(row.id)
        setMsg('Payment recorded and invoice updated. You can download the receipt PDF below.')
        setPayByFeeHead({})
        if (!isCashPayment) setTxnRef('')
        await loadStudentFees(student)
      } else {
        setErr('Payment did not return an Id.')
      }
    } catch (er) {
      setErr(apiErrMessage(er))
    } finally {
      setSubmitting(false)
    }
  }

  const downloadPdf = async () => {
    if (!lastPaymentId) return
    try {
      const blob = await staffPaymentsApi.downloadReceiptPdf(lastPaymentId, 'inline')
      const url = URL.createObjectURL(blob)
      window.open(url, '_blank', 'noopener,noreferrer')
      setTimeout(() => URL.revokeObjectURL(url), 60_000)
    } catch {
      setErr('Could not open receipt PDF.')
    }
  }

  return (
    <div style={styles.pageShell}>
      <AdmissionOrgScopeBanner />

      <div style={styles.cardMain}>
        <nav style={styles.breadcrumbSimple} aria-label="Breadcrumb">
          <Link to={ROUTES.HOME} style={styles.linkBlue}>
            Dashboard
          </Link>
          <span style={styles.breadcrumbSep}>/</span>
          <Link to={ROUTES.FINANCE_COLLECT} style={styles.linkBlue}>
            Finance
          </Link>
          <span style={styles.breadcrumbSep}>/</span>
          <span style={styles.breadcrumbCurrent}>Collect fees</span>
        </nav>

        <h1 style={styles.h1Card}>Fees Payment</h1>
        {collectionScopeLabel !== 'this school' ? (
          <p style={styles.scopeTiny}>{collectionScopeLabel}</p>
        ) : null}

        {appPersona === 'parent' ? (
          <div style={styles.parentNotice} role="status">
            <strong>Parent accounts</strong> pay from{' '}
            <Link to={ROUTES.PARENT_FEES} style={styles.linkBlue}>
              Pay fees (parent portal)
            </Link>
            . This page is for staff collecting fees in school.
          </div>
        ) : null}

        <div style={styles.helpPanel}>
          <div style={styles.helpPanelTitle}>See this full fee table</div>
          <ol style={styles.helpList}>
            <li>
              In the <strong>left tree</strong>, select <strong>school → class → division</strong> (required for the
              student list).
            </li>
            <li>
              Open <strong>Finance</strong> in the module tray, then the <strong>Collect fees</strong> tab — or paste{' '}
              <code style={styles.codeInline}>/finance/collect</code> in the address bar after the site host (e.g.{' '}
              <code style={styles.codeInline}>localhost:3000/finance/collect</code>).
            </li>
            <li>
              Choose <strong>Academic year</strong> and <strong>Student (roll — name)</strong> — the fee table loads
              automatically. Use <strong>Search</strong> to refresh. If there is no open invoice yet, you will still see
              all fee heads; create an invoice below to enable the <strong>Amount</strong> column and <strong>Pay</strong>.
            </li>
          </ol>
          <p style={styles.helpFoot}>
            <a href="/fees-collect-ui-reference.png" target="_blank" rel="noopener noreferrer" style={styles.linkBlue}>
              Open sample layout image (reference)
            </a>{' '}
            — compare with your browser window.
          </p>
        </div>

        <p style={styles.linksRow}>
          <Link to={ROUTES.FINANCE_PAYMENTS} style={styles.linkBlue}>
            Payments
          </Link>
          {' · '}
          <Link to={ROUTES.FINANCE_MASTERS_CLASS_FEE_MAP} style={styles.linkBlue}>
            Class fee map
          </Link>
        </p>

        {!branchId ? (
          <p style={styles.warn}>Choose a school in the left tree — collection is scoped to that branch.</p>
        ) : null}
        {!divisionId && branchId ? (
          <p style={styles.warn}>
            Select a <strong>division</strong> (and class) in the tree to load the student dropdown for this screen.
          </p>
        ) : null}

        <label style={styles.fieldBlockFull}>
          <span style={styles.fieldLab}>Academic year</span>
          <select
            style={styles.fieldControl}
            value={academicYearId}
            onChange={(e) => setAcademicYearId(e.target.value)}
            disabled={years.length === 0}
          >
            {years.map((y) => (
              <option key={y.id} value={y.id}>
                {y.label}
              </option>
            ))}
          </select>
        </label>

        <div style={styles.grid3}>
          <label style={styles.fieldBlock}>
            <span style={styles.fieldLab}>Class</span>
            <div
              style={styles.fieldControlStatic}
              title={divisionId ? 'From the organization tree' : 'Select class and division in the left tree'}
            >
              {divisionId ? classLabelFromTree : '— Use tree —'}
            </div>
          </label>
          {divisionId ? (
            <label style={{ ...styles.fieldBlock, gridColumn: 'span 2' }}>
              <span style={styles.fieldLab}>Student (roll — name)</span>
              <select
                style={styles.fieldControl}
                value={rosterSelectId}
                onChange={(e) => void onRosterStudentChange(e.target.value)}
                disabled={rosterLoading || !branchId}
              >
                <option value="">{rosterLoading ? 'Loading…' : '— Select student —'}</option>
                {rosterFiltered.map((s) => (
                  <option key={s.id} value={s.id}>
                    {rosterStudentLabel(s)}
                  </option>
                ))}
              </select>
            </label>
          ) : (
            <label style={{ ...styles.fieldBlock, gridColumn: 'span 2' }}>
              <span style={styles.fieldLab}>Roll no. or student Id</span>
              <input
                style={styles.fieldControl}
                value={searchQ}
                onChange={(e) => setSearchQ(e.target.value)}
                placeholder="e.g. 12 or student GUID"
                onKeyDown={(e) => e.key === 'Enter' && void onErpSearch()}
                autoComplete="off"
              />
            </label>
          )}
        </div>

        {divisionId ? (
          <label style={{ ...styles.fieldBlockFull, marginBottom: 12 }}>
            <span style={styles.fieldLabMuted}>Narrow roll / name list (optional)</span>
            <input
              style={styles.fieldControl}
              value={rosterFilter}
              onChange={(e) => setRosterFilter(e.target.value)}
              placeholder="Filter dropdown options"
              autoComplete="off"
            />
          </label>
        ) : null}

        <div style={styles.toolbarActions}>
          <Link to={ROUTES.FINANCE_MASTERS_CLASS_FEE_MAP} style={styles.addFeesGreen}>
            + Add Fees
          </Link>
          <button
            type="button"
            style={styles.btnSearch}
            onClick={() => void onErpSearch()}
            disabled={!branchId || (!divisionId && searching)}
          >
            {searching && !divisionId ? 'Searching…' : 'Search'}
          </button>
        </div>

      {divisionId && branchId && !rosterLoading && divisionRoster.length === 0 ? (
        <p style={styles.muted}>No students found for this division in the API.</p>
      ) : null}

      {err ? <p style={styles.err}>{err}</p> : null}
      {msg ? <p style={styles.ok}>{msg}</p> : null}

      {candidates.length > 1 ? (
        <div style={styles.card}>
          <strong>Multiple matches — pick one</strong>
          <ul style={styles.pickList}>
            {candidates.map((c) => (
              <li key={c.id}>
                <button type="button" style={styles.pickBtn} onClick={() => void pickCandidate(c)}>
                  {studentName(c)} — roll {c.rollNumber ?? '—'} — {c.admissionNo ?? c.id.slice(0, 8)}
                </button>
              </li>
            ))}
          </ul>
        </div>
      ) : null}

        {student ? (
          <>
            <div style={styles.divider} />
            <p style={styles.schoolLine}>
              <strong>{branch?.name ?? 'School'}</strong>
              {branch?.city ? ` · ${[branch.city, branch?.state].filter(Boolean).join(', ')}` : null}
            </p>
            <div style={styles.studentSummary}>
              <span>
                <strong>Student:</strong> {studentName(student)}
              </span>
              <span>
                <strong>Roll:</strong> {student.rollNumber ?? '—'}
              </span>
              <span>
                <strong>Class:</strong> {className}
              </span>
              <span>
                <strong>Division:</strong> {divName}
              </span>
            </div>

            {loadInv ? (
              <div style={styles.spin}>
                <Spinner />
              </div>
            ) : collectionRows.length > 0 ? (
              <>
                {invoices.length > 1 ? (
                  <label style={styles.fieldBlockFull}>
                    <span style={styles.fieldLab}>Open invoice</span>
                    <select
                      style={styles.fieldControl}
                      value={invoiceId}
                      onChange={(e) => {
                        setInvoiceId(e.target.value)
                        setPayByFeeHead({})
                      }}
                    >
                      {invoices.map((i) => (
                        <option key={i.id} value={i.id}>
                          {i.invoiceNo ?? i.id.slice(0, 8)} — balance {rupee(i.remainingAmount)}
                        </option>
                      ))}
                    </select>
                  </label>
                ) : null}

                {!selectedInvoice ? (
                  <div style={styles.noInvoiceBanner} role="status">
                    <strong>No open invoice yet for this year.</strong> Rows come from the <strong>class fee map</strong>{' '}
                    for this student’s class. When you <strong>Pay</strong>, the system creates the invoice (with one line
                    per fee head) and records this payment in one step.
                  </div>
                ) : null}

                <form onSubmit={onPay}>
                  <p style={styles.splitHint}>
                    Fee heads are driven by the <strong>class fee map</strong> for this student’s class and academic year.
                    Enter how much of this payment applies to each head (e.g. split ₹20,000 across tuition, uniform, ID
                    card). If there is no invoice yet, <strong>Pay</strong> will create it from these mapped fees, then
                    post the payment.
                  </p>
                  <table style={styles.tableBordered}>
                    <thead>
                      <tr>
                        <th style={styles.thGray}>SR.NO.</th>
                        <th style={styles.thGray}>Particulars</th>
                        <th style={{ ...styles.thGray, textAlign: 'right' }}>Total Fees</th>
                        <th style={{ ...styles.thGray, textAlign: 'right' }}>Paid</th>
                        <th style={{ ...styles.thGray, textAlign: 'right' }}>Remaining</th>
                        <th style={{ ...styles.thGray, textAlign: 'right' }}>Amount</th>
                      </tr>
                    </thead>
                    <tbody>
                      {splitLineCalcs.map(({ row }, idx) => (
                        <tr key={row.feeHeadId}>
                          <td style={styles.tdCell}>{idx + 1}</td>
                          <td style={styles.tdCell}>
                            {row.feeHeadName}
                            {row.maxPay <= 0.009 ? (
                              <span style={styles.rowNote}> — nothing due</span>
                            ) : row.invLines.length === 0 ? (
                              <span style={styles.rowNote}> — from class map</span>
                            ) : null}
                          </td>
                          <td style={{ ...styles.tdCell, textAlign: 'right', fontVariantNumeric: 'tabular-nums' }}>
                            {rupee(row.displayTotal)}
                          </td>
                          <td style={{ ...styles.tdCell, textAlign: 'right', fontVariantNumeric: 'tabular-nums' }}>
                            {rupee(row.displayPaid)}
                          </td>
                          <td
                            style={{
                              ...styles.tdCell,
                              textAlign: 'right',
                              fontWeight: 600,
                              fontVariantNumeric: 'tabular-nums',
                              color: row.displayRemaining <= 0.009 ? '#15803d' : '#0f172a',
                            }}
                          >
                            {rupee(row.displayRemaining)}
                          </td>
                          <td style={styles.tdCell}>
                            {row.maxPay > 0.009 ? (
                              <input
                                type="text"
                                inputMode="decimal"
                                aria-label={`Amount for ${row.feeHeadName}`}
                                style={styles.payNowInput}
                                value={payByFeeHead[row.feeHeadId] ?? ''}
                                onChange={(e) =>
                                  setPayByFeeHead((prev) => ({
                                    ...prev,
                                    [row.feeHeadId]: e.target.value,
                                  }))
                                }
                                placeholder="Enter amount"
                              />
                            ) : (
                              <span style={styles.cellMuted}>—</span>
                            )}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                    {invoiceGridTotals ? (
                      <tfoot>
                        <tr style={styles.tfootYellow}>
                          <td style={styles.tfootTd} colSpan={2}>
                            <strong>TOTAL</strong>
                          </td>
                          <td style={{ ...styles.tfootTd, textAlign: 'right', fontVariantNumeric: 'tabular-nums' }}>
                            <strong>{rupee(invoiceGridTotals.sumLineTotal)}</strong>
                          </td>
                          <td style={{ ...styles.tfootTd, textAlign: 'right', fontVariantNumeric: 'tabular-nums' }}>
                            <strong>{rupee(invoiceGridTotals.sumPaidBefore)}</strong>
                          </td>
                          <td style={{ ...styles.tfootTd, textAlign: 'right', fontVariantNumeric: 'tabular-nums' }}>
                            <strong>{rupee(invoiceGridTotals.sumRemainingBefore)}</strong>
                          </td>
                          <td style={{ ...styles.tfootTd, textAlign: 'right', fontVariantNumeric: 'tabular-nums' }}>
                            <strong>{rupee(invoiceGridTotals.sumPaying)}</strong>
                          </td>
                        </tr>
                      </tfoot>
                    ) : null}
                  </table>

                  <div style={styles.wordsBox}>
                    <strong>Total in Words: </strong>
                    {totalPayWords(totalPaying)}
                  </div>

                  <div style={styles.payMetaCompact}>
                    <label style={styles.inlineMeta}>
                      Pay method
                      <select
                        style={styles.fieldControlSm}
                        value={paymentMethod}
                        onChange={(e) => {
                          const v = e.target.value
                          setPaymentMethod(v)
                          if (v === 'Cash') setTxnRef('')
                        }}
                      >
                        <option value="Cash">Cash</option>
                        <option value="UPI">UPI</option>
                        <option value="Card">Card</option>
                        <option value="Bank">Bank transfer</option>
                      </select>
                    </label>
                    {isCashPayment ? (
                      <p style={styles.cashHint}>Cash — no transaction reference needed.</p>
                    ) : (
                      <label style={{ ...styles.inlineMeta, flex: '1 1 200px' }}>
                        {txnFieldMeta.label}
                        <input
                          style={styles.fieldControlSm}
                          value={txnRef}
                          onChange={(e) => setTxnRef(e.target.value)}
                          placeholder={txnFieldMeta.placeholder}
                          autoComplete="off"
                        />
                      </label>
                    )}
                  </div>

                  <div style={styles.payBtnWrap}>
                    {lastPaymentId ? (
                      <button type="button" style={styles.btnReceipt} onClick={() => void downloadPdf()}>
                        Receipt PDF
                      </button>
                    ) : null}
                    <button type="submit" style={styles.btnPay} disabled={submitting || totalPaying <= 0}>
                      {submitting ? 'Processing…' : `Pay ${rupee(totalPaying)}`}
                    </button>
                  </div>
                </form>

                {!selectedInvoice ? (
                  <div style={styles.createInvoiceSection}>
                    <h3 style={styles.h3Create}>Create invoice only (optional)</h3>
                    <p style={styles.muted}>
                      You can take payment with <strong>Pay</strong> above — an invoice is created automatically. Use
                      this if you need an open bill first without recording a payment.
                    </p>
                    {(feeDash?.installments ?? []).some((i) => i.remainingAmount > 0.009) ? (
                      <label
                        style={{
                          display: 'flex',
                          flexWrap: 'wrap',
                          alignItems: 'center',
                          gap: 8,
                          marginBottom: 12,
                        }}
                      >
                        <span style={styles.fieldLab}>Tag lines to installment (optional)</span>
                        <select
                          style={{ ...styles.fieldControl, minWidth: 220 }}
                          value={scheduleInstallmentId}
                          onChange={(e) => setScheduleInstallmentId(e.target.value)}
                        >
                          <option value="">— Not set —</option>
                          {(feeDash?.installments ?? [])
                            .filter((i) => i.remainingAmount > 0.009)
                            .map((i) => (
                              <option key={i.installmentId} value={i.installmentId}>
                                {i.installmentName} — due {money(i.remainingAmount)}
                              </option>
                            ))}
                        </select>
                      </label>
                    ) : null}
                    <Button type="button" onClick={() => void onCreateInvoiceFromSchedule()} disabled={creatingSchedule}>
                      {creatingSchedule ? 'Creating…' : 'Create open invoice from schedule'}
                    </Button>
                  </div>
                ) : null}
              </>
            ) : scheduleOutstandingHeads.length > 0 ? (
              <div style={{ padding: '12px 0' }}>
                <p style={{ margin: '0 0 12px', color: '#334155', fontSize: 14 }}>
                  No open invoice yet. Below is the fee schedule (remaining per head). Create an open invoice from this
                  schedule, then collect payment as usual.
                </p>
                {(feeDash?.installments ?? []).some((i) => i.remainingAmount > 0.009) ? (
                  <label
                    style={{
                      display: 'flex',
                      flexWrap: 'wrap',
                      alignItems: 'center',
                      gap: 8,
                      marginBottom: 12,
                    }}
                  >
                    <span style={styles.fieldLab}>Tag lines to installment (optional)</span>
                    <select
                      style={{ ...styles.fieldControl, minWidth: 220 }}
                      value={scheduleInstallmentId}
                      onChange={(e) => setScheduleInstallmentId(e.target.value)}
                    >
                      <option value="">— Not set —</option>
                      {(feeDash?.installments ?? [])
                        .filter((i) => i.remainingAmount > 0.009)
                        .map((i) => (
                          <option key={i.installmentId} value={i.installmentId}>
                            {i.installmentName} — due {money(i.remainingAmount)}
                          </option>
                        ))}
                    </select>
                  </label>
                ) : null}
                <p style={styles.tableCaption}>
                  Schedule preview (read-only). Use <strong>Create open invoice from schedule</strong> to enable collection in
                  the table above.
                </p>
                <table style={styles.tableBordered}>
                  <thead>
                    <tr>
                      <th style={styles.thGray}>#</th>
                      <th style={styles.thGray}>Particulars</th>
                      <th style={{ ...styles.thGray, textAlign: 'right' }}>Total Fees</th>
                      <th style={{ ...styles.thGray, textAlign: 'right' }}>Paid</th>
                      <th style={{ ...styles.thGray, textAlign: 'right' }}>Remaining</th>
                      <th style={{ ...styles.thGray, textAlign: 'right' }}>Amount</th>
                    </tr>
                  </thead>
                  <tbody>
                    {scheduleOutstandingHeads.map((h, idx) => (
                      <tr key={h.feeHeadId}>
                        <td style={styles.tdCell}>{idx + 1}</td>
                        <td style={styles.tdCell}>{h.feeHeadName}</td>
                        <td style={{ ...styles.tdCell, textAlign: 'right', fontVariantNumeric: 'tabular-nums' }}>
                          {rupee(h.totalAmount)}
                        </td>
                        <td style={{ ...styles.tdCell, textAlign: 'right', fontVariantNumeric: 'tabular-nums' }}>
                          {rupee(h.paidAmount)}
                        </td>
                        <td style={{ ...styles.tdCell, textAlign: 'right', fontWeight: 600, fontVariantNumeric: 'tabular-nums' }}>
                          {rupee(h.remainingAmount)}
                        </td>
                        <td style={{ ...styles.tdCell, textAlign: 'right', color: '#94a3b8', fontSize: 13 }}>
                          After invoice
                        </td>
                      </tr>
                    ))}
                  </tbody>
                  {scheduleTotals ? (
                    <tfoot>
                      <tr style={styles.tfootYellow}>
                        <td style={styles.tfootTd} colSpan={2}>
                          <strong>TOTAL (schedule)</strong>
                        </td>
                        <td style={{ ...styles.tfootTd, textAlign: 'right', fontVariantNumeric: 'tabular-nums' }}>
                          <strong>{rupee(scheduleTotals.sumTotal)}</strong>
                        </td>
                        <td style={{ ...styles.tfootTd, textAlign: 'right', fontVariantNumeric: 'tabular-nums' }}>
                          <strong>{rupee(scheduleOutstandingHeads.reduce((s, h) => s + h.paidAmount, 0))}</strong>
                        </td>
                        <td style={{ ...styles.tfootTd, textAlign: 'right', fontVariantNumeric: 'tabular-nums' }}>
                          <strong>{rupee(scheduleTotals.sumRemaining)}</strong>
                        </td>
                        <td style={styles.tfootTd} />
                      </tr>
                    </tfoot>
                  ) : null}
                </table>
                <div style={{ marginTop: 16 }}>
                  <Button type="button" onClick={() => void onCreateInvoiceFromSchedule()} disabled={creatingSchedule}>
                    {creatingSchedule ? 'Creating…' : 'Create open invoice from schedule'}
                  </Button>
                </div>
              </div>
            ) : (
              <p style={styles.muted}>
                No open invoices for this student in the selected academic year, and the fee schedule shows nothing due.
                Check class fee mapping and installments, or create an invoice under Finance → Invoices.
              </p>
            )}

            {studentInstalls.length > 0 ? (
              <section style={styles.analytics}>
                <h3 style={styles.h3}>Student installments (this year)</h3>
                <table style={styles.tableSmall}>
                  <thead>
                    <tr>
                      <th style={styles.th}>Installment</th>
                      <th style={{ ...styles.th, textAlign: 'right' }}>Total</th>
                      <th style={{ ...styles.th, textAlign: 'right' }}>Paid</th>
                      <th style={{ ...styles.th, textAlign: 'right' }}>Due</th>
                    </tr>
                  </thead>
                  <tbody>
                    {studentInstalls.map((r) => (
                      <tr key={r.installmentId || r.installmentName}>
                        <td style={styles.td}>{r.installmentName}</td>
                        <td style={{ ...styles.td, textAlign: 'right' }}>{money(r.totalAmount)}</td>
                        <td style={{ ...styles.td, textAlign: 'right' }}>{money(r.paidAmount)}</td>
                        <td
                          style={{
                            ...styles.td,
                            textAlign: 'right',
                            fontWeight: 600,
                            color: r.remainingAmount <= 0.01 ? '#047857' : '#b45309',
                          }}
                        >
                          {money(r.remainingAmount)}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </section>
            ) : null}
          </>
        ) : (
          <p style={styles.muted}>Select class & division in the tree, choose a student, then tap Search to load fee lines.</p>
        )}
      </div>

      {branchId && academicYearId.trim() ? (
        <section style={styles.branchAnalytics}>
          <h3 style={styles.h3}>Installment collection — {collectionScopeLabel}</h3>
          <p style={styles.muted}>
            Expected vs collected per installment for the selected academic year. Amounts are split by invoice lines
            linked to installments; scope follows the organization tree (school, class, division).
          </p>
          {installmentRows.length === 0 ? (
            <p style={{ ...styles.muted, marginTop: 12 }}>
              No installment-tagged lines for this scope. Select a class or division if you expect class-specific
              figures, or ensure invoices tag fee lines with installments.
            </p>
          ) : (
            <div style={styles.barWrap}>
              {installmentRows.map((r) => {
                const exp = r.expectedAmount
                const col = r.collectedAmount
                const pctFromApi =
                  r.collectionPercent != null && Number.isFinite(r.collectionPercent) ? r.collectionPercent : null
                const pct =
                  pctFromApi != null ? Math.min(100, pctFromApi) : exp > 0 ? Math.min(100, (col / exp) * 100) : 0
                const scopeLine = [r.branchName, r.className, r.divisionName].filter(Boolean).join(' · ')
                return (
                  <div key={r.installmentId} style={styles.barRow}>
                    <div style={styles.barHead}>
                      <div style={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                        <span>{r.installmentName}</span>
                        {scopeLine ? (
                          <span style={{ fontSize: 11, fontWeight: 500, color: t.textMuted }}>{scopeLine}</span>
                        ) : null}
                      </div>
                      <span style={styles.barNums}>
                        {money(col)} / {money(exp)} ({exp > 0 ? pct.toFixed(0) : '—'}%)
                      </span>
                    </div>
                    <div style={styles.barTrack}>
                      <div style={{ ...styles.barFill, width: `${pct}%` }} />
                    </div>
                  </div>
                )
              })}
            </div>
          )}
        </section>
      ) : null}
    </div>
  )
}

const uiBlue = '#2563eb'
const uiGreen = '#16a34a'

const styles: Record<string, CSSProperties> = {
  pageShell: {
    fontFamily: t.fontSans,
    background: 'transparent',
    padding: 0,
    margin: 0,
    minHeight: 0,
    boxSizing: 'border-box',
  },
  cardMain: {
    maxWidth: 1024,
    margin: '0 auto',
    background: t.surface,
    borderRadius: 16,
    boxShadow: '0 10px 40px rgba(15, 23, 42, 0.08)',
    padding: 24,
    border: '1px solid #e5e7eb',
  },
  parentNotice: {
    marginBottom: 16,
    padding: '12px 14px',
    borderRadius: 10,
    background: '#eff6ff',
    border: '1px solid #bfdbfe',
    fontSize: 14,
    color: '#1e3a5f',
    lineHeight: 1.5,
  },
  noInvoiceBanner: {
    marginBottom: 16,
    padding: '12px 14px',
    borderRadius: 10,
    background: '#fffbeb',
    border: '1px solid #fcd34d',
    fontSize: 14,
    color: '#713f12',
    lineHeight: 1.55,
  },
  createInvoiceSection: {
    marginTop: 24,
    paddingTop: 20,
    borderTop: '1px solid #e5e7eb',
  },
  h3Create: {
    margin: '0 0 8px',
    fontSize: 16,
    fontWeight: 700,
    color: '#0f172a',
  },
  helpPanel: {
    marginBottom: 20,
    padding: '14px 16px',
    borderRadius: 12,
    background: '#f8fafc',
    border: '1px solid #e2e8f0',
  },
  helpPanelTitle: {
    fontSize: 13,
    fontWeight: 800,
    color: '#0f172a',
    letterSpacing: '0.02em',
    marginBottom: 8,
  },
  helpList: {
    margin: 0,
    paddingLeft: 20,
    fontSize: 13,
    color: '#475569',
    lineHeight: 1.55,
  },
  helpFoot: {
    margin: '12px 0 0',
    fontSize: 13,
    color: '#64748b',
    lineHeight: 1.5,
  },
  codeInline: {
    fontSize: 12,
    fontFamily: 'ui-monospace, SFMono-Regular, Menlo, Consolas, monospace',
    background: '#e2e8f0',
    padding: '2px 6px',
    borderRadius: 4,
    color: '#0f172a',
  },
  breadcrumbSimple: { display: 'flex', alignItems: 'center', gap: 8, fontSize: 14, marginBottom: 12 },
  linkBlue: { color: uiBlue, textDecoration: 'none', fontWeight: 600 },
  breadcrumbSep: { color: '#9ca3af' },
  breadcrumbCurrent: { color: '#374151', fontWeight: 600 },
  h1Card: { margin: '0 0 8px', fontSize: 24, fontWeight: 800, color: '#111827' },
  scopeTiny: { margin: '0 0 12px', fontSize: 13, color: '#6b7280' },
  linksRow: { margin: '0 0 16px', fontSize: 13, color: t.textMuted },
  warn: { color: '#a16207', marginBottom: 12 },
  err: { color: t.danger, marginBottom: 12 },
  ok: { color: '#047857', marginBottom: 12 },
  muted: { color: t.textMuted, fontSize: 14 },
  card: {
    padding: 16,
    marginBottom: 16,
    background: '#fffbeb',
    borderRadius: 10,
    border: '1px solid #fcd34d',
  },
  pickList: { margin: '10px 0 0', paddingLeft: 18 },
  pickBtn: {
    background: 'none',
    border: 'none',
    color: uiBlue,
    cursor: 'pointer',
    textDecoration: 'underline',
    fontSize: 14,
    padding: 4,
  },
  fieldBlockFull: {
    display: 'flex',
    flexDirection: 'column',
    gap: 6,
    marginBottom: 16,
    width: '100%',
  },
  fieldBlock: {
    display: 'flex',
    flexDirection: 'column',
    gap: 6,
    minWidth: 0,
  },
  fieldLab: { fontSize: 12, fontWeight: 700, color: '#374151' },
  fieldLabMuted: { fontSize: 12, fontWeight: 600, color: '#6b7280' },
  fieldControl: {
    padding: '10px 12px',
    borderRadius: 8,
    border: '1px solid #d1d5db',
    fontSize: 14,
    background: '#fff',
    width: '100%',
    boxSizing: 'border-box',
  },
  fieldControlStatic: {
    padding: '10px 12px',
    borderRadius: 8,
    border: '1px solid #d1d5db',
    fontSize: 14,
    background: '#f9fafb',
    fontWeight: 600,
    color: '#111827',
    boxSizing: 'border-box',
  },
  fieldControlSm: {
    padding: '6px 10px',
    borderRadius: 8,
    border: '1px solid #d1d5db',
    fontSize: 13,
    width: '100%',
    boxSizing: 'border-box',
  },
  grid3: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
    gap: 16,
    marginBottom: 16,
  },
  toolbarActions: {
    display: 'flex',
    flexWrap: 'wrap',
    alignItems: 'center',
    justifyContent: 'flex-end',
    gap: 16,
    marginBottom: 20,
  },
  addFeesGreen: {
    color: uiGreen,
    fontWeight: 700,
    fontSize: 15,
    textDecoration: 'none',
  },
  btnSearch: {
    background: uiBlue,
    color: '#fff',
    border: 'none',
    borderRadius: 8,
    padding: '10px 22px',
    fontSize: 15,
    fontWeight: 700,
    cursor: 'pointer',
  },
  divider: { height: 1, background: '#e5e7eb', margin: '8px 0 16px' },
  schoolLine: { fontSize: 14, color: '#4b5563', margin: '0 0 12px' },
  studentSummary: {
    display: 'flex',
    flexWrap: 'wrap',
    gap: '12px 20px',
    alignItems: 'center',
    fontSize: 14,
    color: '#374151',
    marginBottom: 16,
  },
  dateInline: { display: 'flex', flexDirection: 'column', gap: 4, fontSize: 12, fontWeight: 600, color: '#6b7280' },
  dateInputSm: { padding: '6px 8px', borderRadius: 6, border: '1px solid #d1d5db', fontSize: 13 },
  spin: { padding: 24, display: 'flex', justifyContent: 'center' },
  tableBordered: {
    width: '100%',
    borderCollapse: 'collapse',
    fontSize: 14,
    marginBottom: 16,
    border: '1px solid #d1d5db',
  },
  thGray: {
    textAlign: 'left',
    padding: 10,
    border: '1px solid #d1d5db',
    background: '#e5e7eb',
    fontWeight: 700,
    color: '#111827',
  },
  tdCell: {
    padding: 10,
    border: '1px solid #e5e7eb',
    verticalAlign: 'middle',
  },
  payNowInput: {
    width: '100%',
    boxSizing: 'border-box',
    padding: '6px 8px',
    borderRadius: 6,
    border: '1px solid #d1d5db',
    fontSize: 14,
    fontVariantNumeric: 'tabular-nums' as const,
  },
  tfootYellow: { background: '#fef9c3' },
  tfootTd: {
    padding: 10,
    border: '1px solid #fde047',
    fontWeight: 600,
    color: '#713f12',
  },
  tableCaption: {
    margin: '0 0 12px',
    fontSize: 13,
    lineHeight: 1.5,
    color: '#475569',
  },
  splitHint: {
    margin: '0 0 12px',
    fontSize: 13,
    lineHeight: 1.55,
    color: '#475569',
  },
  rowNote: { fontWeight: 500, color: '#94a3b8', fontSize: 12 },
  cellMuted: { color: '#94a3b8', fontSize: 13 },
  wordsBox: {
    marginTop: 4,
    marginBottom: 16,
    padding: 12,
    background: '#f9fafb',
    borderRadius: 8,
    border: '1px solid #e5e7eb',
    fontSize: 14,
    color: '#374151',
  },
  payMetaCompact: {
    display: 'flex',
    flexWrap: 'wrap',
    alignItems: 'flex-end',
    gap: 12,
    marginBottom: 16,
    justifyContent: 'flex-end',
  },
  cashHint: {
    margin: 0,
    flex: '1 1 180px',
    fontSize: 13,
    color: '#64748b',
    lineHeight: 1.4,
    paddingBottom: 2,
  },
  inlineMeta: {
    display: 'flex',
    flexDirection: 'column',
    gap: 4,
    fontSize: 11,
    fontWeight: 600,
    color: '#6b7280',
    minWidth: 100,
  },
  payBtnWrap: { display: 'flex', flexWrap: 'wrap', gap: 12, justifyContent: 'flex-end' },
  btnPay: {
    background: uiBlue,
    color: '#fff',
    border: 'none',
    borderRadius: 8,
    padding: '10px 24px',
    fontSize: 16,
    fontWeight: 700,
    cursor: 'pointer',
    boxShadow: '0 4px 12px rgba(37, 99, 235, 0.35)',
  },
  btnReceipt: {
    background: '#fff',
    color: uiBlue,
    border: `2px solid ${uiBlue}`,
    borderRadius: 8,
    padding: '8px 16px',
    fontSize: 14,
    fontWeight: 600,
    cursor: 'pointer',
  },
  analytics: {
    marginTop: 24,
    padding: 16,
    background: '#f8fafc',
    borderRadius: t.radius,
    border: `1px solid ${t.border}`,
  },
  h3: { margin: '0 0 12px', fontSize: 15, fontWeight: 700, color: t.text },
  tableSmall: { width: '100%', borderCollapse: 'collapse', fontSize: 13 },
  th: {
    textAlign: 'left',
    padding: '8px 6px',
    borderBottom: `1px solid ${t.borderStrong}`,
    fontSize: 11,
    fontWeight: 700,
    textTransform: 'uppercase',
    color: t.textSecondary,
  },
  td: { padding: '8px 6px', borderBottom: `1px solid ${t.border}`, verticalAlign: 'middle' },
  branchAnalytics: {
    marginTop: 20,
    maxWidth: 1024,
    marginLeft: 'auto',
    marginRight: 'auto',
    padding: 20,
    background: t.surface,
    borderRadius: t.radius,
    border: `1px solid ${t.border}`,
  },
  barWrap: { display: 'flex', flexDirection: 'column', gap: 12, marginTop: 12 },
  barRow: { display: 'flex', flexDirection: 'column', gap: 6 },
  barHead: { display: 'flex', justifyContent: 'space-between', fontSize: 13, fontWeight: 600, color: t.text },
  barNums: { fontWeight: 500, color: t.textMuted, fontVariantNumeric: 'tabular-nums' },
  barTrack: { height: 10, background: '#e2e8f0', borderRadius: 5, overflow: 'hidden' },
  barFill: { height: '100%', background: `linear-gradient(90deg, ${uiBlue}, #3b82f6)`, borderRadius: 5 },
}
