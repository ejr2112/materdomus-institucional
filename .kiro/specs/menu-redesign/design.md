# Design Document — menu-redesign

## Overview

Este documento descreve a arquitetura e as decisões técnicas para o redesenho do menu de navegação do site institucional Mater Domus (Blazor WebAssembly). As mudanças se concentram em três camadas: **estrutura HTML** (`MainLayout.razor` + novo componente `BottomNav.razor`), **estilos CSS** (`wwwroot/css/site.css`) e **comportamento de scroll** (JavaScript interop via arquivo `wwwroot/js/scrollBehavior.js`).

Nenhuma dependência externa será adicionada. Todo o código segue os padrões existentes do projeto: C#/Blazor, CSS puro e JavaScript vanilla.

---

## Architecture

### Visão Geral dos Componentes

```
MainLayout.razor
├── <header class="header">          ← DesktopNav + scroll hide/show
│   └── <nav aria-label="...">       ← NavLinks com ActivePill (CSS)
├── <main>                           ← conteúdo + padding-bottom móvel
├── <BottomNav />                    ← componente móvel separado
└── <footer>
```

### Fluxo de Responsabilidade

| Camada | Responsabilidade |
|---|---|
| `site.css` | ActivePill styles, DesktopNav layout, BottomNav layout, media queries, transições |
| `scrollBehavior.js` | Detecta direção de scroll, adiciona/remove classe `.header--hidden` no header |
| `MainLayout.razor` | Carrega scroll JS via `IJSRuntime`, renderiza header e `<BottomNav />` |
| `BottomNav.razor` | Renderiza 5 NavLinks com ícones SVG inline e rótulos de texto |

---

## Components

### 1. `MainLayout.razor` (modificado)

Responsável por:
- Adicionar `aria-label="Navegação principal"` ao `<nav>` do header
- Injetar `IJSRuntime` e invocar `scrollBehavior.js` via `OnAfterRenderAsync`
- Registrar e liberar o listener de scroll via `IAsyncDisposable`
- Renderizar `<BottomNav />` abaixo do `<header>`

```csharp
@inherits LayoutComponentBase
@using Microsoft.AspNetCore.Components.Routing
@inject IJSRuntime JS
@implements IAsyncDisposable

<PageViewTracker />

<header class="header" id="main-header">
    <div class="container">
        <div class="logo">
            <img src="images/logo.png" alt="Mater Domus" />
        </div>
        <nav aria-label="Navegação principal">
            <NavLink href="/" Match="NavLinkMatch.All">Início</NavLink>
            <NavLink href="/produtos">Produtos</NavLink>
            <NavLink href="/sobre">Sobre</NavLink>
            <NavLink href="/fornecedores">Fornecedores</NavLink>
            <NavLink href="/contato">Contato</NavLink>
        </nav>
    </div>
</header>

<BottomNav />

<main>
    @Body
</main>

<footer class="footer">
    <p>© @DateTime.Now.Year Mater Domus</p>
</footer>

@code {
    private IJSObjectReference? _scrollModule;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _scrollModule = await JS.InvokeAsync<IJSObjectReference>(
                "import", "./js/scrollBehavior.js");
            await _scrollModule.InvokeVoidAsync("initScrollBehavior", "main-header");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_scrollModule is not null)
        {
            await _scrollModule.InvokeVoidAsync("destroyScrollBehavior");
            await _scrollModule.DisposeAsync();
        }
    }
}
```

### 2. `BottomNav.razor` (novo componente)

Renderiza a barra inferior móvel com 5 NavLinks, cada um contendo um ícone SVG inline (`aria-hidden="true"`) e um rótulo de texto visível.

Localização: `Shared/BottomNav.razor`

