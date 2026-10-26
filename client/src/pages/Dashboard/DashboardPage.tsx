import { useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { getCurrentUser } from '../../api/auth'
import { UserRole, type CurrentUser } from '../../interfaces/auth'
import { logout } from '../../utils/auth'
import './Dashboard.css'

interface QuickAction {
  label: string
  roles: UserRole[]
  /** Page the action opens; actions without one are shown disabled until their feature is built. */
  to?: string
}

const quickActions: QuickAction[] = [
  { label: 'Projektai', roles: [UserRole.Manager, UserRole.Viewer] },
  { label: 'Medžiagų katalogas', roles: [UserRole.Manager] },
  { label: 'Darbų katalogas', roles: [UserRole.Manager] },
  { label: 'Komanda', roles: [UserRole.Manager], to: '/team' },
]

const roleNames: Record<UserRole, string> = {
  [UserRole.Manager]: 'Vadovas',
  [UserRole.Viewer]: 'Peržiūrėtojas',
}

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
    <div className="dashboard">
      <header className="dashboard-header">
        <span className="dashboard-company">{user?.companyName}</span>
        <button type="button" onClick={handleLogout}>
          Atsijungti
        </button>
      </header>

      <main>
        {loadFailed && <p role="alert">Nepavyko įkelti duomenų. Bandykite dar kartą.</p>}
        {!user && !loadFailed && <p>Įkeliama…</p>}
        {user && (
          <>
            <h1>Sveiki, {user.firstName}!</h1>
            <p>
              {user.firstName} {user.lastName} · {user.companyName} · {roleNames[user.role]}
            </p>

            <h2>Greiti veiksmai</h2>
            <ul className="dashboard-actions">
              {quickActions
                .filter((action) => action.roles.includes(user.role))
                .map((action) => (
                  <li key={action.label}>
                    {action.to ? (
                      <Link to={action.to}>{action.label}</Link>
                    ) : (
                      <button type="button" disabled title="Netrukus">
                        {action.label}
                      </button>
                    )}
                  </li>
                ))}
            </ul>
          </>
        )}
      </main>
    </div>
  )
}
