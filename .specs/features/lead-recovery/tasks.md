# Lead Recovery Tasks

## Execution Protocol (MANDATORY -- do not skip)

Implement these tasks with the `tlc-spec-driven` skill: **activate it by name and follow its Execute flow and Critical Rules.** Do not search for skill files by filesystem path. The skill is the source of truth for the full flow (per-task cycle, sub-agent delegation, adequacy review, Verifier, discrimination sensor).

**If the skill cannot be activated, STOP and tell the user - do not proceed without it.**

**Tool selection (confirmed with user):** every code task applies the `coding-guidelines` skill; Frontend-phase tasks (T14-T21) additionally apply `react-best-practices` and `react-composition-patterns`; T24 applies `playwright-skill` for an automated click-through instead of a fully manual one. No project MCP is relevant to this C#/.NET/MongoDB/React stack.

**Pre-Verifier polish (confirmed with user):** after T24 completes and before the automatic Verifier sub-agent runs, invoke `/simplify` on the full accumulated diff to remove duplication/accidental complexity introduced across the 24 tasks. This is a review-and-apply pass, not a new task — no separate commit budget beyond what `/simplify` itself produces.

---

**Design**: `.specs/features/lead-recovery/design.md`
**Status**: Approved

---

## Test Coverage Matrix

> Generated from codebase (no existing tests — skeleton project only), `README.md` seção 8 (minimum test list), and user decision: backend xUnit unit+integration (Testcontainers.MongoDb), no automated frontend tests.

| Code Layer | Required Test Type | Coverage Expectation | Location Pattern | Run Command |
| --- | --- | --- | --- | --- |
| Domain / business logic (`SimulationCalculator`, `PendingRequirementsValidator`, `MockOutcomeResolver`, `SensitiveDataMasker`) | unit | All branches; 1:1 to spec ACs; every listed edge case has a test | `backend/src/ConsignadoLeads.Api.Tests/Unit/**/*Tests.cs` | `dotnet test backend/ConsignadoLeads.slnx --filter Category=Unit` |
| Endpoint/handler slices (Consultation, Leads, Simulation, Identification, ProfessionalBankingData, Documents, Confirmation) | integration | All routes in scope: happy path + every listed edge case + error paths; Confirmation includes the two-simultaneous-confirms concurrency test | `backend/src/ConsignadoLeads.Api.Tests/Integration/**/*Tests.cs` | `dotnet test backend/ConsignadoLeads.slnx --filter Category=Integration` |
| Core/config (`MongoContext`, exception types, Problem Details wiring, Swagger setup) | none | Exercised transitively by slice integration tests | — | build gate only |
| Frontend (`frontend/src/**`) | none | Explicit user decision — no automated frontend test suite for this challenge; TypeScript compile is the only gate | — | `cd frontend && npm run build` |

## Gate Check Commands

> Generated from codebase (`backend/src/ConsignadoLeads.Api/ConsignadoLeads.Api.csproj`, `frontend/package.json`) — confirm before Execute.

| Gate Level | When to Use | Command |
| --- | --- | --- |
| Quick | After tasks with unit tests only | `dotnet test backend/ConsignadoLeads.slnx --filter Category=Unit` |
| Full | After tasks with integration tests | `dotnet test backend/ConsignadoLeads.slnx` (runs unit + integration; requires Docker for Testcontainers) |
| Build (backend) | After config/entity-only backend tasks or phase completion | `dotnet build backend/ConsignadoLeads.slnx && dotnet format backend/ConsignadoLeads.slnx --verify-no-changes && dotnet test backend/ConsignadoLeads.slnx` |
| Build (frontend) | After any frontend task | `cd frontend && npm run build` |
| Docs | After documentation-only tasks | manual review — no command |
| Stack | Final end-to-end check | `docker compose up --build` (verify `/health` 200, frontend loads on `:3000`, then stop) |

---

## Execution Plan

Phases are ordered and run sequentially - each phase completes before the next begins, and tasks within a phase execute in order.

### Phase 1: Test & Core Foundation

```
T2 → T3
T1 → T5
T1 → T6
```

(T1 and T4 have no same-phase dependencies; all of T1-T6 run in listed order.)

### Phase 2: Backend Vertical Slices

```
T7 → T8
T7 → T9
T7 → T10
T7 → T11
T7 → T12
T9 → T13
T10 → T13
T11 → T13
T12 → T13
```

