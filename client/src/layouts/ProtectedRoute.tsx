import { Navigate, useLocation } from 'react-router-dom'
import { useAuth } from '../core'
import { ROUTES } from '../config/routes'

interface ProtectedRouteProps {
  children: React.ReactNode
}

export function ProtectedRoute({ children }: ProtectedRouteProps) {
  const { isAuthenticated } = useAuth()
  const location = useLocation()

  if (!isAuthenticated) {
    return <Navigate to={ROUTES.LOGIN} state={{ from: location }} replace />
  }
  return <>{children}</>
}
