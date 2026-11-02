import { Link } from 'react-router-dom'
import type { Material } from '../../interfaces/material'
import { ui } from '../../utils/ui'

const priceFormat = new Intl.NumberFormat('lt-LT', { style: 'currency', currency: 'EUR' })

interface MaterialsTableProps {
  materials: Material[]
  onDelete: (material: Material) => void
}

/** The material catalog as a table, with edit and delete actions per row. */
export default function MaterialsTable({ materials, onDelete }: MaterialsTableProps) {
  return (
    <div className={ui.tableWrapper}>
      <table className={ui.table}>
        <thead>
          <tr>
            <th className={ui.cell}>Pavadinimas</th>
            <th className={ui.cell}>Mato vnt.</th>
            <th className={`${ui.cell} text-right`}>Kaina</th>
            <th className={ui.cell}>Veiksmai</th>
          </tr>
        </thead>
        <tbody>
          {materials.map((material) => (
            <tr key={material.id}>
              <td className={ui.cell}>{material.name}</td>
              <td className={ui.cell}>{material.unit}</td>
              <td className={`${ui.cell} text-right`}>{priceFormat.format(material.price)}</td>
              <td className={ui.actionsCell}>
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
