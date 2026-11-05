import { Link } from 'react-router-dom'
import type { EstimateSummary } from '../../interfaces/estimate'
import { estimateStatusNames } from '../../utils/estimateStatus'
import { formatMoney } from '../../utils/format'
import { ui } from '../../utils/ui'

/** A project's estimates for a Manager: name, status as text, and total with VAT. */
export default function ProjectEstimatesTable({ estimates }: { estimates: EstimateSummary[] }) {
  return (
    <div className={ui.tableWrapper}>
      <table className={ui.listTable}>
        <thead className={ui.listHead}>
          <tr>
            <th className={ui.cell}>Pavadinimas</th>
            <th className={ui.cell}>Būsena</th>
            <th className={`${ui.cell} text-right`}>Suma su PVM</th>
            <th className={ui.cell}>Veiksmai</th>
          </tr>
        </thead>
        <tbody className={ui.listBody}>
          {estimates.map((estimate) => (
            <tr className={ui.listRow} key={estimate.id}>
              <td className={ui.listTitleCell}>{estimate.name}</td>
              <td className={ui.listCell} data-label="Būsena">{estimateStatusNames[estimate.status]}</td>
              <td className={`${ui.listCell} text-right`} data-label="Suma su PVM">{formatMoney(estimate.total)}</td>
              <td className={ui.listActionsCell}>
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
