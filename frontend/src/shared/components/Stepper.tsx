export interface StepperStep {
  id: string
  label: string
}

interface StepperProps {
  steps: StepperStep[]
  currentStepId: string
  completedStepIds: ReadonlySet<string>
  /** Omit to render a non-interactive progress indicator. */
  onStepClick?: (stepId: string) => void
}

/** Renders the 6-step progress indicator. Status (current/completed/pending) is derived from
 * `currentStepId`/`completedStepIds` sets rather than per-step boolean props, so adding or
 * reordering steps never grows the component's prop surface. */
export function Stepper({ steps, currentStepId, completedStepIds, onStepClick }: StepperProps) {
  return (
    <ol style={styles.list} aria-label="Progresso do cadastro">
      {steps.map((step, index) => {
        const isCurrent = step.id === currentStepId
        const isCompleted = completedStepIds.has(step.id)
        return (
          <li key={step.id} style={styles.item}>
            <button
              type="button"
              onClick={onStepClick ? () => onStepClick(step.id) : undefined}
              disabled={!onStepClick}
              aria-current={isCurrent ? 'step' : undefined}
              style={{
                ...styles.button,
                ...(isCurrent ? styles.buttonCurrent : isCompleted ? styles.buttonCompleted : styles.buttonPending),
                cursor: onStepClick ? 'pointer' : 'default',
              }}
            >
              <span style={styles.index}>{isCompleted && !isCurrent ? '✓' : index + 1}</span>
              <span>{step.label}</span>
            </button>
          </li>
        )
      })}
    </ol>
  )
}

const styles = {
  list: {
    display: 'flex',
    flexWrap: 'wrap' as const,
    gap: '0.5rem',
    listStyle: 'none',
    padding: 0,
    margin: '0 0 1.5rem',
  },
  item: { margin: 0 },
  button: {
    display: 'flex',
    alignItems: 'center',
    gap: '0.4rem',
    padding: '0.4rem 0.75rem',
    borderRadius: '999px',
    border: '1px solid #ccc',
    background: '#f5f5f5',
    fontSize: '0.85rem',
  },
  buttonCurrent: { background: '#1a56db', color: '#fff', borderColor: '#1a56db' },
  buttonCompleted: { background: '#e6f4ea', borderColor: '#2e7d32', color: '#2e7d32' },
  buttonPending: { background: '#f5f5f5', color: '#555' },
  index: {
    display: 'inline-flex',
    alignItems: 'center',
    justifyContent: 'center',
    width: '1.25rem',
    height: '1.25rem',
    borderRadius: '50%',
    background: 'rgba(0,0,0,0.08)',
    fontSize: '0.75rem',
  },
}
