import type { CSSProperties } from 'react'
import { Button } from '../../../shared'
import type { useAdmissionScopeFilters } from '../hooks/useAdmissionScopeFilters'

type Scope = ReturnType<typeof useAdmissionScopeFilters>

type Props = {
  s: Scope
  /** Show button to copy sidebar hierarchy into these filters */
  showApplyOrg?: boolean
}

export function AdmissionScopeFilterBar({ s, showApplyOrg }: Props) {
  return (
    <div style={styles.wrap}>
      {showApplyOrg ? (
        <div style={styles.applyRow}>
          <Button type="button" variant="secondary" onClick={() => s.applyOrgContext()} style={styles.applyBtn}>
            Apply sidebar organization
          </Button>
          <span style={styles.hint}>Uses the school / class / division selected in the left hierarchy.</span>
        </div>
      ) : null}
      <div style={styles.filters}>
        <label style={styles.label}>
          School group
          <select
            value={s.groupId}
            onChange={(e) => s.setGroupId(e.target.value)}
            style={styles.select}
            disabled={s.loading}
          >
            <option value="">All groups</option>
            {s.groups.map((g) => (
              <option key={g.id} value={g.id}>
                {g.name ?? g.id}
              </option>
            ))}
          </select>
        </label>
        <label style={styles.label}>
          School (branch)
          <select
            value={s.branchId}
            onChange={(e) => s.setBranchId(e.target.value)}
            style={styles.select}
            disabled={s.loading || (s.groupId !== '' && s.branchesForGroup.length === 0)}
          >
            <option value="">— All schools in scope —</option>
            {s.branchesForGroup.map((b) => (
              <option key={b.id} value={b.id}>
                {b.name ?? b.id}
              </option>
            ))}
          </select>
        </label>
        <label style={styles.label}>
          Class
          <select
            value={s.classId}
            onChange={(e) => s.setClassId(e.target.value)}
            style={styles.select}
            disabled={!s.branchId}
          >
            <option value="">— All classes —</option>
            {s.classesForBranch.map((c) => (
              <option key={c.id} value={c.id}>
                {c.name ?? c.id}
              </option>
            ))}
          </select>
        </label>
        <label style={styles.label}>
          Division
          <select
            value={s.divisionId}
            onChange={(e) => s.setDivisionId(e.target.value)}
            style={styles.select}
            disabled={!s.classId}
          >
            <option value="">— All divisions —</option>
            {s.divisionsForClass.map((d) => (
              <option key={d.id} value={d.id}>
                {d.name ?? d.id}
              </option>
            ))}
          </select>
        </label>
      </div>
      {s.error && <p style={styles.error}>{s.error}</p>}
    </div>
  )
}

const styles: Record<string, CSSProperties> = {
  wrap: { marginBottom: 20 },
  applyRow: { display: 'flex', flexWrap: 'wrap', alignItems: 'center', gap: 12, marginBottom: 14 },
  applyBtn: { padding: '8px 14px', fontSize: 13 },
  hint: { fontSize: 12, color: '#64748b', maxWidth: 420 },
  filters: { display: 'flex', gap: 20, flexWrap: 'wrap' },
  label: { display: 'flex', flexDirection: 'column', gap: 4, fontSize: 13, fontWeight: 600, color: '#334155' },
  select: { padding: '8px 10px', border: '1px solid #cbd5e1', borderRadius: 8, minWidth: 176, fontSize: 14, background: '#fff' },
  error: { color: '#b91c1c', fontSize: 13, marginTop: 8 },
}
