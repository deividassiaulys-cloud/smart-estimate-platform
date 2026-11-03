import { Link } from 'react-router-dom'
import type { EstimateName } from '../../interfaces/estimate'
import { ui } from '../../utils/ui'

/** A project's estimates for a Viewer: names only, with no status or total (wireframe wf12, BR-005). */
export default function ProjectEstimateNamesTable({ estimates }: { estimates: EstimateName[] }) {
  return (
    <div className={ui.tableWrapper}>
      <table className={ui.table}>
        <thead>
          <tr>
            <th className={ui.cell}>Pavadinimas</th>
            <th className={ui.cell}>Veiksmai</th>
          </tr>
        </thead>
        <tbody>
          {estimates.map((estimate) => (
            <tr key={estimate.id}>
              <td className={ui.cell}>{estimate.name}</td>
              <td className={ui.cell}>
                <Link className={ui.link} to={`/estimates/${estimate.id}/view`}>
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
