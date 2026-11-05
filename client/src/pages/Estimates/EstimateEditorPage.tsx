import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import {
  addWorkLine,
  changeLabourRate,
  changeMaterialPrice,
  changePercentages,
  changeQuantity,
  confirmEstimate,
  getEstimate,
  removeMaterialLine,
  removeWorkLine,
  replaceMaterial,
  returnEstimateToDraft,
} from '../../api/estimates'
import { ApiError } from '../../api/http'
import { getMaterials } from '../../api/materials'
import { getWorks } from '../../api/works'
import ConfirmDialog from '../../components/ConfirmDialog'
import { EstimateStatus, type Estimate, type EstimateMaterialLine, type EstimateWorkLine } from '../../interfaces/estimate'
import type { Material } from '../../interfaces/material'
import type { Work } from '../../interfaces/work'
import { ui } from '../../utils/ui'
import AddWorkForm from './AddWorkForm'
import EstimateExportButton from './EstimateExportButton'
import EstimatePercentagesForm from './EstimatePercentagesForm'
import EstimateStatusActions from './EstimateStatusActions'
import EstimateTotals from './EstimateTotals'
import EstimateWorkLineRow from './EstimateWorkLineRow'
import { unexpectedError } from '../../utils/errors'

function saveErrorMessage(error: unknown): string {
  if (error instanceof ApiError && error.status === 400) return 'Patikrinkite įvestus duomenis.'
  if (error instanceof ApiError && error.status === 409) return 'Sąmatos būsena jau pasikeitė, todėl pakeitimas neišsaugotas.'
  return unexpectedError('išsaugoti pakeitimo')
}

/**
 * The Estimate Editor. Each change is saved at once and the page shows the estimate the server sends back,
 * so every amount on screen is the server's calculation. A Confirmed estimate is shown read-only.
 */
export default function EstimateEditorPage() {
  const estimateId = Number(useParams().id)

  const [estimate, setEstimate] = useState<Estimate | null>(null)
  const [works, setWorks] = useState<Work[]>([])
  const [error, setError] = useState<string | null>(null)
  const [toRemove, setToRemove] = useState<EstimateWorkLine | null>(null)
  const [removing, setRemoving] = useState(false)
  const [materials, setMaterials] = useState<Material[]>([])
  const [materialToRemove, setMaterialToRemove] = useState<{ line: EstimateWorkLine; material: EstimateMaterialLine } | null>(null)

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
            : unexpectedError('įkelti sąmatos'),
        ),
      )
  }, [estimateId])

  const loaded = estimate !== null
  const readOnly = estimate?.status === EstimateStatus.Confirmed

  // The catalog is only needed to offer replacement materials, so a Confirmed estimate does not load it.
  useEffect(() => {
    if (!loaded || readOnly) return
    getMaterials()
      .then(setMaterials)
      .catch(() => setMaterials([]))
  }, [loaded, readOnly])

  // Runs a change and shows the recalculated estimate, or a message when the server rejects it.
  async function apply(change: () => Promise<Estimate>) {
    try {
      setEstimate(await change())
      setError(null)
    } catch (err) {
      await showFailure(err)
    }
  }

  // Like apply, but a 400 is left to the field that sent the value: it resolves to false so the field shows
  // its own message.
  async function applyField(change: () => Promise<Estimate>): Promise<boolean> {
    try {
      setEstimate(await change())
      setError(null)
    } catch (err) {
      if (err instanceof ApiError && err.status === 400) return false
      await showFailure(err)
    }
    return true
  }

  // A 409 means the status changed elsewhere (e.g. another tab), so the estimate is reloaded and shown as it now is.
  async function showFailure(err: unknown) {
    setError(saveErrorMessage(err))
    if (err instanceof ApiError && err.status === 409) {
      setEstimate(await getEstimate(estimateId))
    }
  }

  async function handleConfirmRemove() {
    if (!toRemove) return
    setRemoving(true)
    await apply(() => removeWorkLine(estimateId, toRemove.id))
    setRemoving(false)
    setToRemove(null)
  }

  async function handleConfirmMaterialRemove() {
    if (!materialToRemove) return
    setRemoving(true)
    await apply(() => removeMaterialLine(estimateId, materialToRemove.line.id, materialToRemove.material.id))
    setRemoving(false)
    setMaterialToRemove(null)
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
            <div className="flex flex-wrap items-start gap-3">
              <EstimateStatusActions
                status={estimate.status}
                onConfirm={() => apply(() => confirmEstimate(estimateId))}
                onReturnToDraft={() => apply(() => returnEstimateToDraft(estimateId))}
              />
              <EstimateExportButton estimateId={estimateId} estimateName={estimate.name} />
            </div>
          </div>

          {!readOnly && (
            <AddWorkForm works={works} onAdd={(workId, quantity) => apply(() => addWorkLine(estimateId, workId, quantity))} />
          )}

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
                    key={`${line.id}-${estimate.status}`}
                    line={line}
                    catalog={materials}
                    readOnly={readOnly}
                    onQuantityChange={(lineId, quantity) => applyField(() => changeQuantity(estimateId, lineId, quantity))}
                    onLabourRateChange={(lineId, labourRate) =>
                      applyField(() => changeLabourRate(estimateId, lineId, labourRate))
                    }
                    onMaterialPriceChange={(lineId, materialLineId, price) =>
                      applyField(() => changeMaterialPrice(estimateId, lineId, materialLineId, price))
                    }
                    onMaterialReplace={(lineId, materialLineId, materialId) =>
                      applyField(() => replaceMaterial(estimateId, lineId, materialLineId, materialId))
                    }
                    onRemove={setToRemove}
                    onMaterialRemove={(removedLine, material) => setMaterialToRemove({ line: removedLine, material })}
                  />
                ))}
              </table>
            </div>
          )}

          <EstimatePercentagesForm
            key={`${estimate.status}-${estimate.overheadPercent}-${estimate.profitPercent}-${estimate.vatPercent}`}
            estimate={estimate}
            readOnly={readOnly}
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
      <ConfirmDialog
        open={materialToRemove !== null}
        title="Pašalinti medžiagą?"
        message={materialToRemove ? `Pašalinti medžiagą „${materialToRemove.material.materialName}“ iš šios sąmatos?` : ''}
        confirmLabel="Pašalinti"
        busy={removing}
        onConfirm={handleConfirmMaterialRemove}
        onCancel={() => setMaterialToRemove(null)}
      />
    </main>
  )
}
