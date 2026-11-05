import { Link } from 'react-router-dom'
import type { Work } from '../../interfaces/work'
import { formatMoney, formatQuantity } from '../../utils/format'
import { ui } from '../../utils/ui'

interface WorksTableProps {
  works: Work[]
  onDelete: (work: Work) => void
}

/** The work catalog as a table, with edit and delete actions per row. */
export default function WorksTable({ works, onDelete }: WorksTableProps) {
  return (
    <div className={ui.tableWrapper}>
      <table className={ui.listTable}>
        <thead className={ui.listHead}>
          <tr>
            <th className={ui.cell}>Pavadinimas</th>
            <th className={ui.cell}>Mato vnt.</th>
            <th className={`${ui.cell} text-right`}>Įkainis, €/val.</th>
            <th className={`${ui.cell} text-right`}>Darbo norma, val.</th>
            <th className={`${ui.cell} text-right`}>Medžiagos</th>
            <th className={ui.cell}>Veiksmai</th>
          </tr>
        </thead>
        <tbody className={ui.listBody}>
          {works.map((work) => (
            <tr className={ui.listRow} key={work.id}>
              <td className={ui.listTitleCell}>{work.name}</td>
              <td className={ui.listCell} data-label="Mato vnt.">{work.unit}</td>
              <td className={`${ui.listCell} text-right`} data-label="Įkainis, €/val.">{formatMoney(work.labourRate)}</td>
              <td className={`${ui.listCell} text-right`} data-label="Darbo norma, val.">{formatQuantity(work.labourNorm)}</td>
              <td className={`${ui.listCell} text-right`} data-label="Medžiagos">{work.lines.length}</td>
              <td className={ui.listActionsCell}>
                <Link className={ui.link} to={`/works/${work.id}/edit`}>
                  Redaguoti
                </Link>
                <button type="button" className={ui.dangerOutlineButton} onClick={() => onDelete(work)}>
                  Ištrinti
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
