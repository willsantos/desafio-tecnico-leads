export interface SimulationFormValues {
  requestedAmount: string
  installments: string
}

export const EMPTY_SIMULATION_FORM: SimulationFormValues = { requestedAmount: '', installments: '' }
