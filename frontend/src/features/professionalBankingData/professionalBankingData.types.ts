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

export const EMPTY_PROFESSIONAL_BANKING_FORM: ProfessionalBankingDataFormValues = {
  professionalData: { employmentType: '', company: '', registrationNumber: '', role: '', monthlyIncome: '', admissionDate: '' },
  bankingData: { bank: '', agency: '', account: '', accountDigit: '', accountType: '', accountHolder: '', pixKey: '' },
}
