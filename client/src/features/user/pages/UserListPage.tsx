import { useEffect, useState } from 'react'
import { Spinner } from '../../../shared'
import { userApi } from '../api/user.api'
import type { UserDetails } from '../user.types'

export function UserListPage() {
  const [users, setUsers] = useState<UserDetails[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    userApi
      .getAll()
      .then(setUsers)
      .catch(() => setError('Failed to load users. Admin access required.'))
      .finally(() => setLoading(false))
  }, [])

  if (loading) return <p><Spinner /> Loading users…</p>
  if (error) return <p style={{ color: '#c00' }}>{error}</p>

  return (
    <div style={styles.wrap}>
      <h2>Users</h2>
      <table style={styles.table}>
        <thead>
          <tr>
            <th>Name</th>
            <th>Email</th>
            <th>Email confirmed</th>
          </tr>
        </thead>
        <tbody>
          {users.map((u) => (
            <tr key={u.id}>
              <td>{u.fullName || `${u.firstName} ${u.lastName}`}</td>
              <td>{u.email}</td>
              <td>{u.emailConfirmed ? 'Yes' : 'No'}</td>
            </tr>
          ))}
        </tbody>
      </table>
      {users.length === 0 && <p>No users found.</p>}
    </div>
  )
}

const styles: Record<string, React.CSSProperties> = {
  wrap: { padding: 24 },
  table: { width: '100%', borderCollapse: 'collapse' },
}
