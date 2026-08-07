# Progress Resume Step Specification

## Problem Statement

O backend é dono do estado de progresso do lead, mas a responsabilidade de decidir "em qual etapa este lead deve retomar" ficou no frontend. O campo `progress.currentStep` tem semântica de "última etapa concluída" (cada handler grava `CurrentStep = X` e adiciona X em `completedSteps` ao mesmo tempo), e o backend nunca o avança além de `professional-banking-data` — as etapas 5 (documents) e 6 (confirmation) não têm marcador próprio. O frontend precisa então inverter a convenção e adivinhar a etapa ativa, o que já produziu um bug real: retomar um lead com dados profissionais concluídos caía na etapa 4 (já feita) em vez da 5 (documents). A decisão de "onde retomar" é uma questão de domínio que pertence ao backend.

## Goals

- [ ] Backend expõe campo derivado `progress.resumeStep` indicando a próxima etapa acionável do lead.
- [ ] Cálculo centralizado no backend (mapper/read-model), considerando `status`, `completedSteps` e documentos ativos.
- [ ] Campo puramente aditivo — `currentStep`, `completedSteps`, `startedSteps`, `pendingItems` mantêm forma e semântica atuais.
- [ ] Suite de testes de integração do backend cobre cada regra de derivação com `resumeStep` concreto.
- [ ] Frontend consome `resumeStep` quando presente, mantendo fallback para leads antigos.
- [ ] `dotnet test` e `npm run build` (tsc) passando.

## Out of Scope

| Feature | Reason |
| --- | --- |
| Mover/renomear/redefinir `currentStep` | Quebra `GET /leads?currentStep=`, exemplos do README seção 5 e consumidores existentes — fora do que o contrato permite |
| Adicionar `resumeStep` ao summary de `GET /leads` (lista) | README seção 5 linha 164 fixa os campos do summary; mudar fere o contrato. resumeStep só no LeadDto completo |
| Persistir `resumeStep` no documento Mongo | Dado derivado de estado existente; persistir introduz deriva e exige migração. Computado em tempo de leitura |
| Transições automáticas de `status` para `abandoned` | Já listado como limitação conhecida no README; fora deste escopo |
| Rastrear documents/confirmation em `completedSteps` | `completedSteps` é contrato; adicionar valores novos muda o enum observável |

---

## Assumptions & Open Questions

| Assumption / decision | Chosen default | Rationale | Confirmed? |
| --- | --- | --- | --- |
| Nome do campo | `resumeStep` (singular, dentro de `progress`) | "próxima etapa para retomar" — claro, distinto de `currentStep` | y |
| Vocabulário dos valores | Mesmo kebab-case de `completedSteps` (`consultation`, `simulation`, `identification`, `professional-banking-data`, `documents`, `confirmation`) | Coerência com enum existente; frontend já mapeia os 4 primeiros via `CURRENT_STEP_TO_STEP_ID` | y |
| Onde é computado | Em `LeadMapper.ToDto` (read-model), nunca persistido | Sempre fresco; sem write-path nos handlers; sem migração | y |
| Disponibilidade de documentos no cálculo | `GET /leads/{id}` já carrega documentos ativos; handlers de `PUT /steps/*` carregarão documentos ativos em paralelo ao montar a resposta | Necessário pra decidir `documents` vs `confirmation` com precisão; consulta extra barata e paralelizável | y |
| Leads antigos (criados antes da feature) | `resumeStep` é derivado do estado existente, então leads antigos também recebem o campo sem migração | Campo é computado, não armazenado — compatibilidade retroativa gratuita | y |
| Leads com `status=abandoned` | `resumeStep` computado normalmente pela regra geral (sem caso especial) | Não há política de "morto"; retomada cai onde parou | y |

**Open questions:** none.

---

## User Stories

### P1: Backend expõe `resumeStep` derivado ⭐ MVP

**User Story**: Como API, quero responder `progress.resumeStep` indicando a próxima etapa acionável do lead, pra que qualquer cliente (frontend ou outro) saiba exatamente onde retomar sem inverter convenções internas.

