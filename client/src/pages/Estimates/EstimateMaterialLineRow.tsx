import { useState, type ChangeEvent } from 'react'
import type { EstimateMaterialLine } from '../../interfaces/estimate'
import type { Material } from '../../interfaces/material'
import { formatMoney, formatQuantity } from '../../utils/format'
import { ui } from '../../utils/ui'
import InlineNumberField from './InlineNumberField'

interface EstimateMaterialLineRowProps {
  material: EstimateMaterialLine
  /** The company's catalog materials; only those with the line's unit are offered as a replacement. */
  catalog: Material[]
  /** A Confirmed estimate: nothing can be changed. */
  readOnly: boolean
  onPriceChange: (price: number) => Promise<boolean>
  /** Replaces the material; resolves to false when the server rejects the choice. */
  onReplace: (materialId: number) => Promise<boolean>
  onRemove: (material: EstimateMaterialLine) => void
}

/**
 * One material line of an estimate work line: its price is edited in place, and in a Draft estimate the
 * material can be replaced by a same-unit catalog material or removed. Amounts come from the server.
 */
export default function EstimateMaterialLineRow({
  material,
  catalog,
  readOnly,
  onPriceChange,
  onReplace,
  onRemove,
}: EstimateMaterialLineRowProps) {
  const [replaceError, setReplaceError] = useState<string | null>(null)
  // The line keeps no link to the catalog (BR-010), so its current material is recognised by name and unit.
  const replacements = catalog.filter((m) => m.unit === material.materialUnit && m.name !== material.materialName)

  async function handleReplace(event: ChangeEvent<HTMLSelectElement>) {
    const accepted = await onReplace(Number(event.target.value))
    setReplaceError(accepted ? null : 'Pasirinkite to paties mato vieneto medžiagą iš katalogo.')
  }

  return (
    <tr>
      <td className={`${ui.cell} pl-8`}>{material.materialName}</td>
      <td className={ui.cell}>{material.materialUnit}</td>
      <td className={ui.cell}>{formatQuantity(material.quantity)}</td>
      <td className={ui.cell}>
        <InlineNumberField
          key={material.price}
          kind="money"
          value={material.price}
          label={`Kaina: ${material.materialName}`}
          readOnly={readOnly}
          onSave={onPriceChange}
        />
      </td>
      <td className={`${ui.cell} text-right`}>{formatMoney(material.cost)}</td>
      <td className={ui.cell}>
        {!readOnly && (
          <div className="flex flex-wrap items-start gap-2">
            {replacements.length > 0 && (
              <div className={ui.field}>
                <select
                  className={ui.input}
                  aria-label={`Pakeisti medžiagą: ${material.materialName}`}
                  value=""
                  onChange={(event) => void handleReplace(event)}
                >
                  <option value="" disabled>
                    Pakeisti medžiagą…
                  </option>
                  {replacements.map((m) => (
                    <option key={m.id} value={m.id}>
                      {m.name} ({formatMoney(m.price)})
                    </option>
                  ))}
                </select>
                {replaceError && <span className={`${ui.fieldError} whitespace-normal`}>{replaceError}</span>}
              </div>
            )}
            <button
              type="button"
              className={ui.dangerOutlineButton}
              aria-label={`Pašalinti ${material.materialName}`}
              onClick={() => onRemove(material)}
            >
              Pašalinti
            </button>
          </div>
        )}
      </td>
    </tr>
  )
}
