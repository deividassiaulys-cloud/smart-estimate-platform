import { useEffect, useState } from 'react'
import { Link, useLocation } from 'react-router-dom'
import { getTeam, removeViewer } from '../../api/team'
import ConfirmDialog from '../../components/ConfirmDialog'
import type { TeamMember } from '../../interfaces/team'
import { ui } from '../../utils/ui'
import TeamTable from './TeamTable'

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
    <main className={ui.listPage}>
      <p>
        <Link className={ui.link} to="/dashboard">
          ← Pagrindinis
        </Link>
      </p>
      <div className={ui.listHeading}>
        <h1 className={ui.h1}>Komanda</h1>
        <Link className={ui.addLink} to="/team/new">
          + Pridėti peržiūrėtoją
        </Link>
      </div>

      {notice && (
        <p className={ui.notice} role="status">
          {notice}
        </p>
      )}
      {error && (
        <p className={ui.error} role="alert">
          {error}
        </p>
      )}
      {!members && !error && <p>Įkeliama…</p>}

      {members && <TeamTable members={members} onRemove={setToRemove} />}

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
