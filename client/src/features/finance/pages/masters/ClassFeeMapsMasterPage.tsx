import { useCallback, useEffect, useMemo, useState } from 'react'
import { AdmissionOrgScopeBanner } from '../../../admission/components/AdmissionOrgScopeBanner'
import { referenceDataApi, type ClassItem, type DivisionItem } from '../../../admission/api/referenceData.api'
import { branchesInGroupScope } from '../../../admission/utils/loadStudentsForOrgTree'
import { useOrgHierarchy } from '../../../../layouts/OrgHierarchyProvider'
import { Button, Spinner } from '../../../../shared'
import { academicYearsApi } from '../../api/academicYears.api'
import {
  classFeeMapsMasterApi,
  feeHeadsMasterApi,
  type ClassFeeMapRow,
  type FeeHeadRow,
} from '../../api/feeMasters.api'
import { FinancePageHeader } from '../../ui/FinancePageHeader'
import { apiErrMessage, money, ms } from './feeMasterShared'

function norm(s: string | undefined | null): string {
  return String(s ?? '')
    .trim()
    .toLowerCase()
}

export function ClassFeeMapsMasterPage() {
  const org = useOrgHierarchy()
  const [rows, setRows] = useState<ClassFeeMapRow[]>([])
  const [heads, setHeads] = useState<FeeHeadRow[]>([])
  const [allClasses, setAllClasses] = useState<ClassItem[]>([])
  const [allDivisions, setAllDivisions] = useState<DivisionItem[]>([])
  const [years, setYears] = useState<{ id: string; label: string }[]>([])
  const [divisions, setDivisions] = useState<DivisionItem[]>([])
  const [loading, setLoading] = useState(true)
  const [err, setErr] = useState<string | null>(null)
  const [msg, setMsg] = useState<string | null>(null)

  const [classId, setClassId] = useState('')
  const [divisionId, setDivisionId] = useState('')
  const [academicYearId, setAcademicYearId] = useState('')
  const [feeHeadId, setFeeHeadId] = useState('')
  const [amount, setAmount] = useState('')
  const [installmentNo, setInstallmentNo] = useState('')
  const [academicYearLabel, setAcademicYearLabel] = useState('')
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
      classFeeMapsMasterApi.getAll(),
      feeHeadsMasterApi.getAll(),
      referenceDataApi.getClasses(),
      referenceDataApi.getDivisions(),
      academicYearsApi.getAll(),
    ])
      .then(([maps, h, c, d, ay]) => {
        setRows(maps)
        setHeads(h)
        setAllClasses(c)
        setAllDivisions(d)
        setYears(
          ay.map((y) => ({
            id: y.id,
            label: [y.yearName, y.startDate].filter(Boolean).join(' · ') || y.id.slice(0, 8),
          })),
        )
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
    setAcademicYearId('')
    setFeeHeadId('')
    setAmount('')
    setInstallmentNo('')
    setAcademicYearLabel('')
    if (!org.classId.trim()) setClassId('')
  }

  const onEdit = (r: ClassFeeMapRow) => {
    setEditingId(r.id)
    setClassId(r.classId ?? '')
    setDivisionId(r.divisionId ?? '')
    setAcademicYearId(r.academicYearId ?? '')
    setFeeHeadId(r.feeHeadId ?? '')
    setAmount(r.amount != null ? String(r.amount) : '')
    setInstallmentNo(r.installmentNo != null ? String(r.installmentNo) : '')
    setAcademicYearLabel(r.academicYear ?? '')
    setMsg(null)
    setErr(null)
  }

  const onSave = async () => {
    const cid = classId.trim()
    const ay = academicYearId.trim()
    const fh = feeHeadId.trim()
    const am = Number(amount)
    if (!cid || !ay || !fh || !Number.isFinite(am)) {
      setErr('Class, academic year, fee head, and amount are required.')
      return
    }
    const instParsed = installmentNo.trim() === '' ? null : Number(installmentNo)
    if (instParsed != null && !Number.isFinite(instParsed)) {
      setErr('Installment no. must be a number or empty.')
      return
    }
    setSaving(true)
    setErr(null)
    setMsg(null)
    try {
      const body = {
        classId: cid,
        divisionId: divisionId.trim() || null,
        academicYearId: ay,
        feeHeadId: fh,
        amount: am,
        installmentNo: instParsed != null ? Math.trunc(instParsed) : null,
        academicYear: academicYearLabel.trim() || null,
      }
      if (editingId) await classFeeMapsMasterApi.update(editingId, body)
      else await classFeeMapsMasterApi.create(body)
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
    if (!window.confirm('Delete this class fee map?')) return
    setErr(null)
    setMsg(null)
    try {
      await classFeeMapsMasterApi.delete(id)
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
  const yearLabel = (id: string | undefined) =>
    id ? (years.find((y) => y.id === id)?.label ?? id.slice(0, 8)) : '—'

  return (
    <div>
      <AdmissionOrgScopeBanner />
      <FinancePageHeader
        title="Class fee mapping"
        description="Map fee heads and amounts to a class (and optional division) for an academic year. Drives student fee dashboards and invoice-from-schedule lines."
      />

      <div style={ms.card}>
        <h2 style={ms.h2}>{editingId ? 'Edit mapping' : 'New mapping'}</h2>
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
            />
          </label>
          <label style={ms.lab}>
            Installment no.
            <input
              style={ms.input}
              type="number"
              min={1}
              value={installmentNo}
              onChange={(e) => setInstallmentNo(e.target.value)}
              placeholder="Optional"
            />
          </label>
          <label style={ms.lab}>
            Academic year label
            <input
              style={ms.input}
              value={academicYearLabel}
              onChange={(e) => setAcademicYearLabel(e.target.value)}
              placeholder="Optional display text"
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
        <h2 style={ms.h2}>Mappings</h2>
        <p style={ms.desc}>
          {visibleClassIds
            ? 'Filtered by the organization tree.'
            : 'Showing all mappings. Select a group or school to narrow.'}
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
                  <th style={ms.th}>Year</th>
                  <th style={ms.th}>Fee head</th>
                  <th style={{ ...ms.th, textAlign: 'right' }}>Amount</th>
                  <th style={ms.th}>Inst.</th>
                  <th style={ms.th}>Actions</th>
                </tr>
              </thead>
              <tbody>
                {filteredRows.length === 0 ? (
                  <tr>
                    <td style={ms.td} colSpan={7}>
                      No mappings.
                    </td>
                  </tr>
                ) : (
                  filteredRows.map((r) => (
                    <tr key={r.id}>
                      <td style={ms.td}>{classLabel(r.classId)}</td>
                      <td style={ms.td}>{r.divisionId ? divLabel(r.divisionId) : '—'}</td>
                      <td style={ms.td}>{yearLabel(r.academicYearId)}</td>
                      <td style={ms.td}>{headLabel(r.feeHeadId)}</td>
                      <td style={{ ...ms.td, textAlign: 'right' }}>{money(r.amount)}</td>
                      <td style={ms.td}>{r.installmentNo ?? '—'}</td>
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
