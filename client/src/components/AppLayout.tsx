import { useEffect, useState } from 'react'
import { Outlet } from 'react-router-dom'
import { getCurrentUser } from '../api/auth'
import NavBar from './NavBar'

/** The frame of every logged-in page: the navigation, then the page itself. */
export default function AppLayout() {
  const [companyName, setCompanyName] = useState<string>()

  useEffect(() => {
    // The company name is only shown in the menu; if it cannot be loaded, the menu still works without it.
    getCurrentUser()
      .then((user) => setCompanyName(user.companyName))
      .catch(() => setCompanyName(undefined))
  }, [])

  return (
    <>
      <NavBar companyName={companyName} />
      <Outlet />
    </>
  )
}
