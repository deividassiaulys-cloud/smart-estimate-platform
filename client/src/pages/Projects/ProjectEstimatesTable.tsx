import { Link } from 'react-router-dom'
import type { EstimateSummary } from '../../interfaces/estimate'
import { estimateStatusNames } from '../../utils/estimateStatus'
import { formatMoney } from '../../utils/format'
import { ui } from '../../utils/ui'

/** A project's estimates for a Manager: name, status as text, and total with VAT. */
export default function ProjectEstimatesTable({ estimates }: { estimates: EstimateSummary[] }) {
  return (
    <div className={ui.tableWrapper}>
      <table className={ui.table}>
        <thead>
          <tr>
            <th className={ui.cell}>Pavadinimas</th>
            <th className={ui.cell}>Būsena</th>
            <th className={`${ui.cell} text-right`}>Suma su PVM</th>
            <th className={ui.cell}>Veiksmai</th>
          </tr>
        </thead>
        <tbody>
          {estimates.map((estimate) => (
            <tr key={estimate.id}>
              <td className={ui.cell}>{estimate.name}</td>
              <td className={ui.cell}>{estimateStatusNames[estimate.status]}</td>
              <td className={`${ui.cell} text-right`}>{formatMoney(estimate.total)}</td>
              <td className={ui.cell}>
                <Link className={ui.link} to={`/estimates/${estimate.id}`}>
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
