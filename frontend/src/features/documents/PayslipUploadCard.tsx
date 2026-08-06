import { useState, type FormEvent } from 'react'
import { getErrorMessage } from '../../shared/api/httpClient'
import { Alert } from '../../shared/components/ui/Alert'
import { Button } from '../../shared/components/ui/Button'
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

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!file) {
      setError('Selecione um arquivo.')
      return
    }
    setSubmitting(true)
    setError(null)
    try {
      await uploadDocument(leadId, file, 'payslip')
      setFile(null)
      onUploaded()
    } catch (err) {
      setError(getErrorMessage(err))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Card title="Contracheque" className={styles.card}>
      <form onSubmit={handleSubmit} className={styles.form}>
        <FileUpload
          id="payslip"
          label="Arquivo"
          description="JPEG, PNG ou PDF, até 10MB"
          accept="image/jpeg,image/png,application/pdf"
          selectedFile={file}
          onFileSelect={setFile}
          disabled={submitting}
        />
        {error && (
          <Alert variant="error" title="Erro no envio">
            {error}
          </Alert>
        )}
        <div className={styles.actions}>
          <Button type="submit" loading={submitting} disabled={!file || submitting}>
            Enviar contracheque
          </Button>
        </div>
      </form>
    </Card>
  )
}
