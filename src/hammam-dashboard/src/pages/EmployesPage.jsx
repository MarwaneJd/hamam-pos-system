import { useState, useEffect } from 'react'
import { employesService, hammamsService } from '../services/api'
import { toast } from 'react-toastify'
import {
    Users,
    Plus,
    Edit,
    Key,
    UserX,
    UserCheck,
    Search,
    X,
    Copy,
    Check,
    Loader2,
} from 'lucide-react'
import LoadingSpinner from '../components/LoadingSpinner'

/**
 * Page de gestion des employés
 */
export default function EmployesPage() {
    const [employes, setEmployes] = useState([])
    const [hammams, setHammams] = useState([])
    const [loading, setLoading] = useState(true)
    const [search, setSearch] = useState('')
    const [selectedHammam, setSelectedHammam] = useState('')
    const [showModal, setShowModal] = useState(false)
    const [modalType, setModalType] = useState('create') // create, edit, password
    const [selectedEmploye, setSelectedEmploye] = useState(null)
    const [newPassword, setNewPassword] = useState('')
    const [copied, setCopied] = useState(false)
    const [saving, setSaving] = useState(false)

    // Charger les données
    useEffect(() => {
        loadData()
    }, [])

    const loadData = async () => {
        try {
            // Charger depuis l'API
            const [employesData, hammamsData] = await Promise.all([
                employesService.getAll(),
                hammamsService.getAll()
            ])

            // Mapper les données pour le format attendu
            setEmployes(employesData.map(e => ({
                id: e.id,
                username: e.username,
                nom: e.nom,
                prenom: e.prenom,
                hammamId: e.hammamId,
                hammamNom: e.hammamNom,
                langue: e.langue?.toUpperCase() === 'AR' ? 'AR' : 'FR',
                role: e.role,
                actif: e.isActif
            })))

            setHammams(hammamsData.map(h => ({
                id: h.id,
                nom: h.nom
            })))
        } catch (error) {
            console.error('Erreur:', error)
            toast.error('Erreur de chargement des données')
        } finally {
            setLoading(false)
        }
    }

    // Filtrer les employés
    const filteredEmployes = employes.filter((e) => {
        const matchSearch =
            e.nom.toLowerCase().includes(search.toLowerCase()) ||
            e.prenom.toLowerCase().includes(search.toLowerCase()) ||
            e.username.toLowerCase().includes(search.toLowerCase())
        const matchHammam = !selectedHammam || e.hammamId === selectedHammam
        return matchSearch && matchHammam
    })

    // Générer un mot de passe aléatoire
    const generatePassword = () => {
        const chars = 'ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789!@#$%'
        let password = ''
        for (let i = 0; i < 10; i++) {
            password += chars.charAt(Math.floor(Math.random() * chars.length))
        }
        return password
    }

    // Ouvrir modal création
    const openCreateModal = () => {
        setModalType('create')
        setSelectedEmploye({
            nom: '',
            prenom: '',
            username: '',
            hammamId: hammams[0]?.id || '',
            langue: 'FR',
            password: generatePassword(),
        })
        setShowModal(true)
    }

    // Ouvrir modal modification
    const openEditModal = (employe) => {
        setModalType('edit')
        setSelectedEmploye({ ...employe })
        setShowModal(true)
    }

    // Ouvrir modal reset password
    const openPasswordModal = (employe) => {
        setModalType('password')
        setSelectedEmploye(employe)
        setNewPassword(generatePassword())
        setShowModal(true)
    }

    // Sauvegarder
    const handleSave = async () => {
        setSaving(true)
        try {
            if (modalType === 'create') {
                // Appel API pour créer l'employé
                const createData = {
                    nom: selectedEmploye.nom,
                    prenom: selectedEmploye.prenom,
                    username: selectedEmploye.username,
                    password: selectedEmploye.password,
                    hammamId: selectedEmploye.hammamId,
                    role: 'Employe',
                    langue: selectedEmploye.langue || 'FR'
                }
                const newEmploye = await employesService.create(createData)

                // Ajouter à l'état local
                setEmployes([...employes, {
                    id: newEmploye.id,
                    username: newEmploye.username,
                    nom: newEmploye.nom,
                    prenom: newEmploye.prenom,
                    hammamId: newEmploye.hammamId,
                    hammamNom: newEmploye.hammamNom || hammams.find(h => h.id === newEmploye.hammamId?.toString())?.nom,
                    langue: newEmploye.langue?.toUpperCase() === 'AR' ? 'AR' : 'FR',
                    role: newEmploye.role,
                    actif: newEmploye.isActif
                }])
                toast.success('Employé créé avec succès')

            } else if (modalType === 'edit') {
                // Appel API pour modifier l'employé
                const updateData = {
                    nom: selectedEmploye.nom,
                    prenom: selectedEmploye.prenom,
                    hammamId: selectedEmploye.hammamId,
                    langue: selectedEmploye.langue
                }
                await employesService.update(selectedEmploye.id, updateData)

                // Mettre à jour l'état local
                setEmployes(employes.map(e =>
                    e.id === selectedEmploye.id
                        ? { ...selectedEmploye, hammamNom: hammams.find(h => h.id === selectedEmploye.hammamId?.toString())?.nom }
                        : e
                ))
                toast.success('Employé modifié avec succès')

            } else if (modalType === 'password') {
                // Appel API pour reset password
                await employesService.resetPassword(selectedEmploye.id, newPassword)
                toast.success('Mot de passe réinitialisé')
            }
            setShowModal(false)
        } catch (error) {
            console.error('Erreur sauvegarde:', error)
            toast.error(error.response?.data?.message || 'Erreur lors de l\'enregistrement')
        } finally {
            setSaving(false)
        }
    }

    // Toggle actif
    const toggleActif = async (employe) => {
        try {
            // Appel API pour toggle le statut
            await employesService.toggleStatus(employe.id)

            // Mettre à jour l'état local
            setEmployes(employes.map(e =>
                e.id === employe.id ? { ...e, actif: !e.actif } : e
            ))
            toast.success(employe.actif ? 'Employé désactivé' : 'Employé activé')
        } catch (error) {
            console.error('Erreur toggle status:', error)
            toast.error('Erreur lors du changement de statut')
        }
    }

    // Copier dans le presse-papier
    const copyToClipboard = (text) => {
        navigator.clipboard.writeText(text)
        setCopied(true)
        setTimeout(() => setCopied(false), 2000)
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
                        <Users className="w-7 h-7 text-primary-400" />
                        Gestion des Employés
                    </h1>
                    <p className="text-slate-400">
                        {employes.length} employés • {employes.filter(e => e.actif).length} actifs
                    </p>
                </div>
                <button onClick={openCreateModal} className="btn-primary flex items-center gap-2">
                    <Plus className="w-5 h-5" />
                    Nouvel employé
                </button>
            </div>

            {/* Filtres */}
            <div className="glass-card p-4 flex flex-col sm:flex-row gap-4">
                <div className="flex-1 relative">
                    <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 text-slate-400" />
                    <input
                        type="text"
                        placeholder="Rechercher un employé..."
                        value={search}
                        onChange={(e) => setSearch(e.target.value)}
                        className="input-field pl-10"
                    />
                </div>
                <select
                    value={selectedHammam}
                    onChange={(e) => setSelectedHammam(e.target.value)}
                    className="input-field w-full sm:w-48"
                >
                    <option value="">Tous les hammams</option>
                    {hammams.map(h => (
                        <option key={h.id} value={h.id}>{h.nom}</option>
                    ))}
                </select>
            </div>

            {/* Table */}
            <div className="glass-card overflow-hidden">
                <div className="table-container">
                    <table className="custom-table">
                        <thead>
                            <tr>
                                <th>Employé</th>
                                <th className="hidden sm:table-cell">Username</th>
                                <th>Hammam</th>
                                <th className="hidden md:table-cell">Langue</th>
                                <th>Statut</th>
                                <th className="text-right">Actions</th>
                            </tr>
                        </thead>
                        <tbody>
                            {filteredEmployes.map((employe) => (
                                <tr key={employe.id}>
                                    <td>
                                        <div className="flex items-center gap-2 sm:gap-3">
                                            <div className="w-8 h-8 sm:w-10 sm:h-10 bg-gradient-to-br from-primary-500 to-accent-500 rounded-full flex items-center justify-center text-white font-bold text-xs sm:text-sm flex-shrink-0">
                                                {employe.prenom[0]}{employe.nom[0]}
                                            </div>
                                            <div className="min-w-0">
                                                <p className="font-medium text-white text-sm truncate">{employe.prenom} {employe.nom}</p>
                                                <p className="text-xs text-slate-400 sm:hidden">{employe.username}</p>
                                            </div>
                                        </div>
                                    </td>
                                    <td className="font-mono text-sm hidden sm:table-cell">{employe.username}</td>
                                    <td className="text-sm">{employe.hammamNom}</td>
                                    <td className="hidden md:table-cell">
                                        <span className="badge badge-primary">
                                            {employe.langue === 'FR' ? '🇫🇷 Français' : '🇲🇦 Arabe'}
                                        </span>
                                    </td>
                                    <td>
                                        <span className={`badge ${employe.actif ? 'badge-success' : 'badge-danger'}`}>
                                            {employe.actif ? 'Actif' : 'Inactif'}
                                        </span>
                                    </td>
                                    <td>
                                        <div className="flex items-center justify-end gap-2">
                                            <button
                                                onClick={() => openEditModal(employe)}
                                                className="p-2 text-slate-400 hover:text-primary-400 hover:bg-primary-500/10 rounded-lg transition-colors"
                                                title="Modifier"
                                            >
                                                <Edit className="w-4 h-4" />
                                            </button>
                                            <button
                                                onClick={() => openPasswordModal(employe)}
                                                className="p-2 text-slate-400 hover:text-warning-400 hover:bg-warning-500/10 rounded-lg transition-colors"
                                                title="Reset mot de passe"
                                            >
                                                <Key className="w-4 h-4" />
                                            </button>
                                            <button
                                                onClick={() => toggleActif(employe)}
                                                className={`p-2 rounded-lg transition-colors ${employe.actif
                                                    ? 'text-slate-400 hover:text-danger-400 hover:bg-danger-500/10'
                                                    : 'text-slate-400 hover:text-success-400 hover:bg-success-500/10'
                                                    }`}
                                                title={employe.actif ? 'Désactiver' : 'Activer'}
                                            >
                                                {employe.actif ? <UserX className="w-4 h-4" /> : <UserCheck className="w-4 h-4" />}
                                            </button>
                                        </div>
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>
            </div>

            {/* Modal */}
            {showModal && (
                <div className="modal-overlay">
                    <div className="modal-content" onClick={(e) => e.stopPropagation()}>
                        <div className="flex items-center justify-between mb-6">
                            <h3 className="text-xl font-semibold text-white">
                                {modalType === 'create' && 'Nouvel employé'}
                                {modalType === 'edit' && 'Modifier employé'}
                                {modalType === 'password' && 'Reset mot de passe'}
                            </h3>
                            <button
                                onClick={() => setShowModal(false)}
                                className="p-2 text-slate-400 hover:text-white rounded-lg transition-colors"
                            >
                                <X className="w-5 h-5" />
                            </button>
                        </div>

                        {modalType === 'password' ? (
                            <div className="space-y-4">
                                <p className="text-slate-300">
                                    Nouveau mot de passe pour <strong>{selectedEmploye?.prenom} {selectedEmploye?.nom}</strong>
                                </p>
                                <div className="flex items-center gap-2">
                                    <input
                                        type="text"
                                        value={newPassword}
                                        readOnly
                                        className="input-field flex-1 font-mono"
                                    />
                                    <button
                                        onClick={() => copyToClipboard(newPassword)}
                                        className="btn-secondary flex items-center gap-2"
                                    >
                                        {copied ? <Check className="w-4 h-4" /> : <Copy className="w-4 h-4" />}
                                        {copied ? 'Copié !' : 'Copier'}
                                    </button>
                                </div>
                                <p className="text-sm text-slate-400">
                                    ⚠️ Envoyez ce mot de passe à l'employé de manière sécurisée (WhatsApp, SMS...)
                                </p>
                            </div>
                        ) : (
                            <div className="space-y-4">
                                <div className="grid grid-cols-2 gap-4">
                                    <div>
                                        <label className="block text-sm font-medium text-slate-300 mb-2">Prénom</label>
                                        <input
                                            type="text"
                                            value={selectedEmploye?.prenom || ''}
                                            onChange={(e) => setSelectedEmploye({ ...selectedEmploye, prenom: e.target.value })}
                                            className="input-field"
                                        />
                                    </div>
                                    <div>
                                        <label className="block text-sm font-medium text-slate-300 mb-2">Nom</label>
                                        <input
                                            type="text"
                                            value={selectedEmploye?.nom || ''}
                                            onChange={(e) => setSelectedEmploye({ ...selectedEmploye, nom: e.target.value })}
                                            className="input-field"
                                        />
                                    </div>
                                </div>

                                {modalType === 'create' && (
                                    <>
                                        <div>
                                            <label className="block text-sm font-medium text-slate-300 mb-2">Username</label>
                                            <input
                                                type="text"
                                                value={selectedEmploye?.username || ''}
                                                onChange={(e) => setSelectedEmploye({ ...selectedEmploye, username: e.target.value })}
                                                className="input-field"
                                            />
                                        </div>
                                        <div>
                                            <label className="block text-sm font-medium text-slate-300 mb-2">Mot de passe</label>
                                            <div className="flex gap-2">
                                                <input
                                                    type="text"
                                                    value={selectedEmploye?.password || ''}
                                                    readOnly
                                                    className="input-field flex-1 font-mono"
                                                />
                                                <button
                                                    onClick={() => copyToClipboard(selectedEmploye?.password)}
                                                    className="btn-secondary"
                                                >
                                                    <Copy className="w-4 h-4" />
                                                </button>
                                            </div>
                                        </div>
                                    </>
                                )}

                                <div>
                                    <label className="block text-sm font-medium text-slate-300 mb-2">Hammam</label>
                                    <select
                                        value={selectedEmploye?.hammamId || ''}
                                        onChange={(e) => setSelectedEmploye({ ...selectedEmploye, hammamId: e.target.value })}
                                        className="input-field"
                                    >
                                        {hammams.map(h => (
                                            <option key={h.id} value={h.id}>{h.nom}</option>
                                        ))}
                                    </select>
                                </div>

                                <div>
                                    <label className="block text-sm font-medium text-slate-300 mb-2">Langue</label>
                                    <select
                                        value={selectedEmploye?.langue || 'FR'}
                                        onChange={(e) => setSelectedEmploye({ ...selectedEmploye, langue: e.target.value })}
                                        className="input-field"
                                    >
                                        <option value="FR">🇫🇷 Français</option>
                                        <option value="AR">🇲🇦 Arabe</option>
                                    </select>
                                </div>
                            </div>
                        )}

                        <div className="flex justify-end gap-3 mt-6">
                            <button onClick={() => setShowModal(false)} className="btn-secondary">
                                Annuler
                            </button>
                            <button onClick={handleSave} disabled={saving} className="btn-primary flex items-center gap-2">
                                {saving && <Loader2 className="w-4 h-4 animate-spin" />}
                                {modalType === 'password' ? 'Confirmer' : 'Enregistrer'}
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    )
}
