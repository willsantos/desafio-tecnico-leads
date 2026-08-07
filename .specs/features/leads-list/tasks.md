# Leads List Tasks

**Design**: `.specs/features/leads-list/design.md`
**Status**: Pending execution

---

## Gate Check Commands

| Gate Level | When to Use | Command |
| --- | --- | --- |
| Build (frontend) | After any frontend change | `cd frontend && npm run build` |
| Visual smoke test | After routing + list page | Abrir `http://localhost:3000/leads` e testar filtros/retomada |
| Stack | Final end-to-end | `docker compose up --build` |

---

## Execution Plan

Phases run in sequence. Tasks within a phase execute in order.

### Phase 1: Routing foundation
- T1 → T2

### Phase 2: List page
- T3 → T4 → T5

### Phase 3: Navigation and polish
- T6 → T7

---

## Task Breakdown

### Phase 1: Routing foundation

#### T1: Install react-router-dom and wire router

**What**: Add `react-router-dom` to `frontend/package.json`, wrap `App` with `BrowserRouter` in `main.tsx`, and set up `Routes`/`Route` in `App.tsx` for `/` and `/leads`.

**Where**:
- `frontend/package.json`
- `frontend/package-lock.json`
- `frontend/src/main.tsx`
- `frontend/src/App.tsx`

**Depends on**: None
**Reuses**: `LeadProvider`, existing page components
**Requirement**: LIST-01

**Done when**:
- [ ] `npm install react-router-dom` executado e lockfile atualizado.
- [ ] `main.tsx` envolve `<App />` com `<BrowserRouter>`.
- [ ] `App.tsx` renderiza `<Routes>` com `/` → wizard e `/leads` → `LeadsListPage`.
- [ ] Acessar `/leads` manualmente funciona.

**Tests**: none
**Gate**: Build (frontend)
**Commit**: `feat(frontend): add react-router-dom and basic routes`

---

#### T2: Add global Header with navigation

**What**: Create `frontend/src/shared/components/layout/Header.tsx` with title and links to `/` and `/leads`; include it in `App.tsx` above routes.

**Where**:
- `frontend/src/shared/components/layout/Header.tsx`
- `frontend/src/shared/components/layout/Header.module.css`
- `frontend/src/App.tsx`

**Depends on**: T1
**Reuses**: `Text`, `Button`/`Link`, tokens
**Requirement**: LIST-05

**Done when**:
- [ ] Header renders on both `/` and `/leads`.
- [ ] Active route is visually highlighted.
- [ ] Clicking links navigates without full page reload.

**Tests**: none
**Gate**: Build (frontend)
**Commit**: `feat(frontend): add global header navigation`

---

### Phase 2: List page

#### T3: Create leads API and label utilities

**What**: Add `frontend/src/features/leads/leadsApi.ts` with `listLeads`; add `frontend/src/shared/utils/leadLabels.ts` with translated status/step labels.

**Where**:
- `frontend/src/features/leads/leadsApi.ts`
- `frontend/src/shared/utils/leadLabels.ts`

**Depends on**: None
**Reuses**: `httpClient`, `PagedLeadsResponse`, formatters
**Requirement**: LIST-02, LIST-03

**Done when**:
- [ ] `listLeads` builds correct query string from filters and pagination.
- [ ] Labels cover all `status` and `currentStep` enum values.

**Tests**: none
**Gate**: Build (frontend)
**Commit**: `feat(frontend): add leads list API and label utilities`

---

#### T4: Create StatusBadge and list item components

**What**: Create `StatusBadge`, `LeadSummaryCard` (mobile), and `LeadSummaryRow` (desktop) under `frontend/src/features/leads/components/`.

**Where**:
- `frontend/src/features/leads/components/StatusBadge.tsx`
- `frontend/src/features/leads/components/LeadSummaryCard.tsx`
- `frontend/src/features/leads/components/LeadSummaryRow.tsx`
- Associated `.module.css` files

**Depends on**: T3
**Reuses**: `Card`, `Button`, `Text`, `maskCpf`, `formatDateToBrazilian`, `leadLabels`
**Requirement**: LIST-02

**Done when**:
- [ ] Card displays CPF, status badge, step, dates, and resume action.
- [ ] Row displays the same data in a table-friendly layout.
- [ ] Resume button is disabled while loading.

**Tests**: none
**Gate**: Build (frontend)
**Commit**: `feat(frontend): add lead list item components`

---

#### T5: Implement LeadsListPage with filters and pagination

**What**: Create `frontend/src/features/leads/LeadsListPage.tsx` with filters, loading/error states, empty state, and pagination.

**Where**:
- `frontend/src/features/leads/LeadsListPage.tsx`
- `frontend/src/features/leads/LeadsListPage.module.css`

**Depends on**: T2, T3, T4
**Reuses**: `Input`, `Select`, `Button`, `Text`, `Alert`, `LoadingError`, `Card`
**Requirement**: LIST-02, LIST-03

**Done when**:
- [ ] Page fetches leads on mount and when filters/page change.
- [ ] Filters render with translated options.
- [ ] CPF input uses `maskCpf` and sends only digits.
- [ ] Pagination respects `totalPages`.
- [ ] Empty state shows CTA to create a new proposal.

**Tests**: none
**Gate**: Build (frontend) + visual smoke test
**Commit**: `feat(frontend): implement leads list page with filters and pagination`

---

### Phase 3: Navigation and polish

#### T6: Wire resume action from list into wizard

**What**: On "Continuar", fetch full lead via `GET /leads/{id}`, call `setLead`, `setStep(resolveStepId(lead))`, and navigate to `/`.

**Where**:
- `frontend/src/features/leads/LeadsListPage.tsx`

**Depends on**: T5
**Reuses**: `useLead`, `resolveStepId`, `useNavigate`
**Requirement**: LIST-04

**Done when**:
- [ ] Clicking resume loads the lead and opens the wizard at the correct step.
- [ ] 404 errors are shown and list is refreshed.

**Tests**: none
**Gate**: Visual smoke test
**Commit**: `feat(frontend): add lead resume from list page`

---

#### T7: Improve CpfReuseModal to show multiple leads

**What**: Update `CpfReuseModal` to list active leads using `LeadSummaryCard`/`LeadSummaryRow` instead of always choosing `activeLeads[0]`.

**Where**:
- `frontend/src/features/consultation/CpfReuseModal.tsx`
- `frontend/src/features/consultation/ConsultationPage.tsx` (callback signature)

**Depends on**: T4
**Reuses**: `LeadSummaryCard`, `LeadSummaryRow`, `Text`, `Modal`
**Requirement**: LIST-06

**Done when**:
- [ ] Modal lists all active leads when more than one exists.
- [ ] User can select which lead to resume.
- [ ] Single lead behavior remains unchanged.

**Tests**: none
**Gate**: Build (frontend) + visual smoke test
**Commit**: `feat(frontend): allow choosing which active lead to resume`

---

## Phase Execution Map

- **Phase 1** (Routing): T1, T2
- **Phase 2** (List page): T3, T4, T5
- **Phase 3** (Navigation/polish): T6, T7

Total: 7 tasks.

---

## Task Granularity Check

| Task | Scope | Status |
| --- | --- | --- |
| T1 | Install router + routes | ✅ Granular |
| T2 | Global header | ✅ Granular |
| T3 | API + labels | ✅ Granular |
| T4 | List item components | ✅ Granular |
| T5 | List page | ✅ Granular |
| T6 | Resume wiring | ✅ Granular |
| T7 | CpfReuseModal improvement | ✅ Granular |
