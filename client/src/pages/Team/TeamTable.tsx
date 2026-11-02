import { Link } from 'react-router-dom'
import { UserRole } from '../../interfaces/auth'
import type { TeamMember } from '../../interfaces/team'
import { roleNames } from '../../utils/roles'
import { ui } from '../../utils/ui'

interface TeamTableProps {
  members: TeamMember[]
  onRemove: (member: TeamMember) => void
}

/** The company's members as a table; Viewers get password-reset and remove actions. */
export default function TeamTable({ members, onRemove }: TeamTableProps) {
  return (
    <div className={ui.tableWrapper}>
      <table className={ui.table}>
        <thead>
          <tr>
            <th className={ui.cell}>Vardas</th>
            <th className={ui.cell}>El. paštas</th>
            <th className={ui.cell}>Rolė</th>
            <th className={ui.cell}>Veiksmai</th>
          </tr>
        </thead>
        <tbody>
          {members.map((member) => (
            <tr key={member.id}>
              <td className={ui.cell}>
                {member.firstName} {member.lastName}
              </td>
              <td className={ui.cell}>{member.email}</td>
              <td className={ui.cell}>{roleNames[member.role]}</td>
              <td className={ui.actionsCell}>
                {member.role === UserRole.Viewer && (
                  <>
                    <Link className={ui.link} to={`/team/${member.id}/password`}>
                      Keisti slaptažodį
                    </Link>
                    <button type="button" className={ui.dangerOutlineButton} onClick={() => onRemove(member)}>
                      Pašalinti
                    </button>
                  </>
                )}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
