import { useState, type FormEvent } from 'react'
import { getErrorMessage } from '../../shared/api/httpClient'
import { LoadingError } from '../../shared/components/LoadingError'
import { useLead } from '../../shared/leadContext'
import { DOCUMENT_TYPES, EMPTY_IDENTIFICATION_FORM, type IdentificationFormValues } from './identification.types'
import { submitIdentification } from './identificationApi'

const OUTCOME_LABELS: Record<string, string> = {
  found: 'Dados encontrados e compatíveis.',
  not_found: 'Dados não encontrados na base de identificação.',
  diverging: 'Dados encontrados, mas divergentes do informado.',
  unavailable: 'Consulta de identificação indisponível no momento.',
}

export function IdentificationPage() {
  const { lead, setLead, setStep } = useLead()

  const [form, setForm] = useState<IdentificationFormValues>(() =>
    lead?.identification
      ? {
          fullName: lead.identification.fullName,
          cpf: lead.identification.cpf,
          birthDate: lead.identification.birthDate,
          email: lead.identification.email,
          phone: lead.identification.phone,
          address: {
            zipCode: lead.identification.address.zipCode,
            street: lead.identification.address.street,
            number: lead.identification.address.number,
            complement: lead.identification.address.complement ?? '',
            neighborhood: lead.identification.address.neighborhood,
            city: lead.identification.address.city,
            state: lead.identification.address.state,
          },
          motherName: lead.identification.motherName,
          maritalStatus: lead.identification.maritalStatus,
          documentType: lead.identification.documentType,
          documentNumber: lead.identification.documentNumber,
          issuingAuthority: lead.identification.issuingAuthority,
          issuingState: lead.identification.issuingState,
          issueDate: lead.identification.issueDate,
        }
      : EMPTY_IDENTIFICATION_FORM,
  )
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  if (!lead) {
    return <p>Complete as etapas anteriores antes de informar a identificação.</p>
  }

  const leadId = lead.id

  function updateField<K extends keyof IdentificationFormValues>(key: K, value: IdentificationFormValues[K]) {
    setForm((previous) => ({ ...previous, [key]: value }))
  }

  function updateAddressField<K extends keyof IdentificationFormValues['address']>(
    key: K,
    value: IdentificationFormValues['address'][K],
  ) {
    setForm((previous) => ({ ...previous, address: { ...previous.address, [key]: value } }))
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      const dto = await submitIdentification(leadId, form, lead?.version)
      setLead(dto)
    } catch (err) {
      setError(getErrorMessage(err))
    } finally {
      setSubmitting(false)
    }
  }

  const outcome = lead.identification?.query?.outcome ?? null

  return (
    <section>
      <h2>Etapa 3 — Identificação do cliente</h2>

      <form onSubmit={handleSubmit}>
        <label>
          Nome completo
          <input value={form.fullName} onChange={(e) => updateField('fullName', e.target.value)} required />
        </label>
        <label>
          CPF
          <input value={form.cpf} onChange={(e) => updateField('cpf', e.target.value)} required maxLength={11} />
        </label>
        <label>
          Data de nascimento
          <input type="date" value={form.birthDate} onChange={(e) => updateField('birthDate', e.target.value)} required />
        </label>
        <label>
          E-mail
          <input type="email" value={form.email} onChange={(e) => updateField('email', e.target.value)} required />
        </label>
        <label>
          Telefone
          <input value={form.phone} onChange={(e) => updateField('phone', e.target.value)} required />
        </label>

        <fieldset>
          <legend>Endereço</legend>
          <label>
            CEP
            <input value={form.address.zipCode} onChange={(e) => updateAddressField('zipCode', e.target.value)} required />
          </label>
          <label>
            Logradouro
            <input value={form.address.street} onChange={(e) => updateAddressField('street', e.target.value)} required />
          </label>
          <label>
            Número
            <input value={form.address.number} onChange={(e) => updateAddressField('number', e.target.value)} required />
          </label>
          <label>
            Complemento
            <input value={form.address.complement} onChange={(e) => updateAddressField('complement', e.target.value)} />
          </label>
          <label>
            Bairro
            <input
              value={form.address.neighborhood}
              onChange={(e) => updateAddressField('neighborhood', e.target.value)}
              required
            />
          </label>
          <label>
            Cidade
            <input value={form.address.city} onChange={(e) => updateAddressField('city', e.target.value)} required />
          </label>
          <label>
            UF
            <input value={form.address.state} onChange={(e) => updateAddressField('state', e.target.value)} required maxLength={2} />
          </label>
        </fieldset>

        <label>
          Nome da mãe
          <input value={form.motherName} onChange={(e) => updateField('motherName', e.target.value)} required />
        </label>
        <label>
          Estado civil
          <input value={form.maritalStatus} onChange={(e) => updateField('maritalStatus', e.target.value)} required />
        </label>
        <label>
          Tipo de documento
          <select value={form.documentType} onChange={(e) => updateField('documentType', e.target.value)} required>
            {DOCUMENT_TYPES.map((type) => (
              <option key={type} value={type}>
                {type}
              </option>
            ))}
          </select>
        </label>
        <label>
          Número do documento
          <input value={form.documentNumber} onChange={(e) => updateField('documentNumber', e.target.value)} required />
        </label>
        <label>
          Órgão emissor
          <input
            value={form.issuingAuthority}
            onChange={(e) => updateField('issuingAuthority', e.target.value)}
            required
          />
        </label>
        <label>
          UF de emissão
          <input value={form.issuingState} onChange={(e) => updateField('issuingState', e.target.value)} required maxLength={2} />
        </label>
        <label>
          Data de emissão
          <input type="date" value={form.issueDate} onChange={(e) => updateField('issueDate', e.target.value)} required />
        </label>

        <button type="submit" disabled={submitting}>
          {lead.identification ? 'Corrigir e reconsultar' : 'Enviar identificação'}
        </button>
      </form>

      <LoadingError error={error} />

      {outcome && (
        <p>
          Resultado da consulta: <strong>{outcome}</strong> — {OUTCOME_LABELS[outcome] ?? outcome}
        </p>
      )}

      <button type="button" disabled={!lead.identification} onClick={() => setStep('professionalBankingData')}>
        Avançar para dados profissionais
      </button>
    </section>
  )
}
