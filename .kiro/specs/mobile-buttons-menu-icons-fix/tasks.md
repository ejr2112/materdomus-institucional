# Plano de Implementação

- [x] 1. Escrever teste de exploração da condição de bug (ANTES de corrigir)
  - **Property 1: Bug Condition** - Layout mobile dos botões da hero e fundo do ícone ativo do menu inferior
  - **CRÍTICO**: Este teste DEVE FALHAR no código não corrigido — a falha confirma que o bug existe
  - **NÃO tente corrigir o teste nem o código quando ele falhar**
  - **NOTA**: Este teste codifica o comportamento esperado — ele validará a correção quando passar após a implementação
  - **OBJETIVO**: Expor contraexemplos que demonstrem que os bugs existem
  - **Abordagem PBT escopada**: Como são bugs determinísticos de CSS, escope a propriedade para os casos concretos que falham (condição de bug de `isBugCondition` no design), garantindo reprodutibilidade
  - Criar `MaterDomus.Tests/Unit/MobileButtonsMenuBugConditionTests.cs`, lendo `wwwroot/css/site.css` como texto (mesmo padrão de `CssCriticalRulesTests`)
  - Bug 1 — dentro do bloco `@media (max-width: 768px)`, asseverar que existe uma regra `.hero-actions` contendo `display: flex` e `flex-direction: column` (condição de bug: `area = "hero-actions" AND viewportWidth <= 768`, do design)
  - Bug 2 — asseverar que a regra `.bottom-nav a.active` contém `background-color: transparent` (condição de bug: `area = "bottom-nav" AND isActive = true AND viewportWidth <= 768`, do design)
  - As asserções devem corresponder às Correctness Properties do design (Property 1): botões da hero organizados/empilhados e ícone ativo do menu inferior sem fundo escuro
  - Rodar o teste no código NÃO corrigido: `dotnet test MaterDomus.Tests/MaterDomus.Tests.csproj --filter "FullyQualifiedName~MobileButtonsMenuBugConditionTests"`
  - **RESULTADO ESPERADO**: Teste FALHA (correto — prova que os bugs existem: `.hero-actions` sem regra flex no mobile; `.bottom-nav a.active` sem `background-color: transparent`)
  - Documentar os contraexemplos encontrados (ex.: ".hero-actions ausente do @media 768px" e ".bottom-nav a.active sem background-color transparent, herdando #1f1f1f de nav a.active")
  - Marcar a tarefa como concluída quando o teste estiver escrito, executado e a falha documentada
  - _Requirements: 1.1, 1.2, 2.1, 2.2_

- [x] 2. Escrever testes de preservação baseados em propriedades (ANTES de corrigir)
  - **Property 2: Preservation** - Comportamento inalterado fora da condição de bug
  - **IMPORTANTE**: Seguir a metodologia observation-first
  - Criar `MaterDomus.Tests/Unit/MobileButtonsMenuPreservationTests.cs`, lendo `wwwroot/css/site.css` como texto
  - Observar no código NÃO corrigido e travar em testes:
    - `nav a.active` mantém `background-color: #1f1f1f` e `color: #ffffff` (menu de topo ativo em desktop — Requisito 3.2)
    - `.hero-actions` fora do `@media (max-width: 768px)` mantém apenas `margin-top: 30px`, sem `display: flex` no nível superior (botões lado a lado em desktop — Requisito 3.1)
    - `.btn-primary` mantém `margin-right: 10px` no nível superior (espaçamento em desktop — Requisitos 3.1, 3.4)
    - `.bottom-nav a` (inativo) mantém `color: #888` sem fundo (Requisito 3.3)
    - Estilos visuais dos botões (`.btn-primary`, `.btn-secondary`: cores, bordas, `border-radius`) permanecem inalterados (Requisito 3.4)
  - Recomenda-se abordagem baseada em propriedades (FsCheck, como em `ProductPreservationTests`): gerar combinações de (área de navegação, estado ativo/inativo) e verificar que o fundo escuro `#1f1f1f` aparece somente em `nav a.active` do menu de topo, nunca no ícone ativo do `.bottom-nav`
  - Rodar os testes no código NÃO corrigido: `dotnet test MaterDomus.Tests/MaterDomus.Tests.csproj --filter "FullyQualifiedName~MobileButtonsMenuPreservationTests"`
  - **RESULTADO ESPERADO**: Testes PASSAM (confirma o comportamento de linha de base a ser preservado)
  - Marcar a tarefa como concluída quando os testes estiverem escritos, executados e passando no código não corrigido
  - _Requirements: 3.1, 3.2, 3.3, 3.4_

