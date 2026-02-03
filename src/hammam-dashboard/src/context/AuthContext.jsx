import { createContext, useContext, useState, useEffect, useCallback } from 'react'
import { useNavigate } from 'react-router-dom'
import { authService } from '../services/api'
import { toast } from 'react-toastify'

const AuthContext = createContext(null)

/**
 * Provider d'authentification
 * Gère le state de l'utilisateur connecté et les tokens JWT
 */
export function AuthProvider({ children }) {
    const [user, setUser] = useState(null)
    const [loading, setLoading] = useState(true)
    const navigate = useNavigate()

    // Vérifier le token au chargement
    useEffect(() => {
        const checkAuth = async () => {
            const token = localStorage.getItem('token')
            const storedUser = localStorage.getItem('user')

            if (token && storedUser) {
                try {
                    // Valider le token auprès du serveur
                    const isValid = await authService.validateToken()

                    if (isValid) {
                        setUser(JSON.parse(storedUser))
                    } else {
                        // Token invalide, nettoyer
                        localStorage.removeItem('token')
                        localStorage.removeItem('refreshToken')
                        localStorage.removeItem('user')
                    }
                } catch (error) {
                    console.error('Erreur de validation token:', error)
                    localStorage.removeItem('token')
                    localStorage.removeItem('refreshToken')
                    localStorage.removeItem('user')
                }
            }

            setLoading(false)
        }

        checkAuth()
    }, [])

    // Vérifier l'expiration du token (8 heures)
    useEffect(() => {
        if (!user) return

        const tokenExpiry = localStorage.getItem('tokenExpiry')

        if (tokenExpiry) {
            const expiryTime = new Date(tokenExpiry).getTime()
            const now = Date.now()
            const timeUntilExpiry = expiryTime - now

            if (timeUntilExpiry <= 0) {
                // Token expiré
                logout()
                toast.warning('Session expirée, veuillez vous reconnecter')
            } else {
                // Programmer la déconnexion automatique
                const timer = setTimeout(() => {
                    logout()
                    toast.warning('Session expirée, veuillez vous reconnecter')
                }, timeUntilExpiry)

                return () => clearTimeout(timer)
            }
        }
    }, [user])

    /**
     * Connexion utilisateur
     */
    const login = useCallback(async (username, password) => {
        try {
            const response = await authService.login(username, password)

            if (response.token) {
                // Stocker les tokens
                localStorage.setItem('token', response.token)
                localStorage.setItem('refreshToken', response.refreshToken)
                localStorage.setItem('user', JSON.stringify(response.employe))
                localStorage.setItem('tokenExpiry', response.expiresAt)

                setUser(response.employe)
                toast.success(`Bienvenue, ${response.employe.prenom} !`)
                navigate('/')

                return { success: true }
            }
        } catch (error) {
            console.error('Erreur de connexion:', error)
            const message = error.response?.data?.message || 'Identifiants invalides'
            toast.error(message)
            return { success: false, error: message }
        }
    }, [navigate])

    /**
     * Déconnexion utilisateur
     */
    const logout = useCallback(async () => {
        try {
            await authService.logout()
        } catch (error) {
            console.error('Erreur lors de la déconnexion:', error)
        } finally {
            // Nettoyer le localStorage
            localStorage.removeItem('token')
            localStorage.removeItem('refreshToken')
            localStorage.removeItem('user')
            localStorage.removeItem('tokenExpiry')

            setUser(null)
            navigate('/login')
        }
    }, [navigate])

    /**
     * Rafraîchir le token
     */
    const refreshAuth = useCallback(async () => {
        try {
            const refreshToken = localStorage.getItem('refreshToken')

            if (!refreshToken) {
                throw new Error('Pas de refresh token')
            }

            const response = await authService.refreshToken(refreshToken)

            if (response.token) {
                localStorage.setItem('token', response.token)
                localStorage.setItem('refreshToken', response.refreshToken)
                localStorage.setItem('tokenExpiry', response.expiresAt)

                return true
            }
        } catch (error) {
            console.error('Erreur refresh token:', error)
            logout()
            return false
        }
    }, [logout])

    const value = {
        user,
        loading,
        isAuthenticated: !!user,
        isAdmin: user?.role === 'Admin',
        login,
        logout,
        refreshAuth,
    }

    return (
        <AuthContext.Provider value={value}>
            {children}
        </AuthContext.Provider>
    )
}

/**
 * Hook pour utiliser le context d'authentification
 */
export function useAuth() {
    const context = useContext(AuthContext)

    if (!context) {
        throw new Error('useAuth doit être utilisé dans un AuthProvider')
    }

    return context
}
