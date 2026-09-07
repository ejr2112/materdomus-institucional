# Correção de Bug Mobile — Botões da Hero e Ícones do Menu Inferior

## Overview

O site institucional da Mater Domus (Blazor) apresenta dois defeitos visuais que se manifestam apenas em telas mobile (`max-width: 768px`):

1. **Botões da hero quebrando no mobile:** os links `.btn-primary` ("Seja nosso fornecedor") e `.btn-secondary` ("Conheça a Mater Domus") dentro de `.hero-actions` não têm tratamento responsivo. Como são elementos inline com `padding: 14px 26px` e `margin-right: 10px`, em telas estreitas eles não cabem lado a lado e quebram de forma inconsistente.

2. **Fundo preto no ícone selecionado do menu inferior:** a regra genérica `nav a.active` aplica `background-color: #1f1f1f`. Os links do menu inferior (`.bottom-nav a`) também correspondem ao seletor `nav a`, então ao ficarem ativos herdam esse fundo escuro. A regra `.bottom-nav a.active` altera apenas a cor, sem reverter o fundo, deixando o ícone praticamente invisível.

A estratégia de correção é **mínima e focada em CSS**, alterando apenas `wwwroot/css/site.css`. Nenhuma marcação (`.razor`) precisa ser tocada. As duas correções são isoladas ao escopo mobile e à regra do menu inferior, preservando integralmente o comportamento em desktop.

## Glossary

- **Bug_Condition (C)**: A condição que dispara o bug — para o Bug 1, renderizar `.hero-actions` em viewport mobile; para o Bug 2, um link `.bottom-nav a.active` em viewport mobile.
- **Property (P)**: O comportamento desejado — botões da hero exibidos de forma organizada e legível (Bug 1); ícone selecionado sem fundo escuro e visível (Bug 2).
- **Preservation**: Comportamento existente que NÃO pode mudar — layout dos botões em desktop, aparência do menu de topo ativo (`nav a.active`) em desktop, links inativos do menu inferior, e estilos visuais dos botões (cores, bordas, raio, destinos).
- **`.hero-actions`**: Contêiner em `Pages/Index.razor` que agrupa os dois botões da seção hero.
- **`nav a.active`**: Regra genérica em `wwwroot/css/site.css` que aplica fundo escuro a links de navegação ativos.
- **`.bottom-nav a.active`**: Regra do link ativo do menu inferior mobile, atualmente definindo apenas `color`.
- **F**: renderização original (antes da correção). **F'**: renderização corrigida (após a correção).

## Bug Details

### Bug Condition

O bug se manifesta em dois cenários independentes, ambos restritos a viewport mobile (`viewportWidth <= 768`):

- **Bug 1:** `.hero-actions` é renderizado sem regras de flex/responsividade no bloco `@media (max-width: 768px)`, fazendo os dois botões inline quebrarem incorretamente.
- **Bug 2:** um link do `.bottom-nav` está ativo e herda `background-color: #1f1f1f` da regra `nav a.active`, sem que `.bottom-nav a.active` reverta esse fundo.

**Formal Specification:**
```
FUNCTION isBugCondition(input)
  INPUT: input of type UiRenderContext  // { area, isActive, viewportWidth }
  OUTPUT: boolean

  // Bug 1 — botões da hero em mobile
  bug1 := input.area = "hero-actions"
          AND input.viewportWidth <= 768

  // Bug 2 — ícone ativo do menu inferior em mobile herda fundo escuro
  bug2 := input.area = "bottom-nav"
          AND input.isActive = true
          AND input.viewportWidth <= 768

  RETURN bug1 OR bug2
END FUNCTION
```

### Examples

- **Bug 1:** Em um viewport de 375px, "Seja nosso fornecedor" e "Conheça a Mater Domus" não cabem lado a lado; o segundo botão quebra para uma segunda linha desalinhado. *Esperado:* botões empilhados verticalmente (ou dispostos com espaçamento adequado) e legíveis.
- **Bug 1 (borda):** Em viewport exatamente 768px, os botões ainda devem ser exibidos de forma organizada segundo as regras mobile.
- **Bug 2:** Ao selecionar "Início" no menu inferior em mobile, o ícone recebe fundo preto `#1f1f1f` e fica ilegível. *Esperado:* sem fundo, ícone destacado apenas pela cor `#1f1f1f` do texto/ícone.
- **Não é bug (preservação):** Em desktop (>768px), o item de menu de topo ativo continua com fundo escuro `#1f1f1f` e texto branco — comportamento correto.

## Expected Behavior

### Preservation Requirements

**Unchanged Behaviors:**
- Os botões da hero em desktop (>768px) continuam lado a lado com o espaçamento atual (`margin-right: 10px`).
- O item de navegação de topo ativo (`.header nav a.active`) em desktop continua com fundo escuro `#1f1f1f` e texto branco.
- Os links inativos do menu inferior em mobile continuam na cor cinza `#888`, sem fundo.
- Os estilos visuais dos botões (cores, bordas, `border-radius`, destinos dos links) permanecem inalterados em qualquer viewport.

