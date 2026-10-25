import { useState, type ChangeEvent, type FormEvent } from 'react'
import { ApiError, register } from '../../api/auth'
import type { RegisterRequest } from '../../interfaces/auth'
import { saveToken } from '../../utils/auth'
import { validateRegisterForm, type RegisterErrors } from './validation'
import './Register.css'

interface RegisterPageProps {
  /** Called after the visitor is registered and logged in. */
  onRegistered: () => void
}

const emptyForm: RegisterRequest = {
  companyName: '',
  firstName: '',
  lastName: '',
  email: '',
  password: '',
}

const fields: { name: keyof RegisterRequest; label: string; type: string; autoComplete: string }[] = [
  { name: 'companyName', label: 'Įmonės pavadinimas', type: 'text', autoComplete: 'organization' },
  { name: 'firstName', label: 'Vardas', type: 'text', autoComplete: 'given-name' },
  { name: 'lastName', label: 'Pavardė', type: 'text', autoComplete: 'family-name' },
  { name: 'email', label: 'El. paštas', type: 'email', autoComplete: 'email' },
  { name: 'password', label: 'Slaptažodis', type: 'password', autoComplete: 'new-password' },
]

function submitErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    if (error.status === 409) return 'Paskyra su šiuo el. paštu jau egzistuoja.'
    if (error.status === 400) return 'Patikrinkite įvestus duomenis. Slaptažodis turi būti bent 8 simbolių ir nesutapti su el. paštu.'
  }
  return 'Registracija nepavyko. Bandykite dar kartą.'
}

export default function RegisterPage({ onRegistered }: RegisterPageProps) {
  const [form, setForm] = useState<RegisterRequest>(emptyForm)
  const [errors, setErrors] = useState<RegisterErrors>({})
  const [submitError, setSubmitError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  function handleChange(event: ChangeEvent<HTMLInputElement>) {
    const { name, value } = event.target
    setForm((current) => ({ ...current, [name]: value }))
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setSubmitError(null)

    const validationErrors = validateRegisterForm(form)
    setErrors(validationErrors)
    if (Object.keys(validationErrors).length > 0) return

    setSubmitting(true)
    try {
      const response = await register({ ...form, email: form.email.trim() })
      saveToken(response.token)
      onRegistered()
    } catch (error) {
      setSubmitError(submitErrorMessage(error))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <main className="register">
      <h1>Registracija</h1>
      <p>Užregistruokite įmonę ir tapkite jos vadovu.</p>

      <form onSubmit={handleSubmit} noValidate>
        {fields.map((field) => (
          <div className="register-field" key={field.name}>
            <label htmlFor={field.name}>{field.label}</label>
            <input
              id={field.name}
              name={field.name}
              type={field.type}
              autoComplete={field.autoComplete}
              value={form[field.name]}
              onChange={handleChange}
              aria-invalid={errors[field.name] ? true : undefined}
              aria-describedby={errors[field.name] ? `${field.name}-error` : undefined}
            />
            {errors[field.name] && (
              <span className="register-error" id={`${field.name}-error`}>
                {errors[field.name]}
              </span>
            )}
          </div>
        ))}

        {submitError && (
          <p className="register-error" role="alert">
            {submitError}
          </p>
        )}

        <button type="submit" disabled={submitting}>
          {submitting ? 'Registruojama…' : 'Registruotis'}
        </button>
      </form>
    </main>
  )
}
