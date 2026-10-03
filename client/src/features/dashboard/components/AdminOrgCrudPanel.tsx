import { useEffect, useMemo, useState, type CSSProperties } from 'react'
import { Link } from 'react-router-dom'
import { ROUTES } from '../../../config/routes'
import { useOrgHierarchy } from '../../../layouts/OrgHierarchyProvider'
import { branchesApi } from '../../branches/api/branches.api'
import type { Branch } from '../../branches/branches.types'
import { referenceDataApi, type ClassItem, type DivisionItem } from '../../admission/api/referenceData.api'
import { schoolGroupApi } from '../../schoolGroups/api/schoolGroup.api'
import type { SchoolGroup } from '../../schoolGroups/schoolGroup.types'

function byName<T extends { name?: string; id: string }>(arr: T[]): T[] {
  return [...arr].sort((a, b) =>
    String(a.name ?? a.id).localeCompare(String(b.name ?? b.id), undefined, { sensitivity: 'base' }),
  )
}

type GroupForm = { id?: string; name: string; code: string; address: string; phone: string; email: string }
type BranchForm = {
  id?: string
  groupId: string
  name: string
  address: string
  city: string
  state: string
  phone: string
  email: string
}
type ClassForm = { id?: string; branchId: string; name: string }
type DivisionForm = { id?: string; classId: string; branchId: string; name: string; code: string; availableSeats: string }

const emptyGroup: GroupForm = { name: '', code: '', address: '', phone: '', email: '' }
const emptyBranch: BranchForm = { groupId: '', name: '', address: '', city: '', state: '', phone: '', email: '' }
const emptyClass: ClassForm = { branchId: '', name: '' }
const emptyDivision: DivisionForm = { classId: '', branchId: '', name: '', code: '', availableSeats: '' }

export type AdminOrgCrudScope = 'all' | 'groups' | 'branches' | 'classes' | 'divisions'

