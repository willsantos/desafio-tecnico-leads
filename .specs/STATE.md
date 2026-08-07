# STATE

## Decisions

### AD-001
- **Decision**: Backend organized as vertical slices by feature (one folder per contract operation), not by technical layer (no repo-wide Controllers/Services/Repositories split).
- **Reason**: Each contract endpoint is close to self-contained (own DTOs, validation, Mongo access); a slice should be readable end-to-end without jumping across layers. Matches `AGENTS.md` (user-mandated).
- **Trade-off**: Some duplication across slices (each builds its own Mongo filter/update) instead of a shared repository method per operation; accepted to keep slices independent.
- **Scope**: `backend/src/ConsignadoLeads.Api` — all feature folders. Frontend follows the same principle per-step where reasonable.
- **Date**: 2026-08-05
- **Status**: active

### AD-002
- **Decision**: Uploaded documents (etapa 5) are stored in MongoDB GridFS; `lead_documents` collection holds only metadata + `gridFsFileId`.
- **Reason**: No extra storage service or Docker volume needed — stays consistent with "MongoDB standalone" and "no manual steps" requirements; `MongoDB.Driver` (with GridFS support) is already the only referenced package.
- **Trade-off**: Binary reads go through GridFS chunked download instead of a direct filesystem/CDN read; acceptable at prototype scale (≤10MB files).
- **Scope**: `Features/Documents/*`, `Core/MongoContext` (GridFS bucket accessor).
- **Date**: 2026-08-05
- **Status**: active

### AD-003
- **Decision**: No mediator/CQRS library (e.g. MediatR) — each slice's endpoint delegates directly to a plain handler class/method.
- **Reason**: The slice count and cross-cutting behavior needs (just Problem Details mapping + optional logging) don't justify an extra dependency inside a 7-day build; direct calls keep the dependency graph identical to what's already referenced (`MongoDB.Driver` only) and are easier to reason about under time pressure.
- **Trade-off**: No built-in pipeline behaviors (validation/logging middleware per request) — cross-cutting concerns are wired explicitly (global exception handler) instead of via a mediator pipeline.
- **Scope**: Whole backend.
- **Date**: 2026-08-05
- **Status**: active

### AD-004
- **Decision**: Concurrency uses optimistic versioning (`version` field, `expectedVersion` request param) via `FindOneAndUpdateAsync` filtered on `{_id, version}`; the confirm/retry mutex reuses the same atomic-update mechanism, gated on `status` instead of `version`.
- **Reason**: MongoDB standalone (no replica set/transactions per README seção 6) rules out session-based transactions; atomic single-document filtered updates give the same safety without that infra.
- **Trade-off**: Multi-field cross-document consistency (e.g. lead + its documents) is not transactional — accepted or mitigated per-slice (see Documents slice write order in `design.md`).
- **Scope**: `Core/MongoContext`, every `Features/*/Handler` that mutates a `Lead`.
- **Date**: 2026-08-05
- **Status**: active

### AD-005
- **Decision**: New feature spec for frontend UI/UX redesign (`ui-redesign`) using CSS Modules + CSS custom properties, with green as the bank primary color.
- **Reason**: The existing frontend is functional but visually crude; a cohesive design system improves trust and usability without touching the frozen API contract.
- **Trade-off**: Adds a one-time refactoring of all 6 pages and shared components; benefits consistency but is a non-trivial frontend-only change.
- **Scope**: `frontend/src/`; `.specs/features/ui-redesign/`
- **Date**: 2026-08-06
- **Status**: active

## Handoff

- **Feature**: lead-recovery (`.specs/features/lead-recovery/`)
- **Phase / Task**: Done — Execute complete (24/24 tasks), `/simplify` pass applied, Verifier PASS
- **Completed**: T1-T24, all committed (see `tasks.md` checkboxes and `git log 3a311bc..HEAD`)
- **In-progress**: none
- **Next step**: UI/UX redesign spec created in `.specs/features/ui-redesign/` (AD-005); implementation can start with Phase 1 (design-system foundation). Non-blocking follow-ups from `validation.md` are optional polish, not required for delivery.
- **Blockers**: none
- **Uncommitted files**: none
- **Branch**: main
