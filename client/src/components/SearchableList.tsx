import { useEffect, useMemo, useRef, useState, type KeyboardEvent } from 'react'
import { ui } from '../utils/ui'

/** One choice in a SearchableList: its name, and an optional detail shown on the right (e.g. price or unit). */
export interface SearchableItem {
  id: number
  name: string
  detail?: string
}

interface SearchableListProps {
  items: SearchableItem[]
  /** Shown when nothing matches the search, e.g. "Darbų nerasta.". */
  noMatch: string
  /** The item currently chosen, marked as pressed. */
  chosenId?: number | null
  onPick: (id: number) => void
  /** Tells the dialog what is typed, e.g. to prefill a new item's name. */
  onSearchChange?: (text: string) => void
}

/**
 * A search field (focused when shown) above a list of choices filtered by part of the name, ignoring letter case,
 * and sorted by name (spec 020). The arrow keys move between the field and the choices; Enter or a click picks.
 */
export default function SearchableList({ items, noMatch, chosenId = null, onPick, onSearchChange }: SearchableListProps) {
  const [search, setSearch] = useState('')
  const wrapperRef = useRef<HTMLDivElement>(null)
  const searchRef = useRef<HTMLInputElement>(null)

  useEffect(() => searchRef.current?.focus(), [])

  const shown = useMemo(() => {
    const needle = search.trim().toLocaleLowerCase('lt-LT')
    return items
      .filter((item) => item.name.toLocaleLowerCase('lt-LT').includes(needle))
      .sort((a, b) => a.name.localeCompare(b.name, 'lt'))
  }, [items, search])

  function handleKeyDown(event: KeyboardEvent<HTMLDivElement>) {
    if (event.key !== 'ArrowDown' && event.key !== 'ArrowUp') return
    event.preventDefault()
    const stops = Array.from(wrapperRef.current?.querySelectorAll<HTMLElement>('input, button') ?? [])
    const current = stops.indexOf(document.activeElement as HTMLElement)
    const step = event.key === 'ArrowDown' ? 1 : -1
    stops[(current + step + stops.length) % stops.length]?.focus()
  }

  return (
    <div ref={wrapperRef} className="flex min-h-0 flex-col gap-3" onKeyDown={handleKeyDown}>
      <label className={ui.field}>
        <span className={ui.label}>Paieška</span>
        <input
          ref={searchRef}
          className={ui.input}
          type="search"
          autoComplete="off"
          value={search}
          onChange={(event) => {
            setSearch(event.target.value)
            onSearchChange?.(event.target.value)
          }}
        />
      </label>
      {shown.length === 0 ? (
        <p>{noMatch}</p>
      ) : (
        <ul className="max-h-[40vh] min-h-0 overflow-y-auto rounded-md border border-line" aria-label="Pasirinkimai">
          {shown.map((item) => (
            <li key={item.id}>
              <button
                type="button"
                aria-pressed={item.id === chosenId}
                className="flex w-full cursor-pointer items-start justify-between gap-3 px-3 py-2 text-left hover:bg-accent-soft focus:bg-accent-soft aria-pressed:bg-accent-soft aria-pressed:font-medium"
                onClick={() => onPick(item.id)}
              >
                <span className="min-w-0 break-words">{item.name}</span>
                {item.detail && <span className="whitespace-nowrap">{item.detail}</span>}
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
