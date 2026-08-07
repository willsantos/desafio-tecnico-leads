# Desafio Técnico C# Pleno/Sênior — Recuperação de Leads em Cadastro de Empréstimo Consignado

Bem-vindo(a)! Este repositório é o ponto de partida do desafio. Ele já contém:

- `backend/` — esqueleto de uma Web API .NET 10 (apenas `/health` implementado), com `MongoDB.Driver` já referenciado no `.csproj`;
- `frontend/` — esqueleto React 18 + TypeScript + Vite;
- `docker-compose.yml` — sobe API, banco (**MongoDB**) e frontend.

Você pode reorganizar a estrutura interna como preferir (camadas, projetos, pastas), desde que o **Contrato de API** (seção 5) e o `docker compose up` continuem funcionando.

---

## 1. Contexto

Uma instituição financeira possui um fluxo de cadastro de empréstimo consignado dividido em **seis etapas**. O cadastro definitivo do cliente e da proposta só deve ser realizado após a confirmação da sexta etapa.

O cliente pode abandonar o processo, fechar o navegador, encontrar um erro durante o cadastro, ou o sistema principal pode falhar no momento da confirmação. Para evitar a perda das informações já fornecidas, a aplicação deve armazenar progressivamente os dados em um banco **NoSQL**, na forma de um **lead de recuperação**.

O lead permite: preservar os dados preenchidos antes da conclusão do cadastro; identificar em qual etapa o cliente interrompeu o processo; permitir a retomada do fluxo; recuperar os dados caso o cadastro definitivo falhe; realizar uma nova tentativa de cadastro no sistema principal.

**O lead não representa, por si só, um cadastro definitivo de empréstimo consignado.** São dois conceitos distintos:

- **Lead de recuperação**: registro parcial no banco NoSQL, pode conter dados incompletos, atualizado progressivamente.
- **Cadastro definitivo**: registro efetivado no sistema principal (mockado neste desafio), somente após a confirmação da etapa 6.

O foco principal do desafio está na **arquitetura da solução**, na **modelagem NoSQL** e no tratamento correto da diferença entre lead parcial e cadastro definitivo.

---

## 2. Prazo

Prazo sugerido: até 7 dias corridos. Não é necessário um produto pronto para produção — esperamos um protótipo funcional que demonstre capacidade de modelar o domínio, estruturar integrações, preservar jornadas incompletas e justificar decisões arquiteturais.

---

## 3. Tecnologias obrigatórias

**Backend**: C#; ASP.NET Core com .NET 10; **MongoDB** (driver oficial `MongoDB.Driver`, já referenciado no `.csproj`); API REST; documentação OpenAPI/Swagger (recomendado, ver seção 11); injeção de dependência; DTOs nas fronteiras da API (nunca expor os documentos MongoDB diretamente).

**Frontend**: React 18; componentização adequada; separação entre páginas, componentes, serviços e tipos — não concentrar tudo em `App.tsx`.

O backend e a modelagem NoSQL são a parte principal da avaliação (juntos, 55% da nota — seção 12). A interface deve permitir usar os fluxos essenciais, mas não é avaliada principalmente por aspectos visuais.

---

## 4. Escopo funcional — fluxo de 6 etapas

### 4.1. Etapa 1 — Consulta

Coleta as informações iniciais para consultar a elegibilidade do cliente: CPF; data de nascimento; tipo de benefício/vínculo; número do benefício/matrícula; instituição pagadora; autorização para consulta.

O envio desta etapa **cria o lead** (ver seção 5, `POST /leads/consultation`) e executa uma consulta de elegibilidade mockada. O resultado da consulta (elegibilidade + margem disponível) é associado ao lead, independentemente do desfecho da consulta — se a consulta estiver indisponível, o lead ainda é criado com os dados informados preservados, permitindo nova tentativa (Cenário 2).

### 4.2. Etapa 2 — Simulação

Apresenta uma simulação de empréstimo. **A fórmula de cálculo é fornecida abaixo — não desenvolva sua própria fórmula financeira, apenas implemente exatamente esta:**

- Taxa mensal fixa mockada: `rate = 0.018` (1,8% a.m.).
- Parcela pela Tabela Price: `installmentAmount = requestedAmount * (rate * (1+rate)^installments) / ((1+rate)^installments - 1)`, arredondado para **2 casas decimais**.
- `totalAmount = installmentAmount * installments`, arredondado para **2 casas decimais**.
- `availableMargin` vem do resultado da consulta da etapa 1 (`consultation.result.availableMargin`), não recalculado aqui.

