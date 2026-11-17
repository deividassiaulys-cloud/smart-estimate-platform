import type { EstimateMaterialLine, EstimateWorkLine } from '../../interfaces/estimate'
import type { Material } from '../../interfaces/material'
import { formatMoney } from '../../utils/format'
import { ui } from '../../utils/ui'
import EstimateLabourRow from './EstimateLabourRow'
import EstimateMaterialLineRow from './EstimateMaterialLineRow'
import InlineNumberField from './InlineNumberField'

interface EstimateWorkLineRowProps {
  line: EstimateWorkLine
  /** The line's number in the estimate, 1, 2, … in the order of the lines. */
  number: number
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
 * One work line with its unit price, amount and Labour / Materials / Machinery (FR-031, FR-032), followed by its labour
 * row and its material lines. The quantity, labour rate and material prices are edited in place; every amount shown
 * comes from the server.
 */
export default function EstimateWorkLineRow({
  line,
  number,
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
        <td className={`${ui.cell} font-medium text-heading`}>{number}</td>
        <td className={`${ui.cell} font-medium text-heading`}>{line.workName}</td>
        <td className={ui.cell}>{line.workUnit}</td>
        <td className={ui.cell}></td>
        <td className={ui.cell}></td>
        <td className={`${ui.cell} text-right`} aria-label={`Vieneto kaina: ${line.workName}`}>
          {formatMoney(line.unitPrice)}
        </td>
        <td className={ui.cell}>
          <InlineNumberField
            kind="quantity"
            value={line.quantity}
            label={`Kiekis: ${line.workName}`}
            readOnly={readOnly}
            onSave={(quantity) => onQuantityChange(line.id, quantity)}
          />
        </td>
        <td className={`${ui.cell} text-right font-medium text-heading`}>{formatMoney(line.amount)}</td>
        <td className={`${ui.cell} text-right`}>{formatMoney(line.labourCost)}</td>
        <td className={`${ui.cell} text-right`}>{formatMoney(line.materials)}</td>
        <td className={`${ui.cell} text-right`}>{formatMoney(line.machinery)}</td>
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
      <EstimateLabourRow
        line={line}
        readOnly={readOnly}
        onLabourRateChange={(labourRate) => onLabourRateChange(line.id, labourRate)}
      />
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
