# Mobile Responsive Menu Fix — Bugfix Design

## Overview

Em viewports móveis (≤ 768px) o site institucional MaterDomus fica sem nenhuma navegação visível. A folha de estilos `wwwroot/css/site.css` já oculta o menu do cabeçalho (`.header nav { display: none; }`) e já prepara toda a estilização da barra inferior (`.bottom-nav`), mas o componente `Shared/BottomNav.razor` — que produz o markup `<nav class="bottom-nav">` — nunca é montado, pois não está referenciado em `Shared/MainLayout.razor`. Sem esse markup no DOM, os estilos `.bottom-nav` não têm conteúdo a exibir e o usuário mobile fica sem qualquer controle de navegação.

A estratégia de correção é mínima e cirúrgica: montar o componente `<BottomNav />` na árvore de layout do `MainLayout.razor`, exatamente o ponto de integração deixado pendente pela tarefa 5.1 do spec `menu-redesign`. Nenhuma alteração de CSS é necessária, porque as regras responsivas já existem e estão corretas — falta apenas o conteúdo que elas estilizam. Nenhuma alteração no `BottomNav.razor` é necessária, porque o componente já está completo e funcional (5 NavLinks para os mesmos destinos, ícones SVG, `aria-label`).

Por ser uma correção aditiva (uma linha de markup), o risco de regressão em desktop é baixo: o `.bottom-nav` permanece `display: none` acima de 768px, portanto o novo componente não é visível nem ocupa espaço em telas maiores.

## Glossary

- **Bug_Condition (C)**: A condição que dispara o bug — a página é renderizada em uma viewport com largura ≤ 768px, onde o CSS oculta a nav do cabeçalho e a única navegação disponível *deveria* ser a `BottomNav`, que está ausente do DOM.
- **Property (P)**: O comportamento desejado sob a condição do bug — a barra `BottomNav` está presente no DOM, fixada na base da tela, com os 5 destinos navegáveis e realce do item ativo.
- **Preservation**: O comportamento em telas > 768px (nav do cabeçalho visível, `.bottom-nav` oculta) e o restante do layout (cabeçalho/logo, `@Body`, rodapé, `PageViewTracker`) que devem permanecer inalterados pela correção.
- **MainLayout**: O componente de layout raiz em `Shared/MainLayout.razor` (herda `LayoutComponentBase`) que compõe cabeçalho, conteúdo (`@Body`) e rodapé para todas as páginas.
- **BottomNav**: O componente em `Shared/BottomNav.razor` que renderiza `<nav class="bottom-nav">` com 5 `NavLink` (Início, Produtos, Sobre, Fornecedores, Contato) e ícones SVG.
- **Breakpoint mobile**: O limiar `@media (max-width: 768px)` definido em `wwwroot/css/site.css` que governa a troca entre nav de cabeçalho e nav inferior.

## Bug Details

### Bug Condition

O bug se manifesta quando a página é renderizada em uma viewport com largura ≤ 768px. Nesse regime, o CSS aplica `.header nav { display: none; }` e ativa `.bottom-nav { display: flex; ... }`. Contudo, como `MainLayout.razor` não renderiza `<BottomNav />`, não existe nenhum elemento com a classe `.bottom-nav` no DOM. O resultado é a ausência total de navegação: a do cabeçalho está oculta por CSS e a inferior não existe.

**Formal Specification:**
```
FUNCTION isBugCondition(input)
  INPUT: input of type RenderContext { viewportWidth: number, layoutTree: DOM }
  OUTPUT: boolean

  RETURN input.viewportWidth <= 768
         AND headerNavIsHidden(input) == TRUE      // .header nav { display: none } aplicado
         AND bottomNavExistsInDom(input) == FALSE   // nenhum elemento .bottom-nav renderizado
END FUNCTION
```

Sob esta condição, o sistema deveria satisfazer a Property: existir um `<nav class="bottom-nav">` visível e fixado na base da tela, com 5 destinos navegáveis.

### Examples

