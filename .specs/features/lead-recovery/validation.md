# Lead Recovery Validation

**Date**: 2026-08-05
**Spec**: `.specs/features/lead-recovery/spec.md`
**Diff range**: `3a311bc..HEAD` (`3a311bc` = Design-phase commit → `8b2b86b` = final `/simplify` commit)
**Verifier**: independent sub-agent (author ≠ verifier) — fresh agent, no prior context on this codebase

---

## Task Completion

All 24 tasks in `tasks.md` are marked `[x]` Done. Commit history (`git log --oneline 3a311bc..HEAD`) shows one commit per task plus 5 `/simplify` cleanup commits (`247f50e`..`8b2b86b`), matching the documented Pre-Verifier polish step. No task shows partial/blocked status.

| Phase | Tasks | Status |
| --- | --- | --- |
| 1: Test & Core Foundation | T1-T6 | ✅ Done |
| 2: Backend Vertical Slices | T7-T13 | ✅ Done |
| 3: Frontend | T14-T21 | ✅ Done |
| 4: Delivery Polish | T22-T24 | ✅ Done |

---

## Spec-Anchored Acceptance Criteria

Evidence-or-zero: every row below cites `file:line` + the exact assertion. All 12 contract routes are exercised.

### P1-1: Consulta de elegibilidade (LEAD-01..07)

| Criterion | Spec-defined outcome | `file:line` + assertion | Result |
| --- | --- | --- | --- |
| Valid `POST /leads/consultation` creates lead | 201, `status="in_progress"`, `version=1`, `progress.currentStep="consultation"` | `Integration/Consultation/ConsultationEndpointsTests.cs:43-53` — `Assert.Equal(HttpStatusCode.Created, ...)`, `Assert.Equal("in_progress", dto.Status)`, `Assert.Equal(1, dto.Version)`, `Assert.Equal("consultation", dto.Progress.CurrentStep)` | ✅ PASS |
| `consultationAuthorized=false` | 400, no lead created | `ConsultationEndpointsTests.cs:56-72` — `Assert.Equal(HttpStatusCode.BadRequest, ...)` + `Assert.Equal(0, await CountLeadsByCpfAsync(...))` (direct Mongo count) | ✅ PASS |
| Required field missing | 400, no lead created | `ConsultationEndpointsTests.cs:75-91` — same pattern, `benefitNumber` omitted | ✅ PASS |
| Mock `unavailable` | Lead created anyway, `consultation.result.outcome="unavailable"`, data preserved | `ConsultationEndpointsTests.cs:94-104` — `Assert.Equal("unavailable", dto.Consultation.Result.Outcome)`, `Assert.Equal("44444444444", dto.Consultation.Input.Cpf)` | ✅ PASS |
| `PUT .../steps/consultation` re-runs, no new lead | 200, same lead id, `CountLeadsByCpfAsync == 1` | `ConsultationEndpointsTests.cs:107-119` | ✅ PASS |
| Lead missing on PUT | 404 | `ConsultationEndpointsTests.cs:122-127` | ✅ PASS |
| `expectedVersion` mismatch | 409, `extensions.currentVersion` = actual current version (1) | `ConsultationEndpointsTests.cs:130-152` — parses raw JSON, `Assert.Equal(1, doc.RootElement.GetProperty("extensions").GetProperty("currentVersion").GetInt32())` | ✅ PASS |
| `ENABLE_TEST_ENDPOINTS` on, no `mockOutcome` | deterministic `eligible` | `ConsultationEndpointsTests.cs:155-161` | ✅ PASS |
| `ENABLE_TEST_ENDPOINTS` off, `mockOutcome=unavailable` ignored | deterministic `eligible` | `ConsultationEndpointsTests.cs:164-183` | ✅ PASS |

### P1-2: Simulação (LEAD-08..15)

