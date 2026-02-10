import { Outlet, NavLink, useLocation } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import {
    LayoutDashboard,
    Users,
    Building2,
    FileBarChart,
    LogOut,
    Menu,
    X,
    Bath,
    Calculator,
    KeyRound,
    Eye,
    EyeOff,
    Lock,
    Loader2,
    Check,
    AlertTriangle,
} from 'lucide-react'
import { useState } from 'react'
import { toast } from 'react-toastify'
import { employesService, authService } from '../services/api'

/**
 * Layout principal avec sidebar
 */
export default function Layout() {
    const { user, logout } = useAuth()
    const location = useLocation()
    const [sidebarOpen, setSidebarOpen] = useState(false)

    // Password change modal state
    const [showPasswordModal, setShowPasswordModal] = useState(false)
    const [currentPassword, setCurrentPassword] = useState('')
    const [newPassword, setNewPassword] = useState('')
    const [confirmPassword, setConfirmPassword] = useState('')
    const [showCurrentPw, setShowCurrentPw] = useState(false)
    const [showNewPw, setShowNewPw] = useState(false)
    const [passwordLoading, setPasswordLoading] = useState(false)
    const [passwordError, setPasswordError] = useState('')

    const navigation = [
        { name: 'Dashboard', href: '/', icon: LayoutDashboard },
        { name: 'Employés', href: '/employes', icon: Users },
        { name: 'Hammams', href: '/hammams', icon: Building2 },
        { name: 'Comptabilité', href: '/comptabilite', icon: Calculator },
        { name: 'Rapports', href: '/rapports', icon: FileBarChart },
    ]

    const openPasswordModal = () => {
        setCurrentPassword('')
        setNewPassword('')
        setConfirmPassword('')
        setPasswordError('')
        setShowCurrentPw(false)
        setShowNewPw(false)
        setShowPasswordModal(true)
    }

    const handlePasswordChange = async () => {
        setPasswordError('')

        if (!currentPassword) {
            setPasswordError('Veuillez entrer votre mot de passe actuel')
            return
        }
        if (!newPassword) {
            setPasswordError('Veuillez entrer un nouveau mot de passe')
            return
        }
        if (newPassword.length < 4) {
            setPasswordError('Le nouveau mot de passe doit contenir au moins 4 caractères')
            return
        }
        if (newPassword !== confirmPassword) {
            setPasswordError('Les mots de passe ne correspondent pas')
            return
        }
        if (currentPassword === newPassword) {
            setPasswordError('Le nouveau mot de passe doit être différent de l\'actuel')
            return
        }

        setPasswordLoading(true)
        try {
            // Verify current password by trying to login
            await authService.login(user.username, currentPassword)

            // Reset to new password
            await employesService.resetPassword(user.id, newPassword)

            toast.success('Mot de passe modifié avec succès')
            setShowPasswordModal(false)
        } catch (error) {
            const msg = error.response?.data?.message
            if (msg === 'Identifiants invalides') {
                setPasswordError('Mot de passe actuel incorrect')
            } else {
                setPasswordError(msg || 'Erreur lors du changement de mot de passe')
            }
        } finally {
            setPasswordLoading(false)
        }
    }

    const handlePasswordKeyDown = (e) => {
        if (e.key === 'Enter') {
            e.preventDefault()
            handlePasswordChange()
        }
    }

    return (
        <div className="min-h-screen animated-bg">
            {/* Overlay mobile */}
            {sidebarOpen && (
                <div
                    className="fixed inset-0 bg-black/50 z-30 lg:hidden"
                    onClick={() => setSidebarOpen(false)}
                />
            )}

            {/* Sidebar */}
            <aside
                className={`sidebar transform transition-transform duration-300 lg:translate-x-0 ${sidebarOpen ? 'translate-x-0' : '-translate-x-full'
                    }`}
            >
                {/* Logo */}
                <div className="p-6 border-b border-slate-700/50">
                    <div className="flex items-center gap-3">
                        <div className="w-10 h-10 bg-gradient-to-br from-primary-500 to-accent-500 rounded-xl flex items-center justify-center">
                            <Bath className="w-6 h-6 text-white" />
                        </div>
                        <div>
                            <h1 className="text-lg font-bold gradient-text">Hammam</h1>
                            <p className="text-xs text-slate-400">Gestion</p>
                        </div>
                    </div>
                </div>

                {/* Navigation */}
                <nav className="mt-6 flex-1">
                    {navigation.map((item) => {
                        const isActive = location.pathname === item.href
                        return (
                            <NavLink
                                key={item.name}
                                to={item.href}
                                className={`sidebar-link ${isActive ? 'active' : ''}`}
                                onClick={() => setSidebarOpen(false)}
                            >
                                <item.icon className="w-5 h-5" />
                                <span>{item.name}</span>
                            </NavLink>
                        )
                    })}
                </nav>

                {/* User info */}
                <div className="p-4 border-t border-slate-700/50">
                    <div className="glass-card p-4">
                        <div className="flex items-center gap-3 mb-3">
                            <div className="w-10 h-10 bg-gradient-to-br from-primary-500 to-accent-500 rounded-full flex items-center justify-center text-white font-bold">
                                {user?.prenom?.[0]}{user?.nom?.[0]}
                            </div>
                            <div className="flex-1 min-w-0">
                                <p className="font-medium text-white truncate">
                                    {user?.prenom} {user?.nom}
                                </p>
                                <p className="text-xs text-slate-400 truncate">
                                    {user?.role === 'Admin' ? 'Administrateur' : 'Employé'}
                                </p>
                            </div>
                        </div>
                        <div className="flex flex-col gap-1">
                            {user?.role === 'Admin' && (
                                <button
                                    onClick={openPasswordModal}
                                    className="flex items-center gap-2 w-full px-3 py-2 text-sm text-slate-400 hover:text-primary-400 hover:bg-primary-500/10 rounded-lg transition-colors"
                                >
                                    <KeyRound className="w-4 h-4" />
                                    Changer le mot de passe
                                </button>
                            )}
                            <button
                                onClick={logout}
                                className="flex items-center gap-2 w-full px-3 py-2 text-sm text-slate-400 hover:text-danger-400 hover:bg-danger-500/10 rounded-lg transition-colors"
                            >
                                <LogOut className="w-4 h-4" />
                                Déconnexion
                            </button>
                        </div>
                    </div>
                </div>
            </aside>

            {/* Main content */}
            <main className="lg:ml-64 min-h-screen">
                {/* Top bar mobile */}
                <header className="lg:hidden sticky top-0 z-20 glass-card rounded-none border-x-0 border-t-0 px-4 py-3">
                    <div className="flex items-center justify-between">
                        <button
                            onClick={() => setSidebarOpen(true)}
                            className="p-2 text-slate-400 hover:text-white transition-colors"
                        >
                            <Menu className="w-6 h-6" />
                        </button>
                        <div className="flex items-center gap-2">
                            <Bath className="w-6 h-6 text-primary-500" />
                            <span className="font-bold gradient-text">Hammam</span>
                        </div>
                        <div className="w-10" /> {/* Spacer */}
                    </div>
                </header>

                {/* Page content */}
                <div className="p-6 lg:p-8">
                    <Outlet />
                </div>
            </main>

            {/* Password Change Modal */}
            {showPasswordModal && (
                <div className="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-[60] animate-fadeIn">
                    <div className="bg-gradient-to-b from-slate-800 to-slate-900 rounded-2xl shadow-2xl border border-slate-700/50 w-full max-w-md mx-4 overflow-hidden">
                        {/* Header */}
                        <div className="px-6 pt-6 pb-4 text-center">
                            <div className="w-16 h-16 mx-auto mb-4 rounded-full bg-gradient-to-br from-primary-500/20 to-accent-500/20 border border-primary-500/30 flex items-center justify-center">
                                <KeyRound className="w-8 h-8 text-primary-400" />
                            </div>
                            <h3 className="text-xl font-semibold text-white mb-1">
                                Changer le mot de passe
                            </h3>
                            <p className="text-sm text-slate-400">
                                Compte administrateur
                            </p>
                        </div>

                        {/* Form */}
                        <div className="px-6 pb-4 space-y-4">
                            {/* Current Password */}
                            <div>
                                <label className="block text-sm font-medium text-slate-300 mb-1.5">
                                    Mot de passe actuel
                                </label>
                                <div className="relative">
                                    <input
                                        type={showCurrentPw ? 'text' : 'password'}
                                        value={currentPassword}
                                        onChange={(e) => { setCurrentPassword(e.target.value); setPasswordError('') }}
                                        onKeyDown={handlePasswordKeyDown}
                                        autoFocus
                                        placeholder="Entrez votre mot de passe actuel"
                                        className="w-full bg-slate-950/50 border-2 border-slate-700/50 rounded-xl px-4 py-3 pr-12 text-white placeholder-slate-500 focus:border-primary-500/50 focus:ring-2 focus:ring-primary-500/20 transition-all outline-none"
                                    />
                                    <button
                                        type="button"
                                        onClick={() => setShowCurrentPw(!showCurrentPw)}
                                        className="absolute right-3 top-1/2 -translate-y-1/2 p-1.5 text-slate-400 hover:text-white transition-colors rounded-lg hover:bg-slate-700/50"
                                    >
                                        {showCurrentPw ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
                                    </button>
                                </div>
                            </div>

                            {/* Divider */}
                            <div className="flex items-center gap-3">
                                <div className="flex-1 h-px bg-slate-700/50"></div>
                                <span className="text-xs text-slate-500">Nouveau</span>
                                <div className="flex-1 h-px bg-slate-700/50"></div>
                            </div>

                            {/* New Password */}
                            <div>
                                <label className="block text-sm font-medium text-slate-300 mb-1.5">
                                    Nouveau mot de passe
                                </label>
                                <div className="relative">
                                    <input
                                        type={showNewPw ? 'text' : 'password'}
                                        value={newPassword}
                                        onChange={(e) => { setNewPassword(e.target.value); setPasswordError('') }}
                                        onKeyDown={handlePasswordKeyDown}
                                        placeholder="Min. 4 caractères"
                                        className="w-full bg-slate-950/50 border-2 border-slate-700/50 rounded-xl px-4 py-3 pr-12 text-white placeholder-slate-500 focus:border-primary-500/50 focus:ring-2 focus:ring-primary-500/20 transition-all outline-none"
                                    />
                                    <button
                                        type="button"
                                        onClick={() => setShowNewPw(!showNewPw)}
                                        className="absolute right-3 top-1/2 -translate-y-1/2 p-1.5 text-slate-400 hover:text-white transition-colors rounded-lg hover:bg-slate-700/50"
                                    >
                                        {showNewPw ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
                                    </button>
                                </div>
                            </div>

                            {/* Confirm Password */}
                            <div>
                                <label className="block text-sm font-medium text-slate-300 mb-1.5">
                                    Confirmer le mot de passe
                                </label>
                                <div className="relative">
                                    <input
                                        type={showNewPw ? 'text' : 'password'}
                                        value={confirmPassword}
                                        onChange={(e) => { setConfirmPassword(e.target.value); setPasswordError('') }}
                                        onKeyDown={handlePasswordKeyDown}
                                        placeholder="Retapez le nouveau mot de passe"
                                        className={`w-full bg-slate-950/50 border-2 rounded-xl px-4 py-3 pr-12 text-white placeholder-slate-500 focus:ring-2 focus:ring-primary-500/20 transition-all outline-none ${confirmPassword && confirmPassword === newPassword
                                                ? 'border-green-500/50'
                                                : confirmPassword && confirmPassword !== newPassword
                                                    ? 'border-red-500/50'
                                                    : 'border-slate-700/50 focus:border-primary-500/50'
                                            }`}
                                    />
                                    {confirmPassword && confirmPassword === newPassword && (
                                        <Check className="absolute right-3 top-1/2 -translate-y-1/2 w-5 h-5 text-green-400" />
                                    )}
                                </div>
                            </div>

                            {/* Error */}
                            {passwordError && (
                                <div className="flex items-center gap-2 px-3 py-2.5 bg-red-500/10 border border-red-500/20 rounded-xl">
                                    <AlertTriangle className="w-4 h-4 text-red-400 flex-shrink-0" />
                                    <p className="text-red-400 text-sm">{passwordError}</p>
                                </div>
                            )}
                        </div>

                        {/* Actions */}
                        <div className="px-6 pb-6 flex gap-3">
                            <button
                                type="button"
                                onClick={() => setShowPasswordModal(false)}
                                disabled={passwordLoading}
                                className="flex-1 py-3 rounded-xl bg-slate-700/50 hover:bg-slate-600/70 text-slate-300 font-medium transition-all duration-200 border border-slate-600/30"
                            >
                                Annuler
                            </button>
                            <button
                                type="button"
                                onClick={handlePasswordChange}
                                disabled={passwordLoading || !currentPassword || !newPassword || !confirmPassword}
                                className="flex-1 py-3 rounded-xl bg-gradient-to-r from-primary-500 to-primary-600 hover:from-primary-400 hover:to-primary-500 text-white font-semibold transition-all duration-200 disabled:opacity-40 disabled:cursor-not-allowed flex items-center justify-center gap-2 shadow-lg shadow-primary-500/20"
                            >
                                {passwordLoading ? (
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

