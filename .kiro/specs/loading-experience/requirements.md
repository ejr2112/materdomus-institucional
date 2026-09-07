# Requirements Document

## Introduction

O site institucional MaterDomus é uma aplicação Blazor WebAssembly. Atualmente a experiência de carregamento é pobre em dois momentos distintos:

1. **Carregamento inicial da aplicação (boot):** enquanto o framework Blazor WebAssembly é baixado e inicializado, o arquivo `index.html` exibe apenas o texto simples `Carregando...` dentro de `<div id="app">`, sem marca, animação ou identidade visual.
2. **Carregamento de conteúdo na página de Produtos:** enquanto os produtos são buscados, a página exibe um spinner simples com o texto "Carregando produtos...".

Esta funcionalidade tem como objetivo aprimorar a experiência de carregamento em toda a aplicação, oferecendo uma tela de boot com identidade visual da marca, estados de carregamento de conteúdo mais polidos (incluindo placeholders de esqueleto), transições suaves e conformidade de acessibilidade. A solução deve manter consistência com o estilo de marca já existente (tipografia `system-ui`, cor de texto `#222`, bordas `#eaeaea`, raio de borda de `6px` e a cor de destaque `#ff9900`).

## Glossary

- **Tela_De_Boot**: A tela de carregamento inicial exibida dentro de `<div id="app">` em `index.html` enquanto o runtime Blazor WebAssembly é baixado e inicializado, antes da renderização do primeiro componente.
- **Estado_De_Carregamento_De_Conteudo**: O indicador visual exibido dentro de uma página já renderizada enquanto dados assíncronos (por exemplo, o catálogo de produtos) são carregados.
- **Esqueleto_De_Carregamento**: Um conjunto de placeholders visuais que reproduzem a estrutura aproximada do conteúdo final (por exemplo, cartões de produto) exibido durante o carregamento de conteúdo.
- **Aplicacao**: A aplicação Blazor WebAssembly MaterDomus como um todo.
- **Pagina_De_Produtos**: A página localizada na rota `/produtos` que exibe o catálogo de produtos.
- **Logotipo_Da_Marca**: A imagem de identidade visual da MaterDomus disponível em `wwwroot/images/logo.png`.
- **Movimento_Reduzido**: A preferência de acessibilidade do usuário indicada pela media query CSS `prefers-reduced-motion: reduce`.
- **Tecnologia_Assistiva**: Software como leitores de tela que interpretam a interface para usuários com deficiência.

## Requirements

### Requisito 1: Tela de boot com identidade visual

**User Story:** Como visitante do site, quero ver uma tela de carregamento inicial com a identidade visual da MaterDomus, para que eu perceba que a página está carregando e reconheça a marca desde o primeiro instante.

#### Acceptance Criteria

1. WHILE o runtime Blazor WebAssembly está sendo baixado e inicializado, THE Tela_De_Boot SHALL exibir o Logotipo_Da_Marca de forma visível na área central da viewport, sem depender de recursos baixados pelo runtime Blazor.
2. WHILE o runtime Blazor WebAssembly está sendo baixado e inicializado, THE Tela_De_Boot SHALL exibir um indicador de progresso animado com movimento contínuo, cujo ciclo de animação se repete em intervalo entre 0,5 e 2 segundos.
3. WHILE o runtime Blazor WebAssembly está sendo baixado e inicializado, THE Tela_De_Boot SHALL exibir uma mensagem textual de carregamento em português com no máximo 60 caracteres.
4. THE Tela_De_Boot SHALL aplicar a cor de texto `#222`, a tipografia `system-ui` e a cor de destaque `#ff9900`.
5. WHEN a renderização do primeiro componente Blazor da Aplicacao é concluída, THE Tela_De_Boot SHALL ser removida do documento em até 500 milisegundos e substituída pelo conteúdo da Aplicacao, preservando o conteúdo da Aplicacao já renderizado.
6. THE Tela_De_Boot SHALL utilizar estilos inline ou o arquivo `css/site.css` já referenciado em `index.html`, sem depender do runtime Blazor.
7. WHEN o documento HTML inicial é carregado pelo navegador e antes do início do download do runtime Blazor WebAssembly, THE Tela_De_Boot SHALL ser renderizada com o Logotipo_Da_Marca e a mensagem textual de carregamento na primeira pintura da página.
8. IF o download e a inicialização do runtime Blazor WebAssembly não são concluídos em até 30 segundos após o carregamento do documento inicial, THEN THE Tela_De_Boot SHALL exibir uma mensagem em português indicando falha no carregamento e uma ação para recarregar a página, mantendo o Logotipo_Da_Marca visível.