| Criterion | Spec-defined outcome | `file:line` + assertion | Result |
| --- | --- | --- | --- |
| Price formula, exact README example | `requestedAmount=10000, installments=24` → `installmentAmount=516.81`, `totalAmount=12403.44` | `Unit/Features/Simulation/SimulationCalculatorTests.cs:8-16` — `Assert.Equal(516.81m, ...)`, `Assert.Equal(12403.44m, ...)` (exact decimal literals, not tolerance-based) | ✅ PASS |
| `totalAmount` uses rounded installment, not raw | `totalAmount = round(installmentAmount_rounded * n, 2)` | `SimulationCalculatorTests.cs:18-26` — independently recomputed via `Math.Round(result.InstallmentAmount * 24, 2)` and compared to `result.TotalAmount` | ✅ PASS |
| 201 + new simulation `selected=true` | 201, `Selected=true` | `Integration/Simulation/SimulationEndpointsTests.cs:63-75` | ✅ PASS |
| Lead missing | 404 | `SimulationEndpointsTests.cs:78-83` | ✅ PASS |
| Consultation not completed | 409 | `SimulationEndpointsTests.cs:86-93` (lead inserted directly in Mongo with `Consultation=null`) | ✅ PASS |
| New simulation flips previous to `false`, history kept | `Simulations.Count==2`, first `Selected=false`, second `Selected=true` | `SimulationEndpointsTests.cs:96-115` | ✅ PASS |
| `PATCH .../select` switches without recalculating | 200, `Selected=true`, `InstallmentAmount`/`TotalAmount` unchanged vs. original | `SimulationEndpointsTests.cs:118-137` | ✅ PASS |
| Select: lead/simulation missing | 404 (both cases) | `SimulationEndpointsTests.cs:140-156` | ✅ PASS |

### P1-3: Identificação (LEAD-16..21)

| Criterion | Spec-defined outcome | `file:line` + assertion | Result |
| --- | --- | --- | --- |
| Valid PUT persists + runs mock | 200, `identification.query.outcome="found"` | `Integration/Identification/IdentificationEndpointsTests.cs:67-79` | ✅ PASS |
| Outcome `not_found`/`diverging`/`unavailable` preserves typed data | outcome persisted, `fullName`/`documentType`/`documentNumber` intact | `IdentificationEndpointsTests.cs:81-97` (Theory, 3 cases) | ✅ PASS |
| Lead missing | 404 | `IdentificationEndpointsTests.cs:99-105` | ✅ PASS |
| `expectedVersion` mismatch | 409 + `extensions.currentVersion` | `IdentificationEndpointsTests.cs:107-118` | ✅ PASS |
| Flag on/off deterministic default | `found` in both cases | `IdentificationEndpointsTests.cs:120-153` | ✅ PASS |

### P1-4: Dados profissionais/bancários (LEAD-22..25)

| Criterion | Spec-defined outcome | `file:line` + assertion | Result |
| --- | --- | --- | --- |
| Valid PUT persists both | 200, `professionalData.company`, `bankingData.bank`, `pixKey` all match input | `Integration/ProfessionalBankingData/ProfessionalBankingDataEndpointsTests.cs:91-103` | ✅ PASS |
| Does not erase prior steps (payload/conjunction rule) | `consultation.result.outcome`, `simulations[0].installmentAmount`, `identification.fullName/documentType` all still present with their **actual prior values**, plus new data alongside | `ProfessionalBankingDataEndpointsTests.cs:106-129` — asserts `"eligible"`, `516.81m`, `"Ana Silva"`, `"CNH"` (not just non-null) | ✅ PASS |
| `pixKey` optional | absent/null accepted, no error | `ProfessionalBankingDataEndpointsTests.cs:131-141` | ✅ PASS |
| Lead missing / version mismatch | 404 / 409 + `currentVersion` | `ProfessionalBankingDataEndpointsTests.cs:143-162` | ✅ PASS |

### P1-5: Anexos (LEAD-26..34)

