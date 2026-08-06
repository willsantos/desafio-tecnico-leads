export interface ProfessionalDataFormValues {
  employmentType: string
  company: string
  registrationNumber: string
  role: string
  monthlyIncome: string
  admissionDate: string
}

export interface BankingDataFormValues {
  bank: string
  agency: string
  account: string
  accountDigit: string
  accountType: string
  accountHolder: string
  pixKey: string
}

export interface ProfessionalBankingDataFormValues {
  professionalData: ProfessionalDataFormValues
  bankingData: BankingDataFormValues
}

export const EMPLOYMENT_TYPES = [
  { value: 'efetivo', label: 'Efetivo' },
  { value: 'estagiario', label: 'Estagiário' },
  { value: 'temporario', label: 'Temporário' },
  { value: 'pj', label: 'PJ' },
  { value: 'autonomo', label: 'Autônomo' },
  { value: 'aposentado', label: 'Aposentado' },
  { value: 'pensionista', label: 'Pensionista' },
  { value: 'outro', label: 'Outro' },
] as const

export const ACCOUNT_TYPES = [
  { value: 'corrente', label: 'Conta corrente' },
  { value: 'poupanca', label: 'Conta poupança' },
  { value: 'salario', label: 'Conta salário' },
  { value: 'outra', label: 'Outra' },
] as const

export const EMPTY_PROFESSIONAL_BANKING_FORM: ProfessionalBankingDataFormValues = {
  professionalData: { employmentType: '', company: '', registrationNumber: '', role: '', monthlyIncome: '', admissionDate: '' },
  bankingData: { bank: '', agency: '', account: '', accountDigit: '', accountType: '', accountHolder: '', pixKey: '' },
}
