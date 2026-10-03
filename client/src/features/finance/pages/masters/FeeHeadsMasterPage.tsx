import { useCallback, useEffect, useState } from 'react'
import { AdmissionOrgScopeBanner } from '../../../admission/components/AdmissionOrgScopeBanner'
import { Button, Spinner } from '../../../../shared'
import { feeHeadsMasterApi, type FeeHeadRow } from '../../api/feeMasters.api'
import { FinancePageHeader } from '../../ui/FinancePageHeader'
import { apiErrMessage, ms } from './feeMasterShared'

export function FeeHeadsMasterPage() {
  const [rows, setRows] = useState<FeeHeadRow[]>([])
  const [loading, setLoading] = useState(true)
  const [err, setErr] = useState<string | null>(null)
  const [msg, setMsg] = useState<string | null>(null)
  const [name, setName] = useState('')
  const [accountCode, setAccountCode] = useState('')
  const [editingId, setEditingId] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)

  const load = useCallback(() => {
    setLoading(true)
    setErr(null)
    void feeHeadsMasterApi
      .getAll()
      .then(setRows)
      .catch((e) => setErr(apiErrMessage(e)))
      .finally(() => setLoading(false))
  }, [])

  useEffect(() => {
    load()
  }, [load])

  const resetForm = () => {
    setEditingId(null)
    setName('')
    setAccountCode('')
  }

  const onEdit = (r: FeeHeadRow) => {
    setEditingId(r.id)
    setName(r.name ?? '')
    setAccountCode(r.accountCode ?? '')
    setMsg(null)
    setErr(null)
  }

  const onSave = async () => {
    const n = name.trim()
    if (!n) {
      setErr('Name is required.')
      return
    }
    setSaving(true)
    setErr(null)
    setMsg(null)
    try {
      if (editingId) {
        await feeHeadsMasterApi.update(editingId, { name: n, accountCode: accountCode.trim() || null })
        setMsg('Fee head updated.')
      } else {
        await feeHeadsMasterApi.create({ name: n, accountCode: accountCode.trim() || null })
        setMsg('Fee head created.')
      }
      resetForm()
      load()
    } catch (e) {
      setErr(apiErrMessage(e))
    } finally {
      setSaving(false)
    }
  }

  const onDelete = async (id: string) => {
    if (!window.confirm('Delete this fee head? Linked maps may break.')) return
    setErr(null)
    setMsg(null)
    try {
      await feeHeadsMasterApi.delete(id)
      setMsg('Deleted.')
      if (editingId === id) resetForm()
      load()
    } catch (e) {
      setErr(apiErrMessage(e))
    }
  }

  return (
    <div>
      <AdmissionOrgScopeBanner />
      <FinancePageHeader
        title="Fee heads"
        description="Ledger-style fee categories (tuition, transport, etc.) used in class mapping and invoices."
      />

      <div style={ms.card}>
        <h2 style={ms.h2}>{editingId ? 'Edit fee head' : 'New fee head'}</h2>
        <p style={ms.desc}>Account code is optional; use it if you export to accounting.</p>
        {err ? <p style={ms.err}>{err}</p> : null}
        {msg ? <p style={ms.ok}>{msg}</p> : null}
        <div style={ms.formGrid}>
          <label style={ms.lab}>
            Name *
            <input style={ms.input} value={name} onChange={(e) => setName(e.target.value)} placeholder="e.g. Tuition" />
          </label>
          <label style={ms.lab}>
            Account code
            <input
              style={ms.input}
              value={accountCode}
              onChange={(e) => setAccountCode(e.target.value)}
              placeholder="Optional"
            />
          </label>
        </div>
        <div style={ms.rowActions}>
          <Button type="button" onClick={() => void onSave()} disabled={saving}>
            {saving ? 'Saving…' : editingId ? 'Update' : 'Create'}
          </Button>
          {editingId ? (
            <Button type="button" variant="secondary" onClick={resetForm}>
              Cancel edit
            </Button>
          ) : null}
        </div>
      </div>

      <div style={ms.card}>
        <h2 style={ms.h2}>All fee heads</h2>
        {loading ? (
          <Spinner />
        ) : (
          <div style={ms.tableWrap}>
            <table style={ms.table}>
              <thead>
                <tr>
                  <th style={ms.th}>Name</th>
                  <th style={ms.th}>Account code</th>
                  <th style={ms.th}>Actions</th>
                </tr>
              </thead>
              <tbody>
                {rows.length === 0 ? (
                  <tr>
                    <td style={ms.td} colSpan={3}>
                      No fee heads yet.
                    </td>
                  </tr>
                ) : (
                  rows.map((r) => (
                    <tr key={r.id}>
                      <td style={ms.td}>{r.name ?? '—'}</td>
                      <td style={ms.td}>{r.accountCode ?? '—'}</td>
                      <td style={ms.td}>
                        <div style={ms.rowActions}>
                          <Button type="button" variant="secondary" onClick={() => onEdit(r)}>
                            Edit
                          </Button>
                          <Button type="button" variant="secondary" onClick={() => void onDelete(r.id)}>
                            Delete
                          </Button>
                        </div>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  )
}
