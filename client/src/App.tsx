import { useState } from 'react'
import RegisterPage from './pages/Register/RegisterPage'

// Routing and the Dashboard arrive with feature 002 (T016); until then a successful
// registration shows a placeholder.
function App() {
  const [registered, setRegistered] = useState(false)

  if (registered) {
    return (
      <main>
        <h1>Sveiki!</h1>
        <p>Jūsų įmonė užregistruota, esate prisijungę kaip vadovas.</p>
      </main>
    )
  }

  return <RegisterPage onRegistered={() => setRegistered(true)} />
}

export default App
