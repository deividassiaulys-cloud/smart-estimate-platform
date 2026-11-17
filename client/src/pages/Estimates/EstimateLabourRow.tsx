import type { EstimateWorkLine } from '../../interfaces/estimate'
import { formatMoney, formatNorm, formatQuantity } from '../../utils/format'
import { ui } from '../../utils/ui'
import InlineNumberField from './InlineNumberField'

interface EstimateLabourRowProps {
  line: EstimateWorkLine
  /** A Confirmed estimate: the labour rate cannot be changed. */
  readOnly: boolean
  onLabourRateChange: (labourRate: number) => Promise<boolean>
}

/**
 * A work line's labour, shown as its own row under the work line (FR-031, spec 017 Q1): labour norm, the labour
 * rate edited in place (FR-023), hours and labour cost, which also fills the Darbas column. Amounts come from the server.
 */
export default function EstimateLabourRow({ line, readOnly, onLabourRateChange }: EstimateLabourRowProps) {
  return (
    <tr>
      <td className={ui.cell}></td>
      <td className={`${ui.cell} pl-8`}>Darbas</td>
      <td className={ui.cell}>val.</td>
      <td className={`${ui.cell} text-right`}>{formatNorm(line.labourNorm)}</td>
      <td className={ui.cell}></td>
      <td className={ui.cell}>
        <InlineNumberField
          key={line.labourRate}
          kind="money"
          value={line.labourRate}
          label={`Darbo įkainis, €/val.: ${line.workName}`}
          readOnly={readOnly}
          onSave={onLabourRateChange}
        />
      </td>
      <td className={`${ui.cell} text-right`}>{formatQuantity(line.hours)}</td>
      <td className={`${ui.cell} text-right`}>{formatMoney(line.labourCost)}</td>
      <td className={`${ui.cell} text-right`}>{formatMoney(line.labourCost)}</td>
      <td className={ui.cell}></td>
      <td className={ui.cell}></td>
      <td className={ui.cell}></td>
    </tr>
  )
}
