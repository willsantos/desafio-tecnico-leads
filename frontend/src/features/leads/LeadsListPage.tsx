import { useCallback, useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { getErrorMessage } from '../../shared/api/httpClient'
import type { LeadSummaryDto, PagedLeadsResponse } from '../../shared/api/types'
import { Alert } from '../../shared/components/ui/Alert'
import { Button } from '../../shared/components/ui/Button'
import { Input } from '../../shared/components/ui/Input'
import { LoadingError } from '../../shared/components/LoadingError'
import { Select } from '../../shared/components/ui/Select'
import { Text } from '../../shared/components/ui/Text'
import { resolveStepId, useLead } from '../../shared/leadContext'
import { maskCpf } from '../../shared/utils/formatters'
import { STEP_LABELS, STATUS_LABELS } from '../../shared/utils/leadLabels'
import { getLeadById, listLeads, type LeadsFilters } from './leadsApi'
import { LeadSummaryCard } from './components/LeadSummaryCard'
import { LeadSummaryRow } from './components/LeadSummaryRow'
import styles from './LeadsListPage.module.css'

const PAGE_SIZE = 20

const STATUS_OPTIONS = Object.entries(STATUS_LABELS).map(([value, label]) => ({ value, label }))

const STEP_OPTIONS = [
  { value: '', label: 'Todas' },
  ...Object.entries(STEP_LABELS).map(([value, label]) => ({ value, label })),
]

function normalizeStepFilter(step: string): string {
  // The UI uses friendly step ids; the backend stores "professional-banking-data".
  const map: Record<string, string> = {
    consultation: 'consultation',
    simulation: 'simulation',
    identification: 'identification',
    professionalBankingData: 'professional-banking-data',
  }
  return map[step] ?? step
}

function formatCpfFilter(value: string): string {
  const digits = value.replace(/\D/g, '')
  return digits.slice(0, 11)
}

export function LeadsListPage() {
  const navigate = useNavigate()
  const { setLead, setStep } = useLead()

  const [filters, setFilters] = useState<LeadsFilters>({
    status: '',
    currentStep: '',
    cpf: '',
  })
  const [page, setPage] = useState(1)
  const [response, setResponse] = useState<PagedLeadsResponse | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [resumingId, setResumingId] = useState<string | null>(null)

  const handleResume = useCallback(
    async (leadId: string) => {
      setResumingId(leadId)
      setError(null)
      try {
        const lead = await getLeadById(leadId)
        setLead(lead)
        setStep(resolveStepId(lead))
        navigate('/')
      } catch (err) {
        setError(getErrorMessage(err))
        setResumingId(null)
      }
    },
    [navigate, setLead, setStep],
  )

  const fetchLeads = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const result = await listLeads(
        {
          status: filters.status || undefined,
          currentStep: filters.currentStep ? normalizeStepFilter(filters.currentStep) : undefined,
          cpf: filters.cpf || undefined,
        },
        page,
        PAGE_SIZE,
      )
      setResponse(result)
    } catch (err) {
      setError(getErrorMessage(err))
    } finally {
      setLoading(false)
    }
  }, [filters, page])

  useEffect(() => {
    void fetchLeads()
  }, [fetchLeads])

  function updateFilter<K extends keyof LeadsFilters>(key: K, value: LeadsFilters[K]) {
    setFilters((previous) => ({ ...previous, [key]: value }))
    setPage(1)
  }

  function handleCpfChange(value: string) {
    updateFilter('cpf', formatCpfFilter(value))
  }

  function handleClearFilters() {
    setFilters({ status: '', currentStep: '', cpf: '' })
    setPage(1)
  }

  const leads = response?.items ?? []
  const totalPages = response?.totalPages ?? 0

  return (
    <section className={styles.wrapper}>
      <div className={styles.heading}>
        <Text variant="title" as="h2">
          Propostas em andamento
        </Text>
        <Link to="/" className={styles.newProposalButton}>
          Nova proposta
        </Link>
      </div>

      <div className={styles.filters}>
        <Select
          label="Status"
          name="status"
          value={filters.status}
          onChange={(e) => updateFilter('status', e.target.value)}
          options={[{ value: '', label: 'Todos' }, ...STATUS_OPTIONS]}
        />
        <Select
          label="Etapa atual"
          name="currentStep"
          value={filters.currentStep}
          onChange={(e) => updateFilter('currentStep', e.target.value)}
          options={STEP_OPTIONS}
        />
        <Input
          label="CPF"
          name="cpf"
          value={maskCpf(filters.cpf ?? '')}
          onValueChange={handleCpfChange}
          placeholder="000.000.000-00"
          maxLength={14}
        />
        <div className={styles.filterActions}>
          <Button variant="secondary" size="sm" onClick={handleClearFilters}>
            Limpar filtros
          </Button>
        </div>
      </div>

      <LoadingError loading={loading} error={error} onRetry={fetchLeads}>
        {leads.length === 0 ? (
          <Alert variant="info" title="Nenhuma proposta encontrada">
            <Text variant="body">
              Não encontramos propostas com os filtros selecionados.{' '}
              <Link to="/" className={styles.link}>
                Inicie uma nova proposta
              </Link>
              .
            </Text>
          </Alert>
        ) : (
          <>
            <div className={styles.mobileList}>
              {leads.map((lead) => (
                <LeadListItem
                  key={lead.id}
                  lead={lead}
                  onResume={handleResume}
                  busy={resumingId === lead.id}
                />
              ))}
            </div>

            <div className={styles.desktopTable}>
              <table className={styles.table}>
                <thead>
                  <tr>
                    <th className={styles.th}>CPF</th>
                    <th className={styles.th}>Status</th>
                    <th className={styles.th}>Etapa</th>
                    <th className={styles.th}>Atualizado em</th>
                    <th className={styles.th}>Criado em</th>
                    <th className={styles.th}>Registro</th>
                    <th className={styles.th} aria-label="Ações" />
                  </tr>
                </thead>
                <tbody>
                  {leads.map((lead) => (
                    <LeadListItem
                      key={lead.id}
                      lead={lead}
                      row
                      onResume={handleResume}
                      busy={resumingId === lead.id}
                    />
                  ))}
                </tbody>
              </table>
            </div>

            {totalPages > 1 && (
              <div className={styles.pagination}>
                <Button
                  variant="secondary"
                  size="sm"
                  onClick={() => setPage((p) => Math.max(1, p - 1))}
                  disabled={page <= 1 || loading}
                >
                  Anterior
                </Button>
                <Text variant="body">
                  Página {page} de {totalPages}
                </Text>
                <Button
                  variant="secondary"
                  size="sm"
                  onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                  disabled={page >= totalPages || loading}
                >
                  Próxima
                </Button>
              </div>
            )}
          </>
        )}
      </LoadingError>
    </section>
  )
}

interface LeadListItemProps {
  lead: LeadSummaryDto
  row?: boolean
  onResume: (leadId: string) => void
  busy: boolean
}

function LeadListItem({ lead, row, onResume, busy }: LeadListItemProps) {
  if (row) {
    return <LeadSummaryRow lead={lead} onResume={onResume} busy={busy} />
  }

  return <LeadSummaryCard lead={lead} onResume={onResume} busy={busy} />
}
