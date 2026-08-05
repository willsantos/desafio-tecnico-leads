import { httpClient } from '../../shared/api/httpClient'
import type { LeadDto } from '../../shared/api/types'

export interface ProfessionalBankingDataRequestBody {
  professionalData: {
    employmentType: string
    company: string
    registrationNumber: string
    role: string
    monthlyIncome: number
    admissionDate: string
  }
  bankingData: {
    bank: string
    agency: string
    account: string
    accountDigit: string
    accountType: string
    accountHolder: string
    pixKey?: string
  }
  expectedVersion?: number
}

export function submitProfessionalBankingData(leadId: string, body: ProfessionalBankingDataRequestBody): Promise<LeadDto> {
  return httpClient.put<LeadDto>(`/leads/${leadId}/steps/professional-banking-data`, body)
}
