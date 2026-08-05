import { httpClient } from '../../shared/api/httpClient'
import type { SimulationDto } from '../../shared/api/types'

export function createSimulation(leadId: string, requestedAmount: number, installments: number): Promise<SimulationDto> {
  return httpClient.post<SimulationDto>(`/leads/${leadId}/steps/simulation`, { requestedAmount, installments })
}

export function selectSimulation(leadId: string, simulationId: string): Promise<SimulationDto> {
  return httpClient.patch<SimulationDto>(`/leads/${leadId}/steps/simulation/${simulationId}/select`)
}
