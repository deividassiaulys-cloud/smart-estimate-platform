import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { getCurrentUser } from '../../api/auth'
import type { CurrentUser } from '../../interfaces/auth'
import { logout } from '../../utils/auth'
import DashboardHeader from './DashboardHeader'
import QuickActions from './QuickActions'
import UserSummary from './UserSummary'

export default function DashboardPage() {
  const navigate = useNavigate()
  const [user, setUser] = useState<CurrentUser | null>(null)
  const [loadFailed, setLoadFailed] = useState(false)

  useEffect(() => {
    getCurrentUser()
      .then(setUser)
      .catch(() => setLoadFailed(true))
  }, [])

  function handleLogout() {
    logout()
    navigate('/login', { replace: true })
  }

  return (
    <div className="w-full px-4 pb-8">
      <DashboardHeader companyName={user?.companyName} onLogout={handleLogout} />

      <main>
        {loadFailed && <p role="alert">Nepavyko įkelti duomenų. Bandykite dar kartą.</p>}
        {!user && !loadFailed && <p>Įkeliama…</p>}
        {user && (
          <>
            <UserSummary user={user} />
            <QuickActions role={user.role} />
          </>
        )}
      </main>
    </div>
  )
}
