# UI Redesign Tasks

**Design**: `.specs/features/ui-redesign/design.md`
**Status:** Pending execution

---

## Gate Check Commands

| Gate Level | When to Use | Command |
| --- | --- | --- |
| Build (frontend) | After any frontend change | `cd frontend && npm run build` |
| Visual smoke test | After stepper/pages | Abrir `http://localhost:3000` e navegar pelas 6 etapas |
| Stack | Final end-to-end | `docker compose up --build` |

---

## Execution Plan

Phases run in sequence. Tasks within a phase execute in order.

### Phase 1: Design-system foundation
- T1 → T2

### Phase 2: Shared components
- T3 → T4 → T5 → T6 → T7

### Phase 3: Page redesigns
- T8 → T9 → T10 → T11 → T12 → T13

### Phase 4: Responsiveness, accessibility & polish
- T14 → T15

---

## Task Breakdown

### Phase 1: Design-system foundation

#### T1: Add design tokens and global styles

**What**: Create `frontend/src/shared/styles/tokens.css` with color, typography, spacing, border and shadow tokens; create `frontend/src/shared/styles/global.css` with a light reset, base font and box-sizing; import both in `main.tsx`.

**Where**:
- `frontend/src/shared/styles/tokens.css`
- `frontend/src/shared/styles/global.css`
- `frontend/src/main.tsx`

**Depends on**: None
**Reuses**: N/A
**Requirement**: UI-01

**Tools**:
- Skill: `react-best-practices`, `react-composition-patterns`

**Done when**:
- [ ] Tokens CSS defines the full green scale, slate neutrals, semantic colors, typography, spacing, radii and shadows.
- [ ] Global CSS sets `box-sizing: border-box`, base font-family and a minimal reset.
- [ ] `main.tsx` imports both CSS files.
- [ ] `npm run build` passes.

**Tests**: none
**Gate**: Build (frontend)
**Commit**: `feat(ui): add design tokens and global styles`

---

#### T2: Add shared typography/layout helpers

**What**: Create small utility CSS classes (or React components) for page title, section title, body text and muted text, all using tokens.

**Where**:
- `frontend/src/shared/styles/utilities.css` (optional)
- Or `frontend/src/shared/components/ui/Text.tsx`

**Depends on**: T1
**Reuses**: tokens.css
**Requirement**: UI-01

**Done when**:
- [ ] Text helpers use tokens for size, weight and color.
- [ ] No hardcoded font sizes or colors outside tokens.

**Tests**: none
**Gate**: Build (frontend)
**Commit**: `feat(ui): add typography helpers`

---

### Phase 2: Shared components

#### T3: Button component

**What**: Implement `frontend/src/shared/components/ui/Button.tsx` + `Button.module.css` with variants `primary`, `secondary`, `danger`, `ghost`; sizes `sm`, `md`, `lg`; states `disabled`, `loading`.

**Where**: `frontend/src/shared/components/ui/`

**Depends on**: T1
**Reuses**: tokens.css
**Requirement**: UI-02

**Done when**:
- [ ] All variants and states render correctly.
- [ ] Loading state shows spinner and disables click.
- [ ] Touch target at least 44×44 px.

**Tests**: none
**Gate**: Build (frontend)
**Commit**: `feat(ui): add Button component`

---

#### T4: Input, Select and Label components

**What**: Implement `Input`, `Select` and `Label` with error state, focus styles and required indicator.

**Where**: `frontend/src/shared/components/ui/`

**Depends on**: T1
**Reuses**: tokens.css
**Requirement**: UI-02

**Done when**:
- [ ] Input supports `type`, `placeholder`, `disabled`, `error`.
- [ ] Select renders options from a prop array.
- [ ] Label associates with input via `htmlFor` and supports required marker.

**Tests**: none
**Gate**: Build (frontend)
**Commit**: `feat(ui): add Input, Select and Label components`

---

#### T5: Card and Alert components

**What**: Implement `Card` (default/primary/danger variants) and `Alert` (info/success/warning/error).

**Where**: `frontend/src/shared/components/ui/`

**Depends on**: T1
**Reuses**: tokens.css
**Requirement**: UI-02

