import { useEffect, useState, type FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { ApiError } from '../../api/http'
import { getTeam, resetViewerPassword } from '../../api/team'
import { UserRole } from '../../interfaces/auth'
import type { TeamMember } from '../../interfaces/team'
import { passwordError } from '../../utils/validation'
import { ui } from '../../utils/ui'
import { unexpectedError } from '../../utils/errors'

export default function ResetPasswordPage() {
  const navigate = useNavigate()
  const viewerId = Number(useParams().id)

  const [viewer, setViewer] = useState<TeamMember | null>(null)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  useEffect(() => {
    getTeam()
      .then((members) => {
        const found = members.find((member) => member.id === viewerId && member.role === UserRole.Viewer)
        if (found) setViewer(found)
        else setLoadError('Peržiūrėtojas nerastas.')
      })
      .catch(() => setLoadError(unexpectedError('įkelti duomenų')))
  }, [viewerId])

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!viewer) return

    const validationError = passwordError(password, viewer.email)
    setError(validationError ?? null)
    if (validationError) return

    setSubmitting(true)
    try {
      await resetViewerPassword(viewer.id, password)
      navigate('/team', { state: { notice: 'Slaptažodis atnaujintas.' } })
    } catch (err) {
      setError(
        err instanceof ApiError && err.status === 404
          ? 'Peržiūrėtojas nerastas.'
          : unexpectedError('pakeisti slaptažodžio'),
      )
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <main className={ui.formPage}>
      <h1 className={ui.h1}>Keisti slaptažodį</h1>
      {loadError && <p className={ui.fieldError} role="alert">{loadError}</p>}
      {!viewer && !loadError && <p>Įkeliama…</p>}

      {viewer && (
        <form className={ui.form} onSubmit={handleSubmit} noValidate>
          <p>
            {viewer.firstName} {viewer.lastName} ({viewer.email})
          </p>
          <div className={ui.field}>
            <label className={ui.label} htmlFor="newPassword">Naujas slaptažodis</label>
            <input
              className={ui.input}
              id="newPassword"
              type="password"
              autoComplete="new-password"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              aria-invalid={error ? true : undefined}
            />
          </div>
          <p>Visos šio peržiūrėtojo atidarytos sesijos bus uždarytos.</p>

          {error && (
            <p className={ui.fieldError} role="alert">
              {error}
            </p>
          )}

          <button type="submit" className={ui.primaryButton} disabled={submitting}>
            {submitting ? 'Saugoma…' : 'Išsaugoti'}
          </button>
        </form>
      )}

      <p className={ui.formFooter}>
        <Link className={ui.link} to="/team">Atšaukti</Link>
      </p>
    </main>
  )
}
