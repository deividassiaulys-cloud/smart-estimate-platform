import { useEffect, useId, useRef, type ReactNode } from 'react'
import { ui } from '../utils/ui'

interface ConfirmDialogProps {
  open: boolean
  title: string
  message: string
  confirmLabel?: string
  cancelLabel?: string
  /** Disables the buttons while the confirmed action runs. */
  busy?: boolean
  /** "danger" (red) for deletions; "primary" for confirmations that destroy nothing, such as export. */
  confirmTone?: 'danger' | 'primary'
  /** Details under the message, such as a list of changes; they scroll inside the dialog. */
  children?: ReactNode
  onConfirm: () => void
  onCancel: () => void
}

/**
 * Asks the user to confirm an action. Every delete/remove action uses it (NFR-011), and so do the status
 * changes and the export.
 * Focus starts on Cancel, Escape cancels, and Tab stays inside the dialog (on the details too, when given, so a
 * keyboard user can scroll them).
 */
export default function ConfirmDialog({
  open,
  title,
  message,
  confirmLabel = 'Patvirtinti',
  cancelLabel = 'Atšaukti',
  busy = false,
  confirmTone = 'danger',
  children,
  onConfirm,
  onCancel,
}: ConfirmDialogProps) {
  const titleId = useId()
  const messageId = useId()
  const detailsId = useId()
  const cancelRef = useRef<HTMLButtonElement>(null)
  const detailsRef = useRef<HTMLDivElement>(null)
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
      // Keep focus inside: Cancel, the details (when given), then the confirm button.
      event.preventDefault()
      const stops = [cancelRef.current, detailsRef.current, confirmRef.current].filter((stop) => stop !== null)
      const current = stops.indexOf(document.activeElement as HTMLButtonElement | HTMLDivElement)
      const step = event.shiftKey ? -1 : 1
      stops[(current + step + stops.length) % stops.length]?.focus()
    }
  }

  return (
    <div className="fixed inset-0 z-10 flex items-center justify-center bg-black/45 p-4">
      <div
        className="w-full max-w-[468px] rounded-lg bg-page p-6 text-left text-body shadow-dialog"
        role="alertdialog"
        aria-modal="true"
        aria-labelledby={titleId}
        aria-describedby={children ? `${messageId} ${detailsId}` : messageId}
        onKeyDown={handleKeyDown}
      >
        <h2 id={titleId} className={ui.h2}>
          {title}
        </h2>
        <p id={messageId}>{message}</p>
        {children && (
          <div id={detailsId} ref={detailsRef} tabIndex={0} className="max-h-[50vh] overflow-y-auto">
            {children}
          </div>
        )}
        <div className="flex justify-end gap-3">
          <button type="button" ref={cancelRef} className={ui.secondaryButton} onClick={onCancel} disabled={busy}>
            {cancelLabel}
          </button>
          <button type="button" ref={confirmRef} className={confirmTone === 'danger' ? ui.dangerButton : ui.primaryButton} onClick={onConfirm} disabled={busy}>
            {confirmLabel}
          </button>
        </div>
      </div>
    </div>
  )
}
