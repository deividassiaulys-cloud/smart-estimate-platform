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
      <table className={ui.table}>
        <thead>
          <tr>
            <th className={ui.cell}>Pavadinimas</th>
            <th className={ui.cell}>Mato vnt.</th>
            <th className={`${ui.cell} text-right`}>Įkainis, €/val.</th>
            <th className={`${ui.cell} text-right`}>Darbo norma, val.</th>
            <th className={`${ui.cell} text-right`}>Medžiagos</th>
            <th className={ui.cell}>Veiksmai</th>
          </tr>
        </thead>
        <tbody>
          {works.map((work) => (
            <tr key={work.id}>
              <td className={ui.cell}>{work.name}</td>
              <td className={ui.cell}>{work.unit}</td>
              <td className={`${ui.cell} text-right`}>{formatMoney(work.labourRate)}</td>
              <td className={`${ui.cell} text-right`}>{formatQuantity(work.labourNorm)}</td>
              <td className={`${ui.cell} text-right`}>{work.lines.length}</td>
              <td className={ui.actionsCell}>
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