- **Celular acessando a Home (viewport 375px)**: Esperado — barra inferior fixa com 5 ícones/labels; Atual — nenhuma navegação; o topo mostra apenas o logo.
- **Tablet retrato em 768px navegando de Produtos para Contato**: Esperado — toque em "Contato" na barra inferior leva à página e realça o item; Atual — não há item algum a tocar.
- **Celular em 600px**: Esperado — `.logo span` oculto (já correto) e barra inferior presente; Atual — logo reduzido mas ainda sem menu.
- **Edge — exatamente 768px de largura**: Esperado — regime mobile ativo, barra inferior visível (o media query usa `max-width: 768px`, inclusivo); Atual — sem navegação.

## Expected Behavior

### Preservation Requirements

**Unchanged Behaviors:**
- Em viewports > 768px, o menu de navegação do cabeçalho (`.header nav`) SHALL continuar visível com os 5 links e realce do link ativo (Req 3.1).
- Em viewports > 768px, a barra inferior SHALL permanecer oculta via `.bottom-nav { display: none; }` (Req 3.2).
- O cabeçalho com logo, o conteúdo principal (`@Body`) e o rodapé SHALL continuar renderizando sem alteração visual em qualquer viewport (Req 3.3).
- O rastreamento de visualização de página via `PageViewTracker` SHALL continuar funcionando sem regressão em qualquer viewport (Req 3.4).

**Scope:**
Todas as entradas que NÃO satisfazem a condição do bug — isto é, qualquer renderização em viewport > 768px, bem como o cabeçalho, o `@Body`, o rodapé e o `PageViewTracker` em qualquer viewport — devem ficar completamente inalteradas por esta correção. Isto inclui:
- Renderização em desktop (nav de cabeçalho visível, barra inferior oculta).
- Estrutura e estilos do cabeçalho (logo, container) e do rodapé.
- Ordem de montagem e comportamento do `PageViewTracker`.
- Roteamento e realce de item ativo do menu de cabeçalho.

**Nota:** O comportamento correto esperado sob a condição do bug está definido na seção Correctness Properties (Property 1). Esta seção foca no que NÃO deve mudar.

## Hypothesized Root Cause

Com base na análise dos requisitos e da inspeção de `MainLayout.razor`, `BottomNav.razor` e `site.css`, a causa raiz é uma integração faltante, não um defeito lógico:

1. **Componente nunca montado (causa raiz principal)**: `Shared/MainLayout.razor` compõe `<PageViewTracker />`, `<header>`, `<main>@Body</main>` e `<footer>`, mas não inclui `<BottomNav />`. Sem esse elemento, nenhum nó `.bottom-nav` existe no DOM. Esta é a tarefa 5.1 do spec `menu-redesign` deixada incompleta.

2. **CSS oculta a nav do cabeçalho sem alternativa renderizada**: A regra `@media (max-width: 768px) { .header nav { display: none; } }` está correta e intencional, mas depende de que a `BottomNav` exista para substituir a navegação — dependência não satisfeita.

3. **Descartado — defeito no componente BottomNav**: O `BottomNav.razor` está completo (5 `NavLink`, `NavLinkMatch.All` na home, ícones SVG, `aria-label`). Não há indício de defeito interno; ele apenas nunca é instanciado.

4. **Descartado — defeito nas regras de mídia/CSS**: As regras `.bottom-nav` (oculta no desktop, `display: flex` fixa na base em ≤ 768px) e o realce `.bottom-nav a.active` já estão presentes e coerentes. A verificação exploratória confirmará que basta montar o componente.

## Correctness Properties

Property 1: Bug Condition - Navegação inferior presente e funcional em mobile

_For any_ renderização onde a condição do bug se mantém (`isBugCondition` retorna true — viewport ≤ 768px com a nav do cabeçalho oculta), o layout corrigido SHALL renderizar o componente `<BottomNav />` produzindo um `<nav class="bottom-nav">` fixado na base da tela, contendo os 5 destinos (Início, Produtos, Sobre, Fornecedores, Contato) navegáveis, com o item correspondente à rota atual marcado como ativo.

**Validates: Requirements 2.1, 2.2, 2.3**

Property 2: Preservation - Desktop e restante do layout inalterados

