import { useState } from 'react'
import { getErrorMessage } from '../../shared/api/httpClient'
import { Alert } from '../../shared/components/ui/Alert'
import { Card } from '../../shared/components/ui/Card'
import { FileUpload } from '../../shared/components/ui/FileUpload'
import { Select } from '../../shared/components/ui/Select'
import { PERSONAL_DOCUMENT_SUBTYPES } from './documents.types'
import { uploadDocument } from './documentsApi'
import styles from './UploadCard.module.css'

interface PersonalDocumentUploadCardProps {
  leadId: string
  /** Pre-selects the subtype matching etapa 3's `documentType`, when known. */
  defaultSubtype?: string
  onUploaded: () => void
}

export function PersonalDocumentUploadCard({ leadId, defaultSubtype, onUploaded }: PersonalDocumentUploadCardProps) {
  const [file, setFile] = useState<File | null>(null)
  const [subtype, setSubtype] = useState(defaultSubtype ?? PERSONAL_DOCUMENT_SUBTYPES[0])
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function handleFileSelect(selected: File | null) {
    setFile(selected)
    if (!selected) {
      setError(null)
      return
    }
    setError(null)
    setSubmitting(true)
    try {
      await uploadDocument(leadId, selected, 'personal_document', subtype)
      setFile(null)
      onUploaded()
    } catch (err) {
      setError(getErrorMessage(err))
      setFile(null)
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Card title="Documento pessoal" className={styles.card}>
      <Select
        label="Tipo de documento"
        name="personalDocumentSubtype"
        value={subtype}
        onChange={(e) => setSubtype(e.target.value)}
        options={PERSONAL_DOCUMENT_SUBTYPES.map((type) => ({ value: type, label: type }))}
        required
        disabled={submitting}
      />
      <FileUpload
        id="personal-document"
        label="Arquivo"
        description="Selecione o arquivo para enviar automaticamente. JPEG, PNG ou PDF, até 10MB."
        accept="image/jpeg,image/png,application/pdf"
        selectedFile={file}
        onFileSelect={handleFileSelect}
        disabled={submitting}
      />
      {error && (
        <Alert variant="error" title="Erro no envio">
          {error}
        </Alert>
      )}
    </Card>
  )
}