### Phase 3: Frontend

```
T14 → T15
T15 → T16
T15 → T17
T15 → T18
T15 → T19
T16 → T20
T17 → T20
T18 → T20
T19 → T20
T20 → T21
```

### Phase 4: Delivery Polish

```
T22 → T23
T23 → T24
```

---

## Task Breakdown

#### Phase 1: Test & Core Foundation

### T1: Scaffold xUnit test project

**What**: Create `ConsignadoLeads.Api.Tests` (xUnit) referencing `Testcontainers.MongoDb` and `Microsoft.AspNetCore.Mvc.Testing`; add it to `ConsignadoLeads.slnx`.
**Where**: `backend/src/ConsignadoLeads.Api.Tests/`
**Depends on**: None
**Reuses**: N/A — skeleton has no test project yet
**Requirement**: infra (enables all backend test tasks)

**Tools**:
- MCP: NONE
- Skill: `coding-guidelines`

**Done when**:
- [x] Project created with `xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`, `Testcontainers.MongoDb`, `Microsoft.AspNetCore.Mvc.Testing`
- [x] Referenced from `ConsignadoLeads.slnx`
- [x] `dotnet test backend/ConsignadoLeads.slnx` runs (0 tests, exit 0)

**Tests**: none
**Gate**: Build (backend)
**Commit**: `chore(tests): scaffold xUnit test project with Testcontainers.MongoDb`

---

### T2: Core Mongo models

**What**: Implement `Lead` (+ nested `Progress`, `Consultation`, `Simulation`, `IdentificationData`, `ProfessionalData`, `BankingData`, `Confirmation`, `ConfirmationAttempt`, `FinalRegistration`) and `LeadDocumentEntity`, per `design.md` Data Models, with `[BsonIgnoreIfNull]` on fields not yet reached.
**Where**: `backend/src/ConsignadoLeads.Api/Core/Models/`
**Depends on**: None
**Reuses**: `MongoDB.Bson` attributes from the already-referenced `MongoDB.Driver` package
**Requirement**: LEAD-60, design.md Data Models

**Tools**:
- MCP: NONE
- Skill: `coding-guidelines`

**Done when**:
- [x] All fields/nested types from `design.md` Data Models present
- [x] Enum-like fields modeled as `string` (mapped to contract enum values)
- [x] Fields for not-yet-reached steps marked `[BsonIgnoreIfNull]`
- [x] Solution builds

**Tests**: none
**Gate**: Build (backend)
**Commit**: `feat(core): add Lead and LeadDocumentEntity Mongo models`

---

### T3: MongoContext

**What**: Implement `MongoContext` wrapping `IMongoDatabase`, exposing `Leads` (`IMongoCollection<Lead>`), `LeadDocuments` (`IMongoCollection<LeadDocumentEntity>`), `Files` (`GridFSBucket`); register a `ConventionPack` with `CamelCaseElementNameConvention`; wire DI in `Program.cs` reading `ConnectionStrings__MongoDb`; create the indexes from `design.md`/spec (status+currentStep, cpf, createdAt on `leads`; leadId+status on `lead_documents`).
**Where**: `backend/src/ConsignadoLeads.Api/Core/MongoContext.cs`
**Depends on**: T2
**Reuses**: `MongoDB.Driver`; existing `ConnectionStrings__MongoDb` wiring in `docker-compose.yml`
**Requirement**: README seção 6 (índices), AD-002

**Tools**:
- MCP: NONE
- Skill: `coding-guidelines`

**Done when**:
- [x] `MongoContext` registered as singleton in `Program.cs`
- [x] Indexes created on startup (idempotent — safe to run every boot)
- [x] API still starts; `docker compose up --build` → `GET /health` returns 200

**Tests**: none
**Gate**: Build (backend)
**Commit**: `feat(core): add MongoContext with typed collections, GridFS bucket and indexes`

---

### T4: Exceptions + Problem Details handler

**What**: Implement the 7 exception types (`LeadNotFoundException`, `DocumentNotFoundException`, `VersionConflictException`, `ConfirmationInProgressException`, `LeadAlreadyCompletedException`, `PendingRequirementsException`, `MainSystemUnavailableException`, `MainSystemTimeoutException`) and `ProblemDetailsExceptionHandler : IExceptionHandler` mapping each to its status + RFC 9457 shape; register via `AddProblemDetails()` + `AddExceptionHandler<T>()`.
**Where**: `backend/src/ConsignadoLeads.Api/Core/Exceptions/`
**Depends on**: None
**Reuses**: ASP.NET Core built-in `IExceptionHandler`/`AddProblemDetails()` — no new package
**Requirement**: LEAD-55, LEAD-56

