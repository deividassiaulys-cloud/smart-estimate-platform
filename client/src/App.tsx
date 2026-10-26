import { Navigate, Route, Routes } from 'react-router-dom'
import ManagerRoute from './components/ManagerRoute'
import ProtectedRoute from './components/ProtectedRoute'
import SessionExpiryListener from './components/SessionExpiryListener'
import DashboardPage from './pages/Dashboard/DashboardPage'
import LandingPage from './pages/Landing/LandingPage'
import LoginPage from './pages/Login/LoginPage'
import RegisterPage from './pages/Register/RegisterPage'
import ResetPasswordPage from './pages/Team/ResetPasswordPage'
import TeamPage from './pages/Team/TeamPage'
import ViewerFormPage from './pages/Team/ViewerFormPage'

function App() {
  return (
    <>
      <SessionExpiryListener />
      <Routes>
        <Route path="/" element={<LandingPage />} />
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />
        <Route
          path="/dashboard"
          element={
            <ProtectedRoute>
              <DashboardPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/team"
          element={
            <ManagerRoute>
              <TeamPage />
            </ManagerRoute>
          }
        />
        <Route
          path="/team/new"
          element={
            <ManagerRoute>
              <ViewerFormPage />
            </ManagerRoute>
          }
        />
        <Route
          path="/team/:id/password"
          element={
            <ManagerRoute>
              <ResetPasswordPage />
            </ManagerRoute>
          }
        />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </>
  )
}

export default App
