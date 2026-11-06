import type { EstimateMaterialLine, EstimateWorkLine } from '../../interfaces/estimate'
import type { Material } from '../../interfaces/material'
import { formatMoney, formatQuantity } from '../../utils/format'
import { ui } from '../../utils/ui'
import EstimateMaterialLineRow from './EstimateMaterialLineRow'
import InlineNumberField from './InlineNumberField'

interface EstimateWorkLineRowProps {
  line: EstimateWorkLine
  /** The company's catalog materials, offered as replacements for the line's materials. */
  catalog: Material[]
  /** A Confirmed estimate: nothing in the line can be changed or removed. */
  readOnly: boolean
  onQuantityChange: (lineId: number, quantity: number) => Promise<boolean>
  onLabourRateChange: (lineId: number, labourRate: number) => Promise<boolean>
  onMaterialPriceChange: (lineId: number, materialLineId: number, price: number) => Promise<boolean>
  onMaterialReplace: (lineId: number, materialLineId: number, materialId: number) => Promise<boolean>
  onRemove: (line: EstimateWorkLine) => void
  onMaterialRemove: (line: EstimateWorkLine, material: EstimateMaterialLine) => void
}

/**
 * One work line and its material lines. The quantity, labour rate and material prices are edited in place;
 * every amount shown comes from the server.
 */
export default function EstimateWorkLineRow({
  line,
  catalog,
  readOnly,
  onQuantityChange,
  onLabourRateChange,
  onMaterialPriceChange,
  onMaterialReplace,
  onRemove,
  onMaterialRemove,
}: EstimateWorkLineRowProps) {
  return (
    <tbody className="border-b-2 border-line">
      <tr className="align-top">
        <td className={`${ui.cell} font-medium text-heading`}>{line.workName}</td>
        <td className={ui.cell}>{line.workUnit}</td>
        <td className={ui.cell}>
          <InlineNumberField
            kind="quantity"
            value={line.quantity}
            label={`Kiekis: ${line.workName}`}
            readOnly={readOnly}
            onSave={(quantity) => onQuantityChange(line.id, quantity)}
          />
        </td>
        <td className={ui.cell}>
          <div className="flex items-start gap-2 whitespace-nowrap">
            <span className="py-2">Darbas: {formatQuantity(line.labourNorm)} val. ×</span>
            <InlineNumberField
              key={line.labourRate}
              kind="money"
              value={line.labourRate}
              label={`Darbo įkainis, €/val.: ${line.workName}`}
              readOnly={readOnly}
              onSave={(labourRate) => onLabourRateChange(line.id, labourRate)}
            />
            <span className="py-2">€/val.</span>
          </div>
        </td>
        <td className={`${ui.cell} text-right`}>{formatMoney(line.labourCost)}</td>
        <td className={ui.cell}>
          {!readOnly && (
            <button
              type="button"
              className={ui.dangerOutlineButton}
              aria-label={`Pašalinti ${line.workName}`}
              onClick={() => onRemove(line)}
            >
              Pašalinti
            </button>
          )}
        </td>
      </tr>
      {line.materialLines.map((material) => (
        <EstimateMaterialLineRow
          key={material.id}
          material={material}
          catalog={catalog}
          readOnly={readOnly}
          onPriceChange={(price) => onMaterialPriceChange(line.id, material.id, price)}
          onReplace={(materialId) => onMaterialReplace(line.id, material.id, materialId)}
          onRemove={(removed) => onMaterialRemove(line, removed)}
        />
      ))}
    </tbody>
  )
}