```html
<nav class="bottom-nav" aria-label="Navegação principal">
    <NavLink href="/" Match="NavLinkMatch.All">
        <svg aria-hidden="true" width="20" height="20" viewBox="0 0 24 24" fill="none"
             stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/>
            <polyline points="9 22 9 12 15 12 15 22"/>
        </svg>
        <span>Início</span>
    </NavLink>
    <NavLink href="/produtos">
        <svg aria-hidden="true" width="20" height="20" viewBox="0 0 24 24" fill="none"
             stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <rect x="2" y="3" width="20" height="14" rx="2" ry="2"/>
            <line x1="8" y1="21" x2="16" y2="21"/>
            <line x1="12" y1="17" x2="12" y2="21"/>
        </svg>
        <span>Produtos</span>
    </NavLink>
    <NavLink href="/sobre">
        <svg aria-hidden="true" width="20" height="20" viewBox="0 0 24 24" fill="none"
             stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <circle cx="12" cy="12" r="10"/>
            <line x1="12" y1="8" x2="12" y2="12"/>
            <line x1="12" y1="16" x2="12.01" y2="16"/>
        </svg>
        <span>Sobre</span>
    </NavLink>
    <NavLink href="/fornecedores">
        <svg aria-hidden="true" width="20" height="20" viewBox="0 0 24 24" fill="none"
             stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/>
            <circle cx="9" cy="7" r="4"/>
            <path d="M23 21v-2a4 4 0 0 0-3-3.87"/>
            <path d="M16 3.13a4 4 0 0 1 0 7.75"/>
        </svg>
        <span>Fornecedores</span>
    </NavLink>
    <NavLink href="/contato">
        <svg aria-hidden="true" width="20" height="20" viewBox="0 0 24 24" fill="none"
             stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M4 4h16c1.1 0 2 .9 2 2v12c0 1.1-.9 2-2 2H4c-1.1 0-2-.9-2-2V6c0-1.1.9-2 2-2z"/>
            <polyline points="22,6 12,13 2,6"/>
        </svg>
        <span>Contato</span>
    </NavLink>
</nav>
```

### 3. `scrollBehavior.js` (novo arquivo)

Localização: `wwwroot/js/scrollBehavior.js`

Módulo ES6 exportado para interop com Blazor. Detecta direção de scroll e aplica/remove a classe `header--hidden` no elemento header.

```javascript
// wwwroot/js/scrollBehavior.js

let lastScrollY = 0;
let headerEl = null;
let scrollHandler = null;
const SCROLL_THRESHOLD = 10;
const MOBILE_BREAKPOINT = 768;

export function initScrollBehavior(headerId) {
    headerEl = document.getElementById(headerId);
    if (!headerEl) return;

    lastScrollY = window.scrollY;

    scrollHandler = () => {
        // Disable on mobile
        if (window.innerWidth <= MOBILE_BREAKPOINT) {
            headerEl.classList.remove('header--hidden');
            return;
        }

        const currentScrollY = window.scrollY;

        // Always show at top of page
        if (currentScrollY <= SCROLL_THRESHOLD) {
            headerEl.classList.remove('header--hidden');
            lastScrollY = currentScrollY;
            return;
        }

        const delta = currentScrollY - lastScrollY;

        if (delta > SCROLL_THRESHOLD) {
            // Scrolling down: hide header
            headerEl.classList.add('header--hidden');
        } else if (delta < 0) {
            // Scrolling up: show header
            headerEl.classList.remove('header--hidden');
        }

        lastScrollY = currentScrollY;
    };

    window.addEventListener('scroll', scrollHandler, { passive: true });
}

export function destroyScrollBehavior() {
    if (scrollHandler) {
        window.removeEventListener('scroll', scrollHandler);
        scrollHandler = null;
    }
    headerEl = null;
}
```

---

## Data Models

Este redesenho não introduz novos modelos de dados. Toda a lógica é baseada em:
- **Estado de rota** gerenciado pelo Blazor Router (classe `active` nos `NavLink`)
- **Estado de scroll** gerenciado em memória no módulo JS (`lastScrollY`, `headerEl`)
- **Viewport width** lido diretamente de `window.innerWidth` em tempo de execução

---

## CSS Design

### Estrutura das Mudanças em `site.css`

As alterações são organizadas em quatro blocos:

