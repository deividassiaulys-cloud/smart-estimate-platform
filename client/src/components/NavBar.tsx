import { useCallback, useId, useRef, useState } from 'react'
import { NavLink, useLocation, useNavigate } from 'react-router-dom'
import { isManager, logout } from '../utils/auth'
import { ui } from '../utils/ui'
import MobileMenu, { type NavItem } from './MobileMenu'

const managerItems: NavItem[] = [
  { label: 'Pagrindinis', to: '/dashboard' },
  { label: 'Projektai', to: '/projects' },
  { label: 'Medžiagos', to: '/materials' },
  { label: 'Darbai', to: '/works' },
  { label: 'Komanda', to: '/team' },
]

const viewerItems: NavItem[] = [
  { label: 'Pagrindinis', to: '/dashboard' },
  { label: 'Projektai', to: '/projects' },
]

/**
 * The navigation on every logged-in page: the company name, the menu items the role may use and the one logout
 * (docs/ux.md §7). Below 768 px the items move behind a "Meniu" button. The server still enforces every rule.
 */
export default function NavBar({ companyName }: { companyName?: string }) {
  const navigate = useNavigate()
  const { pathname } = useLocation()
  const menuId = useId()
  const menuButtonRef = useRef<HTMLButtonElement>(null)
  // The page the panel was opened on; navigating anywhere else (including to Login) closes it.
  const [openedOn, setOpenedOn] = useState<string | null>(null)
  const menuOpen = openedOn === pathname
  const items = isManager() ? managerItems : viewerItems

  const closeMenu = useCallback(() => {
    setOpenedOn(null)
    menuButtonRef.current?.focus()
  }, [])

  function handleLogout() {
    setOpenedOn(null)
    logout()
    navigate('/login', { replace: true })
  }

  return (
    <header className="flex items-center justify-between gap-4 border-b border-line px-4 py-3">
      <span className="truncate font-semibold text-heading">{companyName}</span>
      <nav className="max-md:hidden" aria-label="Pagrindinis meniu">
        <ul className="flex items-center gap-4">
          {items.map((item) => (
            <li key={item.to}>
              <NavLink
                to={item.to}
                className={({ isActive }) => (isActive ? 'font-semibold text-accent' : 'text-heading hover:text-accent')}
              >
                {item.label}
              </NavLink>
            </li>
          ))}
          <li>
            <button type="button" className={ui.secondaryButton} onClick={handleLogout}>
              Atsijungti
            </button>
          </li>
        </ul>
      </nav>
      <button
        ref={menuButtonRef}
        type="button"
        className={`${ui.secondaryButton} md:hidden`}
        aria-expanded={menuOpen}
        aria-controls={menuId}
        onClick={() => setOpenedOn(pathname)}
      >
        Meniu
      </button>
      {menuOpen && <MobileMenu id={menuId} items={items} onClose={closeMenu} onLogout={handleLogout} />}
    </header>
  )
}
