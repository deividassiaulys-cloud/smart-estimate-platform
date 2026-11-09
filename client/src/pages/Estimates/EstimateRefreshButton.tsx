import { useState } from 'react'
import { previewRefresh } from '../../api/estimates'
import { ApiError } from '../../api/http'
import ConfirmDialog from '../../components/ConfirmDialog'
import { RefreshedField, type EstimateRefreshPreview } from '../../interfaces/estimate'
import { unexpectedError } from '../../utils/errors'
import { formatMoney, formatPercent, formatQuantity } from '../../utils/format'
import { ui } from '../../utils/ui'

const fields: Record<RefreshedField, { label: string; format: (value: number) => string }> = {
  [RefreshedField.LabourRate]: { label: 'Darbo įkainis', format: formatMoney },
  [RefreshedField.LabourNorm]: { label: 'Darbo norma', format: formatQuantity },
  [RefreshedField.Price]: { label: 'Kaina', format: formatMoney },
  [RefreshedField.ConsumptionNorm]: { label: 'Sunaudojimo norma', format: formatQuantity },
  [RefreshedField.WastePercent]: { label: 'Nuostoliai', format: formatPercent },
}

const lineName = (workName: string, materialName: string | null) =>
  materialName ? `${workName} / ${materialName}` : workName

interface EstimateRefreshButtonProps {
  estimateId: number
  /** Runs the update; the editor shows the recalculated estimate or its own message. */
  onRefresh: () => Promise<void>
  /** The estimate was confirmed elsewhere (409): the editor reloads it and says so. */
  onConflict: (error: unknown) => Promise<void>
}

/**
 * Updates a Draft estimate's prices and norms from the catalog (FR-024): first shows every change, the lines not
 * found and the total before and after, then applies all of them after the Manager confirms.
 */
export default function EstimateRefreshButton({ estimateId, onRefresh, onConflict }: EstimateRefreshButtonProps) {
  const [preview, setPreview] = useState<EstimateRefreshPreview | null>(null)
  const [loading, setLoading] = useState(false)
  const [refreshing, setRefreshing] = useState(false)
  const [message, setMessage] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  async function handleClick() {
    setLoading(true)
    setMessage(null)
    setError(null)
    try {
      const found = await previewRefresh(estimateId)
      if (found.changes.length === 0) setMessage('Sąmatos kainos ir normos jau sutampa su katalogu.')
      else setPreview(found)
    } catch (err) {
      if (err instanceof ApiError && err.status === 409) await onConflict(err)
      else setError(unexpectedError('įkelti kainų atnaujinimo'))
    } finally {
      setLoading(false)
    }
  }

  async function handleConfirm() {
    setRefreshing(true)
    await onRefresh()
    setRefreshing(false)
    setPreview(null)
  }

  return (
    <div className="flex flex-col items-end gap-1">
      <button type="button" className={ui.secondaryButton} disabled={loading} onClick={() => void handleClick()}>
        Atnaujinti kainas iš katalogo
      </button>
      {message && <p role="status">{message}</p>}
      {error && (
        <p className={ui.fieldError} role="alert">
          {error}
        </p>
      )}
      <ConfirmDialog
        open={preview !== null}
        title="Atnaujinti kainas iš katalogo?"
        message="Šios sąmatos eilutės gaus dabartines katalogo kainas ir normas. Kiekiai ir procentai nesikeis."
        confirmLabel="Atnaujinti"
        confirmTone="primary"
        busy={refreshing}
        onConfirm={() => void handleConfirm()}
        onCancel={() => setPreview(null)}
      >
        {preview && (
          <>
            <ul className="list-disc pl-5">
              {preview.changes.map((change, index) => {
                const field = fields[change.field]
                return (
                  <li key={index}>
                    {lineName(change.workName, change.materialName)} — {field.label}: {field.format(change.oldValue)} →{' '}
                    {field.format(change.newValue)}
                  </li>
                )
              })}
            </ul>
            {preview.notFound.length > 0 && (
              <>
                <p className="mt-2">Kataloge nerasta, liks nepakeista:</p>
                <ul className="list-disc pl-5">
                  {preview.notFound.map((miss, index) => (
                    <li key={index}>{lineName(miss.workName, miss.materialName)}</li>
                  ))}
                </ul>
              </>
            )}
            <p className="mt-2 font-medium">
              Iš viso su PVM: {formatMoney(preview.currentTotal)} → {formatMoney(preview.newTotal)}
            </p>
          </>
        )}
      </ConfirmDialog>
    </div>
  )
}
