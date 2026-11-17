import ClampedText from '../../components/ClampedText'
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
                  <ClampedText text={project.name} />
                </Link>
              </td>
              <td className={`${ui.listCell} max-w-[22rem]`} data-label="Aprašymas">
                {/* A long description wraps onto up to 2 lines instead of widening the table (spec 020). */}
                <ClampedText text={project.description ?? ''} />
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
