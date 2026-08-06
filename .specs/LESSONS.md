# LESSONS - auto-maintained by scripts/lessons.py

> Machine-owned. Do NOT hand-edit. Changes are overwritten on the next `lessons.py` write.
> Canonical state lives in `.specs/lessons.json`. Edit lessons only via the script.
> promote_threshold=2 distinct features · window_days=45 · quarantine_threshold=2

## Confirmed (load these at Specify/Design)

Corroborated across multiple features. Safe to apply as guidance.

_none_

## Candidates (under observation - do NOT load as guidance yet)

Seen once or not yet corroborated. Tracked, not trusted.

### L-001 - For payload-bearing 422/409 responses, parse the Problem Details extensions array and assert its exact contents, not a substring match on the raw JSON body.
- signal: `spec_precision_gap` · recurrence: 1 feature(s) · scope: `backend/tests` · harmful: 0
- features: lead-recovery
- evidence: validation.md#P1-6-AC2 - ConfirmationEndpointsTests.cs:114 (backend/tests)
- last seen: 2026-08-06T01:11:16Z

### L-002 - When a spec requires an atomic counter to increment by exactly 1 per update, add a dedicated test asserting the exact before/after delta, not just that a downstream conflict check works.
- signal: `spec_precision_gap` · recurrence: 1 feature(s) · scope: `backend/tests` · harmful: 0
- features: lead-recovery
- evidence: validation.md#P1-8 - MongoContext.cs:96-98 (backend/tests)
- last seen: 2026-08-06T01:11:16Z

### L-003 - Every named edge case in spec.md needs its own dedicated test, even when a more general validation test already covers the same code path indirectly.
- signal: `ac_gap` · recurrence: 1 feature(s) · scope: `backend/tests` · harmful: 0
- features: lead-recovery
- evidence: spec.md#Edge-Cases (malformed CPF -> 400) (backend/tests)
- last seen: 2026-08-06T01:11:24Z

### L-004 - When spec.md names a specific route for a concurrency edge case, add a Task.WhenAll race test for that exact route, not just for the route where concurrency risk is highest.
- signal: `ac_gap` · recurrence: 1 feature(s) · scope: `backend/tests` · harmful: 0
- features: lead-recovery
- evidence: spec.md#Edge-Cases (simultaneous PUT steps/identification same expectedVersion) (backend/tests)
- last seen: 2026-08-06T01:11:24Z

## Quarantined (failed when applied - ignore)

A confirmed lesson that recurred alongside failure. Kept for the maintainer to review.

_none_
