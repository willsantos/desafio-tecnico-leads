# UI Redesign Context

**Gathered:** 2026-08-06
**Spec:** `.specs/features/ui-redesign/spec.md`
**Status:** Ready for design

---

## Feature Boundary

Redesign visual e de UX do frontend React do wizard de recuperação de leads de empréstimo consignado. O escopo é **frontend-only**: mantém o fluxo de 6 etapas, as chamadas à API e o contrato existente intactos. O foco é entregar uma identidade visual elegante, profissional e com cara de instituição financeira, usando verde como cor primária, com sistema de design consistente, layout responsivo e melhorias de acessibilidade.

---

## Implementation Decisions

### Paleta de cores
- Cor primária do banco: **verde**, em uma escala de 50 a 900.
- Neutros em escala *slate* para textos, bordas e fundos.
- Cores semânticas próprias para erro, aviso e sucesso, alinhadas às mensagens do sistema.

### Abordagem de estilo
- **CSS Modules + design tokens em CSS custom properties** (sem adicionar biblioteca de UI pesada).
- Tokens centralizados em `frontend/src/shared/styles/tokens.css` e importados globalmente em `main.tsx`.
- Cada componente/page recebe seu `.module.css` para manter o escopo local e evitar conflitos.

### Tipografia
- Fonte sans-serif do sistema (stack `Inter, system-ui, sans-serif`), com escala tipográfica definida por tokens.
- Hierarquia clara entre título da aplicação, título de etapa, labels, legendas e mensagens de erro.

### Componentização
- Criar uma pequena biblioteca interna em `frontend/src/shared/components/ui/`:
  `Button`, `Input`, `Select`, `Label`, `Card`, `Alert`, `Modal`, `FileUpload`, `StepIndicator`.
- Redesenhar componentes existentes (`Stepper`, `LoadingError`) para o novo visual.
- Cada página do wizard será refatorada para usar os componentes de UI e os tokens.

### UX / formulários
- Estados de foco, hover, disabled e loading explicitamente definidos.
- Feedback de erro próximo ao campo, usando as razões do Problem Details quando aplicável.
- Botões de ação primários e secundários com hierarquia visual clara.
- Upload de arquivos com drag-and-drop visual simples e indicação de estado.

### Responsividade
- Layout fluído: em telas pequenas o stepper vira uma lista vertical compacta e os formulários ocupam 100% da largura.
- Container centralizado com `max-width`, mas sem quebrar em mobile.

### Acessibilidade
- Contraste mínimo 4.5:1 para textos.
- Labels associados a inputs (`htmlFor` + `id`).
- Estados de foco visíveis e `aria-live` para mensagens de erro.

---

## Specific References

- Produto mantido: fluxo e contrato definidos em `README.md` seção 5 e `.specs/features/lead-recovery/spec.md`.
- Público-alvo: clientes de instituição financeira — precisa transmitir confiança, segurança e clareza.
- Restrição técnica: React 18 + Vite; sem alterar backend ou rotas.

---

## Deferred Ideas

- Dark mode — fora do escopo inicial; tokens são preparados para facilitar futura adição.
- Animações complexas — apenas transições CSS simples (fade/slide) para não adicionar dependências.
- Biblioteca de ícones — usar emojis/Unicode ou SVGs simples inline; se necessário, adicionar uma lib leve (ex.: `lucide-react`) como decisão de implementação.
