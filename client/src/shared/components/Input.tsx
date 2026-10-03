import type { InputHTMLAttributes } from 'react'

interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  label?: string
  error?: string
}

export function Input({ label, error, style, ...props }: InputProps) {
  return (
    <label style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
      {label && <span style={{ fontSize: 14, fontWeight: 500 }}>{label}</span>}
      <input
        style={{
          padding: 10,
          border: `1px solid ${error ? '#c00' : '#ccc'}`,
          borderRadius: 6,
          fontSize: 16,
          ...style,
        }}
        {...props}
      />
      {error && <span style={{ fontSize: 12, color: '#c00' }}>{error}</span>}
    </label>
  )
}
