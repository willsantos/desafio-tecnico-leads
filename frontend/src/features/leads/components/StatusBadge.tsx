import type { LeadStatus } from '../../../shared/utils/leadLabels'
import { getStatusLabel } from '../../../shared/utils/leadLabels'
import styles from './StatusBadge.module.css'

type BadgeVariant = 'success' | 'info' | 'warning' | 'error'

const STATUS_VARIANTS: Record<LeadStatus, BadgeVariant> = {
  draft: 'warning',
  in_progress: 'info',
  pending_confirmation: 'info',
  confirming: 'info',
  completed: 'success',
  failed_retryable: 'error',
  pending_verification: 'warning',
  abandoned: 'error',
}

interface StatusBadgeProps {
  status: LeadStatus
}

export function StatusBadge({ status }: StatusBadgeProps) {
  const variant = STATUS_VARIANTS[status]
  return (
    <span className={[styles.badge, styles[variant]].join(' ')}>
      {getStatusLabel(status)}
    </span>
  )
}
