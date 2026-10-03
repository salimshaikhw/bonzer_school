import axios from 'axios'
import { useState, useEffect } from 'react'
import { useNavigate, Link } from 'react-router-dom'
import { env } from '../../../config/env'
import { ROUTES } from '../../../config/routes'
import { Button, Input, Spinner } from '../../../shared'
import { useLogin } from '../hooks/useLogin'

const SAVED_EMAIL_KEY = 'bonzer_login_email'
const SAVED_PASSWORD_KEY = 'bonzer_login_password'
const SAVE_PASSWORD_KEY = 'bonzer_save_password'

export function LoginPage() {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [savePassword, setSavePassword] = useState(false)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const { login } = useLogin()
  const navigate = useNavigate()

  useEffect(() => {
    try {
      const saved = localStorage.getItem(SAVE_PASSWORD_KEY)
      if (saved === 'true') {
        setSavePassword(true)
        const savedEmail = localStorage.getItem(SAVED_EMAIL_KEY)
        const savedPassword = localStorage.getItem(SAVED_PASSWORD_KEY)
        if (savedEmail) setEmail(savedEmail)
        if (savedPassword) setPassword(savedPassword)
      } else {
        const savedEmail = localStorage.getItem(SAVED_EMAIL_KEY)
        if (savedEmail) setEmail(savedEmail)
      }
    } catch {
      // ignore
    }
  }, [])

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError('')
    setLoading(true)
    try {
      await login({ email: email.trim(), password })
      try {
        if (savePassword) {
          localStorage.setItem(SAVE_PASSWORD_KEY, 'true')
          localStorage.setItem(SAVED_EMAIL_KEY, email)
          localStorage.setItem(SAVED_PASSWORD_KEY, password)
        } else {
          localStorage.removeItem(SAVE_PASSWORD_KEY)
          localStorage.removeItem(SAVED_EMAIL_KEY)
          localStorage.removeItem(SAVED_PASSWORD_KEY)
        }
      } catch {
        // ignore
      }
      navigate(ROUTES.HOME, { replace: true })
    } catch (err) {
      if (axios.isAxiosError(err)) {
        const data = err.response?.data
        const status = err.response?.status
        if (typeof data === 'string' && data.trim()) {
          setError(data.trim())
        } else if (data && typeof data === 'object') {
          const o = data as Record<string, unknown>
          const msg = o.message ?? o.detail ?? o.title ?? o.Message
          if (typeof msg === 'string' && msg.trim()) setError(msg.trim())
          else if (status === 401) setError('Invalid email or password.')
          else if (!err.response) setError('Cannot reach the API. Start the API (see proxy in vite.config) and PostgreSQL, then try again.')
          else setError(`Sign-in failed (HTTP ${String(status)}).`)
        } else if (status === 401) setError('Invalid email or password.')
        else if (!err.response) setError('Cannot reach the API. Start the API (see proxy in vite.config) and PostgreSQL, then try again.')
        else setError('Sign-in failed.')
      } else if (err instanceof Error && err.message) {
        setError(err.message)
      } else {
        setError('Sign-in failed.')
      }
    } finally {
      setLoading(false)
    }
  }

  return (
    <div style={styles.container}>
      <div style={styles.card}>
        <h1 style={styles.title}>BonzerSchool</h1>
        <p style={styles.subtitle}>Multi-tenant school ERP — sign in to your workspace</p>
        {env.isDev && (
          <p style={styles.devHint}>
            Dev: first-run admin is often <code style={styles.code}>admin@local</code> / <code style={styles.code}>Admin123!</code> (see <code style={styles.code}>API/appsettings.Development.json</code>).
          </p>
        )}
        <form onSubmit={handleSubmit} style={styles.form}>
          {error && <div style={styles.error}>{error}</div>}
          <Input
            type="email"
            placeholder="Email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
          />
          <Input
            type="password"
            placeholder="Password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
          />
          <label style={styles.checkLabel}>
            <input
              type="checkbox"
              checked={savePassword}
              onChange={(e) => setSavePassword(e.target.checked)}
            />
            Save password
          </label>
          <Button type="submit" disabled={loading} fullWidth>
            {loading ? <Spinner size={20} /> : 'Sign in'}
          </Button>
          <div style={styles.links}>
            <Link to={ROUTES.FORGOT_PASSWORD} style={styles.link}>
              Forgot password?
            </Link>
            <Link to={ROUTES.REGISTER} style={styles.link}>
              Create account
            </Link>
          </div>
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
    maxWidth: 380,
  },
  title: { margin: '0 0 4px', fontSize: 24, color: '#1e3a5f' },
  subtitle: { margin: '0 0 8px', color: '#666', fontSize: 14 },
  devHint: { margin: '0 0 16px', fontSize: 12, color: '#475569', lineHeight: 1.45 },
  code: { fontSize: 11, background: '#f1f5f9', padding: '1px 4px', borderRadius: 4 },
  form: { display: 'flex', flexDirection: 'column', gap: 16 },
  checkLabel: { display: 'flex', alignItems: 'center', gap: 8, cursor: 'pointer', fontSize: 14, color: '#333' },
  error: { background: '#fee', color: '#c00', padding: 10, borderRadius: 6, fontSize: 14 },
  links: { display: 'flex', justifyContent: 'space-between', marginTop: 8 },
  link: { color: '#1e3a5f', fontSize: 14 },
}
