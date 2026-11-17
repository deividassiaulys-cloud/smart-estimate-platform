import ClampedText from '../../components/ClampedText'
import type { EstimateSection } from '../../interfaces/estimate'
import { ui } from '../../utils/ui'
import InlineNumberField from './InlineNumberField'

interface EstimateSectionHeaderRowProps {
  /** The section, or null for the group of lines without a section ("Be skyriaus"). */
  section: EstimateSection | null
  readOnly: boolean
  /** Asks to change the section quantity; resolves to false when the server rejects the value. */
  onQuantityChange: (section: EstimateSection, quantity: number) => Promise<boolean>
  onRemove: (section: EstimateSection) => void
}

/**
 * The heading row of an estimate section (FR-034): its name, its quantity (edited in place in a Draft estimate,
 * after a confirmation, FR-032) with its unit, and its remove action. Without a section it is the "Be skyriaus" heading.
 */
export default function EstimateSectionHeaderRow({ section, readOnly, onQuantityChange, onRemove }: EstimateSectionHeaderRowProps) {
  return (
    <tbody>
      <tr className="bg-accent-soft align-top">
        <th scope="rowgroup" colSpan={2} className={`${ui.nameCell} font-semibold text-heading`}>
          <ClampedText text={section ? section.name : 'Be skyriaus'} />
        </th>
        <td className={ui.cell}>
          {section && (
            <div className="flex items-start gap-2">
              <InlineNumberField
                kind="quantity"
                value={section.quantity}
                label={`Skyriaus kiekis: ${section.name}`}
                readOnly={readOnly}
                onSave={(quantity) => onQuantityChange(section, quantity)}
              />
              <span className="py-2">{section.unit}</span>
            </div>
          )}
        </td>
        <td className={ui.cell} colSpan={2}></td>
        <td className={ui.cell}>
          {section && !readOnly && (
            <button
              type="button"
              className={ui.smallDangerButton}
              aria-label={`Pašalinti skyrių ${section.name}`}
              onClick={() => onRemove(section)}
            >
              Šalinti
            </button>
          )}
        </td>
      </tr>
    </tbody>
  )
}
