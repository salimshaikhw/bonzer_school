import { useState } from 'react'
import { Link } from 'react-router-dom'
import { ROUTES } from '../../../config/routes'
import { Button, Input } from '../../../shared'
import { userApi } from '../api/user.api'

export function ForgotPasswordPage() {
  const [email, setEmail] = useState('')
  const [sent, setSent] = useState(false)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError('')
    setLoading(true)
    try {
      await userApi.sendPasswordReset(email)
      setSent(true)
    } catch {
      setError('Failed to send reset link. Check the email address.')
    } finally {
      setLoading(false)
    }
  }

  if (sent) {
    return (
      <div style={styles.container}>
        <div style={styles.card}>
          <p style={styles.success}>
            If an account exists for that email, a password reset link has been sent.
          </p>
          <Link to={ROUTES.LOGIN} style={styles.link}>
            Back to sign in
          </Link>
        </div>
      </div>
    )
  }

  return (
    <div style={styles.container}>
      <div style={styles.card}>
        <h1 style={styles.title}>Forgot password</h1>
        <p style={styles.subtitle}>Enter your email to receive a reset link</p>
        <form onSubmit={handleSubmit} style={styles.form}>
          {error && <div style={styles.error}>{error}</div>}
          <Input
            type="email"
            label="Email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
          />
          <Button type="submit" disabled={loading} fullWidth>
            {loading ? 'Sending…' : 'Send reset link'}
          </Button>
          <Link to={ROUTES.LOGIN} style={styles.link}>
            Back to sign in
          </Link>
        </form>
      </div>
    </div>
  )
}

const styles: Record<string, React.CSSProperties> = {
  container: {
    minHeight: '100vh',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    background: 'linear-gradient(135deg, #1e3a5f 0%, #0d1b2a 100%)',
  },
  card: {
    background: '#fff',
    padding: 32,
    borderRadius: 12,
    boxShadow: '0 8px 32px rgba(0,0,0,0.2)',
    width: '100%',
    maxWidth: 400,
  },
  title: { margin: '0 0 4px', fontSize: 24, color: '#1e3a5f' },
  subtitle: { margin: '0 0 24px', color: '#666', fontSize: 14 },
  form: { display: 'flex', flexDirection: 'column', gap: 16 },
  error: { background: '#fee', color: '#c00', padding: 10, borderRadius: 6, fontSize: 14 },
  success: { color: '#0a0', margin: '0 0 16px' },
  link: { textAlign: 'center', color: '#1e3a5f', fontSize: 14 },
}
