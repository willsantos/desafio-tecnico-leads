# Lead Recovery Context

**Gathered:** 2026-08-05
**Spec:** `.specs/features/lead-recovery/spec.md`
**Status:** Ready for design

---

## Feature Boundary

Fluxo completo de recuperação de leads (6 etapas) do cadastro de empréstimo consignado — API REST conforme contrato fixo da seção 5 do README, persistência progressiva em MongoDB, três integrações mockadas (elegibilidade, identificação, sistema principal) determinísticas via `mockOutcome`, frontend React navegável pelas 6 etapas com retomada por id. Cadastro definitivo só ocorre após confirmação da etapa 6.

---

## Implementation Decisions

### Storage de arquivos (etapa 5)
- GridFS do próprio MongoDB (`MongoDB.Driver.GridFS`). `lead_documents` guarda metadado + `gridFsFileId`. Sem serviço de storage extra, sem volume novo no compose.

### Mock default (ENABLE_TEST_ENDPOINTS=false ou mockOutcome ausente)
- Determinístico: elegibilidade → `eligible`; identificação → `found`; sistema principal → `success`. Fluxo manual/demo sempre previsível de ponta a ponta.

### Duplicidade de CPF
- Backend permanece permissivo — `POST /leads/consultation` sempre cria lead novo (contrato intacto).
- Frontend, antes de submeter etapa 1, consulta `GET /leads?cpf=X&status=draft,in_progress,pending_confirmation,confirming,failed_retryable,pending_verification` (filtro `cpf` é extensão aditiva ao contrato, não renomeia/remove nada existente).
- Achou lead ativo → modal: "Continuar de onde parou" (busca `GET /leads/{id}`, navega pro `progress.currentStep`) ou "Começar novo" (segue fluxo normal de criação; lead antigo fica preservado no banco, nunca apagado/marcado).

### Paginação `GET /leads`
- `pageSize` default 20, máximo 100 (valores acima do máximo são clampados, não rejeitados). `page` 1-based, default 1. Ordenação fixa `createdAt` desc.

### Reconciliação `pending_verification`
- Mesmo mutex de `confirm`/`retry-submission`. `pending_verification` entra no conjunto de status que permite nova tentativa. `retry-submission` chama o sistema principal mockado de novo (não é idempotência por cache, é retentativa real, pois não há `registrationId` gravado ainda nesse estado). Sucesso grava `finalRegistration` e completa; qualquer novo desfecho não-sucesso repete o tratamento padrão (`failed_retryable`, etc.).

### Agent's Discretion
- Exceções distintas por tipo pra 404/409, exatamente como pedido no README, sem mais input do usuário necessário.
- Índices exatos (`status+currentStep`, `cpf`, `createdAt`) — decisão técnica, não produto.

### Declined / Undiscussed Gray Areas → Assumptions
- Autenticação/autorização: fora de escopo do MVP (README lista como diferencial opcional em 11).
- Criptografia de CPF/dados bancários em repouso: fora de escopo do MVP dado orçamento de 7 dias — documentar como limitação conhecida no README final; em log, mascarar sempre (isso é requisito obrigatório, não diferencial).
- Transição pro status `abandoned`: nenhum mecanismo automático (TTL/Change Streams) no MVP — enum existe no contrato mas nada o define automaticamente; README já lista Change Streams como diferencial opcional em 11.
- Cap de crescimento de `simulations[]`/`confirmation.attempts[]`: sem limite artificial — bounded pela natureza da interação humana (dezenas no máximo), revisitar se a suíte de avaliação gerar volume incomum.

---

## Specific References

Nenhuma referência visual/produto externa — o contrato de API (README seção 5) e as descrições funcionais (seção 4, cenários 1-11 citados inline) são a fonte de verdade do comportamento esperado.

---

## Deferred Ideas

- Endpoint/processo de verificação manual pra `pending_verification` fora do fluxo de retry automático — descartado em favor da opção mais simples (retry re-tenta o mock).
- Enforcement rígido de duplicidade de CPF no backend (bloquear/409) — descartado; modal no frontend resolve sem risco de quebrar a suíte automatizada que não conhece esse comportamento.
