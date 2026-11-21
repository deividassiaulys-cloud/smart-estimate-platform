import { useEffect, useState, type ChangeEvent, type FormEvent } from 'react'
import { createCatalogSection, getCatalogSection, updateCatalogSection } from '../../api/catalogSections'
import { ApiError } from '../../api/http'
import { getUnits } from '../../api/materials'
import { getWorks } from '../../api/works'
import type { CatalogSection, CatalogSectionRequest } from '../../interfaces/catalogSection'
import type { Work } from '../../interfaces/work'
import { unexpectedError } from '../../utils/errors'
import { toInputNumber } from '../../utils/format'
import { ui } from '../../utils/ui'
import { decimalPattern, normDecimals, parseDecimal } from '../../utils/validation'
import CatalogSectionLinesEditor, { type SectionLineErrors, type SectionLineForm } from './CatalogSectionLinesEditor'

interface SectionForm {
  name: string
  unit: string
}

type SectionErrors = Partial<Record<keyof SectionForm | 'lines', string>>

// The same limits as CatalogSectionRules on the server, which stays the authoritative check.
const coefficientPattern = decimalPattern(normDecimals)

function validate(form: SectionForm, units: string[], lineCount: number): SectionErrors {
  const errors: SectionErrors = {}
  if (!form.name.trim()) errors.name = 'Įveskite pavadinimą.'
  if (!units.includes(form.unit)) errors.unit = 'Pasirinkite mato vienetą.'
  if (lineCount === 0) errors.lines = 'Pridėkite bent vieną darbą.'
  return errors
}

function validateLine(line: SectionLineForm): SectionLineErrors {
  const errors: SectionLineErrors = {}
  if (!line.workId) errors.workId = 'Pasirinkite darbą.'
  if (!coefficientPattern.test(line.coefficient.trim()) || parseDecimal(line.coefficient) <= 0) {
    errors.coefficient = `Koeficientas turi būti didesnis už 0, ne daugiau kaip ${normDecimals} skaitmenys po kablelio.`
  }
  return errors
}

interface CatalogSectionFormProps {
  /** The item to edit, or null to create a new one. */
  sectionId: number | null
  /** The name a new item starts with, e.g. what was typed in an add dialog's search (spec 020). */
  initialName?: string
  /** Called with the saved item; the page navigates, the add dialog chooses it. */
  onSaved: (saved: CatalogSection) => void
}

/**
 * The catalog form for one catalog section, used by its catalog page and by the Estimate Editor's "create new"
 * (spec 020): same fields, rules and messages in both places.
 */
export default function CatalogSectionForm({ sectionId, initialName = '', onSaved }: CatalogSectionFormProps) {

  const [units, setUnits] = useState<string[]>([])
  const [works, setWorks] = useState<Work[]>([])
  const [form, setForm] = useState<SectionForm>({ name: initialName, unit: '' })
  const [lines, setLines] = useState<SectionLineForm[]>([])
  const [loaded, setLoaded] = useState(sectionId === null)
  const [errors, setErrors] = useState<SectionErrors>({})
  const [lineErrors, setLineErrors] = useState<SectionLineErrors[]>([])
  const [submitError, setSubmitError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  useEffect(() => {
    Promise.all([getUnits(), getWorks()])
      .then(([foundUnits, foundWorks]) => {
        setUnits(foundUnits)
        setWorks(foundWorks)
      })
      .catch(() => setSubmitError(unexpectedError('įkelti mato vienetų ir darbų')))
  }, [])

  useEffect(() => {
    if (sectionId === null) return
    getCatalogSection(sectionId)
      .then((section) => {
        setForm({ name: section.name, unit: section.unit })
        setLines(section.lines.map((line) => ({ workId: String(line.workId), coefficient: toInputNumber(line.coefficient) })))
        setLoaded(true)
      })
      .catch((error) =>
        setSubmitError(
          error instanceof ApiError && error.status === 404 ? 'Skyrius nerastas.' : unexpectedError('įkelti skyriaus'),
        ),
      )
  }, [sectionId])

  function handleChange(event: ChangeEvent<HTMLInputElement | HTMLSelectElement>) {
    const { name, value } = event.target
    setForm((current) => ({ ...current, [name]: value }))
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setSubmitError(null)

    const validationErrors = validate(form, units, lines.length)
    const validationLineErrors = lines.map(validateLine)
    setErrors(validationErrors)
    setLineErrors(validationLineErrors)
    if (Object.keys(validationErrors).length > 0 || validationLineErrors.some((e) => Object.keys(e).length > 0)) return

    const request: CatalogSectionRequest = {
      name: form.name.trim(),
      unit: form.unit,
      lines: lines.map((line) => ({ workId: Number(line.workId), coefficient: parseDecimal(line.coefficient) })),
    }

    setSubmitting(true)
    try {
      if (sectionId === null) {
        onSaved(await createCatalogSection(request))
      } else {
        onSaved(await updateCatalogSection(sectionId, request))
      }
    } catch (error) {
      if (error instanceof ApiError && error.status === 409) {
        setSubmitError('Skyrius su tokiu pavadinimu ir mato vienetu jau yra.')
      } else if (error instanceof ApiError && error.status === 400) {
        setSubmitError('Patikrinkite įvestus duomenis.')
      } else if (error instanceof ApiError && error.status === 404) {
        setSubmitError('Skyrius nerastas.')
      } else {
        setSubmitError(unexpectedError('išsaugoti skyriaus'))
      }
    } finally {
      setSubmitting(false)
    }
  }

  function fieldProps(name: keyof SectionForm) {
    return {
      id: name,
      name,
      value: form[name],
      onChange: handleChange,
      'aria-invalid': errors[name] ? true : undefined,
      'aria-describedby': errors[name] ? `${name}-error` : undefined,
    }
  }

  function fieldError(name: keyof SectionErrors) {
    return (
      errors[name] && (
        <span className={ui.fieldError} id={`${name}-error`}>
          {errors[name]}
        </span>
      )
    )
  }

  return (
    <>
      {loaded && (
        <form className={ui.form} onSubmit={handleSubmit} noValidate>
          <div className={`${ui.form} max-w-[420px]`}>
            <div className={ui.field}>
              <label className={ui.label} htmlFor="name">
                Pavadinimas
              </label>
              <input className={ui.input} type="text" autoComplete="off" maxLength={200} {...fieldProps('name')} />
              {fieldError('name')}
            </div>

            <div className={ui.field}>
              <label className={ui.label} htmlFor="unit">
                Mato vienetas
              </label>
              <select className={ui.input} {...fieldProps('unit')}>
                <option value="">Pasirinkite…</option>
                {units.map((unit) => (
                  <option key={unit} value={unit}>
                    {unit}
                  </option>
                ))}
              </select>
              {fieldError('unit')}
            </div>
          </div>

          <CatalogSectionLinesEditor lines={lines} works={works} unit={form.unit} errors={lineErrors} onChange={setLines} />
          {fieldError('lines')}

          {submitError && (
            <p className={ui.fieldError} role="alert">
              {submitError}
            </p>
          )}

          <button type="submit" className={`${ui.primaryButton} max-w-[420px]`} disabled={submitting}>
            {submitting ? 'Saugoma…' : 'Išsaugoti'}
          </button>
        </form>
      )}
      {!loaded && submitError && (
        <p className={ui.fieldError} role="alert">
          {submitError}
        </p>
      )}
      {!loaded && !submitError && <p>Įkeliama…</p>}
    </>
  )
}
