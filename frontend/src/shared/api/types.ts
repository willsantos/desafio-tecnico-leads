// Mirrors `backend/src/ConsignadoLeads.Api/Core/Dtos/LeadDto.cs` and `DocumentDto.cs` field
// for field (the frozen API contract, README seção 5). Shared across every feature that reads
// or displays a lead — same rationale LeadDto.cs itself documents: one contract shape defined
// once beats N independently drifting copies.

export interface ProgressDto {
  currentStep: string
  startedSteps: string[]
  completedSteps: string[]
  pendingItems: string[]
  lastUpdatedAt: string
}

export interface ConsultationInputDto {
  cpf: string
  birthDate: string
  benefitType: string
  benefitNumber: string
  payingInstitution: string
}

export interface ConsultationResultDto {
  outcome: string
  availableMargin: number | null
  checkedAt: string
}

export interface ConsultationDto {
  input: ConsultationInputDto
  result: ConsultationResultDto | null
}

export interface SimulationDto {
  id: string
  requestedAmount: number
  installments: number
  interestRate: number
  installmentAmount: number
  totalAmount: number
  selected: boolean
  simulatedAt: string
}

export interface AddressDto {
  zipCode: string
  street: string
  number: string
  complement?: string | null
  neighborhood: string
  city: string
  state: string
}

export interface IdentificationQueryDto {
  outcome: string
}

export interface IdentificationDataDto {
  fullName: string
  cpf: string
  birthDate: string
  email: string
  phone: string
  address: AddressDto
  motherName: string
  maritalStatus: string
  documentType: string
  documentNumber: string
  issuingAuthority: string
  issuingState: string
  issueDate: string
  query: IdentificationQueryDto | null
}

export interface ProfessionalDataDto {
  employmentType: string
  company: string
  registrationNumber: string
  role: string
  monthlyIncome: number
  admissionDate: string
}

export interface BankingDataDto {
  bank: string
  agency: string
  account: string
  accountDigit: string
  accountType: string
  accountHolder: string
  pixKey?: string | null
}

export interface ConfirmationAttemptDto {
  attemptedAt: string
  outcome: string
  reason?: string | null
}

export interface FinalRegistrationDto {
  registrationId: string
  completedAt: string
}

export interface ConfirmationDto {
  confirmedAt: string | null
  attempts: ConfirmationAttemptDto[]
  finalRegistration: FinalRegistrationDto | null
}

export interface DocumentDto {
  id: string
  leadId: string
  type: 'personal_document' | 'payslip'
  personalDocumentSubtype: string | null
  status: string
  uploadedAt: string
}

export interface LeadDto {
  id: string
  status: string
  version: number
  progress: ProgressDto
  consultation: ConsultationDto | null
  simulations: SimulationDto[]
  identification: IdentificationDataDto | null
  professionalData: ProfessionalDataDto | null
  bankingData: BankingDataDto | null
  confirmation: ConfirmationDto
  createdAt: string
  updatedAt: string
  documents?: DocumentDto[] | null
}

export interface LeadSummaryDto {
  id: string
  status: string
  cpf: string
  progress: { currentStep: string }
  createdAt: string
  updatedAt: string
  finalRegistration: { registrationId: string } | null
}

export interface PagedLeadsResponse {
  items: LeadSummaryDto[]
  page: number
  pageSize: number
  totalItems: number
  totalPages: number
}
