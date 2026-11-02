import { ui } from '../../utils/ui'

interface DashboardHeaderProps {
  companyName?: string
  onLogout: () => void
}

/** Top bar of the Dashboard: the company name and the logout button. */
export default function DashboardHeader({ companyName, onLogout }: DashboardHeaderProps) {
  return (
    <header className="flex items-center justify-between gap-4 border-b border-line py-3">
      <span className="font-semibold text-heading">{companyName}</span>
      <button type="button" className={ui.secondaryButton} onClick={onLogout}>
        Atsijungti
      </button>
    </header>
  )
}
