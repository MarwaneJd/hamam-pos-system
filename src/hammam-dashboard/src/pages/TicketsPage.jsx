import { useState, useEffect } from 'react'
import { ticketsService, hammamsService, employesService } from '../services/api'
import { toast } from 'react-toastify'
import {
    Ticket,
    Search,
    Filter,
    Calendar,
    Building2,
    Users,
    RefreshCw,
    ChevronLeft,
    ChevronRight,
    Hash,
    Clock,
    Tag,
    DollarSign,
    Download,
    Loader2,
} from 'lucide-react'
import LoadingSpinner from '../components/LoadingSpinner'

/**
 * Page de visualisation des tickets pour l'admin
 */
export default function TicketsPage() {
    const [tickets, setTickets] = useState([])
    const [hammams, setHammams] = useState([])
    const [employes, setEmployes] = useState([])
    const [filteredEmployes, setFilteredEmployes] = useState([])
    const [loading, setLoading] = useState(true)
    const [refreshing, setRefreshing] = useState(false)

    // Filtres
    const [selectedHammam, setSelectedHammam] = useState('')
    const [selectedEmploye, setSelectedEmploye] = useState('')
    const [dateFrom, setDateFrom] = useState(() => {
        const today = new Date()
        return today.toISOString().split('T')[0]
    })
    const [dateTo, setDateTo] = useState(() => {
        const today = new Date()
        return today.toISOString().split('T')[0]
    })
    const [searchQuery, setSearchQuery] = useState('')

    // Pagination
    const [currentPage, setCurrentPage] = useState(1)
    const itemsPerPage = 20

    // Charger les données initiales
    useEffect(() => {
        loadInitialData()
    }, [])

    // Charger les tickets quand les filtres changent avec un délai
    useEffect(() => {
        const timeoutId = setTimeout(() => {
            loadTickets()
        }, 300)
        return () => clearTimeout(timeoutId)
    }, [selectedHammam, selectedEmploye, dateFrom, dateTo])

    // Filtrer les employés quand le hammam change
    useEffect(() => {
        if (selectedHammam) {
            setFilteredEmployes(employes.filter(e => e.hammamId === selectedHammam))
            // Reset employee filter if not in the selected hammam
            const currentEmployeInHammam = employes.find(e => e.id === selectedEmploye && e.hammamId === selectedHammam)
            if (!currentEmployeInHammam) {
                setSelectedEmploye('')
            }
        } else {
            setFilteredEmployes(employes)
        }
    }, [selectedHammam, employes])

    const loadInitialData = async () => {
        try {
            const [hammamsData, employesData] = await Promise.all([
                hammamsService.getAll(),
                employesService.getAll()
            ])

            setHammams(hammamsData.map(h => ({
                id: h.id,
                nom: h.nom
            })))

            setEmployes(employesData.map(e => ({
                id: e.id,
                nom: `${e.prenom} ${e.nom}`,
                hammamId: e.hammamId
            })))

            setFilteredEmployes(employesData.map(e => ({
                id: e.id,
                nom: `${e.prenom} ${e.nom}`,
                hammamId: e.hammamId
            })))

            await loadTickets()
        } catch (error) {
            console.error('Erreur chargement initial:', error)
            toast.error('Erreur de chargement des données')
        } finally {
            setLoading(false)
        }
    }

    const loadTickets = async () => {
        try {
            setRefreshing(true)
            setCurrentPage(1)

            // Construire les paramètres de filtre
            const from = dateFrom ? new Date(dateFrom + 'T00:00:00').toISOString() : null
            const to = dateTo ? new Date(dateTo + 'T23:59:59').toISOString() : null

            const data = await ticketsService.getAll(
                selectedHammam || null,
                selectedEmploye || null,
                from,
                to
            )

            setTickets(data || [])
        } catch (error) {
            console.error('Erreur chargement tickets:', error)
            toast.error('Erreur de chargement des tickets')
        } finally {
            setRefreshing(false)
        }
    }

    // Recherche locale dans les tickets chargés
    const displayedTickets = tickets.filter(t => {
        if (!searchQuery) return true
        const q = searchQuery.toLowerCase()
        return (
            t.id?.toLowerCase().includes(q) ||
            t.typeTicketNom?.toLowerCase().includes(q) ||
            t.employeNom?.toLowerCase().includes(q) ||
            t.hammamNom?.toLowerCase().includes(q) ||
            t.prix?.toString().includes(q)
        )
    })

    // Pagination
    const totalPages = Math.ceil(displayedTickets.length / itemsPerPage)
    const paginatedTickets = displayedTickets.slice(
        (currentPage - 1) * itemsPerPage,
        currentPage * itemsPerPage
    )

    // Stats rapides
    const totalTickets = displayedTickets.length
    const totalRevenue = displayedTickets.reduce((sum, t) => sum + (t.prix || 0), 0)

    // Formater la date/heure
    const formatDateTime = (dateStr) => {
        if (!dateStr) return '-'
        const date = new Date(dateStr)
        return date.toLocaleString('fr-FR', {
            day: '2-digit',
            month: '2-digit',
            year: 'numeric',
            hour: '2-digit',
            minute: '2-digit',
            second: '2-digit',
        })
    }

    // Formater le prix
    const formatPrice = (prix) => {
        return `${(prix || 0).toFixed(2)} DH`
    }

    // Raccourcir l'ID pour l'affichage
    const shortId = (id) => {
        if (!id) return '-'
        return id.substring(0, 8).toUpperCase()
    }

    // Exporter les tickets en CSV
    const exportCsv = () => {
        if (displayedTickets.length === 0) {
            toast.warning('Aucun ticket à exporter')
            return
        }

        const headers = ['ID Ticket', 'Produit', 'Prix (DH)', 'Hammam', 'Employé', 'Date/Heure', 'Statut Sync']
        const rows = displayedTickets.map(t => [
            shortId(t.id),
            t.typeTicketNom || '',
            (t.prix || 0).toFixed(2),
            t.hammamNom || '',
            t.employeNom || '',
            formatDateTime(t.createdAt),
            t.syncStatus || ''
        ])

        const csvContent = [
            headers.join(','),
            ...rows.map(r => r.map(cell => `"${cell}"`).join(','))
        ].join('\n')

        const blob = new Blob(['\ufeff' + csvContent], { type: 'text/csv;charset=utf-8;' })
        const url = window.URL.createObjectURL(blob)
        const link = document.createElement('a')
        link.href = url
        link.setAttribute('download', `Tickets_${dateFrom}_${dateTo}.csv`)
        document.body.appendChild(link)
        link.click()
        link.remove()
        window.URL.revokeObjectURL(url)

        toast.success('Export CSV téléchargé')
    }

    if (loading) {
        return (
            <div className="flex items-center justify-center h-64">
                <LoadingSpinner size="lg" />
            </div>
        )
    }

    return (
        <div className="space-y-6 animate-fade-in">
            {/* Header */}
            <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
                <div>
                    <h1 className="text-2xl font-bold text-white flex items-center gap-2">
                        <Ticket className="w-7 h-7 text-primary-400" />
                        Tickets
                    </h1>
                    <p className="text-slate-400">
                        Historique et suivi de tous les tickets vendus
                    </p>
                </div>
                <div className="flex items-center gap-3">
                    <button
                        onClick={exportCsv}
                        className="btn-secondary flex items-center gap-2"
                        disabled={displayedTickets.length === 0}
                    >
                        <Download className="w-4 h-4" />
                        Exporter CSV
                    </button>
                    <button
                        onClick={loadTickets}
                        disabled={refreshing}
                        className="btn-primary flex items-center gap-2"
                    >
                        <RefreshCw className={`w-4 h-4 ${refreshing ? 'animate-spin' : ''}`} />
                        Actualiser
                    </button>
                </div>
            </div>

            {/* Stats rapides */}
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div className="glass-card p-4">
                    <div className="flex items-center gap-3">
                        <div className="w-12 h-12 bg-gradient-to-br from-primary-500/20 to-primary-600/20 rounded-xl flex items-center justify-center border border-primary-500/20">
                            <Ticket className="w-6 h-6 text-primary-400" />
                        </div>
                        <div>
                            <p className="text-sm text-slate-400">Total Tickets</p>
                            <p className="text-2xl font-bold text-white">{totalTickets.toLocaleString()}</p>
                        </div>
                    </div>
                </div>
                <div className="glass-card p-4">
                    <div className="flex items-center gap-3">
                        <div className="w-12 h-12 bg-gradient-to-br from-success-500/20 to-success-600/20 rounded-xl flex items-center justify-center border border-success-500/20">
                            <DollarSign className="w-6 h-6 text-success-400" />
                        </div>
                        <div>
                            <p className="text-sm text-slate-400">Total Revenus</p>
                            <p className="text-2xl font-bold text-white">{totalRevenue.toLocaleString()} <span className="text-sm font-normal text-slate-400">DH</span></p>
                        </div>
                    </div>
                </div>
            </div>

            {/* Filtres */}
            <div className="glass-card p-5">
                <div className="flex items-center gap-2 mb-4">
                    <Filter className="w-5 h-5 text-primary-400" />
                    <h3 className="font-semibold text-white">Filtres</h3>
                </div>
                <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
                    {/* Filtre Hammam */}
                    <div>
                        <label className="flex items-center gap-1.5 text-sm font-medium text-slate-300 mb-2">
                            <Building2 className="w-4 h-4" />
                            Hammam
                        </label>
                        <select
                            value={selectedHammam}
                            onChange={(e) => setSelectedHammam(e.target.value)}
                            className="input-field w-full"
                        >
                            <option value="">Tous les hammams</option>
                            {hammams.map(h => (
                                <option key={h.id} value={h.id}>{h.nom}</option>
                            ))}
                        </select>
                    </div>

                    {/* Filtre Employé */}
                    <div>
                        <label className="flex items-center gap-1.5 text-sm font-medium text-slate-300 mb-2">
                            <Users className="w-4 h-4" />
                            Employé
                        </label>
                        <select
                            value={selectedEmploye}
                            onChange={(e) => setSelectedEmploye(e.target.value)}
                            className="input-field w-full"
                        >
                            <option value="">Tous les employés</option>
                            {filteredEmployes.map(e => (
                                <option key={e.id} value={e.id}>{e.nom}</option>
                            ))}
                        </select>
                    </div>

                    {/* Date De */}
                    <div>
                        <label className="flex items-center gap-1.5 text-sm font-medium text-slate-300 mb-2">
                            <Calendar className="w-4 h-4" />
                            Du
                        </label>
                        <input
                            type="date"
                            value={dateFrom}
                            onChange={(e) => setDateFrom(e.target.value)}
                            className="input-field w-full"
                        />
                    </div>

                    {/* Date À */}
                    <div>
                        <label className="flex items-center gap-1.5 text-sm font-medium text-slate-300 mb-2">
                            <Calendar className="w-4 h-4" />
                            Au
                        </label>
                        <input
                            type="date"
                            value={dateTo}
                            onChange={(e) => setDateTo(e.target.value)}
                            className="input-field w-full"
                        />
                    </div>
                </div>

                {/* Barre de recherche */}
                <div className="mt-4 relative">
                    <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 text-slate-400" />
                    <input
                        type="text"
                        placeholder="Rechercher par ID, produit, employé..."
                        value={searchQuery}
                        onChange={(e) => { setSearchQuery(e.target.value); setCurrentPage(1) }}
                        className="input-field pl-10 w-full"
                    />
                </div>
            </div>

            {/* Table des tickets */}
            <div className="glass-card overflow-hidden">
                {refreshing ? (
                    <div className="flex items-center justify-center py-12">
                        <Loader2 className="w-8 h-8 text-primary-400 animate-spin" />
                    </div>
                ) : displayedTickets.length === 0 ? (
                    <div className="flex flex-col items-center justify-center py-16 text-slate-400">
                        <Ticket className="w-16 h-16 mb-4 opacity-30" />
                        <p className="text-lg font-medium">Aucun ticket trouvé</p>
                        <p className="text-sm mt-1">Essayez de modifier les filtres pour voir des résultats</p>
                    </div>
                ) : (
                    <>
                        <div className="table-container">
                            <table className="custom-table">
                                <thead>
                                    <tr>
                                        <th>
                                            <div className="flex items-center gap-1.5">
                                                <Hash className="w-4 h-4" />
                                                ID Ticket
                                            </div>
                                        </th>
                                        <th>
                                            <div className="flex items-center gap-1.5">
                                                <Tag className="w-4 h-4" />
                                                Produit
                                            </div>
                                        </th>
                                        <th>
                                            <div className="flex items-center gap-1.5">
                                                <DollarSign className="w-4 h-4" />
                                                Prix
                                            </div>
                                        </th>
                                        <th>
                                            <div className="flex items-center gap-1.5">
                                                <Building2 className="w-4 h-4" />
                                                Hammam
                                            </div>
                                        </th>
                                        <th>
                                            <div className="flex items-center gap-1.5">
                                                <Users className="w-4 h-4" />
                                                Employé
                                            </div>
                                        </th>
                                        <th>
                                            <div className="flex items-center gap-1.5">
                                                <Clock className="w-4 h-4" />
                                                Date / Heure
                                            </div>
                                        </th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {paginatedTickets.map((ticket) => (
                                        <tr key={ticket.id} className="group">
                                            <td>
                                                <span className="font-mono text-sm bg-slate-700/50 px-2.5 py-1 rounded-lg text-primary-300 border border-slate-600/30">
                                                    #{shortId(ticket.id)}
                                                </span>
                                            </td>
                                            <td>
                                                <span className="font-medium text-white">
                                                    {ticket.typeTicketNom || '-'}
                                                </span>
                                            </td>
                                            <td>
                                                <span className="font-semibold text-success-400">
                                                    {formatPrice(ticket.prix)}
                                                </span>
                                            </td>
                                            <td>
                                                <span className="text-slate-300">
                                                    {ticket.hammamNom || '-'}
                                                </span>
                                            </td>
                                            <td>
                                                <div className="flex items-center gap-2">
                                                    <div className="w-7 h-7 bg-gradient-to-br from-primary-500/30 to-accent-500/30 rounded-full flex items-center justify-center text-white text-xs font-bold border border-primary-500/20">
                                                        {ticket.employeNom?.[0] || '?'}
                                                    </div>
                                                    <span className="text-slate-300">
                                                        {ticket.employeNom || '-'}
                                                    </span>
                                                </div>
                                            </td>
                                            <td>
                                                <div className="flex items-center gap-1.5 text-slate-300">
                                                    <Clock className="w-3.5 h-3.5 text-slate-500" />
                                                    <span className="text-sm">{formatDateTime(ticket.createdAt)}</span>
                                                </div>
                                            </td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>

                        {/* Pagination */}
                        {totalPages > 1 && (
                            <div className="flex items-center justify-between px-6 py-4 border-t border-slate-700/50">
                                <p className="text-sm text-slate-400">
                                    Affichage de {((currentPage - 1) * itemsPerPage) + 1} à {Math.min(currentPage * itemsPerPage, displayedTickets.length)} sur {displayedTickets.length} tickets
                                </p>
                                <div className="flex items-center gap-2">
                                    <button
                                        onClick={() => setCurrentPage(p => Math.max(1, p - 1))}
                                        disabled={currentPage === 1}
                                        className="p-2 text-slate-400 hover:text-white hover:bg-slate-700/50 rounded-lg transition-colors disabled:opacity-30 disabled:cursor-not-allowed"
                                    >
                                        <ChevronLeft className="w-5 h-5" />
                                    </button>
                                    
                                    {/* Page numbers */}
                                    {Array.from({ length: Math.min(5, totalPages) }, (_, i) => {
                                        let pageNum
                                        if (totalPages <= 5) {
                                            pageNum = i + 1
                                        } else if (currentPage <= 3) {
                                            pageNum = i + 1
                                        } else if (currentPage >= totalPages - 2) {
                                            pageNum = totalPages - 4 + i
                                        } else {
                                            pageNum = currentPage - 2 + i
                                        }
                                        return (
                                            <button
                                                key={pageNum}
                                                onClick={() => setCurrentPage(pageNum)}
                                                className={`w-9 h-9 rounded-lg text-sm font-medium transition-all ${
                                                    currentPage === pageNum
                                                        ? 'bg-primary-500 text-white shadow-lg shadow-primary-500/30'
                                                        : 'text-slate-400 hover:text-white hover:bg-slate-700/50'
                                                }`}
                                            >
                                                {pageNum}
                                            </button>
                                        )
                                    })}

                                    <button
                                        onClick={() => setCurrentPage(p => Math.min(totalPages, p + 1))}
                                        disabled={currentPage === totalPages}
                                        className="p-2 text-slate-400 hover:text-white hover:bg-slate-700/50 rounded-lg transition-colors disabled:opacity-30 disabled:cursor-not-allowed"
                                    >
                                        <ChevronRight className="w-5 h-5" />
                                    </button>
                                </div>
                            </div>
                        )}
                    </>
                )}
            </div>
        </div>
    )
}
