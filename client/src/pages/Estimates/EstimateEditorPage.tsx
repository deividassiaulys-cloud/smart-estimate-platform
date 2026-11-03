import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import {
  addWorkLine,
  changePercentages,
  changeQuantity,
  getEstimate,
  removeWorkLine,
} from '../../api/estimates'
import { ApiError } from '../../api/http'
import { getWorks } from '../../api/works'
import ConfirmDialog from '../../components/ConfirmDialog'
import type { Estimate, EstimateWorkLine } from '../../interfaces/estimate'
import type { Work } from '../../interfaces/work'
import { estimateStatusNames } from '../../utils/estimateStatus'
import { ui } from '../../utils/ui'
import AddWorkForm from './AddWorkForm'
import EstimatePercentagesForm from './EstimatePercentagesForm'
import EstimateTotals from './EstimateTotals'
import EstimateWorkLineRow from './EstimateWorkLineRow'

function saveErrorMessage(error: unknown): string {
  if (error instanceof ApiError && error.status === 400) return 'Patikrinkite įvestus duomenis.'
  if (error instanceof ApiError && error.status === 409) return 'Sąmata patvirtinta, jos keisti negalima.'
  return 'Nepavyko išsaugoti pakeitimo. Bandykite dar kartą.'
}

/**
 * The Estimate Editor. Each change is saved at once and the page shows the estimate the server sends back,
 * so every amount on screen is the server's calculation.
 */
export default function EstimateEditorPage() {
  const estimateId = Number(useParams().id)

  const [estimate, setEstimate] = useState<Estimate | null>(null)
  const [works, setWorks] = useState<Work[]>([])
  const [error, setError] = useState<string | null>(null)
  const [toRemove, setToRemove] = useState<EstimateWorkLine | null>(null)
  const [removing, setRemoving] = useState(false)

  useEffect(() => {
    Promise.all([getEstimate(estimateId), getWorks()])
      .then(([foundEstimate, foundWorks]) => {
        setEstimate(foundEstimate)
        setWorks(foundWorks)
      })
      .catch((err) =>
        setError(
          err instanceof ApiError && err.status === 404
            ? 'Sąmata nerasta.'
            : 'Nepavyko įkelti sąmatos. Bandykite dar kartą.',
        ),
      )
  }, [estimateId])

  // Runs a change and shows the recalculated estimate, or a message when the server rejects it.
  async function apply(change: () => Promise<Estimate>) {
    try {
      setEstimate(await change())
      setError(null)
    } catch (err) {
      setError(saveErrorMessage(err))
    }
  }

  async function handleConfirmRemove() {
    if (!toRemove) return
    setRemoving(true)
    await apply(() => removeWorkLine(estimateId, toRemove.id))
    setRemoving(false)
    setToRemove(null)
  }

  return (
    <main className={ui.listPage}>
      {estimate && (
        <p>
          <Link className={ui.link} to={`/projects/${estimate.projectId}`}>
            ← Projektas
          </Link>
        </p>
      )}

      {error && (
        <p className={ui.error} role="alert">
          {error}
        </p>
      )}
      {!estimate && !error && <p>Įkeliama…</p>}

      {estimate && (
        <>
          <div className={ui.listHeading}>
            <h1 className={ui.h1}>{estimate.name}</h1>
            <span className="rounded-full border border-line px-3 py-1 text-heading">
              {estimateStatusNames[estimate.status]}
            </span>
          </div>

          <AddWorkForm works={works} onAdd={(workId, quantity) => apply(() => addWorkLine(estimateId, workId, quantity))} />

          {estimate.workLines.length === 0 && <p>Sąmatoje dar nėra darbų.</p>}
          {estimate.workLines.length > 0 && (
            <div className={ui.tableWrapper}>
              <table className={ui.table}>
                <thead>
                  <tr>
                    <th className={ui.cell}>Pavadinimas</th>
                    <th className={ui.cell}>Mato vnt.</th>
                    <th className={ui.cell}>Kiekis</th>
                    <th className={ui.cell}>Norma ir kaina</th>
                    <th className={`${ui.cell} text-right`}>Suma</th>
                    <th className={ui.cell}></th>
                  </tr>
                </thead>
                {estimate.workLines.map((line) => (
                  <EstimateWorkLineRow
                    key={line.id}
                    line={line}
                    onQuantityChange={(lineId, quantity) => apply(() => changeQuantity(estimateId, lineId, quantity))}
                    onRemove={setToRemove}
                  />
                ))}
              </table>
            </div>
          )}

          <EstimatePercentagesForm
            estimate={estimate}
            onSave={(percentages) => apply(() => changePercentages(estimateId, percentages))}
          />
          <EstimateTotals estimate={estimate} />
        </>
      )}

      <ConfirmDialog
        open={toRemove !== null}
        title="Pašalinti darbą?"
        message={toRemove ? `Darbas „${toRemove.workName}“ ir visos jo medžiagos bus pašalinti iš sąmatos.` : ''}
        confirmLabel="Pašalinti"
        busy={removing}
        onConfirm={handleConfirmRemove}
        onCancel={() => setToRemove(null)}
      />
    </main>
  )
}
