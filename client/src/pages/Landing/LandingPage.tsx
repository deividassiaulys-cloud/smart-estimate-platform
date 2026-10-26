import { Link } from 'react-router-dom'
import './Landing.css'

export default function LandingPage() {
  return (
    <main className="landing">
      <h1>Smart Estimate Platform</h1>
      <p>Statybos sąmatų rengimas jūsų įmonei: medžiagos, darbai, projektai ir sąmatos vienoje vietoje.</p>
      <div className="landing-actions">
        <Link className="landing-primary" to="/register">
          Užregistruoti įmonę
        </Link>
        <Link to="/login">Prisijungti</Link>
      </div>
    </main>
  )
}