**Tools**:
- MCP: NONE
- Skill: `coding-guidelines`

**Done when**:
- [x] Every exception type from `design.md` Error Handling Strategy exists and maps to its documented status code
- [x] Response body includes `type`, `title`, `status`, `detail` (+ `extensions.currentVersion` for `VersionConflictException`)
- [x] No behavior test yet (nothing throws these until Phase 2) — mapping verified transitively by Phase 2 integration tests

**Tests**: none
**Gate**: Build (backend)
**Commit**: `feat(core): add typed exceptions and Problem Details exception handler`

---

### T5: MockOutcomeResolver

**What**: Implement `MockOutcomeResolver.Resolve<TEnum>(string? mockOutcome, TEnum deterministicDefault)` reading `ENABLE_TEST_ENDPOINTS` from configuration, per `design.md`.
**Where**: `backend/src/ConsignadoLeads.Api/Core/MockOutcomeResolver.cs`
**Depends on**: T1
**Reuses**: none
**Requirement**: LEAD-04, LEAD-21, LEAD-44

**Tools**:
- MCP: NONE
- Skill: `coding-guidelines`

**Done when**:
- [x] Flag off (or unset) → always returns `deterministicDefault`, ignoring `mockOutcome`
- [x] Flag on + valid `mockOutcome` string → returns parsed enum value
- [x] Flag on + missing/invalid `mockOutcome` → returns `deterministicDefault`
- [x] Unit tests cover all 3 branches (3+ test cases)

**Tests**: unit
**Gate**: Quick
**Commit**: `feat(core): add MockOutcomeResolver with unit tests`

---

### T6: Structured logging + sensitive-data masking

**What**: Add a request-logging middleware (method, path, status, duration) and a `SensitiveDataMasker` utility that redacts CPF/banking fields from any logged object; wire the middleware in `Program.cs`.
**Where**: `backend/src/ConsignadoLeads.Api/Core/Logging/`
**Depends on**: T1
**Reuses**: `Microsoft.Extensions.Logging` (built-in, no new package)
**Requirement**: LEAD-63

**Tools**:
- MCP: NONE
- Skill: `coding-guidelines`

**Done when**:
- [x] `SensitiveDataMasker` redacts `cpf`, `documentNumber`, `bankingData.*` fields (present, absent, nested cases)
- [x] Request-logging middleware never logs raw request/response bodies, only method/path/status/duration
- [x] Unit tests cover masker branches (present/absent/nested, 3+ test cases)

**Tests**: unit
**Gate**: Quick
**Commit**: `feat(core): add request logging middleware and sensitive-data masker`

---

#### Phase 2: Backend Vertical Slices

### T7: Consultation slice

**What**: Implement `POST /leads/consultation` and `PUT /leads/{id}/steps/consultation`, DTOs, `EligibilityMock`, handler.
**Where**: `backend/src/ConsignadoLeads.Api/Features/Consultation/`
**Depends on**: T2, T3, T4, T5, T6
**Reuses**: `MongoContext`, `MockOutcomeResolver`, exception types from Core
**Requirement**: LEAD-01–07

**Tools**:
- MCP: NONE
- Skill: `coding-guidelines`

**Done when**:
- [x] `POST /leads/consultation` creates lead (201, `status=in_progress`, `version=1`) even on `unavailable` outcome (LEAD-03)
- [x] 400 when `consultationAuthorized=false` or required field missing, no lead created
- [x] `PUT .../steps/consultation` re-runs without creating a new lead (200); 404 if lead missing; 409 + `extensions.currentVersion` on `expectedVersion` mismatch
- [x] Deterministic default (`eligible`) when `ENABLE_TEST_ENDPOINTS != true`

**Tests**: integration
**Gate**: Full
**Commit**: `feat(consultation): implement POST /leads/consultation and PUT steps/consultation`

---

### T8: Leads listing/retrieval slice

**What**: Implement `GET /leads` (paginated, filters `status`/`currentStep`/`page`/`pageSize`/`cpf`) and `GET /leads/{id}` (joins active `lead_documents`).
**Where**: `backend/src/ConsignadoLeads.Api/Features/Leads/`
**Depends on**: T7
**Reuses**: `MongoContext`
**Requirement**: LEAD-49–54

