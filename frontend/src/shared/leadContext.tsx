import { createContext, useCallback, useContext, useMemo, useState, type ReactNode } from 'react'
import { httpClient } from './api/httpClient'
import type { LeadDto, ProgressDto } from './api/types'

/** The 6 wizard steps (design.md "Frontend — src/features/{step}/"), in flow order. */
export const STEP_IDS = [
  'consultation',
  'simulation',
  'identification',
  'professionalBankingData',
  'documents',
  'confirmation',
] as const

export type StepId = (typeof STEP_IDS)[number]

// `progress.currentStep` only ever holds one of these 4 values (backend never advances it past
// "professional-banking-data" — documents/confirmation have no step-tracking field of their own).
const CURRENT_STEP_TO_STEP_ID: Record<string, StepId> = {
  consultation: 'consultation',
  simulation: 'simulation',
  identification: 'identification',
  'professional-banking-data': 'professionalBankingData',
}

// A lead in any of these statuses has already reached (or passed) etapa 6 at least once, so
// resuming it should land on the confirmation step rather than back on professionalBankingData.
const CONFIRMATION_REACHED_STATUSES = new Set([
  'pending_confirmation',
  'confirming',
  'pending_verification',
  'failed_retryable',
  'completed',
])

/** Maps a lead's server-tracked progress to the frontend's local step cursor (used on resume). */
export function resolveStepId(lead: LeadDto): StepId {
  if (CONFIRMATION_REACHED_STATUSES.has(lead.status)) {
    return 'confirmation'
  }
  return CURRENT_STEP_TO_STEP_ID[lead.progress.currentStep] ?? 'consultation'
}

interface LeadContextValue {
  /** Full lead as last fetched/returned by the API; null before etapa 1 creates one. */
  lead: LeadDto | null
  leadId: string | null
  progress: ProgressDto | null
  /** Local UI step cursor — starts synced to `progress.currentStep` on load/resume, then
   * advances independently as the user completes documents/confirmation (steps the backend
   * doesn't track in `progress`). */
  step: StepId
  setLead: (lead: LeadDto) => void
  setStep: (step: StepId) => void
  /** Re-fetches `GET /leads/{id}` and replaces the stored lead (e.g. after an action that
   * only returns a sub-resource, like a simulation or a document). */
  refreshLead: () => Promise<LeadDto | null>
  resetLead: () => void
}

const LeadContext = createContext<LeadContextValue | undefined>(undefined)

export function LeadProvider({ children }: { children: ReactNode }) {
  const [lead, setLeadState] = useState<LeadDto | null>(null)
  const [step, setStep] = useState<StepId>('consultation')

  const setLead = useCallback((next: LeadDto) => {
    setLeadState(next)
  }, [])

  const refreshLead = useCallback(async () => {
    if (!lead) {
      return null
    }
    const fresh = await httpClient.get<LeadDto>(`/leads/${lead.id}`)
    setLeadState(fresh)
    return fresh
  }, [lead])

  const resetLead = useCallback(() => {
    setLeadState(null)
    setStep('consultation')
  }, [])

  const value = useMemo<LeadContextValue>(
    () => ({
      lead,
      leadId: lead?.id ?? null,
      progress: lead?.progress ?? null,
      step,
      setLead,
      setStep,
      refreshLead,
      resetLead,
    }),
    [lead, step, setLead, refreshLead, resetLead],
  )

  return <LeadContext.Provider value={value}>{children}</LeadContext.Provider>
}

export function useLead(): LeadContextValue {
  const context = useContext(LeadContext)
  if (!context) {
    throw new Error('useLead must be used within a LeadProvider')
  }
  return context
}
