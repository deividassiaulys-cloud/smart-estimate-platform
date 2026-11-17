import { useEffect, useId, useRef, type KeyboardEvent, type ReactNode } from 'react'
import { createPortal } from 'react-dom'
import { ui } from '../utils/ui'

interface DialogFrameProps {
  title: string
  onClose: () => void
  /** A wide dialog for a whole form (spec 020 "create new"); the default suits a list. */
  wide?: boolean
  children: ReactNode
}

/**
 * The frame of every pick-and-act dialog (spec 020): rendered at the end of the page, so it inherits nothing
 * from the table or form that opens it; Escape closes it, Tab stays inside, and on closing the focus goes back to
 * where it was before. The content scrolls inside the frame; the page behind does not.
 */
export default function DialogFrame({ title, onClose, wide = false, children }: DialogFrameProps) {
  const titleId = useId()
  const frameRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    const opener = document.activeElement as HTMLElement | null
    return () => opener?.focus()
  }, [])

  function handleKeyDown(event: KeyboardEvent<HTMLDivElement>) {
    if (event.key === 'Escape') {
      event.preventDefault()
      event.stopPropagation()
      onClose()
    } else if (event.key === 'Tab') {
      const stops = Array.from(
        frameRef.current?.querySelectorAll<HTMLElement>('input, select, textarea, button, a[href]') ?? [],
      ).filter((element) => !element.hasAttribute('disabled'))
      if (stops.length === 0) return
      event.preventDefault()
      const current = stops.indexOf(document.activeElement as HTMLElement)
      const step = event.shiftKey ? -1 : 1
      stops[(current + step + stops.length) % stops.length].focus()
    }
  }

  return createPortal(
    <div className="fixed inset-0 z-10 flex items-center justify-center bg-black/45 p-4">
      <div
        ref={frameRef}
        className={`flex max-h-[90vh] w-full flex-col gap-3 overflow-y-auto rounded-lg bg-page p-6 text-left text-body shadow-dialog ${wide ? 'max-w-[960px]' : 'max-w-[560px]'}`}
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        onKeyDown={handleKeyDown}
      >
        <h2 id={titleId} className={`${ui.h2} break-words`}>
          {title}
        </h2>
        {children}
      </div>
    </div>,
    document.body,
  )
}
