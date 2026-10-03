import type { CSSProperties } from 'react'
import { useEffect, useMemo, useState } from 'react'
import { AdmissionOrgScopeBanner } from '../../admission/components/AdmissionOrgScopeBanner'
import { branchesInGroupScope } from '../../admission/utils/loadStudentsForOrgTree'
import { useOrgHierarchy } from '../../../layouts/OrgHierarchyProvider'
import { Spinner } from '../../../shared'
import { invoicesApi, type InvoiceStatementDto } from '../api/invoices.api'
import type { InvoiceRow } from '../types'
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

export function FinanceInvoicesPage() {
  const org = useOrgHierarchy()
  const [rows, setRows] = useState<InvoiceRow[]>([])
  const [loading, setLoading] = useState(true)
  const [err, setErr] = useState<string | null>(null)
  const [q, setQ] = useState('')
  const [creating, setCreating] = useState(false)
  const [formErr, setFormErr] = useState<string | null>(null)
  const [stmtModalId, setStmtModalId] = useState<string | null>(null)
  const [stmt, setStmt] = useState<InvoiceStatementDto | null>(null)
  const [stmtLoading, setStmtLoading] = useState(false)
  const [stmtErr, setStmtErr] = useState<string | null>(null)

  const openStatement = (id: string) => {
    setStmtModalId(id)
    setStmt(null)
    setStmtErr(null)
    setStmtLoading(true)
    void invoicesApi
      .getStatement(id)
      .then((s) => {
        if (!s) setStmtErr('Invoice not found.')
        else setStmt(s)
      })
      .catch((e) => setStmtErr(e instanceof Error ? e.message : 'Failed to load details.'))
      .finally(() => setStmtLoading(false))
  }

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

  const refresh = () => {
    setLoading(true)
    setErr(null)
    void invoicesApi
      .getAll()
      .then((r) => setRows(r))
      .catch((e) => setErr(e instanceof Error ? e.message : 'Failed to load invoices.'))
      .finally(() => setLoading(false))
  }

  useEffect(() => {
    refresh()
  }, [])

  const filtered = useMemo(() => {
    if (!hasTreeScope) return []
    const qq = q.trim().toLowerCase()
    return rows.filter((inv) => {
      if (org.branchId.trim()) {
        if (norm(inv.branchId) !== norm(org.branchId)) return false
      } else if (org.groupScopeId.trim()) {
        const bid = inv.branchId ?? ''
        if (!bid || !inGroupBranchNorm.has(norm(bid))) return false
      }
      if (org.classId.trim() && norm(inv.classId) !== norm(org.classId)) return false
      if (org.divisionId.trim() && norm(inv.divisionId) !== norm(org.divisionId)) return false
      if (!qq) return true
      const hay = [
        inv.invoiceNo,
        inv.studentId,
        inv.status,
        inv.id,
        inv.branchId,
        inv.classId,
        inv.divisionId,
      ]
        .filter(Boolean)
        .join(' ')
        .toLowerCase()
      return hay.includes(qq)
    })
  }, [rows, hasTreeScope, org.branchId, org.groupScopeId, org.classId, org.divisionId, inGroupBranchNorm, q])

  const [draft, setDraft] = useState({
    invoiceNo: '',
    invoiceDate: '',
    dueDate: '',
    totalAmount: '',
    studentId: '',
    status: 'Open',
  })

  const openCreate = () => {
    setFormErr(null)
    setDraft({
      invoiceNo: '',
      invoiceDate: new Date().toISOString().slice(0, 10),
      dueDate: '',
      totalAmount: '',
      studentId: '',
      status: 'Open',
    })
    setCreating(true)
    if (!org.branchId.trim()) {
      setFormErr('Select a school in the organization tree so the invoice has a branch.')
    }
  }

  const submitCreate = async () => {
    setFormErr(null)
    const branchId = org.branchId.trim()
    if (!branchId) {
      setFormErr('Branch is required (pick a school in the tree).')
      return
    }
    const total = Number(draft.totalAmount)
    if (!Number.isFinite(total) || total <= 0) {
      setFormErr('Enter a valid total amount.')
      return
    }
    const sid = draft.studentId.trim()
    try {
      const id = crypto.randomUUID()
      const row = await invoicesApi.create({
        id,
        branchId,
        classId: org.classId.trim() || null,
        divisionId: org.divisionId.trim() || null,
        studentId: sid || null,
        invoiceNo: draft.invoiceNo.trim() || null,
        invoiceDate: draft.invoiceDate.trim() || null,
        dueDate: draft.dueDate.trim() || null,
        totalAmount: total,
        paidAmount: 0,
        remainingAmount: total,
        status: draft.status.trim() || null,
      })
      if (row) setRows((prev) => [row, ...prev])
      setCreating(false)
    } catch (e) {
      setFormErr(e instanceof Error ? e.message : 'Create failed.')
    }
  }

  const needBranchForCreate = !org.branchId.trim()

  return (
    <div style={{ fontFamily: t.fontSans }}>
      <AdmissionOrgScopeBanner />

      <FinancePageHeader
        title="Invoices"
        description="Ledger of fee invoices for the selected organization scope. Create new invoices when a school (branch) is selected in the tree."
        actions={
          hasTreeScope ? (
            <span style={styles.countPill}>{filtered.length} in scope</span>
          ) : null
        }
      />

      {scopeTitle ? (
        <div style={styles.scopeStrip}>
          <strong style={styles.scopeK}>List scope</strong>
          <span style={styles.scopeLine}>{scopeTitle}</span>
          {org.classId.trim() || org.divisionId.trim() ? (
            <span style={styles.scopeSub}>Rows match class/division columns on invoices when set.</span>
          ) : null}
        </div>
      ) : (
        <div style={styles.emptyBox}>
          <div style={styles.emptyGlyph} aria-hidden>
            I
          </div>
          <strong style={styles.emptyTitle}>Select an organization scope</strong>
          <p style={styles.muted}>
            Choose a school group or school in the tree. The list stays empty until you do — consistent with Admission.
          </p>
        </div>
      )}

      <div style={styles.toolbarCard}>
        <input
          type="search"
          placeholder="Search invoice no, student id, status…"
          value={q}
          onChange={(e) => setQ(e.target.value)}
          style={styles.search}
        />
        <button
          type="button"
          style={{ ...styles.btnPrimary, opacity: needBranchForCreate ? 0.5 : 1, cursor: needBranchForCreate ? 'not-allowed' : 'pointer' }}
          onClick={openCreate}
          title={
            needBranchForCreate
              ? 'Select a school (not only a group) in the tree to create an invoice with a branch.'
              : undefined
          }
        >
          + New invoice
        </button>
        <button type="button" style={styles.btnGhost} onClick={refresh} disabled={loading}>
          Refresh
        </button>
      </div>

      {creating ? (
        <div style={styles.modalWrap}>
          <div style={styles.modal}>
            <h3 style={styles.h3}>Create invoice</h3>
            <p style={styles.hint}>
              Branch, class, and division come from the current tree selection. Adjust the tree if needed.
            </p>
            {formErr ? <p style={styles.error}>{formErr}</p> : null}
            <label style={styles.field}>
              Invoice no
              <input
                style={styles.input}
                value={draft.invoiceNo}
                onChange={(e) => setDraft((d) => ({ ...d, invoiceNo: e.target.value }))}
              />
            </label>
            <label style={styles.field}>
              Invoice date
              <input
                type="date"
                style={styles.input}
                value={draft.invoiceDate}
                onChange={(e) => setDraft((d) => ({ ...d, invoiceDate: e.target.value }))}
              />
            </label>
            <label style={styles.field}>
              Due date
              <input
                type="date"
                style={styles.input}
                value={draft.dueDate}
                onChange={(e) => setDraft((d) => ({ ...d, dueDate: e.target.value }))}
              />
            </label>
            <label style={styles.field}>
              Total amount
              <input
                style={styles.input}
                inputMode="decimal"
                value={draft.totalAmount}
                onChange={(e) => setDraft((d) => ({ ...d, totalAmount: e.target.value }))}
              />
            </label>
            <label style={styles.field}>
              Student id (optional)
              <input
                style={styles.input}
                value={draft.studentId}
                onChange={(e) => setDraft((d) => ({ ...d, studentId: e.target.value }))}
                placeholder="GUID"
              />
            </label>
            <label style={styles.field}>
              Status
              <input
                style={styles.input}
                value={draft.status}
                onChange={(e) => setDraft((d) => ({ ...d, status: e.target.value }))}
              />
            </label>
            <div style={styles.modalActions}>
              <button type="button" style={styles.btnGhost} onClick={() => setCreating(false)}>
                Cancel
              </button>
              <button type="button" style={styles.btnPrimary} onClick={() => void submitCreate()}>
                Save
              </button>
            </div>
          </div>
        </div>
      ) : null}

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
                <th style={styles.th}>Invoice</th>
                <th style={styles.th}>Date</th>
                <th style={styles.th}>Student</th>
                <th style={styles.th}>Status</th>
                <th style={styles.th}>Total</th>
                <th style={styles.th}>Paid</th>
                <th style={styles.th}>Remaining</th>
                <th style={styles.th}>Details</th>
              </tr>
            </thead>
            <tbody>
              {filtered.length === 0 ? (
                <tr>
                  <td colSpan={8} style={styles.tdMuted}>
                    {!hasTreeScope
                      ? 'Select a school group or school in the organization tree — invoices are filtered by that scope.'
                      : 'No invoices match this scope or search.'}
                  </td>
                </tr>
              ) : (
                filtered.map((inv) => (
                  <tr key={inv.id}>
                    <td style={styles.td}>{inv.invoiceNo ?? inv.id.slice(0, 8)}</td>
                    <td style={styles.td}>{inv.invoiceDate ?? '—'}</td>
                    <td style={styles.td}>{inv.studentId ?? '—'}</td>
                    <td style={styles.td}>{inv.status ?? '—'}</td>
                    <td style={styles.td}>{money(inv.totalAmount)}</td>
                    <td style={styles.td}>{money(inv.paidAmount)}</td>
                    <td style={{ ...styles.td, fontWeight: 700 }}>{money(inv.remainingAmount)}</td>
                    <td style={styles.td}>
                      <button
                        type="button"
                        style={styles.btnLink}
                        onClick={() => openStatement(inv.id)}
                      >
                        View
                      </button>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      )}

      {stmtModalId ? (
        <div style={styles.modalWrap} role="dialog" aria-modal="true" aria-labelledby="inv-stmt-title">
          <div style={styles.modalWide}>
            <h3 id="inv-stmt-title" style={styles.h3}>
              Invoice details
            </h3>
            {stmtLoading ? (
              <div style={styles.spin}>
                <Spinner />
              </div>
            ) : stmtErr ? (
              <p style={styles.error}>{stmtErr}</p>
            ) : stmt ? (
              <>
                <p style={styles.hint}>
                  <strong>{stmt.invoice.invoiceNo ?? stmt.invoice.id.slice(0, 8)}</strong>
                  {stmt.invoice.invoiceDate ? ` · ${stmt.invoice.invoiceDate}` : null}
                  {stmt.invoice.studentId ? ` · Student ${stmt.invoice.studentId.slice(0, 8)}…` : null}
                </p>
                <h4 style={styles.h4}>Fee lines (invoice)</h4>
                <table style={styles.stmtTable}>
                  <thead>
                    <tr>
                      <th style={styles.thSm}>Particulars</th>
                      <th style={{ ...styles.thSm, textAlign: 'right' }}>Total</th>
                      <th style={{ ...styles.thSm, textAlign: 'right' }}>Paid</th>
                      <th style={{ ...styles.thSm, textAlign: 'right' }}>Due</th>
                    </tr>
                  </thead>
                  <tbody>
                    {stmt.lines.length === 0 ? (
                      <tr>
                        <td colSpan={4} style={styles.tdMuted}>
                          No line items.
                        </td>
                      </tr>
                    ) : (
                      stmt.lines.map((ln) => (
                        <tr key={ln.feeHeadId}>
                          <td style={styles.tdSm}>{ln.feeHeadName}</td>
                          <td style={{ ...styles.tdSm, textAlign: 'right' }}>{money(ln.totalAmount)}</td>
                          <td style={{ ...styles.tdSm, textAlign: 'right' }}>{money(ln.paidAmount)}</td>
                          <td style={{ ...styles.tdSm, textAlign: 'right', fontWeight: 600 }}>
                            {money(ln.remainingAmount)}
                          </td>
                        </tr>
                      ))
                    )}
                  </tbody>
                </table>
                <h4 style={styles.h4}>Last payment — fee breakdown</h4>
                {stmt.lastPayment ? (
                  <>
                    <p style={styles.muted}>
                      {stmt.lastPayment.paymentDate ?? '—'} · {money(stmt.lastPayment.amount)} ·{' '}
                      {stmt.lastPayment.paymentMethod ?? '—'}
                      {stmt.lastPayment.transactionNumber
                        ? ` · Ref ${stmt.lastPayment.transactionNumber}`
                        : null}
                    </p>
                    <table style={styles.stmtTable}>
                      <thead>
                        <tr>
                          <th style={styles.thSm}>Particulars</th>
                          <th style={{ ...styles.thSm, textAlign: 'right' }}>Paid</th>
                        </tr>
                      </thead>
                      <tbody>
                        {stmt.lastPayment.feeLines.length === 0 ? (
                          <tr>
                            <td colSpan={2} style={styles.tdMuted}>
                              No stored fee lines for this payment.
                            </td>
                          </tr>
                        ) : (
                          stmt.lastPayment.feeLines.map((fl, i) => (
                            <tr key={`${stmt.lastPayment!.paymentId}-${i}`}>
                              <td style={styles.tdSm}>{fl.particulars}</td>
                              <td style={{ ...styles.tdSm, textAlign: 'right' }}>{money(fl.amount)}</td>
                            </tr>
                          ))
                        )}
                      </tbody>
                    </table>
                  </>
                ) : (
                  <p style={styles.muted}>No payments recorded against this invoice yet.</p>
                )}
              </>
            ) : null}
            <div style={styles.modalActions}>
              <button
                type="button"
                style={styles.btnPrimary}
                onClick={() => {
                  setStmtModalId(null)
                  setStmt(null)
                }}
              >
                Close
              </button>
            </div>
          </div>
        </div>
      ) : null}
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
    background: t.surface,
  },
  btnPrimary: {
    padding: '10px 20px',
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
  muted: { color: t.textMuted, fontSize: 14, lineHeight: 1.55, margin: 0 },
  error: { color: t.danger, marginBottom: 12, fontSize: 14 },
  spin: { padding: 40, display: 'flex', justifyContent: 'center' },
  tableWrap: {
    background: t.surface,
    borderRadius: t.radius,
    border: `1px solid ${t.border}`,
    overflow: 'auto',
    boxShadow: t.shadow,
  },
  table: { width: '100%', borderCollapse: 'collapse', fontSize: 14, minWidth: 720 },
  th: {
    textAlign: 'left',
    padding: '12px 14px',
    background: t.bgPage,
    color: t.textSecondary,
    fontSize: 11,
    fontWeight: 600,
    textTransform: 'uppercase',
    letterSpacing: '0.05em',
    whiteSpace: 'nowrap',
    borderBottom: `1px solid ${t.border}`,
  },
  td: { padding: '12px 14px', borderBottom: `1px solid ${t.border}`, verticalAlign: 'top', color: t.text },
  tdMuted: { padding: '32px 14px', color: t.textMuted, textAlign: 'center', fontSize: 14 },
  modalWrap: {
    position: 'fixed',
    inset: 0,
    background: 'rgba(15, 23, 42, 0.5)',
    backdropFilter: 'blur(4px)',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    zIndex: 50,
    padding: 20,
  },
  modal: {
    width: '100%',
    maxWidth: 460,
    background: t.surface,
    borderRadius: t.radiusLg,
    padding: '28px 26px',
    boxShadow: '0 25px 50px -12px rgba(0,0,0,0.25)',
    border: `1px solid ${t.border}`,
  },
  modalWide: {
    width: '100%',
    maxWidth: 640,
    maxHeight: '90vh',
    overflowY: 'auto',
    background: t.surface,
    borderRadius: t.radiusLg,
    padding: '28px 26px',
    boxShadow: '0 25px 50px -12px rgba(0,0,0,0.25)',
    border: `1px solid ${t.border}`,
  },
  btnLink: {
    background: 'none',
    border: 'none',
    color: t.accent,
    fontWeight: 600,
    fontSize: 14,
    cursor: 'pointer',
    padding: 0,
    textDecoration: 'underline',
  },
  h4: { margin: '16px 0 8px', fontSize: 14, fontWeight: 700, color: t.text },
  stmtTable: { width: '100%', borderCollapse: 'collapse', fontSize: 13, marginBottom: 8 },
  thSm: {
    textAlign: 'left',
    padding: '8px 10px',
    background: t.bgPage,
    color: t.textSecondary,
    fontSize: 10,
    fontWeight: 600,
    textTransform: 'uppercase',
    borderBottom: `1px solid ${t.border}`,
  },
  tdSm: { padding: '8px 10px', borderBottom: `1px solid ${t.border}`, color: t.text },
  h3: { margin: '0 0 8px', fontSize: 18, fontWeight: 700, color: t.text },
  hint: { margin: '0 0 16px', fontSize: 13, color: t.textMuted, lineHeight: 1.5 },
  field: {
    display: 'flex',
    flexDirection: 'column',
    gap: 8,
    marginBottom: 14,
    fontSize: 12,
    fontWeight: 600,
    color: t.textSecondary,
  },
  input: {
    padding: '10px 12px',
    borderRadius: 8,
    border: `1px solid ${t.borderStrong}`,
    fontSize: 14,
    fontFamily: t.fontSans,
  },
  modalActions: { display: 'flex', justifyContent: 'flex-end', gap: 10, marginTop: 20 },
}
