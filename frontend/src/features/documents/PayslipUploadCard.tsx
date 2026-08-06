import { useState, type FormEvent } from 'react'
import { getErrorMessage } from '../../shared/api/httpClient'
import { LoadingError } from '../../shared/components/LoadingError'
import { uploadDocument } from './documentsApi'

interface PayslipUploadCardProps {
  leadId: string
  onUploaded: () => void
}

/** Independent upload widget for `type=payslip` — no subtype field (only `personal_document`
 * has one), kept as its own component rather than a boolean branch on a shared one. */
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
    <form onSubmit={handleSubmit} style={{ border: '1px solid #ccc', borderRadius: 4, padding: '1rem' }}>
      <h3>Contracheque</h3>
      <label>
        Arquivo (JPEG, PNG ou PDF, até 10MB)
        <input
          type="file"
          accept="image/jpeg,image/png,application/pdf"
          onChange={(e) => setFile(e.target.files?.[0] ?? null)}
        />
      </label>
      <button type="submit" disabled={submitting}>
        Enviar contracheque
      </button>
      <LoadingError error={error} />
    </form>
  )
}
