import { useEffect, useId, useMemo, useRef, useState, type KeyboardEvent } from 'react'
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
 * Chooses a replacement material for an estimate material line (FR-023, spec 020 US2): a search field at the top
 * (focused on open) filters the list by part of the name, ignoring letter case. Arrow keys move through the list,
 * Enter or a click chooses, Escape or "Atšaukti" closes without a change, and Tab stays inside the dialog.
 */
export default function ReplaceMaterialDialog({ materialName, options, onChoose, onCancel }: ReplaceMaterialDialogProps) {
  const titleId = useId()
  const searchId = useId()
  const [search, setSearch] = useState('')
  const dialogRef = useRef<HTMLDivElement>(null)
  const searchRef = useRef<HTMLInputElement>(null)

  useEffect(() => searchRef.current?.focus(), [])

  const shown = useMemo(() => {
    const needle = search.trim().toLocaleLowerCase('lt-LT')
    return [...options]
      .filter((option) => option.name.toLocaleLowerCase('lt-LT').includes(needle))
      .sort((a, b) => a.name.localeCompare(b.name, 'lt'))
  }, [options, search])

  // The search field, then each option, then Atšaukti: the stops for the arrow keys and for Tab.
  function stops(): HTMLElement[] {
    return Array.from(dialogRef.current?.querySelectorAll<HTMLElement>('input, button') ?? [])
  }

  function move(step: number) {
    const all = stops()
    const current = all.indexOf(document.activeElement as HTMLElement)
    all[(current + step + all.length) % all.length]?.focus()
  }

  function handleKeyDown(event: KeyboardEvent<HTMLDivElement>) {
    if (event.key === 'Escape') {
      event.preventDefault()
      onCancel()
    } else if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
      event.preventDefault()
      move(event.key === 'ArrowDown' ? 1 : -1)
    } else if (event.key === 'Tab') {
      event.preventDefault()
      move(event.shiftKey ? -1 : 1)
    }
  }

  return (
    <div className="fixed inset-0 z-10 flex items-center justify-center bg-black/45 p-4">
      <div
        ref={dialogRef}
        className="flex max-h-[85vh] w-full max-w-[520px] flex-col gap-3 rounded-lg bg-page p-6 text-left text-body shadow-dialog"
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        onKeyDown={handleKeyDown}
      >
        <h2 id={titleId} className={`${ui.h2} break-words`}>
          Pakeisti medžiagą: {materialName}
        </h2>
        <div className={ui.field}>
          <label className={ui.label} htmlFor={searchId}>
            Paieška
          </label>
          <input
            id={searchId}
            ref={searchRef}
            className={ui.input}
            type="search"
            autoComplete="off"
            value={search}
            onChange={(event) => setSearch(event.target.value)}
          />
        </div>
        {shown.length === 0 ? (
          <p>Medžiagų nerasta.</p>
        ) : (
          <ul className="min-h-0 overflow-y-auto" aria-label="Medžiagos">
            {shown.map((option) => (
              <li key={option.id}>
                <button
                  type="button"
                  className="flex w-full cursor-pointer items-start justify-between gap-3 rounded-md px-3 py-2 text-left hover:bg-accent-soft focus:bg-accent-soft"
                  onClick={() => onChoose(option.id)}
                >
                  <span className="break-words">{option.name}</span>
                  <span className="whitespace-nowrap">{formatPrice(option.price)}</span>
                </button>
              </li>
            ))}
          </ul>
        )}
        <div className="flex justify-end">
          <button type="button" className={ui.secondaryButton} onClick={onCancel}>
            Atšaukti
          </button>
        </div>
      </div>
    </div>
  )
}
