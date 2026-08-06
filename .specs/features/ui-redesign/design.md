# UI Redesign Design

**Spec**: `.specs/features/ui-redesign/spec.md`
**Status**: Approved

---

## Design Overview

O redesign introduz um **sistema de design leve** baseado em CSS custom properties e CSS Modules. A identidade visual busca transmitir confiança, segurança e clareza — qualidades esperadas de uma instituição financeira — usando verde como cor primária, com tons equilibrados e neutros frios (*slate*) para textos e fundos.

### Pilares

1. **Clareza visual**: hierarquia tipográfica forte, espaçamento generoso, separação entre seções.
2. **Confiança**: cor verde sólida, formulários organizados, feedback de erro amigável.
3. **Consistência**: todos os componentes derivam dos mesmos tokens.
4. **Responsividade**: layout fluído que funciona de 360 px a 1440 px.
5. **Acessibilidade**: contraste adequado, labels explícitos, foco visível.

---

## Tokens de Design

Arquivo central: `frontend/src/shared/styles/tokens.css`

### Cores

```css
:root {
  /* Primary — green bank identity */
  --color-primary-50: #ecfdf5;
  --color-primary-100: #d1fae5;
  --color-primary-200: #a7f3d0;
  --color-primary-300: #6ee7b7;
  --color-primary-400: #34d399;
  --color-primary-500: #10b981;
  --color-primary-600: #059669;
  --color-primary-700: #047857;
  --color-primary-800: #065f46;
  --color-primary-900: #064e3b;

  /* Neutrals — slate */
  --color-gray-50: #f8fafc;
  --color-gray-100: #f1f5f9;
  --color-gray-200: #e2e8f0;
  --color-gray-300: #cbd5e1;
  --color-gray-400: #94a3b8;
  --color-gray-500: #64748b;
  --color-gray-600: #475569;
  --color-gray-700: #334155;
  --color-gray-800: #1e293b;
  --color-gray-900: #0f172a;

  /* Semantic */
  --color-error-50: #fef2f2;
  --color-error-100: #fee2e2;
  --color-error-500: #ef4444;
  --color-error-700: #b91c1c;
  --color-warning-500: #f59e0b;
  --color-warning-700: #b45309;
  --color-success-500: #22c55e;
  --color-success-700: #15803d;
  --color-info-500: #3b82f6;

  /* Surfaces */
  --color-background: var(--color-gray-50);
  --color-surface: #ffffff;
  --color-border: var(--color-gray-200);
  --color-border-focus: var(--color-primary-500);

  /* Text */
  --color-text-primary: var(--color-gray-900);
  --color-text-secondary: var(--color-gray-500);
  --color-text-on-primary: #ffffff;
  --color-text-error: var(--color-error-700);
}
```

### Tipografia

```css
:root {
  --font-family-base: 'Inter', system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;

  --font-size-xs: 0.75rem;   /* 12px */
  --font-size-sm: 0.875rem;  /* 14px */
  --font-size-base: 1rem;    /* 16px */
  --font-size-lg: 1.125rem;  /* 18px */
  --font-size-xl: 1.25rem;   /* 20px */
  --font-size-2xl: 1.5rem;   /* 24px */
  --font-size-3xl: 2rem;     /* 32px */

  --font-weight-normal: 400;
  --font-weight-medium: 500;
  --font-weight-semibold: 600;
  --font-weight-bold: 700;

  --line-height-tight: 1.25;
  --line-height-normal: 1.5;
  --line-height-relaxed: 1.625;
}
```

### Espaçamento, bordas e sombras

```css
:root {
  --space-xs: 0.25rem;   /* 4px */
  --space-sm: 0.5rem;    /* 8px */
  --space-md: 1rem;      /* 16px */
  --space-lg: 1.5rem;    /* 24px */
  --space-xl: 2rem;      /* 32px */
  --space-2xl: 3rem;     /* 48px */
  --space-3xl: 4rem;     /* 64px */

  --radius-sm: 0.25rem;
  --radius-md: 0.5rem;
  --radius-lg: 0.75rem;
  --radius-xl: 1rem;
  --radius-full: 9999px;

  --shadow-sm: 0 1px 2px 0 rgb(0 0 0 / 0.05);
  --shadow-md: 0 4px 6px -1px rgb(0 0 0 / 0.1), 0 2px 4px -2px rgb(0 0 0 / 0.1);
  --shadow-lg: 0 10px 15px -3px rgb(0 0 0 / 0.1), 0 4px 6px -4px rgb(0 0 0 / 0.1);
}
```

---

## Componentes

Localização: `frontend/src/shared/components/ui/`

### Button

- Variantes: `primary` (verde 700, texto branco), `secondary` (fundo branco, borda cinza, texto cinza 700), `danger` (vermelho), `ghost` (sem fundo).
- Estados: `disabled` (opacidade reduzida, cursor not-allowed), `loading` (spinner inline, desabilita clique).
- Tamanhos: `sm`, `md`, `lg`.
- Touch target mínimo: 44×44 px.

### Input / Select

