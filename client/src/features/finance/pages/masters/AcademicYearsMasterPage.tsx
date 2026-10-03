import { useCallback, useEffect, useState } from 'react'
import { AdmissionOrgScopeBanner } from '../../../admission/components/AdmissionOrgScopeBanner'
import { Button, Spinner } from '../../../../shared'
import { academicYearsApi, type AcademicYearUpsert } from '../../api/academicYears.api'
import type { AcademicYearRow } from '../../types'
import { FinancePageHeader } from '../../ui/FinancePageHeader'
import { apiErrMessage, ms } from './feeMasterShared'

export function AcademicYearsMasterPage() {
  const [rows, setRows] = useState<AcademicYearRow[]>([])
  const [loading, setLoading] = useState(true)
  const [err, setErr] = useState<string | null>(null)
  const [msg, setMsg] = useState<string | null>(null)
  const [yearName, setYearName] = useState('')
  const [startDate, setStartDate] = useState('')
  const [endDate, setEndDate] = useState('')
  const [isActive, setIsActive] = useState(true)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)

  const load = useCallback(() => {
    setLoading(true)
    setErr(null)
    void academicYearsApi
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
    setYearName('')
    setStartDate('')
    setEndDate('')
    setIsActive(true)
  }

  const onEdit = (r: AcademicYearRow) => {
    setEditingId(r.id)
    setYearName(r.yearName ?? '')
    setStartDate(r.startDate?.slice(0, 10) ?? '')
    setEndDate(r.endDate?.slice(0, 10) ?? '')
    setIsActive(r.isActive !== false)
    setMsg(null)
    setErr(null)
  }

  const onSave = async () => {
    const name = yearName.trim()
    const sd = startDate.trim()
    const ed = endDate.trim()
    if (!name || !sd || !ed) {
      setErr('Year name, start date, and end date are required.')
      return
    }
    const body: AcademicYearUpsert = {
      yearName: name,
      startDate: sd,
      endDate: ed,
      isActive,
    }
    setSaving(true)
    setErr(null)
    setMsg(null)
    try {
      if (editingId) await academicYearsApi.update(editingId, body)
      else await academicYearsApi.create(body)
      setMsg(editingId ? 'Academic year updated.' : 'Academic year created.')
      resetForm()
      load()
    } catch (e) {
      setErr(apiErrMessage(e))
    } finally {
      setSaving(false)
    }
  }

  const onDelete = async (id: string) => {
    if (!window.confirm('Delete this academic year? Linked fees and students may reference it.')) return
    setErr(null)
    setMsg(null)
    try {
      await academicYearsApi.delete(id)
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
        title="Academic years"
        description="School years used across admission, fees, installments, and class fee mapping. Mark one as active when your process requires it."
      />

      <div style={ms.card}>
        <h2 style={ms.h2}>{editingId ? 'Edit academic year' : 'New academic year'}</h2>
        {err ? <p style={ms.err}>{err}</p> : null}
        {msg ? <p style={ms.ok}>{msg}</p> : null}
        <div style={ms.formGrid}>
          <label style={ms.lab}>
            Year name *
            <input
              style={ms.input}
              value={yearName}
              onChange={(e) => setYearName(e.target.value)}
              placeholder="e.g. 2025–26"
            />
          </label>
          <label style={ms.lab}>
            Start date *
            <input style={ms.input} type="date" value={startDate} onChange={(e) => setStartDate(e.target.value)} />
          </label>
          <label style={ms.lab}>
            End date *
            <input style={ms.input} type="date" value={endDate} onChange={(e) => setEndDate(e.target.value)} />
          </label>
          <label style={{ ...ms.lab, flexDirection: 'row', alignItems: 'center', gap: 10 }}>
            <input type="checkbox" checked={isActive} onChange={(e) => setIsActive(e.target.checked)} />
            Active
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
        <h2 style={ms.h2}>All academic years</h2>
        {loading ? (
          <Spinner />
        ) : (
          <div style={ms.tableWrap}>
            <table style={ms.table}>
              <thead>
                <tr>
                  <th style={ms.th}>Name</th>
                  <th style={ms.th}>Start</th>
                  <th style={ms.th}>End</th>
                  <th style={ms.th}>Active</th>
                  <th style={ms.th}>Actions</th>
                </tr>
              </thead>
              <tbody>
                {rows.length === 0 ? (
                  <tr>
                    <td style={ms.td} colSpan={5}>
                      No academic years yet.
                    </td>
                  </tr>
                ) : (
                  rows.map((r) => (
                    <tr key={r.id}>
                      <td style={ms.td}>{r.yearName ?? '—'}</td>
                      <td style={ms.td}>{r.startDate?.slice(0, 10) ?? '—'}</td>
                      <td style={ms.td}>{r.endDate?.slice(0, 10) ?? '—'}</td>
                      <td style={ms.td}>{r.isActive === false ? 'No' : r.isActive === true ? 'Yes' : '—'}</td>
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
