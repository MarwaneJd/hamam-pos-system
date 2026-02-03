import axios from 'axios'

// Configuration de base Axios
const api = axios.create({
    baseURL: '/api',
    headers: {
        'Content-Type': 'application/json',
    },
})

// Intercepteur pour ajouter le token JWT à chaque requête
api.interceptors.request.use(
    (config) => {
        const token = localStorage.getItem('token')
        if (token) {
            config.headers.Authorization = `Bearer ${token}`
        }
        return config
    },
    (error) => Promise.reject(error)
)

// Intercepteur pour gérer les erreurs 401 (token expiré)
api.interceptors.response.use(
    (response) => response,
    async (error) => {
        const originalRequest = error.config

        // Si erreur 401 et pas déjà une tentative de retry
        if (error.response?.status === 401 && !originalRequest._retry) {
            originalRequest._retry = true

            try {
                const refreshToken = localStorage.getItem('refreshToken')

                if (refreshToken) {
                    const response = await axios.post('/api/auth/refresh', {
                        refreshToken,
                    })

                    if (response.data.token) {
                        localStorage.setItem('token', response.data.token)
                        localStorage.setItem('refreshToken', response.data.refreshToken)

                        // Réessayer la requête originale
                        originalRequest.headers.Authorization = `Bearer ${response.data.token}`
                        return api(originalRequest)
                    }
                }
            } catch (refreshError) {
                // Échec du refresh, déconnecter l'utilisateur
                localStorage.removeItem('token')
                localStorage.removeItem('refreshToken')
                localStorage.removeItem('user')
                window.location.href = '/login'
            }
        }

        return Promise.reject(error)
    }
)

// ==================== AUTH SERVICE ====================

export const authService = {
    /**
     * Connexion utilisateur
     */
    login: async (username, password) => {
        const response = await api.post('/auth/login', { username, password })
        return response.data
    },

    /**
     * Déconnexion
     */
    logout: async () => {
        const response = await api.post('/auth/logout')
        return response.data
    },

    /**
     * Rafraîchir le token
     */
    refreshToken: async (refreshToken) => {
        const response = await api.post('/auth/refresh', { refreshToken })
        return response.data
    },

    /**
     * Valider le token actuel
     */
    validateToken: async () => {
        try {
            await api.get('/auth/validate')
            return true
        } catch {
            return false
        }
    },
}

// ==================== STATS SERVICE ====================

export const statsService = {
    /**
     * Récupérer les stats du dashboard
     */
    getDashboardStats: async (from = null, to = null) => {
        const params = {}
        if (from) params.from = from.toISOString()
        if (to) params.to = to.toISOString()

        const response = await api.get('/stats/dashboard', { params })
        return response.data
    },

    /**
     * Stats par hammam
     */
    getHammamStats: async (from, to) => {
        const response = await api.get('/stats/hammams', {
            params: { from: from.toISOString(), to: to.toISOString() },
        })
        return response.data
    },

    /**
     * Stats par employé
     */
    getEmployeStats: async (from, to) => {
        const response = await api.get('/stats/employes', {
            params: { from: from.toISOString(), to: to.toISOString() },
        })
        return response.data
    },
}

// ==================== EMPLOYES SERVICE ====================

export const employesService = {
    /**
     * Récupérer tous les employés
     */
    getAll: async () => {
        const response = await api.get('/employes')
        return response.data
    },

    /**
     * Récupérer un employé par ID
     */
    getById: async (id) => {
        const response = await api.get(`/employes/${id}`)
        return response.data
    },

    /**
     * Récupérer les employés d'un hammam
     */
    getByHammam: async (hammamId) => {
        const response = await api.get(`/employes/hammam/${hammamId}`)
        return response.data
    },

    /**
     * Créer un employé
     */
    create: async (data) => {
        const response = await api.post('/employes', data)
        return response.data
    },

    /**
     * Modifier un employé
     */
    update: async (id, data) => {
        const response = await api.put(`/employes/${id}`, data)
        return response.data
    },

    /**
     * Supprimer un employé
     */
    delete: async (id) => {
        const response = await api.delete(`/employes/${id}`)
        return response.data
    },

    /**
     * Reset mot de passe
     */
    resetPassword: async (id, newPassword) => {
        const response = await api.patch(`/employes/${id}/reset-password`, { newPassword })
        return response.data
    },

    /**
     * Activer/Désactiver un employé (toggle)
     */
    toggleStatus: async (id) => {
        const response = await api.patch(`/employes/${id}/toggle-status`)
        return response.data
    },
}

// ==================== HAMMAMS SERVICE ====================

