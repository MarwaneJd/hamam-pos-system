/**
 * Composant de chargement
 */
export default function LoadingSpinner({ size = 'md', className = '' }) {
    const sizeClasses = {
        sm: 'w-5 h-5 border-2',
        md: 'w-8 h-8 border-4',
        lg: 'w-12 h-12 border-4',
        xl: 'w-16 h-16 border-4',
    }

    return (
        <div
            className={`${sizeClasses[size]} border-slate-600 border-t-primary-500 rounded-full animate-spin ${className}`}
            role="status"
            aria-label="Chargement..."
        />
    )
}
