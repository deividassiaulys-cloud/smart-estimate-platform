import { useState, type FormEvent } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { login } from '../../api/auth'
import { ApiError } from '../../api/http'
import { saveToken } from '../../utils/auth'
import { ui } from '../../utils/ui'

function loginErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    if (error.status === 401) return 'Neteisingas el. paštas arba slaptažodis.'
    if (error.status === 423) {
      const minutes = error.body.minutesRemaining
      return `Paskyra laikinai užrakinta dėl per daug nesėkmingų bandymų. Bandykite dar kartą po ${minutes} min.`
    }
  }
  return 'Prisijungti nepavyko. Bandykite dar kartą.'
}

export default function LoginPage() {
  const navigate = useNavigate()
  const location = useLocation()
  const sessionExpired = (location.state as { sessionExpired?: boolean } | null)?.sessionExpired === true

  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!email.trim() || !password) {
      setError('Įveskite el. paštą ir slaptažodį.')
      return
    }

    setError(null)
    setSubmitting(true)
    try {
      const response = await login({ email: email.trim(), password })
      saveToken(response.token)
      navigate('/dashboard', { replace: true })
    } catch (err) {
      setError(loginErrorMessage(err))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <main className={ui.formPage}>
      <h1 className={ui.h1}>Prisijungimas</h1>

      <form className={ui.form} onSubmit={handleSubmit} noValidate>
        {sessionExpired && !error && (
          <p className={ui.notice} role="status">
            Jūsų sesija baigėsi, prisijunkite iš naujo.
          </p>
        )}

        <div className={ui.field}>
          <label className={ui.label} htmlFor="email">El. paštas</label>
          <input
            className={ui.input}
            id="email"
            type="email"
            autoComplete="email"
            value={email}
            onChange={(event) => setEmail(event.target.value)}
          />
        </div>

        <div className={ui.field}>
          <label className={ui.label} htmlFor="password">Slaptažodis</label>
          <input
            className={ui.input}
            id="password"
            type="password"
            autoComplete="current-password"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
          />
        </div>

        {error && (
          <p className={ui.fieldError} role="alert">
            {error}
          </p>
        )}

        <button type="submit" className={ui.primaryButton} disabled={submitting}>
          {submitting ? 'Jungiamasi…' : 'Prisijungti'}
        </button>
      </form>

      <p className={ui.formFooter}>
        Neturite paskyros? <Link className={ui.link} to="/register">Užregistruokite įmonę</Link>
      </p>
    </main>
  )
}
