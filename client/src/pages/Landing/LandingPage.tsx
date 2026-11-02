import { Link } from 'react-router-dom'
import { ui } from '../../utils/ui'

export default function LandingPage() {
  return (
    <main className="mx-auto max-w-[672px] px-4 py-16">
      <h1 className={ui.h1}>Smart Estimate Platform</h1>
      <p>Statybos sąmatų rengimas jūsų įmonei: medžiagos, darbai, projektai ir sąmatos vienoje vietoje.</p>
      <div className="mt-6 flex flex-wrap justify-center gap-4">
        <Link className="rounded-md border border-accent bg-accent px-4 py-2.5 text-white" to="/register">
          Užregistruoti įmonę
        </Link>
        <Link className="rounded-md border border-accent-line px-4 py-2.5 text-accent" to="/login">
          Prisijungti
        </Link>
      </div>
    </main>
  )
}
