# progress-resume-step Validation

**Date**: 2026-08-06
**Spec**: `.specs/features/progress-resume-step/spec.md`
**Diff range**: `4b7303c..HEAD` (commits `51b7aa9`, `868834e`, `0e1747f`, `8f7e538`)
**Verifier**: independent sub-agent (author ≠ verifier)

---

## Task Completion

| Task | Status  | Notes |
| ---- | ------- | ----- |
| P1 — Backend derives `progress.resumeStep` | ✅ Done | `ResumeStepResolver` + `LeadMapper.MapProgress`; `ProfessionalBankingDataHandler` loads active docs |
| P2 — Frontend consumes `resumeStep` with fallback | ✅ Done | `resolveStepId` prefers `resumeStep`, falls back to `completedSteps` derivation |
| P3 — README contract updated | ✅ Done | Seção 5 example + derived-field note + trade-off bullet |

---

## Spec-Anchored Acceptance Criteria

| Criterion (WHEN X THEN Y) | Spec-defined outcome | `file:line` + assertion | Result |
| ------------------------- | -------------------- | ----------------------- | ------ |
| **PRS-01** LeadDto.progress SHALL include `resumeStep` in every `GET /leads/{id}`, `PUT /steps/*`, `POST /leads/consultation` response | Non-empty string, always present | `backend/src/ConsignadoLeads.Api/Core/Dtos/LeadDto.cs:33` — `ProgressDto(... string ResumeStep)` non-nullable; `LeadDto.cs:105` always calls resolver. `ResumeStepEndpointsTests.cs:119` — `Assert.False(string.IsNullOrWhiteSpace(dto!.Progress.ResumeStep))`. Ubiquity verified: all 7 LeadDto-returning call sites wire `LeadMapper.ToDto` (`ConsultationHandler.cs:51,82`; `IdentificationHandler.cs:54`; `ProfessionalBankingDataHandler.cs:53`; `ConfirmationHandler.cs:44,113,124`; `LeadsHandler.cs:72`) | ✅ PASS |
| **PRS-02** WHILE `status` ∈ {`pending_confirmation`, `confirming`, `pending_verification`, `failed_retryable`, `completed`} → `resumeStep = "confirmation"` | Exact 5-status set → `"confirmation"` | `ResumeStepResolver.cs:20-27,37-40` — `ConfirmationReachedStatuses` set matches spec set exactly; returns `"confirmation"`. `ResumeStepResolverTests.cs:14-28` — `[Theory]` over all 5 statuses, `Assert.Equal("confirmation", result)` | ✅ PASS |
| **PRS-03** WHEN a backend step (`consultation`/`simulation`/`identification`/`professional-banking-data`) is missing from `completedSteps` → first such step by flow order | First missing in `BackendStepOrder` | `ResumeStepResolver.cs:43-49` — foreach over `["consultation","simulation","identification","professional-banking-data"]`, returns first missing. `ResumeStepResolverTests.cs:31-45` — 3 progression cases assert expected next step. `ResumeStepEndpointsTests.cs:124-131` — after consultation only → `"simulation"` | ✅ PASS |
| **PRS-04** WHILE all 4 backend steps done AND active docs do NOT contain both `personal_document` + `payslip` → `"documents"` | `"documents"` | `ResumeStepResolver.cs:51-54` — ternary returns `"documents"` when condition false. `ResumeStepResolverTests.cs:48-56` — empty docs → `"documents"`. `ResumeStepEndpointsTests.cs:134-144` (GET) + `:148-180` (PUT response) → `"documents"` | ✅ PASS |
| **PRS-05** WHILE all 4 backend steps done AND active docs contain both → `"confirmation"` | `"confirmation"` | `ResumeStepResolver.cs:52-53`. `ResumeStepResolverTests.cs:59-70` — both docs → `"confirmation"`. `ResumeStepEndpointsTests.cs:183-195` — both uploads via real endpoint → `"confirmation"` | ✅ PASS |
| **PRS-06** IF `completedSteps` empty or absent → `"consultation"` | `"consultation"` | `ResumeStepResolver.cs:42` — `?? []`, loop returns first `BackendStepOrder` item. `ResumeStepResolverTests.cs:73-87` — both `[]` and `null` cases assert `"consultation"` | ✅ PASS |
| **PRS-07** Mapper SHALL preserve `currentStep`, `startedSteps`, `completedSteps`, `pendingItems`, `lastUpdatedAt` (additive only) | All five passed through verbatim | `LeadDto.cs:107-113` — `MapProgress` passes the five existing fields unchanged, appends only `resumeStep`. `ResumeStepEndpointsTests.cs:199-210` — asserts `currentStep="identification"`, `completedSteps=[consultation,simulation,identification]`, `LastUpdatedAt != default` | ✅ PASS (minor: `startedSteps`/`pendingItems` not explicitly asserted, but code is verbatim pass-through — `ProgressDto` ctor receives the original lists) |
| **PRS-08** WHEN client receives a lead created before the feature → `resumeStep` present & correct (no migration) | Derived at read time; no persistence flag distinguishes old vs new leads | `LeadDto.cs:98-114` — field is computed in the read-model, never written to Mongo. Structural: any lead (regardless of age) flows through the same `MapProgress`, so the "old lead" scenario is equivalent to the freshly-created leads exercised by the integration suite | ✅ PASS |
| **PRS-09** WHEN `lead.progress.resumeStep` present → `resolveStepId` returns mapped `StepId` | Mapped `StepId` from `CURRENT_STEP_TO_STEP_ID` | `frontend/src/shared/leadContext.tsx:49-51` — `if (lead.progress.resumeStep && CURRENT_STEP_TO_STEP_ID[lead.progress.resumeStep]) return CURRENT_STEP_TO_STEP_ID[lead.progress.resumeStep]`. Guard checks both presence AND map membership | ⚠️ Spec-precision gap — no executable frontend test (no test runner in repo). Verified by code reading only |
| **PRS-10** WHERE `resumeStep` absent → `resolveStepId` uses `completedSteps`-based fallback | Local derivation (confirmation-reached statuses → `confirmation`; else first missing backend step; else `documents`) | `leadContext.tsx:53-65` — fallback path intact (unchanged statuses set + loop). `leadContext.tsx:56-64` walks `CURRENT_STEP_TO_STEP_ID` skipping `documents`/`confirmation`, returns first missing, defaults to `'documents'` | ⚠️ Spec-precision gap — no executable frontend test. `npm run build` (tsc) is type-only, does not exercise the logic |
| **PRS-11** Frontend maps `documents` and `confirmation` `resumeStep` values to their `StepId`s | Both keys present in `CURRENT_STEP_TO_STEP_ID` | `leadContext.tsx:22-29` — map includes `documents: 'documents'` and `confirmation: 'confirmation'` alongside the 4 backend steps | ⚠️ Spec-precision gap — no executable frontend test |
| **PRS-12** README seção 5 lead-shape example includes `resumeStep` with concrete value; README includes derived/non-persisted note + `currentStep` semantics note | Example has concrete value; explanatory note present | `README.md:179` (example) — `"resumeStep": "professional-banking-data",` (concrete value matching an identification-done lead). `README.md:195` (blockquote) — note explains derived/non-persisted, lists all 6 possible values, restates `currentStep` semantics, declares `resumeStep` canonical for resume. Trade-off bullet `README.md:373` updated to reflect backend-owned resume decision | ✅ PASS (documentation — verified by reading `git show 0e1747f`) |

