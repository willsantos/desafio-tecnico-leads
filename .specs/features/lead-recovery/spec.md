# Lead Recovery Specification

## Problem Statement

O fluxo de cadastro de empréstimo consignado tem 6 etapas; o cliente pode abandonar, falhar, ou o sistema principal pode ficar indisponível antes da confirmação final. Sem persistência progressiva, todo o preenchimento se perde. A aplicação precisa salvar um "lead de recuperação" a cada etapa em MongoDB, distinto do cadastro definitivo (que só existe após confirmação da etapa 6), permitindo retomada, nova tentativa e recuperação de falhas sem duplicar o cadastro.

## Goals

- [ ] Implementar as 12 rotas do contrato (README seção 5) com shape de JSON, status codes e enums exatamente como especificado
- [ ] Modelar `leads` + `lead_documents` no MongoDB com atomicidade por etapa, concorrência otimista e sem expor documentos Mongo diretamente na API
- [ ] Frontend React navegável pelas 6 etapas com retomada por id, upload de documentos e resumo antes da confirmação
- [ ] `docker compose up --build` sobe a stack sem passo manual, CI (`ci.yml`) permanece verde
- [ ] Suíte de testes cobrindo os 8 casos mínimos da seção 8 do README

## Out of Scope

| Feature | Reason |
| --- | --- |
| Autenticação/autorização | Diferencial opcional (README 11) |
| Token de retomada dedicado com expiração | Diferencial opcional; retomada usa o `id` do lead diretamente |
| Detecção automática de abandono (Change Streams/TTL) | Diferencial opcional; enum `abandoned` existe no contrato mas nada o define automaticamente no MVP |
| Concorrência otimista completa em todos os endpoints | Contrato só define `expectedVersion` nos 3 `PUT /steps/*`; demais endpoints usam updates atômicos sem esse parâmetro |
| Criptografia de CPF/dados bancários em repouso | Fora do orçamento de 7 dias; documentado como limitação conhecida — mascaramento em log continua obrigatório |
| Verificação manual externa para `pending_verification` | Resolvido via retry automático no mock (ver Assumptions) |
| Bloqueio server-side de CPF duplicado | Resolvido via modal no frontend, sem alterar contrato de `POST /leads/consultation` |
| OpenAPI/Swagger completo com exemplos | Incluído como P2 básico (auto-geração), não é o foco de avaliação |

---

## Assumptions & Open Questions

| Assumption / decision | Chosen default | Rationale | Confirmed? |
| --- | --- | --- | --- |
| Storage de arquivos (etapa 5) | GridFS no MongoDB | Sem infra extra, sem volume novo, consistente com "MongoDB standalone" e "sem passos manuais" | y |
| Mock default (flag off/sem mockOutcome) | Determinístico, sempre sucesso | Fluxo demo previsível; falhas só via `mockOutcome` explícito da suíte | y |
| Duplicidade de CPF | Modal frontend (continuar/começar novo) + filtro aditivo `cpf` em `GET /leads` | Evita duplicidade sem risco de quebrar suíte automatizada que espera 201 sempre em `POST /leads/consultation` | y |
| Paginação `GET /leads` | `pageSize` default 20 / máx 100, `page` 1-based, sort `createdAt` desc | Contrato não define, precisa de default determinístico e testável | y |
| Reconciliação `pending_verification` | `retry-submission` tenta o mock de novo | Simples, sem infra extra, condizente com RF15-17 | y |
| Autenticação | Fora de escopo MVP | Diferencial opcional explícito no README | y (assumido, não discutido) |
| Criptografia em repouso (CPF/dados bancários) | Fora de escopo MVP, documentar limitação | Orçamento de 7 dias; mascaramento em log é o requisito obrigatório real | y (assumido, não discutido) |
| Transição `abandoned` | Nenhum mecanismo automático no MVP | Diferencial opcional (Change Streams) no README | y (assumido, não discutido) |
| Cap de arrays `simulations[]`/`confirmation.attempts[]` | Sem limite artificial | Bounded pela natureza da interação humana | y (assumido, não discutido) |
| Rate limiting / throttle | N/A — sem requisito no contrato ou README | Dimensão não presente neste desafio | y (N/A) |
| TTL/expiração de dados | N/A — sem requisito de expiração no contrato | README não pede expiração; abandono é diferencial opcional | y (N/A) |

