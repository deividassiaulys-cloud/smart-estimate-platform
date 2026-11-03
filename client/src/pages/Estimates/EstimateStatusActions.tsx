import { useState } from 'react'
import ConfirmDialog from '../../components/ConfirmDialog'
import { EstimateStatus } from '../../interfaces/estimate'
import { estimateStatusNames } from '../../utils/estimateStatus'
import { ui } from '../../utils/ui'

interface EstimateStatusActionsProps {
  status: EstimateStatus
  onConfirm: () => Promise<void>
  onReturnToDraft: () => Promise<void>
}

/**
 * The estimate's status as text and the one status change it allows: "Patvirtinti" for a Draft,
 * "Grąžinti į juodraštį" for a Confirmed estimate. Both ask for confirmation first (NFR-011).
 */
export default function EstimateStatusActions({ status, onConfirm, onReturnToDraft }: EstimateStatusActionsProps) {
  const [asking, setAsking] = useState(false)
  const [busy, setBusy] = useState(false)
  const isDraft = status === EstimateStatus.Draft

  async function handleConfirm() {
    setBusy(true)
    await (isDraft ? onConfirm() : onReturnToDraft())
    setBusy(false)
    setAsking(false)
  }

  return (
    <div className="flex items-center gap-3">
      <span className="rounded-full border border-line px-3 py-1 text-heading">{estimateStatusNames[status]}</span>
      <button type="button" className={isDraft ? ui.primaryButton : ui.secondaryButton} onClick={() => setAsking(true)}>
        {isDraft ? 'Patvirtinti' : 'Grąžinti į juodraštį'}
      </button>
      <ConfirmDialog
        open={asking}
        title={isDraft ? 'Patvirtinti sąmatą?' : 'Grąžinti sąmatą į juodraštį?'}
        message={
          isDraft
            ? 'Patvirtintos sąmatos darbų, kiekių ir procentų keisti nebus galima, kol negrąžinsite jos į juodraštį.'
            : 'Sąmatą vėl bus galima keisti.'
        }
        confirmLabel={isDraft ? 'Patvirtinti' : 'Grąžinti'}
        busy={busy}
        onConfirm={handleConfirm}
        onCancel={() => setAsking(false)}
      />
    </div>
  )
}
