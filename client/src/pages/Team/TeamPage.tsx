import { useEffect, useState } from 'react'
import { Link, useLocation } from 'react-router-dom'
import { getTeam, removeViewer } from '../../api/team'
import ConfirmDialog from '../../components/ConfirmDialog'
import { UserRole } from '../../interfaces/auth'
import type { TeamMember } from '../../interfaces/team'
import './Team.css'

const roleNames: Record<UserRole, string> = {
  [UserRole.Manager]: 'Vadovas',
  [UserRole.Viewer]: 'Peržiūrėtojas',
}

export default function TeamPage() {
  const location = useLocation()
  const notice = (location.state as { notice?: string } | null)?.notice

  const [members, setMembers] = useState<TeamMember[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [toRemove, setToRemove] = useState<TeamMember | null>(null)
  const [removing, setRemoving] = useState(false)

  useEffect(() => {
    getTeam()
      .then(setMembers)
      .catch(() => setError('Nepavyko įkelti komandos. Bandykite dar kartą.'))
  }, [])

  async function handleConfirmRemove() {
    if (!toRemove) return
    setRemoving(true)
    try {
      await removeViewer(toRemove.id)
      setMembers((current) => current?.filter((member) => member.id !== toRemove.id) ?? null)
      setError(null)
    } catch {
      setError('Nepavyko pašalinti peržiūrėtojo. Bandykite dar kartą.')
    } finally {
      setRemoving(false)
      setToRemove(null)
    }
  }

  return (
    <main className="team">
      <p>
        <Link to="/dashboard">← Pagrindinis</Link>
      </p>
      <div className="team-heading">
        <h1>Komanda</h1>
        <Link className="team-add" to="/team/new">
          + Pridėti peržiūrėtoją
        </Link>
      </div>

      {notice && (
        <p className="team-notice" role="status">
          {notice}
        </p>
      )}
      {error && (
        <p className="team-error" role="alert">
          {error}
        </p>
      )}
      {!members && !error && <p>Įkeliama…</p>}

      {members && (
        <div className="team-table-wrapper">
          <table className="team-table">
            <thead>
              <tr>
                <th>Vardas</th>
                <th>El. paštas</th>
                <th>Rolė</th>
                <th>Veiksmai</th>
              </tr>
            </thead>
            <tbody>
              {members.map((member) => (
                <tr key={member.id}>
                  <td>
                    {member.firstName} {member.lastName}
                  </td>
                  <td>{member.email}</td>
                  <td>{roleNames[member.role]}</td>
                  <td className="team-actions">
                    {member.role === UserRole.Viewer && (
                      <>
                        <Link to={`/team/${member.id}/password`}>Keisti slaptažodį</Link>
                        <button type="button" onClick={() => setToRemove(member)}>
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
      )}

      <ConfirmDialog
        open={toRemove !== null}
        title="Pašalinti peržiūrėtoją?"
        message={
          toRemove
            ? `${toRemove.firstName} ${toRemove.lastName} nebegalės prisijungti, prieiga baigsis iš karto.`
            : ''
        }
        confirmLabel="Pašalinti"
        busy={removing}
        onConfirm={handleConfirmRemove}
        onCancel={() => setToRemove(null)}
      />
    </main>
  )
}
