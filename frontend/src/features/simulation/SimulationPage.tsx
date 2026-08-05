import { useState, type FormEvent } from 'react'
import { getErrorMessage } from '../../shared/api/httpClient'
import { LoadingError } from '../../shared/components/LoadingError'
import { useLead } from '../../shared/leadContext'
import { EMPTY_SIMULATION_FORM, type SimulationFormValues } from './simulation.types'
import { createSimulation, selectSimulation } from './simulationApi'

export function SimulationPage() {
  const { lead, setStep, refreshLead } = useLead()
  const [form, setForm] = useState<SimulationFormValues>(EMPTY_SIMULATION_FORM)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  if (!lead) {
    return <p>Complete a etapa 1 antes de simular.</p>
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

  return (
    <section>
      <h2>Etapa 2 — Simulação</h2>

      <form onSubmit={handleSubmit}>
        <label>
          Valor solicitado (R$)
          <input
            type="number"
            min="0"
            step="0.01"
            value={form.requestedAmount}
            onChange={(e) => setForm((previous) => ({ ...previous, requestedAmount: e.target.value }))}
            required
          />
        </label>
        <label>
          Número de parcelas
          <input
            type="number"
            min="1"
            step="1"
            value={form.installments}
            onChange={(e) => setForm((previous) => ({ ...previous, installments: e.target.value }))}
            required
          />
        </label>
        <button type="submit" disabled={submitting}>
          Simular
        </button>
      </form>

      <LoadingError error={error} />

      {lead.simulations.length > 0 && (
        <table>
          <thead>
            <tr>
              <th>Valor solicitado</th>
              <th>Parcelas</th>
              <th>Valor da parcela</th>
              <th>Valor total</th>
              <th>Selecionada</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {lead.simulations.map((simulation) => (
              <tr key={simulation.id}>
                <td>R$ {simulation.requestedAmount.toFixed(2)}</td>
                <td>{simulation.installments}x</td>
                <td>R$ {simulation.installmentAmount.toFixed(2)}</td>
                <td>R$ {simulation.totalAmount.toFixed(2)}</td>
                <td>{simulation.selected ? 'Sim' : 'Não'}</td>
                <td>
                  {!simulation.selected && (
                    <button type="button" onClick={() => handleSelect(simulation.id)} disabled={submitting}>
                      Selecionar
                    </button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      <button type="button" disabled={lead.simulations.length === 0} onClick={() => setStep('identification')}>
        Avançar para identificação
      </button>
    </section>
  )
}