**Scope:**
Todas as entradas que NÃO sejam `.hero-actions` em mobile (Bug 1) nem um link ativo do `.bottom-nav` em mobile (Bug 2) devem permanecer completamente inalteradas. Isso inclui:
- Navegação de topo em desktop (ativa ou inativa).
- Botões da hero em desktop.
- Links inativos do menu inferior em mobile.
- Qualquer outro elemento ou viewport não coberto pela condição de bug.

**Nota:** O comportamento correto esperado está definido na seção Correctness Properties (Property 1 e Property 2). Esta seção foca no que NÃO deve mudar.

## Hypothesized Root Cause

Com base na descrição do bug, as causas mais prováveis são:

1. **Ausência de regra responsiva para `.hero-actions`**: O bloco `@media (max-width: 768px)` em `wwwroot/css/site.css` não contém nenhuma regra para `.hero-actions`. O contêiner mantém o fluxo inline padrão, e os dois botões com `padding: 14px 26px` não cabem lado a lado em telas estreitas, quebrando de forma inconsistente.

2. **Herança de fundo pelo seletor genérico `nav a.active`**: A regra `nav a.active { background-color: #1f1f1f; }` aplica-se a qualquer `<a class="active">` dentro de um `<nav>`. Como o menu inferior usa `.bottom-nav a`, seus links ativos casam também com `nav a.active` e herdam o fundo escuro.

3. **Regra `.bottom-nav a.active` incompleta**: `.bottom-nav a.active` define apenas `color: #1f1f1f`, sem sobrescrever `background-color`, portanto não reverte o fundo herdado da regra genérica.

4. **Especificidade suficiente para a correção**: `.bottom-nav a.active` tem especificidade maior que `nav a.active`, o que permite reverter o fundo declarando `background-color: transparent` na regra do menu inferior — sem `!important` e sem alterar a regra genérica que serve ao menu de topo.

## Correctness Properties

Property 1: Bug Condition - Layout mobile correto dos botões da hero e ícone visível no menu inferior

_For any_ entrada em que a condição de bug é verdadeira (`isBugCondition` retorna `true`), a renderização corrigida SHALL exibir os botões da hero de forma organizada e legível sem quebra incorreta (Bug 1) e SHALL exibir o ícone ativo do menu inferior sem fundo escuro, mantendo-o visível e destacado apenas pela cor de destaque (Bug 2).

**Validates: Requirements 2.1, 2.2**

Property 2: Preservation - Comportamento inalterado fora da condição de bug

_For any_ entrada em que a condição de bug é falsa (`isBugCondition` retorna `false`), a renderização corrigida SHALL produzir o mesmo resultado da renderização original, preservando o layout dos botões da hero em desktop, o fundo escuro `#1f1f1f` do menu de topo ativo em desktop, os links inativos do menu inferior em cinza `#888` sem fundo, e todos os estilos visuais dos botões.

**Validates: Requirements 3.1, 3.2, 3.3, 3.4**

## Fix Implementation

### Changes Required

Assumindo que nossa análise de causa raiz está correta, todas as alterações ficam em um único arquivo CSS. Nenhuma marcação Razor é modificada.

**File**: `wwwroot/css/site.css`

**Alvos**: bloco `@media (max-width: 768px)` (hero) e regra `.bottom-nav a.active` (menu inferior)

**Specific Changes**:

1. **Bug 1 — Layout responsivo dos botões da hero**: Adicionar, dentro do bloco `@media (max-width: 768px)` já existente, uma regra para `.hero-actions` que estabeleça um layout controlado em mobile:
   - `display: flex;`
   - `flex-direction: column;` (empilhar os botões verticalmente)
   - `align-items: center;`
   - `gap: 12px;` (espaçamento consistente entre os botões)
   - Opcionalmente `.hero-actions .btn-primary { margin-right: 0; }` dentro do mesmo bloco mobile, para neutralizar o `margin-right: 10px` que só faz sentido no layout lado a lado do desktop. O `gap` passa a controlar o espaçamento em mobile.

2. **Bug 2 — Reverter fundo do ícone ativo do menu inferior**: Na regra existente `.bottom-nav a.active` (dentro do bloco `@media (max-width: 768px)`), adicionar `background-color: transparent;`, mantendo a cor de destaque `color: #1f1f1f;`. A especificidade de `.bottom-nav a.active` (0,3,0) é maior que a de `nav a.active` (0,2,0), garantindo que o fundo transparente prevaleça sem `!important`.

3. **Não alterar a regra genérica `nav a.active`**: Ela permanece intacta para preservar o fundo escuro do menu de topo em desktop (Requisito 3.2).

4. **Não alterar `Pages/Index.razor`**: A marcação dos botões e seus destinos (`/fornecedores`, `/sobre`) permanecem inalterados (Requisito 3.4).

5. **Escopo restrito ao mobile**: As regras de `.hero-actions` ficam dentro do `@media (max-width: 768px)`; fora dele, `.hero-actions` mantém apenas `margin-top: 30px`, preservando o desktop (Requisito 3.1).

## Testing Strategy

