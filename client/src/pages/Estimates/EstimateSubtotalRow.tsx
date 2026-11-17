import { formatMoney } from '../../utils/format'
import { ui } from '../../utils/ui'

/** A section's subtotal row (FR-034), as calculated by the server. */
export default function EstimateSubtotalRow({ name, subtotal }: { name: string; subtotal: number }) {
  return (
    <tbody className="border-b-2 border-line">
      <tr>
        <td colSpan={4} className={`${ui.cell} text-right font-semibold text-heading`}>
          Skyriaus suma: {name}
        </td>
        <td className={`${ui.cell} text-right font-semibold text-heading`}>{formatMoney(subtotal)}</td>
        <td className={ui.cell}></td>
      </tr>
    </tbody>
  )
}
