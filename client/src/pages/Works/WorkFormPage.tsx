import { useEffect, useState, type ChangeEvent, type FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { ApiError } from '../../api/http'
import { getMaterials, getUnits } from '../../api/materials'
import { createWork, getWork, updateWork } from '../../api/works'
import type { Material } from '../../interfaces/material'
import type { WorkRequest } from '../../interfaces/work'
import { toInputNumber } from '../../utils/format'
import { ui } from '../../utils/ui'
import { decimalPattern, parseDecimal } from '../../utils/validation'
import WorkLinesEditor, { type LineErrors, type LineForm } from './WorkLinesEditor'
import { unexpectedError } from '../../utils/errors'

interface WorkForm {
  name: string
  unit: string
  labourRate: string
  labourNorm: string
}

type WorkErrors = Partial<Record<keyof WorkForm, string>>

const emptyForm: WorkForm = { name: '', unit: '', labourRate: '', labourNorm: '' }

// The same limits as WorkRules on the server, which stays the authoritative check.
const moneyPattern = decimalPattern(2)
const normPattern = decimalPattern(3)

function validate(form: WorkForm, units: string[]): WorkErrors {
  const errors: WorkErrors = {}
  if (!form.name.trim()) errors.name = 'Įveskite pavadinimą.'
  if (!units.includes(form.unit)) errors.unit = 'Pasirinkite mato vienetą.'
  if (!moneyPattern.test(form.labourRate.trim())) {
    errors.labourRate = 'Įkainis turi būti 0 arba daugiau, ne daugiau kaip 2 skaitmenys po kablelio.'
  }
  if (!normPattern.test(form.labourNorm.trim())) {
    errors.labourNorm = 'Darbo norma turi būti 0 arba daugiau, ne daugiau kaip 3 skaitmenys po kablelio.'
  }
  return errors
}

function validateLine(line: LineForm): LineErrors {
  const errors: LineErrors = {}
  if (!line.materialId) errors.materialId = 'Pasirinkite medžiagą.'
  if (!normPattern.test(line.consumptionNorm.trim()) || parseDecimal(line.consumptionNorm) <= 0) {
    errors.consumptionNorm = 'Sąnaudų norma turi būti didesnė už 0, ne daugiau kaip 3 skaitmenys po kablelio.'
  }
  if (!moneyPattern.test(line.wastePercent.trim()) || parseDecimal(line.wastePercent) > 100) {
    errors.wastePercent = 'Atliekos turi būti nuo 0 iki 100 %, ne daugiau kaip 2 skaitmenys po kablelio.'
  }
  return errors
}

/** Creates a work, or edits one (including its whole composition) when the route has an ID. */
export default function WorkFormPage() {
  const navigate = useNavigate()
  const { id } = useParams()
  const workId = id ? Number(id) : null

  const [units, setUnits] = useState<string[]>([])
  const [materials, setMaterials] = useState<Material[]>([])
  const [form, setForm] = useState<WorkForm>(emptyForm)
  const [lines, setLines] = useState<LineForm[]>([])
  const [loaded, setLoaded] = useState(workId === null)
  const [errors, setErrors] = useState<WorkErrors>({})
  const [lineErrors, setLineErrors] = useState<LineErrors[]>([])
  const [submitError, setSubmitError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  useEffect(() => {
    Promise.all([getUnits(), getMaterials()])
      .then(([foundUnits, foundMaterials]) => {
        setUnits(foundUnits)
        setMaterials(foundMaterials)
      })
      .catch(() => setSubmitError(unexpectedError('įkelti mato vienetų ir medžiagų')))
  }, [])

  useEffect(() => {
    if (workId === null) return
    getWork(workId)
      .then((work) => {
        setForm({
          name: work.name,
          unit: work.unit,
          labourRate: toInputNumber(work.labourRate, 2),
          labourNorm: toInputNumber(work.labourNorm),
        })
        setLines(
          work.lines.map((line) => ({
            materialId: String(line.materialId),
            consumptionNorm: toInputNumber(line.consumptionNorm),
            wastePercent: toInputNumber(line.wastePercent),
          })),
        )
        setLoaded(true)
      })
      .catch((error) =>
        setSubmitError(
          error instanceof ApiError && error.status === 404
            ? 'Darbas nerastas.'
            : unexpectedError('įkelti darbo'),
        ),
      )
  }, [workId])

  function handleChange(event: ChangeEvent<HTMLInputElement | HTMLSelectElement>) {
    const { name, value } = event.target
    setForm((current) => ({ ...current, [name]: value }))
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setSubmitError(null)

    const validationErrors = validate(form, units)
    const validationLineErrors = lines.map(validateLine)
    setErrors(validationErrors)
    setLineErrors(validationLineErrors)
    const hasErrors =
      Object.keys(validationErrors).length > 0 || validationLineErrors.some((e) => Object.keys(e).length > 0)
    if (hasErrors) return

    const request: WorkRequest = {
      name: form.name.trim(),
      unit: form.unit,
      labourRate: parseDecimal(form.labourRate),
      labourNorm: parseDecimal(form.labourNorm),
      lines: lines.map((line) => ({
        materialId: Number(line.materialId),
        consumptionNorm: parseDecimal(line.consumptionNorm),
        wastePercent: parseDecimal(line.wastePercent),
      })),
    }

    setSubmitting(true)
    try {
      if (workId === null) {
        await createWork(request)
        navigate('/works', { state: { notice: 'Darbas sukurtas.' } })
      } else {
        await updateWork(workId, request)
        navigate('/works', { state: { notice: 'Darbas atnaujintas.' } })
      }
    } catch (error) {
      if (error instanceof ApiError && error.status === 409) {
        setSubmitError('Darbas su tokiu pavadinimu ir mato vienetu jau yra.')
      } else if (error instanceof ApiError && error.status === 400) {
        setSubmitError('Patikrinkite įvestus duomenis.')
      } else if (error instanceof ApiError && error.status === 404) {
        setSubmitError('Darbas nerastas.')
      } else {
        setSubmitError(unexpectedError('išsaugoti darbo'))
      }
    } finally {
      setSubmitting(false)
    }
  }

  function fieldProps(name: keyof WorkForm) {
    return {
      id: name,
      name,
      value: form[name],
      onChange: handleChange,
      'aria-invalid': errors[name] ? true : undefined,
      'aria-describedby': errors[name] ? `${name}-error` : undefined,
    }
  }

  function fieldError(name: keyof WorkForm) {
    return (
      errors[name] && (
        <span className={ui.fieldError} id={`${name}-error`}>
          {errors[name]}
        </span>
      )
    )
  }

  return (
    <main className={ui.listPage}>
      <h1 className={ui.h1}>{workId === null ? 'Naujas darbas' : 'Redaguoti darbą'}</h1>

      {loaded && (
        <form className={ui.form} onSubmit={handleSubmit} noValidate>
          {/* The single fields keep the usual form width; the material table below uses the full width. */}
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

            <div className={ui.field}>
              <label className={ui.label} htmlFor="labourRate">
                Darbo įkainis, €/val.
              </label>
              <input className={ui.input} type="text" inputMode="decimal" autoComplete="off" {...fieldProps('labourRate')} />
              {fieldError('labourRate')}
            </div>

            <div className={ui.field}>
              <label className={ui.label} htmlFor="labourNorm">
                Darbo norma, val. vienetui
              </label>
              <input className={ui.input} type="text" inputMode="decimal" autoComplete="off" {...fieldProps('labourNorm')} />
              {fieldError('labourNorm')}
            </div>
          </div>

          <WorkLinesEditor lines={lines} materials={materials} errors={lineErrors} onChange={setLines} />

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

      <p className={ui.formFooter}>
        <Link className={ui.link} to="/works">
          Atšaukti
        </Link>
      </p>
    </main>
  )
}
