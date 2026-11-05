import { useEffect, useState, type ChangeEvent, type FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { ApiError } from '../../api/http'
import { createProject, getProject, updateProject } from '../../api/projects'
import { ui } from '../../utils/ui'
import { unexpectedError } from '../../utils/errors'

interface ProjectForm {
  name: string
  description: string
}

type ProjectErrors = Partial<Record<keyof ProjectForm, string>>

// The same limits as ProjectRules on the server, which stays the authoritative check.
const nameMaxLength = 200
const descriptionMaxLength = 2000

function validate(form: ProjectForm): ProjectErrors {
  const errors: ProjectErrors = {}
  if (!form.name.trim()) errors.name = 'Įveskite pavadinimą.'
  else if (form.name.trim().length > nameMaxLength) errors.name = `Pavadinimas gali būti iki ${nameMaxLength} simbolių.`
  if (form.description.length > descriptionMaxLength) {
    errors.description = `Aprašymas gali būti iki ${descriptionMaxLength} simbolių.`
  }
  return errors
}

/** Creates a project, or edits one when the route has an ID. */
export default function ProjectFormPage() {
  const navigate = useNavigate()
  const { id } = useParams()
  const projectId = id ? Number(id) : null

  const [form, setForm] = useState<ProjectForm>({ name: '', description: '' })
  const [loaded, setLoaded] = useState(projectId === null)
  const [errors, setErrors] = useState<ProjectErrors>({})
  const [submitError, setSubmitError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  useEffect(() => {
    if (projectId === null) return
    getProject(projectId)
      .then((project) => {
        setForm({ name: project.name, description: project.description ?? '' })
        setLoaded(true)
      })
      .catch((error) =>
        setSubmitError(
          error instanceof ApiError && error.status === 404
            ? 'Projektas nerastas.'
            : unexpectedError('įkelti projekto'),
        ),
      )
  }, [projectId])

  function handleChange(event: ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) {
    const { name, value } = event.target
    setForm((current) => ({ ...current, [name]: value }))
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setSubmitError(null)

    const validationErrors = validate(form)
    setErrors(validationErrors)
    if (Object.keys(validationErrors).length > 0) return

    const request = { name: form.name.trim(), description: form.description.trim() || null }
    setSubmitting(true)
    try {
      const saved = projectId === null ? await createProject(request) : await updateProject(projectId, request)
      navigate(`/projects/${saved.id}`)
    } catch (error) {
      if (error instanceof ApiError && error.status === 400) setSubmitError('Patikrinkite įvestus duomenis.')
      else if (error instanceof ApiError && error.status === 404) setSubmitError('Projektas nerastas.')
      else setSubmitError(unexpectedError('išsaugoti projekto'))
    } finally {
      setSubmitting(false)
    }
  }

  function fieldProps(name: keyof ProjectForm) {
    return {
      id: name,
      name,
      value: form[name],
      onChange: handleChange,
      'aria-invalid': errors[name] ? true : undefined,
      'aria-describedby': errors[name] ? `${name}-error` : undefined,
    }
  }

  function fieldError(name: keyof ProjectForm) {
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
      <h1 className={ui.h1}>{projectId === null ? 'Naujas projektas' : 'Redaguoti projektą'}</h1>

      {loaded && (
        <form className={ui.form} onSubmit={handleSubmit} noValidate>
          <div className={ui.field}>
            <label className={ui.label} htmlFor="name">
              Pavadinimas
            </label>
            <input className={ui.input} type="text" autoComplete="off" maxLength={nameMaxLength} {...fieldProps('name')} />
            {fieldError('name')}
          </div>

          <div className={ui.field}>
            <label className={ui.label} htmlFor="description">
              Aprašymas (nebūtinas)
            </label>
            <textarea className={`${ui.input} min-h-32`} maxLength={descriptionMaxLength} {...fieldProps('description')} />
            {fieldError('description')}
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
        <Link className={ui.link} to={projectId === null ? '/projects' : `/projects/${projectId}`}>
          Atšaukti
        </Link>
      </p>
    </main>
  )
}
