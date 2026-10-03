import { Spinner } from '../../../shared'
import { Link } from 'react-router-dom'
import { ROUTES } from '../../../config/routes'
import { useOrgHierarchy } from '../../../layouts/OrgHierarchyProvider'
import { useAdminDashboardData } from '../hooks/useAdminDashboardData'
import { AdminOrgCrudPanel } from '../components/AdminOrgCrudPanel'
import { AdmissionOrgScopeBanner } from '../../admission/components/AdmissionOrgScopeBanner'

export function AdminDashboardPage() {
  const org = useOrgHierarchy()
  const {
    hasOrgScope,
    branchesInScopeCount,
    totalStudents,
    todayAdmissions,
    branchCounts,
    classes,
    divisions,
    strengthByClass,
    strengthByDivision,
    genderDistribution,
    admissionTrend,
    loading,
    analyticsLoading,
    error,
    getName,
  } = useAdminDashboardData()

  if (!org.ready || org.loading) {
    return (
      <p>
        <Spinner /> Loading…
      </p>
    )
  }

  if (error) return <p style={styles.error}>{error}</p>

  if (!hasOrgScope) {
    return (
      <div style={styles.wrap}>
        <h2>Admin – Admission &amp; Analytics Dashboard</h2>
        <AdmissionOrgScopeBanner />
        <p style={styles.links}>
          <Link to={ROUTES.ADMISSION_ANALYTICS}>Admission analytics</Link>
          {' · '}
          <Link to={ROUTES.ADMIN_ORG_GROUPS}>School Groups</Link>
          {' · '}
          <Link to={ROUTES.ADMIN_ORG_SCHOOLS}>Schools</Link>
          {' · '}
          <Link to={ROUTES.ADMIN_ORG_CLASSES}>Classes</Link>
          {' · '}
          <Link to={ROUTES.ADMIN_ORG_DIVISIONS}>Divisions</Link>
        </p>
        <p style={styles.muted}>Select a school group or a school in the organization tree to load dashboard and admission metrics.</p>
        <AdminOrgCrudPanel />
      </div>
    )
  }

  if (loading) {
    return (
      <div style={styles.wrap}>
        <AdmissionOrgScopeBanner />
        <p>
          <Spinner /> Loading dashboard…
        </p>
      </div>
    )
  }

  return (
    <div style={styles.wrap}>
      <h2>Admin – Admission &amp; Analytics Dashboard</h2>
      <AdmissionOrgScopeBanner />
      <p style={styles.links}>
        <Link to={ROUTES.ADMISSION_ANALYTICS}>Admission analytics (same scope)</Link>
        {' · '}
        <Link to={ROUTES.ADMIN_ORG_GROUPS}>School Groups</Link>
        {' · '}
        <Link to={ROUTES.ADMIN_ORG_SCHOOLS}>Schools</Link>
        {' · '}
        <Link to={ROUTES.ADMIN_ORG_CLASSES}>Classes</Link>
        {' · '}
        <Link to={ROUTES.ADMIN_ORG_DIVISIONS}>Divisions</Link>
      </p>
      <AdminOrgCrudPanel />

      <section style={styles.section}>
        <h3>Summary (selected tree scope)</h3>
        <p style={styles.scopeHint}>
          Figures match your current organization tree selection — same rules as the Admission screens.
        </p>
        <div className="form-grid" style={styles.cards}>
          <div style={styles.card}>
            <div style={styles.cardValue}>{totalStudents}</div>
            <div style={styles.cardLabel}>Students in scope</div>
          </div>
          <div style={styles.card}>
            <div style={styles.cardValue}>{todayAdmissions}</div>
            <div style={styles.cardLabel}>Today&apos;s admissions (scope)</div>
          </div>
          <div style={styles.card}>
            <div style={styles.cardValue}>{branchesInScopeCount}</div>
            <div style={styles.cardLabel}>Schools in scope</div>
          </div>
        </div>
      </section>

      <section style={styles.section}>
        <h3>School-wise students (scope)</h3>
        <table style={styles.tableWide}>
          <thead>
            <tr>
              <th style={styles.th}>School / branch</th>
              <th style={styles.th}>Students</th>
            </tr>
          </thead>
          <tbody>
            {branchCounts.map(({ branchId, branchName, count }) => (
              <tr key={branchId}>
                <td style={styles.td}>{branchName}</td>
                <td style={styles.td}>{count}</td>
              </tr>
            ))}
            {branchCounts.length === 0 && (
              <tr>
                <td colSpan={2} style={styles.td}>
                  No schools in this scope
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </section>

      <section style={styles.section}>
        <h3>Remaining Seats</h3>
        <p style={styles.muted}>Seat capacity can be configured per class/branch in settings. Not available in this view yet.</p>
      </section>

      <section style={styles.section}>
        <h3>Analytics (selected scope)</h3>
        {org.classId.trim() || org.divisionId.trim() ? (
          <p style={styles.scopeHint}>
            Class or division selected: charts are derived from the student list for this branch scope. Year trend uses admission
            date when present.
          </p>
        ) : (
          <p style={styles.scopeHint}>
            School or group scope: class, division, gender, and year-wise trends aggregate analytics APIs across schools in scope.
          </p>
        )}
        {analyticsLoading && (
          <p>
            <Spinner /> Loading analytics…
          </p>
        )}
        {!analyticsLoading && (
          <>
            <h4 style={styles.h4}>Class-wise admission</h4>
            <table style={styles.tableWide}>
              <thead>
                <tr>
                  <th style={styles.th}>Class</th>
                  <th style={styles.th}>Count</th>
                </tr>
              </thead>
              <tbody>
                {strengthByClass.map((row, i) => (
                  <tr key={row.id ?? i}>
                    <td style={styles.td}>{row.id ? getName(row.id, classes) : '—'}</td>
                    <td style={styles.td}>{row.count}</td>
                  </tr>
                ))}
                {strengthByClass.length === 0 && (
                  <tr>
                    <td colSpan={2} style={styles.td}>
                      No data
                    </td>
                  </tr>
                )}
              </tbody>
            </table>

            <h4 style={styles.h4}>Division-wise admission</h4>
            <table style={styles.tableWide}>
              <thead>
                <tr>
                  <th style={styles.th}>Division</th>
                  <th style={styles.th}>Count</th>
                </tr>
              </thead>
              <tbody>
                {strengthByDivision.map((row, i) => (
                  <tr key={row.id ?? i}>
                    <td style={styles.td}>{row.id ? getName(row.id, divisions) : '—'}</td>
                    <td style={styles.td}>{row.count}</td>
                  </tr>
                ))}
                {strengthByDivision.length === 0 && (
                  <tr>
                    <td colSpan={2} style={styles.td}>
                      No data
                    </td>
                  </tr>
                )}
              </tbody>
            </table>

            <h4 style={styles.h4}>Gender-wise distribution</h4>
            <table style={styles.tableWide}>
              <thead>
                <tr>
                  <th style={styles.th}>Gender</th>
                  <th style={styles.th}>Count</th>
                </tr>
              </thead>
              <tbody>
                {genderDistribution.map((row) => (
                  <tr key={row.key}>
                    <td style={styles.td}>{row.key || '—'}</td>
                    <td style={styles.td}>{row.count}</td>
                  </tr>
                ))}
                {genderDistribution.length === 0 && (
                  <tr>
                    <td colSpan={2} style={styles.td}>
                      No data
                    </td>
                  </tr>
                )}
              </tbody>
            </table>

            <h4 style={styles.h4}>Admission trend (year-wise)</h4>
            <table style={styles.tableWide}>
              <thead>
                <tr>
                  <th style={styles.th}>Year</th>
                  <th style={styles.th}>Admissions</th>
                </tr>
              </thead>
              <tbody>
                {admissionTrend.map((row) => (
                  <tr key={row.year}>
                    <td style={styles.td}>{row.year}</td>
                    <td style={styles.td}>{row.count}</td>
                  </tr>
                ))}
                {admissionTrend.length === 0 && (
                  <tr>
                    <td colSpan={2} style={styles.td}>
                      No data
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </>
        )}
      </section>
    </div>
  )
}

const styles: Record<string, React.CSSProperties> = {
  wrap: { padding: 0 },
  links: { marginBottom: 16 },
  scopeHint: { margin: '0 0 12px', fontSize: 13, color: '#64748b', maxWidth: 720 },
  section: { marginBottom: 32 },
  cards: { display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(160px, 1fr))', gap: '16px', marginTop: 8 },
  card: { background: '#fff', padding: 20, borderRadius: 8, boxShadow: '0 1px 3px rgba(0,0,0,0.1)' },
  cardValue: { fontSize: 28, fontWeight: 700, color: '#1e3a5f' },
  cardLabel: { fontSize: 13, color: '#666', marginTop: 4 },
  tableWide: { width: '100%', maxWidth: 520, borderCollapse: 'collapse', background: '#fff', borderRadius: 8, overflow: 'hidden', boxShadow: '0 1px 3px rgba(0,0,0,0.1)' },
  th: { textAlign: 'left', padding: '10px 12px', background: '#1e3a5f', color: '#fff', fontSize: 14 },
  td: { padding: '10px 12px', borderBottom: '1px solid #eee', fontSize: 14 },
  h4: { marginTop: 24, marginBottom: 8, fontSize: 16 },
  muted: { color: '#666', fontSize: 14 },
  error: { color: '#c00' },
}
