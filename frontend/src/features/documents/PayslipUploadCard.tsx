import { useState } from 'react'
import { getErrorMessage } from '../../shared/api/httpClient'
import { Alert } from '../../shared/components/ui/Alert'
import { Card } from '../../shared/components/ui/Card'
import { FileUpload } from '../../shared/components/ui/FileUpload'
import { uploadDocument } from './documentsApi'
import styles from './UploadCard.module.css'

interface PayslipUploadCardProps {
  leadId: string
  onUploaded: () => void
}

export function PayslipUploadCard({ leadId, onUploaded }: PayslipUploadCardProps) {
  const [file, setFile] = useState<File | null>(null)
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
      await uploadDocument(leadId, selected, 'payslip')
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
    <Card title="Contracheque" className={styles.card}>
      <FileUpload
        id="payslip"
        label="Arquivo"
        description="Selecione o arquivo para enviar automaticamente. JPEG, PNG ou PDF, até 10MB."
        accept="image/jpeg,image/png,application/pdf"
        selectedFile={file}
        onFileSelect={handleFileSelect}
        disabled={submitting}
        loading={submitting}
      />
      {error && (
        <Alert variant="error" title="Erro no envio">
          {error}
        </Alert>
      )}
    </Card>
  )
}
