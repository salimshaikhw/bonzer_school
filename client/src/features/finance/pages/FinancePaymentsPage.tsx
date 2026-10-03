import type { CSSProperties } from 'react'
import { useEffect, useMemo, useState } from 'react'
import { AdmissionOrgScopeBanner } from '../../admission/components/AdmissionOrgScopeBanner'
import { branchesInGroupScope } from '../../admission/utils/loadStudentsForOrgTree'
import { useOrgHierarchy } from '../../../layouts/OrgHierarchyProvider'
import { Spinner } from '../../../shared'
import { feeHeadsApi } from '../api/feeHeads.api'
import { staffPaymentsApi } from '../api/staffPayments.api'
import type { PaymentRow } from '../types'
import { financeScopeTitle, getFinanceEffectiveBranchIds, getFinanceScopeLevel } from '../utils/financeOrgScope'
import { FinancePageHeader } from '../ui/FinancePageHeader'
import { financeTheme as t } from '../ui/financeTheme'

function norm(s: string | undefined | null): string {
  return String(s ?? '')
    .trim()
    .toLowerCase()
}

function money(n: number | undefined): string {
  if (n == null || !Number.isFinite(n)) return '—'
  return n.toLocaleString(undefined, { minimumFractionDigits: 0, maximumFractionDigits: 2 })
}

