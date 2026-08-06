import { useState, type FormEvent } from 'react'
import { getErrorMessage } from '../../shared/api/httpClient'
import { Alert } from '../../shared/components/ui/Alert'
import { Button } from '../../shared/components/ui/Button'
import { Card } from '../../shared/components/ui/Card'
import { Input } from '../../shared/components/ui/Input'
import { Select } from '../../shared/components/ui/Select'
import { Text } from '../../shared/components/ui/Text'
import { LoadingError } from '../../shared/components/LoadingError'
import { useLead } from '../../shared/leadContext'
import { EMPTY_SIMULATION_FORM, type SimulationFormValues } from './simulation.types'
import { createSimulation, selectSimulation } from './simulationApi'
import styles from './SimulationPage.module.css'

function formatCurrency(value: number): string {
  return new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(value)
}

export function SimulationPage() {
  const { lead, setStep, refreshLead } = useLead()
  const [form, setForm] = useState<SimulationFormValues>(EMPTY_SIMULATION_FORM)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  if (!lead) {
    return (
      <Alert variant="warning" title="Etapa anterior não concluída">
        Complete a etapa 1 antes de simular.
      </Alert>
    )
  }

  const leadId = lead.id

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError(null)

    const requestedAmount = Number(form.requestedAmount)
    const installments = Number(form.installments)
    if (!Number.isFinite(requestedAmount) || requestedAmount <= 0 || !Number.isInteger(installments) || installments <= 0) {
      setError('Informe um valor e uma quantidade de parcelas válidos.')
      return
    }

    setSubmitting(true)
    try {
      await createSimulation(leadId, requestedAmount, installments)
      await refreshLead()
      setForm(EMPTY_SIMULATION_FORM)
    } catch (err) {
      setError(getErrorMessage(err))
    } finally {
      setSubmitting(false)
    }
  }

  async function handleSelect(simulationId: string) {
    setSubmitting(true)
    setError(null)
    try {
      await selectSimulation(leadId, simulationId)
      await refreshLead()
    } catch (err) {
      setError(getErrorMessage(err))
    } finally {
      setSubmitting(false)
    }
  }

  const selectedSimulation = lead.simulations.find((s) => s.selected)
  const history = lead.simulations.filter((s) => !s.selected)

  return (
    <section className={styles.wrapper}>
      <Text variant="title" as="h2" className={styles.heading}>
        Simulação de empréstimo
      </Text>

      <form onSubmit={handleSubmit} className={styles.form}>
        <div className={styles.row}>
          <Input
            label="Valor solicitado (R$)"
            name="requestedAmount"
            type="number"
            min="0"
            step="0.01"
            value={form.requestedAmount}
            onChange={(e) => setForm((previous) => ({ ...previous, requestedAmount: e.target.value }))}
            required
          />
          <Select
            label="Número de parcelas"
            name="installments"
            value={form.installments}
            onChange={(e) => setForm((previous) => ({ ...previous, installments: e.target.value }))}
            options={[
              { value: '6', label: '6 parcelas' },
              { value: '12', label: '12 parcelas' },
              { value: '24', label: '24 parcelas' },
              { value: '36', label: '36 parcelas' },
              { value: '48', label: '48 parcelas' },
              { value: '60', label: '60 parcelas' },
              { value: '72', label: '72 parcelas' },
            ]}
            placeholder="Selecione"
            required
          />
        </div>
        <div className={styles.actions}>
          <Button type="submit" loading={submitting} disabled={submitting}>
            Simular
          </Button>
        </div>
      </form>

      <LoadingError error={error} />

      {selectedSimulation && (
        <Card variant="primary" title="Simulação selecionada" className={styles.selectedCard}>
          <div className={styles.amounts}>
            <div>
              <Text variant="caption">Valor solicitado</Text>
              <Text variant="subtitle">{formatCurrency(selectedSimulation.requestedAmount)}</Text>
            </div>
            <div>
              <Text variant="caption">Parcelamento</Text>
              <Text variant="subtitle">{selectedSimulation.installments}x</Text>
            </div>
            <div>
              <Text variant="caption">Valor da parcela</Text>
              <Text variant="subtitle">{formatCurrency(selectedSimulation.installmentAmount)}</Text>
            </div>
            <div>
              <Text variant="caption">Valor total</Text>
              <Text variant="subtitle">{formatCurrency(selectedSimulation.totalAmount)}</Text>
            </div>
          </div>
        </Card>
      )}

      {history.length > 0 && (
        <div className={styles.history}>
          <Text variant="subtitle" as="h3">
            Histórico de simulações
          </Text>
          <div className={styles.historyGrid}>
            {history.map((simulation) => (
              <Card key={simulation.id} className={styles.historyCard}>
                <div className={styles.historyRow}>
                  <div>
                    <Text variant="caption">Parcela</Text>
                    <Text variant="body">{formatCurrency(simulation.installmentAmount)}</Text>
                  </div>
                  <div>
                    <Text variant="caption">Total</Text>
                    <Text variant="body">{formatCurrency(simulation.totalAmount)}</Text>
                  </div>
                </div>
                <Button
                  variant="secondary"
                  size="sm"
                  onClick={() => handleSelect(simulation.id)}
                  disabled={submitting}
                >
                  Selecionar
                </Button>
              </Card>
            ))}
          </div>
        </div>
      )}

      <div className={styles.actions}>
        <Button
          variant="primary"
          onClick={() => setStep('identification')}
          disabled={lead.simulations.length === 0 || submitting}
        >
          Avançar para identificação
        </Button>
      </div>
    </section>
  )
}
