import { useState, type KeyboardEvent } from 'react'
import type { Estimate, Percentages } from '../../interfaces/estimate'
import { ui } from '../../utils/ui'
import { decimalPattern, parseDecimal } from '../../utils/validation'

const percentPattern = decimalPattern(2)

const fields: { name: keyof Percentages; label: string }[] = [
  { name: 'overheadPercent', label: 'Pridėtinės išlaidos, %' },
  { name: 'profitPercent', label: 'Pelnas, %' },
  { name: 'vatPercent', label: 'PVM, %' },
]

interface EstimatePercentagesFormProps {
  estimate: Estimate
  /** A Confirmed estimate: the percentages cannot be changed. */
  readOnly: boolean
  onSave: (percentages: Percentages) => Promise<void>
}

/** Overhead, profit and VAT percentages, saved when a field loses focus or on Enter. */
export default function EstimatePercentagesForm({ estimate, readOnly, onSave }: EstimatePercentagesFormProps) {
  const [values, setValues] = useState<Record<keyof Percentages, string>>({
    overheadPercent: String(estimate.overheadPercent),
    profitPercent: String(estimate.profitPercent),
    vatPercent: String(estimate.vatPercent),
  })
  const [error, setError] = useState<string | null>(null)

  async function save() {
    const parsed = Object.fromEntries(fields.map((f) => [f.name, parseDecimal(values[f.name])])) as unknown as Percentages
    if (fields.every((f) => parsed[f.name] === estimate[f.name])) return
    const invalid = fields.find((f) => !percentPattern.test(values[f.name].trim()) || parsed[f.name] > 100)
    if (invalid) {
      setError(`${invalid.label.replace(', %', '')} turi būti nuo 0 iki 100, ne daugiau kaip 2 skaitmenys po kablelio.`)
      return
    }
    setError(null)
    await onSave(parsed)
  }

  function handleKeyDown(event: KeyboardEvent<HTMLInputElement>) {
    if (event.key === 'Enter') {
      event.preventDefault()
      void save()
    }
  }

  return (
    <div className="my-6 flex flex-col gap-2">
      <div className="flex flex-wrap gap-4">
        {fields.map((field) => (
          <div className={ui.field} key={field.name}>
            <label className={ui.label} htmlFor={field.name}>
              {field.label}
            </label>
            <input
              id={field.name}
              className={`${ui.input} w-32`}
              type="text"
              inputMode="decimal"
              autoComplete="off"
              disabled={readOnly}
              value={values[field.name]}
              onChange={(event) => setValues((current) => ({ ...current, [field.name]: event.target.value }))}
              onBlur={() => void save()}
              onKeyDown={handleKeyDown}
            />
          </div>
        ))}
      </div>
      {error && (
        <p className={ui.fieldError} role="alert">
          {error}
        </p>
      )}
    </div>
  )
}
