import { useEffect, useState, type ChangeEvent, type FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { ApiError } from '../../api/http'
import { createMaterial, getMaterial, getUnits, updateMaterial } from '../../api/materials'
import { toInputNumber } from '../../utils/format'
import { ui } from '../../utils/ui'
import { decimalPattern, parseDecimal } from '../../utils/validation'
import { unexpectedError } from '../../utils/errors'

interface MaterialForm {
  name: string
  unit: string
  price: string
}

type MaterialErrors = Partial<Record<keyof MaterialForm, string>>

const emptyForm: MaterialForm = { name: '', unit: '', price: '' }

const pricePattern = decimalPattern(2)

function validate(form: MaterialForm, units: string[]): MaterialErrors {
  const errors: MaterialErrors = {}
  if (!form.name.trim()) errors.name = 'Įveskite pavadinimą.'
  if (!units.includes(form.unit)) errors.unit = 'Pasirinkite mato vienetą.'
  if (!pricePattern.test(form.price.trim())) {
    errors.price = 'Kaina turi būti 0 arba daugiau, ne daugiau kaip 2 skaitmenys po kablelio.'
  }
  return errors
}

/** Creates a material, or edits one when the route has an ID. */
export default function MaterialFormPage() {
  const navigate = useNavigate()
  const { id } = useParams()
  const materialId = id ? Number(id) : null

  const [units, setUnits] = useState<string[]>([])
  const [form, setForm] = useState<MaterialForm>(emptyForm)
  const [loaded, setLoaded] = useState(materialId === null)
  const [errors, setErrors] = useState<MaterialErrors>({})
  const [submitError, setSubmitError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  useEffect(() => {
    getUnits()
      .then(setUnits)
      .catch(() => setSubmitError(unexpectedError('įkelti mato vienetų')))
  }, [])

  useEffect(() => {
    if (materialId === null) return
    getMaterial(materialId)
      .then((material) => {
        setForm({ name: material.name, unit: material.unit, price: toInputNumber(material.price, 2) })
        setLoaded(true)
      })
      .catch((error) =>
        setSubmitError(
          error instanceof ApiError && error.status === 404
            ? 'Medžiaga nerasta.'
            : unexpectedError('įkelti medžiagos'),
        ),
      )
  }, [materialId])

  function handleChange(event: ChangeEvent<HTMLInputElement | HTMLSelectElement>) {
    const { name, value } = event.target
    setForm((current) => ({ ...current, [name]: value }))
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setSubmitError(null)

    const validationErrors = validate(form, units)
    setErrors(validationErrors)
    if (Object.keys(validationErrors).length > 0) return

    const request = {
      name: form.name.trim(),
      unit: form.unit,
      price: parseDecimal(form.price),
    }

    setSubmitting(true)
    try {
      if (materialId === null) {
        await createMaterial(request)
        navigate('/materials', { state: { notice: 'Medžiaga sukurta.' } })
      } else {
        await updateMaterial(materialId, request)
        navigate('/materials', { state: { notice: 'Medžiaga atnaujinta.' } })
      }
    } catch (error) {
      if (error instanceof ApiError && error.status === 409) {
        setSubmitError('Medžiaga su tokiu pavadinimu ir mato vienetu jau yra.')
      } else if (error instanceof ApiError && error.status === 400) {
        setSubmitError('Patikrinkite įvestus duomenis.')
      } else if (error instanceof ApiError && error.status === 404) {
        setSubmitError('Medžiaga nerasta.')
      } else {
        setSubmitError(unexpectedError('išsaugoti medžiagos'))
      }
    } finally {
      setSubmitting(false)
    }
  }

  function fieldProps(name: keyof MaterialForm) {
    return {
      id: name,
      name,
      value: form[name],
      onChange: handleChange,
      'aria-invalid': errors[name] ? true : undefined,
      'aria-describedby': errors[name] ? `${name}-error` : undefined,
    }
  }

  function fieldError(name: keyof MaterialForm) {
    return (
      errors[name] && (
        <span className={ui.fieldError} id={`${name}-error`}>
          {errors[name]}
        </span>
      )
    )
  }

  return (
    <main className={ui.formPage}>
      <h1 className={ui.h1}>{materialId === null ? 'Nauja medžiaga' : 'Redaguoti medžiagą'}</h1>

      {loaded && (
        <form className={ui.form} onSubmit={handleSubmit} noValidate>
          <div className={ui.field}>
            <label className={ui.label} htmlFor="name">Pavadinimas</label>
            <input className={ui.input} type="text" autoComplete="off" maxLength={200} {...fieldProps('name')} />
            {fieldError('name')}
          </div>

          <div className={ui.field}>
            <label className={ui.label} htmlFor="unit">Mato vienetas</label>
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
            <label className={ui.label} htmlFor="price">Kaina, €</label>
            <input className={ui.input} type="text" inputMode="decimal" autoComplete="off" {...fieldProps('price')} />
            {fieldError('price')}
          </div>

          {submitError && (
            <p className={ui.fieldError} role="alert">
              {submitError}
            </p>
          )}

          <button type="submit" className={ui.primaryButton} disabled={submitting}>
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
        <Link className={ui.link} to="/materials">Atšaukti</Link>
      </p>
    </main>
  )
}
