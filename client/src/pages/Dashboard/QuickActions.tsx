import { Link } from 'react-router-dom'
import { UserRole } from '../../interfaces/auth'
import { ui } from '../../utils/ui'

interface QuickAction {
  label: string
  roles: UserRole[]
  /** Page the action opens; actions without one are shown disabled until their feature is built. */
  to?: string
}

const quickActions: QuickAction[] = [
  { label: 'Projektai', roles: [UserRole.Manager, UserRole.Viewer] },
  { label: 'Medžiagų katalogas', roles: [UserRole.Manager], to: '/materials' },
  { label: 'Darbų katalogas', roles: [UserRole.Manager], to: '/works' },
  { label: 'Komanda', roles: [UserRole.Manager], to: '/team' },
]

/** The Dashboard's quick actions that the given role may use. */
export default function QuickActions({ role }: { role: UserRole }) {
  return (
    <>
      <h2 className={ui.h2}>Greiti veiksmai</h2>
      <ul className="my-4 flex flex-wrap justify-center gap-3">
        {quickActions
          .filter((action) => action.roles.includes(role))
          .map((action) => (
            <li key={action.label}>
              {action.to ? (
                <Link className="inline-block rounded-md border border-accent-line px-3.5 py-2 text-accent" to={action.to}>
                  {action.label}
                </Link>
              ) : (
                <button type="button" className={ui.secondaryButton} disabled title="Netrukus">
                  {action.label}
                </button>
              )}
            </li>
          ))}
      </ul>
    </>
  )
}