**Open questions:** none — todas resolvidas ou logadas acima.

---

## User Stories

### P1-1: Etapa 1 — Consulta de elegibilidade ⭐ MVP

**User Story**: Como cliente solicitando empréstimo consignado, quero informar meus dados básicos e consultar elegibilidade, pra iniciar o cadastro sem perder o que já preenchi se algo falhar.

**Why P1**: Cria o lead — é o ponto de entrada de todo o fluxo.

**Acceptance Criteria**:

1. WHEN o cliente envia `POST /leads/consultation` com `cpf`, `birthDate`, `benefitType`, `benefitNumber`, `payingInstitution`, `consultationAuthorized=true` THEN o sistema SHALL criar um lead com `status="in_progress"`, `version=1`, `progress.currentStep="consultation"` e retornar **201**
2. IF `consultationAuthorized=false` OR campo obrigatório ausente THEN o sistema SHALL retornar **400** com Problem Details, sem criar lead
3. WHEN a consulta de elegibilidade mockada retorna `unavailable` (via `mockOutcome=unavailable` com `ENABLE_TEST_ENDPOINTS=true`, ou indisponibilidade real) THEN o sistema SHALL criar o lead mesmo assim, preservando os dados informados, com `consultation.result.outcome="unavailable"`
4. WHILE `ENABLE_TEST_ENDPOINTS != true` the system SHALL ignorar `mockOutcome` e usar lógica padrão determinística (`eligible`)
5. WHEN o cliente reenvia `PUT /leads/{id}/steps/consultation` para um lead existente THEN o sistema SHALL corrigir/re-executar a consulta sem criar lead novo, retornando **200**
6. IF o lead não existir em `PUT /leads/{id}/steps/consultation` THEN o sistema SHALL retornar **404**
7. IF `expectedVersion` informado divergir da versão atual THEN o sistema SHALL retornar **409** com `extensions.currentVersion`

**Independent Test**: `POST /leads/consultation` com CPF válido cria lead com `id`; `GET /leads/{id}` retorna o mesmo `consultation.result`.

---

### P1-2: Etapa 2 — Simulação ⭐ MVP

**User Story**: Como cliente, quero simular parcelas do empréstimo com valores diferentes e escolher a simulação que prefiro, pra decidir antes de seguir o cadastro.

**Why P1**: Faz parte do fluxo obrigatório de 6 etapas; fórmula de cálculo é fixa e testada pela suíte.

**Acceptance Criteria**:

1. WHEN o cliente envia `POST /leads/{id}/steps/simulation` com `requestedAmount` e `installments` THEN o sistema SHALL calcular `installmentAmount = requestedAmount * (rate * (1+rate)^installments) / ((1+rate)^installments - 1)` com `rate=0.018`, arredondado 2 casas decimais
2. The system SHALL calcular `totalAmount = installmentAmount_arredondado * installments`, arredondado 2 casas decimais (produto do valor já arredondado, não do bruto)
3. WHEN uma simulação é criada THEN o sistema SHALL marcá-la `selected=true` e marcar todas as simulações anteriores do mesmo lead `selected=false`, mantendo-as na lista (histórico cumulativo)
4. The system SHALL retornar **201** com a simulação criada
5. IF o lead não existir THEN o sistema SHALL retornar **404**
6. IF a etapa de consulta ainda não foi concluída THEN o sistema SHALL retornar **409**
7. WHEN o cliente envia `PATCH /leads/{id}/steps/simulation/{simulationId}/select` THEN o sistema SHALL trocar a simulação selecionada sem recalcular valores, retornando **200**
8. IF lead ou simulação não existirem no select THEN o sistema SHALL retornar **404**

**Independent Test**: `requestedAmount=10000, installments=24` → `installmentAmount=516.81`, `totalAmount=12403.44` (valor exato do exemplo do README).

---

### P1-3: Etapa 3 — Identificação do cliente ⭐ MVP

