import { useState } from 'react'
import { Link, useLocation } from 'react-router-dom'
import { deleteWork, getWorks } from '../../api/works'
import ConfirmDialog from '../../components/ConfirmDialog'
import SearchField from '../../components/SearchField'
import { useDelayedSearch } from '../../hooks/useDelayedSearch'
import type { Work } from '../../interfaces/work'
import { ui } from '../../utils/ui'
import WorksTable from './WorksTable'

export default function WorksPage() {
  const location = useLocation()
  const notice = (location.state as { notice?: string } | null)?.notice

  const { search, setSearch, items: works, setItems: setWorks, error, setError } = useDelayedSearch(
    getWorks,
    'Nepavyko įkelti darbų. Bandykite dar kartą.',
  )
  const [toDelete, setToDelete] = useState<Work | null>(null)
  const [deleting, setDeleting] = useState(false)

  async function handleConfirmDelete() {
    if (!toDelete) return
    setDeleting(true)
    try {
      await deleteWork(toDelete.id)
      setWorks((current) => current?.filter((work) => work.id !== toDelete.id) ?? null)
      setError(null)
    } catch {
      setError('Nepavyko ištrinti darbo. Bandykite dar kartą.')
    } finally {
      setDeleting(false)
      setToDelete(null)
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
        <h1 className={ui.h1}>Darbų katalogas</h1>
        <Link className={ui.addLink} to="/works/new">
          + Naujas darbas
        </Link>
      </div>

      <SearchField id="work-search" label="Paieška pagal pavadinimą" value={search} onChange={setSearch} />

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
      {!works && !error && <p>Įkeliama…</p>}

      {works && works.length === 0 && (
        <p>{search.trim() ? 'Pagal paiešką darbų nerasta.' : 'Katalogas tuščias. Pridėkite pirmą darbą.'}</p>
      )}

      {works && works.length > 0 && <WorksTable works={works} onDelete={setToDelete} />}

      <ConfirmDialog
        open={toDelete !== null}
        title="Ištrinti darbą?"
        message={toDelete ? `Darbas „${toDelete.name}“ (${toDelete.unit}) bus ištrintas iš katalogo.` : ''}
        confirmLabel="Ištrinti"
        busy={deleting}
        onConfirm={handleConfirmDelete}
        onCancel={() => setToDelete(null)}
      />
    </main>
  )
}
