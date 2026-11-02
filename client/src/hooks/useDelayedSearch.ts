import { useEffect, useState } from 'react'

/** How long to wait after the last keystroke before searching. */
const searchDelayMs = 300

/**
 * Loads a catalog list on the server and searches it as the user types: at once on the first load,
 * then after a short pause in typing. A late answer to an older search is ignored.
 */
export function useDelayedSearch<T>(load: (search: string) => Promise<T[]>, loadError: string) {
  const [search, setSearch] = useState('')
  const [items, setItems] = useState<T[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    const timer = setTimeout(() => {
      load(search)
        .then((found) => {
          if (cancelled) return
          setItems(found)
          setError(null)
        })
        .catch(() => {
          if (!cancelled) setError(loadError)
        })
    }, search ? searchDelayMs : 0)
    return () => {
      cancelled = true
      clearTimeout(timer)
    }
  }, [search, load, loadError])

  return { search, setSearch, items, setItems, error, setError }
}
