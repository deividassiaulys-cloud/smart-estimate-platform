import { useRef, useState, type FormEvent } from 'react'
import DialogFrame from '../../components/DialogFrame'
import SearchableList, { type SearchableItem } from '../../components/SearchableList'
import type { EstimateSection } from '../../interfaces/estimate'
import { ui } from '../../utils/ui'
import { decimalPattern, parseDecimal, quantityDecimals } from '../../utils/validation'

// The same limit as EstimateRules on the server, which stays the authoritative check.
const quantityPattern = decimalPattern(quantityDecimals)

/** A catalog work or section offered in the dialog, with the unit its quantity is entered in. */
export interface CatalogChoice extends SearchableItem {
  unit: string
}

interface AddFromCatalogDialogProps {
  title: string
  items: CatalogChoice[]
  /** Shown when nothing matches the search, e.g. "Darbų nerasta.". */
  noMatch: string
  /** E.g. "+ Kurti naują darbą". */
  createLabel: string
  /** An item to show as chosen when the dialog opens, e.g. one just created. */
  initialChosenId?: number | null
  /** The estimate's sections, offered only when adding a work (FR-033). */
  sections?: EstimateSection[]
  /** Adds the chosen item; resolves to an error message to show, or null when it was added. */
  onAdd: (id: number, quantity: number, sectionId: number | null) => Promise<string | null>
  /** Opens the catalog form for a new item, with what was typed in the search as its name (spec 021 Q1). */
  onCreate: (name: string) => void
  onClose: () => void
}

/**
 * Adds a catalog work or section to the estimate (spec 021): search and choose, enter the quantity (and, for a work,
 * its section), then "Pridėti". The add itself is the existing one (FR-012, FR-031, FR-033); a refusal is shown here
 * and the dialog stays open. "Create new" opens the catalog form for a missing item.
 */
export default function AddFromCatalogDialog({
  title,
  items,
  noMatch,
  createLabel,
  initialChosenId = null,
  sections = [],
  onAdd,
  onCreate,
  onClose,
}: AddFromCatalogDialogProps) {
  const [chosenId, setChosenId] = useState<number | null>(initialChosenId)
  const [quantity, setQuantity] = useState('')
  const [sectionId, setSectionId] = useState('')
  const [search, setSearch] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [adding, setAdding] = useState(false)
  const quantityRef = useRef<HTMLInputElement>(null)
  const chosen = items.find((item) => item.id === chosenId)

  function choose(id: number) {
    setChosenId(id)
    setError(null)
    quantityRef.current?.focus()
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!chosen) return setError('Pasirinkite iš sąrašo.')
    if (!quantityPattern.test(quantity.trim()) || parseDecimal(quantity) <= 0) {
      return setError(`Kiekis turi būti didesnis už 0, ne daugiau kaip ${quantityDecimals} skaitmenys po kablelio.`)
    }

    setError(null)
    setAdding(true)
    const refusal = await onAdd(chosen.id, parseDecimal(quantity), sectionId ? Number(sectionId) : null)
    setAdding(false)
    if (refusal) setError(refusal)
    else onClose()
  }

  return (
    <DialogFrame title={title} onClose={onClose}>
      <SearchableList items={items} noMatch={noMatch} chosenId={chosenId} onPick={choose} onSearchChange={setSearch} />
      <div>
        <button type="button" className={ui.smallButton} onClick={() => onCreate(search.trim())}>
          {createLabel}
        </button>
      </div>
      <form className="flex flex-col gap-3 border-t border-line pt-3" onSubmit={handleSubmit} noValidate>
        <p className="break-words">
          <span className={ui.label}>Pasirinkta: </span>
          {chosen ? chosen.name : 'nieko'}
        </p>
        <div className="flex flex-wrap items-end gap-3">
          <label className={ui.field}>
            <span className={ui.label}>Kiekis{chosen ? `, ${chosen.unit}` : ''}</span>
            <input
              ref={quantityRef}
              className={`${ui.input} w-32`}
              type="text"
              inputMode="decimal"
              autoComplete="off"
              value={quantity}
              onChange={(event) => setQuantity(event.target.value)}
            />
          </label>
          {sections.length > 0 && (
            <label className={ui.field}>
              <span className={ui.label}>Skyrius</span>
              <select className={ui.input} value={sectionId} onChange={(event) => setSectionId(event.target.value)}>
                <option value="">Be skyriaus</option>
                {sections.map((section) => (
                  <option key={section.id} value={section.id}>
                    {section.name}
                  </option>
                ))}
              </select>
            </label>
          )}
        </div>
        {error && (
          <p className={`${ui.fieldError} break-words`} role="alert">
            {error}
          </p>
        )}
        <div className="flex flex-wrap justify-end gap-3">
          <button type="button" className={ui.secondaryButton} onClick={onClose}>
            Atšaukti
          </button>
          <button type="submit" className={ui.primaryButton} disabled={adding}>
            {adding ? 'Pridedama…' : 'Pridėti'}
          </button>
        </div>
      </form>
    </DialogFrame>
  )
}