_For any_ renderização onde a condição do bug NÃO se mantém (`isBugCondition` retorna false — viewport > 768px, ou os elementos de cabeçalho/`@Body`/rodapé/`PageViewTracker` em qualquer viewport), o layout corrigido SHALL produzir o mesmo resultado que o layout original, preservando: a nav do cabeçalho visível com realce de ativo, a barra inferior oculta (`display: none`), a estrutura visual de cabeçalho/logo/`@Body`/rodapé, e o rastreamento via `PageViewTracker`.

**Validates: Requirements 3.1, 3.2, 3.3, 3.4**

## Fix Implementation

### Changes Required

Assumindo que a análise de causa raiz está correta, a correção é aditiva e restrita a um único arquivo.

**File**: `Shared/MainLayout.razor`

**Function/Componente**: Markup do layout (`MainLayout`, herdando `LayoutComponentBase`)

**Specific Changes**:
1. **Montar o BottomNav na árvore de layout**: Inserir `<BottomNav />` no markup do `MainLayout`, após o `<footer>` (a posição no DOM é indiferente para o comportamento, pois `.bottom-nav` usa `position: fixed`; colocá-lo após o rodapé mantém a ordem lógica de conteúdo e uma ordem de tabulação natural, com a navegação principal ao final).
   - Nenhuma propriedade/parâmetro é necessária: o `BottomNav` não recebe parâmetros.
   - `BottomNav` já está no namespace do projeto e coberto por `_Imports.razor` (mesmo namespace `Shared` dos demais componentes usados), portanto não é necessário `@using` adicional.

2. **Sem alterações em CSS**: `wwwroot/css/site.css` já contém `.bottom-nav { display: none; }` (desktop) e o bloco `@media (max-width: 768px)` que torna a barra visível e oculta `.header nav`. Nada a modificar.

3. **Sem alterações em `BottomNav.razor`**: O componente já renderiza os 5 `NavLink` corretos com ícones e `aria-label`. Nada a modificar.

4. **Sem alterações em `PageViewTracker`, cabeçalho ou rodapé**: Permanecem exatamente como estão; a inserção do `BottomNav` não altera sua ordem de montagem nem seu markup.

**Diff conceitual (MainLayout.razor):**
```razor
<footer class="footer">
    <p>© @DateTime.Now.Year Mater Domus</p>
</footer>

+<BottomNav />
```

## Testing Strategy

### Validation Approach

A estratégia segue duas fases: primeiro, expor contraexemplos que demonstram o bug no código NÃO corrigido (a `BottomNav` está ausente do DOM); depois, verificar que a correção monta a navegação corretamente em mobile e preserva o comportamento em desktop e no restante do layout. O projeto usa bUnit/xUnit (ver `MaterDomus.Tests`, com `BottomNavRenderTests.cs` já existente) para renderizar componentes e inspecionar o markup resultante.

### Exploratory Bug Condition Checking

**Goal**: Expor contraexemplos que demonstram o bug ANTES de implementar a correção. Confirmar ou refutar a análise de causa raiz. Se refutarmos, será necessário re-hipotetizar.

**Test Plan**: Renderizar `MainLayout` (via bUnit, envolvendo um `@Body` de teste) e inspecionar a árvore renderizada em busca de um elemento `nav.bottom-nav`. Executar no código NÃO corrigido para observar a falha (ausência do elemento) e confirmar que a causa raiz é a não-montagem do componente.

**Test Cases**:
1. **BottomNav ausente no MainLayout**: Renderizar `MainLayout` e afirmar a existência de `nav.bottom-nav` (falhará no código não corrigido).
2. **Contagem de destinos na barra inferior**: Afirmar que a barra inferior renderizada contém 5 `NavLink`/âncoras para `/`, `/produtos`, `/sobre`, `/fornecedores`, `/contato` (falhará no código não corrigido, pois o `nav` não existe).
3. **BottomNav renderiza isoladamente**: Renderizar `BottomNav` diretamente e confirmar que produz `nav.bottom-nav` com 5 links — controle que demonstra que o defeito está na montagem em `MainLayout`, não no componente (deve passar mesmo no código não corrigido).

**Expected Counterexamples**:
- Ao renderizar `MainLayout`, nenhum nó `nav.bottom-nav` é encontrado.
- Causa provável confirmada: `<BottomNav />` não referenciado em `MainLayout.razor` (integração faltante), enquanto o componente isolado renderiza corretamente.

