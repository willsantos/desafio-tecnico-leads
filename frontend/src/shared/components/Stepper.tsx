import { StepIndicator, type StepIndicatorStep } from './ui/StepIndicator'
import { Text } from './ui/Text'
import styles from './Stepper.module.css'

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

export function Stepper({ steps, currentStepId, completedStepIds, onStepClick }: StepperProps) {
  const currentIndex = steps.findIndex((s) => s.id === currentStepId)
  const stepNumber = currentIndex >= 0 ? currentIndex + 1 : 1

  return (
    <div className={styles.wrapper}>
      <div className={styles.compact}>
        <Text variant="label" as="p">
          Passo {stepNumber} de {steps.length}
        </Text>
        <Text variant="subtitle" as="p">
          {steps[currentIndex]?.label ?? ''}
        </Text>
      </div>
      <div className={styles.desktop}>
        <StepIndicator
          steps={steps as StepIndicatorStep[]}
          currentStepId={currentStepId}
          completedStepIds={completedStepIds}
          onStepClick={onStepClick}
        />
      </div>
    </div>
  )
}
