# Bugfix Requirements Document

## Introduction

No site institucional MaterDomus (Blazor WebAssembly, .NET 9), a navegação desaparece completamente em dispositivos móveis. Em viewports de largura ≤ 768px, a folha de estilos `wwwroot/css/site.css` oculta o menu superior do cabeçalho (`.header nav { display: none; }`) e prepara os estilos de uma barra de navegação inferior (`.bottom-nav`) que deveria substituí-lo. Entretanto, o componente `Shared/BottomNav.razor` nunca é renderizado: ele não está referenciado em `Shared/MainLayout.razor`. O resultado é que, no celular, o menu superior fica escondido e a barra inferior não existe no DOM, deixando o usuário sem nenhuma forma de navegação.

Essa situação corresponde ao relato do usuário: "A tela inicial não está responsiva e os menus não aparecem no celular." A tarefa 5.1 do spec `menu-redesign` (inserir `<BottomNav />` no `MainLayout.razor`) foi deixada incompleta, o que deixou a experiência móvel em um estado quebrado.

**Componentes afetados:**
- `Shared/MainLayout.razor` — não renderiza `<BottomNav />`.
- `wwwroot/css/site.css` — oculta a nav do cabeçalho em ≤ 768px sem uma alternativa visível.
- `Shared/BottomNav.razor` — componente existente e funcional, porém nunca montado na árvore de layout.

## Bug Analysis

### Current Behavior (Defect)

Quando o site é acessado em uma viewport móvel, nenhum menu de navegação fica disponível.

1.1 WHEN a página é renderizada em uma viewport com largura ≤ 768px THEN o sistema oculta o menu de navegação do cabeçalho (`.header nav`) via `display: none` e não exibe nenhuma navegação substituta, pois `<BottomNav />` não está presente no DOM

1.2 WHEN o usuário está em uma viewport móvel (≤ 768px) e tenta navegar entre páginas (Início, Produtos, Sobre, Fornecedores, Contato) THEN o sistema não oferece nenhum controle de navegação visível ou acionável

1.3 WHEN o `MainLayout` é montado THEN o sistema não renderiza o componente `BottomNav`, apesar de os estilos `.bottom-nav` existirem e estarem preparados para exibição em mobile

### Expected Behavior (Correct)

Em viewports móveis, a barra de navegação inferior deve substituir o menu do cabeçalho, mantendo a navegação sempre acessível.

2.1 WHEN a página é renderizada em uma viewport com largura ≤ 768px THEN o sistema SHALL exibir a barra de navegação inferior (`BottomNav`) fixada na parte inferior da tela com os 5 destinos (Início, Produtos, Sobre, Fornecedores, Contato)

2.2 WHEN o usuário está em uma viewport móvel (≤ 768px) e toca em um item da navegação inferior THEN o sistema SHALL navegar para a página correspondente e marcar o item ativo

2.3 WHEN o `MainLayout` é montado THEN o sistema SHALL renderizar o componente `<BottomNav />` na árvore de layout para que os estilos `.bottom-nav` de mobile tenham conteúdo a exibir

### Unchanged Behavior (Regression Prevention)

O comportamento em telas maiores e o restante do layout devem permanecer intactos.

3.1 WHEN a página é renderizada em uma viewport com largura > 768px THEN o sistema SHALL CONTINUAR a exibir o menu de navegação do cabeçalho (`.header nav`) com os 5 links e o realce do link ativo

3.2 WHEN a página é renderizada em uma viewport com largura > 768px THEN o sistema SHALL CONTINUAR a ocultar a barra de navegação inferior (`.bottom-nav { display: none; }`)

3.3 WHEN qualquer viewport é usada THEN o sistema SHALL CONTINUAR a renderizar o cabeçalho com logo, o conteúdo principal (`@Body`) e o rodapé sem alterações visuais

3.4 WHEN uma página é navegada em qualquer viewport THEN o sistema SHALL CONTINUAR a rastrear a visualização via `PageViewTracker` sem regressão
