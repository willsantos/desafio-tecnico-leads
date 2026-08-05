import type { ReactNode } from 'react'

interface LoadingErrorProps {
  loading?: boolean
  error?: string | null
  loadingLabel?: string
  onRetry?: () => void
  children?: ReactNode
}

/** Reusable loading/error-state wrapper: renders the loading label while `loading`, the error
 * (with an optional retry button) while `error` is set, otherwise `children`. Callers branch on
 * two inputs (`loading`, `error`) instead of scattering ad hoc conditionals per step page. */
export function LoadingError({ loading, error, loadingLabel = 'Carregando…', onRetry, children }: LoadingErrorProps) {
  if (loading) {
    return <p role="status">{loadingLabel}</p>
  }

  if (error) {
    return (
      <div role="alert" style={{ color: '#b00020', border: '1px solid #b00020', borderRadius: 4, padding: '0.75rem' }}>
        <p style={{ margin: 0 }}>{error}</p>
        {onRetry && (
          <button type="button" onClick={onRetry} style={{ marginTop: '0.5rem' }}>
            Tentar novamente
          </button>
        )}
      </div>
    )
  }

  return <>{children}</>
}