### Validation Approach

A estratégia de testes segue duas fases: primeiro, expor contraexemplos que demonstrem os bugs no código não corrigido; depois, verificar que a correção funciona e que o comportamento existente é preservado. Como as correções são puramente de CSS em uma aplicação Blazor, os testes se apoiam nos testes de renderização/unidade já presentes no projeto (`MaterDomus.Tests`), complementados por verificação visual manual em viewport mobile.

### Exploratory Bug Condition Checking

**Goal**: Expor contraexemplos que demonstrem os bugs ANTES de implementar a correção. Confirmar ou refutar a análise de causa raiz. Se refutada, será necessário re-hipotetizar.

**Test Plan**: Inspecionar o CSS gerado e a renderização em viewport mobile (por exemplo, 375px). Verificar a ausência de regra responsiva para `.hero-actions` e a presença do fundo escuro herdado no `.bottom-nav a.active`. Rodar sobre o código NÃO corrigido para observar as falhas.

**Test Cases**:
1. **Hero em mobile (375px)**: Renderizar a home e observar que `.hero-actions` não tem regra flex no `@media (max-width: 768px)`, resultando em botões quebrados (falha no código não corrigido).
2. **Ícone ativo do menu inferior (375px)**: Ativar um item do `.bottom-nav` e observar `background-color: #1f1f1f` herdado de `nav a.active` (falha no código não corrigido).
3. **Borda de 768px**: Verificar o comportamento dos botões da hero exatamente em 768px (pode falhar no código não corrigido).

**Expected Counterexamples**:
- `.hero-actions` sem `display: flex`/`flex-direction` em mobile, com botões desalinhados.
- `.bottom-nav a.active` com fundo `#1f1f1f` computado, tornando o ícone ilegível.
- Causas prováveis: ausência de regra responsiva para `.hero-actions`; herança do fundo pela regra genérica `nav a.active`.

### Fix Checking

**Goal**: Verificar que, para toda entrada em que a condição de bug é verdadeira, a renderização corrigida produz o comportamento esperado.

**Pseudocode:**
```
FOR ALL input WHERE isBugCondition(input) DO
  result := renderUi_fixed(input)
  ASSERT expectedBehavior(result)
  // Bug 1: result.heroActions.display = "flex" AND NOT brokenLayout(result)
  // Bug 2: result.activeBottomNavLink.backgroundColor = "transparent" AND iconVisible(result)
END FOR
```

### Preservation Checking

**Goal**: Verificar que, para toda entrada em que a condição de bug é falsa, a renderização corrigida produz o mesmo resultado da renderização original.

**Pseudocode:**
```
FOR ALL input WHERE NOT isBugCondition(input) DO
  ASSERT renderUi_original(input) = renderUi_fixed(input)
END FOR
```

**Testing Approach**: O teste baseado em propriedades é recomendado para a verificação de preservação porque:
- Gera muitos casos automaticamente pelo domínio de entrada (viewports, áreas de navegação, estados ativo/inativo).
- Captura casos de borda que testes manuais poderiam não cobrir.
- Fornece garantias fortes de que o comportamento é inalterado para todas as entradas não afetadas.

**Test Plan**: Observar o comportamento no código NÃO corrigido para desktop (botões lado a lado, menu de topo ativo com fundo escuro) e para links inativos do menu inferior; depois escrever testes que capturem esse comportamento e confirmem que continua após a correção.

**Test Cases**:
1. **Botões da hero em desktop**: Observar que os botões ficam lado a lado com `margin-right: 10px` no código não corrigido e verificar que isso continua após a correção.
2. **Menu de topo ativo em desktop**: Observar `nav a.active` com fundo `#1f1f1f` e texto branco no código não corrigido e verificar que permanece após a correção.
3. **Links inativos do menu inferior**: Observar cor `#888` sem fundo no código não corrigido e verificar que permanece após a correção.

### Unit Tests

- Verificar que a folha de estilos contém regra `.hero-actions` com `display: flex` dentro do `@media (max-width: 768px)`.
- Verificar que `.bottom-nav a.active` declara `background-color: transparent` mantendo `color: #1f1f1f`.
- Verificar que a regra genérica `nav a.active` permanece com `background-color: #1f1f1f` (preservação do desktop).

### Property-Based Tests

- Gerar combinações de (área de navegação, estado ativo/inativo, viewport) e verificar que o fundo escuro aparece somente no menu de topo ativo, nunca no ícone ativo do menu inferior.
- Gerar viewports variados e verificar que os estilos visuais dos botões (cores, bordas, raio, destinos) permanecem constantes.
- Verificar, por múltiplos viewports, que fora da condição de bug a renderização é idêntica ao original.

### Integration Tests

- Fluxo completo da home em mobile: carregar a página, confirmar botões da hero empilhados e legíveis.
- Alternância de itens do menu inferior em mobile: confirmar que o item selecionado fica visível sem fundo escuro e que os demais permanecem cinza.
- Verificação em desktop: confirmar que botões da hero ficam lado a lado e que o menu de topo ativo mantém o fundo escuro.
