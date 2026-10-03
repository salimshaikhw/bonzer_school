import { useCallback, useEffect, useMemo, useState } from 'react'
import { AdmissionOrgScopeBanner } from '../../../admission/components/AdmissionOrgScopeBanner'
import { referenceDataApi, type ClassItem, type DivisionItem } from '../../../admission/api/referenceData.api'
import { branchesInGroupScope } from '../../../admission/utils/loadStudentsForOrgTree'
import { useOrgHierarchy } from '../../../../layouts/OrgHierarchyProvider'
import { Button, Spinner } from '../../../../shared'
import {
  feeHeadsMasterApi,
  feeStructuresMasterApi,
  type FeeHeadRow,
  type FeeStructureRow,
} from '../../api/feeMasters.api'
import { FinancePageHeader } from '../../ui/FinancePageHeader'
import { apiErrMessage, money, ms } from './feeMasterShared'

function norm(s: string | undefined | null): string {
  return String(s ?? '')
    .trim()
    .toLowerCase()
}

export function FeeStructuresMasterPage() {
  const org = useOrgHierarchy()
  const [rows, setRows] = useState<FeeStructureRow[]>([])
  const [heads, setHeads] = useState<FeeHeadRow[]>([])
  const [allClasses, setAllClasses] = useState<ClassItem[]>([])
  const [divisions, setDivisions] = useState<DivisionItem[]>([])
  const [allDivisions, setAllDivisions] = useState<DivisionItem[]>([])
  const [loading, setLoading] = useState(true)
  const [err, setErr] = useState<string | null>(null)
  const [msg, setMsg] = useState<string | null>(null)

  const [classId, setClassId] = useState('')
  const [divisionId, setDivisionId] = useState('')
  const [feeHeadId, setFeeHeadId] = useState('')
  const [amount, setAmount] = useState('')
  const [editingId, setEditingId] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)

  const branches = org.branches

  const scopedClasses = useMemo(() => {
    if (org.branchId.trim()) {
      return allClasses.filter((c) => norm(c.branchId) === norm(org.branchId))
    }
    if (org.groupScopeId.trim()) {
      const inGroup = branchesInGroupScope(org.groupScopeId, branches)
      const bids = new Set(inGroup.map((b) => b.id.toLowerCase()))
      return allClasses.filter((c) => c.branchId && bids.has(c.branchId.toLowerCase()))
    }
    return allClasses
  }, [allClasses, org.branchId, org.groupScopeId, branches])

  const visibleClassIds = useMemo(() => {
    if (!org.branchId.trim() && !org.groupScopeId.trim()) return null
    return new Set(scopedClasses.map((c) => c.id.toLowerCase()))
  }, [org.branchId, org.groupScopeId, scopedClasses])

  const filteredRows = useMemo(() => {
    if (!visibleClassIds) return rows
    return rows.filter((r) => r.classId && visibleClassIds.has(r.classId.toLowerCase()))
  }, [rows, visibleClassIds])

  const load = useCallback(() => {
    setLoading(true)
    setErr(null)
    void Promise.all([
      feeStructuresMasterApi.getAll(),
      feeHeadsMasterApi.getAll(),
      referenceDataApi.getClasses(),
      referenceDataApi.getDivisions(),
    ])
      .then(([r, h, c, d]) => {
        setRows(r)
        setHeads(h)
        setAllClasses(c)
        setAllDivisions(d)
      })
      .catch((e) => setErr(apiErrMessage(e)))
      .finally(() => setLoading(false))
  }, [])

  useEffect(() => {
    load()
  }, [load])

  useEffect(() => {
    const cid = classId.trim()
    if (!cid) {
      setDivisions([])
      return
    }
    void referenceDataApi.getDivisionsByClass(cid).then(setDivisions)
  }, [classId])

  useEffect(() => {
    if (org.classId.trim() && scopedClasses.some((c) => c.id === org.classId)) {
      setClassId(org.classId)
    }
  }, [org.classId, scopedClasses])

  const resetForm = () => {
    setEditingId(null)
    setDivisionId('')
    setFeeHeadId('')
    setAmount('')
    if (!org.classId.trim()) setClassId('')
  }

  const onEdit = (r: FeeStructureRow) => {
    setEditingId(r.id)
    setClassId(r.classId ?? '')
    setDivisionId(r.divisionId ?? '')
    setFeeHeadId(r.feeHeadId ?? '')
    setAmount(r.amount != null ? String(r.amount) : '')
    setMsg(null)
    setErr(null)
  }

  const onSave = async () => {
    const cid = classId.trim()
    const fh = feeHeadId.trim()
    const am = Number(amount)
    if (!cid || !fh || !Number.isFinite(am)) {
      setErr('Class, fee head, and a numeric amount are required.')
      return
    }
    setSaving(true)
    setErr(null)
    setMsg(null)
    try {
      const body = {
        classId: cid,
        divisionId: divisionId.trim() || null,
        feeHeadId: fh,
        amount: am,
      }
      if (editingId) await feeStructuresMasterApi.update(editingId, body)
      else await feeStructuresMasterApi.create(body)
      setMsg(editingId ? 'Updated.' : 'Created.')
      resetForm()
      load()
    } catch (e) {
      setErr(apiErrMessage(e))
    } finally {
      setSaving(false)
    }
  }

  const onDelete = async (id: string) => {
    if (!window.confirm('Delete this fee structure row?')) return
    setErr(null)
    setMsg(null)
    try {
      await feeStructuresMasterApi.delete(id)
      setMsg('Deleted.')
      if (editingId === id) resetForm()
      load()
    } catch (e) {
      setErr(apiErrMessage(e))
    }
  }

  const headLabel = (id: string | undefined) =>
    id ? (heads.find((h) => h.id === id)?.name ?? id.slice(0, 8)) : '—'
  const classLabel = (id: string | undefined) =>
    id ? (allClasses.find((c) => c.id === id)?.name ?? id.slice(0, 8)) : '—'
  const divLabel = (id: string | undefined) =>
    id ? (allDivisions.find((d) => d.id === id)?.name ?? id.slice(0, 8)) : '—'

  return (
    <div>
      <AdmissionOrgScopeBanner />
      <FinancePageHeader
        title="Fee structure"
        description="Optional per-class (and division) default amounts by fee head. Use together with class fee mapping for the academic year."
      />

      <div style={ms.card}>
        <h2 style={ms.h2}>{editingId ? 'Edit row' : 'New row'}</h2>
        {err ? <p style={ms.err}>{err}</p> : null}
        {msg ? <p style={ms.ok}>{msg}</p> : null}
        <div style={ms.formGrid}>
          <label style={ms.lab}>
            Class *
            <select style={ms.select} value={classId} onChange={(e) => setClassId(e.target.value)}>
              <option value="">— Select —</option>
              {(org.branchId.trim() || org.groupScopeId.trim() ? scopedClasses : allClasses).map((c) => (
                <option key={c.id} value={c.id}>
                  {c.name ?? c.id.slice(0, 8)}
                </option>
              ))}
            </select>
          </label>
          <label style={ms.lab}>
            Division
            <select style={ms.select} value={divisionId} onChange={(e) => setDivisionId(e.target.value)}>
              <option value="">— All / none —</option>
              {divisions.map((d) => (
                <option key={d.id} value={d.id}>
                  {d.name ?? d.id.slice(0, 8)}
                </option>
              ))}
            </select>
          </label>
          <label style={ms.lab}>
            Fee head *
            <select style={ms.select} value={feeHeadId} onChange={(e) => setFeeHeadId(e.target.value)}>
              <option value="">— Select —</option>
              {heads.map((h) => (
                <option key={h.id} value={h.id}>
                  {h.name ?? h.id.slice(0, 8)}
                </option>
              ))}
            </select>
          </label>
          <label style={ms.lab}>
            Amount (Rs.) *
            <input
              style={ms.input}
              type="text"
              inputMode="decimal"
              value={amount}
              onChange={(e) => setAmount(e.target.value)}
              placeholder="0"
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
        <h2 style={ms.h2}>Rows</h2>
        <p style={ms.desc}>
          {visibleClassIds
            ? 'Filtered by the organization tree (group or school).'
            : 'Showing all rows. Select a group or school in the tree to narrow.'}
        </p>
        {loading ? (
          <Spinner />
        ) : (
          <div style={ms.tableWrap}>
            <table style={ms.table}>
              <thead>
                <tr>
                  <th style={ms.th}>Class</th>
                  <th style={ms.th}>Division</th>
                  <th style={ms.th}>Fee head</th>
                  <th style={{ ...ms.th, textAlign: 'right' }}>Amount</th>
                  <th style={ms.th}>Actions</th>
                </tr>
              </thead>
              <tbody>
                {filteredRows.length === 0 ? (
                  <tr>
                    <td style={ms.td} colSpan={5}>
                      No rows.
                    </td>
                  </tr>
                ) : (
                  filteredRows.map((r) => (
                    <tr key={r.id}>
                      <td style={ms.td}>{classLabel(r.classId)}</td>
                      <td style={ms.td}>{r.divisionId ? divLabel(r.divisionId) : '—'}</td>
                      <td style={ms.td}>{headLabel(r.feeHeadId)}</td>
                      <td style={{ ...ms.td, textAlign: 'right' }}>{money(r.amount)}</td>
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