**Tools**:
- MCP: NONE
- Skill: `coding-guidelines`

**Done when**:
- [x] `pageSize` default 20, clamped at 100; `page` 1-based default 1; sort `createdAt` desc
- [x] `cpf` additive filter works (used by frontend P2-1 modal)
- [x] `GET /leads/{id}` 404 when missing; includes active documents when present

**Tests**: integration
**Gate**: Full
**Commit**: `feat(leads): implement GET /leads and GET /leads/{id}`

---

### T9: Simulation slice

**What**: Implement `SimulationCalculator` (pure Price-formula function) and `POST /leads/{id}/steps/simulation` + `PATCH /leads/{id}/steps/simulation/{simulationId}/select`.
**Where**: `backend/src/ConsignadoLeads.Api/Features/Simulation/`
**Depends on**: T7
**Reuses**: `MongoContext`
**Requirement**: LEAD-08–15

**Tools**:
- MCP: NONE
- Skill: `coding-guidelines`

**Done when**:
- [x] `SimulationCalculator` unit test matches README exact example (`requestedAmount=10000, installments=24` → `516.81`/`12403.44`)
- [x] New simulation always `selected=true`, previous ones flip to `false`, history preserved
- [x] 404 lead missing; 409 if consultation step not completed; select endpoint 404 if lead/simulation missing, never recalculates

**Tests**: unit, integration
**Gate**: Full
**Commit**: `feat(simulation): implement Price calculator and simulation endpoints`

---

### T10: Identification slice

**What**: Implement `PUT /leads/{id}/steps/identification` + `IdentificationMock`.
**Where**: `backend/src/ConsignadoLeads.Api/Features/Identification/`
**Depends on**: T7
**Reuses**: `MongoContext`, `MockOutcomeResolver`
**Requirement**: LEAD-16–21

**Tools**:
- MCP: NONE
- Skill: `coding-guidelines`

**Done when**:
- [x] Persists all identification fields + query result regardless of outcome
- [x] `not_found`/`diverging`/`unavailable` preserve already-typed data (no data loss)
- [x] 404 missing lead; 409 `expectedVersion` mismatch; deterministic default (`found`) when flag off

**Tests**: integration
**Gate**: Full
**Commit**: `feat(identification): implement PUT steps/identification`

---

### T11: ProfessionalBankingData slice

**What**: Implement `PUT /leads/{id}/steps/professional-banking-data`.
**Where**: `backend/src/ConsignadoLeads.Api/Features/ProfessionalBankingData/`
**Depends on**: T7
**Reuses**: `MongoContext`
**Requirement**: LEAD-22–25

**Tools**:
- MCP: NONE
- Skill: `coding-guidelines`

**Done when**:
- [x] Persists both `professionalData` and `bankingData` without erasing other step data (assert `consultation`/`simulations`/`identification` intact after this update)
- [x] `pixKey` optional (absent or empty accepted)
- [x] 404 missing lead; 409 `expectedVersion` mismatch

**Tests**: integration
**Gate**: Full
**Commit**: `feat(professional-banking-data): implement PUT steps/professional-banking-data`

---

### T12: Documents slice

**What**: Implement `POST/GET /leads/{id}/documents`, `DELETE /leads/{id}/documents/{documentId}` using GridFS (write order: upload bytes first, then insert/replace metadata, per `design.md` Risks & Concerns).
**Where**: `backend/src/ConsignadoLeads.Api/Features/Documents/`
**Depends on**: T7
**Reuses**: `MongoContext` (`LeadDocuments` collection + `Files` GridFS bucket)
**Requirement**: LEAD-26–34

**Tools**:
- MCP: NONE
- Skill: `coding-guidelines`

**Done when**:
- [x] Upload rejects >10MB or non-`image/jpeg`/`image/png`/`application/pdf` with 400, before touching Mongo/GridFS
- [x] 404 on missing lead (upload never creates a lead)
- [x] Re-upload of same `type` marks previous version `replaced`
- [x] `personalDocumentSubtype` mismatch vs. `identification.documentType` is recorded as a blocking pendency (surfaced at etapa 6, not just silently accepted)
- [x] `GET` excludes `deleted`/`replaced`; `DELETE` soft-deletes (204), 404 if missing
- [x] Separate calls with abandonment between them both persist correctly

