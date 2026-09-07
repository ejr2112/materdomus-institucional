# Implementation Plan: Loading Experience

## Overview

Este plano converte o design da funcionalidade **loading-experience** em uma série de tarefas de código incrementais. A implementação segue duas camadas independentes que compartilham apenas tokens de marca via CSS:

1. **Camada de boot** (HTML/CSS/JS estático em `wwwroot/index.html` e `wwwroot/css/site.css`), 100% desacoplada do runtime Blazor.
2. **Camada Blazor** — lógica pura em `Helpers/LoadingStateHelpers.cs`, componente reutilizável `Shared/SkeletonLoader.razor`, integração em `Pages/Produtos.razor` e estilos em `wwwroot/css/produtos.css`.

Cada tarefa constrói sobre as anteriores e termina com a integração ao fluxo existente, sem código órfão. Testes baseados em propriedades (FsCheck.Xunit, `MaxTest = 100`) cobrem exclusivamente a lógica pura de `LoadingStateHelpers`; testes de renderização/CSS (bUnit + xUnit, no estilo de `CssCriticalRulesTests`) cobrem UI, acessibilidade e CSS.

## Tasks

- [x] 1. Implementar a lógica pura de estado de carregamento
  - [x] 1.1 Criar `Helpers/LoadingStateHelpers.cs` com o enum `LoadingPhase` e as funções puras
    - Definir `public enum LoadingPhase { Initial, Prolonged, Success, Error }`
    - Implementar `ClampPlaceholderCount(int requested)` retornando `Math.Clamp(requested, 4, 12)`
    - Implementar `DerivePhase(double elapsedSeconds, bool completed, bool errored)`: retorna `Error` se `errored` ou `elapsedSeconds > 10` sem concluir; `Success` se `completed`; `Prolonged` se `3 <= elapsedSeconds <= 10`; caso contrário `Initial`
    - Implementar `IsValidLoadingMessage(string message)`: verdadeiro se não vazia e `Length <= 60`
    - _Requirements: 1.3, 2.1, 2.2, 6.1, 6.3_

  - [ ]* 1.2 Escrever teste de propriedade para `ClampPlaceholderCount`
    - **Property 1: Contagem de placeholders sempre no intervalo válido**
    - Verificar que o resultado está em [4, 12] e que devolve o próprio `n` quando `n` já pertence a [4, 12]
    - Tag: `// Feature: loading-experience, Property 1: Contagem de placeholders sempre no intervalo válido`
    - FsCheck.Xunit `[Property(MaxTest = 100)]`
    - **Validates: Requirements 2.1**

  - [ ]* 1.3 Escrever teste de propriedade para falha/timeout de `DerivePhase`
    - **Property 2: Falha ou timeout sempre resulta em estado de erro**
    - Verificar que `errored` ou `t > 10` sem concluir resulta em `Error`
    - Tag: `// Feature: loading-experience, Property 2: Falha ou timeout sempre resulta em estado de erro`
    - FsCheck.Xunit `[Property(MaxTest = 100)]`
    - **Validates: Requirements 6.1, 2.5**

  - [ ]* 1.4 Escrever teste de propriedade para totalidade/determinismo de `DerivePhase`
    - **Property 3: Classificação de fase é total e determinística**
    - Verificar que toda entrada produz exatamente uma `LoadingPhase` e sempre o mesmo resultado para a mesma entrada
    - Tag: `// Feature: loading-experience, Property 3: Classificação de fase é total e determinística`
    - FsCheck.Xunit `[Property(MaxTest = 100)]`
    - **Validates: Requirements 6.1, 6.3**

  - [ ]* 1.5 Escrever teste de propriedade para a faixa de carregamento prolongado
    - **Property 4: Faixa de carregamento prolongado é sinalizada**
    - Verificar que `3 <= t <= 10` (sem concluir/erro) resulta em `Prolonged` e `0 <= t < 3` resulta em `Initial`
    - Tag: `// Feature: loading-experience, Property 4: Faixa de carregamento prolongado é sinalizada`
    - FsCheck.Xunit `[Property(MaxTest = 100)]`
    - **Validates: Requirements 6.3**

  - [ ]* 1.6 Escrever teste de propriedade para `IsValidLoadingMessage`
    - **Property 5: Validação de mensagem de carregamento respeita o limite**
    - Verificar que é verdadeiro se e somente se `s` é não vazia e tem no máximo 60 caracteres
    - Tag: `// Feature: loading-experience, Property 5: Validação de mensagem de carregamento respeita o limite`
    - FsCheck.Xunit `[Property(MaxTest = 100)]`
    - **Validates: Requirements 1.3, 2.2**

