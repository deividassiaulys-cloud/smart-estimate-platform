import type { Estimate } from '../../interfaces/estimate'
import { formatMoney } from '../../utils/format'

/** The estimate's totals as calculated by the server (BR-015–BR-017). */
export default function EstimateTotals({ estimate }: { estimate: Estimate }) {
  const rows = [
    ['Tiesioginės išlaidos', estimate.directCosts],
    [`Pridėtinės išlaidos (${estimate.overheadPercent} %)`, estimate.overhead],
    [`Pelnas (${estimate.profitPercent} %)`, estimate.profit],
    [`PVM (${estimate.vatPercent} %)`, estimate.vat],
  ] as const

  return (
    <dl className="ml-auto grid max-w-sm grid-cols-[1fr_auto] gap-x-6 gap-y-1">
      {rows.map(([label, amount]) => (
        <div key={label} className="contents">
          <dt>{label}</dt>
          <dd className="text-right">{formatMoney(amount)}</dd>
        </div>
      ))}
      <dt className="border-t border-line pt-2 font-semibold text-heading">Iš viso su PVM</dt>
      <dd className="border-t border-line pt-2 text-right font-semibold text-heading">{formatMoney(estimate.total)}</dd>
    </dl>
  )
}
