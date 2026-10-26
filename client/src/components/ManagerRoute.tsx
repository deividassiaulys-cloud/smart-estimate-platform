import type { ReactNode } from 'react'
import { Navigate } from 'react-router-dom'
import { getRoleName, getToken } from '../utils/auth'

/** Shows its children only to a logged-in Manager; Viewers go to the Dashboard, others to Login. */
export default function ManagerRoute({ children }: { children: ReactNode }) {
  if (!getToken()) return <Navigate to="/login" replace />
  if (getRoleName() !== 'Manager') return <Navigate to="/dashboard" replace />
  return children
}
