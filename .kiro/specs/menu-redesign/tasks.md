# Implementation Plan: menu-redesign

## Overview

Implementação do redesenho do menu de navegação do site Mater Domus em três etapas incrementais: estilos CSS (ActivePill + ScrollBehavior + BottomNav), comportamento de scroll em JavaScript e atualização dos componentes Blazor (`MainLayout.razor` + novo `BottomNav.razor`).

## Tasks

- [x] 1. Atualizar `site.css` com os novos estilos de navegação
  - [x] 1.1 Adicionar estilos ActivePill à DesktopNav
    - Modificar a regra `nav a` existente adicionando `padding: 6px 12px`, `border-radius: 20px`, `color: #222`, `text-decoration: none` e `transition: background-color 0.2s ease, color 0.2s ease`
    - Modificar a regra `nav a.active` existente substituindo por `background-color: #1f1f1f`, `color: #ffffff`, `font-weight: 600`
    - _Requirements: 1.1, 1.2, 1.4, 1.5, 5.1, 5.2_

  - [x] 1.2 Adicionar estilos ScrollBehavior ao `.header`
    - Atualizar a regra `.header` com `position: sticky`, `top: 0`, `z-index: 100` e `transition: transform 0.3s ease`
    - Adicionar nova regra `.header--hidden { transform: translateY(-100%); }`
    - _Requirements: 2.3, 2.4, 5.5_

  - [x] 1.3 Adicionar estilos da BottomNav e media queries mobile
    - Adicionar regra `.bottom-nav { display: none; }` para ocultar por padrão em desktop
    - Dentro de `@media (max-width: 768px)`: exibir `.bottom-nav` com `display: flex`, `justify-content: space-around`, `position: fixed`, `bottom: 0`, `left: 0`, `right: 0`, `background: #ffffff`, `border-top: 1px solid #eaeaea`, `padding: 8px 0`, `z-index: 200`
    - Dentro do mesmo media query: estilizar `.bottom-nav a` com `flex-direction: column`, `color: #888`, `font-size: 0.7rem`, `transition: color 0.15s ease`; estilizar `.bottom-nav a.active` com `color: #1f1f1f`
    - Dentro do mesmo media query: ocultar `.header nav { display: none; }` e adicionar `main { padding-bottom: 64px; }`
    - _Requirements: 3.1, 3.2, 3.4, 3.5, 3.6, 3.7, 3.8, 3.9, 5.1, 5.2_

  - [x] 1.4 Escrever testes unitários para verificar regras CSS críticas
    - Verificar que `.bottom-nav` tem `display: none` fora do media query
    - Verificar que `nav a.active` contém `background-color: #1f1f1f` e `color: #ffffff`
    - _Requirements: 1.1, 3.7_

- [ ] 2. Criar `wwwroot/js/scrollBehavior.js`
  - [x] 2.1 Implementar o módulo ES6 `scrollBehavior.js`
    - Criar arquivo em `wwwroot/js/scrollBehavior.js`
    - Implementar `initScrollBehavior(headerId)`: busca o elemento por ID, registra listener passivo de scroll, aplica/remove classe `header--hidden` conforme direção de scroll e limiar de 10 px, desabilita comportamento em viewport ≤ 768 px
    - Implementar `destroyScrollBehavior()`: remove o listener e limpa referências
    - _Requirements: 2.1, 2.2, 2.3, 2.5, 2.6_

  - [-] 2.2 Escrever teste de propriedade — Property 1: Scroll para baixo oculta o header
    - **Property 1: Scroll para baixo oculta o header**
    - Simular sequência de scrollY crescente (delta > 10, scrollY > 10) e verificar que `header--hidden` é adicionada
    - **Validates: Requirements 2.1**

  - [-] 2.3 Escrever teste de propriedade — Property 2: Scroll para cima exibe o header
    - **Property 2: Scroll para cima exibe o header**
    - Simular scrollY decrescente e verificar que `header--hidden` é removida
    - **Validates: Requirements 2.2**

  - [-] 2.4 Escrever teste de propriedade — Property 3: Topo da página mantém header visível
    - **Property 3: Topo da página mantém header sempre visível**
    - Para qualquer evento de scroll com `scrollY ≤ 10`, verificar que `header--hidden` nunca é aplicada
    - **Validates: Requirements 2.5**

