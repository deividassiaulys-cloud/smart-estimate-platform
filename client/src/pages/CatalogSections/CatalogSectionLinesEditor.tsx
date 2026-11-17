import type { Work } from '../../interfaces/work'
import { ui } from '../../utils/ui'

/** One work line of a catalog section as typed in the form. */
export interface SectionLineForm {
  workId: string
  coefficient: string
}

export type SectionLineErrors = Partial<Record<keyof SectionLineForm, string>>

const emptyLine: SectionLineForm = { workId: '', coefficient: '1' }

interface CatalogSectionLinesEditorProps {
  lines: SectionLineForm[]
  works: Work[]
  unit: string
  errors: SectionLineErrors[]
  onChange: (lines: SectionLineForm[]) => void
}

/**
 * The section's works: one row per work with its coefficient (work quantity per 1 unit of the section). The same
 * work may be chosen on several rows. Removing a row only changes the unsaved form, so it needs no confirmation.
 */
export default function CatalogSectionLinesEditor({ lines, works, unit, errors, onChange }: CatalogSectionLinesEditorProps) {
  function update(index: number, field: keyof SectionLineForm, value: string) {
    onChange(lines.map((line, i) => (i === index ? { ...line, [field]: value } : line)))
  }

  function fieldError(index: number, field: keyof SectionLineForm) {
    const message = errors[index]?.[field]
    return (
      message && (
        <span className={`${ui.fieldError} whitespace-normal`} id={`section-line-${index}-${field}-error`}>
          {message}
        </span>
      )
    )
  }

  function fieldProps(index: number, field: keyof SectionLineForm, label: string) {
    const invalid = Boolean(errors[index]?.[field])
    return {
      'aria-label': `${label} (${index + 1} eilutė)`,
      'aria-invalid': invalid ? true : undefined,
      'aria-describedby': invalid ? `section-line-${index}-${field}-error` : undefined,
      value: lines[index][field],
    }
  }

  const perUnit = unit ? `darbo kiekis 1 ${unit}` : 'darbo kiekis skyriaus vienetui'

  return (
    <fieldset className="flex min-w-0 flex-col gap-2">
      <legend className={ui.label}>Darbai</legend>
      {lines.length === 0 && <p>Pridėkite bent vieną darbą.</p>}
      {lines.length > 0 && (
        <table className={ui.listTable}>
          <thead className={ui.listHead}>
            <tr>
              <th className={`${ui.cell} w-full`}>Darbas</th>
              <th className={`${ui.cell} whitespace-normal`}>Koeficientas ({perUnit})</th>
              <th className={ui.cell}></th>
            </tr>
          </thead>
          <tbody className={ui.listBody}>
            {lines.map((_, index) => (
              <tr key={index} className={`${ui.listRow} align-top`}>
                <td className={ui.listTitleCell}>
                  <div className={ui.field}>
                    <select
                      className={`${ui.input} w-full min-w-0`}
                      {...fieldProps(index, 'workId', 'Darbas')}
                      onChange={(event) => update(index, 'workId', event.target.value)}
                    >
                      <option value="">Pasirinkite…</option>
                      {works.map((work) => (
                        <option key={work.id} value={work.id}>
                          {work.name} ({work.unit})
                        </option>
                      ))}
                    </select>
                    {fieldError(index, 'workId')}
                  </div>
                </td>
                <td className={ui.listCell} data-label="Koeficientas">
                  <div className={ui.field}>
                    <input
                      className={`${ui.input} w-28`}
                      type="text"
                      inputMode="decimal"
                      autoComplete="off"
                      {...fieldProps(index, 'coefficient', 'Koeficientas')}
                      onChange={(event) => update(index, 'coefficient', event.target.value)}
                    />
                    {fieldError(index, 'coefficient')}
                  </div>
                </td>
                <td className={ui.listActionsCell}>
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
      )}
      <div>
        <button type="button" className={ui.secondaryButton} onClick={() => onChange([...lines, emptyLine])}>
          + Pridėti darbą
        </button>
      </div>
    </fieldset>
  )
}