### Fix Checking

**Goal**: Verificar que, para todas as entradas onde a condição do bug se mantém, o layout corrigido produz o comportamento esperado.

**Pseudocode:**
```
FOR ALL input WHERE isBugCondition(input) DO
  result := renderMainLayout_fixed(input)
  ASSERT result CONTÉM nav.bottom-nav fixado na base
  ASSERT nav.bottom-nav CONTÉM 5 destinos navegáveis (/, /produtos, /sobre, /fornecedores, /contato)
  ASSERT o item correspondente à rota atual está marcado como ativo
END FOR
```

### Preservation Checking

**Goal**: Verificar que, para todas as entradas onde a condição do bug NÃO se mantém, o layout corrigido produz o mesmo resultado que o layout original.

**Pseudocode:**
```
FOR ALL input WHERE NOT isBugCondition(input) DO
  ASSERT renderMainLayout_original(input) == renderMainLayout_fixed(input)
  // para viewport > 768px: nav do cabeçalho visível, .bottom-nav display:none
  // para qualquer viewport: cabeçalho/logo, @Body, rodapé e PageViewTracker inalterados
END FOR
```

**Testing Approach**: O teste baseado em propriedades é recomendado para a verificação de preservação porque:
- Gera muitos casos automaticamente pelo domínio de entradas (rotas atuais, larguras de viewport simuladas).
- Captura casos de borda que testes unitários manuais poderiam não cobrir (ex.: largura exatamente em 769px).
- Oferece forte garantia de que o comportamento permanece inalterado para todas as entradas não-bugadas.

**Test Plan**: Observar o comportamento no código NÃO corrigido para desktop e para os elementos de layout, depois escrever testes que capturem esse comportamento e confirmem que ele se mantém após a correção.

**Test Cases**:
1. **Preservação da nav do cabeçalho**: Observar que `.header nav` com 5 links é renderizado; após a correção, afirmar que continua presente e inalterado.
2. **Preservação do padrão de ocultação da barra inferior**: Afirmar que a regra `.bottom-nav { display: none; }` (desktop) permanece na folha de estilos e inalterada; a visibilidade em mobile é responsabilidade do media query já existente.
3. **Preservação de cabeçalho/logo, @Body e rodapé**: Afirmar que o markup do cabeçalho (logo), a região `@Body` e o rodapé continuam presentes e na mesma estrutura após a correção.
4. **Preservação do PageViewTracker**: Afirmar que `PageViewTracker` continua sendo renderizado e que a inserção do `BottomNav` não altera sua presença nem ordem.

### Unit Tests

- Verificar que `MainLayout` renderiza `nav.bottom-nav` (fix) além do cabeçalho, `@Body` e rodapé.
- Verificar que `BottomNav` contém exatamente 5 `NavLink` com os `href` corretos e um `aria-label`.
- Verificar que a folha de estilos mantém `.bottom-nav { display: none; }` no escopo desktop e `.header nav { display: none; }` dentro do `@media (max-width: 768px)` (garante que o contrato de responsividade permaneça intacto).

### Property-Based Tests

- Para uma amostra ampla de rotas atuais (`/`, `/produtos`, `/sobre`, `/fornecedores`, `/contato`), verificar que exatamente o item correspondente da `BottomNav` é marcado como ativo e os demais não.
- Gerar larguras de viewport simuladas > 768px e afirmar que o resultado renderizado do `MainLayout` (estrutura de cabeçalho/`@Body`/rodapé) é idêntico ao original — preservação.
- Verificar, em muitas combinações de rota, que a presença e a ordem de montagem de `PageViewTracker`, cabeçalho e rodapé permanecem invariantes.

### Integration Tests

- Fluxo completo em viewport mobile: montar o app, confirmar a barra inferior visível e navegar entre as 5 páginas via toque, validando a troca de rota e o realce do item ativo.
- Alternância de contexto: simular a transição desktop → mobile (nav de cabeçalho → barra inferior) e mobile → desktop, confirmando que exatamente uma navegação está visível em cada regime.
- Feedback visual: confirmar que, ao navegar em mobile, o item ativo da barra inferior recebe o estado `active` e que o `PageViewTracker` registra a visualização sem regressão.