**Status**: ✅ All 12 ACs covered (9 PASS with executable evidence; 3 ⚠️ spec-precision gaps on the frontend slice PRS-09/10/11 — no frontend test harness exists, so evidence is code-reading only). No ❌ gaps.

---

## Discrimination Sensor

| Mutation | File:line | Description | Killed? |
| -------- | --------- | ----------- | ------- |
| 1 | `ResumeStepResolver.cs:39` (scratch copy) | Confirmation-status branch returns `"documents"` instead of `"confirmation"` | ✅ Killed — 5 failures (`Resolve_ConfirmationReachedStatus_ReturnsConfirmation` over all 5 statuses) |
| 2 | `ResumeStepResolver.cs:52-54` (scratch copy) | Swapped documents-vs-confirmation ternary arms (both-docs → `"documents"`, else → `"confirmation"`) | ✅ Killed — 8 failures (unit PRS-04/05 + partial-doc + integration PRS-04/05 cases) |
| 3 | `ResumeStepResolver.cs:45` (scratch copy) | Flipped first-missing loop condition `!completed.Contains(step)` → `completed.Contains(step)` | ✅ Killed — 16 failures (first-missing, empty/null, unknown-ignore, abandoned, integration simulation/documents) |
| 4 | `LeadDto.cs:105` (scratch copy) | `MapProgress` drops active docs — passes `null` instead of `activeDocTypes` to resolver | ✅ Killed — 1 failure (`AfterProfessionalBankingData_AndBothDocuments_ResumeStep_IsConfirmation`, the real handler→mapper→resolver path) |
| F1 (frontend, not executable) | `leadContext.tsx:49-51` | Remove `resumeStep` branch (force fallback) | ⏭️ Not runnable — no frontend test harness. Reasoned: fallback coincidentally returns the same value for several states (e.g. all-backend-done + no docs → `'documents'`; `failed_retryable` → `'confirmation'`), so this mutant would likely **survive** if a test existed only for the happy path. Structural coverage gap, not a test-quality defect |
| F2 (frontend, not executable) | `leadContext.tsx:27-28` | Omit `documents`/`confirmation` keys from `CURRENT_STEP_TO_STEP_ID` | ⏭️ Not runnable. Reasoned: the guard at `:49` would reject unmapped values and fall through to fallback, which again masks the fault for several states → likely **survives** |

