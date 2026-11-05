import { Link } from 'react-router-dom'
import type { Material } from '../../interfaces/material'
import { formatMoney } from '../../utils/format'
import { ui } from '../../utils/ui'

interface MaterialsTableProps {
  materials: Material[]
  onDelete: (material: Material) => void
}

/** The material catalog as a table, with edit and delete actions per row. */
export default function MaterialsTable({ materials, onDelete }: MaterialsTableProps) {
  return (
    <div className={ui.tableWrapper}>
      <table className={ui.listTable}>
        <thead className={ui.listHead}>
          <tr>
            <th className={ui.cell}>Pavadinimas</th>
            <th className={ui.cell}>Mato vnt.</th>
            <th className={`${ui.cell} text-right`}>Kaina</th>
            <th className={ui.cell}>Veiksmai</th>
          </tr>
        </thead>
        <tbody className={ui.listBody}>
          {materials.map((material) => (
            <tr className={ui.listRow} key={material.id}>
              <td className={ui.listTitleCell}>{material.name}</td>
              <td className={ui.listCell} data-label="Mato vnt.">{material.unit}</td>
              <td className={`${ui.listCell} text-right`} data-label="Kaina">{formatMoney(material.price)}</td>
              <td className={ui.listActionsCell}>
                <Link className={ui.link} to={`/materials/${material.id}/edit`}>
                  Redaguoti
                </Link>
                <button type="button" className={ui.dangerOutlineButton} onClick={() => onDelete(material)}>
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
