import { useState, type FormEvent } from 'react'
import { getErrorMessage } from '../../shared/api/httpClient'
import { LoadingError } from '../../shared/components/LoadingError'
import { useLead } from '../../shared/leadContext'
import { EMPTY_PROFESSIONAL_BANKING_FORM, type ProfessionalBankingDataFormValues } from './professionalBankingData.types'
import { submitProfessionalBankingData } from './professionalBankingDataApi'

export function ProfessionalBankingDataPage() {
  const { lead, setLead, setStep } = useLead()

  const [form, setForm] = useState<ProfessionalBankingDataFormValues>(() =>
    lead?.professionalData && lead.bankingData
      ? {
          professionalData: {
            employmentType: lead.professionalData.employmentType,
            company: lead.professionalData.company,
            registrationNumber: lead.professionalData.registrationNumber,
            role: lead.professionalData.role,
            monthlyIncome: String(lead.professionalData.monthlyIncome),
            admissionDate: lead.professionalData.admissionDate,
          },
          bankingData: {
            bank: lead.bankingData.bank,
            agency: lead.bankingData.agency,
            account: lead.bankingData.account,
            accountDigit: lead.bankingData.accountDigit,
            accountType: lead.bankingData.accountType,
            accountHolder: lead.bankingData.accountHolder,
            pixKey: lead.bankingData.pixKey ?? '',
          },
        }
      : EMPTY_PROFESSIONAL_BANKING_FORM,
  )
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  if (!lead) {
    return <p>Complete as etapas anteriores antes de informar dados profissionais e bancários.</p>
  }

  const leadId = lead.id
  const expectedVersion = lead.version

  function updateProfessional<K extends keyof ProfessionalBankingDataFormValues['professionalData']>(
    key: K,
    value: ProfessionalBankingDataFormValues['professionalData'][K],
  ) {
    setForm((previous) => ({ ...previous, professionalData: { ...previous.professionalData, [key]: value } }))
  }

  function updateBanking<K extends keyof ProfessionalBankingDataFormValues['bankingData']>(
    key: K,
    value: ProfessionalBankingDataFormValues['bankingData'][K],
  ) {
    setForm((previous) => ({ ...previous, bankingData: { ...previous.bankingData, [key]: value } }))
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError(null)

    const monthlyIncome = Number(form.professionalData.monthlyIncome)
    if (!Number.isFinite(monthlyIncome) || monthlyIncome < 0) {
      setError('Informe uma renda mensal válida.')
      return
    }

    setSubmitting(true)
    try {
      const dto = await submitProfessionalBankingData(leadId, {
        professionalData: { ...form.professionalData, monthlyIncome },
        bankingData: { ...form.bankingData, pixKey: form.bankingData.pixKey || undefined },
        expectedVersion,
      })
      setLead(dto)
    } catch (err) {
      setError(getErrorMessage(err))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <section>
      <h2>Etapa 4 — Dados profissionais e bancários</h2>

      <form onSubmit={handleSubmit}>
        <fieldset>
          <legend>Dados profissionais</legend>
          <label>
            Tipo de vínculo
            <input
              value={form.professionalData.employmentType}
              onChange={(e) => updateProfessional('employmentType', e.target.value)}
              required
            />
          </label>
          <label>
            Empresa/Órgão
            <input value={form.professionalData.company} onChange={(e) => updateProfessional('company', e.target.value)} required />
          </label>
          <label>
            Matrícula
            <input
              value={form.professionalData.registrationNumber}
              onChange={(e) => updateProfessional('registrationNumber', e.target.value)}
              required
            />
          </label>
          <label>
            Cargo
            <input value={form.professionalData.role} onChange={(e) => updateProfessional('role', e.target.value)} required />
          </label>
          <label>
            Renda mensal (R$)
            <input
              type="number"
              min="0"
              step="0.01"
              value={form.professionalData.monthlyIncome}
              onChange={(e) => updateProfessional('monthlyIncome', e.target.value)}
              required
            />
          </label>
          <label>
            Data de admissão
            <input
              type="date"
              value={form.professionalData.admissionDate}
              onChange={(e) => updateProfessional('admissionDate', e.target.value)}
              required
            />
          </label>
        </fieldset>

        <fieldset>
          <legend>Dados bancários</legend>
          <label>
            Banco
            <input value={form.bankingData.bank} onChange={(e) => updateBanking('bank', e.target.value)} required />
          </label>
          <label>
            Agência
            <input value={form.bankingData.agency} onChange={(e) => updateBanking('agency', e.target.value)} required />
          </label>
          <label>
            Conta
            <input value={form.bankingData.account} onChange={(e) => updateBanking('account', e.target.value)} required />
          </label>
          <label>
            Dígito
            <input value={form.bankingData.accountDigit} onChange={(e) => updateBanking('accountDigit', e.target.value)} required />
          </label>
          <label>
            Tipo de conta
            <input value={form.bankingData.accountType} onChange={(e) => updateBanking('accountType', e.target.value)} required />
          </label>
          <label>
            Titular
            <input
              value={form.bankingData.accountHolder}
              onChange={(e) => updateBanking('accountHolder', e.target.value)}
              required
            />
          </label>
          <label>
            Chave PIX (opcional)
            <input value={form.bankingData.pixKey} onChange={(e) => updateBanking('pixKey', e.target.value)} />
          </label>
        </fieldset>

        <button type="submit" disabled={submitting}>
          {lead.professionalData ? 'Atualizar dados' : 'Salvar dados'}
        </button>
      </form>

      <LoadingError error={error} />

      <button type="button" disabled={!lead.professionalData} onClick={() => setStep('documents')}>
        Avançar para anexos
      </button>
    </section>
  )
}
