import { httpClient } from '../../shared/api/httpClient'
import type { LeadDto, PagedLeadsResponse } from '../../shared/api/types'

export interface LeadsFilters {
  status?: string
  currentStep?: string
  cpf?: string
}

export function listLeads(filters: LeadsFilters, page = 1, pageSize = 20): Promise<PagedLeadsResponse> {
  const params = new URLSearchParams()
  params.set('page', String(page))
  params.set('pageSize', String(pageSize))

  if (filters.status) params.set('status', filters.status)
  if (filters.currentStep) params.set('currentStep', filters.currentStep)
  if (filters.cpf) params.set('cpf', filters.cpf)

  return httpClient.get<PagedLeadsResponse>(`/leads?${params.toString()}`)
}

export function getLeadById(leadId: string): Promise<LeadDto> {
  return httpClient.get<LeadDto>(`/leads/${leadId}`)
}
