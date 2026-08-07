import type { ReactNode } from 'react'
import { Alert } from './ui/Alert'
import { Button } from './ui/Button'
import { Text } from './ui/Text'

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
export function LoadingError({
  loading,
  error,
  loadingLabel = 'Carregando…',
  onRetry,
  children,
}: LoadingErrorProps) {
  if (loading) {
    return (
      <div role="status" aria-live="polite">
        <Text variant="muted">{loadingLabel}</Text>
      </div>
    )
  }

  if (error) {
    return (
      <Alert variant="error" title="Algo deu errado">
        <Text variant="body">{error}</Text>
        {onRetry && (
          <div style={{ marginTop: '0.75rem' }}>
            <Button variant="secondary" size="sm" onClick={onRetry}>
              Tentar novamente
            </Button>
          </div>
        )}
      </Alert>
    )
  }

  return <>{children}</>
}
