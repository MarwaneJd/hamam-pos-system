import { useState, useEffect } from 'react'
import { toast } from 'react-toastify'
import { hammamsService, employesService, typeTicketsService } from '../services/api'
import {
    Building2,
    Plus,
    Edit,
    MapPin,
    Users,
    Ticket,
    X,
    Loader2,
    RefreshCw,
    User,
    Baby,
    Droplets,
    DollarSign,
    Trash2,
    Eye,
    EyeOff,
    Key,
    Save,
    ToggleLeft,
    ToggleRight,
    Lock,
    Shield,
    Delete,
    Upload,
    Image,
} from 'lucide-react'
import LoadingSpinner from '../components/LoadingSpinner'

/**
 * Page de gestion des Hammams avec connexion au backend réel
 */
export default function HammamsPage() {
    const [hammams, setHammams] = useState([])
    const [loading, setLoading] = useState(true)
    const [refreshing, setRefreshing] = useState(false)
    const [showModal, setShowModal] = useState(false)
    const [modalType, setModalType] = useState('create')
    const [selectedHammam, setSelectedHammam] = useState(null)
    const [saving, setSaving] = useState(false)

    // État pour le formulaire de création avec employés et types de tickets
    const [newEmployes, setNewEmployes] = useState([])
    const [newTypeTickets, setNewTypeTickets] = useState([])

    // État pour le mode édition - données existantes
    const [existingEmployes, setExistingEmployes] = useState([])
    const [existingTypeTickets, setExistingTypeTickets] = useState([])
    const [loadingDetails, setLoadingDetails] = useState(false)
    const [showPasswords, setShowPasswords] = useState({})

    const [editingProduct, setEditingProduct] = useState(null)
    const [editingEmploye, setEditingEmploye] = useState(null)
    const [savingItem, setSavingItem] = useState(null)

    // État pour le modal PIN personnalisé
    const [pinModal, setPinModal] = useState({ show: false, type: null, employe: null })
    const [pinValue, setPinValue] = useState('')
    const [pinError, setPinError] = useState('')
    const [showPinValue, setShowPinValue] = useState(false)
    const [pinLoading, setPinLoading] = useState(false)

    useEffect(() => {
        loadData()
    }, [])

    const loadData = async (showRefresh = false) => {
        if (showRefresh) setRefreshing(true)
        else setLoading(true)

        try {
            const data = await hammamsService.getAll()
            setHammams(data)
        } catch (error) {
            console.error('Erreur de chargement:', error)
            toast.error('Erreur de chargement des hammams')
        } finally {
            setLoading(false)
            setRefreshing(false)
        }
    }

    const openCreateModal = () => {
        setModalType('create')
        setSelectedHammam({ code: '', nom: '', nomArabe: '', adresse: '' })
        setNewEmployes([])
        setNewTypeTickets([
            { nom: 'HOMME', prix: 15, couleur: '#3B82F6', icone: 'User', ordre: 1 },
            { nom: 'FEMME', prix: 15, couleur: '#EC4899', icone: 'UserCheck', ordre: 2 },
            { nom: 'ENFANT', prix: 10, couleur: '#10B981', icone: 'Baby', ordre: 3 },
            { nom: 'DOUCHE', prix: 8, couleur: '#06B6D4', icone: 'Droplets', ordre: 4 },
        ])
        setShowModal(true)
    }

    const openEditModal = async (hammam) => {
        setModalType('edit')
        setSelectedHammam({ ...hammam })
        setNewEmployes([])
        setNewTypeTickets([])
        setExistingEmployes([])
        setExistingTypeTickets([])
        setShowPasswords({})
        setShowModal(true)

        // Charger les détails du hammam (employés et types de tickets)
        setLoadingDetails(true)
        try {
            const [employes, typeTickets] = await Promise.all([
                employesService.getByHammam(hammam.id),
                typeTicketsService.getByHammam(hammam.id)
            ])
            setExistingEmployes(employes || [])
            setExistingTypeTickets(typeTickets || [])
        } catch (error) {
            console.error('Erreur chargement détails:', error)
            toast.error('Erreur lors du chargement des détails')
        } finally {
            setLoadingDetails(false)
        }
    }

    const togglePasswordVisibility = (employeId) => {
        setShowPasswords(prev => ({
            ...prev,
            [employeId]: !prev[employeId]
        }))
    }

    // ======= Fonctions pour les produits existants (mode édition) =======
    const updateExistingProduct = async (product) => {
        setSavingItem(`product-${product.id}`)
        try {
            await typeTicketsService.update(product.id, {
                nom: product.nom,
                prix: product.prix,
                couleur: product.couleur,
                icone: product.icone,
                ordre: product.ordre
            })
            toast.success(`Produit "${product.nom}" mis à jour`)
            setEditingProduct(null)
        } catch (error) {
            console.error('Erreur mise à jour produit:', error)
            toast.error('Erreur lors de la mise à jour')
        } finally {
            setSavingItem(null)
        }
    }

    const toggleProductStatus = async (product) => {
        setSavingItem(`product-${product.id}`)
        try {
            await typeTicketsService.toggleStatus(product.id)
            // Mettre à jour l'état local
            setExistingTypeTickets(prev => prev.map(p =>
                p.id === product.id ? { ...p, isActif: !p.isActif } : p
            ))
            toast.success(product.isActif ? 'Produit désactivé' : 'Produit activé')
        } catch (error) {
            toast.error('Erreur lors du changement de statut')
        } finally {
            setSavingItem(null)
        }
    }

    const deleteExistingProduct = async (product) => {
        if (!window.confirm(`Supprimer le produit "${product.nom}" ?`)) return

        setSavingItem(`product-${product.id}`)
        try {
            await typeTicketsService.delete(product.id)
            setExistingTypeTickets(prev => prev.filter(p => p.id !== product.id))
            toast.success('Produit supprimé')
        } catch (error) {
            toast.error('Impossible de supprimer ce produit')
        } finally {
            setSavingItem(null)
        }
    }

    const handleProductImageUpload = async (productId, file) => {
        if (!file) return
        setSavingItem(`product-${productId}`)
        try {
            const updated = await typeTicketsService.uploadImage(productId, file)
            setExistingTypeTickets(prev => prev.map(p =>
                p.id === productId ? { ...p, imageUrl: updated.imageUrl } : p
            ))
            toast.success('Image mise à jour')
        } catch (error) {
            toast.error(error.response?.data || 'Erreur lors de l\'upload')
        } finally {
            setSavingItem(null)
        }
    }

    const handleDeleteProductImage = async (productId) => {
        setSavingItem(`product-${productId}`)
        try {
            await typeTicketsService.deleteImage(productId)
            setExistingTypeTickets(prev => prev.map(p =>
                p.id === productId ? { ...p, imageUrl: null } : p
            ))
            toast.success('Image supprimée')
        } catch (error) {
            toast.error('Erreur lors de la suppression')
        } finally {
            setSavingItem(null)
        }
    }

    const addNewProductToHammam = async () => {
        if (!selectedHammam?.id) return

        const newProduct = {
            nom: 'Nouveau produit',
            prix: 10,
            couleur: '#6366F1',
            icone: 'User',
            ordre: existingTypeTickets.length + 1,
            hammamId: selectedHammam.id
        }

        setSavingItem('new-product')
        try {
            const created = await typeTicketsService.create(newProduct)
            setExistingTypeTickets(prev => [...prev, created])
            setEditingProduct(created.id)
            toast.success('Nouveau produit ajouté')
        } catch (error) {
            toast.error('Erreur lors de la création du produit')
        } finally {
            setSavingItem(null)
        }
    }

    // ======= Fonctions pour les employés existants (mode édition) =======
    const updateExistingEmploye = async (employe) => {
        setSavingItem(`employe-${employe.id}`)
        try {
            await employesService.update(employe.id, {
                nom: employe.nom,
                prenom: employe.prenom,
                username: employe.username,
                langue: employe.langue,
                role: employe.role
            })
            toast.success(`Employé "${employe.prenom} ${employe.nom}" mis à jour`)
            setEditingEmploye(null)
        } catch (error) {
            console.error('Erreur mise à jour employé:', error)
            toast.error('Erreur lors de la mise à jour')
        } finally {
            setSavingItem(null)
        }
    }

    const resetEmployePassword = (employe) => {
        setPinModal({ show: true, type: 'reset', employe })
        setPinValue('')
        setPinError('')
        setShowPinValue(false)
    }

    const addNewEmployeToHammam = () => {
        if (!selectedHammam?.id) return

        // Max 2 employés par hammam
        const activeCount = existingEmployes.filter(e => e.isActif).length
        if (activeCount >= 2) {
            toast.error('Maximum 2 employés par hammam')
            return
        }

        setPinModal({ show: true, type: 'create', employe: null })
        setPinValue('')
        setPinError('')
        setShowPinValue(false)
    }

    const handlePinSubmit = async () => {
        if (!pinValue) {
            setPinError('Veuillez entrer un code PIN')
            return
        }
        if (!/^\d+$/.test(pinValue)) {
            setPinError('Le code PIN doit contenir uniquement des chiffres')
            return
        }
        if (pinValue.length < 3) {
            setPinError('Le code PIN doit contenir au moins 3 chiffres')
            return
        }

        setPinLoading(true)
        setPinError('')

        try {
            if (pinModal.type === 'reset') {
                // Réinitialiser le mot de passe
                const emp = pinModal.employe
                setSavingItem(`employe-${emp.id}`)
                await employesService.resetPassword(emp.id, pinValue)
                setExistingEmployes(prev => prev.map(e =>
                    e.id === emp.id ? { ...e, passwordClair: pinValue } : e
                ))
                toast.success('Code PIN réinitialisé avec succès')
                setSavingItem(null)
            } else if (pinModal.type === 'create') {
                // Créer un nouvel employé
                const activeCount = existingEmployes.filter(e => e.isActif).length
                const newEmploye = {
                    nom: '',
                    prenom: `Employé ${activeCount + 1}`,
                    password: pinValue,
                    langue: 'FR',
                    role: 'Employe',
                    hammamId: selectedHammam.id
                }

                setSavingItem('new-employe')
                const created = await employesService.create(newEmploye)
                created.passwordClair = pinValue
                setExistingEmployes(prev => [...prev, created])
                setEditingEmploye(created.id)
                toast.success('Nouvel employé ajouté')
                setSavingItem(null)
            }
            setPinModal({ show: false, type: null, employe: null })
        } catch (error) {
            const msg = error.response?.data?.message || 'Erreur lors de l\'opération'
            setPinError(msg)
        } finally {
            setPinLoading(false)
        }
    }

    const handlePinKeyDown = (e) => {
        if (e.key === 'Enter') {
            e.preventDefault()
            handlePinSubmit()
        }
    }

    const handlePinDigitClick = (digit) => {
        if (pinValue.length < 8) {
            setPinValue(prev => prev + digit)
            setPinError('')
        }
    }

    const handlePinBackspace = () => {
        setPinValue(prev => prev.slice(0, -1))
        setPinError('')
    }

    const toggleEmployeStatus = async (employe) => {
        setSavingItem(`employe-${employe.id}`)
        try {
            await employesService.toggleStatus(employe.id)
            setExistingEmployes(prev => prev.map(e =>
                e.id === employe.id ? { ...e, isActif: !e.isActif } : e
            ))
            toast.success(employe.isActif ? 'Employé désactivé' : 'Employé activé')
        } catch (error) {
            toast.error('Erreur lors du changement de statut')
        } finally {
            setSavingItem(null)
        }
    }

    const deleteExistingEmploye = async (employe) => {
        if (!window.confirm(`Supprimer l'employé "${employe.prenom} ${employe.nom}" ?`)) return

        setSavingItem(`employe-${employe.id}`)
        try {
            await employesService.delete(employe.id)
            setExistingEmployes(prev => prev.filter(e => e.id !== employe.id))
            toast.success('Employé supprimé')
        } catch (error) {
            toast.error('Impossible de supprimer cet employé')
        } finally {
            setSavingItem(null)
        }
    }

    const updateExistingProductField = (productId, field, value) => {
        setExistingTypeTickets(prev => prev.map(p =>
            p.id === productId ? { ...p, [field]: value } : p
        ))
    }

    const updateExistingEmployeField = (employeId, field, value) => {
        setExistingEmployes(prev => prev.map(e =>
            e.id === employeId ? { ...e, [field]: value } : e
        ))
    }

    const handleSave = async () => {
        if (!selectedHammam?.nom) {
            toast.error('Le nom est obligatoire')
            return
        }

        setSaving(true)
        try {
            if (modalType === 'create') {
                const payload = {
                    nom: selectedHammam.nom,
                    nomArabe: selectedHammam.nomArabe || undefined,
                    code: selectedHammam.code || undefined,
                    adresse: selectedHammam.adresse,
                    typeTickets: newTypeTickets.length > 0 ? newTypeTickets : undefined,
                    employes: newEmployes.length > 0 ? newEmployes : undefined,
                }
                await hammamsService.create(payload)
                toast.success('Hammam créé avec succès')
            } else {
                await hammamsService.update(selectedHammam.id, {
                    nom: selectedHammam.nom,
                    nomArabe: selectedHammam.nomArabe,
                    prefixeTicket: selectedHammam.prefixeTicket,
                    adresse: selectedHammam.adresse,
                })
                toast.success('Hammam modifié avec succès')
            }
            setShowModal(false)
            loadData(true)
        } catch (error) {
            console.error('Erreur:', error)
            toast.error(error.response?.data?.message || 'Erreur lors de la sauvegarde')
        } finally {
            setSaving(false)
        }
    }

    const toggleActif = async (hammam) => {
        try {
            await hammamsService.toggleStatus(hammam.id)
            toast.success(hammam.isActif ? 'Hammam désactivé' : 'Hammam activé')
            loadData(true)
        } catch (error) {
            toast.error('Erreur lors du changement de statut')
        }
    }

    const deleteHammam = async (hammam) => {
        if (!window.confirm(`Êtes-vous sûr de vouloir supprimer "${hammam.nom}" ?\n\nCette action est irréversible.`)) {
            return
        }
        try {
            await hammamsService.delete(hammam.id)
            toast.success('Hammam supprimé avec succès')
            loadData(true)
        } catch (error) {
            console.error('Erreur suppression:', error)
            toast.error(error.response?.data?.message || 'Impossible de supprimer ce hammam')
        }
    }

    // Ajouter un employé au formulaire
    const addEmploye = () => {
        if (newEmployes.length >= 2) {
            toast.error('Maximum 2 employés par hammam')
            return
        }
        setNewEmployes([...newEmployes, {
            password: '',
            nom: '',
            prenom: '',
            langue: 'FR',
            role: 'Employe'
        }])
    }

    const removeEmploye = (index) => {
        setNewEmployes(newEmployes.filter((_, i) => i !== index))
    }

    const updateEmploye = (index, field, value) => {
        const updated = [...newEmployes]
        updated[index][field] = value
        setNewEmployes(updated)
    }

    // Ajouter un type de ticket
    const addTypeTicket = () => {
        setNewTypeTickets([...newTypeTickets, {
            nom: '',
            prix: 0,
            couleur: '#3B82F6',
            icone: 'User',
            ordre: newTypeTickets.length + 1
        }])
    }

    const removeTypeTicket = (index) => {
        setNewTypeTickets(newTypeTickets.filter((_, i) => i !== index))
    }

    const updateTypeTicket = (index, field, value) => {
        const updated = [...newTypeTickets]
        updated[index][field] = value
        setNewTypeTickets(updated)
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
                        <Building2 className="w-7 h-7 text-accent-400" />
                        Gestion des Hammams
                    </h1>
                    <p className="text-slate-400">
                        {hammams.length} établissements • {hammams.filter(h => h.isActif).length} actifs
                    </p>
                </div>
                <div className="flex gap-2">
                    <button
                        onClick={() => loadData(true)}
                        disabled={refreshing}
                        className="p-3 bg-slate-800 rounded-xl text-slate-400 hover:text-white transition-colors"
                    >
                        <RefreshCw className={`w-5 h-5 ${refreshing ? 'animate-spin' : ''}`} />
                    </button>
                    <button onClick={openCreateModal} className="btn-primary flex items-center gap-2">
                        <Plus className="w-5 h-5" />
                        Nouveau Hammam
                    </button>
                </div>
            </div>

            {/* Grid de cartes */}
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                {hammams.map((hammam) => (
                    <div
                        key={hammam.id}
                        className={`glass-card glass-card-hover p-6 relative overflow-hidden ${!hammam.isActif ? 'opacity-60' : ''
                            }`}
                    >
                        {/* Badge statut */}
                        <div className="absolute top-4 right-4">
                            <span className={`badge ${hammam.isActif ? 'badge-success' : 'badge-danger'}`}>
                                {hammam.isActif ? 'Actif' : 'Inactif'}
                            </span>
                        </div>

                        {/* Header */}
                        <div className="mb-4">
                            <p className="text-xs text-primary-400 font-mono mb-1">{hammam.code}</p>
                            <h3 className="text-xl font-bold text-white">{hammam.nom}</h3>
                            <p className="text-sm text-slate-400 flex items-center gap-1 mt-1">
                                <MapPin className="w-4 h-4" />
                                {hammam.adresse || 'Adresse non définie'}
                            </p>
                        </div>

                        {/* Stats */}
                        <div className="grid grid-cols-3 gap-4 py-4 border-t border-b border-slate-700/50">
                            <div className="text-center">
                                <div className="flex items-center justify-center gap-1 text-slate-400 text-xs mb-1">
                                    <Users className="w-3 h-3" />
                                    Employés
                                </div>
                                <p className="text-lg font-bold text-white">{hammam.nombreEmployes || 0}</p>
                            </div>
                            <div className="text-center">
                                <div className="flex items-center justify-center gap-1 text-slate-400 text-xs mb-1">
                                    <Ticket className="w-3 h-3" />
                                    Tickets
                                </div>
                                <p className="text-lg font-bold text-white">{hammam.ticketsAujourdhui || 0}</p>
                            </div>
                            <div className="text-center">
                                <div className="text-slate-400 text-xs mb-1">Revenus</div>
                                <p className="text-lg font-bold text-success-400">{hammam.recetteAujourdhui || 0} DH</p>
                            </div>
                        </div>

                        {/* Actions */}
                        <div className="flex gap-2 mt-4">
                            <button
                                onClick={() => openEditModal(hammam)}
                                className="flex-1 px-4 py-2 bg-slate-700/50 hover:bg-slate-700 text-slate-300 rounded-lg transition-colors flex items-center justify-center gap-2"
                            >
                                <Edit className="w-4 h-4" />
                                Modifier
                            </button>
                            <button
                                onClick={() => toggleActif(hammam)}
                                className={`px-4 py-2 rounded-lg transition-colors ${hammam.isActif
                                    ? 'bg-danger-500/20 text-danger-400 hover:bg-danger-500/30'
                                    : 'bg-success-500/20 text-success-400 hover:bg-success-500/30'
                                    }`}
                            >
                                {hammam.isActif ? 'Désactiver' : 'Activer'}
                            </button>
                            <button
                                onClick={() => deleteHammam(hammam)}
                                className="px-3 py-2 bg-red-900/30 text-red-400 hover:bg-red-900/50 rounded-lg transition-colors"
                                title="Supprimer ce hammam"
                            >
                                <Trash2 className="w-4 h-4" />
                            </button>
                        </div>
                    </div>
                ))}
            </div>

            {/* Modal */}
            {showModal && (
                <div className="modal-overlay" onClick={() => setShowModal(false)}>
                    <div className="modal-content max-w-3xl max-h-[90vh] overflow-y-auto" onClick={(e) => e.stopPropagation()}>
                        <div className="flex items-center justify-between mb-6">
                            <h3 className="text-xl font-semibold text-white">
                                {modalType === 'create' ? 'Nouveau Hammam' : 'Modifier Hammam'}
                            </h3>
                            <button
                                onClick={() => setShowModal(false)}
                                className="p-2 text-slate-400 hover:text-white rounded-lg transition-colors"
                            >
                                <X className="w-5 h-5" />
                            </button>
                        </div>

                        <div className="space-y-6">
                            {/* Informations de base */}
                            <div className="space-y-4">
                                <h4 className="text-lg font-medium text-white flex items-center gap-2">
                                    <Building2 className="w-5 h-5 text-primary-400" />
                                    Informations générales
                                </h4>
                                <div className="grid grid-cols-2 gap-4">
                                    <div>
                                        <label className="block text-sm font-medium text-slate-300 mb-2">Code</label>
                                        <input
                                            type="text"
                                            value={selectedHammam?.code || ''}
                                            onChange={(e) => setSelectedHammam({ ...selectedHammam, code: e.target.value })}
                                            className="input-field font-mono"
                                            placeholder="HAM001 (auto-généré si vide)"
                                            disabled={modalType === 'edit'}
                                        />
                                    </div>
                                    <div>
                                        <label className="block text-sm font-medium text-slate-300 mb-2">Nom *</label>
                                        <input
                                            type="text"
                                            value={selectedHammam?.nom || ''}
                                            onChange={(e) => setSelectedHammam({ ...selectedHammam, nom: e.target.value })}
                                            className="input-field"
                                            placeholder="Hammam Centre"
                                        />
                                    </div>
                                </div>
                                <div>
                                    <label className="block text-sm font-medium text-slate-300 mb-2">Nom en Arabe</label>
                                    <input
                                        type="text"
                                        value={selectedHammam?.nomArabe || ''}
                                        onChange={(e) => setSelectedHammam({ ...selectedHammam, nomArabe: e.target.value })}
                                        className="input-field text-right"
                                        dir="rtl"
                                        placeholder="حمام الحرية"
                                    />
                                </div>
                                <div>
                                    <label className="block text-sm font-medium text-slate-300 mb-2">Préfixe Ticket (6 chiffres)</label>
                                    <input
                                        type="number"
                                        value={selectedHammam?.prefixeTicket || 100000}
                                        onChange={(e) => setSelectedHammam({ ...selectedHammam, prefixeTicket: parseInt(e.target.value) || 100000 })}
                                        className="input-field font-mono"
                                        placeholder="822200"
                                        min="100000"
                                        max="999999"
                                    />
                                    <p className="text-xs text-slate-500 mt-1">Ex: 822200 → Tickets: 822201, 822202...</p>
                                </div>
                                <div>
                                    <label className="block text-sm font-medium text-slate-300 mb-2">Adresse</label>
                                    <textarea
                                        value={selectedHammam?.adresse || ''}
                                        onChange={(e) => setSelectedHammam({ ...selectedHammam, adresse: e.target.value })}
                                        className="input-field"
                                        rows={2}
                                        placeholder="123 Rue Principale, Casablanca"
                                    />
                                </div>
                            </div>

                            {/* Types de tickets - Mode édition (modifiable) */}
                            {modalType === 'edit' && (
                                <div className="space-y-4">
                                    <div className="flex items-center justify-between">
                                        <h4 className="text-lg font-medium text-white flex items-center gap-2">
                                            <Ticket className="w-5 h-5 text-success-400" />
                                            Produits / Tarifs
                                        </h4>
                                        <button
                                            type="button"
                                            onClick={addNewProductToHammam}
                                            disabled={savingItem === 'new-product'}
                                            className="text-sm text-primary-400 hover:text-primary-300 flex items-center gap-1"
                                        >
                                            {savingItem === 'new-product' ? <Loader2 className="w-4 h-4 animate-spin" /> : <Plus className="w-4 h-4" />}
                                            Ajouter un produit
                                        </button>
                                    </div>
                                    {loadingDetails ? (
                                        <div className="flex items-center justify-center py-4">
                                            <Loader2 className="w-5 h-5 animate-spin text-primary-400" />
                                            <span className="ml-2 text-slate-400">Chargement...</span>
                                        </div>
                                    ) : existingTypeTickets.length === 0 ? (
                                        <p className="text-slate-400 text-sm text-center py-4">
                                            Aucun produit configuré. Cliquez sur "Ajouter un produit" pour commencer.
                                        </p>
                                    ) : (
                                        <div className="space-y-2">
                                            {existingTypeTickets.map((type) => (
                                                <div key={type.id} className="p-3 bg-slate-800/50 rounded-lg">
                                                    {editingProduct === type.id ? (
                                                        // Mode édition du produit
                                                        <div className="space-y-3">
                                                            <div className="flex gap-2 items-center">
                                                                <input
                                                                    type="text"
                                                                    value={type.nom}
                                                                    onChange={(e) => updateExistingProductField(type.id, 'nom', e.target.value)}
                                                                    className="input-field flex-1"
                                                                    placeholder="Nom du produit"
                                                                />
                                                                <input
                                                                    type="color"
                                                                    value={type.couleur || '#3B82F6'}
                                                                    onChange={(e) => updateExistingProductField(type.id, 'couleur', e.target.value)}
                                                                    className="w-10 h-10 rounded cursor-pointer"
                                                                />
                                                            </div>
                                                            {/* Image upload */}
                                                            <div className="flex gap-2 items-center">
                                                                {type.imageUrl ? (
                                                                    <div className="flex items-center gap-2">
                                                                        <img src={type.imageUrl} alt={type.nom} className="w-12 h-12 rounded-lg object-cover border border-slate-600" />
                                                                        <button
                                                                            type="button"
                                                                            onClick={() => handleDeleteProductImage(type.id)}
                                                                            disabled={savingItem === `product-${type.id}`}
                                                                            className="text-xs text-danger-400 hover:text-danger-300"
                                                                        >
                                                                            Supprimer image
                                                                        </button>
                                                                    </div>
                                                                ) : (
                                                                    <label className="flex items-center gap-1 px-3 py-2 bg-slate-700/50 text-slate-300 hover:bg-slate-700 rounded-lg cursor-pointer text-sm">
                                                                        <Upload className="w-4 h-4" />
                                                                        Image
                                                                        <input
                                                                            type="file"
                                                                            accept="image/*"
                                                                            className="hidden"
                                                                            onChange={(e) => handleProductImageUpload(type.id, e.target.files[0])}
                                                                        />
                                                                    </label>
                                                                )}
                                                            </div>
                                                            <div className="flex gap-2 items-center">
                                                                <div className="flex items-center gap-1">
                                                                    <input
                                                                        type="number"
                                                                        value={type.prix}
                                                                        onChange={(e) => updateExistingProductField(type.id, 'prix', parseFloat(e.target.value) || 0)}
                                                                        className="input-field w-24"
                                                                        placeholder="Prix"
                                                                    />
                                                                    <span className="text-slate-400">DH</span>
                                                                </div>
                                                                <button
                                                                    type="button"
                                                                    onClick={() => updateExistingProduct(type)}
                                                                    disabled={savingItem === `product-${type.id}`}
                                                                    className="px-3 py-2 bg-success-500/20 text-success-400 hover:bg-success-500/30 rounded-lg flex items-center gap-1"
                                                                >
                                                                    {savingItem === `product-${type.id}` ? <Loader2 className="w-4 h-4 animate-spin" /> : <Save className="w-4 h-4" />}
                                                                    Sauvegarder
                                                                </button>
                                                                <button
                                                                    type="button"
                                                                    onClick={() => setEditingProduct(null)}
                                                                    className="px-3 py-2 bg-slate-600/50 text-slate-300 hover:bg-slate-600 rounded-lg"
                                                                >
                                                                    Annuler
                                                                </button>
                                                            </div>
                                                        </div>
                                                    ) : (
                                                        // Mode affichage du produit
                                                        <div className="flex gap-2 items-center">
                                                            {type.imageUrl ? (
                                                                <img src={type.imageUrl} alt={type.nom} className="w-6 h-6 rounded-full object-cover flex-shrink-0" />
                                                            ) : (
                                                                <div
                                                                    className="w-4 h-4 rounded-full flex-shrink-0"
                                                                    style={{ backgroundColor: type.couleur || '#3B82F6' }}
                                                                />
                                                            )}
                                                            <span className="flex-1 text-white font-medium">{type.nom}</span>
                                                            <span className="text-success-400 font-bold">{type.prix} DH</span>
                                                            <button
                                                                type="button"
                                                                onClick={() => toggleProductStatus(type)}
                                                                disabled={savingItem === `product-${type.id}`}
                                                                className={`p-1 rounded transition-colors ${type.isActif ? 'text-success-400 hover:text-success-300' : 'text-slate-500 hover:text-slate-400'}`}
                                                                title={type.isActif ? 'Désactiver' : 'Activer'}
                                                            >
                                                                {savingItem === `product-${type.id}` ? <Loader2 className="w-5 h-5 animate-spin" /> :
                                                                    type.isActif ? <ToggleRight className="w-5 h-5" /> : <ToggleLeft className="w-5 h-5" />}
                                                            </button>
                                                            <button
                                                                type="button"
                                                                onClick={() => setEditingProduct(type.id)}
                                                                className="p-1 text-primary-400 hover:text-primary-300"
                                                                title="Modifier"
                                                            >
                                                                <Edit className="w-4 h-4" />
                                                            </button>
                                                            <button
                                                                type="button"
                                                                onClick={() => deleteExistingProduct(type)}
                                                                disabled={savingItem === `product-${type.id}`}
                                                                className="p-1 text-danger-400 hover:text-danger-300"
                                                                title="Supprimer"
                                                            >
                                                                <Trash2 className="w-4 h-4" />
                                                            </button>
                                                        </div>
                                                    )}
                                                </div>
                                            ))}
                                        </div>
                                    )}
                                </div>
                            )}

                            {/* Employés - Mode édition (modifiable) */}
                            {modalType === 'edit' && (
                                <div className="space-y-4">
                                    <div className="flex items-center justify-between">
                                        <h4 className="text-lg font-medium text-white flex items-center gap-2">
                                            <Users className="w-5 h-5 text-accent-400" />
                                            Employés
                                        </h4>
                                        <button
                                            type="button"
                                            onClick={addNewEmployeToHammam}
                                            disabled={savingItem === 'new-employe' || existingEmployes.filter(e => e.isActif).length >= 2}
                                            className="text-sm text-primary-400 hover:text-primary-300 flex items-center gap-1 disabled:opacity-50 disabled:cursor-not-allowed"
                                        >
                                            {savingItem === 'new-employe' ? <Loader2 className="w-4 h-4 animate-spin" /> : <Plus className="w-4 h-4" />}
                                            Ajouter un employé
                                        </button>
                                    </div>
                                    {loadingDetails ? (
                                        <div className="flex items-center justify-center py-4">
                                            <Loader2 className="w-5 h-5 animate-spin text-primary-400" />
                                            <span className="ml-2 text-slate-400">Chargement...</span>
                                        </div>
                                    ) : existingEmployes.length === 0 ? (
                                        <p className="text-slate-400 text-sm text-center py-4">
                                            Aucun employé assigné. Cliquez sur "Ajouter un employé" pour commencer.
                                        </p>
                                    ) : (
                                        <div className="space-y-3">
                                            {existingEmployes.map((emp) => (
                                                <div key={emp.id} className="p-4 bg-slate-800/50 rounded-lg">
                                                    {editingEmploye === emp.id ? (
                                                        // Mode édition de l'employé
                                                        <div className="space-y-3">
                                                            <div className="grid grid-cols-2 gap-3">
                                                                <input
                                                                    type="text"
                                                                    value={emp.prenom || ''}
                                                                    onChange={(e) => updateExistingEmployeField(emp.id, 'prenom', e.target.value)}
                                                                    className="input-field"
                                                                    placeholder="Prénom"
                                                                />
                                                                <input
                                                                    type="text"
                                                                    value={emp.nom || ''}
                                                                    onChange={(e) => updateExistingEmployeField(emp.id, 'nom', e.target.value)}
                                                                    className="input-field"
                                                                    placeholder="Nom"
                                                                />
                                                                <select
                                                                    value={emp.langue || 'FR'}
                                                                    onChange={(e) => updateExistingEmployeField(emp.id, 'langue', e.target.value)}
                                                                    className="input-field"
                                                                >
                                                                    <option value="FR">Français</option>
                                                                    <option value="AR">العربية</option>
                                                                </select>
                                                                <div className="input-field bg-slate-800/30 flex items-center text-slate-400 text-sm cursor-default">
                                                                    {emp.icone === 'User2' ? '🟢 Vert' : '🔵 Bleu'} (auto)
                                                                </div>
                                                            </div>
                                                            <p className="text-xs text-slate-500">
                                                                Utilisateur: {emp.username}
                                                            </p>
                                                            <div className="flex gap-2">
                                                                <button
                                                                    type="button"
                                                                    onClick={() => updateExistingEmploye(emp)}
                                                                    disabled={savingItem === `employe-${emp.id}`}
                                                                    className="px-3 py-2 bg-success-500/20 text-success-400 hover:bg-success-500/30 rounded-lg flex items-center gap-1"
                                                                >
                                                                    {savingItem === `employe-${emp.id}` ? <Loader2 className="w-4 h-4 animate-spin" /> : <Save className="w-4 h-4" />}
                                                                    Sauvegarder
                                                                </button>
                                                                <button
                                                                    type="button"
                                                                    onClick={() => setEditingEmploye(null)}
                                                                    className="px-3 py-2 bg-slate-600/50 text-slate-300 hover:bg-slate-600 rounded-lg"
                                                                >
                                                                    Annuler
                                                                </button>
                                                            </div>
                                                        </div>
                                                    ) : (
                                                        // Mode affichage de l'employé
                                                        <>
                                                            <div className="flex items-center justify-between mb-2">
                                                                <span className="text-white font-medium">
                                                                    {emp.prenom} {emp.nom}
                                                                </span>
                                                                <div className="flex items-center gap-2">
                                                                    <button
                                                                        type="button"
                                                                        onClick={() => toggleEmployeStatus(emp)}
                                                                        disabled={savingItem === `employe-${emp.id}`}
                                                                        className={`p-1 rounded transition-colors ${emp.isActif ? 'text-success-400 hover:text-success-300' : 'text-slate-500 hover:text-slate-400'}`}
                                                                        title={emp.isActif ? 'Désactiver' : 'Activer'}
                                                                    >
                                                                        {savingItem === `employe-${emp.id}` ? <Loader2 className="w-5 h-5 animate-spin" /> :
                                                                            emp.isActif ? <ToggleRight className="w-5 h-5" /> : <ToggleLeft className="w-5 h-5" />}
                                                                    </button>
                                                                    <button
                                                                        type="button"
                                                                        onClick={() => setEditingEmploye(emp.id)}
                                                                        className="p-1 text-primary-400 hover:text-primary-300"
                                                                        title="Modifier"
                                                                    >
                                                                        <Edit className="w-4 h-4" />
                                                                    </button>
                                                                    <button
                                                                        type="button"
                                                                        onClick={() => deleteExistingEmploye(emp)}
                                                                        disabled={savingItem === `employe-${emp.id}`}
                                                                        className="p-1 text-danger-400 hover:text-danger-300"
                                                                        title="Supprimer"
                                                                    >
                                                                        <Trash2 className="w-4 h-4" />
                                                                    </button>
                                                                </div>
                                                            </div>
                                                            <div className="grid grid-cols-2 gap-3 text-sm">
                                                                <div>
                                                                    <span className="text-slate-400">Utilisateur:</span>
                                                                    <span className="ml-2 text-white font-mono">{emp.username}</span>
                                                                </div>
                                                                <div className="flex items-center gap-2">
                                                                    <span className="text-slate-400">Mot de passe:</span>
                                                                    <span className="text-white font-mono flex items-center gap-1">
                                                                        <Key className="w-3 h-3 text-accent-400" />
                                                                        {showPasswords[emp.id] ? (emp.passwordClair || '(non disponible)') : '••••••'}
                                                                    </span>
                                                                    <button
                                                                        type="button"
                                                                        onClick={() => togglePasswordVisibility(emp.id)}
                                                                        className="p-1 text-slate-400 hover:text-white transition-colors"
                                                                        title={showPasswords[emp.id] ? 'Masquer' : 'Afficher'}
                                                                    >
                                                                        {showPasswords[emp.id] ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
                                                                    </button>
                                                                    <button
                                                                        type="button"
                                                                        onClick={() => resetEmployePassword(emp)}
                                                                        disabled={savingItem === `employe-${emp.id}`}
                                                                        className="px-2 py-1 text-xs bg-accent-500/20 text-accent-400 hover:bg-accent-500/30 rounded"
                                                                        title="Changer le mot de passe"
                                                                    >
                                                                        Changer
                                                                    </button>
                                                                </div>
                                                            </div>
                                                            <div className="mt-2 text-xs text-slate-500">
                                                                Rôle: {emp.role} • Langue: {emp.langue}
                                                            </div>
                                                        </>
                                                    )}
                                                </div>
                                            ))}
                                        </div>
                                    )}
                                </div>
                            )}

                            {/* Types de tickets (seulement pour création) */}
                            {modalType === 'create' && (
                                <div className="space-y-4">
                                    <div className="flex items-center justify-between">
                                        <h4 className="text-lg font-medium text-white flex items-center gap-2">
                                            <Ticket className="w-5 h-5 text-success-400" />
                                            Produits / Tarifs
                                        </h4>
                                        <button
                                            type="button"
                                            onClick={addTypeTicket}
                                            className="text-sm text-primary-400 hover:text-primary-300"
                                        >
                                            + Ajouter un produit
                                        </button>
                                    </div>
                                    <div className="space-y-2">
                                        {newTypeTickets.map((type, index) => (
                                            <div key={index} className="flex gap-2 items-center p-3 bg-slate-800/50 rounded-lg">
                                                <input
                                                    type="text"
                                                    value={type.nom}
                                                    onChange={(e) => updateTypeTicket(index, 'nom', e.target.value)}
                                                    className="input-field flex-1"
                                                    placeholder="Nom du produit"
                                                />
                                                <div className="flex items-center gap-1">
                                                    <input
                                                        type="number"
                                                        value={type.prix}
                                                        onChange={(e) => updateTypeTicket(index, 'prix', parseFloat(e.target.value))}
                                                        className="input-field w-20"
                                                        placeholder="Prix"
                                                    />
                                                    <span className="text-slate-400">DH</span>
                                                </div>
                                                <input
                                                    type="color"
                                                    value={type.couleur}
                                                    onChange={(e) => updateTypeTicket(index, 'couleur', e.target.value)}
                                                    className="w-10 h-10 rounded cursor-pointer"
                                                />
                                                <button
                                                    type="button"
                                                    onClick={() => removeTypeTicket(index)}
                                                    className="p-2 text-danger-400 hover:text-danger-300"
                                                >
                                                    <Trash2 className="w-4 h-4" />
                                                </button>
                                            </div>
                                        ))}
                                    </div>
                                </div>
                            )}

                            {/* Employés (seulement pour création) */}
                            {modalType === 'create' && (
                                <div className="space-y-4">
                                    <div className="flex items-center justify-between">
                                        <h4 className="text-lg font-medium text-white flex items-center gap-2">
                                            <Users className="w-5 h-5 text-accent-400" />
                                            Employés
                                        </h4>
                                        <button
                                            type="button"
                                            onClick={addEmploye}
                                            disabled={newEmployes.length >= 2}
                                            className="text-sm text-primary-400 hover:text-primary-300 disabled:opacity-50 disabled:cursor-not-allowed"
                                        >
                                            + Ajouter un employé (max 2)
                                        </button>
                                    </div>
                                    <div className="space-y-3">
                                        {newEmployes.map((emp, index) => (
                                            <div key={index} className="p-4 bg-slate-800/50 rounded-lg space-y-3">
                                                <div className="flex items-center justify-between">
                                                    <span className="text-sm font-medium text-primary-400">Employé {index + 1}</span>
                                                    <button
                                                        type="button"
                                                        onClick={() => removeEmploye(index)}
                                                        className="p-1 text-danger-400 hover:text-danger-300"
                                                    >
                                                        <Trash2 className="w-4 h-4" />
                                                    </button>
                                                </div>
                                                <div className="grid grid-cols-2 gap-3">
                                                    <input
                                                        type="text"
                                                        value={emp.nom}
                                                        onChange={(e) => updateEmploye(index, 'nom', e.target.value)}
                                                        className="input-field"
                                                        placeholder="Nom"
                                                    />
                                                    <input
                                                        type="text"
                                                        value={emp.prenom}
                                                        onChange={(e) => updateEmploye(index, 'prenom', e.target.value)}
                                                        className="input-field"
                                                        placeholder="Prénom"
                                                    />
                                                    <input
                                                        type="tel"
                                                        inputMode="numeric"
                                                        pattern="[0-9]*"
                                                        value={emp.password}
                                                        onChange={(e) => {
                                                            const val = e.target.value.replace(/\D/g, '')
                                                            updateEmploye(index, 'password', val)
                                                        }}
                                                        className="input-field"
                                                        placeholder="Code PIN (chiffres)"
                                                    />
                                                </div>
                                                <p className="text-xs text-slate-500">
                                                    Icône et nom d'utilisateur assignés automatiquement
                                                </p>
                                            </div>
                                        ))}
                                        {newEmployes.length === 0 && (
                                            <p className="text-slate-400 text-sm text-center py-4">
                                                Aucun employé ajouté. Vous pourrez en ajouter plus tard.
                                            </p>
                                        )}
                                    </div>
                                </div>
                            )}
                        </div>

                        <div className="flex justify-end gap-3 mt-6 pt-4 border-t border-slate-700/50">
                            <button onClick={() => setShowModal(false)} className="btn-secondary">
                                Annuler
                            </button>
                            <button onClick={handleSave} disabled={saving} className="btn-primary flex items-center gap-2">
                                {saving && <Loader2 className="w-4 h-4 animate-spin" />}
                                Enregistrer
                            </button>
                        </div>
                    </div>
                </div>
            )}

            {/* Modal PIN personnalisé */}
            {pinModal.show && (
                <div className="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-[60] animate-fadeIn">
                    <div className="bg-gradient-to-b from-slate-800 to-slate-900 rounded-2xl shadow-2xl border border-slate-700/50 w-full max-w-sm mx-4 overflow-hidden">
                        {/* Header */}
                        <div className="px-6 pt-6 pb-4 text-center">
                            <div className="w-16 h-16 mx-auto mb-4 rounded-full bg-gradient-to-br from-primary-500/20 to-accent-500/20 border border-primary-500/30 flex items-center justify-center">
                                {pinModal.type === 'reset' ? (
                                    <Key className="w-8 h-8 text-primary-400" />
                                ) : (
                                    <Shield className="w-8 h-8 text-accent-400" />
                                )}
                            </div>
                            <h3 className="text-xl font-semibold text-white mb-1">
                                {pinModal.type === 'reset' ? 'Changer le code PIN' : 'Code PIN du nouvel employé'}
                            </h3>
                            {pinModal.type === 'reset' && pinModal.employe && (
                                <p className="text-sm text-slate-400">
                                    {pinModal.employe.username}
                                </p>
                            )}
                            <p className="text-xs text-slate-500 mt-2">
                                Entrez un code numérique unique (min 3 chiffres)
                            </p>
                        </div>

                        {/* PIN Display */}
                        <div className="px-6 pb-4">
                            <div className="relative">
                                <div
                                    className="w-full bg-slate-950/50 border-2 rounded-xl px-4 py-4 text-center font-mono tracking-[0.5em] text-2xl transition-all duration-200 focus-within:ring-2 focus-within:ring-primary-500/50"
                                    style={{
                                        borderColor: pinError ? '#ef4444' : pinValue ? '#3b82f6' : '#334155'
                                    }}
                                >
                                    {pinValue ? (
                                        <span className="text-white">
                                            {showPinValue ? pinValue : '●'.repeat(pinValue.length)}
                                        </span>
                                    ) : (
                                        <span className="text-slate-600">• • • •</span>
                                    )}
                                    <input
                                        type="tel"
                                        inputMode="numeric"
                                        pattern="[0-9]*"
                                        value={pinValue}
                                        onChange={(e) => {
                                            const val = e.target.value.replace(/\D/g, '').slice(0, 8)
                                            setPinValue(val)
                                            setPinError('')
                                        }}
                                        onKeyDown={handlePinKeyDown}
                                        autoFocus
                                        className="absolute inset-0 w-full h-full opacity-0 cursor-text"
                                    />
                                </div>
                                <button
                                    type="button"
                                    onClick={() => setShowPinValue(!showPinValue)}
                                    className="absolute right-3 top-1/2 -translate-y-1/2 p-1.5 text-slate-400 hover:text-white transition-colors rounded-lg hover:bg-slate-700/50"
                                >
                                    {showPinValue ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
                                </button>
                            </div>

                            {/* Error message */}
                            {pinError && (
                                <div className="mt-3 px-3 py-2 bg-red-500/10 border border-red-500/20 rounded-lg">
                                    <p className="text-red-400 text-sm text-center">{pinError}</p>
                                </div>
                            )}
                        </div>

                        {/* Numeric Keypad */}
                        <div className="px-6 pb-4">
                            <div className="grid grid-cols-3 gap-2">
                                {[1, 2, 3, 4, 5, 6, 7, 8, 9].map(digit => (
                                    <button
                                        key={digit}
                                        type="button"
                                        onClick={() => handlePinDigitClick(String(digit))}
                                        className="h-14 rounded-xl bg-slate-700/50 hover:bg-slate-600/70 active:bg-slate-500/70 text-white text-xl font-semibold transition-all duration-150 active:scale-95 border border-slate-600/30 hover:border-slate-500/50"
                                    >
                                        {digit}
                                    </button>
                                ))}
                                <button
                                    type="button"
                                    onClick={() => { setPinValue(''); setPinError('') }}
                                    className="h-14 rounded-xl bg-slate-800/50 hover:bg-slate-700/50 text-slate-400 hover:text-white text-sm font-medium transition-all duration-150 active:scale-95 border border-slate-700/30"
                                >
                                    Effacer
                                </button>
                                <button
                                    type="button"
                                    onClick={() => handlePinDigitClick('0')}
                                    className="h-14 rounded-xl bg-slate-700/50 hover:bg-slate-600/70 active:bg-slate-500/70 text-white text-xl font-semibold transition-all duration-150 active:scale-95 border border-slate-600/30 hover:border-slate-500/50"
                                >
                                    0
                                </button>
                                <button
                                    type="button"
                                    onClick={handlePinBackspace}
                                    className="h-14 rounded-xl bg-slate-800/50 hover:bg-slate-700/50 text-slate-400 hover:text-white transition-all duration-150 active:scale-95 border border-slate-700/30 flex items-center justify-center"
                                >
                                    <Delete className="w-5 h-5" />
                                </button>
                            </div>
                        </div>

                        {/* Actions */}
                        <div className="px-6 pb-6 flex gap-3">
                            <button
                                type="button"
                                onClick={() => setPinModal({ show: false, type: null, employe: null })}
                                disabled={pinLoading}
                                className="flex-1 py-3 rounded-xl bg-slate-700/50 hover:bg-slate-600/70 text-slate-300 font-medium transition-all duration-200 border border-slate-600/30"
                            >
                                Annuler
                            </button>
                            <button
                                type="button"
                                onClick={handlePinSubmit}
                                disabled={pinLoading || !pinValue || pinValue.length < 3}
                                className="flex-1 py-3 rounded-xl bg-gradient-to-r from-primary-500 to-primary-600 hover:from-primary-400 hover:to-primary-500 text-white font-semibold transition-all duration-200 disabled:opacity-40 disabled:cursor-not-allowed flex items-center justify-center gap-2 shadow-lg shadow-primary-500/20"
                            >
                                {pinLoading ? (
                                    <Loader2 className="w-5 h-5 animate-spin" />
                                ) : (
                                    <>
                                        <Lock className="w-4 h-4" />
                                        Confirmer
                                    </>
                                )}
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    )
}
