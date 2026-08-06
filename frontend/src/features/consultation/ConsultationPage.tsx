import { useState, type FormEvent } from 'react'
import { getErrorMessage } from '../../shared/api/httpClient'
import type { LeadSummaryDto } from '../../shared/api/types'
import { Alert } from '../../shared/components/ui/Alert'
import { Button } from '../../shared/components/ui/Button'
import { Input } from '../../shared/components/ui/Input'
import { Select } from '../../shared/components/ui/Select'
import { Text } from '../../shared/components/ui/Text'
import { maskCpf } from '../../shared/utils/formatters'
import { LoadingError } from '../../shared/components/LoadingError'
import { resolveStepId, useLead } from '../../shared/leadContext'
import { CpfReuseModal } from './CpfReuseModal'
import { BENEFIT_TYPES, EMPTY_CONSULTATION_FORM, type ConsultationFormValues } from './consultation.types'
import { createConsultation, findActiveLeadsByCpf, getLeadById, updateConsultation } from './consultationApi'
import styles from './ConsultationPage.module.css'

function formatCurrency(value: number | null | undefined): string {
  if (value == null) return ''
  return new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(value)
}

export function ConsultationPage() {
  const { lead, setLead, setStep } = useLead()

  const [form, setForm] = useState<ConsultationFormValues>(() =>
    lead?.consultation
      ? {
          cpf: lead.consultation.input.cpf,
          birthDate: lead.consultation.input.birthDate,
          benefitType: lead.consultation.input.benefitType,
          benefitNumber: lead.consultation.input.benefitNumber,
          payingInstitution: lead.consultation.input.payingInstitution,
          consultationAuthorized: true,
        }
      : EMPTY_CONSULTATION_FORM,
  )
  const [submitting, setSubmitting] = useState(false)
  const [checkingCpf, setCheckingCpf] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [activeLeads, setActiveLeads] = useState<LeadSummaryDto[] | null>(null)

  function updateField<K extends keyof ConsultationFormValues>(key: K, value: ConsultationFormValues[K]) {
    setForm((previous) => ({ ...previous, [key]: value }))
  }

  async function submitConsultation(values: ConsultationFormValues) {
    setSubmitting(true)
    setError(null)
    try {
      const dto = lead ? await updateConsultation(lead.id, values, lead.version) : await createConsultation(values)
      setLead(dto)
      setStep('simulation')
    } catch (err) {
      setError(getErrorMessage(err))
    } finally {
      setSubmitting(false)
    }
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError(null)

    if (lead) {
      await submitConsultation(form)
      return
    }

    setCheckingCpf(true)
    try {
      const result = await findActiveLeadsByCpf(form.cpf)
      if (result.items.length > 0) {
        setActiveLeads(result.items)
        return
      }
    } catch (err) {
      console.warn('Falha ao checar CPF duplicado, prosseguindo sem o modal.', err)
    } finally {
      setCheckingCpf(false)
    }

    await submitConsultation(form)
  }

  async function handleContinueExisting(leadId: string) {
    setSubmitting(true)
    setError(null)
    try {
      const fullLead = await getLeadById(leadId)
      setLead(fullLead)
      setStep(resolveStepId(fullLead))
      setActiveLeads(null)
    } catch (err) {
      setError(getErrorMessage(err))
    } finally {
      setSubmitting(false)
    }
  }

  function handleStartNew() {
    setActiveLeads(null)
    void submitConsultation(form)
  }

  const canSubmit = form.consultationAuthorized && !submitting && !checkingCpf

  return (
    <section className={styles.wrapper}>
      <Text variant="title" as="h2" className={styles.heading}>
        Consulta de elegibilidade
      </Text>

      <CpfReuseModal
        isOpen={activeLeads !== null}
        leads={activeLeads ?? []}
        onContinue={handleContinueExisting}
        onStartNew={handleStartNew}
        busy={submitting}
      />

      <form onSubmit={handleSubmit} className={styles.form}>
        <div className={styles.row}>
          <Input
            label="CPF"
            name="cpf"
            value={form.cpf}
            mask={maskCpf}
            onValueChange={(value) => value.length <= 11 && updateField('cpf', value)}
            required
            maxLength={14}
            placeholder="000.000.000-00"
          />
          <Input
            label="Data de nascimento"
            name="birthDate"
            type="date"
            value={form.birthDate}
            onChange={(e) => updateField('birthDate', e.target.value)}
            required
          />
        </div>

        <div className={styles.row}>
          <Select
            label="Tipo de benefício"
            name="benefitType"
            value={form.benefitType}
            onChange={(e) => updateField('benefitType', e.target.value)}
            options={BENEFIT_TYPES.map((t) => ({ value: t.value, label: t.label }))}
            required
          />
          <Input
            label="Número do benefício"
            name="benefitNumber"
            value={form.benefitNumber}
            onChange={(e) => updateField('benefitNumber', e.target.value)}
            required
          />
        </div>

        <Input
          label="Instituição pagadora"
          name="payingInstitution"
          value={form.payingInstitution}
          onChange={(e) => updateField('payingInstitution', e.target.value)}
          required
        />

        <label className={styles.checkbox}>
          <input
            type="checkbox"
            checked={form.consultationAuthorized}
            onChange={(e) => updateField('consultationAuthorized', e.target.checked)}
            required
          />
          <Text variant="body">Autorizo a consulta dos meus dados</Text>
        </label>

        <div className={styles.actions}>
          <Button type="submit" loading={submitting || checkingCpf} disabled={!canSubmit}>
            {checkingCpf ? 'Verificando CPF…' : lead ? 'Corrigir e reconsultar' : 'Consultar elegibilidade'}
          </Button>
        </div>
      </form>

      <LoadingError error={error} />

      {lead?.consultation?.result && (
        <div className={styles.result}>
          <Alert variant={lead.consultation.result.outcome === 'eligible' ? 'success' : 'warning'} title="Resultado da consulta">
            <Text variant="body">
              {lead.consultation.result.outcome === 'eligible'
                ? 'Você está elegível para continuar o cadastro.'
                : `Desfecho da consulta: ${lead.consultation.result.outcome}`}
              {lead.consultation.result.availableMargin != null && (
                <>
                  {' '}
                  — Margem disponível: <strong>{formatCurrency(lead.consultation.result.availableMargin)}</strong>
                </>
              )}
            </Text>
          </Alert>
        </div>
      )}
    </section>
  )
}