**Done when**:
- [ ] Card accepts header/body/footer slots and variants.
- [ ] Alert renders icon + message + optional title.

**Tests**: none
**Gate**: Build (frontend)
**Commit**: `feat(ui): add Card and Alert components`

---

#### T6: Modal component

**What**: Implement `Modal` with overlay, close button and action footer.

**Where**: `frontend/src/shared/components/ui/`

**Depends on**: T5
**Reuses**: Card, Button, tokens.css
**Requirement**: UI-02

**Done when**:
- [ ] Modal renders via portal or fixed overlay.
- [ ] Escape key and overlay click close the modal.
- [ ] Focus is trapped inside while open.

**Tests**: none
**Gate**: Build (frontend)
**Commit**: `feat(ui): add Modal component`

---

#### T7: FileUpload component and redesigned LoadingError

**What**: Implement `FileUpload` with dashed drop area and file list; redesenhar `LoadingError` para usar tokens e o novo `Alert`/`Button`.

**Where**:
- `frontend/src/shared/components/ui/FileUpload.tsx`
- `frontend/src/shared/components/LoadingError.tsx`

**Depends on**: T4, T5, T6
**Reuses**: Alert, Button, tokens.css
**Requirement**: UI-02, UI-09

**Done when**:
- [ ] FileUpload supports click-to-select and drag-over visual state.
- [ ] LoadingError uses new Alert and has a retry button.

**Tests**: none
**Gate**: Build (frontend)
**Commit**: `feat(ui): add FileUpload and redesenha LoadingError`

---

### Phase 3: Page redesigns

#### T8: Redesign Stepper

**What**: Refactor `frontend/src/shared/components/Stepper.tsx` to use `StepIndicator` component and tokens; add mobile compact variant.

**Where**: `frontend/src/shared/components/Stepper.tsx`

**Depends on**: T3, T7
**Reuses**: StepIndicator, tokens.css
**Requirement**: UI-03

**Done when**:
- [ ] Stepper uses new visual tokens.
- [ ] Mobile viewport shows compact stepper.
- [ ] Clicking previous completed steps still works.

**Tests**: none
**Gate**: Build (frontend)
**Commit**: `feat(ui): redesenha Stepper com design tokens`

---

#### T9: Redesign Consultation page and CPF-reuse modal

**What**: Refactor `ConsultationPage.tsx` and `CpfReuseModal.tsx` to use new UI components and layout; add disabled submit until checkbox checked.

**Where**: `frontend/src/features/consultation/`

**Depends on**: T3, T4, T6, T8
**Reuses**: Button, Input, Select, Label, Card, Alert, Modal, tokens.css
**Requirement**: UI-04

**Done when**:
- [ ] Consultation form uses new components.
- [ ] Submit button is disabled when `consultationAuthorized` is false.
- [ ] CPF-reuse modal uses new Modal + Card.

**Tests**: none
**Gate**: Build (frontend)
**Commit**: `feat(ui): redesenha etapa de Consulta e modal de CPF`

---

#### T10: Redesign Simulation page

**What**: Refactor `SimulationPage.tsx` to highlight selected simulation and list history in Cards.

**Where**: `frontend/src/features/simulation/`

**Depends on**: T5, T8
**Reuses**: Card, Button, Input, Label, Alert, tokens.css
**Requirement**: UI-05

**Done when**:
- [ ] Selected simulation is visually prominent.
- [ ] History list uses secondary cards.
- [ ] Currency values are formatted with `Intl.NumberFormat`.

**Tests**: none
**Gate**: Build (frontend)
**Commit**: `feat(ui): redesenha etapa de Simulação`

---

#### T11: Redesign Identification page

**What**: Refactor `IdentificationPage.tsx` into grouped sections (personal, address, document) using Card and responsive grid.

**Where**: `frontend/src/features/identification/`

**Depends on**: T4, T5, T8
**Reuses**: Input, Select, Label, Card, Button, Alert, tokens.css
**Requirement**: UI-06

**Done when**:
- [ ] Fields are grouped in visual sections.
- [ ] Responsive grid for side-by-side fields on desktop.
- [ ] Required fields are marked.