Cada simulação realizada fica associada ao lead (histórico cumulativo — simulações antigas não são apagadas). Uma nova simulação passa a ser a **selecionada**; a resposta sempre indica qual simulação está selecionada no momento (Cenário 4).

Exemplo: `requestedAmount=10000`, `installments=24` → `installmentAmount = 10000 * (0.018*1.018^24)/(1.018^24-1) = 516.81`, `totalAmount = 516.81 * 24 = 12403.44` (produto dos dois valores já arredondados, não do valor de parcela não arredondado).

### 4.3. Etapa 3 — Identificação do cliente

Coleta os dados de identificação: nome completo; CPF; data de nascimento; e-mail; telefone; endereço; nome da mãe; estado civil; tipo de documento (`CNH` ou `RG`); número do documento; órgão emissor; UF emissora; data de emissão.

O **tipo de documento** informado aqui é usado depois para validar o documento enviado na etapa 5 (RF10) — se o cliente informar `CNH`, a etapa 5 exige um arquivo classificado como `CNH`; se informar `RG`, exige `RG`.

Esta etapa também executa uma **consulta de identificação mockada** (`mockOutcome` ∈ `found | not_found | diverging | unavailable`), com o resultado persistido no lead. Falha ou divergência não apaga os dados já digitados — o cliente pode continuar ou corrigir (Cenário 6).

### 4.4. Etapa 4 — Dados profissionais e bancários

Dados profissionais: tipo de vínculo; empresa/órgão; matrícula; cargo; renda mensal; data de admissão.
Dados bancários: banco; agência; conta; dígito; tipo de conta; titularidade; chave Pix (opcional).

Armazenados no lead — **não** geram cadastro definitivo.

### 4.5. Etapa 5 — Anexos

Upload de documento pessoal (compatível com o tipo informado na etapa 3) e contracheque, associados a um **lead já existente** — o upload nunca cria um lead novo (RF11) e pode ser feito em envios separados, com abandono entre eles (Cenário 7).

Um documento pessoal cujo subtipo diverge do informado na etapa 3 (ex.: `RG` enviado quando a etapa 3 informou `CNH`) impede a conclusão da etapa (Cenário 8, RF13).

Estados do documento: `pending` → `uploaded` → (`validated` | `invalid`) → (`replaced` | `deleted`).

### 4.6. Etapa 6 — Confirmação e efetivação do cadastro

Apresenta um resumo completo (consulta, simulação selecionada, identificação, dados profissionais/bancários, documentos). Antes de confirmar, valida: simulação selecionada existe; consulta de identificação concluída; compatibilidade de documento; documentos obrigatórios enviados; sem pendências impeditivas — qualquer pendência bloqueia a confirmação e lista os motivos exatos (Cenário 9).

Confirmado, o backend chama o **sistema principal mockado**, que pode retornar: sucesso; recusado; erro de validação; indisponível; timeout; indeterminado (ver seção 5, `mockOutcome`). Em qualquer desfecho que não seja sucesso, o lead é preservado integralmente e uma nova tentativa é permitida sem duplicar o cadastro definitivo (RF15-17, Cenários 10-11).

---

## 5. CONTRATO DE API (obrigatório — não altere rotas, portas nem nomes de campos)

A avaliação é feita por uma suíte automatizada que consome exatamente este contrato. Divergências de rota, porta ou shape de JSON reprovam os testes automatizados.

- A API deve escutar em **`http://localhost:8080`** (via `docker compose up`).
- O frontend deve escutar em **`http://localhost:3000`**.
- JSON em **camelCase**; enums como **string**.
- Datas em ISO 8601 (UTC).
- A API deve habilitar **CORS** para o frontend.
- Erros seguem **Problem Details** (RFC 9457), com pelo menos `type`, `title`, `status`, `detail`:

```json
{
  "type": "https://.../consignado-leads/version-conflict",
  "title": "Conflict",
  "status": 409,
  "detail": "A versão enviada (5) não corresponde à versão atual do lead (7).",
  "extensions": { "currentVersion": 7 }
}
```

