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

/** Principais bancos do Brasil (código Febraban/COMPE - nome). Valor enviado p/ API no formato "codigo - Nome". */
export const BANKS = [
  { code: '001', name: 'Banco do Brasil' },
  { code: '237', name: 'Banco Bradesco' },
  { code: '341', name: 'Itaú Unibanco' },
  { code: '104', name: 'Caixa Econômica Federal' },
  { code: '033', name: 'Banco Santander' },
  { code: '260', name: 'Nu Pagamentos (Nubank)' },
  { code: '076', name: 'Banco Inter' },
  { code: '212', name: 'Banco Original' },
  { code: '336', name: 'Banco C6' },
  { code: '748', name: 'Sicredi' },
  { code: '756', name: 'Sicoob' },
  { code: '422', name: 'Banco Safra' },
  { code: '655', name: 'Banco Votorantim' },
  { code: '389', name: 'Banco Mercantil do Brasil' },
  { code: '746', name: 'Banco Modal' },
] as const

export const BANK_OPTIONS = BANKS.map((b) => ({
  value: `${b.code} - ${b.name}`,
  label: `${b.code} - ${b.name}`,
}))

export const EMPTY_PROFESSIONAL_BANKING_FORM: ProfessionalBankingDataFormValues = {
  professionalData: { employmentType: '', company: '', registrationNumber: '', role: '', monthlyIncome: '', admissionDate: '' },
  bankingData: { bank: '', agency: '', account: '', accountDigit: '', accountType: '', accountHolder: '', pixKey: '' },
}
