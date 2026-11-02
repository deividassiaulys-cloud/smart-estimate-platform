import type { Material } from '../../interfaces/material'
import { ui } from '../../utils/ui'

/** One material line as typed in the form. */
export interface LineForm {
  materialId: string
  consumptionNorm: string
  wastePercent: string
}

export type LineErrors = Partial<Record<keyof LineForm, string>>

const emptyLine: LineForm = { materialId: '', consumptionNorm: '', wastePercent: '0' }

interface WorkLinesEditorProps {
  lines: LineForm[]
  materials: Material[]
  errors: LineErrors[]
  onChange: (lines: LineForm[]) => void
}

/**
 * The work's composition: one row per material line. The same material may be chosen on several rows.
 * Removing a row only changes the unsaved form, so it needs no confirmation.
 */
export default function WorkLinesEditor({ lines, materials, errors, onChange }: WorkLinesEditorProps) {
  function update(index: number, field: keyof LineForm, value: string) {
    onChange(lines.map((line, i) => (i === index ? { ...line, [field]: value } : line)))
  }

  function fieldError(index: number, field: keyof LineForm) {
    const message = errors[index]?.[field]
    return (
      message && (
        // Table cells do not wrap; the message must, so it never widens the table.
        <span className={`${ui.fieldError} whitespace-normal`} id={`line-${index}-${field}-error`}>
          {message}
        </span>
      )
    )
  }

  function fieldProps(index: number, field: keyof LineForm, label: string) {
    const invalid = Boolean(errors[index]?.[field])
    return {
      'aria-label': `${label} (${index + 1} eilutė)`,
      'aria-invalid': invalid ? true : undefined,
      'aria-describedby': invalid ? `line-${index}-${field}-error` : undefined,
      value: lines[index][field],
    }
  }

  return (
    <fieldset className="flex flex-col gap-2">
      <legend className={ui.label}>Medžiagos</legend>
      {lines.length === 0 && <p>Darbas be medžiagų.</p>}
      {lines.length > 0 && (
        <div className={ui.tableWrapper}>
          <table className={ui.table}>
            <thead>
              <tr>
                <th className={ui.cell}>Medžiaga</th>
                <th className={ui.cell}>Sąnaudų norma vienetui</th>
                <th className={ui.cell}>Atliekos, %</th>
                <th className={ui.cell}></th>
              </tr>
            </thead>
            <tbody>
              {lines.map((_, index) => (
                <tr key={index} className="align-top">
                  <td className={ui.cell}>
                    <div className={ui.field}>
                      <select
                        className={ui.input}
                        {...fieldProps(index, 'materialId', 'Medžiaga')}
                        onChange={(event) => update(index, 'materialId', event.target.value)}
                      >
                        <option value="">Pasirinkite…</option>
                        {materials.map((material) => (
                          <option key={material.id} value={material.id}>
                            {material.name} ({material.unit})
                          </option>
                        ))}
                      </select>
                      {fieldError(index, 'materialId')}
                    </div>
                  </td>
                  <td className={ui.cell}>
                    <div className={ui.field}>
                      <input
                        className={ui.input}
                        type="text"
                        inputMode="decimal"
                        autoComplete="off"
                        {...fieldProps(index, 'consumptionNorm', 'Sąnaudų norma')}
                        onChange={(event) => update(index, 'consumptionNorm', event.target.value)}
                      />
                      {fieldError(index, 'consumptionNorm')}
                    </div>
                  </td>
                  <td className={ui.cell}>
                    <div className={ui.field}>
                      <input
                        className={ui.input}
                        type="text"
                        inputMode="decimal"
                        autoComplete="off"
                        {...fieldProps(index, 'wastePercent', 'Atliekos')}
                        onChange={(event) => update(index, 'wastePercent', event.target.value)}
                      />
                      {fieldError(index, 'wastePercent')}
                    </div>
                  </td>
                  <td className={ui.cell}>
                    <button
                      type="button"
                      className={ui.dangerOutlineButton}
                      aria-label={`Pašalinti ${index + 1} eilutę`}
                      onClick={() => onChange(lines.filter((_, i) => i !== index))}
                    >
                      Pašalinti
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
      <div>
        <button type="button" className={ui.secondaryButton} onClick={() => onChange([...lines, emptyLine])}>
          + Pridėti medžiagą
        </button>
      </div>
    </fieldset>
  )
}
