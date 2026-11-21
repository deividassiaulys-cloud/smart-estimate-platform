import DialogFrame from '../../components/DialogFrame'
import SearchableList from '../../components/SearchableList'
import type { Material } from '../../interfaces/material'
import { formatPrice } from '../../utils/format'
import { ui } from '../../utils/ui'

interface ReplaceMaterialDialogProps {
  /** The material line's current material, named in the title. */
  materialName: string
  /** The catalog materials it may be replaced with: the same unit, without the current one. */
  options: Material[]
  onChoose: (materialId: number) => void
  onCancel: () => void
}

/**
 * Chooses a replacement material for an estimate material line (FR-023, spec 020 US2): search by part of the name,
 * then a click or Enter chooses at once; Escape or "Atšaukti" closes without a change.
 */
export default function ReplaceMaterialDialog({ materialName, options, onChoose, onCancel }: ReplaceMaterialDialogProps) {
  return (
    <DialogFrame title={`Pakeisti medžiagą: ${materialName}`} onClose={onCancel}>
      <SearchableList
        items={options.map((option) => ({ id: option.id, name: option.name, detail: formatPrice(option.price) }))}
        noMatch="Medžiagų nerasta."
        onPick={onChoose}
      />
      <div className="flex justify-end">
        <button type="button" className={ui.secondaryButton} onClick={onCancel}>
          Atšaukti
        </button>
      </div>
    </DialogFrame>
  )
}