export function FinancePaymentsPage() {
  const org = useOrgHierarchy()
  const [rows, setRows] = useState<PaymentRow[]>([])
  const [feeHeads, setFeeHeads] = useState<{ id: string; name?: string }[]>([])
  const [loading, setLoading] = useState(true)
  const [err, setErr] = useState<string | null>(null)
  const [q, setQ] = useState('')

  const [amount, setAmount] = useState('')
  const [paymentDate, setPaymentDate] = useState(() => new Date().toISOString().slice(0, 10))
  const [paymentMethod, setPaymentMethod] = useState('Cash')
  const [studentId, setStudentId] = useState('')
  const [invoiceId, setInvoiceId] = useState('')
  const [remarks, setRemarks] = useState('')
  const [feeHeadId, setFeeHeadId] = useState('')
  const [feeLineAmount, setFeeLineAmount] = useState('')
  const [submitErr, setSubmitErr] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const inGroupBranches = useMemo(
    () => branchesInGroupScope(org.groupScopeId, org.branches),
    [org.groupScopeId, org.branches],
  )
  const inGroupBranchNorm = useMemo(() => new Set(inGroupBranches.map((b) => norm(b.id))), [inGroupBranches])

  const hasTreeScope = !!(org.groupScopeId.trim() || org.branchId.trim())
  const analyticsLevel = useMemo(() => getFinanceScopeLevel(org), [
    org.branchId,
    org.groupScopeId,
    org.classId,
    org.divisionId,
  ])
  const effectiveBranchIds = useMemo(() => getFinanceEffectiveBranchIds(org), [
    org.branchId,
    org.groupScopeId,
    org.branches,
  ])
  const scopeTitle = useMemo(() => financeScopeTitle(analyticsLevel, org, effectiveBranchIds.length), [
    analyticsLevel,
    effectiveBranchIds.length,
    org.branchId,
    org.groupScopeId,
    org.classId,
    org.divisionId,
    org.branches,
    org.schoolGroups,
    org.treeClasses,
    org.treeDivisions,
    org.classes,
    org.divisions,
  ])
  const needBranchToPay = !org.branchId.trim()

  const refresh = () => {
    setLoading(true)
    setErr(null)
    void staffPaymentsApi
      .getAll()
      .then((r) => setRows(r))
      .catch((e) => setErr(e instanceof Error ? e.message : 'Failed to load payments.'))
      .finally(() => setLoading(false))
  }

  useEffect(() => {
    refresh()
    void feeHeadsApi
      .getAll()
      .then((h) => setFeeHeads(h.map((x) => ({ id: x.id, name: x.name }))))
      .catch(() => setFeeHeads([]))
  }, [])

  const filtered = useMemo(() => {
    if (!hasTreeScope) return []
    const qq = q.trim().toLowerCase()
    return rows.filter((p) => {
      if (org.branchId.trim()) {
        if (norm(p.branchId) !== norm(org.branchId)) return false
      } else if (org.groupScopeId.trim()) {
        const bid = p.branchId ?? ''
        if (!bid || !inGroupBranchNorm.has(norm(bid))) return false
      }
      if (org.divisionId.trim() && norm(p.divisionId) !== norm(org.divisionId)) return false
      if (!qq) return true
      const hay = [p.id, p.studentId, p.invoiceId, p.paymentMethod, p.transactionNumber, p.remarks]
        .filter(Boolean)
        .join(' ')
        .toLowerCase()
      return hay.includes(qq)
    })
  }, [rows, org.branchId, org.groupScopeId, org.divisionId, inGroupBranchNorm, q])

  const submitPayment = async () => {
    setSubmitErr(null)
    const branchId = org.branchId.trim()
    if (!branchId) {
      setSubmitErr('Select a school (branch) in the organization tree.')
      return
    }
    const amt = Number(amount)
    if (!Number.isFinite(amt) || amt <= 0) {
      setSubmitErr('Enter a valid amount.')
      return
    }
    const sid = studentId.trim()
    const iid = invoiceId.trim()
    const fh = feeHeadId.trim()
    const flAmt = Number(feeLineAmount)
    const feeLines =
      fh && Number.isFinite(flAmt) && flAmt > 0 ? [{ feeHeadId: fh, amount: flAmt }] : undefined

    setSubmitting(true)
    try {
      const row = await staffPaymentsApi.create({
        branchId,
        divisionId: org.divisionId.trim() || null,
        studentId: sid || null,
        invoiceId: iid || null,
        paymentDate: paymentDate.trim() || null,
        amount: amt,
        paymentMethod: paymentMethod.trim() || null,
        remarks: remarks.trim() || null,
        feeLines: feeLines ?? null,
      })
      if (row) setRows((prev) => [row, ...prev])
      setAmount('')
      setInvoiceId('')
      setRemarks('')
      setFeeLineAmount('')
    } catch (e) {
      setSubmitErr(e instanceof Error ? e.message : 'Payment failed.')
    } finally {
      setSubmitting(false)
    }
  }

  const downloadPdf = async (id: string) => {
    try {
      const blob = await staffPaymentsApi.downloadReceiptPdf(id, 'attachment')
      const url = URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = `receipt-${id}.pdf`
      a.click()
      URL.revokeObjectURL(url)
    } catch {
      setErr('Could not download receipt PDF.')
    }
  }

  return (
    <div style={{ fontFamily: t.fontSans }}>
      <AdmissionOrgScopeBanner />

      <FinancePageHeader
        title="Payments"
        description="Record collections and download receipt PDFs. The ledger respects your tree scope; recording a payment requires a school (branch) row."
        actions={
          hasTreeScope ? (
            <span style={styles.countPill}>{filtered.length} in scope</span>
          ) : null
        }
      />

      {scopeTitle ? (
        <div style={styles.scopeStrip}>
          <strong style={styles.scopeK}>List & payment scope</strong>
          <span style={styles.scopeLine}>{scopeTitle}</span>
          {needBranchToPay ? (
            <span style={styles.scopeSub}>
              Group scope shows payments for all schools in the group. Select a single school in the tree to record a
              new payment (branch is required).
            </span>
          ) : org.divisionId.trim() ? (
            <span style={styles.scopeSub}>Payment list is filtered by division when the tree is on a division.</span>
          ) : null}
        </div>
      ) : (
        <div style={styles.emptyBox}>
          <div style={styles.emptyGlyph} aria-hidden>
            P
          </div>
          <strong style={styles.emptyTitle}>Select an organization scope</strong>
          <p style={styles.muted}>
            Choose a school group or school in the tree — the payment history stays empty until you do.
          </p>
        </div>
      )}

      <section style={{ ...styles.card, opacity: needBranchToPay ? 0.92 : 1 }}>
        <h2 style={styles.h2}>Record payment</h2>
        <p style={styles.muted}>
          Uses the same payment API as the rest of the system. Optional fee line allocates the amount to a fee head
          when not paying against a specific invoice.
        </p>
        {needBranchToPay ? (
          <p style={styles.warnBox}>
            Select a <strong>school</strong> (branch) in the organization tree — not only a group row — to enable this
            form.
          </p>
        ) : null}
        {submitErr ? <p style={styles.error}>{submitErr}</p> : null}
        <div style={styles.formGrid}>
          <label style={styles.field}>
            Amount
            <input
              style={styles.input}
              inputMode="decimal"
              value={amount}
              onChange={(e) => setAmount(e.target.value)}
            />
          </label>
          <label style={styles.field}>
            Payment date
            <input
              type="date"
              style={styles.input}
              value={paymentDate}
              onChange={(e) => setPaymentDate(e.target.value)}
            />
          </label>
          <label style={styles.field}>
            Method
            <input
              style={styles.input}
              value={paymentMethod}
              onChange={(e) => setPaymentMethod(e.target.value)}
            />
          </label>
          <label style={styles.field}>
            Student id (optional)
            <input style={styles.input} value={studentId} onChange={(e) => setStudentId(e.target.value)} />
          </label>
          <label style={styles.field}>
            Invoice id (optional)
            <input style={styles.input} value={invoiceId} onChange={(e) => setInvoiceId(e.target.value)} />
          </label>
          <label style={styles.field}>
            Remarks
            <input style={styles.input} value={remarks} onChange={(e) => setRemarks(e.target.value)} />
          </label>
          <label style={styles.field}>
            Fee line — head
            <select style={styles.input} value={feeHeadId} onChange={(e) => setFeeHeadId(e.target.value)}>
              <option value="">— none —</option>
              {feeHeads.map((h) => (
                <option key={h.id} value={h.id}>
                  {h.name ?? h.id}
                </option>
              ))}
            </select>
          </label>
          <label style={styles.field}>
            Fee line — amount
            <input
              style={styles.input}
              inputMode="decimal"
              value={feeLineAmount}
              onChange={(e) => setFeeLineAmount(e.target.value)}
            />
          </label>
        </div>
        <button
          type="button"
          style={{ ...styles.btnPrimary, opacity: submitting || needBranchToPay ? 0.55 : 1 }}
          onClick={() => void submitPayment()}
          disabled={submitting || needBranchToPay}
        >
          {submitting ? 'Saving…' : 'Save payment'}
        </button>
      </section>

      <div style={styles.toolbarCard}>
        <input
          type="search"
          placeholder="Search payments…"
          value={q}
          onChange={(e) => setQ(e.target.value)}
          style={styles.search}
        />
        <button type="button" style={styles.btnGhost} onClick={refresh} disabled={loading}>
          Refresh
        </button>
      </div>

      {err ? <p style={styles.error}>{err}</p> : null}
      {loading ? (
        <div style={styles.spin}>
          <Spinner />
        </div>
      ) : (
        <div style={styles.tableWrap}>
          <table style={styles.table}>
            <thead>
              <tr>
                <th style={styles.th}>Date</th>
                <th style={styles.th}>Amount</th>
                <th style={styles.th}>Method</th>
                <th style={styles.th}>Student</th>
                <th style={styles.th}>Invoice</th>
                <th style={styles.th}>Receipt</th>
              </tr>
            </thead>
            <tbody>
              {filtered.length === 0 ? (
                <tr>
                  <td colSpan={6} style={styles.tdMuted}>
                    No payments match this scope or search.
                  </td>
                </tr>
              ) : (
                filtered.map((p) => (
                  <tr key={p.id}>
                    <td style={styles.td}>{p.paymentDate ?? '—'}</td>
                    <td style={styles.td}>{money(p.amount)}</td>
                    <td style={styles.td}>{p.paymentMethod ?? '—'}</td>
                    <td style={styles.td}>{p.studentId ?? '—'}</td>
                    <td style={styles.td}>{p.invoiceId ?? '—'}</td>
                    <td style={styles.td}>
                      <button type="button" style={styles.linkBtn} onClick={() => void downloadPdf(p.id)}>
                        Download PDF
                      </button>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}

const styles: Record<string, CSSProperties> = {
  countPill: {
    fontSize: 13,
    fontWeight: 600,
    color: t.textSecondary,
    padding: '8px 14px',
    background: t.bgPage,
    borderRadius: t.radiusPill,
    border: `1px solid ${t.border}`,
  },
  scopeStrip: {
    marginBottom: 18,
    padding: '14px 18px',
    borderRadius: t.radius,
    background: t.accentSoft,
    border: `1px solid ${t.accentMuted}`,
    borderLeft: `4px solid ${t.accent}`,
    display: 'flex',
    flexDirection: 'column',
    gap: 6,
  },
  scopeK: { fontSize: 11, letterSpacing: '0.08em', textTransform: 'uppercase', color: t.accent, fontWeight: 700 },
  scopeLine: { fontSize: 15, fontWeight: 700, color: t.text },
  scopeSub: { fontSize: 13, color: t.textMuted },
  emptyBox: {
    marginBottom: 20,
    padding: '32px 24px',
    borderRadius: t.radius,
    border: `1px dashed ${t.borderStrong}`,
    background: t.bgPage,
    textAlign: 'center' as const,
    maxWidth: 480,
    marginLeft: 'auto',
    marginRight: 'auto',
  },
  emptyGlyph: {
    width: 48,
    height: 48,
    margin: '0 auto 14px',
    borderRadius: 12,
    background: t.accentSoft,
    border: `1px solid ${t.accentMuted}`,
    color: t.accent,
    fontSize: 20,
    fontWeight: 800,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
  },
  emptyTitle: { display: 'block', marginBottom: 8, fontSize: 17, fontWeight: 700, color: t.text },
  warnBox: {
    padding: '14px 16px',
    borderRadius: t.radius,
    background: t.warningSoft,
    border: `1px solid #fcd34d`,
    color: '#92400e',
    fontSize: 14,
    lineHeight: 1.5,
    marginBottom: 14,
  },
  card: {
    marginBottom: 24,
    padding: '24px 22px',
    background: t.surface,
    borderRadius: t.radius,
    border: `1px solid ${t.border}`,
    boxShadow: t.shadow,
  },
  h2: { margin: '0 0 8px', fontSize: 15, fontWeight: 700, color: t.text, letterSpacing: '-0.02em' },
  muted: { margin: '0 0 16px', fontSize: 14, color: t.textMuted, lineHeight: 1.55 },
  formGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(200px, 1fr))',
    gap: 14,
    marginBottom: 18,
  },
  field: { display: 'flex', flexDirection: 'column', gap: 8, fontSize: 12, fontWeight: 600, color: t.textSecondary },
  input: {
    padding: '10px 12px',
    borderRadius: 8,
    border: `1px solid ${t.borderStrong}`,
    fontSize: 14,
    fontFamily: t.fontSans,
    background: t.surface,
  },
  btnPrimary: {
    padding: '12px 22px',
    borderRadius: 8,
    border: 'none',
    background: t.accent,
    color: '#fff',
    fontWeight: 600,
    fontSize: 14,
    cursor: 'pointer',
    boxShadow: '0 1px 2px rgba(79, 70, 229, 0.25)',
  },
  btnGhost: {
    padding: '10px 18px',
    borderRadius: 8,
    border: `1px solid ${t.borderStrong}`,
    background: t.surface,
    fontWeight: 600,
    fontSize: 14,
    color: t.textSecondary,
    cursor: 'pointer',
  },
  toolbarCard: {
    display: 'flex',
    flexWrap: 'wrap',
    gap: 12,
    marginBottom: 20,
    padding: '16px 18px',
    background: t.surface,
    borderRadius: t.radius,
    border: `1px solid ${t.border}`,
    boxShadow: t.shadow,
    alignItems: 'center',
  },
  search: {
    flex: '1 1 260px',
    padding: '10px 14px',
    borderRadius: 8,
    border: `1px solid ${t.borderStrong}`,
    fontSize: 14,
    fontFamily: t.fontSans,
  },
  error: { color: t.danger, marginBottom: 12, fontSize: 14 },
  spin: { padding: 40, display: 'flex', justifyContent: 'center' },
  tableWrap: {
    background: t.surface,
    borderRadius: t.radius,
    border: `1px solid ${t.border}`,
    overflow: 'auto',
    boxShadow: t.shadow,
  },
  table: { width: '100%', borderCollapse: 'collapse', fontSize: 14, minWidth: 640 },
  th: {
    textAlign: 'left',
    padding: '12px 14px',
    background: t.bgPage,
    color: t.textSecondary,
    fontSize: 11,
    fontWeight: 600,
    textTransform: 'uppercase',
    letterSpacing: '0.05em',
    borderBottom: `1px solid ${t.border}`,
  },
  td: { padding: '12px 14px', borderBottom: `1px solid ${t.border}`, color: t.text },
  tdMuted: { padding: '32px 14px', color: t.textMuted, textAlign: 'center', fontSize: 14 },
  linkBtn: {
    padding: '6px 12px',
    borderRadius: 6,
    border: `1px solid ${t.accentMuted}`,
    background: t.accentSoft,
    color: t.accent,
    fontWeight: 600,
    fontSize: 13,
    cursor: 'pointer',
  },
}
