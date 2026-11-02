import { useState, type ChangeEvent, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { ApiError } from '../../api/http'
import { createViewer } from '../../api/team'
import type { CreateViewerRequest } from '../../interfaces/team'
import { emailError, passwordError } from '../../utils/validation'
import { ui } from '../../utils/ui'

type ViewerErrors = Partial<Record<keyof CreateViewerRequest, string>>

const emptyForm: CreateViewerRequest = { firstName: '', lastName: '', email: '', password: '' }

const fields: { name: keyof CreateViewerRequest; label: string; type: string }[] = [
  { name: 'firstName', label: 'Vardas', type: 'text' },
  { name: 'lastName', label: 'Pavardė', type: 'text' },
  { name: 'email', label: 'El. paštas', type: 'email' },
  { name: 'password', label: 'Slaptažodis', type: 'password' },
]

function validate(form: CreateViewerRequest): ViewerErrors {
  const errors: ViewerErrors = {
    email: emailError(form.email),
    password: passwordError(form.password, form.email),
  }
  if (!form.firstName.trim()) errors.firstName = 'Įveskite vardą.'
  if (!form.lastName.trim()) errors.lastName = 'Įveskite pavardę.'
  return Object.fromEntries(Object.entries(errors).filter(([, message]) => message)) as ViewerErrors
}

export default function ViewerFormPage() {
  const navigate = useNavigate()
  const [form, setForm] = useState<CreateViewerRequest>(emptyForm)
  const [errors, setErrors] = useState<ViewerErrors>({})
  const [submitError, setSubmitError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  function handleChange(event: ChangeEvent<HTMLInputElement>) {
    const { name, value } = event.target
    setForm((current) => ({ ...current, [name]: value }))
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setSubmitError(null)

    const validationErrors = validate(form)
    setErrors(validationErrors)
    if (Object.keys(validationErrors).length > 0) return

    setSubmitting(true)
    try {
      await createViewer({ ...form, email: form.email.trim() })
      navigate('/team', { state: { notice: 'Peržiūrėtojas sukurtas.' } })
    } catch (error) {
      if (error instanceof ApiError && error.status === 409) {
        setSubmitError('Paskyra su šiuo el. paštu jau egzistuoja.')
      } else if (error instanceof ApiError && error.status === 400) {
        setSubmitError('Patikrinkite įvestus duomenis.')
      } else {
        setSubmitError('Nepavyko sukurti peržiūrėtojo. Bandykite dar kartą.')
      }
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <main className={ui.formPage}>
      <h1 className={ui.h1}>Naujas peržiūrėtojas</h1>

      <form className={ui.form} onSubmit={handleSubmit} noValidate>
        {fields.map((field) => (
          <div className={ui.field} key={field.name}>
            <label className={ui.label} htmlFor={field.name}>{field.label}</label>
            <input
              className={ui.input}
              id={field.name}
              name={field.name}
              type={field.type}
              autoComplete={field.type === 'password' ? 'new-password' : 'off'}
              value={form[field.name]}
              onChange={handleChange}
              aria-invalid={errors[field.name] ? true : undefined}
              aria-describedby={errors[field.name] ? `${field.name}-error` : undefined}
            />
            {errors[field.name] && (
              <span className={ui.fieldError} id={`${field.name}-error`}>
                {errors[field.name]}
              </span>
            )}
          </div>
        ))}
        <p>Prisijungimo duomenis perduokite darbuotojui patys.</p>

        {submitError && (
          <p className={ui.fieldError} role="alert">
            {submitError}
          </p>
        )}

        <button type="submit" className={ui.primaryButton} disabled={submitting}>
          {submitting ? 'Saugoma…' : 'Išsaugoti'}
        </button>
      </form>

      <p className={ui.formFooter}>
        <Link className={ui.link} to="/team">Atšaukti</Link>
      </p>
    </main>
  )
}