export const hammamsService = {
    /**
     * Récupérer tous les hammams
     */
    getAll: async () => {
        const response = await api.get('/hammams')
        return response.data
    },

    /**
     * Récupérer un hammam par ID
     */
    getById: async (id) => {
        const response = await api.get(`/hammams/${id}`)
        return response.data
    },

    /**
     * Créer un hammam
     */
    create: async (data) => {
        const response = await api.post('/hammams', data)
        return response.data
    },

    /**
     * Modifier un hammam
     */
    update: async (id, data) => {
        const response = await api.put(`/hammams/${id}`, data)
        return response.data
    },

    /**
     * Supprimer un hammam
     */
    delete: async (id) => {
        const response = await api.delete(`/hammams/${id}`)
        return response.data
    },

    /**
     * Activer/Désactiver un hammam (toggle)
     */
    toggleStatus: async (id) => {
        const response = await api.patch(`/hammams/${id}/toggle-status`)
        return response.data
    },
}

// ==================== TICKETS SERVICE ====================

export const ticketsService = {
    /**
     * Récupérer les tickets d'un hammam
     */
    getByHammam: async (hammamId, from = null, to = null) => {
        const params = {}
        if (from) params.from = from.toISOString()
        if (to) params.to = to.toISOString()

        const response = await api.get(`/tickets/hammam/${hammamId}`, { params })
        return response.data
    },

    /**
     * Récupérer les tickets d'un employé
     */
    getByEmploye: async (employeId, from = null, to = null) => {
        const params = {}
        if (from) params.from = from.toISOString()
        if (to) params.to = to.toISOString()

        const response = await api.get(`/tickets/employe/${employeId}`, { params })
        return response.data
    },

    /**
     * Compteur de tickets aujourd'hui
     */
    getTodayCount: async (hammamId = null, employeId = null) => {
        const params = {}
        if (hammamId) params.hammamId = hammamId
        if (employeId) params.employeId = employeId

        const response = await api.get('/tickets/count/today', { params })
        return response.data
    },

    /**
     * Revenu d'aujourd'hui
     */
    getTodayRevenue: async (hammamId = null, employeId = null) => {
        const params = {}
        if (hammamId) params.hammamId = hammamId
        if (employeId) params.employeId = employeId

        const response = await api.get('/tickets/revenue/today', { params })
        return response.data
    },
}

// ==================== TYPE TICKETS SERVICE ====================

export const typeTicketsService = {
    /**
     * Récupérer tous les types de tickets
     */
    getAll: async () => {
        const response = await api.get('/typetickets')
        return response.data
    },

    /**
     * Récupérer les types de tickets d'un hammam
     */
    getByHammam: async (hammamId) => {
        const response = await api.get(`/typetickets/hammam/${hammamId}`)
        return response.data
    },

    /**
     * Récupérer un type de ticket par ID
     */
    getById: async (id) => {
        const response = await api.get(`/typetickets/${id}`)
        return response.data
    },

    /**
     * Créer un type de ticket
     */
    create: async (data) => {
        const response = await api.post('/typetickets', data)
        return response.data
    },

    /**
     * Modifier un type de ticket
     */
    update: async (id, data) => {
        const response = await api.put(`/typetickets/${id}`, data)
        return response.data
    },

    /**
     * Supprimer un type de ticket
     */
    delete: async (id) => {
        const response = await api.delete(`/typetickets/${id}`)
        return response.data
    },

    /**
     * Activer/Désactiver un type de ticket
     */
    toggleStatus: async (id) => {
        const response = await api.patch(`/typetickets/${id}/toggle-status`)
        return response.data
    },
}

// ==================== RAPPORTS SERVICE ====================

export const rapportsService = {
    /**
     * Prévisualiser un rapport
     */
    preview: async (request) => {
        const response = await api.post('/rapports/preview', request)
        return response.data
    },

    /**
     * Télécharger en Excel (format CSV compatible)
     */
    downloadExcel: async (request) => {
        const response = await api.post('/rapports/excel', request, {
            responseType: 'blob',
        })

        // Créer le téléchargement avec extension .csv
        const url = window.URL.createObjectURL(new Blob([response.data]))
        const link = document.createElement('a')
        link.href = url
        link.setAttribute('download', `Rapport_${new Date().toISOString().split('T')[0]}.csv`)
        document.body.appendChild(link)
        link.click()
        link.remove()
    },

    /**
     * Télécharger en format texte (rapport formaté)
     */
    downloadPdf: async (request) => {
        const response = await api.post('/rapports/pdf', request, {
            responseType: 'blob',
        })

        // Télécharger comme fichier texte
        const url = window.URL.createObjectURL(new Blob([response.data]))
        const link = document.createElement('a')
        link.href = url
        link.setAttribute('download', `Rapport_${new Date().toISOString().split('T')[0]}.txt`)
        document.body.appendChild(link)
        link.click()
        link.remove()
    },

    /**
     * Télécharger en CSV
     */
    downloadCsv: async (request) => {
        const response = await api.post('/rapports/csv', request, {
            responseType: 'blob',
        })

        const url = window.URL.createObjectURL(new Blob([response.data]))
        const link = document.createElement('a')
        link.href = url
        link.setAttribute('download', `Rapport_${new Date().toISOString().split('T')[0]}.csv`)
        document.body.appendChild(link)
        link.click()
        link.remove()
    },
}

export default api
