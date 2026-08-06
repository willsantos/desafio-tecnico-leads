import { useMemo, type ComponentType } from 'react'
import { Route, Routes } from 'react-router-dom'
import { LeadsListPage } from './features/leads/LeadsListPage'
import { ConfirmationPage } from './features/confirmation/ConfirmationPage'
import { ConsultationPage } from './features/consultation/ConsultationPage'
import { DocumentsPage } from './features/documents/DocumentsPage'
import { IdentificationPage } from './features/identification/IdentificationPage'
import { ProfessionalBankingDataPage } from './features/professionalBankingData/ProfessionalBankingDataPage'
import { SimulationPage } from './features/simulation/SimulationPage'
import type { ProgressDto } from './shared/api/types'
import { Header } from './shared/components/layout/Header'
import { Stepper, type StepperStep } from './shared/components/Stepper'
import { Text } from './shared/components/ui/Text'
import { CURRENT_STEP_TO_STEP_ID, LeadProvider, STEP_IDS, useLead, type StepId } from './shared/leadContext'
import styles from './App.module.css'

const STEP_LABELS: Record<StepId, string> = {
  consultation: 'Consulta',
  simulation: 'Simulação',
  identification: 'Identificação',
  professionalBankingData: 'Dados profissionais',
  documents: 'Anexos',
  confirmation: 'Confirmação',
}

const STEP_PAGES: Record<StepId, ComponentType> = {
  consultation: ConsultationPage,
  simulation: SimulationPage,
  identification: IdentificationPage,
  professionalBankingData: ProfessionalBankingDataPage,
  documents: DocumentsPage,
  confirmation: ConfirmationPage,
}

const STEPPER_STEPS: StepperStep[] = STEP_IDS.map((id) => ({ id, label: STEP_LABELS[id] }))

// `progress.completedSteps` only ever names these 4 (backend never tracks documents/confirmation
// there — see leadContext's CURRENT_STEP_TO_STEP_ID comment). Steps before the local step cursor
// are treated as completed too, so the stepper reflects steps the user has already passed even
// when the backend has nothing to say about them.
function deriveCompletedStepIds(step: StepId, progress: ProgressDto | null, hasFinalRegistration: boolean): Set<StepId> {
  const completed = new Set<StepId>()
  const backendCompleted = progress?.completedSteps ?? []
  for (const [backendStep, stepId] of Object.entries(CURRENT_STEP_TO_STEP_ID)) {
    if (backendCompleted.includes(backendStep)) completed.add(stepId)
  }

  const currentIndex = STEP_IDS.indexOf(step)
  STEP_IDS.slice(0, currentIndex).forEach((id) => completed.add(id))
  if (hasFinalRegistration) completed.add('confirmation')

  return completed
}

function WizardPage() {
  const { lead, step, setStep } = useLead()

  const completedStepIds = useMemo(
    () => deriveCompletedStepIds(step, lead?.progress ?? null, Boolean(lead?.confirmation.finalRegistration)),
    [step, lead?.progress, lead?.confirmation.finalRegistration],
  )

  const currentIndex = STEP_IDS.indexOf(step)

  function handleStepClick(stepId: string) {
    const targetIndex = STEP_IDS.indexOf(stepId as StepId)
    if (targetIndex >= 0 && targetIndex <= currentIndex) {
      setStep(stepId as StepId)
    }
  }

  const StepPage = STEP_PAGES[step]

  return (
    <>
      <Stepper
        steps={STEPPER_STEPS}
        currentStepId={step}
        completedStepIds={completedStepIds}
        onStepClick={lead ? handleStepClick : undefined}
      />
      <section className={styles.content}>
        <StepPage />
      </section>
    </>
  )
}

function AppShell() {
  return (
    <div className={styles.layout}>
      <header className={styles.header}>
        <Text variant="display" as="h1" className={styles.title}>
          Recuperação de Leads
        </Text>
        <Text variant="muted" as="p">
          Empréstimo Consignado
        </Text>
      </header>
      <Header />
      <main className={styles.main}>
        <Routes>
          <Route path="/" element={<WizardPage />} />
          <Route path="/leads" element={<LeadsListPage />} />
        </Routes>
      </main>
    </div>
  )
}

export default function App() {
  return (
    <LeadProvider>
      <AppShell />
    </LeadProvider>
  )
}