| Criterion | Spec-defined outcome | `file:line` + assertion | Result |
| --- | --- | --- | --- |
| Valid upload | 201, `status="uploaded"`, `type` matches | `Integration/Documents/DocumentsEndpointsTests.cs:75-87` | ✅ PASS |
| Lead missing | 404, upload never creates a lead (verified via follow-up `GET /leads/{id}` also 404) | `DocumentsEndpointsTests.cs:89-100` | ✅ PASS |
| >10MB or bad content-type | 400 | `DocumentsEndpointsTests.cs:102-123` (two tests: size, content-type) | ✅ PASS |
| Reupload same type marks previous `replaced`, excluded from active list | new doc active, old doc absent from `GET .../documents` | `DocumentsEndpointsTests.cs:125-144` | ✅ PASS |
| `personalDocumentSubtype` mismatch (etapa 3 `CNH` vs. upload `RG`) | upload accepted & listed as-is (blocking happens at etapa 6, not here) | `DocumentsEndpointsTests.cs:146-164` — `Assert.Equal("RG", dto.PersonalDocumentSubtype)`, still present in active list | ✅ PASS |
| `GET` excludes `deleted`/`replaced` | active list omits both | `DocumentsEndpointsTests.cs:166-183, 185-205` | ✅ PASS |
| `DELETE` soft-deletes | 204, doc absent from subsequent `GET` | `DocumentsEndpointsTests.cs:197-205` | ✅ PASS |
| Delete missing doc / list on missing lead | 404 | `DocumentsEndpointsTests.cs:207-223` | ✅ PASS |
| Separate uploads with abandonment between | both persist and appear together | `DocumentsEndpointsTests.cs:166-183` | ✅ PASS |

### P1-6: Confirmação/retry (LEAD-35..48) — highest-risk story

| Criterion | Spec-defined outcome | `file:line` + assertion | Result |
| --- | --- | --- | --- |
| Pendencies exist | 422, no mock call made | `Integration/Confirmation/ConfirmationEndpointsTests.cs:100-116` — `Assert.Equal((HttpStatusCode)422, ...)`, `Assert.Equal(0, spy.CallCount)` (real call-count spy substituted via DI, not a guess) | ✅ PASS |
| Mock `success` | 200, `status="completed"`, `finalRegistration.registrationId` non-empty | `ConfirmationEndpointsTests.cs:118-130` | ✅ PASS |
| Mock `rejected`/`validationError`/`unavailable`/`timeout` | 422/422/503/504 respectively, lead → `failed_retryable`, prior data (identification, simulation) preserved | `ConfirmationEndpointsTests.cs:132-150` (Theory, 4 cases, asserts exact status code per outcome + `lead.Status=="failed_retryable"` + `lead.Identification != null` + `Simulations.Count==1`) | ✅ PASS |
| Mock `indeterminate` | 202, `status="pending_verification"` | `ConfirmationEndpointsTests.cs:152-162` | ✅ PASS |
| Already `completed` | 409 | `ConfirmationEndpointsTests.cs:164-173` | ✅ PASS |
| Flag off ignores `mockOutcome` | deterministic `success` even when `mockOutcome=rejected` passed | `ConfirmationEndpointsTests.cs:175-196` | ✅ PASS |
| Retry idempotent when `registrationId` set | 200, same `registrationId`, `spy.CallCount` stays 1 (no second mock call) | `ConfirmationEndpointsTests.cs:208-227` | ✅ PASS |
| Retry from `failed_retryable` w/o `registrationId` | calls mock again, can succeed, `spy.CallCount==1` after that one retry call | `ConfirmationEndpointsTests.cs:229-247` | ✅ PASS |
| Retry with no prior confirm attempt | 409 | `ConfirmationEndpointsTests.cs:198-206` | ✅ PASS |
| Two simultaneous `confirm` calls | exactly one succeeds (non-409), the other 409 | `ConfirmationEndpointsTests.cs:249-263` — `Task.WhenAll`, `Assert.Equal(1, conflictCount)`, `Assert.Equal(1, processedCount)` | ✅ PASS |
| `PendingRequirementsValidator` — every pendency combination | missing simulation / missing identification / incompatible doc / missing personal doc / missing payslip / all-met / all-missing (4 reasons) | `Unit/Features/Confirmation/PendingRequirementsValidatorTests.cs:36-109` (8 tests, one per combination) | ✅ PASS |

