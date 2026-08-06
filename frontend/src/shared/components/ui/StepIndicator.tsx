import styles from './StepIndicator.module.css'

export interface StepIndicatorStep {
  id: string
  label: string
}

interface StepIndicatorProps {
  steps: StepIndicatorStep[]
  currentStepId: string
  completedStepIds: ReadonlySet<string>
  onStepClick?: (stepId: string) => void
}

export function StepIndicator({
  steps,
  currentStepId,
  completedStepIds,
  onStepClick,
}: StepIndicatorProps) {
  return (
    <nav aria-label="Progresso do cadastro">
      <ol className={styles.list}>
        {steps.map((step, index) => {
          const isCurrent = step.id === currentStepId
          const isCompleted = completedStepIds.has(step.id)
          const isClickable = Boolean(onStepClick) && isCompleted && !isCurrent

          return (
            <li key={step.id} className={styles.item}>
              <button
                type="button"
                onClick={isClickable ? () => onStepClick?.(step.id) : undefined}
                disabled={!isClickable}
                aria-current={isCurrent ? 'step' : undefined}
                className={[
                  styles.button,
                  isCurrent ? styles.current : isCompleted ? styles.completed : styles.pending,
                  isClickable ? styles.clickable : '',
                ].join(' ')}
              >
                <span className={styles.circle} aria-hidden="true">
                  {isCompleted && !isCurrent ? '✓' : index + 1}
                </span>
                <span className={styles.label}>{step.label}</span>
              </button>
              {index < steps.length - 1 && <span className={styles.connector} aria-hidden="true" />}
            </li>
          )
        })}
      </ol>
    </nav>
  )
}