**Tests**: none
**Gate**: Build (frontend)
**Commit**: `feat(ui): redesenha etapa de Identificação`

---

#### T12: Redesign Professional/Banking data page

**What**: Refactor `ProfessionalBankingDataPage.tsx` similarly to Identification, with sections for professional and banking data.

**Where**: `frontend/src/features/professionalBankingData/`

**Depends on**: T4, T5, T8
**Reuses**: Input, Select, Label, Card, Button, Alert, tokens.css
**Requirement**: UI-07

**Done when**:
- [ ] Professional and banking data are in separate Cards.
- [ ] Optional `pixKey` is visually marked as optional.

**Tests**: none
**Gate**: Build (frontend)
**Commit**: `feat(ui): redesenha etapa de Dados Profissionais e Bancários`

---

#### T13: Redesign Documents page

**What**: Refactor `DocumentsPage.tsx`, `PersonalDocumentUploadCard.tsx` and `PayslipUploadCard.tsx` to use `FileUpload`; list uploaded files in Cards.

**Where**: `frontend/src/features/documents/`

**Depends on**: T7, T8
**Reuses**: FileUpload, Card, Button, Alert, tokens.css
**Requirement**: UI-08

**Done when**:
- [ ] Upload cards use new FileUpload component.
- [ ] Uploaded files are listed with type, size and remove action.
- [ ] File-type/size errors show in Alert.

**Tests**: none
**Gate**: Build (frontend)
**Commit**: `feat(ui): redesenha etapa de Anexos`

---

### Phase 4: Responsiveness, accessibility & polish

#### T14: Redesign Confirmation page

**What**: Refactor `ConfirmationPage.tsx` to show summary in Cards, pending errors in Alert and action buttons with clear hierarchy.

**Where**: `frontend/src/features/confirmation/`

**Depends on**: T5, T8
**Reuses**: Card, Alert, Button, tokens.css
**Requirement**: UI-09

**Done when**:
- [ ] Summary is divided into Cards per section.
- [ ] Pending requirements render as Alert error list.
- [ ] Success state shows registrationId in Alert success.

**Tests**: none
**Gate**: Build (frontend)
**Commit**: `feat(ui): redesenha etapa de Confirmação`

---

#### T15: Responsive and accessibility pass

**What**: Add media queries for stepper and form grids; verify labels, focus states and touch targets across all pages.

**Where**: All `*.module.css` and shared components

**Depends on**: T8–T14
**Reuses**: tokens.css
**Requirement**: UI-10, UI-11, UI-12

**Done when**:
- [ ] Layout works at 360 px and 1440 px viewports.
- [ ] All inputs have associated labels.
- [ ] Focus rings are visible.
- [ ] No horizontal scroll on mobile.

**Tests**: none
**Gate**: Build (frontend) + visual smoke test
**Commit**: `feat(ui): responsiveness and accessibility pass`

---

## Phase Execution Map

- **Phase 1** (Foundation): T1, T2
- **Phase 2** (Components): T3, T4, T5, T6, T7
- **Phase 3** (Pages): T8, T9, T10, T11, T12, T13, T14
- **Phase 4** (Polish): T15

Total: 15 tasks.

---

## Task Granularity Check

| Task | Scope | Status |
| --- | --- | --- |
| T1 | Tokens + global styles | ✅ Granular |
| T2 | Typography helpers | ✅ Granular |
| T3 | Button | ✅ Granular |
| T4 | Input/Select/Label | ✅ Granular |
| T5 | Card/Alert | ✅ Granular |
| T6 | Modal | ✅ Granular |
| T7 | FileUpload + LoadingError | ✅ Granular |
| T8 | Stepper | ✅ Granular |
| T9 | Consultation page + modal | ✅ Granular |
| T10 | Simulation page | ✅ Granular |
| T11 | Identification page | ✅ Granular |
| T12 | Professional/banking page | ✅ Granular |
| T13 | Documents page | ✅ Granular |
| T14 | Confirmation page | ✅ Granular |
| T15 | Responsive/a11y pass | ✅ Granular |
