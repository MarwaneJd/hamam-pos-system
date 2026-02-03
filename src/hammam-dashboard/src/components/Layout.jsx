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
} from 'lucide-react'
import { useState } from 'react'

/**
 * Layout principal avec sidebar
 */
export default function Layout() {
    const { user, logout } = useAuth()
    const location = useLocation()
    const [sidebarOpen, setSidebarOpen] = useState(false)

    const navigation = [
        { name: 'Dashboard', href: '/', icon: LayoutDashboard },
        { name: 'Employés', href: '/employes', icon: Users },
        { name: 'Hammams', href: '/hammams', icon: Building2 },
        { name: 'Comptabilité', href: '/comptabilite', icon: Calculator },
        { name: 'Rapports', href: '/rapports', icon: FileBarChart },
    ]

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
                        <button
                            onClick={logout}
                            className="flex items-center gap-2 w-full px-3 py-2 text-sm text-slate-400 hover:text-danger-400 hover:bg-danger-500/10 rounded-lg transition-colors"
                        >
                            <LogOut className="w-4 h-4" />
                            Déconnexion
                        </button>
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
        </div>
    )
}
