import type { LeadSummaryDto } from '../../shared/api/types'
import { Button } from '../../shared/components/ui/Button'
import { Card } from '../../shared/components/ui/Card'
import { Modal } from '../../shared/components/ui/Modal'
import { Text } from '../../shared/components/ui/Text'
import { formatDateTimeToBrazilian, maskCpf } from '../../shared/utils/formatters'
import { getStepLabel } from '../../shared/utils/leadLabels'
import { StatusBadge } from '../leads/components/StatusBadge'
import styles from './CpfReuseModal.module.css'

interface CpfReuseModalProps {
  isOpen: boolean
  leads: LeadSummaryDto[]
  onContinue: (leadId: string) => void
  onStartNew: () => void
  busy?: boolean
}

/** P2-1: shown when the CPF typed on etapa 1 already has one or more active leads. */
export function CpfReuseModal({ isOpen, leads, onContinue, onStartNew, busy }: CpfReuseModalProps) {
  const hasMultiple = leads.length > 1

  return (
    <Modal
      isOpen={isOpen}
      onClose={() => {}}
      title="Já existe um cadastro em andamento"
      footer={
        <Button variant="secondary" onClick={onStartNew} disabled={busy}>
          Começar novo
        </Button>
      }
    >
      <div className={styles.wrapper}>
        <Text variant="body">
          {hasMultiple
            ? `Encontramos ${leads.length} cadastros em andamento para este CPF. Escolha qual deseja continuar:`
            : 'Encontramos um cadastro em andamento para este CPF. Deseja continuar de onde parou ou começar um novo cadastro?'}
        </Text>

        {hasMultiple ? (
          <ul className={styles.list}>
            {leads.map((lead) => (
              <li key={lead.id}>
                <Card className={styles.leadCard}>
                  <div className={styles.leadInfo}>
                    <Text variant="subtitle" as="h3">
                      {lead.cpf ? maskCpf(lead.cpf) : '—'}
                    </Text>
                    <StatusBadge status={lead.status} />
                  </div>
                  <div className={styles.leadMeta}>
                    <Text variant="caption">Etapa: {getStepLabel(lead.progress.currentStep)}</Text>
                    <Text variant="caption">Atualizado: {formatDateTimeToBrazilian(lead.updatedAt)}</Text>
                  </div>
                  <Button
                    onClick={() => onContinue(lead.id)}
                    loading={busy}
                    disabled={busy}
                    size="sm"
                    className={styles.continueButton}
                  >
                    Continuar este cadastro
                  </Button>
                </Card>
              </li>
            ))}
          </ul>
        ) : (
          <div className={styles.singleActions}>
            <Button variant="secondary" onClick={onStartNew} disabled={busy}>
              Começar novo
            </Button>
            <Button onClick={() => onContinue(leads[0]?.id)} loading={busy} disabled={busy}>
              Continuar de onde parei
            </Button>
          </div>
        )}
      </div>
    </Modal>
  )
}
