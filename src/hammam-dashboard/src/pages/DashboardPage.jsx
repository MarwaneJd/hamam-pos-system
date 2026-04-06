import { useState, useEffect } from 'react'
import { statsService } from '../services/api'
import {
    Ticket,
    DollarSign,
    Building2,
    TrendingUp,
    TrendingDown,
    AlertTriangle,
    RefreshCw,
} from 'lucide-react'
import LoadingSpinner from '../components/LoadingSpinner'
import { format, startOfDay, endOfDay, subDays, startOfMonth, endOfMonth } from 'date-fns'
import { fr } from 'date-fns/locale'

/**
 * Page Dashboard avec statistiques temps réel
 */
export default function DashboardPage() {
    const [stats, setStats] = useState(null)
    const [loading, setLoading] = useState(true)
    const [error, setError] = useState(null)
    const [period, setPeriod] = useState('today')
    const [refreshing, setRefreshing] = useState(false)

    // Calculer les dates selon la période
    const getDateRange = () => {
        const now = new Date()
        switch (period) {
            case 'today':
                return { from: startOfDay(now), to: endOfDay(now) }
            case 'yesterday': {
                const yesterday = subDays(now, 1)
                return { from: startOfDay(yesterday), to: endOfDay(yesterday) }
            }
            case 'week':
                return { from: startOfDay(subDays(now, 7)), to: endOfDay(now) }
            case 'month':
                return { from: startOfMonth(now), to: endOfMonth(now) }
            default:
                return { from: startOfDay(now), to: endOfDay(now) }
        }
    }

    // Charger les stats depuis l'API
    const loadStats = async (showRefresh = false) => {
        if (showRefresh) setRefreshing(true)
        else setLoading(true)
        setError(null)

        try {
            const { from, to } = getDateRange()
            const data = await statsService.getDashboardStats(from, to)
            setStats(data)
        } catch (err) {
            console.error('Erreur chargement stats:', err)
            setError('Impossible de charger les statistiques. Veuillez réessayer.')
            // Ne pas utiliser de données mockées - afficher l'erreur
        } finally {
            setLoading(false)
            setRefreshing(false)
        }
    }

    useEffect(() => {
        loadStats()
    }, [period])

    // Auto-refresh toutes les 2 minutes
    useEffect(() => {
        const interval = setInterval(() => {
            loadStats(true)
        }, 120000)
        return () => clearInterval(interval)
    }, [period])

    if (loading) {
        return (
            <div className="flex items-center justify-center h-64">
                <LoadingSpinner size="lg" />
            </div>
        )
    }

    if (error && !stats) {
        return (
            <div className="glass-card p-8 text-center">
                <AlertTriangle className="w-12 h-12 text-danger-400 mx-auto mb-4" />
                <p className="text-white text-lg mb-4">{error}</p>
                <button
                    onClick={() => loadStats()}
                    className="btn-primary"
                >
                    Réessayer
                </button>
            </div>
        )
    }

    const periodLabels = {
        today: "Aujourd'hui",
        yesterday: 'Hier',
        week: '7 derniers jours',
        month: 'Ce mois',
    }

    // Variation en pourcentage
    const renderVariation = (value, isPositiveGood = true) => {
        if (value === undefined || value === null) return null
        const isPositive = value >= 0
        const colorClass = isPositiveGood
            ? (isPositive ? 'text-success-400' : 'text-danger-400')
            : (isPositive ? 'text-danger-400' : 'text-success-400')

        return (
            <div className={`flex items-center gap-1 mt-2 ${colorClass} text-xs sm:text-sm`}>
                {isPositive ? <TrendingUp className="w-3 h-3 sm:w-4 sm:h-4 flex-shrink-0" /> : <TrendingDown className="w-3 h-3 sm:w-4 sm:h-4 flex-shrink-0" />}
                <span className="leading-tight">{isPositive ? '+' : ''}{value}% vs période précédente</span>
            </div>
        )
    }

    return (
        <div className="space-y-6 animate-fade-in">
            {/* Header */}
            <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
                <div>
                    <h1 className="text-xl sm:text-2xl font-bold text-white">Dashboard</h1>
                    <p className="text-slate-400 text-sm sm:text-base">
                        Vue d'ensemble des performances • {format(new Date(), 'EEEE d MMMM yyyy', { locale: fr })}
                    </p>
                </div>

                <div className="flex items-center gap-2 sm:gap-3">
                    {/* Sélecteur de période */}
                    <div className="flex items-center gap-1 bg-slate-800 rounded-xl p-1 flex-wrap">
                        {Object.entries(periodLabels).map(([key, label]) => (
                            <button
                                key={key}
                                onClick={() => setPeriod(key)}
                                className={`px-2 sm:px-4 py-1.5 sm:py-2 rounded-lg text-xs sm:text-sm font-medium transition-all whitespace-nowrap ${period === key
                                    ? 'bg-gradient-to-r from-primary-500 to-accent-500 text-white'
                                    : 'text-slate-400 hover:text-white'
                                    }`}
                            >
                                {label}
                            </button>
                        ))}
                    </div>

                    {/* Bouton refresh */}
                    <button
                        onClick={() => loadStats(true)}
                        disabled={refreshing}
                        className="p-2 sm:p-3 bg-slate-800 rounded-xl text-slate-400 hover:text-white transition-colors flex-shrink-0"
                        title="Actualiser les données"
                    >
                        <RefreshCw className={`w-4 h-4 sm:w-5 sm:h-5 ${refreshing ? 'animate-spin' : ''}`} />
                    </button>
                </div>
            </div>

            {/* Message d'erreur non bloquant */}
            {error && (
                <div className="glass-card p-4 border border-danger-500/50 bg-danger-500/10">
                    <p className="text-danger-400 text-sm flex items-center gap-2">
                        <AlertTriangle className="w-4 h-4" />
                        {error}
                    </p>
                </div>
            )}

            {/* KPI Cards */}
            <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 gap-4 sm:gap-6">
                {/* Tickets */}
                <div className="stat-card glass-card-hover">
                    <div className="flex items-start justify-between">
                        <div className="min-w-0 flex-1">
                            <p className="text-slate-400 text-xs sm:text-sm font-medium">Total Tickets</p>
                            <p className="text-2xl sm:text-3xl font-bold text-white mt-1 sm:mt-2">
                                {stats?.totalTicketsToday?.toLocaleString() ?? 0}
                            </p>
                            {renderVariation(stats?.variationTickets)}
                        </div>
                        <div className="w-10 h-10 sm:w-12 sm:h-12 bg-primary-500/20 rounded-xl flex items-center justify-center flex-shrink-0 ml-2">
                            <Ticket className="w-5 h-5 sm:w-6 sm:h-6 text-primary-400" />
                        </div>
                    </div>
                </div>

                {/* Revenus */}
                <div className="stat-card glass-card-hover">
                    <div className="flex items-start justify-between">
                        <div className="min-w-0 flex-1">
                            <p className="text-slate-400 text-xs sm:text-sm font-medium">Total Revenus</p>
                            <p className="text-2xl sm:text-3xl font-bold text-white mt-1 sm:mt-2">
                                {stats?.totalRevenueToday?.toLocaleString() ?? 0} <span className="text-base sm:text-lg">DH</span>
                            </p>
                            {renderVariation(stats?.variationRevenue)}
                        </div>
                        <div className="w-10 h-10 sm:w-12 sm:h-12 bg-success-500/20 rounded-xl flex items-center justify-center flex-shrink-0 ml-2">
                            <DollarSign className="w-5 h-5 sm:w-6 sm:h-6 text-success-400" />
                        </div>
                    </div>
                </div>

                {/* Hammams actifs */}
                <div className="stat-card glass-card-hover sm:col-span-2 md:col-span-1">
                    <div className="flex items-start justify-between">
                        <div className="min-w-0 flex-1">
                            <p className="text-slate-400 text-xs sm:text-sm font-medium">Hammams Actifs</p>
                            <p className="text-2xl sm:text-3xl font-bold text-white mt-1 sm:mt-2">
                                {stats?.hammamsActifs ?? 0}
                            </p>
                            <div className="flex items-center gap-1 mt-2 text-primary-400 text-xs sm:text-sm">
                                <Building2 className="w-3 h-3 sm:w-4 sm:h-4" />
                                <span>Tous opérationnels</span>
                            </div>
                        </div>
                        <div className="w-10 h-10 sm:w-12 sm:h-12 bg-accent-500/20 rounded-xl flex items-center justify-center flex-shrink-0 ml-2">
                            <Building2 className="w-5 h-5 sm:w-6 sm:h-6 text-accent-400" />
                        </div>
                    </div>
                </div>
            </div>

            {/* Tables */}
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-4 sm:gap-6">
                {/* Tableau Hammams */}
                <div className="glass-card p-4 sm:p-6">
                    <h2 className="text-base sm:text-lg font-semibold text-white mb-3 sm:mb-4 flex items-center gap-2">
                        <Building2 className="w-5 h-5 text-primary-400" />
                        Performances par Hammam
                    </h2>
                    <div className="table-container">
                        <table className="custom-table">
                            <thead>
                                <tr>
                                    <th>Hammam</th>
                                    <th className="text-right">Tickets</th>
                                    <th className="text-right">Revenus</th>
                                    <th className="text-right hidden sm:table-cell">Écart</th>
                                </tr>
                            </thead>
                            <tbody>
                                {stats?.hammamStats?.length > 0 ? (
                                    stats.hammamStats.map((hammam) => (
                                        <tr key={hammam.hammamId}>
                                            <td className="font-medium text-white text-sm">
                                                {hammam.hammamNom}
                                            </td>
                                            <td className="text-right text-sm">{hammam.ticketsCount}</td>
                                            <td className="text-right text-sm whitespace-nowrap">{hammam.revenue} DH</td>
                                            <td className="text-right hidden sm:table-cell">
                                                <span
                                                    className={`inline-flex items-center gap-1 font-medium text-sm ${
                                                        hammam.ecart > 0
                                                            ? 'text-success-400'
                                                            : hammam.ecart < 0
                                                                ? 'text-danger-400'
                                                                : 'text-slate-400'
                                                        }`}
                                                >
                                                    {hammam.ecart < 0 && <AlertTriangle className="w-3 h-3" />}
                                                    {Math.abs(hammam.ecart || 0).toFixed(2)} DH
                                                </span>
                                            </td>
                                        </tr>
                                    ))
                                ) : (
                                    <tr>
                                        <td colSpan="4" className="text-center text-slate-400 py-8">
                                            Aucune donnée disponible pour cette période
                                        </td>
                                    </tr>
                                )}
                            </tbody>
                        </table>
                    </div>
                </div>

                {/* Tableau Employés */}
                <div className="glass-card p-4 sm:p-6">
                    <h2 className="text-base sm:text-lg font-semibold text-white mb-3 sm:mb-4 flex items-center gap-2">
                        <TrendingUp className="w-5 h-5 text-success-400" />
                        Classement Employés
                    </h2>
                    <div className="table-container">
                        <table className="custom-table">
                            <thead>
                                <tr>
                                    <th>#</th>
                                    <th>Employé</th>
                                    <th className="hidden sm:table-cell">Hammam</th>
                                    <th className="text-right">Tickets</th>
                                    <th className="text-right">Revenus</th>
                                </tr>
                            </thead>
                            <tbody>
                                {stats?.employeStats?.length > 0 ? (
                                    stats.employeStats.slice(0, 10).map((employe) => (
                                        <tr key={employe.employeId}>
                                            <td>
                                                <span
                                                    className={`inline-flex items-center justify-center w-6 h-6 rounded-full text-xs font-bold ${employe.classement === 1
                                                        ? 'bg-yellow-500/20 text-yellow-400'
                                                        : employe.classement === 2
                                                            ? 'bg-slate-400/20 text-slate-300'
                                                            : employe.classement === 3
                                                                ? 'bg-amber-700/20 text-amber-600'
                                                                : 'bg-slate-700 text-slate-400'
                                                        }`}
                                                >
                                                    {employe.classement}
                                                </span>
                                            </td>
                                            <td className="font-medium text-white text-sm">{employe.employeNom}</td>
                                            <td className="text-slate-400 text-sm hidden sm:table-cell">{employe.hammamNom}</td>
                                            <td className="text-right text-sm">{employe.ticketsCount}</td>
                                            <td className="text-right text-success-400 font-medium text-sm whitespace-nowrap">
                                                {employe.revenue} DH
                                            </td>
                                        </tr>
                                    ))
                                ) : (
                                    <tr>
                                        <td colSpan="5" className="text-center text-slate-400 py-8">
                                            Aucune donnée disponible pour cette période
                                        </td>
                                    </tr>
                                )}
                            </tbody>
                        </table>
                    </div>
                </div>
            </div>
        </div>
    )
}
