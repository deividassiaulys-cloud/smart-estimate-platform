import { Link } from 'react-router-dom'
import type { Project } from '../../interfaces/project'
import { ui } from '../../utils/ui'

/** The company's projects; each name opens Project Detail. */
export default function ProjectsTable({ projects }: { projects: Project[] }) {
  return (
    <div className={ui.tableWrapper}>
      <table className={ui.table}>
        <thead>
          <tr>
            <th className={ui.cell}>Pavadinimas</th>
            <th className={ui.cell}>Aprašymas</th>
          </tr>
        </thead>
        <tbody>
          {projects.map((project) => (
            <tr key={project.id}>
              <td className={ui.cell}>
                <Link className={ui.link} to={`/projects/${project.id}`}>
                  {project.name}
                </Link>
              </td>
              <td className={ui.cell}>
                {/* Table cells do not wrap; a long description must. */}
                <span className="whitespace-normal">{project.description}</span>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
