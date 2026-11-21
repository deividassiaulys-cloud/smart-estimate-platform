import type { EstimateMaterialLine, EstimateWorkLine } from '../../interfaces/estimate'
import type { Material } from '../../interfaces/material'
import ClampedText from '../../components/ClampedText'
import { formatMoney, formatNorm } from '../../utils/format'
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
        <td className={`${ui.nameCell} font-medium text-heading`}>
          <ClampedText text={line.workName} />
        </td>
        <td className={ui.cell}>{line.workUnit}</td>
        <td className={ui.cell}>
          <InlineNumberField
            // Re-created when the server changes the quantity (e.g. a section quantity change, FR-032), so it never
            // keeps showing the old value.
            key={line.quantity}
            kind="quantity"
            value={line.quantity}
            label={`Kiekis: ${line.workName}`}
            readOnly={readOnly}
            onSave={(quantity) => onQuantityChange(line.id, quantity)}
          />
        </td>
        <td className={ui.cell}>
          {/* Wraps onto two short lines when the table is narrow (spec 020). */}
          <div className="flex max-w-[15rem] flex-wrap items-start gap-x-2">
            <span className="py-2 whitespace-nowrap">Darbas: {formatNorm(line.labourNorm)} val. ×</span>
            <InlineNumberField
              key={line.labourRate}
              kind="money"
              value={line.labourRate}
              label={`Darbo įkainis, €/val.: ${line.workName}`}
              readOnly={readOnly}
              onSave={(labourRate) => onLabourRateChange(line.id, labourRate)}
            />
            <span className="py-2 whitespace-nowrap">€/val.</span>
          </div>
        </td>
        <td className={`${ui.cell} text-right`}>{formatMoney(line.labourCost)}</td>
        <td className={ui.cell}>
          {!readOnly && (
            <button
              type="button"
              className={ui.smallDangerButton}
              aria-label={`Pašalinti ${line.workName}`}
              onClick={() => onRemove(line)}
            >
              Šalinti
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
