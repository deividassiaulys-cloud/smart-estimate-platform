import { Navigate, Route, Routes } from 'react-router-dom'
import ManagerRoute from './components/ManagerRoute'
import ProtectedRoute from './components/ProtectedRoute'
import SessionExpiryListener from './components/SessionExpiryListener'
import DashboardPage from './pages/Dashboard/DashboardPage'
import EstimateCreatePage from './pages/Estimates/EstimateCreatePage'
import EstimateEditorPage from './pages/Estimates/EstimateEditorPage'
import EstimateViewPage from './pages/Estimates/EstimateViewPage'
import LandingPage from './pages/Landing/LandingPage'
import LoginPage from './pages/Login/LoginPage'
import MaterialFormPage from './pages/Materials/MaterialFormPage'
import MaterialsPage from './pages/Materials/MaterialsPage'
import ProjectDetailPage from './pages/Projects/ProjectDetailPage'
import ProjectFormPage from './pages/Projects/ProjectFormPage'
import ProjectsPage from './pages/Projects/ProjectsPage'
import RegisterPage from './pages/Register/RegisterPage'
import ResetPasswordPage from './pages/Team/ResetPasswordPage'
import TeamPage from './pages/Team/TeamPage'
import ViewerFormPage from './pages/Team/ViewerFormPage'
import WorkFormPage from './pages/Works/WorkFormPage'
import WorksPage from './pages/Works/WorksPage'

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
        <Route
          path="/materials"
          element={
            <ManagerRoute>
              <MaterialsPage />
            </ManagerRoute>
          }
        />
        <Route
          path="/materials/new"
          element={
            <ManagerRoute>
              <MaterialFormPage />
            </ManagerRoute>
          }
        />
        <Route
          path="/materials/:id/edit"
          element={
            <ManagerRoute>
              <MaterialFormPage />
            </ManagerRoute>
          }
        />
        <Route
          path="/works"
          element={
            <ManagerRoute>
              <WorksPage />
            </ManagerRoute>
          }
        />
        <Route
          path="/works/new"
          element={
            <ManagerRoute>
              <WorkFormPage />
            </ManagerRoute>
          }
        />
        <Route
          path="/works/:id/edit"
          element={
            <ManagerRoute>
              <WorkFormPage />
            </ManagerRoute>
          }
        />
        <Route
          path="/projects"
          element={
            <ProtectedRoute>
              <ProjectsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/projects/new"
          element={
            <ManagerRoute>
              <ProjectFormPage />
            </ManagerRoute>
          }
        />
        <Route
          path="/projects/:id"
          element={
            <ProtectedRoute>
              <ProjectDetailPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/projects/:id/edit"
          element={
            <ManagerRoute>
              <ProjectFormPage />
            </ManagerRoute>
          }
        />
        <Route
          path="/projects/:projectId/estimates/new"
          element={
            <ManagerRoute>
              <EstimateCreatePage />
            </ManagerRoute>
          }
        />
        <Route
          path="/estimates/:id"
          element={
            <ManagerRoute>
              <EstimateEditorPage />
            </ManagerRoute>
          }
        />
        <Route
          path="/estimates/:id/view"
          element={
            <ProtectedRoute>
              <EstimateViewPage />
            </ProtectedRoute>
          }
        />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </>
  )
}

export default App
