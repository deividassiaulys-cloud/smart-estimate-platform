import { useState, type KeyboardEvent } from 'react'
import { toInputNumber } from '../../utils/format'
import { ui } from '../../utils/ui'
import { decimalPattern, parseDecimal } from '../../utils/validation'

/** The two kinds of numbers edited in an estimate line, with the server's rules mirrored for quick feedback. */
const kinds = {
  quantity: {
    pattern: decimalPattern(3),
    isValid: (value: number) => value > 0,
    text: (value: number) => toInputNumber(value),
    error: 'Kiekis turi būti didesnis už 0, ne daugiau kaip 3 skaitmenys po kablelio.',
  },
  money: {
    pattern: decimalPattern(2),
    isValid: (value: number) => value >= 0,
    text: (value: number) => toInputNumber(value, 2),
    error: 'Kaina turi būti 0 arba didesnė, ne daugiau kaip 2 skaitmenys po kablelio.',
  },
}

interface InlineNumberFieldProps {
  kind: keyof typeof kinds
  value: number
  label: string
  readOnly: boolean
  /** Saves the new value; resolves to false when the server rejects it as invalid. */
  onSave: (value: number) => Promise<boolean>
}

/**
 * A number edited in place in an estimate line: saved when the field loses focus or on Enter, unless it is
 * unchanged. An invalid value shows a message under the field and is not saved.
 */
export default function InlineNumberField({ kind, value, label, readOnly, onSave }: InlineNumberFieldProps) {
  const rules = kinds[kind]
  const [text, setText] = useState(rules.text(value))
  const [error, setError] = useState<string | null>(null)

  async function save() {
    const typed = parseDecimal(text)
    if (typed === value) return
    if (!rules.pattern.test(text.trim()) || !rules.isValid(typed)) {
      setError(rules.error)
      return
    }
    setError(null)
    if (!(await onSave(typed))) setError(rules.error)
  }

  function handleKeyDown(event: KeyboardEvent<HTMLInputElement>) {
    if (event.key === 'Enter') {
      event.preventDefault()
      void save()
    }
  }

  return (
    <div className={ui.field}>
      <input
        className={`${ui.input} w-28`}
        type="text"
        inputMode="decimal"
        autoComplete="off"
        aria-label={label}
        aria-invalid={error ? true : undefined}
        disabled={readOnly}
        value={text}
        onChange={(event) => setText(event.target.value)}
        onBlur={() => void save()}
        onKeyDown={handleKeyDown}
      />
      {error && <span className={`${ui.fieldError} whitespace-normal`}>{error}</span>}
    </div>
  )
}
