import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { ROUTES } from '../../../config/routes'
import { Button, Input } from '../../../shared'
import { userApi } from '../api/user.api'

export function RegisterPage() {
  const [firstName, setFirstName] = useState('')
  const [lastName, setLastName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [role, setRole] = useState('User')
  const [error, setError] = useState('')
  const [success, setSuccess] = useState(false)
  const [loading, setLoading] = useState(false)
  const navigate = useNavigate()

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError('')
    setLoading(true)
    try {
      const result = await userApi.register({
        firstName,
        lastName,
        email,
        password,
        role,
      })
      if (result.isSuccessful) {
        setSuccess(true)
        setTimeout(() => navigate(ROUTES.LOGIN), 2000)
      } else {
        setError(result.message ?? 'Registration failed')
      }
    } catch {
      setError('Registration failed. Try again.')
    } finally {
      setLoading(false)
    }
  }

  if (success) {
    return (
      <div style={styles.container}>
        <div style={styles.card}>
          <p style={styles.success}>Registration successful. Redirecting to login…</p>
        </div>
      </div>
    )
  }

  return (
    <div style={styles.container}>
      <div style={styles.card}>
        <h1 style={styles.title}>Register</h1>
        <p style={styles.subtitle}>Create a new account</p>
        <form onSubmit={handleSubmit} style={styles.form}>
          {error && <div style={styles.error}>{error}</div>}
          <Input
            label="First name"
            value={firstName}
            onChange={(e) => setFirstName(e.target.value)}
            required
            minLength={3}
          />
          <Input
            label="Last name"
            value={lastName}
            onChange={(e) => setLastName(e.target.value)}
            required
            minLength={3}
          />
          <Input
            type="email"
            label="Email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
          />
          <Input
            type="password"
            label="Password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
            minLength={6}
          />
          <label style={styles.label}>
            Role
            <select
              value={role}
              onChange={(e) => setRole(e.target.value)}
              style={styles.select}
            >
              <option value="User">User</option>
              <option value="Admin">Admin</option>
            </select>
          </label>
          <Button type="submit" disabled={loading} fullWidth>
            {loading ? 'Registering…' : 'Register'}
          </Button>
          <Link to={ROUTES.LOGIN} style={styles.link}>
            Already have an account? Sign in
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
  success: { color: '#0a0', margin: 0 },
  label: { display: 'flex', flexDirection: 'column', gap: 4, fontSize: 14, fontWeight: 500 },
  select: { padding: 10, border: '1px solid #ccc', borderRadius: 6, fontSize: 16 },
  link: { textAlign: 'center', color: '#1e3a5f', fontSize: 14 },
}
