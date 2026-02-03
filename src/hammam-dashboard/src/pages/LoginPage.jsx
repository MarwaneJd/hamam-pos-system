import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { useAuth } from '../context/AuthContext'
import { Bath, Eye, EyeOff, Loader2 } from 'lucide-react'

/**
 * Page de connexion avec design premium
 */
export default function LoginPage() {
    const { login } = useAuth()
    const [showPassword, setShowPassword] = useState(false)
    const [loading, setLoading] = useState(false)

    const {
        register,
        handleSubmit,
        formState: { errors },
    } = useForm()

    const onSubmit = async (data) => {
        setLoading(true)
        await login(data.username, data.password)
        setLoading(false)
    }

    return (
        <div className="min-h-screen animated-bg flex items-center justify-center p-4">
            {/* Background decorations */}
            <div className="fixed inset-0 overflow-hidden pointer-events-none">
                <div className="absolute -top-40 -right-40 w-80 h-80 bg-primary-500/20 rounded-full blur-3xl" />
                <div className="absolute -bottom-40 -left-40 w-80 h-80 bg-accent-500/20 rounded-full blur-3xl" />
            </div>

            <div className="w-full max-w-md relative z-10">
                {/* Logo */}
                <div className="text-center mb-8 animate-fade-in">
                    <div className="inline-flex items-center justify-center w-20 h-20 bg-gradient-to-br from-primary-500 to-accent-500 rounded-2xl mb-4 shadow-lg glow-blue">
                        <Bath className="w-10 h-10 text-white" />
                    </div>
                    <h1 className="text-3xl font-bold gradient-text">Hammam Dashboard</h1>
                    <p className="text-slate-400 mt-2">Connectez-vous pour accéder au système</p>
                </div>

                {/* Login form */}
                <form
                    onSubmit={handleSubmit(onSubmit)}
                    className="glass-card p-8 animate-slide-up"
                >
                    <h2 className="text-xl font-semibold text-white mb-6">Connexion</h2>

                    {/* Username */}
                    <div className="mb-5">
                        <label className="block text-sm font-medium text-slate-300 mb-2">
                            Nom d'utilisateur
                        </label>
                        <input
                            type="text"
                            {...register('username', {
                                required: "Le nom d'utilisateur est requis",
                            })}
                            className="input-field"
                            placeholder="Entrez votre username"
                            autoComplete="username"
                        />
                        {errors.username && (
                            <p className="mt-1 text-sm text-danger-400">
                                {errors.username.message}
                            </p>
                        )}
                    </div>

                    {/* Password */}
                    <div className="mb-6">
                        <label className="block text-sm font-medium text-slate-300 mb-2">
                            Mot de passe
                        </label>
                        <div className="relative">
                            <input
                                type={showPassword ? 'text' : 'password'}
                                {...register('password', {
                                    required: 'Le mot de passe est requis',
                                })}
                                className="input-field pr-12"
                                placeholder="Entrez votre mot de passe"
                                autoComplete="current-password"
                            />
                            <button
                                type="button"
                                onClick={() => setShowPassword(!showPassword)}
                                className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-white transition-colors"
                            >
                                {showPassword ? (
                                    <EyeOff className="w-5 h-5" />
                                ) : (
                                    <Eye className="w-5 h-5" />
                                )}
                            </button>
                        </div>
                        {errors.password && (
                            <p className="mt-1 text-sm text-danger-400">
                                {errors.password.message}
                            </p>
                        )}
                    </div>

                    {/* Submit button */}
                    <button
                        type="submit"
                        disabled={loading}
                        className="btn-primary w-full flex items-center justify-center gap-2 disabled:opacity-50 disabled:cursor-not-allowed"
                    >
                        {loading ? (
                            <>
                                <Loader2 className="w-5 h-5 animate-spin" />
                                Connexion en cours...
                            </>
                        ) : (
                            'Se connecter'
                        )}
                    </button>

                    {/* Demo credentials */}
                    <div className="mt-6 p-4 bg-slate-800/50 rounded-xl border border-slate-700">
                        <p className="text-xs text-slate-400 text-center">
                            <span className="font-semibold text-primary-400">Démo :</span>
                            <br />
                            Username: <code className="bg-slate-700 px-1 rounded">admin</code>
                            <br />
                            Password: <code className="bg-slate-700 px-1 rounded">Admin@123</code>
                        </p>
                    </div>
                </form>

                {/* Footer */}
                <p className="text-center text-slate-500 text-sm mt-6">
                    © 2026 Système de Gestion Hammam. Tous droits réservés.
                </p>
            </div>
        </div>
    )
}