### Requisito 2: Estado de carregamento de conteúdo aprimorado na página de produtos

**User Story:** Como visitante da página de produtos, quero ver um estado de carregamento que represente a estrutura do conteúdo que está por vir, para que a espera seja mais agradável e a página pareça mais responsiva.

#### Acceptance Criteria

1. WHILE o catálogo de produtos está sendo carregado, THE Pagina_De_Produtos SHALL exibir um Esqueleto_De_Carregamento contendo entre 4 e 12 cartões placeholder que reproduzem a estrutura da grade de cartões de produto.
2. WHILE o catálogo de produtos está sendo carregado, THE Pagina_De_Produtos SHALL exibir uma mensagem textual de carregamento em português com no máximo 60 caracteres.
3. WHEN o carregamento do catálogo de produtos é concluído com sucesso, THE Pagina_De_Produtos SHALL substituir o Esqueleto_De_Carregamento pela grade de produtos, com transição concluída em até 500 milisegundos.
4. THE Estado_De_Carregamento_De_Conteudo SHALL aplicar a cor de borda `#eaeaea`, o raio de borda de `6px` e a tipografia `system-ui`.
5. IF o carregamento do catálogo de produtos falha, THEN THE Pagina_De_Produtos SHALL remover o Esqueleto_De_Carregamento, exibir uma mensagem de erro em português e não exibir a grade de produtos.

### Requisito 3: Transições suaves

**User Story:** Como visitante do site, quero que a transição entre o estado de carregamento e o conteúdo final seja suave, para que a mudança não seja abrupta.

#### Acceptance Criteria

1. WHEN a Tela_De_Boot é substituída pelo conteúdo da Aplicacao, THE Aplicacao SHALL aplicar uma transição de opacidade da Tela_De_Boot de 100% para 0% e do conteúdo da Aplicacao de 0% para 100%, com duração entre 200ms e 500ms, e ao término remover a Tela_De_Boot do fluxo visível.
2. WHEN o Esqueleto_De_Carregamento é substituído pela grade de produtos, THE Pagina_De_Produtos SHALL aplicar uma transição de opacidade do Esqueleto_De_Carregamento de 100% para 0% e da grade de produtos de 0% para 100%, com duração entre 200ms e 500ms, e ao término remover o Esqueleto_De_Carregamento do fluxo visível.
3. IF a transição de opacidade é interrompida ou falha antes de sua conclusão, THEN THE Aplicacao SHALL exibir o conteúdo final com opacidade de 100% e remover a Tela_De_Boot e o Esqueleto_De_Carregamento do fluxo visível.
4. WHERE Movimento_Reduzido está ativo, THE Aplicacao SHALL exibir o conteúdo final com opacidade de 100% sem aplicar transição de opacidade.

### Requisito 4: Acessibilidade dos estados de carregamento

**User Story:** Como usuário de tecnologia assistiva, quero que os estados de carregamento sejam anunciados de forma adequada, para que eu entenda o que está acontecendo mesmo sem ver a tela.

#### Acceptance Criteria

