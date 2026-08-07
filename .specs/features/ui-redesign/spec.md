# UI Redesign Specification

## Problem Statement

O frontend atual do wizard de recuperação de leads é funcional, mas visualmente cru e pouco profissional: estilos inline espalhados, ausência de hierarquia tipográfica, feedback de erro genérico e layout que não transmite a confiança esperada de uma instituição financeira. O sistema precisa de uma identidade visual coesa, com cor verde como carro-chefe, tipografia limpa, componentes consistentes e uma experiência de formulário clara e acessível — sem alterar o contrato de API ou o fluxo de 6 etapas.

## Goals

- [ ] Definir e implementar um sistema de design com paleta verde, tipografia, espaçamento e tokens de borda/sombra.
- [ ] Criar biblioteca interna de componentes de UI reutilizáveis (`Button`, `Input`, `Select`, `Label`, `Card`, `Alert`, `Modal`, `FileUpload`, `StepIndicator`).
- [ ] Redesenhar `Stepper`, `LoadingError` e as 6 páginas do wizard para usarem o novo sistema.
- [ ] Melhorar a UX dos formulários: estados de erro por campo, loading, desabilitação de ações inválidas e mensagens amigáveis.
- [ ] Tornar o layout responsivo (desktop e mobile) e acessível (contraste, labels, foco visível).
- [ ] Manter `npm run build` passando e nenhuma chamada de API alterada.

## Out of Scope

| Feature | Reason |
| --- | --- |
| Mudanças no backend ou contrato de API | Feature é frontend-only |
| Novas páginas/rotas | Wizard já tem as 6 etapas necessárias |
| Dark mode | Diferencial futuro; tokens devem facilitar, mas não é obrigatório |
| Biblioteca de ícones externa pesada | Pode usar SVG inline ou lucide-react leve, mas não é requisito |
| Animações complexas | Apenas transições CSS simples |

---

## Assumptions & Open Questions

| Assumption / decision | Chosen default | Rationale | Confirmed? |
| --- | --- | --- | --- |
| Estratégia de estilo | CSS Modules + CSS custom properties | Sem nova dependência pesada; escopo local por componente; fácil manutenção | y |
| Fonte | Stack `Inter, system-ui, sans-serif` | Tipografia limpa, sem carregar fonte externa | y |
| Cor primária | Verde (`#047857` base) | Cor de banco/confiança; escala completa definida em design.md | y |
| Responsividade | Mobile-first, container fluído | Público pode acessar por celular | y |
| Ícones | SVG inline simples | Evita dependência; substituível depois | y |

**Open questions:** none.

---

## User Stories

### P1-1: Sistema de design tokens ⭐ MVP

**User Story**: Como desenvolvedor, quero tokens centralizados de cores, tipografia e espaçamento, pra manter consistência visual em todas as telas.

**Why P1**: Base de todo o redesign.

**Acceptance Criteria**:

1. WHEN o projeto builda THEN os tokens CSS (`tokens.css`) devem estar importados globalmente e acessíveis em qualquer componente.
2. The system SHALL definir pelo menos: escala de verde (50-900), escala de cinza/slate, cores semânticas (erro, aviso, sucesso), tipografia (tamanhos e pesos), espaçamento (xs a 3xl), raios de borda e sombras.
3. No componente deve usar valores hardcoded de cor/espaçamento — todos via tokens.

**Independent Test**: Abrir DevTools e verificar que `background-color`, `color`, `padding` e `font-size` dos componentes derivam das variáveis CSS.

---

### P1-2: Biblioteca de componentes de UI ⭐ MVP

**User Story**: Como desenvolvedor, quero componentes de UI reutilizáveis e consistentes, pra reconstruir as páginas sem duplicar estilo.

**Why P1**: Garante consistência e velocidade na refatoração das páginas.

**Acceptance Criteria**:

1. The system SHALL fornecer componentes: `Button`, `Input`, `Select`, `Label`, `Card`, `Alert`, `Modal`, `FileUpload`, `StepIndicator`.
2. Each component SHALL aceitar variantes de cor (`primary`, `secondary`, `danger`, `ghost`) e estados (`disabled`, `loading`) quando aplicável.
3. Each component SHALL respeitar os tokens de design e ter foco/hover visíveis.
4. The system SHALL manter os componentes em `frontend/src/shared/components/ui/`.

**Independent Test**: Renderizar cada componente isoladamente em uma página temporária ou story (sem precisar de backend) e verificar estados.

---

### P1-3: Stepper redesenhado ⭐ MVP

**User Story**: Como cliente, quero ver claramente em qual etapa estou e quais já completei, pra não me perder no wizard.

**Why P1**: O stepper é o elemento de navegação principal.

**Acceptance Criteria**:

1. The system SHALL exibir 6 passos com número/ícone, label e conector visual.
2. Estados visuais distintos: `pending`, `current`, `completed`, `disabled`.
3. Em telas pequenas, o stepper pode colapsar para uma versão compacta (apenas etapa atual + indicador de progresso).
4. Clique em etapas anteriores completadas continua permitindo navegação retroativa.

**Independent Test**: Visualmente identificar, em cada etapa, qual passo está ativo e quais estão completos.

---

### P1-4: Página de Consulta (etapa 1) ⭐ MVP

**User Story**: Como cliente, quero um formulário de consulta limpo e com feedback claro, pra começar meu cadastro com confiança.

**Acceptance Criteria**:

