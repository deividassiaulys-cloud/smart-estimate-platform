import { Fragment, useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import {
  addSection,
  addWorkLine,
  changeLabourRate,
  changeMaterialPrice,
  changePercentages,
  changeQuantity,
  changeSectionQuantity,
  confirmEstimate,
  getEstimate,
  refreshEstimate,
  removeMaterialLine,
  removeSection,
  removeWorkLine,
  replaceMaterial,
  returnEstimateToDraft,
} from '../../api/estimates'
import { getCatalogSections } from '../../api/catalogSections'
import { ApiError } from '../../api/http'
import { getMaterials } from '../../api/materials'
import { getWorks } from '../../api/works'
import ConfirmDialog from '../../components/ConfirmDialog'
import type { CatalogSection } from '../../interfaces/catalogSection'
import {
  EstimateStatus,
  type Estimate,
  type EstimateMaterialLine,
  type EstimateSection,
  type EstimateWorkLine,
} from '../../interfaces/estimate'
import type { Material } from '../../interfaces/material'
import type { Work } from '../../interfaces/work'
import { ui } from '../../utils/ui'
import DialogFrame from '../../components/DialogFrame'
import CatalogSectionForm from '../CatalogSections/CatalogSectionForm'
import WorkForm from '../Works/WorkForm'
import AddFromCatalogDialog from './AddFromCatalogDialog'
import EstimateExportButton from './EstimateExportButton'
import EstimatePercentagesForm from './EstimatePercentagesForm'
import EstimateRefreshButton from './EstimateRefreshButton'
import EstimateSectionHeaderRow from './EstimateSectionHeaderRow'
import EstimateStatusActions from './EstimateStatusActions'
import EstimateSubtotalRow from './EstimateSubtotalRow'
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
  const [catalogSections, setCatalogSections] = useState<CatalogSection[]>([])
  const [sectionToRemove, setSectionToRemove] = useState<EstimateSection | null>(null)
  // A section quantity waiting for the recalculation warning (FR-032); `done` tells the field the outcome.
  const [quantityChange, setQuantityChange] = useState<{
    section: EstimateSection
    quantity: number
    done: (accepted: boolean) => void
  } | null>(null)
  // The open add dialog (spec 020), with an item to show as chosen, e.g. one just created.
  const [addDialog, setAddDialog] = useState<{ kind: 'work' | 'section'; chosenId: number | null } | null>(null)
  // The catalog form opened from an add dialog's "create new", with the search text as the new item's name.
  const [creating, setCreating] = useState<{ kind: 'work' | 'section'; name: string } | null>(null)
  // Bumped when a quantity change is cancelled, so the section's field shows the stored quantity again.
  const [sectionFieldsVersion, setSectionFieldsVersion] = useState(0)

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
    getCatalogSections()
      .then(setCatalogSections)
      .catch(() => setCatalogSections([]))
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

  // Adds from a dialog: shows the recalculated estimate, or returns the message for the dialog to show. A 409 still
  // reloads the estimate, as for every other change.
  async function addFromDialog(change: () => Promise<Estimate>): Promise<string | null> {
    try {
      setEstimate(await change())
      setError(null)
      return null
    } catch (err) {
      if (err instanceof ApiError && err.status === 409) setEstimate(await getEstimate(estimateId))
      return saveErrorMessage(err)
    }
  }

  // Back from the catalog form to the add dialog it was opened from, with the new item chosen when one was saved.
  function returnToAddDialog(kind: 'work' | 'section', chosenId: number | null) {
    setCreating(null)
    setAddDialog({ kind, chosenId })
  }

  async function handleConfirmSectionRemove() {
    if (!sectionToRemove) return
    setRemoving(true)
    await apply(() => removeSection(estimateId, sectionToRemove.id))
    setRemoving(false)
    setSectionToRemove(null)
  }

  // The section quantity field resolves once the Manager confirms or cancels the recalculation warning.
  function requestQuantityChange(section: EstimateSection, quantity: number): Promise<boolean> {
    return new Promise((done) => setQuantityChange({ section, quantity, done }))
  }

  async function handleConfirmQuantityChange() {
    if (!quantityChange) return
    const { section, quantity, done } = quantityChange
    setQuantityChange(null)
    done(await applyField(() => changeSectionQuantity(estimateId, section.id, quantity)))
  }

  function handleCancelQuantityChange() {
    quantityChange?.done(true)
    setQuantityChange(null)
    setSectionFieldsVersion((version) => version + 1)
  }

  function renderLines(lines: EstimateWorkLine[]) {
    if (!estimate) return null
    return lines.map((line) => (
      <EstimateWorkLineRow
        key={`${line.id}-${estimate.status}`}
        line={line}
        catalog={materials}
        readOnly={readOnly}
        onQuantityChange={(lineId, quantity) => applyField(() => changeQuantity(estimateId, lineId, quantity))}
        onLabourRateChange={(lineId, labourRate) => applyField(() => changeLabourRate(estimateId, lineId, labourRate))}
        onMaterialPriceChange={(lineId, materialLineId, price) =>
          applyField(() => changeMaterialPrice(estimateId, lineId, materialLineId, price))
        }
        onMaterialReplace={(lineId, materialLineId, materialId) =>
          applyField(() => replaceMaterial(estimateId, lineId, materialLineId, materialId))
        }
        onRemove={setToRemove}
        onMaterialRemove={(removedLine, material) => setMaterialToRemove({ line: removedLine, material })}
      />
    ))
  }

  // With sections, the lines are grouped under their section with a subtotal, the lines without a section last;
  // without sections, the estimate looks as it always did (spec 019 Q2).
  function renderBody() {
    if (!estimate) return null
    if (estimate.sections.length === 0) return renderLines(estimate.workLines)
    const unsectioned = estimate.workLines.filter((line) => line.sectionId === null)
    return (
      <>
        {estimate.sections.map((section) => (
          <Fragment key={section.id}>
            <EstimateSectionHeaderRow
              key={`${section.id}-${section.quantity}-${estimate.status}-${sectionFieldsVersion}`}
              section={section}
              readOnly={readOnly}
              onQuantityChange={requestQuantityChange}
              onRemove={setSectionToRemove}
            />
            {renderLines(estimate.workLines.filter((line) => line.sectionId === section.id))}
            <EstimateSubtotalRow name={section.name} subtotal={section.subtotal} />
          </Fragment>
        ))}
        {unsectioned.length > 0 && (
          <>
            <EstimateSectionHeaderRow section={null} readOnly={readOnly} onQuantityChange={requestQuantityChange} onRemove={setSectionToRemove} />
            {renderLines(unsectioned)}
            <EstimateSubtotalRow name="Be skyriaus" subtotal={estimate.unsectionedSubtotal} />
          </>
        )}
      </>
    )
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
              {!readOnly && (
                <EstimateRefreshButton
                  estimateId={estimateId}
                  onRefresh={() => apply(() => refreshEstimate(estimateId))}
                  onConflict={showFailure}
                />
              )}
              <EstimateExportButton estimateId={estimateId} estimateName={estimate.name} />
            </div>
          </div>

          {!readOnly && (
            <div className="my-4 flex flex-wrap gap-3">
              <button type="button" className={ui.secondaryButton} onClick={() => setAddDialog({ kind: 'section', chosenId: null })}>
                + Pridėti skyrių
              </button>
              <button type="button" className={ui.secondaryButton} onClick={() => setAddDialog({ kind: 'work', chosenId: null })}>
                + Pridėti darbą
              </button>
            </div>
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
                {renderBody()}
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
      {estimate && addDialog?.kind === 'work' && (
        <AddFromCatalogDialog
          title="Pridėti darbą"
          items={works.map((work) => ({ id: work.id, name: work.name, unit: work.unit, detail: work.unit }))}
          noMatch="Darbų nerasta."
          createLabel="+ Kurti naują darbą"
          initialChosenId={addDialog.chosenId}
          sections={estimate.sections}
          onAdd={(workId, quantity, sectionId) => addFromDialog(() => addWorkLine(estimateId, workId, quantity, sectionId))}
          onCreate={(name) => {
            setAddDialog(null)
            setCreating({ kind: 'work', name })
          }}
          onClose={() => setAddDialog(null)}
        />
      )}
      {addDialog?.kind === 'section' && (
        <AddFromCatalogDialog
          title="Pridėti skyrių"
          items={catalogSections.map((section) => ({
            id: section.id,
            name: section.name,
            unit: section.unit,
            detail: `${section.unit} · ${section.lines.length} darb.`,
          }))}
          noMatch="Skyrių nerasta."
          createLabel="+ Kurti naują skyrių"
          initialChosenId={addDialog.chosenId}
          onAdd={(catalogSectionId, quantity) => addFromDialog(() => addSection(estimateId, catalogSectionId, quantity))}
          onCreate={(name) => {
            setAddDialog(null)
            setCreating({ kind: 'section', name })
          }}
          onClose={() => setAddDialog(null)}
        />
      )}
      {creating && (
        <DialogFrame
          wide
          title={creating.kind === 'work' ? 'Naujas darbas' : 'Naujas skyrius'}
          onClose={() => returnToAddDialog(creating.kind, null)}
        >
          {creating.kind === 'work' ? (
            <WorkForm
              workId={null}
              initialName={creating.name}
              onSaved={(work) => {
                setWorks((current) => [...current, work])
                returnToAddDialog('work', work.id)
              }}
            />
          ) : (
            <CatalogSectionForm
              sectionId={null}
              initialName={creating.name}
              onSaved={(section) => {
                setCatalogSections((current) => [...current, section])
                returnToAddDialog('section', section.id)
              }}
            />
          )}
          <div className="flex justify-end">
            <button type="button" className={ui.secondaryButton} onClick={() => returnToAddDialog(creating.kind, null)}>
              Atšaukti
            </button>
          </div>
        </DialogFrame>
      )}
      <ConfirmDialog
        open={sectionToRemove !== null}
        title="Pašalinti skyrių?"
        message={sectionToRemove ? `Skyrius „${sectionToRemove.name}“ ir visi jo darbai su medžiagomis bus pašalinti iš sąmatos.` : ''}
        confirmLabel="Pašalinti"
        busy={removing}
        onConfirm={handleConfirmSectionRemove}
        onCancel={() => setSectionToRemove(null)}
      />
      <ConfirmDialog
        open={quantityChange !== null}
        title="Perskaičiuoti skyriaus darbus?"
        message="Visų skyriaus darbų kiekiai bus perskaičiuoti pagal naują skyriaus kiekį; ranka pakeisti kiekiai bus perrašyti. Ranka pridėti darbai liks nepakeisti."
        confirmLabel="Perskaičiuoti"
        confirmTone="primary"
        onConfirm={handleConfirmQuantityChange}
        onCancel={handleCancelQuantityChange}
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
