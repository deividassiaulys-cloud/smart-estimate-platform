import { Link } from 'react-router-dom'
import type { CatalogSection } from '../../interfaces/catalogSection'
import { ui } from '../../utils/ui'

interface CatalogSectionsTableProps {
  sections: CatalogSection[]
  onDelete: (section: CatalogSection) => void
}

/** The catalog sections as a table, with edit and delete actions per row. */
export default function CatalogSectionsTable({ sections, onDelete }: CatalogSectionsTableProps) {
  return (
    <div className={ui.tableWrapper}>
      <table className={ui.listTable}>
        <thead className={ui.listHead}>
          <tr>
            <th className={ui.cell}>Pavadinimas</th>
            <th className={ui.cell}>Mato vnt.</th>
            <th className={`${ui.cell} text-right`}>Darbai</th>
            <th className={ui.cell}>Veiksmai</th>
          </tr>
        </thead>
        <tbody className={ui.listBody}>
          {sections.map((section) => (
            <tr className={ui.listRow} key={section.id}>
              <td className={ui.listTitleCell}>{section.name}</td>
              <td className={ui.listCell} data-label="Mato vnt.">{section.unit}</td>
              <td className={`${ui.listCell} text-right`} data-label="Darbai">{section.lines.length}</td>
              <td className={ui.listActionsCell}>
                <Link className={ui.link} to={`/catalog-sections/${section.id}/edit`}>
                  Redaguoti
                </Link>
                <button type="button" className={ui.dangerOutlineButton} onClick={() => onDelete(section)}>
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
