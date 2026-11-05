import { Link } from 'react-router-dom'
import type { EstimateName } from '../../interfaces/estimate'
import { ui } from '../../utils/ui'

/** A project's estimates for a Viewer: names only, with no status or total (wireframe wf12, BR-005). */
export default function ProjectEstimateNamesTable({ projectId, estimates }: { projectId: number; estimates: EstimateName[] }) {
  return (
    <div className={ui.tableWrapper}>
      <table className={ui.listTable}>
        <thead className={ui.listHead}>
          <tr>
            <th className={ui.cell}>Pavadinimas</th>
            <th className={ui.cell}>Veiksmai</th>
          </tr>
        </thead>
        <tbody className={ui.listBody}>
          {estimates.map((estimate) => (
            <tr className={ui.listRow} key={estimate.id}>
              <td className={ui.listTitleCell}>{estimate.name}</td>
              <td className={ui.listActionsCell}>
                <Link className={ui.link} to={`/estimates/${estimate.id}/view`} state={{ projectId }}>
                  Atidaryti
                </Link>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