**User Story**: Como cliente, quero informar meus dados de identificação e ter o documento validado, pra prosseguir com confiança de que os dados batem.

**Why P1**: Define o `documentType` que valida o upload da etapa 5; dispara consulta mockada de identificação.

**Acceptance Criteria**:

1. WHEN o cliente envia `PUT /leads/{id}/steps/identification` com todos os campos obrigatórios (`fullName`, `cpf`, `birthDate`, `email`, `phone`, `address`, `motherName`, `maritalStatus`, `documentType`, `documentNumber`, `issuingAuthority`, `issuingState`, `issueDate`) THEN o sistema SHALL persistir os dados e executar a consulta de identificação mockada, retornando **200**
2. The system SHALL persistir o resultado da consulta (`found`/`not_found`/`diverging`/`unavailable`) em `identification.query.result` independente do desfecho
3. IF a consulta retornar `not_found`, `diverging` ou `unavailable` THEN o sistema SHALL preservar os dados já digitados, permitindo o cliente continuar ou corrigir (Cenário 6)
4. IF o lead não existir THEN o sistema SHALL retornar **404**
5. IF `expectedVersion` divergir THEN o sistema SHALL retornar **409** com `extensions.currentVersion`
6. WHILE `ENABLE_TEST_ENDPOINTS != true` the system SHALL ignorar `mockOutcome` e usar lógica padrão determinística (`found`)

**Independent Test**: `PUT .../identification` com `documentType="CNH"` grava o tipo; etapa 5 depois exige subtipo `CNH` (verificado na story P1-5).

---

### P1-4: Etapa 4 — Dados profissionais e bancários ⭐ MVP

**User Story**: Como cliente, quero informar meus dados profissionais e bancários, pra completar as informações necessárias ao cadastro definitivo.

**Why P1**: Parte obrigatória do fluxo; não gera cadastro definitivo, só persiste no lead.

**Acceptance Criteria**:

1. WHEN o cliente envia `PUT /leads/{id}/steps/professional-banking-data` com `professionalData` e `bankingData` completos THEN o sistema SHALL persistir ambos no lead sem apagar dados de outras etapas, retornando **200**
2. IF o lead não existir THEN o sistema SHALL retornar **404**
3. IF `expectedVersion` divergir THEN o sistema SHALL retornar **409**
4. The system SHALL aceitar `pixKey` ausente ou vazio (opcional) sem erro

**Independent Test**: Grava etapa 4 depois de etapas 1-3 preenchidas; `GET /leads/{id}` mostra `consultation`, `simulations`, `identification` intactos junto com os novos dados (não sobrescritos).

---

### P1-5: Etapa 5 — Anexos ⭐ MVP

**User Story**: Como cliente, quero enviar meu documento pessoal e contracheque separadamente (podendo abandonar entre um envio e outro), pra não perder o progresso se a conexão cair no meio do upload.

**Why P1**: Upload nunca cria lead — precisa de lead existente; validação de compatibilidade de documento é bloqueante.

**Acceptance Criteria**:

1. WHEN o cliente envia `POST /leads/{id}/documents` (multipart, `file` + `type` + `personalDocumentSubtype` quando `type=personal_document`) para um lead existente THEN o sistema SHALL armazenar o arquivo no GridFS, criar metadado em `lead_documents` com `status="uploaded"`, e retornar **201**
2. IF o lead não existir THEN o sistema SHALL retornar **404** (upload nunca cria lead — RF11)
3. IF o arquivo exceder 10MB OU o `contentType` não for `image/jpeg`, `image/png` ou `application/pdf` THEN o sistema SHALL retornar **400**
4. WHEN o cliente reenvia um documento do mesmo `type` THEN o sistema SHALL marcar a versão anterior `status="replaced"` (RF12)
5. IF `personalDocumentSubtype` divergir do `documentType` informado na etapa 3 (ex.: `RG` enviado quando etapa 3 informou `CNH`) THEN o sistema SHALL impedir a conclusão da etapa 5 (Cenário 8, RF13) — o upload em si pode ser aceito e listado, mas a etapa 6 bloqueia com essa pendência
6. WHEN o cliente chama `GET /leads/{id}/documents` THEN o sistema SHALL retornar apenas documentos `status` ativo (excluindo `deleted` e `replaced`)
7. WHEN o cliente chama `DELETE /leads/{id}/documents/{documentId}` THEN o sistema SHALL marcar `status="deleted"` (soft-delete) e retornar **204**
8. IF documento ou lead não existirem no DELETE THEN o sistema SHALL retornar **404**
9. The system SHALL permitir envios em chamadas separadas, com abandono entre elas, sem perder o que já foi enviado (Cenário 7)

