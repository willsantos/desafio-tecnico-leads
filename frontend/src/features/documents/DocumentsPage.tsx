import { useCallback, useEffect, useState } from 'react'
import { getErrorMessage } from '../../shared/api/httpClient'
import type { DocumentDto } from '../../shared/api/types'
import { Alert } from '../../shared/components/ui/Alert'
import { Button } from '../../shared/components/ui/Button'
import { Card } from '../../shared/components/ui/Card'
import { Text } from '../../shared/components/ui/Text'
import { LoadingError } from '../../shared/components/LoadingError'
import { useLead } from '../../shared/leadContext'
import { PayslipUploadCard } from './PayslipUploadCard'
import { PersonalDocumentUploadCard } from './PersonalDocumentUploadCard'
import { deleteDocument, listDocuments } from './documentsApi'
import styles from './DocumentsPage.module.css'

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
    return (
      <Alert variant="warning" title="Etapa anterior não concluída">
        Complete as etapas anteriores antes de enviar anexos.
      </Alert>
    )
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
    <section className={styles.wrapper}>
      <Text variant="title" as="h2" className={styles.heading}>
        Anexos
      </Text>
      <Text variant="body">
        Envie o documento pessoal e o contracheque separadamente — cada envio é independente.
      </Text>

      <div className={styles.uploadGrid}>
        <PersonalDocumentUploadCard
          leadId={activeLeadId}
          defaultSubtype={lead.identification?.documentType}
          onUploaded={loadDocuments}
        />
        <PayslipUploadCard leadId={activeLeadId} onUploaded={loadDocuments} />
      </div>

      <LoadingError loading={loading} error={error} onRetry={loadDocuments}>
        {documents.length === 0 ? (
          <Alert variant="info" title="Nenhum documento enviado">
            Envie pelo menos um documento pessoal e um contracheque para prosseguir.
          </Alert>
        ) : (
          <div className={styles.list}>
            <Text variant="subtitle" as="h3">
              Documentos enviados
            </Text>
            {documents.map((doc) => (
              <Card key={doc.id} className={styles.documentCard}>
                <div className={styles.documentInfo}>
                  <Text variant="body">
                    {TYPE_LABELS[doc.type] ?? doc.type}
                    {doc.personalDocumentSubtype && ` (${doc.personalDocumentSubtype})`}
                  </Text>
                  <Text variant="caption">{doc.status}</Text>
                </div>
                <Button variant="ghost" size="sm" onClick={() => handleDelete(doc.id)}>
                  Remover
                </Button>
              </Card>
            ))}
          </div>
        )}
      </LoadingError>

      <div className={styles.actions}>
        <Button
          variant="primary"
          onClick={() => setStep('confirmation')}
          disabled={!hasPersonalDocument || !hasPayslip}
        >
          Avançar para confirmação
        </Button>
      </div>
    </section>
  )
}
