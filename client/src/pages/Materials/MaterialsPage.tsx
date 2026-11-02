import { useEffect, useState } from 'react'
import { Link, useLocation } from 'react-router-dom'
import { ApiError } from '../../api/http'
import { deleteMaterial, getMaterials } from '../../api/materials'
import ConfirmDialog from '../../components/ConfirmDialog'
import type { Material } from '../../interfaces/material'
import './Materials.css'

/** How long to wait after the last keystroke before searching. */
const searchDelayMs = 300

const priceFormat = new Intl.NumberFormat('lt-LT', { style: 'currency', currency: 'EUR' })

export default function MaterialsPage() {
  const location = useLocation()
  const notice = (location.state as { notice?: string } | null)?.notice

  const [search, setSearch] = useState('')
  const [materials, setMaterials] = useState<Material[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [toDelete, setToDelete] = useState<Material | null>(null)
  const [deleting, setDeleting] = useState(false)

  useEffect(() => {
    let cancelled = false
    const timer = setTimeout(() => {
      getMaterials(search)
        .then((found) => {
          if (cancelled) return
          setMaterials(found)
          setError(null)
        })
        .catch(() => {
          if (!cancelled) setError('Nepavyko įkelti medžiagų. Bandykite dar kartą.')
        })
    }, search ? searchDelayMs : 0)
    return () => {
      cancelled = true
      clearTimeout(timer)
    }
  }, [search])

  async function handleConfirmDelete() {
    if (!toDelete) return
    setDeleting(true)
    try {
      await deleteMaterial(toDelete.id)
      setMaterials((current) => current?.filter((material) => material.id !== toDelete.id) ?? null)
      setError(null)
    } catch (deleteError) {
      if (deleteError instanceof ApiError && deleteError.status === 409) {
        setError('Medžiagos negalima ištrinti, nes ji naudojama darbuose.')
      } else {
        setError('Nepavyko ištrinti medžiagos. Bandykite dar kartą.')
      }
    } finally {
      setDeleting(false)
      setToDelete(null)
    }
  }

  return (
    <main className="materials">
      <p>
        <Link to="/dashboard">← Pagrindinis</Link>
      </p>
      <div className="materials-heading">
        <h1>Medžiagų katalogas</h1>
        <Link className="materials-add" to="/materials/new">
          + Nauja medžiaga
        </Link>
      </div>

      <div className="materials-search">
        <label htmlFor="material-search">Paieška pagal pavadinimą</label>
        <input
          id="material-search"
          type="search"
          value={search}
          onChange={(event) => setSearch(event.target.value)}
        />
      </div>

      {notice && (
        <p className="materials-notice" role="status">
          {notice}
        </p>
      )}
      {error && (
        <p className="materials-error" role="alert">
          {error}
        </p>
      )}
      {!materials && !error && <p>Įkeliama…</p>}

      {materials && materials.length === 0 && (
        <p>{search.trim() ? 'Pagal paiešką medžiagų nerasta.' : 'Katalogas tuščias. Pridėkite pirmą medžiagą.'}</p>
      )}

      {materials && materials.length > 0 && (
        <div className="materials-table-wrapper">
          <table className="materials-table">
            <thead>
              <tr>
                <th>Pavadinimas</th>
                <th>Mato vnt.</th>
                <th className="materials-price">Kaina</th>
                <th>Veiksmai</th>
              </tr>
            </thead>
            <tbody>
              {materials.map((material) => (
                <tr key={material.id}>
                  <td>{material.name}</td>
                  <td>{material.unit}</td>
                  <td className="materials-price">{priceFormat.format(material.price)}</td>
                  <td className="materials-actions">
                    <Link to={`/materials/${material.id}/edit`}>Redaguoti</Link>
                    <button type="button" onClick={() => setToDelete(material)}>
                      Ištrinti
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

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
