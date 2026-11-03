import { useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { getProjectEstimateNames, getProjectEstimates } from '../../api/estimates'
import { ApiError } from '../../api/http'
import { deleteProject, getProject } from '../../api/projects'
import ConfirmDialog from '../../components/ConfirmDialog'
import type { EstimateName, EstimateSummary } from '../../interfaces/estimate'
import type { Project } from '../../interfaces/project'
import { isManager } from '../../utils/auth'
import { ui } from '../../utils/ui'
import ProjectEstimateNamesTable from './ProjectEstimateNamesTable'
import ProjectEstimatesTable from './ProjectEstimatesTable'

/**
 * One project: its name, description and estimates for every user. A Manager also sees each estimate's status
 * and total and can edit or delete the project; a Viewer gets the estimates' names only (BR-005).
 */
export default function ProjectDetailPage() {
  const navigate = useNavigate()
  const projectId = Number(useParams().id)

  const [project, setProject] = useState<Project | null>(null)
  const manager = isManager()
  const [estimates, setEstimates] = useState<EstimateSummary[] | null>(null)
  const [estimateNames, setEstimateNames] = useState<EstimateName[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [confirming, setConfirming] = useState(false)
  const [deleting, setDeleting] = useState(false)

  useEffect(() => {
    getProject(projectId)
      .then(setProject)
      .catch((err) =>
        setError(
          err instanceof ApiError && err.status === 404
            ? 'Projektas nerastas.'
            : 'Nepavyko įkelti projekto. Bandykite dar kartą.',
        ),
      )
    const estimatesLoadError = () => setError('Nepavyko įkelti sąmatų. Bandykite dar kartą.')
    if (manager) {
      getProjectEstimates(projectId).then(setEstimates).catch(estimatesLoadError)
    } else {
      getProjectEstimateNames(projectId).then(setEstimateNames).catch(estimatesLoadError)
    }
  }, [projectId, manager])

  const listed = manager ? estimates : estimateNames

  async function handleConfirmDelete() {
    setDeleting(true)
    try {
      await deleteProject(projectId)
      navigate('/projects', { state: { notice: 'Projektas ištrintas.' } })
    } catch {
      setError('Nepavyko ištrinti projekto. Bandykite dar kartą.')
      setDeleting(false)
      setConfirming(false)
    }
  }

  return (
    <main className={ui.listPage}>
      <p>
        <Link className={ui.link} to="/projects">
          ← Projektai
        </Link>
      </p>

      {error && (
        <p className={ui.error} role="alert">
          {error}
        </p>
      )}
      {!project && !error && <p>Įkeliama…</p>}

      {project && (
        <>
          <div className={ui.listHeading}>
            <h1 className={ui.h1}>{project.name}</h1>
            {manager && (
              <div className="flex items-center gap-3">
                <Link className={ui.link} to={`/projects/${project.id}/edit`}>
                  Redaguoti
                </Link>
                <button type="button" className={ui.dangerOutlineButton} onClick={() => setConfirming(true)}>
                  Ištrinti
                </button>
              </div>
            )}
          </div>
          {project.description && <p className="whitespace-pre-line">{project.description}</p>}

          <div className={`${ui.listHeading} mt-8`}>
            <h2 className={ui.h2}>Sąmatos</h2>
            {manager && (
              <Link className={ui.addLink} to={`/projects/${project.id}/estimates/new`}>
                + Nauja sąmata
              </Link>
            )}
          </div>
          {listed && listed.length === 0 && <p>Sąmatų dar nėra.</p>}
          {estimates && estimates.length > 0 && <ProjectEstimatesTable estimates={estimates} />}
          {estimateNames && estimateNames.length > 0 && <ProjectEstimateNamesTable estimates={estimateNames} />}
        </>
      )}

      <ConfirmDialog
        open={confirming && project !== null}
        title="Ištrinti projektą?"
        message={project ? `Projektas „${project.name}“ ir visos jo sąmatos bus ištrinti visam laikui.` : ''}
        confirmLabel="Ištrinti"
        busy={deleting}
        onConfirm={handleConfirmDelete}
        onCancel={() => setConfirming(false)}
      />
    </main>
  )
}
