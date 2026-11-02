import { useEffect, useId, useRef } from 'react'
import { ui } from '../utils/ui'

interface ConfirmDialogProps {
  open: boolean
  title: string
  message: string
  confirmLabel?: string
  cancelLabel?: string
  /** Disables the buttons while the confirmed action runs. */
  busy?: boolean
  onConfirm: () => void
  onCancel: () => void
}

/**
 * Asks the user to confirm a destructive action. Every delete/remove action uses it (NFR-011).
 * Focus starts on Cancel, Escape cancels, and Tab stays inside the dialog.
 */
export default function ConfirmDialog({
  open,
  title,
  message,
  confirmLabel = 'Patvirtinti',
  cancelLabel = 'Atšaukti',
  busy = false,
  onConfirm,
  onCancel,
}: ConfirmDialogProps) {
  const titleId = useId()
  const messageId = useId()
  const cancelRef = useRef<HTMLButtonElement>(null)
  const confirmRef = useRef<HTMLButtonElement>(null)

  useEffect(() => {
    if (open) cancelRef.current?.focus()
  }, [open])

  if (!open) return null

  function handleKeyDown(event: React.KeyboardEvent<HTMLDivElement>) {
    if (event.key === 'Escape' && !busy) {
      event.preventDefault()
      onCancel()
    } else if (event.key === 'Tab') {
      // Keep focus on the two buttons.
      event.preventDefault()
      const next = document.activeElement === cancelRef.current ? confirmRef.current : cancelRef.current
      next?.focus()
    }
  }

  return (
    <div className="fixed inset-0 z-10 flex items-center justify-center bg-black/45 p-4">
      <div
        className="w-full max-w-[468px] rounded-lg bg-page p-6 text-left text-body shadow-dialog"
        role="alertdialog"
        aria-modal="true"
        aria-labelledby={titleId}
        aria-describedby={messageId}
        onKeyDown={handleKeyDown}
      >
        <h2 id={titleId} className={ui.h2}>
          {title}
        </h2>
        <p id={messageId}>{message}</p>
        <div className="flex justify-end gap-3">
          <button type="button" ref={cancelRef} className={ui.secondaryButton} onClick={onCancel} disabled={busy}>
            {cancelLabel}
          </button>
          <button type="button" ref={confirmRef} className={ui.dangerButton} onClick={onConfirm} disabled={busy}>
            {confirmLabel}
          </button>
        </div>
      </div>
    </div>
  )
}