**Independent Test**: Envia `personal_document` sozinho, abandona, retorna depois e envia `payslip` — ambos aparecem em `GET /leads/{id}/documents`.

---

### P1-6: Etapa 6 — Confirmação e efetivação do cadastro ⭐ MVP

**User Story**: Como cliente, quero revisar um resumo completo e confirmar, pra efetivar meu cadastro definitivo sem risco de duplicidade se algo falhar no meio do caminho.

**Why P1**: É o ponto de não-retorno do fluxo — precisa de validação de pendências, mutex de concorrência e idempotência de retry.

**Acceptance Criteria**:

1. WHEN o cliente chama `POST /leads/{id}/confirm` THEN o sistema SHALL validar: simulação selecionada existe; consulta de identificação concluída; compatibilidade de documento (etapa 3 x etapa 5); documentos obrigatórios (`personal_document` + `payslip`) enviados e ativos
2. IF qualquer validação de pendência falhar THEN o sistema SHALL retornar **422** com lista exata dos motivos, SEM chamar o sistema principal mockado (RF14, Cenário 9)
3. WHEN todas as validações passam THEN o sistema SHALL chamar o sistema principal mockado
4. IF o mock retornar `success` THEN o sistema SHALL preencher `confirmation.finalRegistration`, mudar `status="completed"` e retornar **200**
5. IF o mock retornar `rejected` OR `validationError` THEN o sistema SHALL retornar **422**, preservando o lead integralmente e mudando `status="failed_retryable"`
6. IF o mock retornar `unavailable` THEN o sistema SHALL retornar **503**, preservando o lead, `status="failed_retryable"`
7. IF o mock retornar `timeout` THEN o sistema SHALL retornar **504**, preservando o lead, `status="failed_retryable"`
8. IF o mock retornar `indeterminate` THEN o sistema SHALL retornar **202** com `status="pending_verification"`
9. IF já existe uma confirmação em andamento para o mesmo lead (mutex) OR o lead já está `completed` THEN o sistema SHALL retornar **409**
10. WHILE `ENABLE_TEST_ENDPOINTS != true` the system SHALL ignorar `mockOutcome` e usar lógica padrão determinística (`success`)
11. WHEN o cliente chama `POST /leads/{id}/retry-submission` E `confirmation.finalRegistration.registrationId` já existe THEN o sistema SHALL retornar **200** idempotente com o mesmo `registrationId`, sem chamar o sistema principal de novo (RF16-17)
12. WHEN o cliente chama `POST /leads/{id}/retry-submission` E o lead está em `failed_retryable` OU `pending_verification` (sem `registrationId` gravado) THEN o sistema SHALL chamar o sistema principal mockado de novo, seguindo as mesmas regras dos itens 4-8
13. IF nunca houve tentativa de confirmação anterior no retry-submission THEN o sistema SHALL retornar **409**
14. The system SHALL usar o mesmo mecanismo de mutex em `confirm` e `retry-submission` (nenhum dos dois pode rodar concorrentemente pro mesmo lead)

**Independent Test**: Duas chamadas simultâneas de `confirm` pro mesmo lead — uma processa (200/202/422/503/504), a outra recebe 409.

---

### P1-7: Listagem e recuperação de lead ⭐ MVP

**User Story**: Como cliente ou operador, quero listar e consultar leads pelo id, pra retomar um fluxo interrompido ou verificar o status de um cadastro.

**Why P1**: Necessário pra retomada (frontend) e pra suíte de avaliação inspecionar o estado.