#### Bloco 1 — ActivePill (DesktopNav)

```css
/* DesktopNav: transição suave em todos os links */
nav a {
    margin-left: 28px;
    font-size: 0.95rem;
    color: #222;
    text-decoration: none;
    padding: 6px 12px;
    border-radius: 20px;
    transition: background-color 0.2s ease, color 0.2s ease;
}

/* ActivePill: link ativo com fundo preenchido */
nav a.active {
    background-color: #1f1f1f;
    color: #ffffff;
    font-weight: 600;
}
```

#### Bloco 2 — ScrollBehavior (Header)

```css
.header {
    padding: 14px 0;
    border-bottom: 1px solid #eaeaea;
    background: #ffffff;
    position: sticky;
    top: 0;
    z-index: 100;
    transition: transform 0.3s ease;
}

.header--hidden {
    transform: translateY(-100%);
}
```

#### Bloco 3 — BottomNav

```css
/* BottomNav: oculto por padrão (desktop) */
.bottom-nav {
    display: none;
}

/* BottomNav: visível em mobile */
@media (max-width: 768px) {
    .bottom-nav {
        display: flex;
        justify-content: space-around;
        align-items: center;
        position: fixed;
        bottom: 0;
        left: 0;
        right: 0;
        background: #ffffff;
        border-top: 1px solid #eaeaea;
        padding: 8px 0;
        z-index: 200;
    }

    .bottom-nav a {
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: 3px;
        color: #888;
        text-decoration: none;
        font-size: 0.7rem;
        transition: color 0.15s ease;
        padding: 4px 8px;
    }

    .bottom-nav a.active {
        color: #1f1f1f;
    }

    .bottom-nav a svg {
        display: block;
    }

    /* Ocultar DesktopNav em mobile */
    .header nav {
        display: none;
    }

    /* Padding para evitar sobreposição com BottomNav */
    main {
        padding-bottom: 64px;
    }
}
```

---

## Error Handling

| Cenário | Comportamento esperado |
|---|---|
| `document.getElementById(headerId)` retorna `null` | `initScrollBehavior` retorna sem registrar listener; header permanece sempre visível |
| `_scrollModule` é `null` no `DisposeAsync` | Verificação de nulidade previne `NullReferenceException` |
| JS não carregado (primeira renderização servidor) | `OnAfterRenderAsync` com `firstRender` garante execução apenas no cliente |
| Viewport redimensionado enquanto header está oculto | O handler de scroll re-verifica `window.innerWidth` a cada evento; em mobile remove a classe `header--hidden` imediatamente |
| Navegação SPA (Blazor router) | `NavLink` do Blazor atualiza a classe `active` automaticamente; CSS responde sem lógica adicional |

---

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system — essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: Scroll para baixo oculta o header

*Para qualquer* sequência de eventos de scroll onde o `scrollY` atual supera o `scrollY` anterior em mais de 10 px e o usuário está além do topo da página (`scrollY > 10`), a função `scrollHandler` SHALL adicionar a classe `header--hidden` ao elemento header.

**Validates: Requirements 2.1**

### Property 2: Scroll para cima exibe o header

*Para qualquer* evento de scroll onde o `scrollY` atual é menor que o `scrollY` anterior, a função `scrollHandler` SHALL remover a classe `header--hidden` do elemento header.

**Validates: Requirements 2.2**

### Property 3: Topo da página mantém header sempre visível

*Para qualquer* evento de scroll com `scrollY ≤ 10`, independentemente da direção anterior, a função `scrollHandler` SHALL remover a classe `header--hidden` (garantindo que o header permaneça visível).

**Validates: Requirements 2.5**

### Property 4: BottomNav items têm estrutura SVG + label

*Para qualquer* item da BottomNav (dos 5 destinos de navegação), o HTML renderizado SHALL conter um elemento SVG com `aria-hidden="true"` e `width="20" height="20"`, seguido de um elemento `<span>` com o rótulo textual do destino.

**Validates: Requirements 3.3, 4.5**