**Tests**: integration
**Gate**: Full
**Commit**: `feat(documents): implement upload, list and soft-delete via GridFS`

---

### T13: Confirmation slice

**What**: Implement `PendingRequirementsValidator` (pure function), `MainSystemMock`, `POST /leads/{id}/confirm`, `POST /leads/{id}/retry-submission`, and the status-based confirmation mutex.
**Where**: `backend/src/ConsignadoLeads.Api/Features/Confirmation/`
**Depends on**: T9, T10, T11, T12
**Reuses**: `MongoContext`, `MockOutcomeResolver`
**Requirement**: LEAD-35–48

**Tools**:
- MCP: NONE
- Skill: `coding-guidelines`

**Done when**:
- [x] `PendingRequirementsValidator` unit tests cover every pendency combination (missing simulation, missing identification, incompatible document, missing required documents)
- [x] 422 with exact pendency reasons when validation fails, no mock call made
- [x] All 6 mock outcomes mapped to the documented status code (`success`→200/completed, `rejected`/`validationError`→422/failed_retryable, `unavailable`→503/failed_retryable, `timeout`→504/failed_retryable, `indeterminate`→202/pending_verification)
- [x] `retry-submission`: idempotent 200 when `finalRegistration.registrationId` already set (no second mock call — assert via call-count spy); real retry (calls mock again) when `failed_retryable`/`pending_verification` without a saved `registrationId`; 409 if never confirmed
- [x] **Concurrency test**: two simultaneous `confirm` calls on the same lead → exactly one succeeds, the other gets 409
- [x] Deterministic default (`success`) when flag off

**Tests**: unit, integration
**Gate**: Full
**Commit**: `feat(confirmation): implement confirm, retry-submission and confirmation mutex`

---

#### Phase 3: Frontend

### T14: Frontend shared foundation

**What**: Implement `httpClient` (fetch wrapper, base URL, JSON + Problem Details error parsing), `leadContext` (current lead id + `progress`), `Stepper` and `LoadingError` shared components.
**Where**: `frontend/src/shared/`
**Depends on**: None
**Reuses**: React 18 + Vite scaffold already in place
**Requirement**: README seção 7

**Tools**:
- MCP: NONE
- Skill: `coding-guidelines`, `react-best-practices`, `react-composition-patterns`

**Done when**:
- [x] `httpClient` targets `http://localhost:8080`, parses Problem Details on non-2xx
- [x] `leadContext` exposes current lead id + progress + setters, consumable by every step
- [x] `npm run build` passes

**Tests**: none
**Gate**: Build (frontend)
**Commit**: `feat(frontend): add shared http client, lead context and stepper shell`

---

### T15: Consultation step + CPF-reuse modal

**What**: Implement the consultation step page/form and the CPF-dedup modal (P2-1: query `GET /leads?cpf=...`, offer continue/start-new).
**Where**: `frontend/src/features/consultation/`
**Depends on**: T14
**Reuses**: `shared/api/httpClient`, `shared/leadContext`
**Requirement**: LEAD-01, 02, 03, 05, 06, 07, 66–69

**Tools**:
- MCP: NONE
- Skill: `coding-guidelines`, `react-best-practices`, `react-composition-patterns`

**Done when**:
- [x] Form submits `POST /leads/consultation`, stores returned lead id in context
- [x] Before submit, queries `GET /leads?cpf=...&status=...`; shows modal with Continue/Start New when an active lead is found
- [x] Continue → `GET /leads/{id}`, navigates to `progress.currentStep`
- [x] Loading/error states handled

**Tests**: none
**Gate**: Build (frontend)
**Commit**: `feat(frontend): implement consultation step with CPF-reuse modal`

---

### T16: Simulation step

**What**: Implement the simulation step (request simulation, show history, select one).
**Where**: `frontend/src/features/simulation/`
**Depends on**: T15
**Reuses**: `shared/api/httpClient`, `shared/leadContext`
**Requirement**: LEAD-08–15

**Tools**:
- MCP: NONE
- Skill: `coding-guidelines`, `react-best-practices`, `react-composition-patterns`

**Done when**:
- [ ] Calls `POST /leads/{id}/steps/simulation`, displays `installmentAmount`/`totalAmount`
- [ ] Lists simulation history, lets the user `PATCH .../select` a different one
- [ ] Loading/error states handled

**Tests**: none
**Gate**: Build (frontend)
**Commit**: `feat(frontend): implement simulation step`