Códigos HTTP: **404** lead/documento não encontrado; **400** requisição inválida (payload malformado, arquivo com formato/tamanho inválido); **409** conflito (documento incompatível, `expectedVersion` divergente, confirmação/retry já em andamento, lead já `completed`); **422** confirmação recusada por pendências (dados/documentos obrigatórios faltando) ou pelo sistema principal (`rejected`/`validationError`); **503** sistema principal indisponível; **504** timeout do sistema principal; **500** erro inesperado.

### Mecanismo de teste determinístico — `mockOutcome` + `ENABLE_TEST_ENDPOINTS`

A suíte de avaliação precisa forçar deterministicamente os desfechos dos 3 pontos de integração mockados (consulta de elegibilidade, consulta de identificação, sistema principal). Para isso, os três endpoints que os disparam aceitam um campo opcional `mockOutcome` no corpo — **só respeitado quando a variável de ambiente `ENABLE_TEST_ENDPOINTS=true`** (nunca ativa em produção real; o `docker-compose.yml` do candidato não precisa setá-la, o harness de avaliação injeta essa variável ao subir a stack para testar). Sem `mockOutcome` ou com a flag desligada, use sua própria lógica padrão (determinística ou aleatória, à sua escolha).

| Endpoint | Valores aceitos em `mockOutcome` |
| --- | --- |
| `POST /leads/consultation` | `eligible` \| `not_eligible` \| `unavailable` |
| `PUT /leads/{id}/steps/identification` | `found` \| `not_found` \| `diverging` \| `unavailable` |
| `POST /leads/{id}/confirm` | `success` \| `rejected` \| `validationError` \| `unavailable` \| `timeout` \| `indeterminate` |

### Enum `status` do lead

`draft` \| `in_progress` \| `pending_confirmation` \| `confirming` \| `completed` \| `failed_retryable` \| `pending_verification` \| `abandoned`

### Rotas

```http
POST   /leads/consultation
PUT    /leads/{id}/steps/consultation
POST   /leads/{id}/steps/simulation
PATCH  /leads/{id}/steps/simulation/{simulationId}/select
PUT    /leads/{id}/steps/identification
PUT    /leads/{id}/steps/professional-banking-data
POST   /leads/{id}/documents
GET    /leads/{id}/documents
DELETE /leads/{id}/documents/{documentId}
POST   /leads/{id}/confirm
POST   /leads/{id}/retry-submission
GET    /leads
GET    /leads/{id}
```

