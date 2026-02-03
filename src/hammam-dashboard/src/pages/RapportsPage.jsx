import { useState, useEffect } from 'react'
import { toast } from 'react-toastify'
import { rapportsService, hammamsService, employesService } from '../services/api'
import {
    FileBarChart,
    Calendar,
    Download,
    FileSpreadsheet,
    FileText,
    Eye,
    Loader2,
    Building2,
    Users,
    Filter,
    RefreshCw,
    TrendingUp,
} from 'lucide-react'
import DatePicker from 'react-datepicker'
import 'react-datepicker/dist/react-datepicker.css'
import { format, startOfMonth, endOfMonth, startOfDay, endOfDay } from 'date-fns'
import { fr } from 'date-fns/locale'
import LoadingSpinner from '../components/LoadingSpinner'

/**
 * Page de génération de rapports - Connectée au backend réel
 */
export default function RapportsPage() {
    const [reportType, setReportType] = useState('journalier')
    const [dateFrom, setDateFrom] = useState(new Date())
    const [dateTo, setDateTo] = useState(new Date())
    const [selectedHammams, setSelectedHammams] = useState([])
    const [selectedEmployes, setSelectedEmployes] = useState([])
    const [preview, setPreview] = useState(null)
    const [loading, setLoading] = useState(false)
    const [downloading, setDownloading] = useState(false)
    const [dataLoading, setDataLoading] = useState(true)

    // Données des hammams et employés depuis l'API
    const [hammams, setHammams] = useState([])
    const [employes, setEmployes] = useState([])
    const [allEmployes, setAllEmployes] = useState([]) // Tous les employés pour le filtrage

    // Charger les données de référence
    useEffect(() => {
        loadReferenceData()
    }, [])

    const loadReferenceData = async () => {
        try {
            const [hammamsData, employesData] = await Promise.all([
                hammamsService.getAll(),
                employesService.getAll()
            ])
            setHammams(hammamsData.map(h => ({ id: h.id, nom: h.nom })))
            const allEmps = employesData.map(e => ({ id: e.id, nom: `${e.prenom} ${e.nom}`, hammamId: e.hammamId }))
            setAllEmployes(allEmps)
            setEmployes(allEmps)
        } catch (error) {
            console.error('Erreur chargement données:', error)
            toast.error('Erreur lors du chargement des filtres')
        } finally {
            setDataLoading(false)
        }
    }

    // Filtrer les employés quand les hammams sélectionnés changent
    useEffect(() => {
        if (selectedHammams.length === 0) {
            setEmployes(allEmployes)
            setSelectedEmployes([])
        } else {
            const filteredEmployes = allEmployes.filter(e => selectedHammams.includes(e.hammamId))
            setEmployes(filteredEmployes)
            // Réinitialiser la sélection des employés si certains ne sont plus valides
            setSelectedEmployes(prev => prev.filter(id => filteredEmployes.some(e => e.id === id)))
        }
    }, [selectedHammams, allEmployes])

    // Gérer le changement de type de rapport
    const handleReportTypeChange = (type) => {
        setReportType(type)
        const now = new Date()

        if (type === 'journalier') {
            setDateFrom(now)
            setDateTo(now)
        } else if (type === 'mensuel') {
            setDateFrom(startOfMonth(now))
            setDateTo(endOfMonth(now))
        }
    }

    // Prévisualiser le rapport via l'API
    const handlePreview = async () => {
        setLoading(true)
        setPreview(null)

        try {
            const request = {
                type: reportType,
                from: startOfDay(dateFrom).toISOString(),
                to: endOfDay(dateTo).toISOString(),
                hammamIds: selectedHammams.length > 0 ? selectedHammams : null,
                employeIds: selectedEmployes.length > 0 ? selectedEmployes : null,
            }

            const data = await rapportsService.preview(request)
            setPreview(data)
            toast.success('Rapport généré avec succès')
        } catch (error) {
            console.error('Erreur génération rapport:', error)
            toast.error('Erreur lors de la génération du rapport')
        } finally {
            setLoading(false)
        }
    }

    // Télécharger le rapport
    const handleDownload = async (downloadFormat) => {
        setDownloading(true)
        try {
            const request = {
                type: reportType,
                from: startOfDay(dateFrom).toISOString(),
                to: endOfDay(dateTo).toISOString(),
                hammamIds: selectedHammams.length > 0 ? selectedHammams : null,
                employeIds: selectedEmployes.length > 0 ? selectedEmployes : null,
            }

            if (downloadFormat === 'excel') {
                await rapportsService.downloadExcel(request)
            } else if (downloadFormat === 'pdf') {
                await rapportsService.downloadPdf(request)
            } else {
                await rapportsService.downloadCsv(request)
            }

            toast.success(`Rapport ${downloadFormat.toUpperCase()} téléchargé`)
        } catch (error) {
            console.error('Erreur téléchargement:', error)
            toast.error('Erreur de téléchargement')
        } finally {
            setDownloading(false)
        }
    }

    if (dataLoading) {
        return (
            <div className="flex items-center justify-center h-64">
                <LoadingSpinner size="lg" />
            </div>
        )
    }

    return (
        <div className="space-y-6 animate-fade-in">
            {/* Header */}
            <div>
                <h1 className="text-2xl font-bold text-white flex items-center gap-2">
                    <FileBarChart className="w-7 h-7 text-success-400" />
                    Génération de Rapports
                </h1>
                <p className="text-slate-400">
                    Générez des rapports personnalisés en Excel, PDF ou CSV
                </p>
            </div>

            {/* Formulaire */}
            <div className="glass-card p-6">
                <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
                    {/* Type de rapport */}
                    <div>
                        <label className="block text-sm font-medium text-slate-300 mb-3">
                            Type de rapport
                        </label>
                        <div className="flex gap-2">
                            {[
                                { value: 'journalier', label: 'Journalier' },
                                { value: 'mensuel', label: 'Mensuel' },
                                { value: 'personnalise', label: 'Personnalisé' },
                            ].map((type) => (
                                <button
                                    key={type.value}
                                    onClick={() => handleReportTypeChange(type.value)}
                                    className={`px-4 py-2 rounded-lg font-medium transition-all ${reportType === type.value
                                        ? 'bg-gradient-to-r from-primary-500 to-accent-500 text-white'
                                        : 'bg-slate-700 text-slate-300 hover:bg-slate-600'
                                        }`}
                                >
                                    {type.label}
                                </button>
                            ))}
                        </div>
                    </div>

                    {/* Dates */}
                    <div className="grid grid-cols-2 gap-4">
                        <div>
                            <label className="block text-sm font-medium text-slate-300 mb-2">
                                <Calendar className="w-4 h-4 inline mr-1" />
                                Date début
                            </label>
                            <DatePicker
                                selected={dateFrom}
                                onChange={(date) => setDateFrom(date)}
                                dateFormat="dd/MM/yyyy"
                                locale={fr}
                                className="input-field w-full"
                                disabled={reportType === 'journalier'}
                            />
                        </div>
                        <div>
                            <label className="block text-sm font-medium text-slate-300 mb-2">
                                <Calendar className="w-4 h-4 inline mr-1" />
                                Date fin
                            </label>
                            <DatePicker
                                selected={dateTo}
                                onChange={(date) => setDateTo(date)}
                                dateFormat="dd/MM/yyyy"
                                locale={fr}
                                className="input-field w-full"
                                disabled={reportType === 'journalier'}
                            />
                        </div>
                    </div>

                    {/* Filtres hammams */}
                    <div>
                        <label className="block text-sm font-medium text-slate-300 mb-2">
                            <Building2 className="w-4 h-4 inline mr-1" />
                            Hammams (optionnel)
                        </label>
                        <select
                            multiple
                            value={selectedHammams}
                            onChange={(e) => setSelectedHammams(Array.from(e.target.selectedOptions, o => o.value))}
                            className="input-field h-24"
                        >
                            {hammams.map(h => (
                                <option key={h.id} value={h.id}>{h.nom}</option>
                            ))}
                        </select>
                        <p className="text-xs text-slate-500 mt-1">Ctrl+click pour sélectionner plusieurs</p>
                    </div>

                    {/* Filtres employés */}
                    <div>
                        <label className="block text-sm font-medium text-slate-300 mb-2">
                            <Users className="w-4 h-4 inline mr-1" />
                            Employés (optionnel)
                        </label>
                        <select
                            multiple
                            value={selectedEmployes}
                            onChange={(e) => setSelectedEmployes(Array.from(e.target.selectedOptions, o => o.value))}
                            className="input-field h-24"
                        >
                            {employes.map(e => (
                                <option key={e.id} value={e.id}>{e.nom}</option>
                            ))}
                        </select>
                        <p className="text-xs text-slate-500 mt-1">Laissez vide pour tout inclure</p>
                    </div>
                </div>

                {/* Bouton prévisualiser */}
                <div className="mt-6 flex justify-center">
                    <button
                        onClick={handlePreview}
                        disabled={loading}
                        className="btn-primary flex items-center gap-2 px-8"
                    >
                        {loading ? (
                            <Loader2 className="w-5 h-5 animate-spin" />
                        ) : (
                            <Eye className="w-5 h-5" />
                        )}
                        Prévisualiser le rapport
                    </button>
                </div>
            </div>

            {/* Prévisualisation */}
            {preview && (
                <div className="glass-card p-6 animate-slide-up">
                    <div className="flex items-center justify-between mb-6">
                        <div>
                            <h2 className="text-xl font-semibold text-white">Aperçu du rapport</h2>
                            <p className="text-sm text-slate-400">
                                Du {format(new Date(preview.periodeDebut || dateFrom), 'dd/MM/yyyy', { locale: fr })} au {format(new Date(preview.periodeFin || dateTo), 'dd/MM/yyyy', { locale: fr })}
                            </p>
                        </div>
                        <div className="flex gap-2">
                            <button
                                onClick={() => handleDownload('excel')}
                                disabled={downloading}
                                className="btn-secondary flex items-center gap-2"
                                title="Télécharger au format CSV (ouvrable avec Excel)"
                            >
                                {downloading ? <Loader2 className="w-4 h-4 animate-spin" /> : <FileSpreadsheet className="w-4 h-4" />}
                                Excel (CSV)
                            </button>
                            <button
                                onClick={() => handleDownload('pdf')}
                                disabled={downloading}
                                className="btn-secondary flex items-center gap-2"
                                title="Télécharger en format texte formaté"
                            >
                                <FileText className="w-4 h-4" />
                                Rapport TXT
                            </button>
                            <button
                                onClick={() => handleDownload('csv')}
                                disabled={downloading}
                                className="btn-secondary flex items-center gap-2"
                            >
                                <Download className="w-4 h-4" />
                                CSV
                            </button>
                        </div>
                    </div>

                    {/* Résumé */}
                    <div className="grid grid-cols-2 gap-4 mb-6">
                        <div className="bg-slate-800/50 rounded-xl p-4 text-center">
                            <p className="text-slate-400 text-sm">Total Tickets</p>
                            <p className="text-3xl font-bold text-white">{(preview.totalTickets || 0).toLocaleString()}</p>
                        </div>
                        <div className="bg-slate-800/50 rounded-xl p-4 text-center">
                            <p className="text-slate-400 text-sm">Total Revenus</p>
                            <p className="text-3xl font-bold text-success-400">{(preview.totalRevenue || 0).toLocaleString()} DH</p>
                        </div>
                    </div>

                    {/* Message si pas de données */}
                    {preview.totalTickets === 0 && (
                        <div className="text-center py-8 text-slate-400">
                            <FileBarChart className="w-12 h-12 mx-auto mb-4 opacity-50" />
                            <p>Aucune donnée pour la période sélectionnée</p>
                        </div>
                    )}

                    {/* Tableaux */}
                    {preview.totalTickets > 0 && (
                        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
                            {/* Par Hammam */}
                            <div>
                                <h3 className="text-lg font-medium text-white mb-3 flex items-center gap-2">
                                    <Building2 className="w-5 h-5 text-primary-400" />
                                    Par Hammam
                                </h3>
                                <div className="table-container">
                                    <table className="custom-table text-sm">
                                        <thead>
                                            <tr>
                                                <th>Hammam</th>
                                                <th className="text-right">Tickets</th>
                                                <th className="text-right">Revenus</th>
                                                <th className="text-right">%</th>
                                            </tr>
                                        </thead>
                                        <tbody>
                                            {preview.lignesParHammam?.map((ligne, i) => (
                                                <tr key={i}>
                                                    <td className="font-medium text-white">{ligne.label}</td>
                                                    <td className="text-right">{ligne.ticketsCount}</td>
                                                    <td className="text-right">{ligne.revenue} DH</td>
                                                    <td className="text-right">
                                                        <div className="flex items-center justify-end gap-2">
                                                            <div className="w-16 h-2 bg-slate-700 rounded-full overflow-hidden">
                                                                <div
                                                                    className="h-full bg-gradient-to-r from-primary-500 to-accent-500 rounded-full"
                                                                    style={{ width: `${Math.min(ligne.pourcentage * 2, 100)}%` }}
                                                                />
                                                            </div>
                                                            <span className="text-slate-400">{ligne.pourcentage}%</span>
                                                        </div>
                                                    </td>
                                                </tr>
                                            ))}
                                        </tbody>
                                    </table>
                                </div>
                            </div>

                            {/* Par Type */}
                            <div>
                                <h3 className="text-lg font-medium text-white mb-3 flex items-center gap-2">
                                    <Filter className="w-5 h-5 text-accent-400" />
                                    Par Type de Ticket
                                </h3>
                                <div className="table-container">
                                    <table className="custom-table text-sm">
                                        <thead>
                                            <tr>
                                                <th>Type</th>
                                                <th className="text-right">Tickets</th>
                                                <th className="text-right">Revenus</th>
                                                <th className="text-right">%</th>
                                            </tr>
                                        </thead>
                                        <tbody>
                                            {preview.lignesParType?.map((ligne, i) => (
                                                <tr key={i}>
                                                    <td>
                                                        <span className={`badge ${ligne.label === 'HOMME' ? 'badge-primary' :
                                                            ligne.label === 'FEMME' ? 'bg-pink-500/20 text-pink-400' :
                                                                ligne.label === 'ENFANT' ? 'badge-success' :
                                                                    'bg-cyan-500/20 text-cyan-400'
                                                            }`}>
                                                            {ligne.label}
                                                        </span>
                                                    </td>
                                                    <td className="text-right">{ligne.ticketsCount}</td>
                                                    <td className="text-right">{ligne.revenue} DH</td>
                                                    <td className="text-right text-slate-400">{ligne.pourcentage}%</td>
                                                </tr>
                                            ))}
                                        </tbody>
                                    </table>
                                </div>
                            </div>

                            {/* Par Employé */}
                            {preview.lignesParEmploye?.length > 0 && (
                                <div className="lg:col-span-2">
                                    <h3 className="text-lg font-medium text-white mb-3 flex items-center gap-2">
                                        <Users className="w-5 h-5 text-success-400" />
                                        Par Employé (Top 10)
                                    </h3>
                                    <div className="table-container">
                                        <table className="custom-table text-sm">
                                            <thead>
                                                <tr>
                                                    <th>#</th>
                                                    <th>Employé</th>
                                                    <th className="text-right">Tickets</th>
                                                    <th className="text-right">Revenus</th>
                                                    <th className="text-right">%</th>
                                                </tr>
                                            </thead>
                                            <tbody>
                                                {preview.lignesParEmploye.slice(0, 10).map((ligne, i) => (
                                                    <tr key={i}>
                                                        <td>
                                                            <span className={`inline-flex items-center justify-center w-6 h-6 rounded-full text-xs font-bold ${i === 0 ? 'bg-yellow-500/20 text-yellow-400' :
                                                                i === 1 ? 'bg-slate-400/20 text-slate-300' :
                                                                    i === 2 ? 'bg-amber-700/20 text-amber-600' :
                                                                        'bg-slate-700 text-slate-400'
                                                                }`}>
                                                                {i + 1}
                                                            </span>
                                                        </td>
                                                        <td className="font-medium text-white">{ligne.label}</td>
                                                        <td className="text-right">{ligne.ticketsCount}</td>
                                                        <td className="text-right text-success-400 font-medium">{ligne.revenue} DH</td>
                                                        <td className="text-right text-slate-400">{ligne.pourcentage}%</td>
                                                    </tr>
                                                ))}
                                            </tbody>
                                        </table>
                                    </div>
                                </div>
                            )}

                            {/* Evolution par jour (si disponible) */}
                            {preview.lignesParJour?.length > 0 && (
                                <div className="lg:col-span-2">
                                    <h3 className="text-lg font-medium text-white mb-3 flex items-center gap-2">
                                        <TrendingUp className="w-5 h-5 text-primary-400" />
                                        Evolution par jour
                                    </h3>
                                    <div className="table-container max-h-64 overflow-y-auto">
                                        <table className="custom-table text-sm">
                                            <thead className="sticky top-0 bg-slate-800">
                                                <tr>
                                                    <th>Date</th>
                                                    <th className="text-right">Tickets</th>
                                                    <th className="text-right">Revenus</th>
                                                </tr>
                                            </thead>
                                            <tbody>
                                                {preview.lignesParJour.map((ligne, i) => (
                                                    <tr key={i}>
                                                        <td className="font-medium text-white">
                                                            {format(new Date(ligne.date), 'EEEE dd/MM', { locale: fr })}
                                                        </td>
                                                        <td className="text-right">{ligne.ticketsCount}</td>
                                                        <td className="text-right text-success-400">{ligne.revenue} DH</td>
                                                    </tr>
                                                ))}
                                            </tbody>
                                        </table>
                                    </div>
                                </div>
                            )}
                        </div>
                    )}
                </div>
            )}
        </div>
    )
}