export function AdminOrgCrudPanel({ scope = 'all' }: { scope?: AdminOrgCrudScope }) {
  const { refreshHierarchy } = useOrgHierarchy()
  const [groups, setGroups] = useState<SchoolGroup[]>([])
  const [branches, setBranches] = useState<Branch[]>([])
  const [classes, setClasses] = useState<ClassItem[]>([])
  const [divisions, setDivisions] = useState<DivisionItem[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)

  const [groupForm, setGroupForm] = useState<GroupForm>(emptyGroup)
  const [branchForm, setBranchForm] = useState<BranchForm>(emptyBranch)
  const [classForm, setClassForm] = useState<ClassForm>(emptyClass)
  const [divisionForm, setDivisionForm] = useState<DivisionForm>(emptyDivision)

  async function refreshAll() {
    setLoading(true)
    setError('')
    try {
      const [g, b, c, d] = await Promise.all([
        schoolGroupApi.getAll(),
        branchesApi.getAll(),
        referenceDataApi.getClasses(),
        referenceDataApi.getDivisions(),
      ])
      setGroups(byName(g))
      setBranches(byName(b))
      setClasses(byName(c))
      setDivisions(byName(d))
    } catch {
      setError('Failed to load organization data. Check API/login and try again.')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    void refreshAll()
  }, [])

  const classBranchById = useMemo(() => {
    const m = new Map<string, string>()
    for (const c of classes) if (c.id && c.branchId) m.set(c.id, c.branchId)
    return m
  }, [classes])

  function resolveName(id: string | undefined, rows: { id: string; name?: string }[]): string {
    if (!id) return '—'
    return rows.find((r) => r.id === id)?.name ?? id
  }

  async function withBusy(fn: () => Promise<void>) {
    setBusy(true)
    setError('')
    try {
      await fn()
      await refreshAll()
      await refreshHierarchy()
    } catch (e) {
      const msg = e instanceof Error && e.message ? e.message : 'Operation failed.'
      setError(msg)
    } finally {
      setBusy(false)
    }
  }

  const showGroups = scope === 'all' || scope === 'groups'
  const showBranches = scope === 'all' || scope === 'branches'
  const showClasses = scope === 'all' || scope === 'classes'
  const showDivisions = scope === 'all' || scope === 'divisions'

  return (
    <section style={styles.wrap}>
      <h3>Organization Master (CRUD)</h3>
      <p style={styles.muted}>
        Manage School Group, School (Branch), Class, and Division from one place. These actions use existing APIs.
      </p>
      <p style={styles.links}>
        <Link to={ROUTES.ADMIN_ORG_GROUPS}>School Groups</Link>
        {' · '}
        <Link to={ROUTES.ADMIN_ORG_SCHOOLS}>Schools</Link>
        {' · '}
        <Link to={ROUTES.ADMIN_ORG_CLASSES}>Classes</Link>
        {' · '}
        <Link to={ROUTES.ADMIN_ORG_DIVISIONS}>Divisions</Link>
      </p>
      {error && <p style={styles.error}>{error}</p>}
      {loading ? (
        <p style={styles.muted}>Loading organization masters…</p>
      ) : (
        <>
          <div style={styles.grid}>
            {showGroups && (
            <article style={styles.card}>
              <h4 style={styles.h4}>School Group</h4>
              <div style={styles.form}>
                <input
                  placeholder="Name"
                  value={groupForm.name}
                  onChange={(e) => setGroupForm((p) => ({ ...p, name: e.target.value }))}
                  style={styles.input}
                />
                <input
                  placeholder="Code"
                  value={groupForm.code}
                  onChange={(e) => setGroupForm((p) => ({ ...p, code: e.target.value }))}
                  style={styles.input}
                />
                <input
                  placeholder="Address"
                  value={groupForm.address}
                  onChange={(e) => setGroupForm((p) => ({ ...p, address: e.target.value }))}
                  style={styles.input}
                />
                <input
                  placeholder="Phone"
                  value={groupForm.phone}
                  onChange={(e) => setGroupForm((p) => ({ ...p, phone: e.target.value }))}
                  style={styles.input}
                />
                <input
                  placeholder="Email"
                  value={groupForm.email}
                  onChange={(e) => setGroupForm((p) => ({ ...p, email: e.target.value }))}
                  style={styles.input}
                />
                <div style={styles.rowBtns}>
                  <button
                    type="button"
                    disabled={busy || !groupForm.name.trim() || !groupForm.code.trim()}
                    style={styles.btnPrimary}
                    onClick={() =>
                      void withBusy(async () => {
                        if (groupForm.id) {
                          await schoolGroupApi.update(groupForm.id, groupForm)
                        } else {
                          await schoolGroupApi.create(groupForm)
                        }
                        setGroupForm(emptyGroup)
                      })
                    }
                  >
                    {groupForm.id ? 'Update' : 'Add'}
                  </button>
                  <button type="button" style={styles.btnGhost} onClick={() => setGroupForm(emptyGroup)} disabled={busy}>
                    Clear
                  </button>
                </div>
              </div>
              <table style={styles.table}>
                <thead>
                  <tr><th style={styles.th}>Name</th><th style={styles.th}>Code</th><th style={styles.th}>Actions</th></tr>
                </thead>
                <tbody>
                  {groups.map((g) => (
                    <tr key={g.id}>
                      <td style={styles.td}>{g.name ?? '—'}</td>
                      <td style={styles.td}>{g.code ?? '—'}</td>
                      <td style={styles.td}>
                        <button
                          type="button"
                          style={styles.linkBtn}
                          onClick={() =>
                            setGroupForm({
                              id: g.id,
                              name: g.name ?? '',
                              code: g.code ?? '',
                              address: '',
                              phone: '',
                              email: '',
                            })
                          }
                        >
                          Edit
                        </button>
                        <button
                          type="button"
                          style={styles.linkDanger}
                          onClick={() => void withBusy(async () => schoolGroupApi.remove(g.id))}
                        >
                          Delete
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </article>
            )}

            {showBranches && (
            <article style={styles.card}>
              <h4 style={styles.h4}>School / Branch</h4>
              <div style={styles.form}>
                <select
                  value={branchForm.groupId}
                  onChange={(e) => setBranchForm((p) => ({ ...p, groupId: e.target.value }))}
                  style={styles.input}
                >
                  <option value="">Select group</option>
                  {groups.map((g) => (
                    <option key={g.id} value={g.id}>{g.name ?? g.id}</option>
                  ))}
                </select>
                <input placeholder="School/Branch name" value={branchForm.name} onChange={(e) => setBranchForm((p) => ({ ...p, name: e.target.value }))} style={styles.input} />
                <input placeholder="Address" value={branchForm.address} onChange={(e) => setBranchForm((p) => ({ ...p, address: e.target.value }))} style={styles.input} />
                <input placeholder="City" value={branchForm.city} onChange={(e) => setBranchForm((p) => ({ ...p, city: e.target.value }))} style={styles.input} />
                <input placeholder="State" value={branchForm.state} onChange={(e) => setBranchForm((p) => ({ ...p, state: e.target.value }))} style={styles.input} />
                <input placeholder="Phone" value={branchForm.phone} onChange={(e) => setBranchForm((p) => ({ ...p, phone: e.target.value }))} style={styles.input} />
                <input placeholder="Email" value={branchForm.email} onChange={(e) => setBranchForm((p) => ({ ...p, email: e.target.value }))} style={styles.input} />
                <div style={styles.rowBtns}>
                  <button
                    type="button"
                    disabled={busy || !branchForm.name.trim()}
                    style={styles.btnPrimary}
                    onClick={() =>
                      void withBusy(async () => {
                        if (branchForm.id) await branchesApi.update(branchForm.id, branchForm)
                        else await branchesApi.create(branchForm)
                        setBranchForm(emptyBranch)
                      })
                    }
                  >
                    {branchForm.id ? 'Update' : 'Add'}
                  </button>
                  <button type="button" style={styles.btnGhost} onClick={() => setBranchForm(emptyBranch)} disabled={busy}>
                    Clear
                  </button>
                </div>
              </div>
              <table style={styles.table}>
                <thead>
                  <tr><th style={styles.th}>Branch</th><th style={styles.th}>Group</th><th style={styles.th}>Actions</th></tr>
                </thead>
                <tbody>
                  {branches.map((b) => (
                    <tr key={b.id}>
                      <td style={styles.td}>{b.name ?? '—'}</td>
                      <td style={styles.td}>{resolveName(b.groupId ?? undefined, groups)}</td>
                      <td style={styles.td}>
                        <button
                          type="button"
                          style={styles.linkBtn}
                          onClick={() =>
                            setBranchForm({
                              id: b.id,
                              groupId: b.groupId ?? '',
                              name: b.name ?? '',
                              address: '',
                              city: b.city ?? '',
                              state: b.state ?? '',
                              phone: b.phone ?? '',
                              email: b.email ?? '',
                            })
                          }
                        >
                          Edit
                        </button>
                        <button type="button" style={styles.linkDanger} onClick={() => void withBusy(async () => branchesApi.remove(b.id))}>
                          Delete
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </article>
            )}
          </div>

          <div style={styles.grid}>
            {showClasses && (
            <article style={styles.card}>
              <h4 style={styles.h4}>Class</h4>
              <div style={styles.form}>
                <select value={classForm.branchId} onChange={(e) => setClassForm((p) => ({ ...p, branchId: e.target.value }))} style={styles.input}>
                  <option value="">Select branch</option>
                  {branches.map((b) => (
                    <option key={b.id} value={b.id}>{b.name ?? b.id}</option>
                  ))}
                </select>
                <input placeholder="Class name" value={classForm.name} onChange={(e) => setClassForm((p) => ({ ...p, name: e.target.value }))} style={styles.input} />
                <div style={styles.rowBtns}>
                  <button
                    type="button"
                    disabled={busy || !classForm.name.trim() || !classForm.branchId}
                    style={styles.btnPrimary}
                    onClick={() =>
                      void withBusy(async () => {
                        if (classForm.id) await referenceDataApi.updateClass(classForm.id, { branchId: classForm.branchId, name: classForm.name })
                        else await referenceDataApi.createClass({ branchId: classForm.branchId, name: classForm.name })
                        setClassForm(emptyClass)
                      })
                    }
                  >
                    {classForm.id ? 'Update' : 'Add'}
                  </button>
                  <button type="button" style={styles.btnGhost} onClick={() => setClassForm(emptyClass)} disabled={busy}>
                    Clear
                  </button>
                </div>
              </div>
              <table style={styles.table}>
                <thead>
                  <tr><th style={styles.th}>Class</th><th style={styles.th}>Branch</th><th style={styles.th}>Actions</th></tr>
                </thead>
                <tbody>
                  {classes.map((c) => (
                    <tr key={c.id}>
                      <td style={styles.td}>{c.name ?? '—'}</td>
                      <td style={styles.td}>{resolveName(c.branchId, branches)}</td>
                      <td style={styles.td}>
                        <button type="button" style={styles.linkBtn} onClick={() => setClassForm({ id: c.id, branchId: c.branchId ?? '', name: c.name ?? '' })}>
                          Edit
                        </button>
                        <button type="button" style={styles.linkDanger} onClick={() => void withBusy(async () => referenceDataApi.deleteClass(c.id))}>
                          Delete
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </article>
            )}

            {showDivisions && (
            <article style={styles.card}>
              <h4 style={styles.h4}>Division</h4>
              <div style={styles.form}>
                <select
                  value={divisionForm.classId}
                  onChange={(e) =>
                    setDivisionForm((p) => ({
                      ...p,
                      classId: e.target.value,
                      branchId: classBranchById.get(e.target.value) ?? p.branchId,
                    }))
                  }
                  style={styles.input}
                >
                  <option value="">Select class</option>
                  {classes.map((c) => (
                    <option key={c.id} value={c.id}>{c.name ?? c.id}</option>
                  ))}
                </select>
                <select value={divisionForm.branchId} onChange={(e) => setDivisionForm((p) => ({ ...p, branchId: e.target.value }))} style={styles.input}>
                  <option value="">Select branch</option>
                  {branches.map((b) => (
                    <option key={b.id} value={b.id}>{b.name ?? b.id}</option>
                  ))}
                </select>
                <input placeholder="Division name" value={divisionForm.name} onChange={(e) => setDivisionForm((p) => ({ ...p, name: e.target.value }))} style={styles.input} />
                <input placeholder="Code (optional)" value={divisionForm.code} onChange={(e) => setDivisionForm((p) => ({ ...p, code: e.target.value }))} style={styles.input} />
                <input placeholder="Available seats (optional)" value={divisionForm.availableSeats} onChange={(e) => setDivisionForm((p) => ({ ...p, availableSeats: e.target.value }))} style={styles.input} />
                <div style={styles.rowBtns}>
                  <button
                    type="button"
                    disabled={busy || !divisionForm.classId || !divisionForm.name.trim()}
                    style={styles.btnPrimary}
                    onClick={() =>
                      void withBusy(async () => {
                        const payload = {
                          classId: divisionForm.classId,
                          branchId: divisionForm.branchId || null,
                          name: divisionForm.name,
                          code: divisionForm.code,
                          availableSeats: divisionForm.availableSeats.trim() ? Number(divisionForm.availableSeats) : null,
                        }
                        if (divisionForm.id) await referenceDataApi.updateDivision(divisionForm.id, payload)
                        else await referenceDataApi.createDivision(payload)
                        setDivisionForm(emptyDivision)
                      })
                    }
                  >
                    {divisionForm.id ? 'Update' : 'Add'}
                  </button>
                  <button type="button" style={styles.btnGhost} onClick={() => setDivisionForm(emptyDivision)} disabled={busy}>
                    Clear
                  </button>
                </div>
              </div>
              <table style={styles.table}>
                <thead>
                  <tr><th style={styles.th}>Division</th><th style={styles.th}>Class</th><th style={styles.th}>Actions</th></tr>
                </thead>
                <tbody>
                  {divisions.map((d) => (
                    <tr key={d.id}>
                      <td style={styles.td}>{d.name ?? '—'}</td>
                      <td style={styles.td}>{resolveName(d.classId, classes)}</td>
                      <td style={styles.td}>
                        <button
                          type="button"
                          style={styles.linkBtn}
                          onClick={() =>
                            setDivisionForm({
                              id: d.id,
                              classId: d.classId ?? '',
                              branchId: d.branchId ?? '',
                              name: d.name ?? '',
                              code: d.code ?? '',
                              availableSeats: d.availableSeats != null ? String(d.availableSeats) : '',
                            })
                          }
                        >
                          Edit
                        </button>
                        <button type="button" style={styles.linkDanger} onClick={() => void withBusy(async () => referenceDataApi.deleteDivision(d.id))}>
                          Delete
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </article>
            )}
          </div>
        </>
      )}
    </section>
  )
}

const styles: Record<string, CSSProperties> = {
  wrap: {
    marginBottom: 32,
    border: '1px solid #e2e8f0',
    background: '#fff',
    borderRadius: 10,
    padding: 16,
  },
  muted: { color: '#64748b', margin: '4px 0 12px' },
  links: { margin: '0 0 12px', fontSize: 14 },
  error: { color: '#b91c1c', fontSize: 14, marginBottom: 12 },
  grid: { display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(380px, 1fr))', gap: 16, marginBottom: 16 },
  card: { border: '1px solid #e2e8f0', borderRadius: 10, padding: 12, background: '#f8fafc' },
  h4: { margin: '0 0 10px', fontSize: 16 },
  form: { display: 'grid', gap: 8, marginBottom: 12 },
  input: { padding: '8px 10px', border: '1px solid #cbd5e1', borderRadius: 8, fontSize: 14 },
  rowBtns: { display: 'flex', gap: 8 },
  btnPrimary: {
    border: 'none',
    background: '#1e3a5f',
    color: '#fff',
    borderRadius: 8,
    padding: '8px 12px',
    fontWeight: 600,
    cursor: 'pointer',
  },
  btnGhost: {
    border: '1px solid #cbd5e1',
    background: '#fff',
    color: '#334155',
    borderRadius: 8,
    padding: '8px 12px',
    fontWeight: 600,
    cursor: 'pointer',
  },
  table: { width: '100%', borderCollapse: 'collapse', background: '#fff', borderRadius: 8, overflow: 'hidden' },
  th: { textAlign: 'left', padding: '8px 10px', background: '#0f172a', color: '#fff', fontSize: 12 },
  td: { padding: '8px 10px', borderBottom: '1px solid #e2e8f0', fontSize: 13 },
  linkBtn: {
    border: 'none',
    background: 'transparent',
    color: '#1d4ed8',
    textDecoration: 'underline',
    cursor: 'pointer',
    marginRight: 8,
  },
  linkDanger: {
    border: 'none',
    background: 'transparent',
    color: '#b91c1c',
    textDecoration: 'underline',
    cursor: 'pointer',
  },
}
