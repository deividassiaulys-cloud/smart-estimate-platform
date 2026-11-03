import { useState, type FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { createEstimate } from '../../api/estimates'
import { ApiError } from '../../api/http'
import { ui } from '../../utils/ui'

// The same limit as EstimateRules on the server, which stays the authoritative check.
const nameMaxLength = 200

/** Names a new estimate of a project; it is created as Draft and opens in the Estimate Editor. */
export default function EstimateCreatePage() {
  const navigate = useNavigate()
  const projectId = Number(useParams().projectId)

  const [name, setName] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!name.trim()) return setError('Įveskite pavadinimą.')

    setError(null)
    setSubmitting(true)
    try {
      const estimate = await createEstimate(projectId, name.trim())
      navigate(`/estimates/${estimate.id}`)
    } catch (err) {
      setError(
        err instanceof ApiError && err.status === 404
          ? 'Projektas nerastas.'
          : 'Nepavyko sukurti sąmatos. Bandykite dar kartą.',
      )
      setSubmitting(false)
    }
  }

  return (
    <main className={ui.formPage}>
      <h1 className={ui.h1}>Nauja sąmata</h1>
      <form className={ui.form} onSubmit={handleSubmit} noValidate>
        <div className={ui.field}>
          <label className={ui.label} htmlFor="name">
            Pavadinimas
          </label>
          <input
            id="name"
            className={ui.input}
            type="text"
            autoComplete="off"
            maxLength={nameMaxLength}
            value={name}
            onChange={(event) => setName(event.target.value)}
            aria-invalid={error ? true : undefined}
          />
        </div>
        {error && (
          <p className={ui.fieldError} role="alert">
            {error}
          </p>
        )}
        <button type="submit" className={ui.primaryButton} disabled={submitting}>
          {submitting ? 'Kuriama…' : 'Sukurti'}
        </button>
      </form>
      <p className={ui.formFooter}>
        <Link className={ui.link} to={`/projects/${projectId}`}>
          Atšaukti
        </Link>
      </p>
    </main>
  )
}