- [x] 3. Correção dos dois bugs de CSS mobile em `wwwroot/css/site.css`

  - [x] 3.1 Bug 1 — Adicionar regra responsiva para `.hero-actions`
    - Dentro do bloco `@media (max-width: 768px)` já existente em `wwwroot/css/site.css`, adicionar a regra `.hero-actions { display: flex; flex-direction: column; align-items: center; gap: 12px; }`
    - Adicionar, no mesmo bloco mobile, `.hero-actions .btn-primary { margin-right: 0; }` para neutralizar o `margin-right: 10px` que só faz sentido no layout lado a lado do desktop (o `gap` controla o espaçamento em mobile)
    - NÃO alterar `.hero-actions` fora do `@media (max-width: 768px)` (preservar `margin-top: 30px` do desktop)
    - _Bug_Condition: isBugCondition(input) onde input.area = "hero-actions" AND input.viewportWidth <= 768 (do design)_
    - _Expected_Behavior: expectedBehavior(result) — result.heroActions.display = "flex" AND NOT brokenLayout(result) (Property 1 do design)_
    - _Preservation: botões lado a lado em desktop com margin-right: 10px; estilos visuais dos botões inalterados (Preservation Requirements do design)_
    - _Requirements: 2.1, 3.1, 3.4_

  - [x] 3.2 Bug 2 — Reverter o fundo do ícone ativo do menu inferior
    - Na regra existente `.bottom-nav a.active` (dentro do bloco `@media (max-width: 768px)`), adicionar `background-color: transparent;`, mantendo `color: #1f1f1f;`
    - NÃO usar `!important` — a especificidade de `.bottom-nav a.active` (0,3,0) já supera `nav a.active` (0,2,0)
    - NÃO alterar a regra genérica `nav a.active` (ela preserva o fundo escuro do menu de topo em desktop)
    - _Bug_Condition: isBugCondition(input) onde input.area = "bottom-nav" AND input.isActive = true AND input.viewportWidth <= 768 (do design)_
    - _Expected_Behavior: expectedBehavior(result) — result.activeBottomNavLink.backgroundColor = "transparent" AND iconVisible(result) (Property 1 do design)_
    - _Preservation: nav a.active mantém background-color #1f1f1f e texto branco em desktop; links inativos do menu inferior em #888 sem fundo (Preservation Requirements do design)_
    - _Requirements: 2.2, 3.2, 3.3_

  - [x] 3.3 Verificar que o teste de exploração da condição de bug agora passa
    - **Property 1: Expected Behavior** - Layout mobile dos botões da hero e fundo do ícone ativo do menu inferior
    - **IMPORTANTE**: Re-executar o MESMO teste da tarefa 1 — NÃO escrever um novo teste
    - O teste da tarefa 1 codifica o comportamento esperado; ao passar, confirma que o comportamento esperado foi satisfeito
    - Rodar: `dotnet test MaterDomus.Tests/MaterDomus.Tests.csproj --filter "FullyQualifiedName~MobileButtonsMenuBugConditionTests"`
    - **RESULTADO ESPERADO**: Teste PASSA (confirma que os bugs foram corrigidos)
    - _Requirements: 2.1, 2.2 (Expected Behavior / Property 1 do design)_

  - [x] 3.4 Verificar que os testes de preservação continuam passando
    - **Property 2: Preservation** - Comportamento inalterado fora da condição de bug
    - **IMPORTANTE**: Re-executar os MESMOS testes da tarefa 2 — NÃO escrever novos testes
    - Rodar: `dotnet test MaterDomus.Tests/MaterDomus.Tests.csproj --filter "FullyQualifiedName~MobileButtonsMenuPreservationTests"`
    - **RESULTADO ESPERADO**: Testes PASSAM (confirma ausência de regressões)
    - Confirmar que `nav a.active` continua com fundo `#1f1f1f`, botões da hero permanecem lado a lado em desktop e links inativos do menu inferior seguem em `#888`
    - _Requirements: 3.1, 3.2, 3.3, 3.4_

- [x] 4. Checkpoint — Garantir que todos os testes passam
  - Rodar a suíte completa: `dotnet test MaterDomus.Tests/MaterDomus.Tests.csproj`
  - Confirmar que os testes de condição de bug (tarefa 1), os testes de preservação (tarefa 2) e os testes existentes do projeto (`CssCriticalRulesTests`, etc.) passam
  - Se surgirem dúvidas ou falhas inesperadas, perguntar ao usuário antes de prosseguir
