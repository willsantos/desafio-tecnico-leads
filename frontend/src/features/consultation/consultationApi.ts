import { httpClient } from '../../shared/api/httpClient'
import type { LeadDto, PagedLeadsResponse } from '../../shared/api/types'
import type { ConsultationFormValues } from './consultation.types'

// P2-1 (context.md "Duplicidade de CPF"): the exact status list a lead must be in to count as
// "active" for the CPF-reuse modal.
const ACTIVE_STATUSES = [
  'draft',
  'in_progress',
  'pending_confirmation',
  'confirming',
  'failed_retryable',
  'pending_verification',
]

export function findActiveLeadsByCpf(cpf: string): Promise<PagedLeadsResponse> {
  const query = new URLSearchParams({ cpf, status: ACTIVE_STATUSES.join(',') })
  return httpClient.get<PagedLeadsResponse>(`/leads?${query.toString()}`)
}

export function createConsultation(values: ConsultationFormValues): Promise<LeadDto> {
  return httpClient.post<LeadDto>('/leads/consultation', values)
}

export function updateConsultation(leadId: string, values: ConsultationFormValues, expectedVersion?: number): Promise<LeadDto> {
  return httpClient.put<LeadDto>(`/leads/${leadId}/steps/consultation`, { ...values, expectedVersion })
}

export function getLeadById(leadId: string): Promise<LeadDto> {
  return httpClient.get<LeadDto>(`/leads/${leadId}`)
}
