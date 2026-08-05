export interface ConsultationFormValues {
  cpf: string
  birthDate: string
  benefitType: string
  benefitNumber: string
  payingInstitution: string
  consultationAuthorized: boolean
}

/** Values accepted by `benefitType` (README seção 5 rota POST /leads/consultation). */
export const BENEFIT_TYPES = [
  { value: 'retirement', label: 'Aposentadoria' },
  { value: 'pension', label: 'Pensão' },
  { value: 'public_servant', label: 'Servidor público' },
  { value: 'clt', label: 'CLT' },
] as const

export const EMPTY_CONSULTATION_FORM: ConsultationFormValues = {
  cpf: '',
  birthDate: '',
  benefitType: BENEFIT_TYPES[0].value,
  benefitNumber: '',
  payingInstitution: '',
  consultationAuthorized: false,
}
