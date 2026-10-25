import type { ReactNode } from 'react'
import { Navigate } from 'react-router-dom'
import { getToken } from '../utils/auth'

/** Shows its children only to a logged-in user; everyone else is sent to Login. */
export default function ProtectedRoute({ children }: { children: ReactNode }) {
  if (!getToken()) return <Navigate to="/login" replace />
  return children
}
