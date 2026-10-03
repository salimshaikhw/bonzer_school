import type { CSSProperties, FormEvent } from 'react'
import { useCallback, useEffect, useMemo, useState } from 'react'
import { Navigate } from 'react-router-dom'
import { useAuth } from '../../../core'
import { ROUTES } from '../../../config/routes'
import { Button, Spinner } from '../../../shared'
import { parentFeesApi, type OpenInvoice, type ParentLinkedStudent, type PaymentHistoryRow } from '../api/parentFees.api'
import type { StudentFeeDashboardDto } from '../../admission/api/fees.api'

export function ParentFeesPage() {
  const { appPersona, isAuthenticated } = useAuth()
  const [students, setStudents] = useState<ParentLinkedStudent[]>([])
  const [loadingStudents, setLoadingStudents] = useState(true)
  const [studentsError, setStudentsError] = useState<string | null>(null)

  const [sel, setSel] = useState<ParentLinkedStudent | null>(null)
  const [summary, setSummary] = useState<StudentFeeDashboardDto | null>(null)
  const [openInv, setOpenInv] = useState<OpenInvoice[]>([])
  const [history, setHistory] = useState<PaymentHistoryRow[]>([])
  const [loadingDetail, setLoadingDetail] = useState(false)
  const [detailError, setDetailError] = useState<string | null>(null)

  const [invoiceId, setInvoiceId] = useState('')
  const [amount, setAmount] = useState('')
  const [method, setMethod] = useState('UPI')
  const [refNo, setRefNo] = useState('')
  const [payMsg, setPayMsg] = useState<string | null>(null)
  const [paying, setPaying] = useState(false)

  const academicYearId = sel?.academicYearId?.trim() || ''

  const loadStudents = useCallback(async () => {
    setLoadingStudents(true)
    setStudentsError(null)
    try {
      const rows = await parentFeesApi.getMyStudents()
      setStudents(rows)
      if (rows.length === 1) setSel(rows[0])
    } catch {
      setStudentsError('Could not load your children. Sign in as a parent or check the API.')
      setStudents([])
    } finally {
      setLoadingStudents(false)
    }
  }, [])

  useEffect(() => {
    if (!isAuthenticated || appPersona !== 'parent') return
    void loadStudents()
  }, [isAuthenticated, appPersona, loadStudents])

  const loadDetail = useCallback(async () => {
    if (!sel?.studentId || !academicYearId) {
      setSummary(null)
      setOpenInv([])
      setHistory([])
      return
    }
    setLoadingDetail(true)
    setDetailError(null)
    try {
      const [sum, inv, pay] = await Promise.all([
        parentFeesApi.getFeeSummary(sel.studentId, academicYearId),
        parentFeesApi.getOpenInvoices(sel.studentId, academicYearId),
        parentFeesApi.getPayments(sel.studentId, academicYearId),
      ])
      setSummary(sum)
      setOpenInv(inv)
      setHistory(pay)
      setInvoiceId('')
      setAmount('')
      setPayMsg(null)
      if (inv.length === 1) {
        setInvoiceId(inv[0].id)
        setAmount(String(inv[0].remainingAmount))
      }
    } catch {
      setDetailError('Could not load fee details. Ensure academic year is set on the student and fees are configured.')
      setSummary(null)
      setOpenInv([])
      setHistory([])
    } finally {
      setLoadingDetail(false)
    }
  }, [sel, academicYearId])

  useEffect(() => {
    void loadDetail()
  }, [loadDetail])

  const selectedInvoice = useMemo(
    () => openInv.find((i) => i.id === invoiceId),
    [openInv, invoiceId],
  )

  if (!isAuthenticated) return <Navigate to={ROUTES.LOGIN} replace />
  if (appPersona !== 'parent') return <Navigate to={ROUTES.HOME} replace />

  async function onPay(e: FormEvent) {
    e.preventDefault()
    setPayMsg(null)
    if (!sel?.studentId || !academicYearId) return
    const inv = selectedInvoice
    if (!inv) {
      setPayMsg('Choose an invoice.')
      return
    }
    const amt = Number(amount)
    if (!Number.isFinite(amt) || amt <= 0) {
      setPayMsg('Enter a valid amount.')
      return
    }
    if (amt > inv.remainingAmount + 0.01) {
      setPayMsg('Amount is more than the invoice balance.')
      return
    }

    const openLines = inv.lines.filter((l) => l.remainingAmount > 0).sort((a, b) => a.remainingAmount - b.remainingAmount)
    let left = amt
    const feeLines: {
      invoiceItemId: string
      feeHeadId: string
      amount: number
    }[] = []
    for (const l of openLines) {
      if (left <= 0) break
      const take = Math.min(l.remainingAmount, left)
      if (take <= 0) continue
      if (!l.invoiceItemId?.trim()) {
        setPayMsg('Invoice line is missing an id; refresh and try again.')
        return
      }
      feeLines.push({
        invoiceItemId: l.invoiceItemId,
        feeHeadId: l.feeHeadId,
        amount: take,
      })
      left -= take
    }
    if (left > 0.01) {
      setPayMsg('Could not allocate this amount across the open invoice lines.')
      return
    }

    setPaying(true)
    try {
      await parentFeesApi.pay(sel.studentId, {
        invoiceId: inv.id,
        academicYearId,
        paymentMethod: method,
        transactionReference: refNo || undefined,
        feeLines,
      })
      setPayMsg('Payment recorded.')
      setRefNo('')
      await loadDetail()
    } catch (err: unknown) {
      const msg =
        err && typeof err === 'object' && 'response' in err
          ? String((err as { response?: { data?: unknown } }).response?.data ?? 'Payment failed')
          : 'Payment failed.'
      setPayMsg(msg)
    } finally {
      setPaying(false)
    }
  }

  return (
    <div style={styles.page}>
      <h1 style={styles.h1}>Pay fees</h1>
      <p style={styles.sub}>Record a payment against a school invoice. History is fee-aware when the school uses invoice lines.</p>

      {loadingStudents ? (
        <Spinner />
      ) : studentsError ? (
        <p style={styles.err}>{studentsError}</p>
      ) : students.length === 0 ? (
        <p style={styles.muted}>
          No students linked to your account. The school must set <code style={styles.code}>parent_users.Username</code> to
          your login email (same as you use here). For the demo dataset, run{' '}
          <code style={styles.code}>Scripts/link_scenario_parent_identity.sql</code> after seeding admission.
        </p>
      ) : (
        <>
          <label style={styles.lab}>
            Child
            <select
              style={styles.select}
              value={sel?.studentId ?? ''}
              onChange={(ev) => {
                const st = students.find((s) => s.studentId === ev.target.value) ?? null
                setSel(st)
              }}
            >
              {students.map((s) => (
                <option key={s.studentId} value={s.studentId}>
                  {(s.firstName ?? '') + ' ' + (s.lastName ?? '')} — {s.admissionNo ?? s.studentId}
                </option>
              ))}
            </select>
          </label>

          {!academicYearId ? (
            <p style={styles.warn}>This student has no academic year on file; fee summary cannot load.</p>
          ) : loadingDetail ? (
            <Spinner />
          ) : detailError ? (
            <p style={styles.err}>{detailError}</p>
          ) : (
            <>
              {summary ? (
                <section style={styles.card}>
                  <h2 style={styles.h2}>Summary ({academicYearId.slice(0, 8)}…)</h2>
                  <div style={styles.kpiRow}>
                    <span>Total {summary.totalFees.toLocaleString()}</span>
                    <span>Paid {summary.paidFees.toLocaleString()}</span>
                    <span style={{ fontWeight: 700 }}>Due {summary.remainingFees.toLocaleString()}</span>
                  </div>
                  {summary.feeHeads.length > 0 && (
                    <table style={styles.table}>
                      <thead>
                        <tr>
                          <th style={styles.th}>Fee head</th>
                          <th style={styles.th}>Total</th>
                          <th style={styles.th}>Paid</th>
                          <th style={styles.th}>Remaining</th>
                        </tr>
                      </thead>
                      <tbody>
                        {summary.feeHeads.map((h) => (
                          <tr key={h.feeHeadId}>
                            <td style={styles.td}>{h.feeHeadName}</td>
                            <td style={styles.td}>{h.totalAmount.toLocaleString()}</td>
                            <td style={styles.td}>{h.paidAmount.toLocaleString()}</td>
                            <td style={{ ...styles.td, fontWeight: h.remainingAmount > 0 ? 700 : 400 }}>
                              {h.remainingAmount.toLocaleString()}
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  )}
                  {summary.installments.length > 0 && (
                    <div style={{ marginTop: 14 }}>
                      <h3 style={styles.h3}>Installments</h3>
                      <table style={styles.table}>
                        <thead>
                          <tr>
                            <th style={styles.th}>Installment</th>
                            <th style={styles.th}>Total</th>
                            <th style={styles.th}>Paid</th>
                            <th style={styles.th}>Due</th>
                          </tr>
                        </thead>
                        <tbody>
                          {summary.installments.map((x) => (
                            <tr key={x.installmentId}>
                              <td style={styles.td}>{x.installmentName}</td>
                              <td style={styles.td}>{x.totalAmount.toLocaleString()}</td>
                              <td style={styles.td}>{x.paidAmount.toLocaleString()}</td>
                              <td style={styles.td}>{x.remainingAmount.toLocaleString()}</td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  )}
                </section>
              ) : null}

              <section style={styles.card}>
                <h2 style={styles.h2}>Open invoices</h2>
                {openInv.length === 0 ? (
                  <p style={styles.muted}>No unpaid invoices in this academic year.</p>
                ) : (
                  <ul style={styles.list}>
                    {openInv.map((i) => (
                      <li key={i.id} style={styles.li}>
                        <strong>{i.invoiceNo ?? i.id.slice(0, 8)}</strong> — Due{' '}
                        {i.remainingAmount.toLocaleString()}
                        {i.lines.length > 0 && (
                          <div style={styles.indent}>
                            {i.lines.map((l) => (
                              <div key={l.feeHeadId}>
                                {l.feeHeadName}: {l.remainingAmount.toLocaleString()} outstanding
                              </div>
                            ))}
                          </div>
                        )}
                      </li>
                    ))}
                  </ul>
                )}
              </section>

              <section style={styles.card}>
                <h2 style={styles.h2}>Record payment</h2>
                <form style={styles.form} onSubmit={onPay}>
                  <label style={styles.lab}>
                    Invoice
                    <select
                      style={styles.select}
                      value={invoiceId}
                      onChange={(ev) => {
                        setInvoiceId(ev.target.value)
                        const inv = openInv.find((x) => x.id === ev.target.value)
                        if (inv) setAmount(String(inv.remainingAmount))
                      }}
                      required
                    >
                      <option value="">Select…</option>
                      {openInv.map((i) => (
                        <option key={i.id} value={i.id}>
                          {i.invoiceNo ?? i.id} — {i.remainingAmount.toLocaleString()} due
                        </option>
                      ))}
                    </select>
                  </label>
                  <label style={styles.lab}>
                    Amount
                    <input
                      style={styles.input}
                      type="number"
                      step="0.01"
                      min="0"
                      value={amount}
                      onChange={(ev) => setAmount(ev.target.value)}
                      required
                    />
                  </label>
                  <label style={styles.lab}>
                    Method
                    <select style={styles.select} value={method} onChange={(ev) => setMethod(ev.target.value)}>
                      <option value="UPI">UPI</option>
                      <option value="Cash">Cash</option>
                      <option value="Bank">Bank transfer</option>
                      <option value="Card">Card</option>
                    </select>
                  </label>
                  <label style={styles.lab}>
                    Reference (optional)
                    <input style={styles.input} value={refNo} onChange={(ev) => setRefNo(ev.target.value)} />
                  </label>
                  {payMsg ? <p style={payMsg.includes('failed') || payMsg.includes('Service') ? styles.err : styles.ok}>{payMsg}</p> : null}
                  <Button type="submit" disabled={paying || openInv.length === 0}>
                    {paying ? 'Saving…' : 'Submit payment'}
                  </Button>
                </form>
              </section>

              <section style={styles.card}>
                <h2 style={styles.h2}>Payment history</h2>
                {history.length === 0 ? (
                  <p style={styles.muted}>No payments in this academic year.</p>
                ) : (
                  <table style={styles.table}>
                    <thead>
                      <tr>
                        <th style={styles.th}>Date</th>
                        <th style={styles.th}>Amount</th>
                        <th style={styles.th}>Method</th>
                        <th style={styles.th}>Ref</th>
                      </tr>
                    </thead>
                    <tbody>
                      {history.map((p) => (
                        <tr key={p.paymentId}>
                          <td style={styles.td}>{p.paymentDate ?? '—'}</td>
                          <td style={styles.td}>{p.amount != null ? p.amount.toLocaleString() : '—'}</td>
                          <td style={styles.td}>{p.paymentMethod ?? '—'}</td>
                          <td style={styles.td}>{p.transactionNumber ?? '—'}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                )}
              </section>
            </>
          )}
        </>
      )}
    </div>
  )
}

const styles: Record<string, CSSProperties> = {
  page: { maxWidth: 720 },
  h1: { margin: '0 0 8px', fontSize: 22, fontWeight: 800, color: '#0f172a' },
  h2: { margin: '0 0 12px', fontSize: 16, fontWeight: 800, color: '#0f172a' },
  h3: { margin: '0 0 8px', fontSize: 14, fontWeight: 700, color: '#334155' },
  sub: { margin: '0 0 20px', color: '#64748b', fontSize: 14, lineHeight: 1.45 },
  card: {
    background: '#fff',
    border: '1px solid #e2e8f0',
    borderRadius: 12,
    padding: 16,
    marginBottom: 16,
  },
  lab: { display: 'flex', flexDirection: 'column', gap: 6, fontSize: 13, fontWeight: 600, color: '#334155', marginBottom: 12 },
  select: { padding: '8px 10px', borderRadius: 8, border: '1px solid #cbd5e1', fontSize: 14 },
  input: { padding: '8px 10px', borderRadius: 8, border: '1px solid #cbd5e1', fontSize: 14 },
  form: { maxWidth: 400 },
  kpiRow: { display: 'flex', gap: 16, flexWrap: 'wrap', fontSize: 14, color: '#334155', marginBottom: 12 },
  table: { width: '100%', borderCollapse: 'collapse', fontSize: 13 },
  th: { textAlign: 'left', padding: '8px', borderBottom: '1px solid #e2e8f0', color: '#64748b' },
  td: { padding: '8px', borderBottom: '1px solid #f1f5f9' },
  list: { margin: 0, paddingLeft: 18, color: '#334155', fontSize: 14 },
  li: { marginBottom: 10 },
  indent: { marginTop: 6, marginLeft: 8, fontSize: 12, color: '#64748b' },
  muted: { color: '#64748b', fontSize: 14, lineHeight: 1.5 },
  err: { color: '#b91c1c', fontSize: 14 },
  ok: { color: '#047857', fontSize: 14 },
  warn: { color: '#a16207', fontSize: 14 },
  code: { fontSize: 12, background: '#f1f5f9', padding: '2px 6px', borderRadius: 4 },
}
