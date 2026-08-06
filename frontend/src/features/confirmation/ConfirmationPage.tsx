import { useEffect, useState } from 'react'
import { ApiError, getErrorMessage, getPendingReasons } from '../../shared/api/httpClient'
import { LoadingError } from '../../shared/components/LoadingError'
import { useLead } from '../../shared/leadContext'
import { confirmLead, retrySubmission } from './confirmationApi'

const RETRYABLE_STATUSES = new Set([503, 504])

export function ConfirmationPage() {
  const { lead, setLead, refreshLead } = useLead()
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [pendingReasons, setPendingReasons] = useState<string[] | null>(null)
  const [retryable, setRetryable] = useState(false)

  useEffect(() => {
    // Etapa 6's summary needs `documents` (only populated by GET /leads/{id}, not by the
    // create/update responses from earlier steps) — refresh once on entry.
    void refreshLead()
  }, [])

  if (!lead) {
    return <p>Complete as etapas anteriores antes de confirmar o cadastro.</p>
  }

  const activeLeadId = lead.id
  const selectedSimulation = lead.simulations.find((simulation) => simulation.selected) ?? null
  const registrationId = lead.confirmation.finalRegistration?.registrationId ?? null

  function handleFailure(err: unknown) {
    setPendingReasons(null)
    setRetryable(false)

    const reasons = getPendingReasons(err)
    if (reasons) {
      setPendingReasons(reasons)
      return
    }

    if (err instanceof ApiError && RETRYABLE_STATUSES.has(err.status)) {
      setRetryable(true)
    }

    setError(getErrorMessage(err))
  }

  async function handleConfirm() {
    setSubmitting(true)
    setError(null)
    setPendingReasons(null)
    setRetryable(false)
    try {
      const dto = await confirmLead(activeLeadId)
      setLead(dto)
    } catch (err) {
      handleFailure(err)
    } finally {
      setSubmitting(false)
    }
  }

  async function handleRetry() {
    setSubmitting(true)
    setError(null)
    try {
      const dto = await retrySubmission(activeLeadId)
      setLead(dto)
      setRetryable(false)
    } catch (err) {
      handleFailure(err)
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <section>
      <h2>Etapa 6 — Confirmação e efetivação do cadastro</h2>

      <h3>Resumo</h3>

      <section>
        <h4>Consulta</h4>
        {lead.consultation?.result ? (
          <p>
            Resultado: {lead.consultation.result.outcome}
            {lead.consultation.result.availableMargin != null &&
              ` — margem disponível: R$ ${lead.consultation.result.availableMargin.toFixed(2)}`}
          </p>
        ) : (
          <p>Consulta não realizada.</p>
        )}
      </section>

      <section>
        <h4>Simulação selecionada</h4>
        {selectedSimulation ? (
          <p>
            R$ {selectedSimulation.requestedAmount.toFixed(2)} em {selectedSimulation.installments}x de R${' '}
            {selectedSimulation.installmentAmount.toFixed(2)} (total R$ {selectedSimulation.totalAmount.toFixed(2)})
          </p>
        ) : (
          <p>Nenhuma simulação selecionada.</p>
        )}
      </section>

      <section>
        <h4>Identificação</h4>
        {lead.identification ? (
          <p>
            {lead.identification.fullName} — {lead.identification.documentType} {lead.identification.documentNumber} — consulta:{' '}
            {lead.identification.query?.outcome ?? 'não realizada'}
          </p>
        ) : (
          <p>Identificação não preenchida.</p>
        )}
      </section>

      <section>
        <h4>Dados profissionais e bancários</h4>
        {lead.professionalData && lead.bankingData ? (
          <p>
            {lead.professionalData.company} — {lead.professionalData.role} | Banco {lead.bankingData.bank}, ag.{' '}
            {lead.bankingData.agency}, conta {lead.bankingData.account}-{lead.bankingData.accountDigit}
          </p>
        ) : (
          <p>Dados profissionais/bancários não preenchidos.</p>
        )}
      </section>

      <section>
        <h4>Documentos</h4>
        {lead.documents && lead.documents.length > 0 ? (
          <ul>
            {lead.documents.map((doc) => (
              <li key={doc.id}>
                {doc.type} {doc.personalDocumentSubtype && `(${doc.personalDocumentSubtype})`} — {doc.status}
              </li>
            ))}
          </ul>
        ) : (
          <p>Nenhum documento enviado.</p>
        )}
      </section>

      {registrationId ? (
        <p>
          Cadastro efetivado com sucesso! Número de registro: <strong>{registrationId}</strong>
        </p>
      ) : (
        <>
          <button type="button" onClick={handleConfirm} disabled={submitting}>
            Confirmar cadastro
          </button>

          {pendingReasons && (
            <div role="alert">
              <p>Não foi possível confirmar. Pendências:</p>
              <ul>
                {pendingReasons.map((reason) => (
                  <li key={reason}>{reason}</li>
                ))}
              </ul>
            </div>
          )}

          {!pendingReasons && <LoadingError error={error} onRetry={retryable ? handleRetry : undefined} />}
        </>
      )}
    </section>
  )
}
