import { useState } from 'react'
import { Link, useLocation } from 'react-router-dom'
import { deleteCatalogSection, getCatalogSections } from '../../api/catalogSections'
import ConfirmDialog from '../../components/ConfirmDialog'
import SearchField from '../../components/SearchField'
import { useDelayedSearch } from '../../hooks/useDelayedSearch'
import type { CatalogSection } from '../../interfaces/catalogSection'
import { unexpectedError } from '../../utils/errors'
import { ui } from '../../utils/ui'
import CatalogSectionsTable from './CatalogSectionsTable'

/** The catalog sections (FR-030): search, list, and delete with confirmation (NFR-011). */
export default function CatalogSectionsPage() {
  const location = useLocation()
  const notice = (location.state as { notice?: string } | null)?.notice

  const { search, setSearch, items: sections, setItems: setSections, error, setError } = useDelayedSearch(
    getCatalogSections,
    unexpectedError('įkelti skyrių'),
  )
  const [toDelete, setToDelete] = useState<CatalogSection | null>(null)
  const [deleting, setDeleting] = useState(false)

  async function handleConfirmDelete() {
    if (!toDelete) return
    setDeleting(true)
    try {
      await deleteCatalogSection(toDelete.id)
      setSections((current) => current?.filter((section) => section.id !== toDelete.id) ?? null)
      setError(null)
    } catch {
      setError(unexpectedError('ištrinti skyriaus'))
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
        <h1 className={ui.h1}>Skyrių katalogas</h1>
        <Link className={ui.addLink} to="/catalog-sections/new">
          + Naujas skyrius
        </Link>
      </div>

      <SearchField id="section-search" label="Paieška pagal pavadinimą" value={search} onChange={setSearch} />

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
      {!sections && !error && <p>Įkeliama…</p>}

      {sections && sections.length === 0 && (
        <p>{search.trim() ? 'Pagal paiešką skyrių nerasta.' : 'Katalogas tuščias. Pridėkite pirmą skyrių.'}</p>
      )}

      {sections && sections.length > 0 && <CatalogSectionsTable sections={sections} onDelete={setToDelete} />}

      <ConfirmDialog
        open={toDelete !== null}
        title="Ištrinti skyrių?"
        message={
          toDelete
            ? `Skyrius „${toDelete.name}“ (${toDelete.unit}) bus ištrintas iš katalogo. Sąmatose jau įdėti skyriai liks.`
            : ''
        }
        confirmLabel="Ištrinti"
        busy={deleting}
        onConfirm={handleConfirmDelete}
        onCancel={() => setToDelete(null)}
      />
    </main>
  )
}