---

### T17: Identification step

**What**: Implement the identification form step.
**Where**: `frontend/src/features/identification/`
**Depends on**: T15
**Reuses**: `shared/api/httpClient`, `shared/leadContext`
**Requirement**: LEAD-16–21

**Tools**:
- MCP: NONE
- Skill: `coding-guidelines`, `react-best-practices`, `react-composition-patterns`

**Done when**:
- [ ] Form submits `PUT .../steps/identification` with all required fields
- [ ] Shows the query result (`found`/`not_found`/`diverging`/`unavailable`) and lets the user continue/correct
- [ ] Loading/error states handled

**Tests**: none
**Gate**: Build (frontend)
**Commit**: `feat(frontend): implement identification step`

---

### T18: Professional/banking data step

**What**: Implement the professional + banking data form step.
**Where**: `frontend/src/features/professionalBankingData/`
**Depends on**: T15
**Reuses**: `shared/api/httpClient`, `shared/leadContext`
**Requirement**: LEAD-22–25

**Tools**:
- MCP: NONE
- Skill: `coding-guidelines`, `react-best-practices`, `react-composition-patterns`

**Done when**:
- [ ] Form submits `PUT .../steps/professional-banking-data`, `pixKey` optional
- [ ] Loading/error states handled

**Tests**: none
**Gate**: Build (frontend)
**Commit**: `feat(frontend): implement professional and banking data step`

---

### T19: Documents step

**What**: Implement the document upload step (personal document + payslip, separate uploads).
**Where**: `frontend/src/features/documents/`
**Depends on**: T15
**Reuses**: `shared/api/httpClient`, `shared/leadContext`
**Requirement**: LEAD-26–34

**Tools**:
- MCP: NONE
- Skill: `coding-guidelines`, `react-best-practices`, `react-composition-patterns`

**Done when**:
- [ ] Uploads via `POST .../documents` (multipart), lists active docs via `GET`, allows delete via `DELETE`
- [ ] Each upload can be done independently (no forced pairing in one request)
- [ ] Loading/error states handled

**Tests**: none
**Gate**: Build (frontend)
**Commit**: `feat(frontend): implement documents upload step`

---

### T20: Confirmation step

**What**: Implement the summary + confirmation step (review all data, confirm, retry on failure).
**Where**: `frontend/src/features/confirmation/`
**Depends on**: T16, T17, T18, T19
**Reuses**: `shared/api/httpClient`, `shared/leadContext`
**Requirement**: LEAD-35–48

**Tools**:
- MCP: NONE
- Skill: `coding-guidelines`, `react-best-practices`, `react-composition-patterns`

**Done when**:
- [ ] Shows full summary (consultation, selected simulation, identification, professional/banking data, documents)
- [ ] Calls `POST .../confirm`; on 422 shows the exact pendency list; on 503/504 offers `POST .../retry-submission`
- [ ] Shows `finalRegistration.registrationId` on success

**Tests**: none
**Gate**: Build (frontend)
**Commit**: `feat(frontend): implement confirmation and summary step`

---

### T21: Wire App shell

**What**: Replace the placeholder `App.tsx` with the stepper wiring across all 6 features.
**Where**: `frontend/src/App.tsx`
**Depends on**: T20
**Reuses**: `shared/components/Stepper`, all `features/*` pages
**Requirement**: README seção 7

**Tools**:
- MCP: NONE
- Skill: `coding-guidelines`, `react-best-practices`, `react-composition-patterns`

**Done when**:
- [ ] `App.tsx` renders the stepper + current step's page based on `leadContext`
- [ ] Manual click-through of all 6 steps works against a running backend
- [ ] `npm run build` passes

**Tests**: none
**Gate**: Build (frontend)
**Commit**: `feat(frontend): wire stepper and routing across all 6 steps in App`

---

#### Phase 4: Delivery Polish

### T22: Swagger/OpenAPI

**What**: Expose `/swagger` with request/response schemas for all routes in development.
**Where**: `backend/src/ConsignadoLeads.Api/`
**Depends on**: None (cross-phase dependency on T13 only — all routes must exist)
**Reuses**: ASP.NET Core's built-in OpenAPI support (or `Swashbuckle.AspNetCore` if richer UI is wanted)
**Requirement**: LEAD-70

**Tools**:
- MCP: NONE
- Skill: `coding-guidelines`