**Sensor depth**: lightweight (default; this is not a P0/auth/payment path)
**Backend result**: 4/4 mutations killed — ✅ PASS
**Frontend result**: 0/0 executable (no harness) — ⚠️ gap acknowledged; the frontend behavior is unverified by automation. Stryker/mutmut not wired into the project.

**Isolation log**:
- Pre-sensor real-tree `git status --porcelain`: 10 entries (9 `M` + 1 `??`).
- Scratch: dedicated worktree at `/tmp/opencode/prs-mutants` (`git worktree add HEAD`); mutated, tested, restored each mutant; worktree clean before removal.
- Post-sensor real-tree `git status --porcelain`: identical 10 entries. `git worktree remove --force` completed. Real tree untouched.

---

## Interactive UAT Results

Not performed — feature is backend-derived-field + frontend mapping logic; no user-facing interaction pattern beyond what the integration suite and `resolveStepId` cover. Per validate.md, automated checks suffice for backend/non-UI-interactive work.

---

## Code Quality

| Principle | Status |
| --------- | ------ |
| Minimum code — no over-engineering | ✅ |
| Surgical changes — only touched files required | ✅ for the contract/feature; see scope-creep note below |
| No scope creep | ⚠️ Commit `868834e` (frontend) bundles `sessionStorage` lead/step persistence (`LEAD_STORAGE_KEY`, `readStoredLead`, `writeStoredLead`, etc.) alongside the `resumeStep` logic. `sessionStorage` is **not** in spec PRS-09/10/11 nor any edge case. Harmless and committed as part of the feature diff, but two unrelated concerns share one commit. Flagged as a minor scope-discipline observation, not a blocker |
| Matches existing patterns | ✅ — resolver is a pure static function mirroring existing `Core/` helpers; mapper extension follows the existing `ProgressDto` shape; frontend map extension follows the existing `CURRENT_STEP_TO_STEP_ID` pattern |
| Spec-anchored outcome check (asserted values match spec) | ✅ backend; ⚠️ frontend by reading |
| Per-layer coverage: domain logic 1:1 ACs | ✅ — `ResumeStepResolver` (the domain decision) has a unit test per branch (PRS-02/03/04/05/06 + 3 edge cases) |
| Per-layer coverage: routes happy+edge+error | ✅ — integration suite covers happy path per state + the `PUT` response surface + the one-doc edge case |
| Every test maps to a spec requirement — no unclaimed tests | ✅ — each test carries an inline comment naming its AC/edge (`// PRS-0X`, `// Edge:`) |
| Documented guidelines followed | none — strong defaults applied (AGENTS.md vertical-slice + API-contract-freeze rules respected; `resumeStep` is additive, summary untouched) |

