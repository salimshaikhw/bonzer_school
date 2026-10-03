import { useCallback, useEffect, useMemo, useState } from 'react'
import { AdmissionOrgScopeBanner } from '../../../admission/components/AdmissionOrgScopeBanner'
import { referenceDataApi, type ClassItem } from '../../../admission/api/referenceData.api'
import { branchesInGroupScope } from '../../../admission/utils/loadStudentsForOrgTree'
import { useOrgHierarchy } from '../../../../layouts/OrgHierarchyProvider'
import { Button, Spinner } from '../../../../shared'
import { academicYearsApi } from '../../api/academicYears.api'
import { feeInstallmentsMasterApi, type FeeInstallmentRow } from '../../api/feeMasters.api'
import { FinancePageHeader } from '../../ui/FinancePageHeader'
import { apiErrMessage, ms } from './feeMasterShared'

export function FeeInstallmentsMasterPage() {
  const org = useOrgHierarchy()
  const [rows, setRows] = useState<FeeInstallmentRow[]>([])
  const [classes, setClasses] = useState<ClassItem[]>([])
  const [allClasses, setAllClasses] = useState<ClassItem[]>([])
  const [years, setYears] = useState<{ id: string; label: string }[]>([])
  const [loading, setLoading] = useState(true)
  const [err, setErr] = useState<string | null>(null)
  const [msg, setMsg] = useState<string | null>(null)

  const [branchId, setBranchId] = useState('')
  const [classId, setClassId] = useState('')
  const [academicYearId, setAcademicYearId] = useState('')
  const [installmentNo, setInstallmentNo] = useState('1')
  const [installmentName, setInstallmentName] = useState('')
  const [dueDate, setDueDate] = useState('')
  const [lateFeeAfter, setLateFeeAfter] = useState('')
  const [editingId, setEditingId] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)

  const branches = org.branches

  const loadRefs = useCallback(async () => {
    const ay = await academicYearsApi.getAll()
    setYears(
      ay.map((y) => ({
        id: y.id,
        label: [y.yearName, y.startDate].filter(Boolean).join(' · ') || y.id.slice(0, 8),
      })),
    )
  }, [])

  const loadClassesForBranch = useCallback(async (bid: string) => {
    if (!bid.trim()) {
      setClasses([])
      return
    }
    const list = await referenceDataApi.getClassesByBranch(bid.trim())
    setClasses(list)
  }, [])

  const load = useCallback(() => {
    setLoading(true)
    setErr(null)
    void feeInstallmentsMasterApi
      .getAll()
      .then(setRows)
      .catch((e) => setErr(apiErrMessage(e)))
      .finally(() => setLoading(false))
  }, [])

  useEffect(() => {
    void loadRefs()
    void referenceDataApi.getClasses().then(setAllClasses)
  }, [loadRefs])

  useEffect(() => {
    load()
  }, [load])

  useEffect(() => {
    const b = org.branchId.trim()
    if (b && !branchId) setBranchId(b)
  }, [org.branchId, branchId])

  useEffect(() => {
    void loadClassesForBranch(branchId)
  }, [branchId, loadClassesForBranch])

  const filteredRows = useMemo(() => {
    if (org.branchId.trim()) {
      return rows.filter(
        (r) => String(r.branchId ?? '').toLowerCase() === org.branchId.trim().toLowerCase(),
      )
    }
    if (org.groupScopeId.trim()) {
      const inGroup = branchesInGroupScope(org.groupScopeId, branches)
      const ids = new Set(inGroup.map((b) => b.id.toLowerCase()))
      return rows.filter((r) => r.branchId && ids.has(r.branchId.toLowerCase()))
    }
    return rows
  }, [rows, org.branchId, org.groupScopeId, branches])

  const classLabel = useCallback(
    (id: string | undefined) => {
      if (!id) return '—'
      const c = allClasses.find((x) => x.id === id) ?? classes.find((x) => x.id === id)
      return c?.name ?? id.slice(0, 8)
    },
    [allClasses, classes],
  )

  const resetForm = () => {
    setEditingId(null)
    setClassId('')
    setAcademicYearId('')
    setInstallmentNo('1')
    setInstallmentName('')
    setDueDate('')
    setLateFeeAfter('')
  }

  const onEdit = (r: FeeInstallmentRow) => {
    setEditingId(r.id)
    setBranchId(r.branchId ?? org.branchId ?? branches[0]?.id ?? '')
    setClassId(r.classId ?? '')
    setAcademicYearId(r.academicYearId ?? '')
    setInstallmentNo(String(r.installmentNo ?? 1))
    setInstallmentName(r.installmentName ?? '')
    setDueDate(r.dueDate?.slice(0, 10) ?? '')
    setLateFeeAfter(r.lateFeeAfter?.slice(0, 10) ?? '')
    setMsg(null)
    setErr(null)
  }

  const onSave = async () => {
    const bid = branchId.trim()
    const cid = classId.trim()
    const ay = academicYearId.trim()
    const no = Number(installmentNo)
    const nm = installmentName.trim()
    const due = dueDate.trim()
    if (!bid || !cid || !ay || !Number.isFinite(no) || !nm || !due) {
      setErr('Branch, class, academic year, installment no., name, and due date are required.')
      return
    }
    setSaving(true)
    setErr(null)
    setMsg(null)
    try {
      const body = {
        branchId: bid,
        classId: cid,
        academicYearId: ay,
        installmentNo: Math.trunc(no),
        installmentName: nm,
        dueDate: due,
        lateFeeAfter: lateFeeAfter.trim() || null,
      }
      if (editingId) await feeInstallmentsMasterApi.update(editingId, body)
      else await feeInstallmentsMasterApi.create(body)
      setMsg(editingId ? 'Installment updated.' : 'Installment created.')
      resetForm()
      if (org.branchId.trim()) setBranchId(org.branchId.trim())
      else if (branches[0]) setBranchId(branches[0].id)
      load()
    } catch (e) {
      setErr(apiErrMessage(e))
    } finally {
      setSaving(false)
    }
  }

  const onDelete = async (id: string) => {
    if (!window.confirm('Delete this installment row?')) return
    setErr(null)
    setMsg(null)
    try {
      await feeInstallmentsMasterApi.delete(id)
      setMsg('Deleted.')
      if (editingId === id) resetForm()
      load()
    } catch (e) {
      setErr(apiErrMessage(e))
    }
  }

  const branchOptions = branches.length > 0 ? branches : []

  return (
    <div>
      <AdmissionOrgScopeBanner />
      <FinancePageHeader
        title="Fee installments & due dates"
        description="Define installment sequence, names, due dates, and optional late-fee date per branch, class, and academic year."
      />

      <div style={ms.card}>
        <h2 style={ms.h2}>{editingId ? 'Edit installment' : 'New installment'}</h2>
        {err ? <p style={ms.err}>{err}</p> : null}
        {msg ? <p style={ms.ok}>{msg}</p> : null}
        <div style={ms.formGrid}>
          <label style={ms.lab}>
            Branch *
            <select style={ms.select} value={branchId} onChange={(e) => setBranchId(e.target.value)}>
              <option value="">— Select —</option>
              {branchOptions.map((b) => (
                <option key={b.id} value={b.id}>
                  {b.name ?? b.id.slice(0, 8)}
                </option>
              ))}
            </select>
          </label>
          <label style={ms.lab}>
            Class *
            <select style={ms.select} value={classId} onChange={(e) => setClassId(e.target.value)}>
              <option value="">— Select —</option>
              {classes.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.name ?? c.id.slice(0, 8)}
                </option>
              ))}
            </select>
          </label>
          <label style={ms.lab}>
            Academic year *
            <select style={ms.select} value={academicYearId} onChange={(e) => setAcademicYearId(e.target.value)}>
              <option value="">— Select —</option>
              {years.map((y) => (
                <option key={y.id} value={y.id}>
                  {y.label}
                </option>
              ))}
            </select>
          </label>
          <label style={ms.lab}>
            Installment no. *
            <input
              style={ms.input}
              type="number"
              min={1}
              value={installmentNo}
              onChange={(e) => setInstallmentNo(e.target.value)}
            />
          </label>
          <label style={ms.lab}>
            Name *
            <input
              style={ms.input}
              value={installmentName}
              onChange={(e) => setInstallmentName(e.target.value)}
              placeholder="e.g. Term 1"
            />
          </label>
          <label style={ms.lab}>
            Due date *
            <input style={ms.input} type="date" value={dueDate} onChange={(e) => setDueDate(e.target.value)} />
          </label>
          <label style={ms.lab}>
            Late fee after
            <input style={ms.input} type="date" value={lateFeeAfter} onChange={(e) => setLateFeeAfter(e.target.value)} />
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
        <h2 style={ms.h2}>Installments</h2>
        <p style={ms.desc}>
          {org.branchId.trim()
            ? 'Filtered to the school selected in the organization tree.'
            : 'Showing all installments. Pick a school in the tree to narrow the list.'}
        </p>
        {loading ? (
          <Spinner />
        ) : (
          <div style={ms.tableWrap}>
            <table style={ms.table}>
              <thead>
                <tr>
                  <th style={ms.th}>No.</th>
                  <th style={ms.th}>Name</th>
                  <th style={ms.th}>Due</th>
                  <th style={ms.th}>Late after</th>
                  <th style={ms.th}>Class</th>
                  <th style={ms.th}>Year</th>
                  <th style={ms.th}>Actions</th>
                </tr>
              </thead>
              <tbody>
                {filteredRows.length === 0 ? (
                  <tr>
                    <td style={ms.td} colSpan={7}>
                      No installments.
                    </td>
                  </tr>
                ) : (
                  filteredRows.map((r) => (
                    <tr key={r.id}>
                      <td style={ms.td}>{r.installmentNo ?? '—'}</td>
                      <td style={ms.td}>{r.installmentName ?? '—'}</td>
                      <td style={ms.td}>{r.dueDate?.slice(0, 10) ?? '—'}</td>
                      <td style={ms.td}>{r.lateFeeAfter?.slice(0, 10) ?? '—'}</td>
                      <td style={ms.td}>{classLabel(r.classId)}</td>
                      <td style={ms.td}>
                        {years.find((y) => y.id === r.academicYearId)?.label ?? r.academicYearId?.slice(0, 8) ?? '—'}
                      </td>
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