**Done when**:
- [ ] `/swagger` reachable in development, lists all 12 contract routes with request/response schemas

**Tests**: none
**Gate**: Build (backend)
**Commit**: `feat(api): expose OpenAPI/Swagger UI in development`

---

### T23: Final README documentation

**What**: Update root `README.md` (or a linked doc) with the NoSQL modeling write-up (embedded vs. referenced, indexes, concurrency, schema versioning, ausente/null/vazio convention — README seção 6), execution instructions, and known trade-offs/limitations.
**Where**: `README.md`
**Depends on**: T22
**Reuses**: `design.md`, `.specs/STATE.md` Decisions (AD-001..004) as source material
**Requirement**: README seção 6, 10

**Tools**:
- MCP: NONE
- Skill: NONE

**Done when**:
- [ ] Modeling rationale documented (embedded vs. referenced, indexes + why, concurrency strategy, schema versioning, field convention)
- [ ] Known limitations documented (no auth, no at-rest encryption, no automatic `abandoned` detection)
- [ ] Execution instructions confirmed accurate against the actual `docker compose up --build` flow

**Tests**: none
**Gate**: Docs
**Commit**: `docs: document NoSQL modeling decisions, trade-offs and execution instructions`

---

### T24: Full-stack verification

**What**: Run the complete acceptance pass — clean `docker compose up --build`, confirm `/health` + frontend + CI parity, walk the 6-step flow manually end to end.
**Where**: repository root
**Depends on**: T23
**Reuses**: `docker-compose.yml`, `.github/workflows/ci.yml`
**Requirement**: spec.md Success Criteria

**Tools**:
- MCP: NONE
- Skill: `playwright-skill` (automated click-through of the 6 steps against the running stack)

**Done when**:
- [ ] `docker compose up --build` starts cleanly, no manual steps
- [ ] `GET /health` → 200; frontend loads on `:3000`
- [ ] Manual click-through of all 6 steps completes and reaches `completed`
- [ ] Any gap found here becomes a follow-up task with its own commit — this task itself makes no code changes unless fixing something trivial

**Tests**: none
**Gate**: Stack
**Commit**: none expected (or a small `fix:` commit if a gap is found and fixed inline)

---

## Phase Execution Map

Phases run in sequence (1, then 2, then 3, then 4); tasks within a phase execute in listed order:

- **Phase 1** (Test & Core Foundation): T1, T2, T3, T4, T5, T6
- **Phase 2** (Backend Vertical Slices): T7, T8, T9, T10, T11, T12, T13
- **Phase 3** (Frontend): T14, T15, T16, T17, T18, T19, T20, T21
- **Phase 4** (Delivery Polish): T22, T23, T24

Execution is strictly sequential - there is no intra-phase parallelism. A single agent (or batch worker) works one task at a time, in order. (See the `Depends on` field on each task and the per-phase diagrams under Execution Plan above for the exact dependency edges.)

**Packing**: 24 tasks total → offer batch sub-agents (see Sub-Agent Delegation question below). Natural packing at the ~7-task budget, phase boundaries only: **Batch 1** = Phase 1 (6 tasks), **Batch 2** = Phase 2 (7 tasks), **Batch 3** = Phase 3 (8 tasks), **Batch 4** = Phase 4 (3 tasks).

---

## Task Granularity Check

Each vertical-slice task (T7-T13, T15-T20) spans several files (endpoint, DTOs, handler, in-slice mock, tests) by design — per `AGENTS.md`'s vertical-slice mandate and the tasks-phase's own "Resolving compilation dependencies" guidance, a slice's endpoint/DTOs/handler cannot be tested independently of each other, so splitting them would produce untestable intermediate commits. Each is still ONE deliverable: one contract operation (or one cohesive frontend step), fully working and tested end-to-end.