---

## Edge Cases

- [x] **Only one of two required docs** (e.g. only `personal_document`) → `"documents"`. Covered: `ResumeStepResolverTests.cs:90-105` (both `[personal_document]` and `[payslip]` cases) + integration `ResumeStepEndpointsTests.cs:213-224`.
- [x] **`status=abandoned`** → general rule, no special-casing. Covered: `ResumeStepResolverTests.cs:121-129` (`abandoned` + `[consultation,simulation]` → `"identification"`).
- [x] **Unknown values in `completedSteps`** → ignored. Covered: `ResumeStepResolverTests.cs:108-117` (`[consultation, bogus, simulation, identification]` → `"professional-banking-data"`).
- [⚠️] **Reupload marks prior doc `replaced`** → only active (`uploaded`) counted. **Handled by design** but **no direct test**: `MongoContext.GetActiveDocumentsAsync` (`Core/MongoContext.cs:71-84`) excludes `replaced`/`deleted` at the DB filter, and `LeadMapper.MapProgress` (`LeadDto.cs:100-103`) additionally filters `Status == "uploaded"` (belt-and-suspenders). No test injects a `replaced` document and asserts it is excluded from the documents-vs-confirmation decision. Minor coverage gap — the resolver unit tests pass `activeDocumentTypes` strings directly, bypassing the document-loading path.
- [x] **`GET /leads/{id}` returns empty documents** (old lead, no uploads) → treated as docs pending. Covered structurally by PRS-04's empty-docs case (`ResumeStepResolverTests.cs:48-56`, `activeDocumentTypes: []`).

---

## Gate Check

- **Gate command**: `dotnet test backend/src/ConsignadoLeads.Api.Tests/ConsignadoLeads.Api.Tests.csproj --nologo`
- **Result (full suite)**: 115 passed, **1 failed**, 0 skipped, 116 total.
- **Result (feature scope, filter `FullyQualifiedName~ResumeStep`)**: **23 passed, 0 failed, 0 skipped** (16 unit + 7 integration).
- **Test count before feature** (at `4b7303c`): 93 → after feature: 116. **Delta: +23** (matches the new test files; no tests deleted or skipped).
- **The 1 full-suite failure**: `LeadsEndpointsTests.GetLeads_SortsByCreatedAtDescending` (`LeadsEndpointsTests.cs:72`). **Pre-existing and unrelated** — that test file is touched only by commit `ed72beb` (original `GET /leads` implementation), never by any feature commit. The test creates 3 leads and asserts strict reverse-creation order via `?status=in_progress`; under concurrent test execution leads can share a `DateTime.UtcNow` tick, making the sort non-deterministic. **Confirmed flaky**: passes deterministically in isolation (2/2 re-runs passed). Not a regression introduced by this feature.
- **Frontend gate** (`npm run build` / tsc): not re-run by the verifier (no behavior coverage; type-only). Spec success criterion lists it as green per the implementer.

---

## Fix Plans (ranked)

No blockers. Ranked minor gaps for future hardening (do not block acceptance):

