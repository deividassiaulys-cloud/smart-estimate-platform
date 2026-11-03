import { useState, type KeyboardEvent } from 'react'
import type { EstimateWorkLine } from '../../interfaces/estimate'
import { formatMoney, formatQuantity } from '../../utils/format'
import { ui } from '../../utils/ui'
import { decimalPattern, parseDecimal } from '../../utils/validation'

const quantityPattern = decimalPattern(3)

interface EstimateWorkLineRowProps {
  line: EstimateWorkLine
  /** A Confirmed estimate: the quantity cannot be changed and the line cannot be removed. */
  readOnly: boolean
  onQuantityChange: (lineId: number, quantity: number) => Promise<void>
  onRemove: (line: EstimateWorkLine) => void
}

/**
 * One work line and its generated material lines. The quantity is saved when the field loses focus or on
 * Enter; every amount shown comes from the server.
 */
export default function EstimateWorkLineRow({ line, readOnly, onQuantityChange, onRemove }: EstimateWorkLineRowProps) {
  const [quantity, setQuantity] = useState(String(line.quantity))
  const [error, setError] = useState<string | null>(null)

  async function save() {
    if (parseDecimal(quantity) === line.quantity) return
    if (!quantityPattern.test(quantity.trim()) || parseDecimal(quantity) <= 0) {
      setError('Kiekis turi būti didesnis už 0, ne daugiau kaip 3 skaitmenys po kablelio.')
      return
    }
    setError(null)
    await onQuantityChange(line.id, parseDecimal(quantity))
  }

  function handleKeyDown(event: KeyboardEvent<HTMLInputElement>) {
    if (event.key === 'Enter') {
      event.preventDefault()
      void save()
    }
  }

  return (
    <tbody className="border-b-2 border-line">
      <tr className="align-top">
        <td className={`${ui.cell} font-medium text-heading`}>{line.workName}</td>
        <td className={ui.cell}>{line.workUnit}</td>
        <td className={ui.cell}>
          <div className={ui.field}>
            <input
              className={`${ui.input} w-28`}
              type="text"
              inputMode="decimal"
              autoComplete="off"
              aria-label={`Kiekis: ${line.workName}`}
              aria-invalid={error ? true : undefined}
              disabled={readOnly}
              value={quantity}
              onChange={(event) => setQuantity(event.target.value)}
              onBlur={() => void save()}
              onKeyDown={handleKeyDown}
            />
            {error && <span className={`${ui.fieldError} whitespace-normal`}>{error}</span>}
          </div>
        </td>
        <td className={ui.cell}>
          Darbas: {formatQuantity(line.labourNorm)} val. × {formatMoney(line.labourRate)}/val.
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
        <tr key={material.id}>
          <td className={`${ui.cell} pl-8`}>{material.materialName}</td>
          <td className={ui.cell}>{material.materialUnit}</td>
          <td className={ui.cell}>{formatQuantity(material.quantity)}</td>
          <td className={ui.cell}>× {formatMoney(material.price)}</td>
          <td className={`${ui.cell} text-right`}>{formatMoney(material.cost)}</td>
          <td className={ui.cell}></td>
        </tr>
      ))}
    </tbody>
  )
}