- [x] 2. Implementar a tela de boot estática (pré-runtime)
  - [x] 2.1 Substituir o markup de boot em `wwwroot/index.html`
    - Substituir `<div id="app">Carregando...</div>` pelo bloco `.boot-screen` com `<img class="boot-screen__logo" src="images/logo.png" alt="MaterDomus" />`, spinner `.boot-screen__spinner` com `aria-hidden="true"`, mensagem `.boot-screen__message` em português (≤ 60 caracteres) e bloco de erro `#boot-error` (oculto por padrão) com mensagem em português e botão "Recarregar página"
    - Adicionar script inline (não-Blazor) com `setTimeout` de 30s que oculta spinner/mensagem e exibe `#boot-error`, mantendo o logo visível
    - _Requirements: 1.1, 1.3, 1.6, 1.7, 1.8, 4.5, 4.6_

  - [x] 2.2 Adicionar os estilos da tela de boot em `wwwroot/css/site.css`
    - Estilizar `.boot-screen` (fixed, centralizado, `background:#fff`, `color:#222`, `font-family: system-ui`)
    - Estilizar `.boot-screen__spinner` com animação `spin` de ciclo `0.8s` e cor de destaque `#ff9900`
    - Aplicar `max-width` à `.boot-screen__message` e estilos ao `.boot-screen__error`
    - Aplicar transição de opacidade em `#app` (duração entre 200ms e 500ms)
    - Adicionar `@media (prefers-reduced-motion: reduce)` desativando a animação do spinner e a transição de opacidade
    - _Requirements: 1.2, 1.4, 3.1, 3.4, 5.1, 5.4_

  - [ ]* 2.3 Escrever testes de string/renderização para a tela de boot
    - Ler `wwwroot/index.html` e `wwwroot/css/site.css` (à la `CssCriticalRulesTests`) e verificar: `images/logo.png` com `alt`; spinner com `aria-hidden`; mensagem ≤ 60 caracteres; bloco de erro presente e oculto; regra `@media (prefers-reduced-motion: reduce)`; duração de transição entre 200–500ms
    - _Requirements: 1.1, 1.3, 1.6, 3.1, 3.4, 4.5, 4.6, 5.1, 5.4_

- [x] 3. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 4. Implementar o componente `SkeletonLoader`
  - [x] 4.1 Criar `Shared/SkeletonLoader.razor`
    - Parâmetros `PlaceholderCount` (int, padrão 6), `Message` (string, ≤ 60 caracteres), `ShowProlongedIndicator` (bool)
    - Renderizar região `role="status" aria-live="polite"` contendo a mensagem
    - Renderizar grade `.skeleton__grid` com `aria-hidden="true"` e cartões `.skeleton-card` (imagem + linhas), usando `LoadingStateHelpers.ClampPlaceholderCount(PlaceholderCount)` para limitar a contagem a [4, 12]
    - Renderizar indicador de atividade prolongada (`aria-hidden="true"`) quando `ShowProlongedIndicator` for verdadeiro
    - _Requirements: 2.1, 2.2, 4.1, 4.7, 6.3_

  - [x] 4.2 Adicionar os estilos do esqueleto em `wwwroot/css/produtos.css`
    - `.skeleton__grid` replicando a grade responsiva de `.products-grid`
    - `.skeleton-card` com `border: var(--card-border)`, `border-radius: var(--card-radius)` e `font-family: system-ui`
    - Animação de shimmer em `.skeleton-card__image` / `.skeleton-card__line`
    - Transições de opacidade (200–500ms) para grade e esqueleto
    - `@media (prefers-reduced-motion: reduce)` desativando shimmer e transições
    - _Requirements: 2.4, 3.2, 3.4, 5.2, 5.3, 5.4_

  - [ ]* 4.3 Escrever testes bUnit para `SkeletonLoader`
    - Verificar que a grade tem entre 4 e 12 cartões conforme `PlaceholderCount` (incluindo valores fora do intervalo)
    - Verificar `aria-hidden="true"` na grade e região `aria-live="polite"` com a mensagem
    - Verificar exibição do indicador prolongado quando `ShowProlongedIndicator=true`
    - _Requirements: 2.1, 2.2, 4.1, 4.7, 6.3_

