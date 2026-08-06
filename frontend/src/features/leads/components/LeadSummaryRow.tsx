import { Button } from '../../../shared/components/ui/Button'
import type { LeadSummaryDto } from '../../../shared/api/types'
import { formatDateTimeToBrazilian, maskCpf } from '../../../shared/utils/formatters'
import { getStepLabel } from '../../../shared/utils/leadLabels'
import { StatusBadge } from './StatusBadge'
import styles from './LeadSummaryRow.module.css'

interface LeadSummaryRowProps {
  lead: LeadSummaryDto
  onResume: (leadId: string) => void | Promise<void>
  busy?: boolean
}

export function LeadSummaryRow({ lead, onResume, busy }: LeadSummaryRowProps) {
  return (
    <tr className={styles.row}>
      <td className={styles.cell}>{maskCpf(lead.cpf)}</td>
      <td className={styles.cell}>
        <StatusBadge status={lead.status} />
      </td>
      <td className={styles.cell}>{getStepLabel(lead.progress.currentStep)}</td>
      <td className={styles.cell}>{formatDateTimeToBrazilian(lead.updatedAt)}</td>
      <td className={styles.cell}>{formatDateTimeToBrazilian(lead.createdAt)}</td>
      <td className={styles.cell}>
        {lead.finalRegistration ? lead.finalRegistration.registrationId : '—'}
      </td>
      <td className={[styles.cell, styles.actions].join(' ')}>
        <Button onClick={() => onResume(lead.id)} loading={busy} disabled={busy} size="sm">
          Continuar
        </Button>
      </td>
    </tr>
  )
}