- **`POST /leads/consultation`** — corpo: `cpf`, `birthDate` (`YYYY-MM-DD`), `benefitType` (`retirement`\|`pension`\|`public_servant`\|`clt`), `benefitNumber`, `payingInstitution`, `consultationAuthorized` (boolean, deve ser `true`), `mockOutcome` (opcional). **201** com o lead criado (`status="in_progress"`, `version=1`, `progress.currentStep="consultation"`, `consultation.result` com o desfecho). **400** se `consultationAuthorized=false` ou campo obrigatório ausente. **Cria o lead mesmo se a consulta retornar `unavailable`** (RF01/RF07).
- **`PUT /leads/{id}/steps/consultation`** — mesmo corpo, sem criar lead novo; corrige/re-executa a consulta de um lead existente. **200**; **404** se o lead não existir; **409** com `expectedVersion` divergente (corpo aceita `expectedVersion` opcional).
- **`POST /leads/{id}/steps/simulation`** — corpo: `requestedAmount` (number), `installments` (int). **201** com a simulação criada e marcada `selected=true` (as anteriores passam `selected=false`, mas continuam na lista — RF08). **404** se o lead não existir. **409** se a etapa de consulta ainda não foi concluída.
- **`PATCH /leads/{id}/steps/simulation/{simulationId}/select`** — sem corpo. **200** com a simulação selecionada trocada, sem recalcular. **404** se lead ou simulação não existirem.
- **`PUT /leads/{id}/steps/identification`** — corpo: `fullName`, `cpf`, `birthDate`, `email`, `phone`, `address`, `motherName`, `maritalStatus`, `documentType` (`CNH`\|`RG`), `documentNumber`, `issuingAuthority`, `issuingState`, `issueDate`, `mockOutcome` (opcional), `expectedVersion` (opcional). **200** com o lead atualizado (`identification` + `identification.query.result`). **404**; **409** (version conflict).
- **`PUT /leads/{id}/steps/professional-banking-data`** — corpo: `professionalData` (`employmentType`, `company`, `registrationNumber`, `role`, `monthlyIncome`, `admissionDate`), `bankingData` (`bank`, `agency`, `account`, `accountDigit`, `accountType`, `accountHolder`, `pixKey` opcional), `expectedVersion` (opcional). **200**; **404**; **409**.
- **`POST /leads/{id}/documents`** — `multipart/form-data`: `file`, `type` (`personal_document`\|`payslip`), `personalDocumentSubtype` (`CNH`\|`RG`, obrigatório quando `type=personal_document`). **201** com o metadado do documento (`status="uploaded"`). **404** se o lead não existir (upload **nunca** cria lead — RF11). **400** se formato/tamanho inválido (limite: 10MB, `image/jpeg`\|`image/png`\|`application/pdf`). Reenvio do mesmo `type` marca a versão anterior como `replaced` (RF12).
- **`GET /leads/{id}/documents`** — **200** com a lista de documentos ativos (exclui `deleted` e `replaced` — um reenvio do mesmo `type` marca a versão anterior como `replaced` e ela some da lista ativa, mas continua no banco como histórico). **404** se o lead não existir.
- **`DELETE /leads/{id}/documents/{documentId}`** — marca `status="deleted"` (soft-delete). **204**; **404**.
- **`POST /leads/{id}/confirm`** — corpo opcional: `mockOutcome`. **200** com `confirmation.finalRegistration` preenchido quando `mockOutcome=success`. **422** com lista de pendências se dados/documentos obrigatórios faltarem — **nenhuma chamada ao sistema principal é feita nesse caso** (RF14). **422** se `mockOutcome` ∈ {`rejected`,`validationError`}; **503** se `unavailable`; **504** se `timeout` — em qualquer um desses, o lead é preservado integralmente e `status` vira `"failed_retryable"`. `mockOutcome=indeterminate` → **202**, `status="pending_verification"`. **409** se já existe uma confirmação em andamento para o mesmo lead (mutex) ou se o lead já está `completed`.
- **`POST /leads/{id}/retry-submission`** — sem corpo. Se `confirmation.finalRegistration.registrationId` já existe, **200** idempotente com o mesmo `registrationId`, **sem** chamar o sistema principal de novo (RF16-17). **409** se nunca houve tentativa de confirmação anterior. Mesmo mutex de `confirm`.
- **`GET /leads`** — query opcional `status`, `currentStep`, `page`, `pageSize`. **200** com lista paginada: `id`, `status`, `progress.currentStep`, `createdAt`, `updatedAt`, `finalRegistration.registrationId` (quando existir).
- **`GET /leads/{id}`** — **200** com o lead completo, incluindo os documentos associados (via `lead_documents`). **404** se não existir.

