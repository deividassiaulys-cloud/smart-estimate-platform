import { useState } from 'react'
import { exportEstimate } from '../../api/estimates'
import ConfirmDialog from '../../components/ConfirmDialog'
import { ui } from '../../utils/ui'

interface EstimateExportButtonProps {
  estimateId: number
  estimateName: string
}

/**
 * Exports the estimate to Excel after a confirmation (wireframe wf15). The file is built by the server from the
 * same data the editor shows; the browser only saves it.
 */
export default function EstimateExportButton({ estimateId, estimateName }: EstimateExportButtonProps) {
  const [asking, setAsking] = useState(false)
  const [exporting, setExporting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function handleConfirm() {
    setExporting(true)
    setError(null)
    try {
      const { blob, fileName } = await exportEstimate(estimateId)
      const url = URL.createObjectURL(blob)
      const link = document.createElement('a')
      link.href = url
      link.download = fileName
      link.click()
      URL.revokeObjectURL(url)
    } catch {
      setError('Eksportuoti nepavyko. Bandykite dar kartą.')
    } finally {
      setExporting(false)
      setAsking(false)
    }
  }

  return (
    <div className="flex flex-col items-end gap-1">
      <button type="button" className={ui.secondaryButton} disabled={exporting} onClick={() => setAsking(true)}>
        {exporting ? 'Eksportuojama…' : 'Eksportuoti į Excel'}
      </button>
      {error && (
        <p className={ui.fieldError} role="alert">
          {error}
        </p>
      )}
      <ConfirmDialog
        open={asking}
        title="Eksportuoti sąmatą?"
        message={`Eksportuoti „${estimateName}“ į Excel? Faile bus tie patys duomenys, kurie matomi ekrane.`}
        confirmLabel="Eksportuoti"
        confirmTone="primary"
        busy={exporting}
        onConfirm={handleConfirm}
        onCancel={() => setAsking(false)}
      />
    </div>
  )
}
