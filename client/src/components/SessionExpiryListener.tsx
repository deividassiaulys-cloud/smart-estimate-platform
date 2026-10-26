import { useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { sessionExpiredEvent } from '../utils/auth'

/** Sends the user to Login with a "session expired" message when the API rejects their token. */
export default function SessionExpiryListener() {
  const navigate = useNavigate()

  useEffect(() => {
    const handleExpired = () => navigate('/login', { replace: true, state: { sessionExpired: true } })
    window.addEventListener(sessionExpiredEvent, handleExpired)
    return () => window.removeEventListener(sessionExpiredEvent, handleExpired)
  }, [navigate])

  return null
}