Shape de um lead (exemplo, etapa de identificação concluída):

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "status": "in_progress",
  "version": 3,
  "progress": {
    "currentStep": "identification",
    "startedSteps": ["consultation", "simulation", "identification"],
    "completedSteps": ["consultation", "simulation", "identification"],
    "pendingItems": [],
    "resumeStep": "professional-banking-data",
    "lastUpdatedAt": "2026-07-30T15:00:00Z"
  },
  "consultation": { "input": { "cpf": "12345678900" }, "result": { "outcome": "eligible", "availableMargin": 350.00, "checkedAt": "2026-07-30T14:50:00Z" } },
  "simulations": [
    { "id": "sim-1", "requestedAmount": 10000, "installments": 24, "interestRate": 0.018, "installmentAmount": 516.81, "totalAmount": 12403.44, "selected": true, "simulatedAt": "2026-07-30T14:55:00Z" }
  ],
  "identification": { "fullName": "…", "documentType": "CNH", "query": { "outcome": "found" } },
  "professionalData": null,
  "bankingData": null,
  "confirmation": { "confirmedAt": null, "attempts": [], "finalRegistration": null },
  "createdAt": "2026-07-30T14:50:00Z",
  "updatedAt": "2026-07-30T15:00:00Z"
}
```

> **`progress.resumeStep`** (campo derivado, aditivo): próxima etapa acionável do lead (`consultation`, `simulation`, `identification`, `professional-banking-data`, `documents` ou `confirmation`), calculada pelo backend a partir de `status`, `completedSteps` e documentos ativos. **Não é persistido** — calculado em tempo de leitura no mapper, então leads antigos também o recebem sem migração. `currentStep` mantém a semântica de "última etapa concluída" (cada handler grava `CurrentStep = X` e adiciona X em `completedSteps` ao mesmo tempo); o backend nunca o avança além de `professional-banking-data`. Por isso `resumeStep` é a fonte canônica de "onde retomar" — clientes não precisam inferir.

---

## 6. Requisitos técnicos — modelagem NoSQL

**Banco de dados**: MongoDB (standalone, sem replica set — não é necessário para este desafio). Justifique no README sua modelagem: o que fica embutido no documento do lead vs. em coleção própria; como você previne crescimento excessivo do documento; quais índices criou e por quê; como versiona o schema; como distingue campo ausente, `null` e vazio.

**Atomicidade dos updates parciais**: cada etapa deve ser salva sem apagar dados de outras etapas já preenchidas — nunca substitua o documento inteiro num update de uma única etapa.

**Concorrência**: MongoDB não tem `SELECT ... FOR UPDATE`. Documente e implemente sua estratégia — por exemplo, concorrência otimista via campo `version` (o contrato aceita `expectedVersion` opcional nos `PUT /steps/*`) e/ou updates atômicos filtrados (`findOneAndUpdate`) para evitar que duas confirmações concorrentes cheguem simultaneamente ao sistema principal.

**Idempotência**: `retry-submission` não pode gerar dois cadastros definitivos para o mesmo lead.

**DTOs**: documentos MongoDB não devem ser expostos diretamente pela API.

**Tratamento de erros**: estratégia centralizada, com tipos de exceção **distintos** para 404 (não encontrado) e 409 (conflito) — nunca o mesmo tipo genérico para os dois.

---

## 7. Frontend

A aplicação React deve permitir navegar pelas 6 etapas do cadastro, com indicador de progresso, salvamento progressivo, retomada de um lead existente (via seu `id`), upload de documentos, resumo antes da confirmação e tratamento de carregamento/erro.

O projeto não deve concentrar páginas, chamadas HTTP, estado e componentes num único `App.tsx`. Não é necessário investir em identidade visual.

---

## 8. Testes

A solução deve possuir testes automatizados. São esperados, no mínimo: teste do cálculo de simulação (seção 4.2); teste de que um update de etapa não apaga dados de outra etapa; teste de upload parcial (um documento sem o outro); teste de documento incompatível bloqueando a etapa 5; teste de confirmação com pendências; teste de falha do sistema principal preservando o lead; teste de retry idempotente; teste de concorrência (duas confirmações simultâneas do mesmo lead).

---

## 9. Requisitos não funcionais

A aplicação deve: utilizar operações assíncronas; não armazenar credenciais no repositório; possuir configuração por ambiente; fornecer logs minimamente estruturados **sem CPF ou dados bancários em texto claro**; possuir código legível e consistente; ser executável localmente com `docker compose up --build`, sem passos manuais.

---

## 10. Entregáveis

Repositório contendo: código-fonte do backend e frontend; testes automatizados; `README.md` com instruções de execução, decisões arquiteturais (em especial a modelagem NoSQL — seção 6), trade-offs e limitações conhecidas.

---

## 11. Diferenciais opcionais

Não são obrigatórios: autenticação/autorização; token de retomada dedicado com expiração; MongoDB Change Streams para detectar abandono; concorrência otimista completa em todas as etapas; URLs assinadas para upload; processamento assíncrono de documentos; antivírus simulado; auditoria detalhada; eventos de domínio; fila de retentativas; circuit breaker; observabilidade; documentação OpenAPI/Swagger; replica set MongoDB com transações multi-documento; ADRs.

Diferenciais não compensam problemas nos requisitos principais.

---

## 12. Critérios de avaliação

| Critério | Peso |
| --- | --- |
| Arquitetura e decisões técnicas | 25% |
| Modelagem NoSQL | 30% |
| Backend | 15% |
| Frontend | 10% |
| Documentação | 10% |
| Testes automatizados | 10% |

- **Arquitetura (25%)**: separação de responsabilidades, tratamento de erros (404 vs 409 distintos), decisões justificadas.
- **Modelagem NoSQL (30%)** — maior peso: estrutura do documento do lead, atomicidade dos updates parciais, modelagem dos documentos (embutido vs. referenciado), índices, estratégia de concorrência, versionamento de schema, convenção ausente/vazio/N/A.
- **Backend (15%)**: qualidade das consultas MongoDB, implementação real dos 3 mocks, idempotência.
- **Frontend (10%)**: fluxo das 6 etapas navegável, componentização.
- **Documentação (10%)**: README explica a modelagem com trade-offs, decisões justificadas.
- **Testes (10%)**: 100% automático (suíte black-box + concorrência + carga).

---

## 13. Perguntas para apresentação técnica

1. Por que MongoDB (e não SQL) para esse cenário?
2. O lead inteiro deveria estar em um único documento? Por que os documentos da etapa 5 ficam numa coleção separada?
3. Como evitar crescimento excessivo do documento do lead?
4. Como evita atualizações concorrentes perderem dados?
5. Como garante idempotência na efetivação do cadastro?
6. Como trataria um timeout cujo resultado é desconhecido (`indeterminate`)?
7. Como evitaria duplicidade de leads para o mesmo CPF?
8. Como protegeria CPF e dados bancários em log e em repouso?
9. Em quais situações um banco relacional seria mais adequado que Mongo aqui?
10. O que mudaria para operar essa aplicação em produção?

---

## 14. Observações finais

O desafio não é avaliado apenas pela quantidade de funcionalidades entregues. Uma solução menor, funcional, testada e bem justificada pode valer mais do que uma solução extensa e incompleta.

É permitido: simplificar partes da interface; documentar decisões que não puderam ser implementadas; usar bibliotecas adicionais. O contrato da seção 5 é a única parte que não pode ser alterada.

---

## Como executar

```bash
docker compose up --build
# API:      http://localhost:8080/health
# Frontend: http://localhost:3000
```

Sem passos manuais: o `docker-compose.yml` sobe `mongo` (com healthcheck), `api` (aguarda o Mongo saudável) e `frontend`, nessa ordem.

Documentação da API (Swagger UI, gerada via `Swashbuckle.AspNetCore`) fica disponível em `http://localhost:8080/swagger`, mas só quando `ASPNETCORE_ENVIRONMENT=Development` — o `docker-compose.yml` do repositório não define essa variável (roda em `Production` por padrão, sem Swagger exposto), então para inspecionar os schemas localmente rode a API fora do Docker com essa variável setada, por exemplo:

```bash
cd backend/src/ConsignadoLeads.Api
ASPNETCORE_ENVIRONMENT=Development ConnectionStrings__MongoDb="mongodb://localhost:27017/consignado_leads" dotnet run
# Swagger UI: http://localhost:5000/swagger (ou a porta que o dotnet run reportar)
```

## CI incluída

O workflow `.github/workflows/ci.yml` roda a cada push: sobe a stack com `docker compose up` e verifica `GET /health` e o frontend. É o pré-requisito mínimo da entrega — mantenha-o verde. A avaliação final usa uma suíte automatizada adicional contra o contrato de API da seção 5.

---

## Modelagem NoSQL — decisões e trade-offs (seção 6)

Respostas às perguntas literais da seção 6, ancoradas no código de `backend/src/ConsignadoLeads.Api/Core/Models/Lead.cs` e `Core/MongoContext.cs`.

### O que fica embutido no documento do lead vs. em coleção própria

Tudo que descreve o **estado do lead em si** fica embutido num único documento da coleção `leads`: `consultation`, `simulations` (histórico cumulativo), `identification`, `professionalData`, `bankingData` e `confirmation` (com sua lista de `attempts`). Cada etapa é uma sub-árvore do mesmo documento — não uma coleção separada — porque o caso de uso dominante é "carregar o lead inteiro para renderizar o resumo/retomar o fluxo" (um `GET /leads/{id}`), e o MongoDB otimiza para ler um documento inteiro de uma vez. Dividir isso em coleções obrigaria a fazer *joins* aplicacionais para reconstruir algo que é lido e escrito quase sempre como uma unidade.

Os **documentos anexados na etapa 5** (`lead_documents`) são a exceção deliberada: vivem numa coleção própria, referenciada por `leadId` (chave de aplicação, sem *foreign key* nativa do Mongo), e o binário do arquivo fica em GridFS (`fs.files`/`fs.chunks`), referenciado a partir de `lead_documents.gridFsFileId`. Três motivos: (1) binários (até 10MB por arquivo) inflariam rapidamente o documento do lead até o limite de 16MB do BSON; (2) o histórico de reenvios (`replaced`) precisa sobreviver independente do ciclo de vida do lead; (3) o padrão de acesso é diferente — o documento é lido por si só (`GET /leads/{id}/documents`), não como parte de toda leitura do lead.

### Como se previne o crescimento excessivo do documento

- Binários nunca entram no documento do lead — ver acima.
- `simulations` cresce só com ações explícitas do usuário na etapa 2 (uma simulação por chamada de `POST /steps/simulation`); não há como um único fluxo gerar centenas de entradas sem centenas de cliques reais.
- `confirmation.attempts` cresce só com tentativas de confirmação/retry — limitado pelo mutex de status (uma tentativa por vez) e pelo comportamento humano, não por nenhum laço automático.
- Não há campos de auditoria granular (log de cada campo alterado) nem eventos de domínio persistidos no lead — se fossem necessários, iriam para uma coleção `lead_events` à parte, não embutidos.

### Índices criados e por quê

Definidos em `Core/MongoContext.cs:44-56` (`EnsureIndexesAsync`, chamado uma vez no startup — `createIndex` é idempotente, então repetir a chamada em cada boot é seguro):

| Coleção | Índice | Por quê |
| --- | --- | --- |
| `leads` | `{status: 1, "progress.currentStep": 1}` | Suporta `GET /leads?status=&currentStep=` (filtros do contrato) sem *collection scan*. |
| `leads` | `{"consultation.input.cpf": 1}` | Caminho natural para localizar leads de um CPF (retomada de fluxo, e a pergunta da seção 13 sobre duplicidade por CPF); hoje não há *unique constraint* aqui — ver limitações abaixo. |
| `leads` | `{createdAt: -1}` | Ordenação padrão de listagem (mais recentes primeiro) sem *sort* em memória. |
| `lead_documents` | `{leadId: 1, status: 1}` | Suporta `GET /leads/{id}/documents` (documentos ativos de um lead) e a consulta que `ConfirmationHandler` faz para montar `activeDocuments` antes de validar pendências — ambas filtram por `leadId` e excluem `deleted`/`replaced` via `status`. |

### Concorrência

Duas mecânicas distintas, ambas via *updates* atômicos filtrados (`findOneAndUpdate`), sem transações — MongoDB standalone não tem `SELECT ... FOR UPDATE` nem replica set aqui (AD-004):

- **Concorrência otimista por etapa** (`Consultation`, `Identification`, `ProfessionalBankingData`): o corpo aceita `expectedVersion` opcional; quando informado, o filtro do update vira `{_id, version: expectedVersion}`. Se o `MatchedCount` vier zero, um segundo `Find` distingue "lead não existe" (404) de "a versão mudou" (409 com `extensions.currentVersion`) — ver `ConsultationHandler.cs:61-101` e o mesmo padrão em `IdentificationHandler.cs` e `ProfessionalBankingDataHandler.cs`.
- **Mutex de confirmação** (`ConfirmationHandler.AcquireMutexAsync`): em vez de comparar `version`, o filtro exige `status ∉ {confirming, completed}` e o update seta `status="confirming"` atomicamente. Só uma chamada concorrente recebe um documento não-nulo de volta; a(s) outra(s) caem no *fallback* que devolve 409 (já em andamento) ou 409 (já `completed`). É o mecanismo coberto pelo teste de duas confirmações simultâneas (seção 8).

### Idempotência do `retry-submission`

`ConfirmationHandler.RetrySubmissionAsync` primeiro olha `confirmation.finalRegistration`: se já existe um `registrationId`, retorna 200 com o mesmo registro sem tocar no sistema principal mockado de novo — nenhuma chamada nova, nenhum mutex necessário, porque nada muda. Só quando não há registro final é que o mutex de confirmação é adquirido e o mock é chamado.

### Versionamento de schema

Todo documento `leads` carrega `schemaVersion` (int, hoje `= 1`, `Lead.cs:20`). Não há migração automática implementada — para um protótipo de 7 dias, a estratégia é: uma mudança de schema futura lê `schemaVersion`, aplica a lógica correta por versão na camada de leitura, e/ou roda um *backfill* script único; o campo existe desde já para que essa evolução não exija adivinhar a forma de documentos antigos.

### Convenção ausente / `null` / vazio

- **Ausente do documento BSON** = etapa ainda não alcançada. Os campos opcionais de sub-objeto (`Consultation.Result`, `Lead.Consultation`, `Lead.Identification`, `Lead.ProfessionalData`, `Lead.BankingData`, `Confirmation.ConfirmedAt`, `Confirmation.FinalRegistration`, `IdentificationData.Query`, `Address.Complement`, `BankingData.PixKey`, `ConfirmationAttempt.Reason`) usam `[BsonIgnoreIfNull]` — quando estão `null` no POCO, o driver simplesmente não escreve a chave no BSON. Isso mantém o documento pequeno nas fases iniciais do lead e reflete literalmente "esse dado não existe ainda", não "existe mas é vazio".
- **`null` no JSON de resposta** é a serialização dessa mesma ausência na fronteira da API — o exemplo de shape da seção 5 mostra isso (`"professionalData": null` antes da etapa 4).
- **Vazio** é reservado para coleções que fazem parte do formato do documento desde a criação, mas ainda não têm itens: `simulations` (`List<Simulation>`, default `new()`), `progress.startedSteps`/`completedSteps`/`pendingItems`, `confirmation.attempts` — todas inicializadas como lista vazia, nunca `null`, porque a spec trata "zero simulações" como um estado válido e distinto de "simulação nunca modelada".

---

## Trade-offs e limitações conhecidas

- **Sem autenticação/autorização.** Qualquer chamador com acesso à porta 8080 pode ler/escrever qualquer lead — aceitável para o escopo do desafio (seção 11 lista isso como diferencial opcional), mas é o primeiro item a resolver antes de produção.
- **Sem criptografia em repouso para CPF e dados bancários.** Eles ficam em texto claro no MongoDB (só os *logs* são mascarados — ver `Core/Logging/SensitiveDataMasker.cs`, que redige `cpf`, `documentNumber` e `bankingData` antes de qualquer `ILogger` gravar uma requisição/resposta). Em produção isso pediria ao menos *field-level encryption* do driver ou criptografia de disco no Mongo.
- **Sem detecção automática de `abandoned`.** O enum de `status` inclui `abandoned`, mas nada no backend transiciona um lead para esse estado sozinho — não há *job*/TTL nem MongoDB Change Streams (também citado como diferencial opcional na seção 11) observando inatividade. Um lead parado fica congelado no último `status` alcançado até que o cliente volte.
- **Sem `unique index` em `consultation.input.cpf`.** O índice existe para consulta rápida, mas não impede dois leads distintos para o mesmo CPF — resolver a pergunta 7 da seção 13 (evitar duplicidade) ficaria por conta de uma constraint adicional (unique index parcial, ou uma checagem de aplicação antes do `POST /leads/consultation`) fora do escopo implementado aqui.
- **`progress.resumeStep` é derivado, não persistido.** O backend nunca avança `progress.currentStep` além de `"professional-banking-data"`, e `currentStep` tem semântica de "última etapa concluída" (não "etapa ativa"). Para evitar que cada cliente inverta essa convenção, o backend expõe `progress.resumeStep` (campo aditivo), calculado em `Core/ResumeStepResolver` no momento de montar o `LeadDto` a partir de `status`, `completedSteps` e documentos ativos. O frontend ainda rastreia um cursor local (`step` no React context) para navegação dentro do wizard, mas a **decisão de "onde retomar"** ao carregar um lead usa `resumeStep` (com fallback para derivação local em respostas antigas sem o campo — `resolveStepId` em `frontend/src/shared/leadContext.tsx`).
- **Campos sem enum definido no contrato são texto livre no frontend.** `maritalStatus`, `employmentType` e `accountType` não têm uma lista de valores fechada na seção 5 do contrato, então os formulários (`IdentificationPage.tsx`, `ProfessionalBankingDataPage.tsx`) os tratam como `<input>` de texto simples em vez de `<select>` — o backend também os persiste como `string` livre (`Lead.cs`), sem validação de valores permitidos.
- **Upload de documento e metadado não são transacionais** (Mongo standalone, sem sessions) — uma falha entre o upload no GridFS e a escrita em `lead_documents` deixa um blob órfão no GridFS (inofensivo, nunca referenciado), nunca o inverso (AD-002).