- [~] 3. Checkpoint — Verificar CSS e JS
  - Garantir que os arquivos `site.css` e `scrollBehavior.js` compilam/carregam sem erros; executar testes disponíveis.

- [ ] 4. Criar o componente `Shared/BottomNav.razor`
  - [x] 4.1 Implementar `BottomNav.razor` com NavLinks e ícones SVG
    - Criar arquivo `Shared/BottomNav.razor`
    - Renderizar elemento `<nav class="bottom-nav" aria-label="Navegação principal">` com 5 `NavLink` (Início, Produtos, Sobre, Fornecedores, Contato)
    - Cada NavLink contém um SVG inline com `aria-hidden="true"`, `width="20"`, `height="20"` e um `<span>` com o rótulo de texto visível
    - Usar os paths SVG definidos no design: casa (Início), monitor (Produtos), info-circle (Sobre), pessoas (Fornecedores), envelope (Contato)
    - _Requirements: 3.3, 4.2, 4.5_

  - [-] 4.2 Escrever teste de propriedade — Property 4: estrutura SVG + label da BottomNav
    - **Property 4: BottomNav items têm estrutura SVG + label**
    - Para cada um dos 5 itens, verificar presença de SVG com `aria-hidden="true"` e dimensões 20×20, e de `<span>` com rótulo textual
    - **Validates: Requirements 3.3, 4.5**

  - [-] 4.3 Escrever testes unitários para `BottomNav.razor`
    - Usar bUnit para renderizar o componente e verificar que todos os 5 NavLinks são renderizados
    - Verificar atributo `aria-label="Navegação principal"` no `<nav>`
    - _Requirements: 3.2, 4.2_

- [ ] 5. Atualizar `Shared/MainLayout.razor`
  - [~] 5.1 Adicionar `id`, `aria-label`, JS interop e `<BottomNav />` ao `MainLayout.razor`
    - Adicionar `id="main-header"` ao elemento `<header>`
    - Adicionar `aria-label="Navegação principal"` ao `<nav>` da DesktopNav
    - Injetar `@inject IJSRuntime JS` e implementar `@implements IAsyncDisposable`
    - Implementar `OnAfterRenderAsync` para importar `./js/scrollBehavior.js` e chamar `initScrollBehavior("main-header")` no primeiro render
    - Implementar `DisposeAsync` para invocar `destroyScrollBehavior()` e liberar o módulo JS
    - Inserir `<BottomNav />` logo após o `</header>`
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 4.1, 4.3_

  - [~] 5.2 Escrever testes de integração para `MainLayout.razor`
    - Verificar que o header possui `id="main-header"` e `<BottomNav />` está presente no DOM
    - Verificar que o `<nav>` do header contém `aria-label="Navegação principal"`
    - _Requirements: 4.1, 4.3_

- [~] 6. Checkpoint final — Garantir que todos os testes passam
  - Executar `dotnet test` para confirmar que todos os testes passam; revisar comportamento visual em viewport desktop e mobile se possível via testes automatizados.

## Notes

- Tarefas marcadas com `*` são opcionais e podem ser puladas para entrega mais rápida
- O design não usa dependências externas — todo CSS é puro e o JS é vanilla ES6
- A propriedade `position: sticky` no header requer atenção a qualquer ancestral com `overflow: hidden` que possa quebrá-la
- O `IJSRuntime` interop só executa no cliente (WebAssembly); `OnAfterRenderAsync` com `firstRender` garante isso
- Os testes existentes em `MaterDomus.Tests` usam xUnit + bUnit; novos testes devem seguir o mesmo padrão

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "1.2", "1.3"] },
    { "id": 1, "tasks": ["1.4", "2.1", "4.1"] },
    { "id": 2, "tasks": ["2.2", "2.3", "2.4", "4.2", "4.3"] },
    { "id": 3, "tasks": ["5.1"] },
    { "id": 4, "tasks": ["5.2"] }
  ]
}
```
