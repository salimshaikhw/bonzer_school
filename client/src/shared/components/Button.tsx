import type { ButtonHTMLAttributes } from 'react'

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'primary' | 'secondary' | 'ghost'
  fullWidth?: boolean
}

const variants = {
  primary: { background: '#1e3a5f', color: '#fff', border: 'none' },
  secondary: { background: 'transparent', color: '#1e3a5f', border: '1px solid #1e3a5f' },
  ghost: { background: 'transparent', color: '#666', border: '1px solid #ccc' },
} as const

export function Button({
  variant = 'primary',
  fullWidth,
  type = 'button',
  style,
  children,
  ...props
}: ButtonProps) {
  return (
    <button
      type={type}
      style={{
        padding: '10px 16px',
        borderRadius: 6,
        fontSize: 16,
        cursor: 'pointer',
        width: fullWidth ? '100%' : undefined,
        ...variants[variant],
        ...style,
      }}
      {...props}
    >
      {children}
    </button>
  )
}