**Why P1**: É a correção raiz do bug de retomada. Independentemente testável via testes de integração (GET /leads/{id} + PUT /steps/* retornam o campo com valor concreto por estado).

**Acceptance Criteria** (each line is one EARS pattern):

> **Precedence:** rules apply in the order below — AC2 (confirmation-reached status) is checked
> first and wins outright; only when AC2 does not apply do AC3/AC4/AC5 run. So a `failed_retryable`
> lead with all four backend steps done but missing a document resolves to `confirmation`, never
> `documents`. The resolver (`Core/ResumeStepResolver`) implements this ordering explicitly.

1. The LeadDto.progress SHALL incluir um campo `resumeStep` do tipo string em toda resposta de `GET /leads/{id}` e dos endpoints `PUT /steps/*` e `POST /leads/consultation`. <!-- ubiquitous -->
2. WHILE `lead.status` ∈ {`pending_confirmation`, `confirming`, `pending_verification`, `failed_retryable`, `completed`} o mapper SHALL setar `resumeStep = "confirmation"`. <!-- state-driven -->
3. WHEN existir uma etapa backend rastreada (`consultation`, `simulation`, `identification`, `professional-banking-data`) ausente de `completedSteps` THEN o mapper SHALL setar `resumeStep` para a primeira dessas etapas pela ordem do fluxo. <!-- event-driven -->
4. WHILE todas as quatro etapas backend estiverem em `completedSteps` E os documentos ativos NÃO contiverem ambos `personal_document` e `payslip` o mapper SHALL setar `resumeStep = "documents"`. <!-- state-driven -->
5. WHILE todas as quatro etapas backend estiverem em `completedSteps` E os documentos ativos contiverem ambos `personal_document` e `payslip` o mapper SHALL setar `resumeStep = "confirmation"`. <!-- state-driven -->
6. IF `completedSteps` estiver vazio ou ausente THEN o mapper SHALL setar `resumeStep = "consultation"`. <!-- unwanted-behavior -->
7. The mapper SHALL preservar os valores e semântica atuais de `currentStep`, `startedSteps`, `completedSteps`, `pendingItems` e `lastUpdatedAt` (mudança puramente aditiva). <!-- ubiquitous -->
8. WHEN o cliente receber um lead criado antes desta feature THEN `resumeStep` SHALL estar presente e correto, derivado do estado existente (nenhuma migração exigida). <!-- event-driven -->

**Independent Test**: Chamar `GET /leads/{id}` em leads preparados em cada estado (após consultation, após identification, após professional-banking-data sem docs, após professional-banking-data com ambos os docs, em `failed_retryable`) e verificar o `resumeStep` retornado.

---

### P2: Frontend consome `resumeStep` com fallback ⭐ MVP

**User Story**: Como cliente no wizard, quero que retomar um lead (via lista ou modal de CPF) me leve direto à etapa certa, lendo `resumeStep` do backend.

**Why P2**: P1 só entrega valor se o frontend usar; mas é independentemente testável (P1 valida o backend isolado). Junto com P1 forma o slice vertical demoável.

**Acceptance Criteria**:

1. WHEN `lead.progress.resumeStep` estiver presente THEN `resolveStepId` SHALL retornar o `StepId` mapeado a partir de `resumeStep`. <!-- event-driven -->
2. WHERE `lead.progress.resumeStep` estiver ausente (lead antigo servido por backend anterior, ou resposta parcial) THEN `resolveStepId` SHALL usar a lógica atual baseada em `completedSteps` como fallback. <!-- optional-feature -->
3. The frontend SHALL mapear os valores `documents` e `confirmation` de `resumeStep` para seus `StepId` correspondentes (estendendo `CURRENT_STEP_TO_STEP_ID`). <!-- ubiquitous -->

**Independent Test**: Retomar um lead com dados profissionais concluídos e sem documentos → cair em "Anexos". Retomar um lead `failed_retryable` → cair em "Confirmação".

---

### P3: Documentação do contrato atualizada

**User Story**: Como desenvolvedor, quero o README seção 5 e as notas de trade-offs refletindo o novo campo, pra manter o contrato documentado.

**Why P3**: Documentação é valor, mas não bloqueia entrega funcional.

**Acceptance Criteria**:

1. WHEN o README seção 5 documentar o shape do lead THEN o exemplo SHALL incluir `resumeStep` em `progress` com um valor concreto.
2. The README SHALL incluir nota explicando que `resumeStep` é derivado (não persistido) e que `currentStep` mantém semântica de "última etapa concluída".

---

## Edge Cases

Edge cases are usually unwanted-behavior (IF/THEN) or boundary (WHEN) criteria:

- IF o lead tem apenas um dos dois documentos obrigatórios (ex.: só `personal_document`) THEN o mapper SHALL setar `resumeStep = "documents"` (ainda pendente).
- IF o lead está em `status=abandoned` THEN o mapper SHALL computar `resumeStep` pela regra geral (sem caso especial).
- IF `completedSteps` contém valores não reconhecidos THEN o mapper SHALL ignorá-los ao aplicar a regra de "primeira etapa ausente".
- WHEN um reenvio de documento marca o anterior como `replaced` THEN o mapper SHALL considerar apenas documentos ativos (`uploaded`) para a decisão documents-vs-confirmation.
- WHEN o `GET /leads/{id}` retorna `documents` vazio (lead antigo sem uploads) THEN o mapper SHALL tratar como "documentos pendentes".

---

## Requirement Traceability

| Requirement ID | Story | Phase | Status |
| -------------- | ----- | ----- | ------ |
| PRS-01 | P1 | Execute | Verified |
| PRS-02 | P1 | Execute | Verified |
| PRS-03 | P1 | Execute | Verified |
| PRS-04 | P1 | Execute | Verified |
| PRS-05 | P1 | Execute | Verified |
| PRS-06 | P1 | Execute | Verified |
| PRS-07 | P1 | Execute | Verified |
| PRS-08 | P1 | Execute | Verified |
| PRS-09 | P2 | Execute | Verified |
| PRS-10 | P2 | Execute | Verified |
| PRS-11 | P2 | Execute | Verified |
| PRS-12 | P3 | Execute | Verified |

**ID format:** `PRS-NN` (progress-resume-step).

**Status values:** Pending → In Design → In Tasks → Implementing → Verified

**Coverage:** 12 total, 12 mapped to stories, 0 unmapped.

---

## Success Criteria

- [ ] Bug de retomada resolvido na raiz: retomar lead com dados profissionais concluídos (sem docs) cai em "Anexos", não em "Dados profissionais".
- [ ] `resumeStep` presente em todo LeadDto retornado pela API, derivado corretamente por estado.
- [ ] Zero quebras de contrato: `currentStep`, `completedSteps`, `startedSteps`, `pendingItems` inalterados; summary de `GET /leads` inalterado.
- [ ] `dotnet test` verde com novos casos cobrindo cada regra de derivação.
- [ ] `npm run build` (tsc) verde; `resolveStepId` usa `resumeStep` com fallback.
