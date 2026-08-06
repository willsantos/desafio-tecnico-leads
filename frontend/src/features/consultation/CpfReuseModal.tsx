import { Button } from '../../shared/components/ui/Button'
import { Modal } from '../../shared/components/ui/Modal'
import { Text } from '../../shared/components/ui/Text'

interface CpfReuseModalProps {
  isOpen: boolean
  leadsFound: number
  onContinue: () => void
  onStartNew: () => void
  busy?: boolean
}

/** P2-1: shown when the CPF typed on etapa 1 already has an active lead. */
export function CpfReuseModal({
  isOpen,
  leadsFound,
  onContinue,
  onStartNew,
  busy,
}: CpfReuseModalProps) {
  return (
    <Modal
      isOpen={isOpen}
      onClose={() => {}}
      title="Já existe um cadastro em andamento"
      footer={
        <>
          <Button variant="secondary" onClick={onStartNew} disabled={busy}>
            Começar novo
          </Button>
          <Button onClick={onContinue} loading={busy}>
            Continuar de onde parei
          </Button>
        </>
      }
    >
      <Text variant="body">
        {leadsFound === 1
          ? 'Encontramos um cadastro em andamento para este CPF.'
          : `Encontramos ${leadsFound} cadastros em andamento para este CPF.`}{' '}
        Deseja continuar de onde parou ou começar um novo cadastro?
      </Text>
    </Modal>
  )
}
