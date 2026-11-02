import type { CurrentUser } from '../../interfaces/auth'
import { roleNames } from '../../utils/roles'
import { ui } from '../../utils/ui'

/** Greeting with the logged-in user's name, company and role. */
export default function UserSummary({ user }: { user: CurrentUser }) {
  return (
    <>
      <h1 className={ui.h1}>Sveiki, {user.firstName}!</h1>
      <p>
        {user.firstName} {user.lastName} · {user.companyName} · {roleNames[user.role]}
      </p>
    </>
  )
}
