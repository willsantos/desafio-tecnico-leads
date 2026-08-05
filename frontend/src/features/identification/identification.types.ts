export interface IdentificationAddressFormValues {
  zipCode: string
  street: string
  number: string
  complement: string
  neighborhood: string
  city: string
  state: string
}

export interface IdentificationFormValues {
  fullName: string
  cpf: string
  birthDate: string
  email: string
  phone: string
  address: IdentificationAddressFormValues
  motherName: string
  maritalStatus: string
  documentType: string
  documentNumber: string
  issuingAuthority: string
  issuingState: string
  issueDate: string
}

/** Values accepted by `documentType` (README seção 5 rota PUT .../identification). */
export const DOCUMENT_TYPES = ['CNH', 'RG'] as const

export const EMPTY_IDENTIFICATION_FORM: IdentificationFormValues = {
  fullName: '',
  cpf: '',
  birthDate: '',
  email: '',
  phone: '',
  address: { zipCode: '', street: '', number: '', complement: '', neighborhood: '', city: '', state: '' },
  motherName: '',
  maritalStatus: '',
  documentType: DOCUMENT_TYPES[0],
  documentNumber: '',
  issuingAuthority: '',
  issuingState: '',
  issueDate: '',
}