- [x] 5. Integrar o carregamento aprimorado em `Pages/Produtos.razor`
  - [x] 5.1 Substituir o bloco `_isLoading` pelo `SkeletonLoader` e adicionar estado aria-live
    - Substituir o `div.produtos-state` com spinner simples por `<SkeletonLoader Message="@_ariaMessage" ShowProlongedIndicator="_isProlonged" />`
    - Adicionar campos `_isProlonged` e `_ariaMessage`
    - Atualizar `_ariaMessage` com a mensagem de carregamento ao iniciar (≤ 500ms) e com mensagens de conclusão/falha em português
    - Aplicar fade-in da grade ao concluir com sucesso
    - _Requirements: 2.2, 2.3, 3.2, 4.1, 4.2, 4.3, 4.4_

  - [x] 5.2 Integrar a detecção de carregamento prolongado (3–10s)
    - Adicionar temporizador (`PeriodicTimer`/`System.Timers.Timer`) que, junto com `LoadingStateHelpers.DerivePhase`, ativa `_isProlonged` na faixa de 3–10s via `StateHasChanged`
    - Garantir descarte do temporizador em `Dispose` e no fim do carregamento
    - _Requirements: 6.3_

  - [x] 5.3 Integrar o tratamento de erro/timeout e o retry
    - Ao falhar ou exceder 10s: remover esqueleto, atualizar `aria-live` com falha, exibir estado de erro com botão "Tentar novamente"
    - Garantir que `RetryLoad` reinicie o carregamento (reentrante, sem limite de tentativas) trocando o estado de erro pelo esqueleto em até 1s
    - _Requirements: 2.5, 6.1, 6.2, 6.4_

  - [ ]* 5.4 Escrever testes bUnit para `Produtos.razor`
    - Usar `IProductCatalogService` fake para exercitar sucesso, erro e timeout
    - Verificar a troca de branch (esqueleto → grade / erro) e o conteúdo da região `aria-live`
    - Verificar exibição do botão "Tentar novamente" e o reinício do carregamento no retry
    - _Requirements: 2.3, 2.5, 4.3, 4.4, 6.1, 6.2_

- [x] 6. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tarefas marcadas com `*` são opcionais e podem ser puladas para um MVP mais rápido.
- Cada tarefa referencia requisitos específicos para rastreabilidade.
- Os checkpoints garantem validação incremental.
- Testes de propriedade (FsCheck.Xunit, `MaxTest = 100`) validam apenas a lógica pura de `LoadingStateHelpers`; testes de renderização/CSS (bUnit + xUnit) validam UI, acessibilidade e CSS.
- O projeto `MaterDomus.Tests` já referencia `FsCheck`, `FsCheck.Xunit` e `bunit`, portanto nenhuma dependência nova é necessária.

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "2.1", "2.2"] },
    { "id": 1, "tasks": ["1.2", "1.3", "1.4", "1.5", "1.6", "2.3", "4.1", "4.2"] },
    { "id": 2, "tasks": ["4.3", "5.1"] },
    { "id": 3, "tasks": ["5.2"] },
    { "id": 4, "tasks": ["5.3"] },
    { "id": 5, "tasks": ["5.4"] }
  ]
}
```
