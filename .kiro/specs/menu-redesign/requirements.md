# Requirements Document

## Introduction

Redesenho do menu de navegação do site institucional Mater Domus (Blazor WebAssembly). O objetivo é elevar a qualidade visual e a usabilidade da navegação sem alterar a identidade monocromática e minimalista do projeto. As melhorias incluem: indicador visual do item ativo com pill/badge de fundo, comportamento inteligente do header no desktop (esconder ao rolar para baixo, reaparecer ao rolar para cima) e substituição da barra de navegação horizontal por uma barra inferior fixa em dispositivos móveis. Todo o estilo deve ser implementado em CSS puro, sem frameworks externos, seguindo a paleta já estabelecida.

---

## Glossary

- **Header**: Elemento `<header class="header">` em `MainLayout.razor`, contém logo e navegação desktop.
- **NavBar**: Componente de navegação principal contendo os 5 links de página (Início, Produtos, Sobre, Fornecedores, Contato).
- **ActiveLink**: NavLink do Blazor com a classe CSS `active` aplicada pela rota atual.
- **ActivePill**: Indicador visual do link ativo — elemento com fundo preenchido e bordas arredondadas (pill/badge).
- **BottomNav**: Barra de navegação fixa na parte inferior da tela, exibida exclusivamente em viewports móveis (≤ 768 px).
- **ScrollBehavior**: Lógica de ocultação e reaparição do Header com base na direção de rolagem da página.
- **CSSTransition**: Animação suave implementada via propriedade `transition` do CSS.
- **DesktopNav**: Navegação horizontal exibida no Header, visível apenas em viewports > 768 px.
- **System**: A aplicação Blazor WebAssembly MaterDomus (conjunto de componentes `MainLayout.razor` + arquivos CSS).

---

## Requirements

### Requirement 1 — Indicador visual do item ativo (ActivePill)

**User Story:** Como visitante do site, quero ver claramente qual página está ativa na navegação, para que eu possa me orientar dentro do site sem esforço.

#### Acceptance Criteria

1. WHEN o Blazor atribui a classe `active` a um NavLink, THE System SHALL aplicar ao link ativo um estilo de pill com fundo `#1f1f1f`, cor de texto `#ffffff` e `border-radius` de 20 px.
2. WHILE um NavLink possui a classe `active`, THE System SHALL manter o padding horizontal de 12 px e padding vertical de 6 px ao redor do texto do link ativo.
3. WHEN o usuário navega para outra página, THE System SHALL remover o estilo ActivePill do link anterior e aplicá-lo ao novo link ativo.
4. THE System SHALL aplicar a transição CSS `background-color 0.2s ease` e `color 0.2s ease` em todos os NavLinks da DesktopNav para garantir mudança de estado suave.
5. THE System SHALL preservar a cor de texto `#222` e ausência de fundo nos NavLinks inativos.

---

### Requirement 2 — Header inteligente no desktop (ScrollBehavior)

**User Story:** Como visitante do site em desktop, quero que o cabeçalho se oculte ao rolar a página para baixo e reapareça ao rolar para cima, para que o conteúdo principal ocupe mais espaço visual durante a leitura.

#### Acceptance Criteria

1. WHEN o usuário rola a página para baixo em mais de 10 px a partir da posição atual, THE System SHALL mover o Header para fora da viewport usando `transform: translateY(-100%)`.
2. WHEN o usuário rola a página para cima, THE System SHALL reposicionar o Header na viewport usando `transform: translateY(0)`.
3. THE System SHALL aplicar `transition: transform 0.3s ease` ao Header para que a ocultação e o reaparecimento sejam animados.
4. THE System SHALL manter o Header com `position: sticky` e `top: 0` durante toda a navegação desktop para que o scroll behavior funcione corretamente.
5. IF o usuário está no topo da página (scrollY ≤ 10 px), THEN THE System SHALL manter o Header sempre visível, independentemente da direção de rolagem anterior.
6. WHERE a viewport tem largura ≤ 768 px, THE System SHALL desabilitar o ScrollBehavior e manter o Header estático visível sem ocultar via scroll.

---

### Requirement 3 — Barra de navegação inferior móvel (BottomNav)

