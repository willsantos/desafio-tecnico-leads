# Leads List Context

**Gathered:** 2026-08-06
**Spec:** `.specs/features/leads-list/spec.md`
**Status:** Ready for implementation

---

## Feature Boundary

Adicionar uma tela de listagem de propostas (leads) ao frontend React, acessível pela rota `/leads`, consumindo a rota `GET /leads` já existente no backend. A feature também melhora a retomada de propostas, permitindo que o operador visualize, filtre e continue cadastros interrompidos diretamente pela interface.

O escopo é **frontend-only**: nenhuma rota, payload ou comportamento de API é alterado. O wizard de 6 etapas continua sendo o fluxo principal; a listagem é uma porta de entrada e retomada.

---

## Implementation Decisions

### Roteamento
- Usar `react-router-dom` v6 para criar as rotas `/` (wizard) e `/leads` (listagem).
- O `nginx.conf` já entrega `index.html` para qualquer caminho (`try_files`), então rotas profundas funcionarão no container.

### Layout
- Adicionar um cabeçalho global simples com links para navegação entre wizard e listagem.
- Manter o `LeadProvider` por fora do router para que o estado do lead sobreviva à navegação.

### Listagem
- Cards empilhados em mobile, tabela simples em desktop.
- Filtros: `status` (Select), `currentStep` (Select), `cpf` (Input com máscara).
- Paginação: botões "Anterior"/"Próxima" + info de página, usando `page`/`totalPages` da API.

### Formatação
- CPF mascarado com `maskCpf`.
- Datas no padrão brasileiro com `formatDateToBrazilian`.
- Status e etapas traduzidos por mapas frontend (`leadLabels.ts`).

### Retomada
- Clicar em "Continuar" busca o lead completo (`GET /leads/{id}`), atualiza o contexto e usa `resolveStepId` para posicionar o wizard.
- Navegação para `/` após carregar o lead.

### Reutilização
- O componente de card/linha da listagem será reaproveitado no `CpfReuseModal` quando houver múltiplos leads ativos para o mesmo CPF.

---

## Specific References

- Backend: `backend/src/ConsignadoLeads.Api/Features/Leads/LeadsEndpoints.cs` expõe `GET /leads` e `GET /leads/{id}`.
- Backend handler: `backend/src/ConsignadoLeads.Api/Features/Leads/LeadsHandler.cs` implementa filtros e paginação.
- Frontend context: `frontend/src/shared/leadContext.tsx` já possui `setLead`, `setStep` e `resolveStepId`.
- Existing API usage: `frontend/src/features/consultation/consultationApi.ts` já consome `GET /leads?cpf=...&status=...`.

---

## Deferred Ideas

- Filtros na URL (query params do browser) — P2; útil para compartilhar/buscar, mas não essencial.
- Ordenação por colunas — P2; a API ordena por `createdAt` desc.
- Ações em lote — fora do contrato.
- Autenticação na listagem — fora de escopo do desafio.