### Fix 1 (Minor): Add a direct test for the `replaced`-document edge case
- **Root cause**: The documents-vs-confirmation branch depends on active-document filtering that happens in two places (`MongoContext` DB filter + `MapProgress` `Status == "uploaded"` filter). Neither is directly tested with a `replaced` document in the lead's history.
- **Fix task**: Add an integration test that uploads `personal_document`, re-uploads `personal_document` (marking the first `replaced`), uploads `payslip`, then asserts `resumeStep == "confirmation"` (proving the replaced doc doesn't double-count or break the active filter).
- **Priority**: Minor.

### Fix 2 (Minor): Add a frontend unit test for `resolveStepId`
- **Root cause**: PRS-09/10/11 have no executable evidence — the repo has no frontend test runner. The discrimination sensor could not run frontend mutants F1/F2, and by reasoning both would likely survive against a happy-path-only test.
- **Fix task**: Introduce `vitest` (or equivalent) and add unit tests for `resolveStepId` covering: `resumeStep` present (each of the 6 values) → mapped `StepId`; `resumeStep` absent → fallback for confirmation-reached status, first-missing-step, and all-done-no-docs cases.
- **Priority**: Minor (the feature is functionally correct; this closes the evidence gap, not a behavior bug).

### Fix 3 (Cosmetic): Commit-message test-count + scope discipline
- **Root cause**: Commit `51b7aa9` claims "15 unit + 7 integration" tests; actual expanded count is **16 unit** + 7 integration (one `[Theory]` has an extra `InlineData`/case). Commit `868834e` bundles `sessionStorage` persistence (out-of-spec) with the `resumeStep` logic.
- **Fix task**: None required for correctness. For future commits, keep one concern per commit and re-check expanded test counts in the message.
- **Priority**: Cosmetic.

---

## Requirement Traceability Update

| Requirement | Previous Status | New Status |
| ----------- | --------------- | ---------- |
| PRS-01 | Verified | ✅ Verified |
| PRS-02 | Verified | ✅ Verified |
| PRS-03 | Verified | ✅ Verified |
| PRS-04 | Verified | ✅ Verified |
| PRS-05 | Verified | ✅ Verified |
| PRS-06 | Verified | ✅ Verified |
| PRS-07 | Verified | ✅ Verified (minor: startedSteps/pendingItems pass-through not explicitly asserted) |
| PRS-08 | Verified | ✅ Verified |
| PRS-09 | Verified | ✅ Verified (code-reading evidence; no frontend harness) |
| PRS-10 | Verified | ✅ Verified (code-reading evidence; no frontend harness) |
| PRS-11 | Verified | ✅ Verified (code-reading evidence; no frontend harness) |
| PRS-12 | Verified | ✅ Verified |

---

## Summary

**Overall**: ✅ Ready

**Spec-anchored check**: 9/12 ACs matched spec outcome with executable evidence; 3 ⚠️ spec-precision gaps on the frontend slice (PRS-09/10/11) — no frontend test harness exists, evidence is code-reading only.
**Sensor**: 4/4 backend mutations killed (lightweight depth; not a P0 path). Frontend mutants not executable (no harness) — gap acknowledged.
**Gate**: 23/23 feature tests pass. Full suite 115/116 (the 1 failure is a pre-existing flaky ordering test, unrelated to the feature, passes in isolation).

**What works**:
- `progress.resumeStep` is derived purely at read time, never persisted, always present and non-empty across every `LeadDto`-returning endpoint.
- The 4 decision branches (confirmation-status, first-missing-step, documents-pending, confirmation-ready) each have dedicated unit + integration evidence with spec-exact asserted values.
- Additive only — `currentStep`/`completedSteps`/`startedSteps`/`pendingItems`/`lastUpdatedAt` flow through unchanged; `GET /leads` summary untouched.
- Frontend prefers `resumeStep` and keeps a correct fallback for older responses; the `documents`/`confirmation` keys extend the existing map.
- README contract example and trade-off note updated consistently with the implementation.

**Issues found**:
- Three frontend ACs (PRS-09/10/11) lack executable tests — structural repo gap, not a behavior defect.
- The `replaced`-document edge case is handled by a double filter but has no direct test (Fix 1).
- Commit `868834e` bundles out-of-spec `sessionStorage` work with the feature (Fix 3).

**Next steps**: Accept the feature as delivered. Route Fix 1 and Fix 2 as minor hardening tasks when test-infra capacity allows; Fix 3 is informational.
