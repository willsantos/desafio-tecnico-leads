# Leads List Design

**Spec**: `.specs/features/leads-list/spec.md`
**Status**: Approved

---

## Design Overview

A tela de listagem de propostas é uma página secundária do sistema, acessível pela rota `/leads`. Ela mantém a identidade visual do redesign (verde, slate, tokens existentes) e reaproveita os componentes do design system já criados: `Card`, `Button`, `Input`, `Select`, `Text`, `Alert`, `LoadingError`.

### Pilares

1. **Clareza operacional**: o operador encontra rapidamente o status e a etapa de cada proposta.
2. **Consistência visual**: mesma paleta, tipografia e espaçamento do wizard.
3. **Mobile-first**: em telas pequenas a listagem vira cards empilhados; em desktop vira tabela simples.
4. **Sem quebrar o fluxo**: o wizard continua sendo a experiência principal; a listagem é uma porta de entrada/retomada.

---

## Estrutura de Arquivos

```
frontend/src/
├── main.tsx                              # envolve App com BrowserRouter
├── App.tsx                               # adiciona Routes + Route + Header global
├── features/
│   └── leads/                            # nova feature
│       ├── LeadsListPage.tsx
│       ├── LeadsListPage.module.css
│       ├── leadsApi.ts
│       ├── leads.types.ts
│       └── components/
│           ├── LeadSummaryCard.tsx       # card mobile
│           ├── LeadSummaryRow.tsx        # linha desktop
│           └── StatusBadge.tsx           # badge colorido por status
├── shared/
│   ├── components/
│   │   └── layout/
│   │       └── Header.tsx                # navegação global
│   └── utils/
│       └── leadLabels.ts                 # tradução de status/currentStep
```

---

## Componentes e Comportamento

### Header global

- Localização: `frontend/src/shared/components/layout/Header.tsx`
- Exibe logo/título "Consignado Leads" à esquerda.
- À direita, links de navegação:
  - "Nova proposta" → `/`
  - "Propostas" → `/leads`
- Destaca visualmente o link ativo.
- Mantém-se fixo no topo do `App` (fora das rotas), dentro do `LeadProvider`.

### LeadsListPage

- Localização: `frontend/src/features/leads/LeadsListPage.tsx`
- Estado local:
  - `leads`: `LeadSummaryDto[]`
  - `loading`, `error`: boolean/string
  - `filters`: `{ status: string; currentStep: string; cpf: string }`
  - `page`, `pageSize`, `totalPages`
- Efeito: quando `filters` ou `page` mudam, chama `listLeads(filters, page, pageSize)`.
- Layout:
  - Título da página.
  - Barra de filtros com `Select` (status), `Select` (currentStep), `Input` (CPF mascarado), botão "Limpar".
  - Lista de resultados.
  - Paginação com "Anterior"/"Próxima" e texto "Página X de Y".
  - Estado vazio com CTA para `/`.

### LeadSummaryCard / LeadSummaryRow

- Recebem `LeadSummaryDto`.
- Exibem:
  - **CPF**: mascarado via `maskCpf`.
  - **Status**: traduzido via `leadLabels.ts` e renderizado no `StatusBadge`.
  - **Etapa atual**: traduzida.
  - **Criado em** / **Atualizado em**: `formatDateToBrazilian` + hora.
  - **Registro final**: quando `finalRegistration?.registrationId` existir.
- Ação: botão "Continuar" que chama `onResume(lead.id)`.

### StatusBadge

- Componente simples que mapeia `status` para uma cor semântica:
  - `completed` → verde (`success`)
  - `failed_retryable` / `abandoned` → vermelho/laranja (`error`/`warning`)
  - `in_progress` / `pending_confirmation` / `confirming` → azul (`info`)
  - `pending_verification` / `draft` → cinza/ciano
- Usa o `Card` ou um span estilizado com borda e fundo leve.

### leadsApi.ts

```ts
export function listLeads(
  filters: { status?: string; currentStep?: string; cpf?: string },
  page = 1,
  pageSize = 20,
): Promise<PagedLeadsResponse>
```

- Monta `URLSearchParams` omitindo valores vazios.
- Preserva CPF sem máscara no query param.

### leadLabels.ts

- Mapas de tradução:
  - `STATUS_LABELS: Record<LeadStatus, string>`
  - `STEP_LABELS: Record<CurrentStep, string>`
- Usado em `LeadsListPage` e no `StatusBadge`.

---

## Tech Decisions

| Decision | Choice | Rationale |
| --- | --- | --- |
| Roteador | `react-router-dom` v6 | Padrão React; nginx já configurado para SPA |
| Layout responsivo | CSS Grid/Flex + media queries | Sem dependência nova; reutiliza tokens |
| Paginação | Controles simples de página | `totalPages` já vem da API |
| Filtros de CPF | `Input` com máscara + `unmaskDigits` | Consistente com etapa 1 |
| Tradução de status | Frontend-only | Contrato mantém enums em inglês |

---

## Risks & Concerns

| Concern | Impact | Mitigation |
| --- | --- | --- |
| Adicionar `react-router-dom` aumenta bundle | Baixo | Lib pequena; tree-shakeable |
| Roteamento pode conflitar com `LeadContext` | Médio | Manter `LeadProvider` por fora do `BrowserRouter`; wizard continua usando `step` local |
| Quebrar layout existente do wizard | Médio | Header é aditivo; não remove o stepper |

---

## Diagram-Definition Cross-Check

- `GET /leads` → `LeadsHandler.ListAsync` → retorna `PagedLeadsResponse`.
- `GET /leads/{id}` → `LeadsHandler.GetByIdAsync` → retorna `LeadDto`.
- `LeadContext.setLead` + `resolveStepId` já existem e serão reutilizados.
