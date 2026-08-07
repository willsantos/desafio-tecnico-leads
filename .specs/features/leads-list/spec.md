# Leads List Specification

## Problem Statement

O backend expõe `GET /leads` e `GET /leads/{id}` desde o MVP, mas o frontend React só usa essa rota no modal de reutilização de CPF. Não existe uma tela dedicada para visualizar todas as propostas em andamento, nem uma URL que permita retomar um lead diretamente. Isso dificulta testes manuais, demonstrações e o uso real da aplicação — o usuário precisa decorar um `id` ou refazer a etapa 1 toda vez que recarregar a página.

## Goals

- [ ] Adicionar roteamento SPA leve ao frontend para suportar uma tela de listagem e retomada por URL.
- [ ] Criar a página `/leads` que lista todas as propostas paginadas, com filtros rápidos de `status`, `currentStep` e CPF.
- [ ] Permitir clicar numa proposta para carregá-la e retomar o wizard na etapa correta (`resolveStepId`).
- [ ] Exibir informações resumidas de forma legível: CPF mascarado, datas no padrão brasileiro, status traduzido e número de registro quando houver.
- [ ] Adicionar navegação global (cabeçalho) para alternar entre "Nova proposta" e "Propostas em andamento".
- [ ] Manter o fluxo atual de 6 etapas intacto: nenhum payload de API alterado.
- [ ] `npm run build` continua passando.

## Out of Scope

| Feature | Reason |
| --- | --- |
| Alterações no backend ou contrato de API | A rota `GET /leads` já existe; feature é frontend-only |
| Ações em lote na listagem (excluir, mudar status) | Fora do contrato; não há endpoints para isso |
| Ordenação customizável | `GET /leads` ordena por `createdAt` desc; manter padrão |
| Exportação de dados | Não solicitado |

---

## Assumptions & Open Questions

| Assumption / decision | Chosen default | Rationale | Confirmed? |
| --- | --- | --- | --- |
| Roteamento | `react-router-dom` v6 | Padrão do ecossistema React; nginx já serve `index.html` para qualquer rota (`try_files`) | y |
| Layout da listagem | Cards responsivos (mobile) + tabela simples (desktop) | Reaproveita `Card` existente; mobile-first | y |
| Filtro de CPF | Campo com máscara, envia dígitos crus | Consistente com o restante do app | y |
| Paginação | Botões "Anterior"/"Próxima" + info de página | `GET /leads` já retorna `page`, `pageSize`, `totalItems`, `totalPages` | y |
| Status traduzidos | Mapa frontend de `status` → label amigável | O contrato define os enums em inglês; a exibição é decisão de UX | y |

**Open questions:** none.

---

## User Stories

### P1-1: Roteamento SPA ⭐ MVP

**User Story**: Como operador, quero acessar `/leads` diretamente pelo navegador, pra ver a listagem sem depender de cliques no wizard.

**Why P1**: Sem rotas, a aplicação é uma única página sem URL navegável.

**Acceptance Criteria**:

1. WHEN a aplicação sobe THEN `react-router-dom` gerencia as rotas `/` e `/leads`.
2. WHEN o usuário acessa `/leads` THEN o frontend renderiza a página de listagem, sem exigir lead carregado.
3. WHEN o usuário acessa `/` THEN o frontend renderiza o wizard iniciando na etapa 1.
4. The system SHALL preservar o estado do lead ao navegar entre `/leads` e `/` (o `LeadProvider` continua por cima do router).

**Independent Test**: Acessar `http://localhost:3000/leads` exibe a listagem; acessar `http://localhost:3000/` exibe o wizard.

---

### P1-2: Página de listagem de propostas ⭐ MVP

**User Story**: Como operador, quero ver todas as propostas com status, etapa e datas, pra acompanhar cadastros em andamento.

**Why P1**: Essa é a funcionalidade central da feature.

**Acceptance Criteria**:

1. WHEN a página carrega THEN o frontend chama `GET /leads?page=1&pageSize=20` e exibe os resultados.
2. The system SHALL renderizar cada item como um card em mobile e como uma linha de tabela em desktop.
3. The system SHALL exibir: CPF mascarado, `status` traduzido, `currentStep` traduzido, `createdAt`/`updatedAt` no formato `dd/mm/aaaa hh:mm`, e `registrationId` quando existir.
4. WHEN não houver resultados THEN exibir mensagem amigável com CTA para criar nova proposta.
5. WHEN houver erro na API THEN exibir `Alert` com botão de retry.

**Independent Test**: Criar 3 leads pela API e abrir `/leads` — os 3 aparecem com CPF mascarado e status traduzido.

---

### P1-3: Filtros de listagem ⭐ MVP

**User Story**: Como operador, quero filtrar propostas por status, etapa ou CPF, pra encontrar rapidamente o cadastro que preciso.

**Why P1**: `GET /leads` já suporta esses filtros; a UI precisa expor de forma acessível.

