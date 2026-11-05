import { useEffect, useState } from 'react'
import { getCurrentUser } from '../../api/auth'
import type { CurrentUser } from '../../interfaces/auth'
import QuickActions from './QuickActions'
import UserSummary from './UserSummary'
import { unexpectedError } from '../../utils/errors'

export default function DashboardPage() {
  const [user, setUser] = useState<CurrentUser | null>(null)
  const [loadFailed, setLoadFailed] = useState(false)

  useEffect(() => {
    getCurrentUser()
      .then(setUser)
      .catch(() => setLoadFailed(true))
  }, [])

  return (
    <div className="w-full px-4 pb-8">
      <main>
        {loadFailed && <p role="alert">{unexpectedError('įkelti duomenų')}</p>}
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
