import { createContext, useCallback, useContext, useMemo, useRef, useState, type ReactNode } from 'react'
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
// "professional-banking-data"). `progress.resumeStep` (server-derived, spec progress-resume-step)
// can additionally be "documents" or "confirmation". Exported so App.tsx's
// `deriveCompletedStepIds` can reuse the same mapping instead of re-deriving it as a second,
// independently-drifting copy.
export const CURRENT_STEP_TO_STEP_ID: Record<string, StepId> = {
  consultation: 'consultation',
  simulation: 'simulation',
  identification: 'identification',
  'professional-banking-data': 'professionalBankingData',
  documents: 'documents',
  confirmation: 'confirmation',
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

/** Maps a lead's server-tracked progress to the frontend's local step cursor (used on resume).
 *
 * Preferred source: `progress.resumeStep`, a server-derived field that names the next actionable
 * step (spec PRS-09). Falls back to a local derivation from `completedSteps` for older API
 * responses that predate the field (PRS-10): the backend treats `currentStep` as "last completed
 * step", so we walk the tracked steps and return the first not in `completedSteps`, landing on
 * `documents` once all four backend steps are done. */
export function resolveStepId(lead: LeadDto): StepId {
  if (lead.progress.resumeStep && CURRENT_STEP_TO_STEP_ID[lead.progress.resumeStep]) {
    return CURRENT_STEP_TO_STEP_ID[lead.progress.resumeStep]
  }

  if (CONFIRMATION_REACHED_STATUSES.has(lead.status)) {
    return 'confirmation'
  }
  const completed = new Set(lead.progress.completedSteps ?? [])
  for (const [backendStep, stepId] of Object.entries(CURRENT_STEP_TO_STEP_ID)) {
    if (backendStep === 'documents' || backendStep === 'confirmation') {
      continue
    }
    if (!completed.has(backendStep)) {
      return stepId
    }
  }
  return 'documents'
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

// sessionStorage keeps the in-progress lead/step across F5 in the same tab, but is cleared when
// the tab closes — so a brand-new proposal (new tab, or the "Nova proposta" button via resetLead)
// always starts clean. Middle ground between "persists too aggressively" and "F5 wipes everything".
const LEAD_STORAGE_KEY = 'consignado-leads:lead'
const STEP_STORAGE_KEY = 'consignado-leads:step'

function readStoredLead(): LeadDto | null {
  try {
    const raw = sessionStorage.getItem(LEAD_STORAGE_KEY)
    return raw ? (JSON.parse(raw) as LeadDto) : null
  } catch {
    return null
  }
}

function readStoredStep(): StepId {
  try {
    const raw = sessionStorage.getItem(STEP_STORAGE_KEY)
    return raw && STEP_IDS.includes(raw as StepId) ? (raw as StepId) : 'consultation'
  } catch {
    return 'consultation'
  }
}

function writeStoredLead(lead: LeadDto | null): void {
  try {
    if (lead) sessionStorage.setItem(LEAD_STORAGE_KEY, JSON.stringify(lead))
    else sessionStorage.removeItem(LEAD_STORAGE_KEY)
  } catch {
    // ignore quota / privacy mode errors
  }
}

function writeStoredStep(step: StepId): void {
  try {
    sessionStorage.setItem(STEP_STORAGE_KEY, step)
  } catch {
    // ignore
  }
}

export function LeadProvider({ children }: { children: ReactNode }) {
  const [lead, setLeadState] = useState<LeadDto | null>(readStoredLead)
  const [step, setStepState] = useState<StepId>(readStoredStep)

  // Tracks the lead id the UI currently expects to hold. A background `refreshLead` whose await
  // resolves after the user switched proposals (or hit "Nova proposta") must NOT commit its stale
  // response — otherwise lead A overwrites lead B / the clean state (cubic P1).
  const activeLeadIdRef = useRef<string | null>(lead?.id ?? null)

  const setLead = useCallback((next: LeadDto) => {
    activeLeadIdRef.current = next.id
    setLeadState(next)
    writeStoredLead(next)
  }, [])

  const setStep = useCallback((next: StepId) => {
    setStepState(next)
    writeStoredStep(next)
  }, [])

  const refreshLead = useCallback(async () => {
    if (!lead) {
      return null
    }
    const expectedId = lead.id
    const fresh = await httpClient.get<LeadDto>(`/leads/${expectedId}`)
    // Bail if the user moved on to a different lead (or reset) while this request was in flight.
    if (activeLeadIdRef.current !== expectedId) {
      return null
    }
    setLeadState(fresh)
    writeStoredLead(fresh)
    return fresh
  }, [lead])

  const resetLead = useCallback(() => {
    activeLeadIdRef.current = null
    setLeadState(null)
    setStepState('consultation')
    writeStoredLead(null)
    writeStoredStep('consultation')
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
    [lead, step, setLead, setStep, refreshLead, resetLead],
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