1. THE Estado_De_Carregamento_De_Conteudo SHALL expor uma região com o atributo `aria-live="polite"` contendo a mensagem textual de carregamento.
2. WHEN o Estado_De_Carregamento_De_Conteudo é iniciado, THE Estado_De_Carregamento_De_Conteudo SHALL atualizar a região aria-live com a mensagem de carregamento em até 500 milisegundos.
3. WHEN o carregamento de conteúdo é concluído com sucesso, THE Estado_De_Carregamento_De_Conteudo SHALL atualizar a região aria-live com uma mensagem em português indicando a conclusão.
4. IF o carregamento de conteúdo falha, THEN THE Estado_De_Carregamento_De_Conteudo SHALL atualizar a região aria-live com uma mensagem em português indicando a falha.
5. THE Tela_De_Boot SHALL fornecer um texto alternativo descritivo em português para o Logotipo_Da_Marca por meio do atributo `alt`, com no máximo 125 caracteres.
6. WHERE o Logotipo_Da_Marca não possui texto alternativo descritivo disponível, THE Tela_De_Boot SHALL ocultar o Logotipo_Da_Marca da Tecnologia_Assistiva por meio do atributo `aria-hidden="true"`.
7. WHERE o indicador de progresso é puramente decorativo, THE Estado_De_Carregamento_De_Conteudo SHALL ocultá-lo da Tecnologia_Assistiva por meio do atributo `aria-hidden="true"`.

### Requisito 5: Respeito à preferência de movimento reduzido

**User Story:** Como usuário sensível a animações, quero que as animações de carregamento sejam reduzidas quando eu configurar essa preferência no sistema, para que eu não tenha desconforto.

#### Acceptance Criteria

1. WHILE Movimento_Reduzido está ativo, THE Tela_De_Boot SHALL desativar todas as animações do indicador de progresso, exibindo-o em estado estático sem movimento.
2. WHILE Movimento_Reduzido está ativo, THE Estado_De_Carregamento_De_Conteudo SHALL desativar todas as animações do Esqueleto_De_Carregamento, exibindo os elementos placeholder em estado estático sem movimento.
3. WHILE Movimento_Reduzido está ativo, THE Estado_De_Carregamento_De_Conteudo SHALL desativar todas as animações do indicador de progresso, exibindo-o em estado estático sem movimento.
4. WHILE Movimento_Reduzido está ativo, THE Aplicacao SHALL desativar as transições de opacidade descritas no Requisito 3, exibindo o conteúdo diretamente em opacidade total sem transição gradual.
5. WHEN o usuário ativa Movimento_Reduzido enquanto um Estado_De_Carregamento_De_Conteudo está em andamento, THE Aplicacao SHALL aplicar a desativação das animações em até 500 milisegundos.

### Requisito 6: Tratamento de carregamento prolongado

**User Story:** Como visitante do site, quero receber uma indicação quando o carregamento demorar mais do que o esperado, para que eu saiba que o sistema ainda está trabalhando.

#### Acceptance Criteria

1. IF o carregamento do catálogo de produtos excede 10 segundos e ainda não foi concluído, THEN THE Pagina_De_Produtos SHALL encerrar o Estado_De_Carregamento_De_Conteudo e exibir um estado de erro contendo uma mensagem em português indicando que o carregamento falhou e um controle acionável rotulado para tentar novamente.
2. WHEN o usuário aciona o controle de tentar novamente, THE Pagina_De_Produtos SHALL substituir o estado de erro pelo Estado_De_Carregamento_De_Conteudo em até 1 segundo e reiniciar o carregamento do catálogo de produtos.
3. WHILE o carregamento do catálogo de produtos está em andamento e o tempo decorrido é de 3 a 10 segundos, THE Pagina_De_Produtos SHALL exibir uma indicação visual de atividade em andamento dentro do Estado_De_Carregamento_De_Conteudo.
4. IF a segunda tentativa de carregamento do catálogo de produtos também excede 10 segundos sem concluir, THEN THE Pagina_De_Produtos SHALL exibir novamente o estado de erro com a mensagem em português e o controle para tentar novamente, sem limite máximo de tentativas subsequentes.
