import { useState } from 'react'
import { Link, useLocation } from 'react-router-dom'
import { ApiError } from '../../api/http'
import { deleteMaterial, getMaterials } from '../../api/materials'
import ConfirmDialog from '../../components/ConfirmDialog'
import SearchField from '../../components/SearchField'
import { useDelayedSearch } from '../../hooks/useDelayedSearch'
import type { Material } from '../../interfaces/material'
import { ui } from '../../utils/ui'
import MaterialsTable from './MaterialsTable'
import { unexpectedError } from '../../utils/errors'

export default function MaterialsPage() {
  const location = useLocation()
  const notice = (location.state as { notice?: string } | null)?.notice

  const { search, setSearch, items: materials, setItems: setMaterials, error, setError } = useDelayedSearch(
    getMaterials,
    unexpectedError('įkelti medžiagų'),
  )
  const [toDelete, setToDelete] = useState<Material | null>(null)
  const [deleting, setDeleting] = useState(false)

  async function handleConfirmDelete() {
    if (!toDelete) return
    setDeleting(true)
    try {
      await deleteMaterial(toDelete.id)
      setMaterials((current) => current?.filter((material) => material.id !== toDelete.id) ?? null)
      setError(null)
    } catch (deleteError) {
      if (deleteError instanceof ApiError && deleteError.status === 409) {
        const works = Array.isArray(deleteError.body.works) ? deleteError.body.works.join(', ') : ''
        setError(`Medžiagos negalima ištrinti, nes ji naudojama darbuose${works ? `: ${works}` : ''}.`)
      } else {
        setError(unexpectedError('ištrinti medžiagos'))
      }
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
        <h1 className={ui.h1}>Medžiagų katalogas</h1>
        <Link className={ui.addLink} to="/materials/new">
          + Nauja medžiaga
        </Link>
      </div>

      <SearchField id="material-search" label="Paieška pagal pavadinimą" value={search} onChange={setSearch} />

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
      {!materials && !error && <p>Įkeliama…</p>}

      {materials && materials.length === 0 && (
        <p>{search.trim() ? 'Pagal paiešką medžiagų nerasta.' : 'Katalogas tuščias. Pridėkite pirmą medžiagą.'}</p>
      )}

      {materials && materials.length > 0 && <MaterialsTable materials={materials} onDelete={setToDelete} />}

      <ConfirmDialog
        open={toDelete !== null}
        title="Ištrinti medžiagą?"
        message={toDelete ? `Medžiaga „${toDelete.name}“ (${toDelete.unit}) bus ištrinta iš katalogo.` : ''}
        confirmLabel="Ištrinti"
        busy={deleting}
        onConfirm={handleConfirmDelete}
        onCancel={() => setToDelete(null)}
      />
    </main>
  )
}
