import { useState, type FormEvent } from 'react'
import { getErrorMessage } from '../../shared/api/httpClient'
import { LoadingError } from '../../shared/components/LoadingError'
import { PERSONAL_DOCUMENT_SUBTYPES } from './documents.types'
import { uploadDocument } from './documentsApi'

interface PersonalDocumentUploadCardProps {
  leadId: string
  /** Pre-selects the subtype matching etapa 3's `documentType`, when known. */
  defaultSubtype?: string
  onUploaded: () => void
}

/** Independent upload widget for `type=personal_document` — has its own file/subtype/submitting
 * state, so it can be filled and sent without the payslip upload being ready (spec P1-5 AC9). */
export function PersonalDocumentUploadCard({ leadId, defaultSubtype, onUploaded }: PersonalDocumentUploadCardProps) {
  const [file, setFile] = useState<File | null>(null)
  const [subtype, setSubtype] = useState(defaultSubtype ?? PERSONAL_DOCUMENT_SUBTYPES[0])
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
      await uploadDocument(leadId, file, 'personal_document', subtype)
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
      <h3>Documento pessoal</h3>
      <label>
        Tipo
        <select value={subtype} onChange={(e) => setSubtype(e.target.value)}>
          {PERSONAL_DOCUMENT_SUBTYPES.map((type) => (
            <option key={type} value={type}>
              {type}
            </option>
          ))}
        </select>
      </label>
      <label>
        Arquivo (JPEG, PNG ou PDF, até 10MB)
        <input
          type="file"
          accept="image/jpeg,image/png,application/pdf"
          onChange={(e) => setFile(e.target.files?.[0] ?? null)}
        />
      </label>
      <button type="submit" disabled={submitting}>
        Enviar documento pessoal
      </button>
      <LoadingError error={error} />
    </form>
  )
}
