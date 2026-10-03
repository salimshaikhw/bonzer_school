import type { CSSProperties } from 'react'
import { Spinner } from '../../../shared'
import type { IntakeKpi } from '../utils/admissionIntakeMetrics'

function accentForLevel(level: IntakeKpi['level']): string {
  switch (level) {
    case 'group':
      return '#1e3a5f'
    case 'school':
      return '#1d4ed8'
    case 'class':
      return '#0369a1'
    case 'division':
      return '#047857'
    default:
      return '#64748b'
  }
}

function fmt(n: number | null) {
  if (n == null) return '—'
  return n.toLocaleString()
}

function KpiCard({ kpi, accent }: { kpi: IntakeKpi; accent: string }) {
  const hasNumbers = kpi.enrolled != null
  return (
    <div style={{ ...cardStyle, borderTop: `3px solid ${accent}` }}>
      <div style={cardLabel}>{kpi.title}</div>
      {kpi.placeholder && !hasNumbers ? (
        <p style={placeholderStyle}>{kpi.placeholder}</p>
      ) : (
        <div style={metricsGrid}>
          <div>
            <div style={metricVal}>{fmt(kpi.enrolled)}</div>
            <div style={metricLbl}>Enrolled</div>
          </div>
          <div>
            <div style={metricVal}>{fmt(kpi.capacity)}</div>
            <div style={metricLbl}>Capacity</div>
          </div>
          <div>
            <div style={{ ...metricVal, color: kpi.remaining === 0 ? '#b45309' : '#0f172a' }}>{fmt(kpi.remaining)}</div>
            <div style={metricLbl}>Remaining seats</div>
          </div>
        </div>
      )}
    </div>
  )
}

export function AdmissionIntakeKpiStrip({ kpi, loading }: { kpi: IntakeKpi | null; loading?: boolean }) {
  if (loading) {
    return (
      <div style={stripWrap}>
        <p style={{ margin: 0, color: '#64748b', display: 'flex', alignItems: 'center', gap: 8 }}>
          <Spinner /> Updating intake figures…
        </p>
      </div>
    )
  }

  if (!kpi) {
    return null
  }

  return (
    <div style={stripWrap}>
      <div style={stripHeader}>
        <span style={stripTitle}>Intake at a glance</span>
        <span style={stripHint}>Scope matches the selected node only (group, school, class, or division)</span>
      </div>
      <KpiCard kpi={kpi} accent={accentForLevel(kpi.level)} />
    </div>
  )
}

const stripWrap: CSSProperties = {
  marginBottom: 28,
  padding: 16,
  background: 'linear-gradient(135deg, #f8fafc 0%, #fff 50%)',
  borderRadius: 12,
  border: '1px solid #e2e8f0',
  boxShadow: '0 1px 3px rgba(15, 23, 42, 0.06)',
}

const stripHeader: CSSProperties = {
  marginBottom: 14,
}

const stripTitle: CSSProperties = {
  display: 'block',
  fontSize: 13,
  fontWeight: 700,
  color: '#0f172a',
  letterSpacing: '0.02em',
}

const stripHint: CSSProperties = {
  display: 'block',
  fontSize: 12,
  color: '#64748b',
  marginTop: 4,
}

const cardStyle: CSSProperties = {
  background: '#fff',
  borderRadius: 10,
  padding: 16,
  border: '1px solid #e2e8f0',
  boxShadow: '0 1px 2px rgba(15, 23, 42, 0.05)',
}

const cardLabel: CSSProperties = {
  fontSize: 11,
  fontWeight: 700,
  textTransform: 'uppercase',
  letterSpacing: '0.06em',
  color: '#64748b',
  marginBottom: 12,
}

const metricsGrid: CSSProperties = {
  display: 'grid',
  gridTemplateColumns: 'repeat(3, 1fr)',
  gap: 8,
  textAlign: 'center',
}

const metricVal: CSSProperties = {
  fontSize: 22,
  fontWeight: 800,
  color: '#0f172a',
  lineHeight: 1.2,
}

const metricLbl: CSSProperties = {
  fontSize: 11,
  color: '#64748b',
  marginTop: 4,
  fontWeight: 500,
}

const placeholderStyle: CSSProperties = {
  margin: 0,
  fontSize: 13,
  color: '#64748b',
  lineHeight: 1.5,
}
