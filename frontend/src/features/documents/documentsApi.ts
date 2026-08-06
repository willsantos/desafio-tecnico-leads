import { httpClient } from '../../shared/api/httpClient'
import type { DocumentDto } from '../../shared/api/types'

export function uploadDocument(
  leadId: string,
  file: File,
  type: 'personal_document' | 'payslip',
  personalDocumentSubtype?: string,
): Promise<DocumentDto> {
  const form = new FormData()
  form.append('file', file)
  form.append('type', type)
  if (personalDocumentSubtype) {
    form.append('personalDocumentSubtype', personalDocumentSubtype)
  }
  return httpClient.postForm<DocumentDto>(`/leads/${leadId}/documents`, form)
}

export function listDocuments(leadId: string): Promise<DocumentDto[]> {
  return httpClient.get<DocumentDto[]>(`/leads/${leadId}/documents`)
}

export function deleteDocument(leadId: string, documentId: string): Promise<void> {
  return httpClient.delete<void>(`/leads/${leadId}/documents/${documentId}`)
}
