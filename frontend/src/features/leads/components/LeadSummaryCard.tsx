import { Button } from '../../../shared/components/ui/Button'
import { Card } from '../../../shared/components/ui/Card'
import { Text } from '../../../shared/components/ui/Text'
import type { LeadSummaryDto } from '../../../shared/api/types'
import { formatDateTimeToBrazilian, maskCpf } from '../../../shared/utils/formatters'
import { getStepLabel } from '../../../shared/utils/leadLabels'
import { StatusBadge } from './StatusBadge'
import styles from './LeadSummaryCard.module.css'

interface LeadSummaryCardProps {
  lead: LeadSummaryDto
  onResume: (leadId: string) => void | Promise<void>
  busy?: boolean
}

export function LeadSummaryCard({ lead, onResume, busy }: LeadSummaryCardProps) {
  return (
    <Card className={styles.card}>
      <div className={styles.header}>
        <Text variant="subtitle" as="h3" className={styles.cpf}>
          {maskCpf(lead.cpf)}
        </Text>
        <StatusBadge status={lead.status} />
      </div>

      <dl className={styles.details}>
        <div className={styles.detail}>
          <Text variant="caption" as="dt">
            Etapa atual
          </Text>
          <Text variant="body" as="dd">
            {getStepLabel(lead.progress.currentStep)}
          </Text>
        </div>
        <div className={styles.detail}>
          <Text variant="caption" as="dt">
            Atualizado em
          </Text>
          <Text variant="body" as="dd">
            {formatDateTimeToBrazilian(lead.updatedAt)}
          </Text>
        </div>
        <div className={styles.detail}>
          <Text variant="caption" as="dt">
            Criado em
          </Text>
          <Text variant="body" as="dd">
            {formatDateTimeToBrazilian(lead.createdAt)}
          </Text>
        </div>
        {lead.finalRegistration && (
          <div className={styles.detail}>
            <Text variant="caption" as="dt">
              Registro
            </Text>
            <Text variant="body" as="dd">
              {lead.finalRegistration.registrationId}
            </Text>
          </div>
        )}
      </dl>

      <div className={styles.actions}>
        <Button onClick={() => onResume(lead.id)} loading={busy} disabled={busy} size="sm">
          Continuar
        </Button>
      </div>
    </Card>
  )
}