| Task | Scope | Status |
| --- | --- | --- |
| T1: Scaffold test project | 1 project | ✅ Granular |
| T2: Core Mongo models | 1 cohesive model set (2 files) | ✅ Granular |
| T3: MongoContext | 1 component | ✅ Granular |
| T4: Exceptions + handler | 1 cohesive error-mapping unit | ✅ Granular |
| T5: MockOutcomeResolver | 1 function | ✅ Granular |
| T6: Logging + masker | 1 cohesive cross-cutting unit | ✅ Granular |
| T7: Consultation slice | 1 vertical slice (2 endpoints) | ✅ Granular |
| T8: Leads slice | 1 vertical slice (2 endpoints) | ✅ Granular |
| T9: Simulation slice | 1 vertical slice (2 endpoints + calculator) | ✅ Granular |
| T10: Identification slice | 1 vertical slice (1 endpoint) | ✅ Granular |
| T11: ProfessionalBankingData slice | 1 vertical slice (1 endpoint) | ✅ Granular |
| T12: Documents slice | 1 vertical slice (3 endpoints) | ✅ Granular |
| T13: Confirmation slice | 1 vertical slice (2 endpoints + validator + mutex) | ✅ Granular |
| T14: Frontend shared foundation | 1 cohesive shared layer | ✅ Granular |
| T15: Consultation step | 1 step + 1 modal | ✅ Granular |
| T16: Simulation step | 1 step | ✅ Granular |
| T17: Identification step | 1 step | ✅ Granular |
| T18: ProfessionalBankingData step | 1 step | ✅ Granular |
| T19: Documents step | 1 step | ✅ Granular |
| T20: Confirmation step | 1 step | ✅ Granular |
| T21: Wire App shell | 1 file | ✅ Granular |
| T22: Swagger | 1 cross-cutting addition | ✅ Granular |
| T23: Final README | 1 file | ✅ Granular |
| T24: Full-stack verification | 1 verification pass | ✅ Granular |

---

## Diagram-Definition Cross-Check

| Task | Depends On (task body) | Diagram Shows | Status |
| --- | --- | --- | --- |
| T2 | None | — | ✅ Match |
| T3 | T2 | T2 → T3 | ✅ Match |
| T5 | T1 | T1 → T5 | ✅ Match |
| T6 | T1 | T1 → T6 | ✅ Match |
| T8 | T7 | T7 → T8 | ✅ Match |
| T9 | T7 | T7 → T9 | ✅ Match |
| T10 | T7 | T7 → T10 | ✅ Match |
| T11 | T7 | T7 → T11 | ✅ Match |
| T12 | T7 | T7 → T12 | ✅ Match |
| T13 | T9, T10, T11, T12 | T9→T13, T10→T13, T11→T13, T12→T13 | ✅ Match |
| T15 | T14 | T14 → T15 | ✅ Match |
| T16 | T15 | T15→T16 | ✅ Match |
| T17 | T15 | T15→T17 | ✅ Match |
| T18 | T15 | T15→T18 | ✅ Match |
| T19 | T15 | T15→T19 | ✅ Match |
| T20 | T16, T17, T18, T19 | T16→T20, T17→T20, T18→T20, T19→T20 | ✅ Match |
| T21 | T20 | T20 → T21 | ✅ Match |
| T23 | T22 | T22 → T23 | ✅ Match |
| T24 | T23 | T23 → T24 | ✅ Match |

Cross-phase dependencies (T7 on Phase 1 tasks, T13 also cross-checked above via same-phase edges, T22 on T13) are validated by the forward-phase-only rule, not by intra-phase diagram parity.

---

## Test Co-location Validation

| Task | Code Layer Created/Modified | Matrix Requires | Task Says | Status |
| --- | --- | --- | --- | --- |
| T1 | infra (test project) | — | none | ✅ OK |
| T2 | Entity (Mongo models) | none | none | ✅ OK |
| T3 | Core/config | none | none | ✅ OK |
| T4 | Core/config | none | none | ✅ OK |
| T5 | Domain logic | unit | unit | ✅ OK |
| T6 | Domain logic (masker) + config (middleware) | unit (highest) | unit | ✅ OK |
| T7 | Endpoint/handler slice | integration | integration | ✅ OK |
| T8 | Endpoint/handler slice | integration | integration | ✅ OK |
| T9 | Domain logic (calculator) + endpoint slice | unit + integration | unit, integration | ✅ OK |
| T10 | Endpoint/handler slice | integration | integration | ✅ OK |
| T11 | Endpoint/handler slice | integration | integration | ✅ OK |
| T12 | Endpoint/handler slice | integration | integration | ✅ OK |
| T13 | Domain logic (validator) + endpoint slice | unit + integration | unit, integration | ✅ OK |
| T14-T21 | Frontend | none | none | ✅ OK |
| T22 | Core/config | none | none | ✅ OK |
| T23 | Docs | — | none | ✅ OK |
| T24 | Verification (no code layer) | — | none | ✅ OK |

No violations.