**Acceptance Criteria**:

1. The system SHALL fornecer um `<Select>` de `status` com opções: Todos, `draft`, `in_progress`, `pending_confirmation`, `confirming`, `completed`, `failed_retryable`, `pending_verification`, `abandoned`.
2. The system SHALL fornecer um `<Select>` de `currentStep` com opções: Todos, `consultation`, `simulation`, `identification`, `professional-banking-data`.
3. The system SHALL fornecer um `<Input>` de CPF com máscara.
4. WHEN o usuário alterar um filtro THEN a página resetta para `page=1` e refaz a chamada.
5. The system SHALL exibir um botão "Limpar filtros" que volta tudo para o padrão.

**Independent Test**: Selecionar `status=in_progress` e CPF `12345678900` dispara `GET /leads?status=in_progress&cpf=12345678900`.

---

### P1-4: Retomada de proposta a partir da listagem ⭐ MVP

**User Story**: Como operador, quero clicar numa proposta e continuar o preenchimento exatamente na etapa em que parou.

**Why P1**: Sem retomada, a listagem é apenas visual.

**Acceptance Criteria**:

1. WHEN o usuário clica em "Continuar" num item da listagem THEN o frontend chama `GET /leads/{id}`, atualiza o `LeadContext` e navega para `/`.
2. The system SHALL usar `resolveStepId(lead)` para posicionar o wizard na etapa correta.
3. IF o lead não for encontrado (404) THEN exibir erro e remover o item da lista (ou atualizar a página).

**Independent Test**: Criar um lead parado na etapa 3, ir para `/leads`, clicar "Continuar" — o wizard abre na etapa de Identificação.

---

### P1-5: Navegação global ⭐ MVP

**User Story**: Como operador, quero alternar entre a listagem e o wizard a qualquer momento, sem depender do botão voltar do navegador.

**Why P1**: Melhora a usabilidade e torna a listagem descobrível.

**Acceptance Criteria**:

1. The system SHALL exibir um cabeçalho global com: título/identidade do sistema, link "Nova proposta" (`/`) e link "Propostas" (`/leads`).
2. WHEN o usuário está no wizard e clica em "Propostas" THEN o `LeadContext` não é resetado, permitindo voltar depois.
3. WHEN o usuário está na listagem e clica em "Nova proposta" THEN o wizard inicia limpo (caso não haja lead carregado) ou mantém o lead atual.

**Independent Test**: Navegar de `/` para `/leads` e voltar para `/` — o lead em andamento continua disponível.

---

### P2-1: Melhorar modal de reutilização de CPF

**User Story**: Como cliente, se meu CPF tiver mais de um lead ativo, quero escolher qual retomar.

**Why P2**: Hoje o `CpfReuseModal` pega sempre `activeLeads[0]`; com a listagem pronta, podemos reaproveitar o componente de seleção.

**Acceptance Criteria**:

1. WHEN `findActiveLeadsByCpf` retorna mais de um lead THEN o modal exibe a lista resumida (CPF, status, etapa, data) e permite escolher.
2. WHEN o usuário escolhe um lead THEN o frontend chama `GET /leads/{id}` e retoma no passo correto.
3. The system SHALL reutilizar o componente de card/linha da listagem quando possível.

**Independent Test**: Criar dois leads para o mesmo CPF; iniciar etapa 1 com esse CPF; o modal mostra os dois para escolha.

---

## Edge Cases

- IF o usuário recarregar `/leads` THEN a página recarrega a listagem do servidor sem perder filtros (os filtros podem ficar na URL como query params, mas isso é P2 opcional).
- IF `GET /leads` retornar `totalPages=0` THEN ocultar a paginação e mostrar estado vazio.
- IF a retomada retornar 404 THEN exibir erro e atualizar a listagem.
- IF o usuário clicar "Continuar" duas vezes rapidamente THEN desabilitar o botão até a navegação terminar.

---

## Requirement Traceability

| Requirement ID | Story | Phase | Status |
| --- | --- | --- | --- |
| LIST-01 | P1-1: Roteamento SPA | Design | Pending |
| LIST-02 | P1-2: Página de listagem | Design | Pending |
| LIST-03 | P1-3: Filtros de listagem | Design | Pending |
| LIST-04 | P1-4: Retomada de proposta | Design | Pending |
| LIST-05 | P1-5: Navegação global | Design | Pending |
| LIST-06 | P2-1: Modal CPF melhorado | Design | Pending |

---

## Success Criteria

- [ ] A rota `/leads` é acessível diretamente e renderiza a listagem.
- [ ] A listagem consome `GET /leads` com filtros e paginação.
- [ ] CPF, datas e status são exibidos de forma amigável.
- [ ] Clicar em "Continuar" carrega o lead e posiciona o wizard na etapa correta.
- [ ] `npm run build` passa sem erros.
- [ ] Nenhuma chamada à API foi alterada (rotas, payloads, porta).
