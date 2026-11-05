import { useEffect, useRef } from 'react'
import { NavLink } from 'react-router-dom'
import { ui } from '../utils/ui'

export interface NavItem {
  label: string
  to: string
}

interface MobileMenuProps {
  id: string
  items: NavItem[]
  onClose: () => void
  onLogout: () => void
}

/**
 * The navigation on screens narrower than 768 px: a full-height panel with the menu items stacked (docs/ux.md §8).
 * Escape, an item or "Uždaryti" closes it.
 */
export default function MobileMenu({ id, items, onClose, onLogout }: MobileMenuProps) {
  const firstLinkRef = useRef<HTMLAnchorElement>(null)

  useEffect(() => {
    firstLinkRef.current?.focus()
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') onClose()
    }
    document.addEventListener('keydown', handleKeyDown)
    return () => document.removeEventListener('keydown', handleKeyDown)
  }, [onClose])

  return (
    <div id={id} className="fixed inset-0 z-20 flex flex-col gap-2 bg-page p-4 text-left md:hidden" role="dialog" aria-label="Meniu">
      <button type="button" className={`${ui.secondaryButton} self-end`} onClick={onClose}>
        Uždaryti
      </button>
      <nav>
        <ul className="flex flex-col">
          {items.map((item, index) => (
            <li key={item.to}>
              <NavLink
                ref={index === 0 ? firstLinkRef : undefined}
                to={item.to}
                onClick={onClose}
                className={({ isActive }) =>
                  `block min-h-11 border-b border-line py-3 text-lg ${isActive ? 'font-semibold text-accent' : 'text-heading'}`
                }
              >
                {item.label}
              </NavLink>
            </li>
          ))}
        </ul>
      </nav>
      <button type="button" className={`${ui.secondaryButton} mt-4`} onClick={onLogout}>
        Atsijungti
      </button>
    </div>
  )
}