**Acceptance Criteria**:

1. WHEN o cliente chama `GET /leads` THEN o sistema SHALL retornar lista paginada com `id`, `status`, `progress.currentStep`, `createdAt`, `updatedAt`, `finalRegistration.registrationId` (quando existir)
2. The system SHALL aceitar query opcionais `status`, `currentStep`, `page`, `pageSize` (e `cpf`, extensão aditiva não-contratual)
3. WHILE `pageSize` não informado the system SHALL usar default 20; WHILE informado acima de 100 the system SHALL clampar em 100
4. The system SHALL ordenar por `createdAt` desc por padrão
5. WHEN o cliente chama `GET /leads/{id}` THEN o sistema SHALL retornar o lead completo, incluindo documentos ativos associados (via `lead_documents`)
6. IF o lead não existir em `GET /leads/{id}` THEN o sistema SHALL retornar **404**

**Independent Test**: Cria 3 leads com CPFs diferentes, filtra por `status=in_progress`, confirma paginação com `pageSize=2`.

---

### P1-8: Contrato de erros e concorrência otimista ⭐ MVP

**User Story**: Como consumidor da API (frontend ou suíte de avaliação), quero erros previsíveis e proteção contra updates concorrentes perdidos, pra confiar no estado retornado.

**Why P1**: Requisito transversal obrigatório (README seção 6) — testado pela suíte automatizada de concorrência.

**Acceptance Criteria**:

1. The system SHALL retornar todo erro no formato Problem Details (RFC 9457) com no mínimo `type`, `title`, `status`, `detail`
2. The system SHALL usar tipos de exceção distintos para 404 (não encontrado) e 409 (conflito) — nunca o mesmo tipo genérico pros dois
3. WHEN um `PUT /leads/{id}/steps/*` recebe `expectedVersion` que não casa com a versão atual THEN o sistema SHALL retornar **409** com `extensions.currentVersion`
4. The system SHALL persistir cada update de etapa via operação atômica (`findOneAndUpdate` filtrado) que nunca substitui o documento inteiro, preservando dados de outras etapas já preenchidas
5. The system SHALL incrementar `version` atomicamente a cada update bem-sucedido, com ou sem `expectedVersion` informado
6. The system SHALL expor apenas DTOs nas fronteiras da API — nenhum documento MongoDB exposto diretamente

**Independent Test**: Update na etapa 4 não apaga `consultation`/`simulations`/`identification` já gravados; update com `expectedVersion` divergente retorna 409 com `currentVersion` correto.

---

### P1-9: Requisitos não-funcionais transversais ⭐ MVP

**User Story**: Como responsável pela operação, quero que a aplicação suba localmente sem passos manuais e não vaze dados sensíveis em log, pra rodar com segurança mínima e ser avaliável pela suíte de CI.

**Why P1**: Pré-requisito de entrega (README seção 9 e CI).

**Acceptance Criteria**:

1. The system SHALL usar operações assíncronas em toda chamada de I/O (Mongo, mock, upload)
2. The system SHALL ler configuração sensível (connection string) via variável de ambiente, nunca hardcoded ou commitada
3. The system SHALL logar de forma estruturada SEM CPF ou dados bancários em texto claro (mascarar/omitir esses campos)
4. WHEN `docker compose up --build` é executado THEN o sistema SHALL subir API + Mongo + frontend sem passo manual adicional
5. The system SHALL manter `GET /health` retornando 200 (CI já valida isso)

**Independent Test**: `docker compose up --build` limpo sobe as 3 stacks; grep nos logs não encontra CPF nem dados bancários em claro.

---

### P2-1: Modal de reutilização de CPF

**User Story**: Como cliente que já iniciou um cadastro, quero ser avisado se meu CPF já tem um lead em andamento, pra decidir continuar de onde parei ou começar do zero.

**Why P2**: Melhora UX e evita leads duplicados confusos, mas não é exigido pelo contrato (README seção 13.7 só pede a explicação, não a implementação).

**Acceptance Criteria**:

