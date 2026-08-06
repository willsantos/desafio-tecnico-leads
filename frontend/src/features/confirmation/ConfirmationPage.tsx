import { useEffect, useState } from 'react'
import { ApiError, getErrorMessage, getPendingReasons } from '../../shared/api/httpClient'
import { Alert } from '../../shared/components/ui/Alert'
import { Button } from '../../shared/components/ui/Button'
import { Card } from '../../shared/components/ui/Card'
import { Text } from '../../shared/components/ui/Text'
import { LoadingError } from '../../shared/components/LoadingError'
import { useLead } from '../../shared/leadContext'
import { formatDateToBrazilian, maskCpf, maskPhone } from '../../shared/utils/formatters'
import { confirmLead, retrySubmission } from './confirmationApi'
import styles from './ConfirmationPage.module.css'

const RETRYABLE_STATUSES = new Set([503, 504])

const TYPE_LABELS: Record<string, string> = {
  personal_document: 'Documento pessoal',
  payslip: 'Contracheque',
}

function formatCurrency(value: number | null | undefined): string {
  if (value == null) return ''
  return new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(value)
}

export function ConfirmationPage() {
  const { lead, setLead, refreshLead } = useLead()
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [pendingReasons, setPendingReasons] = useState<string[] | null>(null)
  const [retryable, setRetryable] = useState(false)

  useEffect(() => {
    void refreshLead()
  }, [])

  if (!lead) {
    return (
      <Alert variant="warning" title="Etapa anterior não concluída">
        Complete as etapas anteriores antes de confirmar o cadastro.
      </Alert>
    )
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
    <section className={styles.wrapper}>
      <Text variant="title" as="h2" className={styles.heading}>
        Confirmação e efetivação
      </Text>

      <div className={styles.summaryGrid}>
        <Card title="Consulta">
          {lead.consultation?.result ? (
            <div className={styles.summaryBody}>
              <Text variant="body">
                Resultado: <strong>{lead.consultation.result.outcome}</strong>
              </Text>
              {lead.consultation.result.availableMargin != null && (
                <Text variant="body">
                  Margem disponível: <strong>{formatCurrency(lead.consultation.result.availableMargin)}</strong>
                </Text>
              )}
            </div>
          ) : (
            <Text variant="muted">Consulta não realizada.</Text>
          )}
        </Card>

        <Card title="Simulação selecionada" variant={selectedSimulation ? 'primary' : 'default'}>
          {selectedSimulation ? (
            <div className={styles.simulation}>
              <Text variant="subtitle">{formatCurrency(selectedSimulation.requestedAmount)}</Text>
              <Text variant="body">
                {selectedSimulation.installments}x de {formatCurrency(selectedSimulation.installmentAmount)}
              </Text>
              <Text variant="caption">Total: {formatCurrency(selectedSimulation.totalAmount)}</Text>
            </div>
          ) : (
            <Text variant="muted">Nenhuma simulação selecionada.</Text>
          )}
        </Card>

        <Card title="Identificação">
          {lead.identification ? (
            <div className={styles.summaryBody}>
              <Text variant="body">
                <strong>{lead.identification.fullName}</strong>
              </Text>
              <Text variant="body">
                CPF: <strong>{maskCpf(lead.identification.cpf)}</strong>
              </Text>
              <Text variant="body">
                Nascimento: <strong>{formatDateToBrazilian(lead.identification.birthDate)}</strong>
              </Text>
              <Text variant="body">
                Telefone: <strong>{maskPhone(lead.identification.phone)}</strong>
              </Text>
              <Text variant="body">
                {lead.identification.documentType} {lead.identification.documentNumber}
              </Text>
              <Text variant="caption">
                Emissão: {formatDateToBrazilian(lead.identification.issueDate)}
              </Text>
              <Text variant="caption">
                Consulta: {lead.identification.query?.outcome ?? 'não realizada'}
              </Text>
            </div>
          ) : (
            <Text variant="muted">Identificação não preenchida.</Text>
          )}
        </Card>

        <Card title="Dados profissionais e bancários">
          {lead.professionalData && lead.bankingData ? (
            <div className={styles.summaryBody}>
              <Text variant="body">
                <strong>{lead.professionalData.company}</strong> — {lead.professionalData.role}
              </Text>
              <Text variant="caption">
                Admissão: {formatDateToBrazilian(lead.professionalData.admissionDate)}
              </Text>
              <Text variant="caption">
                Banco {lead.bankingData.bank}, ag. {lead.bankingData.agency}, conta {lead.bankingData.account}-
                {lead.bankingData.accountDigit}
              </Text>
            </div>
          ) : (
            <Text variant="muted">Dados profissionais/bancários não preenchidos.</Text>
          )}
        </Card>

        <Card title="Documentos" className={styles.span2}>
          {lead.documents && lead.documents.length > 0 ? (
            <ul className={styles.documentList}>
              {lead.documents.map((doc) => (
                <li key={doc.id} className={styles.documentItem}>
                  <Text variant="body">
                    {TYPE_LABELS[doc.type] ?? doc.type} {doc.personalDocumentSubtype && `(${doc.personalDocumentSubtype})`}
                  </Text>
                  <Text variant="caption">{doc.status}</Text>
                </li>
              ))}
            </ul>
          ) : (
            <Text variant="muted">Nenhum documento enviado.</Text>
          )}
        </Card>
      </div>

      {registrationId ? (
        <Alert variant="success" title="Cadastro efetivado com sucesso!">
          <Text variant="body">
            Número de registro: <strong>{registrationId}</strong>
          </Text>
        </Alert>
      ) : (
        <div className={styles.actions}>
          <Button type="button" onClick={handleConfirm} loading={submitting} disabled={submitting} size="lg">
            Confirmar cadastro
          </Button>
        </div>
      )}

      {pendingReasons && (
        <Alert variant="error" title="Não foi possível confirmar. Pendências:">
          <ul className={styles.reasonsList}>
            {pendingReasons.map((reason) => (
              <li key={reason}>
                <Text variant="body">{reason}</Text>
              </li>
            ))}
          </ul>
        </Alert>
      )}

      {!pendingReasons && !registrationId && (
        <LoadingError error={error} onRetry={retryable ? handleRetry : undefined} />
      )}
    </section>
  )
}
