import type { CSSProperties } from 'react'

interface CpfReuseModalProps {
  leadsFound: number
  onContinue: () => void
  onStartNew: () => void
  busy?: boolean
}

/** P2-1: shown when the CPF typed on etapa 1 already has an active lead. */
export function CpfReuseModal({ leadsFound, onContinue, onStartNew, busy }: CpfReuseModalProps) {
  return (
    <div role="dialog" aria-modal="true" aria-labelledby="cpf-reuse-title" style={overlayStyle}>
      <div style={dialogStyle}>
        <h3 id="cpf-reuse-title">Já existe um cadastro em andamento</h3>
        <p>
          {leadsFound === 1
            ? 'Encontramos um cadastro em andamento para este CPF.'
            : `Encontramos ${leadsFound} cadastros em andamento para este CPF.`}{' '}
          Deseja continuar de onde parou ou começar um novo cadastro?
        </p>
        <div style={{ display: 'flex', gap: '0.5rem', marginTop: '1rem' }}>
          <button type="button" onClick={onContinue} disabled={busy}>
            Continuar de onde parei
          </button>
          <button type="button" onClick={onStartNew} disabled={busy}>
            Começar novo
          </button>
        </div>
      </div>
    </div>
  )
}

const overlayStyle: CSSProperties = {
  position: 'fixed',
  inset: 0,
  background: 'rgba(0,0,0,0.4)',
  display: 'flex',
  alignItems: 'center',
  justifyContent: 'center',
  zIndex: 10,
}

const dialogStyle: CSSProperties = {
  background: '#fff',
  color: '#111',
  padding: '1.5rem',
  borderRadius: 8,
  maxWidth: 420,
}
