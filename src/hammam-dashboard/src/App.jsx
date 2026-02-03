import { Routes, Route, Navigate } from 'react-router-dom'
import { useAuth } from './context/AuthContext'
import Layout from './components/Layout'
import LoginPage from './pages/LoginPage'
import DashboardPage from './pages/DashboardPage'
import EmployesPage from './pages/EmployesPage'
import HammamsPage from './pages/HammamsPage'
import RapportsPage from './pages/RapportsPage'
import ComptabilitePage from './pages/ComptabilitePage'
import LoadingSpinner from './components/LoadingSpinner'

/**
 * Composant de protection des routes
 */
function ProtectedRoute({ children }) {
    const { isAuthenticated, loading } = useAuth()

    if (loading) {
        return (
            <div className="min-h-screen flex items-center justify-center">
                <LoadingSpinner size="lg" />
            </div>
        )
    }

    if (!isAuthenticated) {
        return <Navigate to="/login" replace />
    }

    return children
}

/**
 * Application principale
 */
function App() {
    const { isAuthenticated, loading } = useAuth()

    if (loading) {
        return (
            <div className="min-h-screen flex items-center justify-center animated-bg">
                <div className="text-center">
                    <LoadingSpinner size="lg" />
                    <p className="mt-4 text-slate-400">Chargement...</p>
                </div>
            </div>
        )
    }

    return (
        <Routes>
            {/* Route de login */}
            <Route
                path="/login"
                element={
                    isAuthenticated ? <Navigate to="/" replace /> : <LoginPage />
                }
            />

            {/* Routes protégées */}
            <Route
                path="/"
                element={
                    <ProtectedRoute>
                        <Layout />
                    </ProtectedRoute>
                }
            >
                <Route index element={<DashboardPage />} />
                <Route path="employes" element={<EmployesPage />} />
                <Route path="hammams" element={<HammamsPage />} />
                <Route path="comptabilite" element={<ComptabilitePage />} />
                <Route path="rapports" element={<RapportsPage />} />
            </Route>

            {/* Redirection par défaut */}
            <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
    )
}

export default App
