import { useState, type FormEvent } from 'react'
import { getErrorMessage } from '../../shared/api/httpClient'
import { Alert } from '../../shared/components/ui/Alert'
import { Button } from '../../shared/components/ui/Button'
import { Card } from '../../shared/components/ui/Card'
import { Input } from '../../shared/components/ui/Input'
import { Select } from '../../shared/components/ui/Select'
import { Text } from '../../shared/components/ui/Text'
import { LoadingError } from '../../shared/components/LoadingError'
import { maskDate, brDateDigitsToIso, isoToBrDateDigits, isValidBrDateDigits } from '../../shared/utils/formatters'
import { useLead } from '../../shared/leadContext'
import { ACCOUNT_TYPES, BANK_OPTIONS, EMPTY_PROFESSIONAL_BANKING_FORM, EMPLOYMENT_TYPES, type ProfessionalBankingDataFormValues } from './professionalBankingData.types'
import { submitProfessionalBankingData } from './professionalBankingDataApi'
import styles from './ProfessionalBankingDataPage.module.css'

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
            admissionDate: isoToBrDateDigits(lead.professionalData.admissionDate),
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
    return (
      <Alert variant="warning" title="Etapa anterior não concluída">
        Complete as etapas anteriores antes de informar dados profissionais e bancários.
      </Alert>
    )
  }

  const leadId = lead.id
  const expectedVersion = lead.version
  // The loan can only be deposited into the applicant's own account.
  const defaultAccountHolder = lead.identification?.fullName ?? ''

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
    if (!isValidBrDateDigits(form.professionalData.admissionDate)) {
      setError('Informe uma data de admissão válida.')
      return
    }

    setSubmitting(true)
    try {
      const dto = await submitProfessionalBankingData(leadId, {
        professionalData: {
          ...form.professionalData,
          monthlyIncome,
          admissionDate: brDateDigitsToIso(form.professionalData.admissionDate),
        },
        bankingData: {
          ...form.bankingData,
          accountHolder: defaultAccountHolder,
          pixKey: form.bankingData.pixKey || undefined,
        },
        expectedVersion,
      })
      setLead(dto)
      setStep('documents')
    } catch (err) {
      setError(getErrorMessage(err))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <section className={styles.wrapper}>
      <Text variant="title" as="h2" className={styles.heading}>
        Dados profissionais e bancários
      </Text>

      <form onSubmit={handleSubmit} className={styles.form}>
        <Card title="Dados profissionais">
          <div className={styles.grid2}>
            <Select
              label="Tipo de vínculo"
              name="employmentType"
              value={form.professionalData.employmentType}
              onChange={(e) => updateProfessional('employmentType', e.target.value)}
              options={EMPLOYMENT_TYPES.map((t) => ({ value: t.value, label: t.label }))}
              placeholder="Selecione"
              required
            />
            <Input label="Empresa/Órgão" name="company" value={form.professionalData.company} onChange={(e) => updateProfessional('company', e.target.value)} required />
            <Input label="Matrícula" name="registrationNumber" value={form.professionalData.registrationNumber} onChange={(e) => updateProfessional('registrationNumber', e.target.value)} required />
            <Input label="Cargo" name="role" value={form.professionalData.role} onChange={(e) => updateProfessional('role', e.target.value)} required />
            <Input label="Renda mensal (R$)" name="monthlyIncome" type="number" min="0" step="0.01" value={form.professionalData.monthlyIncome} onChange={(e) => updateProfessional('monthlyIncome', e.target.value)} required />
            <Input
              label="Data de admissão"
              name="admissionDate"
              type="text"
              inputMode="numeric"
              mask={maskDate}
              value={form.professionalData.admissionDate}
              onValueChange={(value) => value.length <= 8 && updateProfessional('admissionDate', value)}
              required
              maxLength={10}
              placeholder="DD/MM/AAAA"
            />
          </div>
        </Card>

        <Card title="Dados bancários">
          <div className={styles.grid2}>
            <Select
              label="Banco"
              name="bank"
              value={form.bankingData.bank}
              onChange={(e) => updateBanking('bank', e.target.value)}
              options={
                form.bankingData.bank && !BANK_OPTIONS.some((o) => o.value === form.bankingData.bank)
                  ? [...BANK_OPTIONS, { value: form.bankingData.bank, label: form.bankingData.bank }]
                  : BANK_OPTIONS
              }
              placeholder="Selecione"
              required
            />
            <Input label="Agência" name="agency" value={form.bankingData.agency} onChange={(e) => updateBanking('agency', e.target.value)} required />
            <Input label="Conta" name="account" value={form.bankingData.account} onChange={(e) => updateBanking('account', e.target.value)} required />
            <Input label="Dígito" name="accountDigit" value={form.bankingData.accountDigit} onChange={(e) => updateBanking('accountDigit', e.target.value)} required />
            <Select
              label="Tipo de conta"
              name="accountType"
              value={form.bankingData.accountType}
              onChange={(e) => updateBanking('accountType', e.target.value)}
              options={ACCOUNT_TYPES.map((t) => ({ value: t.value, label: t.label }))}
              placeholder="Selecione"
              required
            />
            <Input label="Chave PIX (opcional)" name="pixKey" value={form.bankingData.pixKey} onChange={(e) => updateBanking('pixKey', e.target.value)} />
          </div>
        </Card>

        <div className={styles.actions}>
          <Button type="submit" loading={submitting} disabled={submitting}>
            {lead.professionalData ? 'Atualizar e avançar' : 'Salvar e avançar'}
          </Button>
        </div>
      </form>

      <LoadingError error={error} />
    </section>
  )
}
