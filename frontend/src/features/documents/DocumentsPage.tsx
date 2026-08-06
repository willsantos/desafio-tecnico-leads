import { useCallback, useEffect, useState } from 'react'
import { getErrorMessage } from '../../shared/api/httpClient'
import type { DocumentDto } from '../../shared/api/types'
import { LoadingError } from '../../shared/components/LoadingError'
import { useLead } from '../../shared/leadContext'
import { PayslipUploadCard } from './PayslipUploadCard'
import { PersonalDocumentUploadCard } from './PersonalDocumentUploadCard'
import { deleteDocument, listDocuments } from './documentsApi'

const TYPE_LABELS: Record<string, string> = {
  personal_document: 'Documento pessoal',
  payslip: 'Contracheque',
}

export function DocumentsPage() {
  const { lead, setStep } = useLead()
  const leadId = lead?.id ?? null

  const [documents, setDocuments] = useState<DocumentDto[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const loadDocuments = useCallback(async () => {
    if (!leadId) {
      return
    }
    setLoading(true)
    setError(null)
    try {
      const items = await listDocuments(leadId)
      setDocuments(items)
    } catch (err) {
      setError(getErrorMessage(err))
    } finally {
      setLoading(false)
    }
  }, [leadId])

  useEffect(() => {
    void loadDocuments()
  }, [loadDocuments])

  if (!lead) {
    return <p>Complete as etapas anteriores antes de enviar anexos.</p>
  }

  const activeLeadId = lead.id

  async function handleDelete(documentId: string) {
    setError(null)
    try {
      await deleteDocument(activeLeadId, documentId)
      await loadDocuments()
    } catch (err) {
      setError(getErrorMessage(err))
    }
  }

  const hasPersonalDocument = documents.some((doc) => doc.type === 'personal_document')
  const hasPayslip = documents.some((doc) => doc.type === 'payslip')

  return (
    <section>
      <h2>Etapa 5 — Anexos</h2>
      <p>Envie o documento pessoal e o contracheque separadamente — cada envio é independente.</p>

      <div style={{ display: 'flex', gap: '1.5rem', flexWrap: 'wrap' }}>
        <PersonalDocumentUploadCard
          leadId={activeLeadId}
          defaultSubtype={lead.identification?.documentType}
          onUploaded={loadDocuments}
        />
        <PayslipUploadCard leadId={activeLeadId} onUploaded={loadDocuments} />
      </div>

      <LoadingError loading={loading} error={error} onRetry={loadDocuments}>
        {documents.length === 0 ? (
          <p>Nenhum documento enviado ainda.</p>
        ) : (
          <ul>
            {documents.map((doc) => (
              <li key={doc.id}>
                {TYPE_LABELS[doc.type] ?? doc.type}
                {doc.personalDocumentSubtype && ` (${doc.personalDocumentSubtype})`} — {doc.status}{' '}
                <button type="button" onClick={() => handleDelete(doc.id)}>
                  Remover
                </button>
              </li>
            ))}
          </ul>
        )}
      </LoadingError>

      <button type="button" disabled={!hasPersonalDocument || !hasPayslip} onClick={() => setStep('confirmation')}>
        Avançar para confirmação
      </button>
    </section>
  )
}
