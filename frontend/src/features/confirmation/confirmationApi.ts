import { httpClient } from '../../shared/api/httpClient'
import type { LeadDto } from '../../shared/api/types'

/** 200 (`success`) or 202 (`indeterminate`/`pending_verification`) both resolve with the
 * updated lead. 409/422/503/504 reject with `ApiError` — handled by the caller. */
export function confirmLead(leadId: string): Promise<LeadDto> {
  return httpClient.post<LeadDto>(`/leads/${leadId}/confirm`)
}

export function retrySubmission(leadId: string): Promise<LeadDto> {
  return httpClient.post<LeadDto>(`/leads/${leadId}/retry-submission`)
}
