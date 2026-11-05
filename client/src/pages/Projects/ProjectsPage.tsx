import { useEffect, useState } from 'react'
import { Link, useLocation } from 'react-router-dom'
import { getProjects } from '../../api/projects'
import type { Project } from '../../interfaces/project'
import { isManager } from '../../utils/auth'
import { ui } from '../../utils/ui'
import ProjectsTable from './ProjectsTable'
import { unexpectedError } from '../../utils/errors'

/** The company's projects, for Managers and Viewers; only a Manager can add one. */
export default function ProjectsPage() {
  const location = useLocation()
  const notice = (location.state as { notice?: string } | null)?.notice

  const [projects, setProjects] = useState<Project[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    getProjects()
      .then(setProjects)
      .catch(() => setError(unexpectedError('įkelti projektų')))
  }, [])

  return (
    <main className={ui.listPage}>
      <p>
        <Link className={ui.link} to="/dashboard">
          ← Pagrindinis
        </Link>
      </p>
      <div className={ui.listHeading}>
        <h1 className={ui.h1}>Projektai</h1>
        {isManager() && (
          <Link className={ui.addLink} to="/projects/new">
            + Naujas projektas
          </Link>
        )}
      </div>

      {notice && (
        <p className={ui.notice} role="status">
          {notice}
        </p>
      )}
      {error && (
        <p className={ui.error} role="alert">
          {error}
        </p>
      )}
      {!projects && !error && <p>Įkeliama…</p>}
      {projects && projects.length === 0 && <p>Projektų dar nėra.</p>}
      {projects && projects.length > 0 && <ProjectsTable projects={projects} />}
    </main>
  )
}
