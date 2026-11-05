import { Link } from 'react-router-dom'
import type { Project } from '../../interfaces/project'
import { ui } from '../../utils/ui'

/** The company's projects; each name opens Project Detail. */
export default function ProjectsTable({ projects }: { projects: Project[] }) {
  return (
    <div className={ui.tableWrapper}>
      <table className={ui.listTable}>
        <thead className={ui.listHead}>
          <tr>
            <th className={ui.cell}>Pavadinimas</th>
            <th className={ui.cell}>Aprašymas</th>
          </tr>
        </thead>
        <tbody className={ui.listBody}>
          {projects.map((project) => (
            <tr className={ui.listRow} key={project.id}>
              <td className={ui.listTitleCell}>
                <Link className={ui.link} to={`/projects/${project.id}`}>
                  {project.name}
                </Link>
              </td>
              <td className={ui.listCell} data-label="Aprašymas">
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