1. WHEN o cliente preenche o CPF na etapa 1 THEN o frontend SHALL consultar `GET /leads?cpf={cpf}&status=draft,in_progress,pending_confirmation,confirming,failed_retryable,pending_verification` antes de submeter
2. IF a consulta retornar ao menos um lead ativo THEN o frontend SHALL exibir modal com as opções "Continuar de onde parei" e "Começar novo"
3. WHEN o cliente escolhe "Continuar de onde parei" THEN o frontend SHALL buscar `GET /leads/{id}` e navegar pro `progress.currentStep`
4. WHEN o cliente escolhe "Começar novo" THEN o frontend SHALL prosseguir com `POST /leads/consultation` normalmente

**Independent Test**: Preenche CPF já usado num lead `in_progress` — modal aparece; escolher "Continuar" navega direto pro passo salvo.

---

### P2-2: Documentação OpenAPI/Swagger

**User Story**: Como avaliador/desenvolvedor, quero explorar a API via Swagger UI, pra entender o contrato sem ler código.

**Why P2**: Recomendado no README (seção 3/11), baixo custo em minimal API .NET.

**Acceptance Criteria**:

1. WHEN a API sobe em ambiente de desenvolvimento THEN o sistema SHALL expor `/swagger` com os schemas de request/response de todas as rotas

---

## Edge Cases

- IF `POST /leads/consultation` recebe CPF malformado THEN o sistema SHALL retornar **400** (validação de formato)
- IF upload excede 10MB THEN o sistema SHALL retornar **400** antes de persistir no GridFS
- IF `confirm` é chamado num lead já `completed` THEN o sistema SHALL retornar **409** (Cenário 9/RF14 combinados com mutex)
- WHEN duas requisições `PUT /leads/{id}/steps/identification` chegam simultaneamente com o mesmo `expectedVersion` THEN apenas uma SHALL suceder (200); a outra SHALL receber **409**
- IF `retry-submission` é chamado sem nunca ter havido `confirm` THEN o sistema SHALL retornar **409** (Cenário 10-11)
- WHEN a consulta de elegibilidade (etapa 1) falha (`unavailable`) THEN o lead SHALL permanecer `in_progress`, permitindo nova tentativa via `PUT /leads/{id}/steps/consultation` (Cenário 2)
- WHEN uma nova simulação é criada, a anterior SHALL permanecer no histórico com `selected=false` (Cenário 4)

---

## Requirement Traceability