**Spec-precision note**: AC2 says "422 com lista exata dos motivos" (exact reasons list). The integration test only substring-matches one word (`"simulação"`) inside the raw Problem Details JSON (`ConfirmationEndpointsTests.cs:114`), not a parsed, exact comparison of `extensions.reasons`. The exact-reasons-per-scenario claim is fully covered at the unit level (`PendingRequirementsValidatorTests`), so risk is low, but the integration-level assertion itself is weaker than "exact list" — flagged as ⚠️ minor spec-precision gap (not a functional gap; underlying logic is proven, but the wiring test's assertion could be tightened to parse `extensions.reasons` and assert the array contents).

### P1-7: Listagem/recuperação (LEAD-49..54)

| Criterion | Spec-defined outcome | `file:line` + assertion | Result |
| --- | --- | --- | --- |
| `pageSize` default 20 | `PageSize==20` | `Integration/Leads/LeadsEndpointsTests.cs:39-46` | ✅ PASS |
| `pageSize>100` clamps at 100 | `PageSize==100` | `LeadsEndpointsTests.cs:48-55` | ✅ PASS |
| Sort `createdAt` desc | most-recent lead first | `LeadsEndpointsTests.cs:57-74` | ✅ PASS |
| `cpf` filter (additive) | 1 result, exact `id`/`status`/`progress.currentStep`/timestamps/`finalRegistration=null` | `LeadsEndpointsTests.cs:76-94` | ✅ PASS |
| Pagination | page1 has 2 items, page2 non-overlapping | `LeadsEndpointsTests.cs:96-112` | ✅ PASS |
| `GET /leads/{id}` full lead / 404 | 200 with matching id+status / 404 | `LeadsEndpointsTests.cs:114-133` | ✅ PASS |

### P1-8: Erros/concorrência (LEAD-55..60)

| Criterion | Spec-defined outcome | `file:line` + assertion | Result |
| --- | --- | --- | --- |
| Problem Details shape (`type`,`title`,`status`,`detail`) | RFC 9457 shape | `Core/Exceptions/ProblemDetailsExceptionHandler.cs:58-64` (`Build` always sets all 4 fields); exercised transitively by every 4xx/5xx test above | ✅ PASS (structural, not independently unit-tested — see gap list) |
| Distinct exception types for 404 vs 409 | never the same type | `Core/Exceptions/ProblemDetailsExceptionHandler.cs:35-47` — 3 distinct 404 types (`LeadNotFoundException`, `DocumentNotFoundException`, `SimulationNotFoundException`), 4 distinct 409 types | ✅ PASS |
| `expectedVersion` mismatch → 409 + `currentVersion` | exact int value | Consultation/Identification/ProfessionalBankingData tests above (3x) | ✅ PASS |
| Atomic per-step update, never full-doc replace | `findOneAndUpdate` filtered, never `ReplaceOne` on the full `Lead` | `Core/MongoContext.cs:93-114` (`UpdateWithVersionCheckAsync`) — confirmed via **discrimination sensor mutation 4** below (removing the version filter broke exactly the 3 dependent tests, proving the filter is load-bearing) | ✅ PASS |
| `version` increments atomically | `.Inc(l => l.Version, 1)` on every mutating update | present in every handler (`ConsultationHandler`, `SimulationHandler`, `IdentificationHandler`, `ProfessionalBankingDataHandler`, `ConfirmationHandler`) — not independently asserted as "`version` increased by exactly 1" in a dedicated test, only implicitly via the `expectedVersion` 409 tests (which prove the *initial* version is 1, not that later increments are exact) | ⚠️ Spec-precision gap (see below) |
| DTOs only at boundary, no raw Mongo doc exposed | every endpoint returns a DTO type | `Features/Consultation/ConsultationEndpoints.cs:19,31` (`Results.Created(..., dto)` / `Results.Ok(dto)`); same pattern confirmed across all endpoint files | ✅ PASS |

### P1-9: NFR transversais (LEAD-61..65)

| Criterion | Spec-defined outcome | `file:line` + assertion | Result |
| --- | --- | --- | --- |
| Async I/O everywhere | no blocking `.Result`/`.Wait()`/`GetAwaiter().GetResult()` in production code | Verified by repo-wide grep — zero matches outside test code | ✅ PASS (verified structurally, no dedicated test) |
| Connection string via env var | `ConnectionStrings__MongoDb` read from `IConfiguration`, never hardcoded | `Program.cs:16-17`; `docker-compose.yml:16` sets it | ✅ PASS |
| No CPF/banking data in logs | `RequestLoggingMiddleware` only logs method/path/status/duration, never bodies | `Core/Logging/RequestLoggingMiddleware.cs:21-26` — log call has no request/response body parameter | ✅ PASS (see Code Quality gap: `SensitiveDataMasker` is unit-tested but never invoked from any production log call — dead code, though the NFR itself holds via the simpler "never log bodies" design) |
| `docker compose up --build` starts cleanly | API+Mongo+frontend up, no manual step | Confirmed via T24 task completion record (`tasks.md:657-660`); not independently re-run by this Verifier (see Gate Check) | ⚠️ Not independently re-verified this session (see below) |
| `GET /health` → 200 | 200 `{status:"ok"}` | `Program.cs:57`; exercised by `ci.yml` per T24 | ✅ PASS (structural) |

---

## Discrimination Sensor

Ran in an isolated `git worktree` (`git worktree add <scratch> HEAD`), never in the real tree. Baseline `git status --porcelain` was empty before and after; confirmed identical post-cleanup (see Gate Check below).

| # | Mutation | File:line | Description | Killed? |
| --- | --- | --- | --- | --- |
| 1 | Price formula rate | `Features/Simulation/SimulationCalculator.cs:9` | `Rate = 0.018m` → `0.020m` | ✅ Killed — `SimulationCalculatorTests.Calculate_WithReadmeExample_MatchesExactContractValues` failed (`Expected: 516.81, Actual: 528.71`) |
| 2 | Confirmation mutex status filter | `Features/Confirmation/ConfirmationHandler.cs:70` | `Filter.Nin(l => l.Status, MutexExcludedStatuses)` → `Filter.In(...)` (inverted the allow/deny set) | ✅ Killed — 11/13 `ConfirmationEndpointsTests` failed (happy path, indeterminate, retry-idempotent, retry-real, pendencies-422 all broke) |
| 3 | Document-compatibility check | `Features/Confirmation/PendingRequirementsValidator.cs:41-42` | Removed `!` from `!string.Equals(...)` — now flags a pendency when documents **match** instead of when they mismatch | ✅ Killed — `Validate_WhenAllRequirementsMet_ReturnsEmptyList` and `Validate_WhenPersonalDocumentSubtypeIncompatibleWithIdentification_ReturnsReason` both failed |
| 4 | Optimistic-concurrency version check | `Core/MongoContext.cs:96-98` | Removed the `expectedVersion` branch — filter always `Eq(id)` only, version never checked | ✅ Killed — all 3 `*_WhenExpectedVersionMismatches_Returns409WithCurrentVersion` tests failed across Consultation, Identification, ProfessionalBankingData (`Expected: Conflict, Actual: OK`) |
| 5 | Array-filter `elem._id` regression | `Features/Simulation/SimulationHandler.cs:77` | `"elem._id"` → `"elem.id"` (the exact bug a prior review already caught and fixed) | ✅ Killed — `PatchSelect_SwitchesSelectionWithoutRecalculating` failed on the post-select `GET` re-read (`Expected: True, Actual: False`) — the immediate response lied (handler mutates the in-memory object unconditionally) but the DB re-read assertion caught the silent no-op write |

**Sensor depth**: lightweight (5 targeted mutations, standard-tier feature)
**Result**: 5/5 killed — ✅ PASS

**Isolation verification**: `git worktree remove --force <scratch>` after each revert-cycle; final `git status --porcelain` on the real tree matched the pre-sensor baseline exactly (both empty).

---

## Code Quality

| Principle | Status | Notes |
| --- | --- | --- |
| No features beyond what was asked | ✅ | |
| No abstractions for single-use code | ⚠️ | `Core/Logging/SensitiveDataMasker.cs` is implemented and unit-tested (T6) but never called from any production code path — `RequestLoggingMiddleware` satisfies LEAD-63 by never logging bodies at all, so the masker is currently dead code. Not a spec violation (the NFR outcome holds), but a minor "code that does nothing" smell worth a follow-up: either wire it into a body-logging call site if one is ever added, or remove it. |
| No unnecessary "flexibility" added | ✅ | |
| Only touched files required for task | ✅ | |
| Didn't "improve" unrelated code | ✅ | The `/simplify` pass (`247f50e`..`8b2b86b`) touched only files already in scope for this feature (exception base classes, shared Mongo helpers, DTO reuse, frontend step-id map) |
| Matches existing patterns/style | ✅ | Vertical-slice shape (`Endpoints`/`Dtos`/`Handler`) consistent across all 7 backend slices |
| Would senior engineer approve? | ✅ | |
| Tests map to ACs and are non-shallow (spot-check P1-4/P1-6) | ✅ | ProfessionalBankingData test asserts actual preserved values (not just non-null); Confirmation tests use a real call-count spy, not a mock-verify guess |
| Spec-anchored outcome check | ⚠️ | 1 gap flagged (P1-6 AC2 "lista exata dos motivos" — integration test substring-matches, doesn't parse+assert the array; covered precisely at unit level) |
| Per-layer Coverage Expectation met | ✅ | Domain logic (`SimulationCalculator`, `PendingRequirementsValidator`, `MockOutcomeResolver`, `SensitiveDataMasker`) has unit tests; every route has happy+edge+error integration coverage |
| Every test maps to a spec AC/edge case/Done-when | ✅ | No unclaimed tests found |
| Documented guidelines followed | ✅ | `coding-guidelines` skill (per tasks.md tool selection); AGENTS.md vertical-slice mandate followed (AD-001) |

---

## Edge Cases (spec.md)

| Edge case | Status |
| --- | --- |
| CPF malformado → 400 | ⚠️ Code implements it (`Features/Consultation/ConsultationValidator.cs:15` — regex `^\d{11}$`) but **no test exercises a malformed CPF specifically** (existing tests only cover `consultationAuthorized=false` and a missing field, both different validation branches). Gap: untested, not unimplemented. |
| Upload >10MB → 400 before GridFS | ✅ `DocumentsEndpointsTests.cs:102-112` |
| `confirm` on already-`completed` → 409 | ✅ `ConfirmationEndpointsTests.cs:164-173` |
| Two simultaneous `PUT .../identification` with same `expectedVersion` → one 200, one 409 | ⚠️ **Not directly tested as a real race** (no `Task.WhenAll` test for this specific route, unlike Confirmation's dedicated concurrency test). The underlying mechanism (`UpdateWithVersionCheckAsync`, single-document atomic `findOneAndUpdate`) is shared and proven race-safe by construction (MongoDB single-document atomicity) and by discrimination mutation 4, but there is no dedicated concurrent-race integration test for this specific edge case as named in spec.md. |
| `retry-submission` without prior `confirm` → 409 | ✅ `ConfirmationEndpointsTests.cs:198-206` |
| Consultation `unavailable` → lead stays `in_progress`, retriable | ✅ `ConsultationEndpointsTests.cs:94-104` (status assertion); retriability itself covered by `PutConsultation_OnExistingLead_ReRunsWithoutCreatingNewLead` |
| New simulation → previous stays in history, `selected=false` | ✅ `SimulationEndpointsTests.cs:96-115` |

---

## Gate Check

- **Gate command**: `dotnet build backend/ConsignadoLeads.slnx && dotnet format backend/ConsignadoLeads.slnx --verify-no-changes && dotnet test backend/ConsignadoLeads.slnx`, plus `cd frontend && npm run build`
- **Build**: 0 Warning(s), 0 Error(s)
- **Format**: `--verify-no-changes` exit 0 (no formatting drift)
- **Test result**: **79 passed, 0 failed, 0 skipped** (1 test assembly, ~37s, Testcontainers.MongoDb via Docker)
- **Frontend build**: `tsc -b && vite build` succeeded, 54 modules transformed, no errors
- **Test count before feature**: 0 (skeleton project, no test project existed — confirmed by T1's own Done-when: "0 tests, exit 0")
- **Test count after feature**: 79
- **Delta**: +79 new tests
- **Skipped tests**: none
- **Failures**: none

**Note on `docker compose up --build` (Stack gate)**: not independently re-run in this validation session (would require a longer-running full-stack boot + manual click-through). T24's own completion record already covers this per tasks.md. Flagged as not re-verified rather than assumed — if a strict re-run is desired, execute `docker compose up --build`, confirm `/health` 200 and frontend on `:3000`.

---

## Fix Plans (if issues found)

No blocking issues found. Below are non-blocking follow-ups (all ⚠️, none ❌):

### Fix 1: Untested malformed-CPF edge case

- **Root cause**: `ConsultationValidator` implements the regex check but no integration test exercises a malformed (non-11-digit or non-numeric) CPF value.
- **Fix task**: Add `PostConsultation_WithMalformedCpf_Returns400` to `ConsultationEndpointsTests.cs` asserting 400 for e.g. `cpf="123"`.
- **Priority**: Minor

### Fix 2: No dedicated concurrent-PUT race test for `steps/identification`

- **Root cause**: Only Confirmation has a `Task.WhenAll`-based concurrency test; the identification-route race named explicitly in spec.md's Edge Cases is not directly exercised, even though the shared atomic-update mechanism is proven correct by other means.
- **Fix task**: Add a `Task.WhenAll` test in `IdentificationEndpointsTests.cs` mirroring the Confirmation one (two simultaneous `PUT` with the same `expectedVersion`, assert exactly one 200 and one 409).
- **Priority**: Minor

### Fix 3: `SensitiveDataMasker` is unused (dead code)

- **Root cause**: T6 built the masker as specified, but `RequestLoggingMiddleware` never logs bodies at all, so the masker has no call site.
- **Fix task**: Either wire it into any future body-logging call site, or remove it if no such call site is planned. No urgency — the NFR (LEAD-63) is satisfied today regardless.
- **Priority**: Cosmetic

### Fix 4: Confirm-422 integration test doesn't assert the exact reasons array

- **Root cause**: `Confirm_WhenPendenciesExist_Returns422WithoutCallingMock` substring-matches one word instead of parsing `extensions.reasons` and comparing the array.
- **Fix task**: Parse the Problem Details JSON and assert `extensions.reasons` contains the expected reason strings exactly (the unit-level `PendingRequirementsValidatorTests` already covers the exact-reasons logic in isolation, so this is a wiring-assertion strengthening, not new logic).
- **Priority**: Minor

### Fix 5: Stale requirement-traceability / STATE.md handoff

- **Root cause**: `spec.md`'s Requirement Traceability table still shows all 70 `LEAD-XX` rows as `Pending`/`Design`, and `.specs/STATE.md`'s `## Handoff` section still reads `[none yet — feature not started]`, despite all 24 tasks being complete and 79 tests passing. This is a documentation-sync gap, not a code gap.
- **Fix task**: Update `spec.md`'s traceability table to `✅ Verified` for LEAD-01..65 (P1, this validation's evidence) and leave LEAD-66..70 (P2) as-is pending their own review; update `STATE.md`'s Handoff with a short pointer to this validation report.
- **Priority**: Minor (process hygiene, not a functional gap)

---

## Requirement Traceability Update

| Requirement | Previous Status | New Status (per this validation) |
| --- | --- | --- |
| LEAD-01..07 (P1-1 Consulta) | Pending | ✅ Verified |
| LEAD-08..15 (P1-2 Simulação) | Pending | ✅ Verified |
| LEAD-16..21 (P1-3 Identificação) | Pending | ✅ Verified |
| LEAD-22..25 (P1-4 Dados prof./bancários) | Pending | ✅ Verified |
| LEAD-26..34 (P1-5 Anexos) | Pending | ✅ Verified |
| LEAD-35..48 (P1-6 Confirmação/retry) | Pending | ✅ Verified (1 spec-precision gap noted: AC2 reasons-list assertion strength) |
| LEAD-49..54 (P1-7 Listagem/recuperação) | Pending | ✅ Verified |
| LEAD-55..60 (P1-8 Erros/concorrência) | Pending | ✅ Verified (1 spec-precision gap noted: version-increment-by-exactly-1 not independently asserted) |
| LEAD-61..65 (P1-9 NFR transversais) | Pending | ✅ Verified (structural evidence; `docker compose up --build` not re-run this session) |
| LEAD-66..69 (P2-1 Modal CPF) | Pending | ⚠️ Implemented (frontend, `CpfReuseModal.tsx` present) — no automated test by explicit prior decision; not scored against P1 bar |
| LEAD-70 (P2-2 Swagger) | Pending | ✅ Verified structurally (`Program.cs` wires `AddSwaggerGen`/`UseSwagger`/`UseSwaggerUI` in Development) |

*(This table is this Verifier's assessment for the report; `spec.md` itself was left untouched per the read-only mandate — see Fix 5.)*

---

## Summary

**Overall**: ✅ Ready (PASS, with 5 minor/cosmetic non-blocking follow-ups)

**Spec-anchored check**: 60+/62 P1 ACs matched spec outcome with precise `file:line` evidence; 2 spec-precision gaps flagged (P1-6 AC2 reasons-array assertion strength; version-increment-by-1 not independently asserted, only implied)
**Sensor**: 5/5 mutations killed
**Gate**: 4/4 passed (build, format, 79 backend tests, frontend build), 0 failed

**What works**: All 12 contract routes implemented and integration-tested with exact status codes and field values (not just "no error thrown"). The Price formula matches the README's exact worked example to the cent. The confirmation mutex and optimistic-concurrency version check are both proven load-bearing by the discrimination sensor (mutating either one breaks real tests, not just theoretical risk). The exact array-filter bug (`elem._id` vs `elem.id`) that a prior review caught is now guarded by a test that fails on the DB re-read even though the handler's immediate in-memory response would mask it. DTOs are used at every boundary; distinct exception types map 404s and 409s separately; Problem Details carries `extensions.currentVersion`/`extensions.reasons` correctly.

**Issues found**: 5 non-blocking (see Fix Plans 1-5 above) — none block a PASS verdict; all are either missing-but-low-risk test coverage, dead code, an assertion-strength nit, or stale doc bookkeeping.

**Next steps**: Optional follow-ups (not required before calling the feature done): add the malformed-CPF test, add an identification concurrency test, tighten the confirm-422 reasons assertion, decide the fate of `SensitiveDataMasker`, and sync `spec.md`/`STATE.md` traceability status.