**User Story:** Como visitante do site em dispositivo móvel, quero uma barra de navegação fixa na parte inferior da tela com os 5 destinos principais, para que eu possa navegar facilmente com o polegar sem precisar de menu hamburguer.

#### Acceptance Criteria

1. WHERE a viewport tem largura ≤ 768 px, THE System SHALL exibir a BottomNav como uma barra fixa na parte inferior da tela com `position: fixed`, `bottom: 0`, `left: 0`, `right: 0`.
2. THE System SHALL distribuir os 5 itens de navegação da BottomNav com `display: flex` e `justify-content: space-around` para ocupar igualmente a largura da tela.
3. THE System SHALL renderizar cada item da BottomNav com ícone SVG inline acima do rótulo de texto, com tamanho de ícone de 20 px × 20 px.
4. WHEN o Blazor atribui a classe `active` a um item da BottomNav, THE System SHALL aplicar a cor `#1f1f1f` ao ícone e ao rótulo do item ativo.
5. WHILE um item da BottomNav está inativo, THE System SHALL exibir o ícone e o rótulo na cor `#888`.
6. THE System SHALL aplicar `background: #ffffff`, `border-top: 1px solid #eaeaea` e `padding: 8px 0` à BottomNav para manter a identidade visual do projeto.
7. WHERE a viewport tem largura ≤ 768 px, THE System SHALL ocultar a DesktopNav (nav do Header) via `display: none` para evitar duplicação de navegação.
8. THE System SHALL adicionar `padding-bottom` equivalente à altura da BottomNav (aproximadamente 64 px) ao elemento `<main>` em viewports ≤ 768 px para que o conteúdo não fique encoberto pela barra fixa.
9. THE System SHALL aplicar `transition: color 0.15s ease` aos itens da BottomNav para mudança de estado suave.

---

### Requirement 4 — Acessibilidade da navegação

**User Story:** Como usuário com necessidades de acessibilidade, quero que a navegação seja operável por teclado e compatível com leitores de tela, para que eu possa usar o site com tecnologias assistivas.

#### Acceptance Criteria

1. THE System SHALL adicionar o atributo `aria-label="Navegação principal"` ao elemento `<nav>` da DesktopNav.
2. THE System SHALL adicionar o atributo `aria-label="Navegação principal"` ao elemento `<nav>` da BottomNav.
3. WHEN um NavLink possui a classe `active`, THE System SHALL garantir que o atributo `aria-current="page"` esteja presente no elemento de âncora correspondente.
4. THE System SHALL garantir que todos os itens de navegação sejam focalizáveis via tecla Tab e ativáveis via tecla Enter ou Espaço.
5. IF um ícone SVG da BottomNav for puramente decorativo, THEN THE System SHALL adicionar `aria-hidden="true"` ao elemento SVG e manter o rótulo de texto visível para leitores de tela.
6. THE System SHALL garantir contraste mínimo de 4,5:1 entre o texto do link ativo (branco `#ffffff` sobre fundo `#1f1f1f`) conforme WCAG 2.1 AA.
7. THE System SHALL garantir contraste mínimo de 4,5:1 entre o texto dos links inativos (`#222` sobre fundo `#ffffff`) conforme WCAG 2.1 AA.

---

### Requirement 5 — Preservação da identidade visual

**User Story:** Como proprietário do produto, quero que o redesenho do menu respeite a identidade visual monocromática e minimalista atual do site, para que a experiência de marca permaneça coerente.

#### Acceptance Criteria

1. THE System SHALL utilizar exclusivamente as cores já definidas no projeto: `#1f1f1f`, `#222`, `#444`, `#666`, `#888`, `#eaeaea` e `#ffffff`.
2. THE System SHALL implementar todos os estilos do menu redesenhado em CSS puro, sem adicionar dependências de frameworks CSS ou bibliotecas de UI externas.
3. THE System SHALL manter a tipografia existente (família `system-ui, sans-serif`) em todos os elementos de navegação.
4. THE System SHALL preservar o logo e o layout geral do Header (`display: flex`, `justify-content: space-between`) sem alterações estruturais além das exigidas pelo redesenho.
5. WHEN o Header está visível, THE System SHALL manter `background: #ffffff` e `border-bottom: 1px solid #eaeaea` conforme definido no CSS atual.