- Fundo branco, borda cinza 200, raio md.
- Foco: anel verde 500, borda verde 500.
- Estado de erro: borda vermelha 500, texto de erro abaixo.
- Label obrigatória acima, com indicador de campo obrigatório.
- Placeholder em cinza 400.

### Label

- Fonte sm, peso medium, cor cinza 700.
- Suporta indicador de obrigatoriedade (`*` vermelho).

### Card

- Fundo branco, borda cinza 200, raio lg, shadow-sm.
- Padding padrão `var(--space-lg)`.
- Variantes: `default`, `primary` (borda esquerda verde 500), `danger` (borda esquerda vermelha 500).

### Alert

- Variantes: `info`, `success`, `warning`, `error`.
- Ícone inline simples + título opcional + mensagem.
- Raio md, padding md.

### Modal

- Overlay escuro semi-transparente.
- Card centralizado com max-width e animação fade-in.
- Botões de ação no footer.

### FileUpload

- Área tracejada (dashed border) com ícone de upload.
- Estados: `idle`, `dragover`, `uploading`, `error`.
- Lista de arquivos abaixo com nome/tamanho e botão remover.

### StepIndicator / Stepper

- Cada passo: círculo com número/ícone + label + conector.
- Cores por estado:
  - `pending`: círculo cinza 300, texto cinza 500.
  - `current`: círculo verde 700, texto verde 800, anel de foco.
  - `completed`: círculo verde 600 com check, texto verde 700.
  - `disabled`: opacidade reduzida, sem clique.
- Mobile: versão compacta (etapa atual + texto "Passo X de 6").

---

## Estrutura de Arquivos

```
frontend/src/
├── main.tsx                              # importa './shared/styles/tokens.css'
├── App.tsx                               # layout shell com container e stepper
├── features/
│   ├── consultation/
│   │   ├── ConsultationPage.tsx
│   │   ├── ConsultationPage.module.css
│   │   └── CpfReuseModal.tsx
│   ├── simulation/
│   │   ├── SimulationPage.tsx
│   │   └── SimulationPage.module.css
│   ├── identification/
│   │   ├── IdentificationPage.tsx
│   │   └── IdentificationPage.module.css
│   ├── professionalBankingData/
│   │   ├── ProfessionalBankingDataPage.tsx
│   │   └── ProfessionalBankingDataPage.module.css
│   ├── documents/
│   │   ├── DocumentsPage.tsx
│   │   ├── PersonalDocumentUploadCard.tsx
│   │   ├── PayslipUploadCard.tsx
│   │   └── DocumentsPage.module.css
│   └── confirmation/
│       ├── ConfirmationPage.tsx
│       └── ConfirmationPage.module.css
├── shared/
│   ├── api/
│   │   └── httpClient.ts
│   ├── components/
│   │   ├── ui/                           # design-system components
│   │   │   ├── Button.tsx
│   │   │   ├── Button.module.css
│   │   │   ├── Input.tsx
│   │   │   ├── Input.module.css
│   │   │   ├── Select.tsx
│   │   │   ├── Label.tsx
│   │   │   ├── Card.tsx
│   │   │   ├── Alert.tsx
│   │   │   ├── Modal.tsx
│   │   │   ├── FileUpload.tsx
│   │   │   └── StepIndicator.tsx
│   │   ├── LoadingError.tsx              # redesenhado
│   │   └── Stepper.tsx                   # redesenhado
│   ├── styles/
│   │   ├── tokens.css
│   │   └── global.css                    # resets leves, box-sizing, fonte base
│   └── leadContext.tsx
```

---

## Tech Decisions

| Decision | Choice | Rationale |
| --- | --- | --- |
| Estilização | CSS Modules + CSS custom properties | Escopo local, sem dependência nova, fácil manter e documentar |
| Reset CSS | `global.css` leve apenas | Evita resets agressivos que quebrem inputs nativos |
| Fonte | System font stack (`Inter` first fallback) | Sem requisição externa, carregamento rápido |
| Ícones | SVG inline | Sem biblioteca extra; substituível por `lucide-react` se escala |
| Responsividade | CSS media queries + flex/grid | Nativo, sem framework |
| Animações | CSS transitions apenas | Leve, sem dependências |

---

## Risks & Concerns

| Concern | Impact | Mitigation |
| --- | --- | --- |
| Refatorar 6 páginas pode introduzir regressões de comportamento | Médio | Manter lógica de estado/API inalterada; alterar apenas JSX/estilo; build como gate |
| CSS Modules podem gerar classes duplicadas se tokens não forem usados | Baixo | Revisão visual + lint de valores hardcoded |
| Tempo de implementação pode crescer se o design system ficar grande | Médio | Limitar a 9 componentes de UI; não implementar dark mode/animações complexas |

---

## Diagram-Definition Cross-Check

Não há dependências complexas entre páginas além do `leadContext` existente. A ordem de implementação sugerida:

1. Tokens + global styles
2. Componentes de UI (sem dependência de backend)
3. Stepper + LoadingError
4. Páginas em ordem do wizard (1 → 6)
5. Responsividade final e ajustes de acessibilidade
