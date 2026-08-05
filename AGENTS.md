# AGENTS.md

Instructions for any AI coding agent working in this repository.

## Commits

- Use **Conventional Commits** for every commit message (`feat:`, `fix:`, `docs:`, `test:`, `refactor:`, `chore:`, `build:`, `ci:`, ...).
- One logical change per commit. Keep the subject line imperative and under ~72 chars; add a body when the "why" isn't obvious from the diff.

## API Contract — never alter

The API contract defined in `README.md` (seção 5, "CONTRATO DE API") is **fixed and non-negotiable**:

- Routes, HTTP methods, ports (`8080` for the API, `3000` for the frontend), and JSON field names/casing (camelCase) must never change.
- Status codes, enum values (`status`, `mockOutcome`, document `status`, etc.), and the Problem Details error shape must match the README exactly.
- The evaluation suite consumes this contract literally — any deviation in route, port, or JSON shape fails the automated tests.
- You may add non-breaking, purely additive extensions (e.g. an extra optional query parameter) only when doing so doesn't rename, remove, or repurpose anything already specified. When in doubt, don't touch the contract — extend elsewhere (frontend logic, internal services) instead.
- Everything *not* covered by seção 5 (internal architecture, folder layout, extra endpoints, libraries) is free to reorganize, per the README's own statement in its introduction.

Always re-check `README.md` seção 5 before touching any request/response shape, route, or status code.

## Architecture — Vertical Slice

Organize the backend (and, where it applies, the frontend) by **feature/vertical slice**, not by technical layer:

- Group everything a use case needs — request/response DTOs, handler/endpoint, validation, MongoDB access — together under one feature folder, instead of spreading it across repo-wide `Controllers/`, `Services/`, `Repositories/` layers.
- One slice per contract operation (e.g. `Features/Consultation/`, `Features/Simulation/`, `Features/Identification/`, `Features/ProfessionalBankingData/`, `Features/Documents/`, `Features/Confirmation/`, `Features/Leads/` for listing/retrieval). Cross-cutting concerns (Mongo connection setup, Problem Details error middleware, DTO base types, shared `Lead`/`LeadDocument` persistence model) live in a small shared/core layer that slices depend on — never the other way around.
- A slice should be readable end-to-end on its own: open its folder, see the whole request → validation → domain logic → persistence → response path without jumping across unrelated feature folders.
- Don't force a slice to reuse another slice's internals directly — share through the common/core layer instead. Some duplication between slices is acceptable if it keeps them independent.
- Same principle on the frontend where reasonable: colocate a step's page/component, its API calls, and its types, instead of centralizing everything in generic `components/` / `services/` grab-bags (still keep `App.tsx` thin, per README seção 7).
