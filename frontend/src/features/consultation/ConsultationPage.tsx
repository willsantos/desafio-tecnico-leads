import { useState, type FormEvent } from 'react'
import { getErrorMessage } from '../../shared/api/httpClient'
import type { LeadSummaryDto } from '../../shared/api/types'
import { LoadingError } from '../../shared/components/LoadingError'
import { resolveStepId, useLead } from '../../shared/leadContext'
import { CpfReuseModal } from './CpfReuseModal'
import { BENEFIT_TYPES, EMPTY_CONSULTATION_FORM, type ConsultationFormValues } from './consultation.types'
import { createConsultation, findActiveLeadsByCpf, getLeadById, updateConsultation } from './consultationApi'

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

    // Retrying/correcting an already-created lead skips the dedup check entirely — it IS the
    // lead being continued, so there is nothing to deduplicate against.
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
      // P2-1 is a UX nicety on top of the frozen contract, not part of it — a failed dedup
      // check must never block etapa 1 submission.
      console.warn('Falha ao checar CPF duplicado, prosseguindo sem o modal.', err)
    } finally {
      setCheckingCpf(false)
    }

    await submitConsultation(form)
  }

  async function handleContinueExisting() {
    if (!activeLeads || activeLeads.length === 0) {
      return
    }
    setSubmitting(true)
    setError(null)
    try {
      const fullLead = await getLeadById(activeLeads[0].id)
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

  return (
    <section>
      <h2>Etapa 1 — Consulta de elegibilidade</h2>

      {activeLeads && (
        <CpfReuseModal
          leadsFound={activeLeads.length}
          onContinue={handleContinueExisting}
          onStartNew={handleStartNew}
          busy={submitting}
        />
      )}

      <form onSubmit={handleSubmit}>
        <label>
          CPF
          <input
            value={form.cpf}
            onChange={(e) => updateField('cpf', e.target.value)}
            required
            maxLength={11}
            placeholder="Somente números"
          />
        </label>
        <label>
          Data de nascimento
          <input type="date" value={form.birthDate} onChange={(e) => updateField('birthDate', e.target.value)} required />
        </label>
        <label>
          Tipo de benefício
          <select value={form.benefitType} onChange={(e) => updateField('benefitType', e.target.value)} required>
            {BENEFIT_TYPES.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </label>
        <label>
          Número do benefício
          <input value={form.benefitNumber} onChange={(e) => updateField('benefitNumber', e.target.value)} required />
        </label>
        <label>
          Instituição pagadora
          <input value={form.payingInstitution} onChange={(e) => updateField('payingInstitution', e.target.value)} required />
        </label>
        <label>
          <input
            type="checkbox"
            checked={form.consultationAuthorized}
            onChange={(e) => updateField('consultationAuthorized', e.target.checked)}
            required
          />
          Autorizo a consulta dos meus dados
        </label>

        <button type="submit" disabled={submitting || checkingCpf}>
          {checkingCpf ? 'Verificando CPF…' : lead ? 'Corrigir e reconsultar' : 'Consultar elegibilidade'}
        </button>
      </form>

      <LoadingError error={error} />

      {lead?.consultation?.result && (
        <p>
          Resultado da consulta: <strong>{lead.consultation.result.outcome}</strong>
          {lead.consultation.result.availableMargin != null &&
            ` — margem disponível: R$ ${lead.consultation.result.availableMargin.toFixed(2)}`}
        </p>
      )}
    </section>
  )
}
