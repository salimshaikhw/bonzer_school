import type { CSSProperties, ReactNode } from 'react'
import { financeTheme as t } from './financeTheme'

type Props = {
  title: string
  description?: ReactNode
  actions?: ReactNode
}

/** SaaS-style page title row inside Finance module body. */
export function FinancePageHeader({ title, description, actions }: Props) {
  return (
    <div style={styles.row}>
      <div style={styles.text}>
        <h2 style={styles.title}>{title}</h2>
        {description ? <p style={styles.desc}>{description}</p> : null}
      </div>
      {actions ? <div style={styles.actions}>{actions}</div> : null}
    </div>
  )
}

const styles: Record<string, CSSProperties> = {
  row: {
    display: 'flex',
    flexWrap: 'wrap',
    alignItems: 'flex-start',
    justifyContent: 'space-between',
    gap: 16,
    marginBottom: 20,
    paddingBottom: 16,
    borderBottom: `1px solid ${t.border}`,
  },
  text: { flex: '1 1 200px', minWidth: 0 },
  title: {
    margin: 0,
    fontFamily: t.fontSans,
    fontSize: 20,
    fontWeight: 700,
    letterSpacing: '-0.02em',
    color: t.text,
  },
  desc: {
    margin: '6px 0 0',
    fontFamily: t.fontSans,
    fontSize: 14,
    lineHeight: 1.5,
    color: t.textMuted,
    maxWidth: 640,
  },
  actions: { display: 'flex', flexWrap: 'wrap', gap: 10, alignItems: 'center' },
}