| Requirement ID | Story | Phase | Status |
| --- | --- | --- | --- |
| LEAD-01 | P1-1: Consulta | Design | Pending |
| LEAD-02 | P1-1: Consulta | Design | Pending |
| LEAD-03 | P1-1: Consulta | Design | Pending |
| LEAD-04 | P1-1: Consulta | Design | Pending |
| LEAD-05 | P1-1: Consulta | Design | Pending |
| LEAD-06 | P1-1: Consulta | Design | Pending |
| LEAD-07 | P1-1: Consulta | Design | Pending |
| LEAD-08 | P1-2: Simulação | Design | Pending |
| LEAD-09 | P1-2: Simulação | Design | Pending |
| LEAD-10 | P1-2: Simulação | Design | Pending |
| LEAD-11 | P1-2: Simulação | Design | Pending |
| LEAD-12 | P1-2: Simulação | Design | Pending |
| LEAD-13 | P1-2: Simulação | Design | Pending |
| LEAD-14 | P1-2: Simulação | Design | Pending |
| LEAD-15 | P1-2: Simulação | Design | Pending |
| LEAD-16 | P1-3: Identificação | Design | Pending |
| LEAD-17 | P1-3: Identificação | Design | Pending |
| LEAD-18 | P1-3: Identificação | Design | Pending |
| LEAD-19 | P1-3: Identificação | Design | Pending |
| LEAD-20 | P1-3: Identificação | Design | Pending |
| LEAD-21 | P1-3: Identificação | Design | Pending |
| LEAD-22 | P1-4: Dados prof./bancários | Design | Pending |
| LEAD-23 | P1-4: Dados prof./bancários | Design | Pending |
| LEAD-24 | P1-4: Dados prof./bancários | Design | Pending |
| LEAD-25 | P1-4: Dados prof./bancários | Design | Pending |
| LEAD-26 | P1-5: Anexos | Design | Pending |
| LEAD-27 | P1-5: Anexos | Design | Pending |
| LEAD-28 | P1-5: Anexos | Design | Pending |
| LEAD-29 | P1-5: Anexos | Design | Pending |
| LEAD-30 | P1-5: Anexos | Design | Pending |
| LEAD-31 | P1-5: Anexos | Design | Pending |
| LEAD-32 | P1-5: Anexos | Design | Pending |
| LEAD-33 | P1-5: Anexos | Design | Pending |
| LEAD-34 | P1-5: Anexos | Design | Pending |
| LEAD-35 | P1-6: Confirmação/retry | Design | Pending |
| LEAD-36 | P1-6: Confirmação/retry | Design | Pending |
| LEAD-37 | P1-6: Confirmação/retry | Design | Pending |
| LEAD-38 | P1-6: Confirmação/retry | Design | Pending |
| LEAD-39 | P1-6: Confirmação/retry | Design | Pending |
| LEAD-40 | P1-6: Confirmação/retry | Design | Pending |
| LEAD-41 | P1-6: Confirmação/retry | Design | Pending |
| LEAD-42 | P1-6: Confirmação/retry | Design | Pending |
| LEAD-43 | P1-6: Confirmação/retry | Design | Pending |
| LEAD-44 | P1-6: Confirmação/retry | Design | Pending |
| LEAD-45 | P1-6: Confirmação/retry | Design | Pending |
| LEAD-46 | P1-6: Confirmação/retry | Design | Pending |
| LEAD-47 | P1-6: Confirmação/retry | Design | Pending |
| LEAD-48 | P1-6: Confirmação/retry | Design | Pending |
| LEAD-49 | P1-7: Listagem/recuperação | Design | Pending |
| LEAD-50 | P1-7: Listagem/recuperação | Design | Pending |
| LEAD-51 | P1-7: Listagem/recuperação | Design | Pending |
| LEAD-52 | P1-7: Listagem/recuperação | Design | Pending |
| LEAD-53 | P1-7: Listagem/recuperação | Design | Pending |
| LEAD-54 | P1-7: Listagem/recuperação | Design | Pending |
| LEAD-55 | P1-8: Erros/concorrência | Design | Pending |
| LEAD-56 | P1-8: Erros/concorrência | Design | Pending |
| LEAD-57 | P1-8: Erros/concorrência | Design | Pending |
| LEAD-58 | P1-8: Erros/concorrência | Design | Pending |
| LEAD-59 | P1-8: Erros/concorrência | Design | Pending |
| LEAD-60 | P1-8: Erros/concorrência | Design | Pending |
| LEAD-61 | P1-9: NFR transversais | Design | Pending |
| LEAD-62 | P1-9: NFR transversais | Design | Pending |
| LEAD-63 | P1-9: NFR transversais | Design | Pending |
| LEAD-64 | P1-9: NFR transversais | Design | Pending |
| LEAD-65 | P1-9: NFR transversais | Design | Pending |
| LEAD-66 | P2-1: Modal CPF | - | Pending |
| LEAD-67 | P2-1: Modal CPF | - | Pending |
| LEAD-68 | P2-1: Modal CPF | - | Pending |
| LEAD-69 | P2-1: Modal CPF | - | Pending |
| LEAD-70 | P2-2: Swagger | - | Pending |

**Coverage:** 70 requisitos totais, 0 mapeados pra tasks ainda, 70 não-mapeados ⚠️ (normal nesta fase — Tasks vem depois de Design)

---

## Success Criteria

- [ ] Todas as 12 rotas do contrato implementadas com shape/status exatos da seção 5
- [ ] `docker compose up --build` sobe stack completa sem passo manual; CI (`ci.yml`) verde
- [ ] Suíte de testes cobre os 8 casos mínimos da seção 8 do README, incluindo teste de concorrência (duas confirmações simultâneas)
- [ ] Nenhum CPF/dado bancário em texto claro nos logs
- [ ] README final documenta modelagem NoSQL com trade-offs (seção 6 do desafio)
