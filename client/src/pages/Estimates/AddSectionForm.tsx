import { useState, type FormEvent } from 'react'
import type { CatalogSection } from '../../interfaces/catalogSection'
import { ui } from '../../utils/ui'
import { decimalPattern, parseDecimal, quantityDecimals } from '../../utils/validation'

// The same limit as EstimateRules on the server, which stays the authoritative check.
const quantityPattern = decimalPattern(quantityDecimals)

interface AddSectionFormProps {
  sections: CatalogSection[]
  onAdd: (catalogSectionId: number, quantity: number) => Promise<void>
}

/** Picks a catalog section and its quantity; the server adds all its works with their materials (FR-031). */
export default function AddSectionForm({ sections, onAdd }: AddSectionFormProps) {
  const [sectionId, setSectionId] = useState('')
  const [quantity, setQuantity] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [adding, setAdding] = useState(false)
  const unit = sections.find((section) => String(section.id) === sectionId)?.unit

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!sectionId) return setError('Pasirinkite skyrių.')
    if (!quantityPattern.test(quantity.trim()) || parseDecimal(quantity) <= 0) {
      return setError(`Kiekis turi būti didesnis už 0, ne daugiau kaip ${quantityDecimals} skaitmenys po kablelio.`)
    }

    setError(null)
    setAdding(true)
    try {
      await onAdd(Number(sectionId), parseDecimal(quantity))
      setSectionId('')
      setQuantity('')
    } finally {
      setAdding(false)
    }
  }

  return (
    <form className="my-4 flex flex-wrap items-end gap-3" onSubmit={handleSubmit} noValidate>
      <div className={ui.field}>
        <label className={ui.label} htmlFor="add-section">
          Skyrius iš katalogo
        </label>
        <select id="add-section" className={ui.input} value={sectionId} onChange={(event) => setSectionId(event.target.value)}>
          <option value="">Pasirinkite…</option>
          {sections.map((section) => (
            <option key={section.id} value={section.id}>
              {section.name} ({section.unit})
            </option>
          ))}
        </select>
      </div>
      <div className={ui.field}>
        <label className={ui.label} htmlFor="add-section-quantity">
          Skyriaus kiekis{unit ? `, ${unit}` : ''}
        </label>
        <input
          id="add-section-quantity"
          className={`${ui.input} w-32`}
          type="text"
          inputMode="decimal"
          autoComplete="off"
          value={quantity}
          onChange={(event) => setQuantity(event.target.value)}
        />
      </div>
      <button type="submit" className={ui.primaryButton} disabled={adding}>
        {adding ? 'Pridedama…' : 'Pridėti skyrių'}
      </button>
      {error && (
        <p className={`${ui.fieldError} w-full`} role="alert">
          {error}
        </p>
      )}
    </form>
  )
}
