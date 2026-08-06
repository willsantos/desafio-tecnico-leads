import { useState, type FormEvent } from 'react'
import { getErrorMessage } from '../../shared/api/httpClient'
import { Alert } from '../../shared/components/ui/Alert'
import { Button } from '../../shared/components/ui/Button'
import { Card } from '../../shared/components/ui/Card'
import { Input } from '../../shared/components/ui/Input'
import { Select } from '../../shared/components/ui/Select'
import { Text } from '../../shared/components/ui/Text'
import { maskCpf, maskPhone } from '../../shared/utils/formatters'
import { LoadingError } from '../../shared/components/LoadingError'
import { useLead } from '../../shared/leadContext'
import { DOCUMENT_TYPES, EMPTY_IDENTIFICATION_FORM, type IdentificationFormValues } from './identification.types'
import { submitIdentification } from './identificationApi'
import styles from './IdentificationPage.module.css'

const OUTCOME_LABELS: Record<string, string> = {
  found: 'Dados encontrados e compatíveis.',
  not_found: 'Dados não encontrados na base de identificação.',
  diverging: 'Dados encontrados, mas divergentes do informado.',
  unavailable: 'Consulta de identificação indisponível no momento.',
}

const OUTCOME_VARIANTS: Record<string, 'success' | 'warning' | 'info' | 'error'> = {
  found: 'success',
  not_found: 'warning',
  diverging: 'warning',
  unavailable: 'info',
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
    return (
      <Alert variant="warning" title="Etapa anterior não concluída">
        Complete as etapas anteriores antes de informar a identificação.
      </Alert>
    )
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
    <section className={styles.wrapper}>
      <Text variant="title" as="h2" className={styles.heading}>
        Identificação do cliente
      </Text>

      <form onSubmit={handleSubmit} className={styles.form}>
        <Card title="Dados pessoais">
          <div className={styles.grid2}>
            <Input label="Nome completo" name="fullName" value={form.fullName} onChange={(e) => updateField('fullName', e.target.value)} required />
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
            <Input label="Data de nascimento" name="birthDate" type="date" value={form.birthDate} onChange={(e) => updateField('birthDate', e.target.value)} required />
            <Input
              label="Telefone"
              name="phone"
              value={form.phone}
              mask={maskPhone}
              onValueChange={(value) => value.length <= 11 && updateField('phone', value)}
              required
              maxLength={15}
              placeholder="(00) 00000-0000"
            />
            <Input label="E-mail" name="email" type="email" value={form.email} onChange={(e) => updateField('email', e.target.value)} required className={styles.span2} />
            <Input label="Nome da mãe" name="motherName" value={form.motherName} onChange={(e) => updateField('motherName', e.target.value)} required className={styles.span2} />
            <Input label="Estado civil" name="maritalStatus" value={form.maritalStatus} onChange={(e) => updateField('maritalStatus', e.target.value)} required />
          </div>
        </Card>

        <Card title="Endereço">
          <div className={styles.grid2}>
            <Input label="CEP" name="zipCode" value={form.address.zipCode} onChange={(e) => updateAddressField('zipCode', e.target.value)} required />
            <Input label="Logradouro" name="street" value={form.address.street} onChange={(e) => updateAddressField('street', e.target.value)} required />
            <Input label="Número" name="number" value={form.address.number} onChange={(e) => updateAddressField('number', e.target.value)} required />
            <Input label="Complemento" name="complement" value={form.address.complement} onChange={(e) => updateAddressField('complement', e.target.value)} />
            <Input label="Bairro" name="neighborhood" value={form.address.neighborhood} onChange={(e) => updateAddressField('neighborhood', e.target.value)} required />
            <Input label="Cidade" name="city" value={form.address.city} onChange={(e) => updateAddressField('city', e.target.value)} required />
            <Input label="UF" name="state" value={form.address.state} onChange={(e) => updateAddressField('state', e.target.value)} required maxLength={2} />
          </div>
        </Card>

        <Card title="Documento de identificação">
          <div className={styles.grid2}>
            <Select
              label="Tipo de documento"
              name="documentType"
              value={form.documentType}
              onChange={(e) => updateField('documentType', e.target.value)}
              options={DOCUMENT_TYPES.map((t) => ({ value: t, label: t }))}
              required
            />
            <Input label="Número do documento" name="documentNumber" value={form.documentNumber} onChange={(e) => updateField('documentNumber', e.target.value)} required />
            <Input label="Órgão emissor" name="issuingAuthority" value={form.issuingAuthority} onChange={(e) => updateField('issuingAuthority', e.target.value)} required />
            <Input label="UF de emissão" name="issuingState" value={form.issuingState} onChange={(e) => updateField('issuingState', e.target.value)} required maxLength={2} />
            <Input label="Data de emissão" name="issueDate" type="date" value={form.issueDate} onChange={(e) => updateField('issueDate', e.target.value)} required />
          </div>
        </Card>

        <div className={styles.actions}>
          <Button type="submit" loading={submitting} disabled={submitting}>
            {lead.identification ? 'Corrigir e reconsultar' : 'Enviar identificação'}
          </Button>
        </div>
      </form>

      <LoadingError error={error} />

      {outcome && (
        <Alert variant={OUTCOME_VARIANTS[outcome] ?? 'info'} title="Resultado da consulta">
          {OUTCOME_LABELS[outcome] ?? outcome}
        </Alert>
      )}

      <div className={styles.actions}>
        <Button variant="primary" onClick={() => setStep('professionalBankingData')} disabled={!lead.identification || submitting}>
          Avançar para dados profissionais
        </Button>
      </div>
    </section>
  )
}
