import { httpClient } from '../../shared/api/httpClient'
import type { LeadDto } from '../../shared/api/types'
import type { IdentificationFormValues } from './identification.types'

export function submitIdentification(
  leadId: string,
  values: IdentificationFormValues,
  expectedVersion?: number,
): Promise<LeadDto> {
  return httpClient.put<LeadDto>(`/leads/${leadId}/steps/identification`, { ...values, expectedVersion })
}