1. The system SHALL usar `Input`, `Select`, `Label` e `Button` do novo design system.
2. The system SHALL exibir mensagens de erro de validação do backend próximas aos campos ou em um `Alert` amigável.
3. The system SHALL desabilitar o botão de submit enquanto `consultationAuthorized` não estiver marcado.
4. O modal de reutilização de CPF (`CpfReuseModal`) deve seguir o novo visual (`Modal` + `Card`).

---

### P1-5: Página de Simulação (etapa 2) ⭐ MVP

**User Story**: Como cliente, quero ver a simulação destacada e o histórico organizado, pra comparar opções.

**Acceptance Criteria**:

1. The system SHALL destacar a simulação selecionada em um `Card` primário.
2. The system SHALL listar simulações anteriores em cards secundários com botão para selecionar.
3. The system SHALL exibir valores monetários formatados (R$) com tipografia hierárquica.

---

### P1-6: Páginas de Identificação e Dados Profissionais/Bancários (etapas 3 e 4) ⭐ MVP

**User Story**: Como cliente, quero preencher meus dados pessoais e bancários em um formulário organizado, com labels claras e erros por campo.

**Acceptance Criteria**:

1. The system SHALL agrupar campos em seções visuais (`Card`) — ex.: dados pessoais, endereço, documento.
2. The system SHALL usar grids responsivos para campos lado a lado quando couberem.
3. The system SHALL marcar campos obrigatórios com indicador visual.
4. The system SHALL exibir erro do backend de forma amigável (ex.: "CPF é obrigatório").

---

### P1-7: Página de Anexos (etapa 5) ⭐ MVP

**User Story**: Como cliente, quero enviar documentos com uma interface clara de drag-and-drop e ver o status de cada arquivo, pra não errar no envio.

**Acceptance Criteria**:

1. The system SHALL usar o componente `FileUpload` com área de drop visual, ícone e texto instrutivo.
2. The system SHALL listar documentos enviados com tipo, nome/tamanho e ação de remover.
3. The system SHALL exibir erro de tamanho/tipo de arquivo de forma clara.

---

### P1-8: Página de Confirmação (etapa 6) ⭐ MVP

**User Story**: Como cliente, quero revisar meus dados em um resumo elegante antes de confirmar, com destaque para pendências.

**Acceptance Criteria**:

1. The system SHALL exibir o resumo em seções de `Card` (consulta, simulação, identificação, dados bancários, documentos).
2. The system SHALL destacar pendências em `Alert` vermelho com lista de itens pendentes.
3. The system SHALL mostrar botões de ação primários (Confirmar) e secundários (Tentar novamente) conforme estado.
4. Em caso de sucesso, the system SHALL exibir `registrationId` em um `Alert` de sucesso.

---

### P1-9: Estados de loading e erro globais ⭐ MVP

**User Story**: Como cliente, quero saber quando o sistema está processando e receber mensagens de erro claras, sem travamentos.

**Acceptance Criteria**:

1. The system SHALL exibir spinner/loading em botões e telas usando o componente `LoadingError` redesenhado.
2. The system SHALL formatar mensagens de erro do backend (`detail` + `extensions.reasons`) de forma legível.
3. The system SHALL manter layout estável durante carregamentos.

---

### P2-1: Responsividade mobile

**User Story**: Como cliente acessando pelo celular, quero usar o wizard sem scroll horizontal ou elementos quebrados.

**Acceptance Criteria**:

1. The system SHALL usar layout fluído com `max-width` e padding adaptativo.
2. The system SHALL empilhar campos lado a lado em telas pequenas.
3. The system SHALL garantir touch targets de pelo menos 44×44 px para botões e inputs.

---

### P2-2: Acessibilidade básica

**User Story**: Como cliente que usa leitor de tela, quero navegar pelos formulários e saber o estado do wizard.

**Acceptance Criteria**:

1. The system SHALL associar labels a inputs (`htmlFor`/`id`).
2. The system SHALL usar `aria-live` em mensagens de erro.
3. The system SHALL manter ordem de foco lógica entre campos e botões.

---

## Edge Cases

- IF o cliente reduzir a largura da janela para 360 px THEN o stepper e os formulários não devem quebrar.
- IF houver múltiplas simulações THEN a selecionada deve ser visualmente dominante sem confundir com o histórico.
- IF um campo tiver erro de validação THEN o foco deve ser visível e a mensagem deve estar próxima ao campo.
- IF o botão estiver em loading THEN não deve permitir novo clique e deve exibir indicador de progresso.

---

## Requirement Traceability

| Requirement ID | Story | Phase | Status |
| --- | --- | --- | --- |
| UI-01 | P1-1: Design tokens | Design | Pending |
| UI-02 | P1-2: Component library | Design | Pending |
| UI-03 | P1-3: Stepper | Design | Pending |
| UI-04 | P1-4: Consultation page | Design | Pending |
| UI-05 | P1-5: Simulation page | Design | Pending |
| UI-06 | P1-6: Identification page | Design | Pending |
| UI-07 | P1-6: Professional/banking page | Design | Pending |
| UI-08 | P1-7: Documents page | Design | Pending |
| UI-09 | P1-8: Confirmation page | Design | Pending |
| UI-10 | P1-9: Loading/error states | Design | Pending |
| UI-11 | P2-1: Responsiveness | Design | Pending |
| UI-12 | P2-2: Accessibility | Design | Pending |

---

## Success Criteria

- [ ] Todas as 6 páginas usam os componentes e tokens do novo design system.
- [ ] `npm run build` passa sem erros ou warnings novos.
- [ ] Layout funciona em viewport de 360 px e 1440 px sem quebras visuais.
- [ ] Nenhuma chamada à API foi alterada (rotas, payloads, porta).
- [ ] Estados de erro, loading e sucesso são visualmente claros.
