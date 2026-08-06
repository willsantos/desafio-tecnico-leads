import type { LeadDto, LeadSummaryDto } from '../api/types'

export type LeadStatus = LeadDto['status']
export type CurrentStep = LeadSummaryDto['progress']['currentStep']

export const STATUS_LABELS: Record<LeadStatus, string> = {
  draft: 'Rascunho',
  in_progress: 'Em andamento',
  pending_confirmation: 'Aguardando confirmação',
  confirming: 'Confirmando',
  completed: 'Concluído',
  failed_retryable: 'Falhou — pode tentar novamente',
  pending_verification: 'Pendente de verificação',
  abandoned: 'Abandonado',
}

export const STEP_LABELS: Record<CurrentStep, string> = {
  consultation: 'Consulta',
  simulation: 'Simulação',
  identification: 'Identificação',
  'professional-banking-data': 'Dados profissionais',
}

export function getStatusLabel(status: LeadStatus): string {
  return STATUS_LABELS[status] ?? status
}

export function getStepLabel(step: CurrentStep): string {
  return STEP_LABELS[step] ?? step
}
