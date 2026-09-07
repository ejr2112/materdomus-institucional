# Plano de Implementação

- [x] 1. Escrever teste de exploração da condição do bug (ausência de artefatos de vídeo)
  - **Property 1: Bug Condition** - Ausência total de artefatos de vídeo
  - **IMPORTANTE**: Escreva este teste baseado em propriedades ANTES de qualquer alteração de código
  - **CRÍTICO**: No estado atual (pós-revert `a646466`) espera-se que este teste PASSE; se ele FALHAR, a falha confirma que um vestígio do recurso de vídeo existe (condição do bug ativa)
  - **NÃO tente "corrigir" o teste** — uma falha aqui expõe o artefato exato a ser removido na tarefa 3
  - **GOAL**: Surgir contraexemplos que demonstrem qualquer vestígio do recurso de vídeo
  - **Abordagem PBT com escopo**: como a condição do bug é determinística sobre superfícies concretas, escopar a propriedade para os casos concretos verificáveis (modelo, marcação do cartão, CSS, dados, `.gitignore`)
  - Verificar via reflexão que o record `Product` NÃO expõe a propriedade `VideoUrl` (de `isBugCondition`: `input.productModel.hasProperty("VideoUrl")`)
  - Renderizar `ProductCard` (bUnit) com um produto de teste e afirmar que a marcação NÃO contém `<iframe` nem as classes `product-card__video`/`product-card__video-wrapper`
  - Deserializar `wwwroot/data/products.json` em `List<Product>` e afirmar que nenhum item contém o campo `videoUrl`; adicionalmente, gerar um payload JSON com um campo `videoUrl` extra e afirmar que a deserialização o ignora sem alterar o `Product` resultante
  - Inspecionar `.gitignore` e afirmar que NÃO contém entradas `*.mov`/`*.mp4` introduzidas para o recurso
  - Inspecionar `wwwroot/css/produtos.css` e afirmar que NÃO define as regras `product-card__video`/`product-card__video-wrapper`
  - Executar no estado atual (código NÃO alterado)
  - **RESULTADO ESPERADO**: teste PASSA (confirma que o revert removeu todos os artefatos). Caso qualquer asserção FALHE, documentar o contraexemplo (o artefato exato encontrado) para orientar a remoção cirúrgica na tarefa 3
  - Marcar a tarefa como concluída quando o teste estiver escrito, executado e o resultado (passagem, ou falha com contraexemplo) documentado
  - _Requirements: 1.1, 1.2, 1.3, 2.1, 2.2, 2.3_

- [x] 2. Escrever testes de preservação (ANTES de qualquer alteração)
  - **Property 2: Preservation** - Comportamento do produto inalterado
  - **IMPORTANTE**: Seguir a metodologia observation-first — observe o comportamento no código NÃO alterado, registre-o e trave-o em testes
  - Observar e registrar no estado atual: renderização do cartão (imagem com fallback `placeholder-product.png`, nome, descrição truncada em 120 chars via `ProductHelpers.TruncateDescription`, preço formatado `R$ X,XX` pt-BR, categoria)
  - Observar e registrar: botão "Comprar na Amazon" aparece somente quando `AmazonUrl` é válida, com `target="_blank"` e `rel="noopener noreferrer"`
  - Observar e registrar: favoritar alterna estado via `FavoritesService.Toggle`, reflete `aria-pressed`/`aria-label` e atualiza o contador; "Ver detalhes" abre/fecha o modal (nome, preço, texto persuasivo, CTA da Amazon) por clique, Enter/Espaço e Escape
  - Observar e registrar: busca, filtro por categoria e filtro de favoritos filtram a grade corretamente; estados de carregamento (skeleton), erro (com "Tentar novamente") e vazio; carregamento/validação do catálogo em `ProductCatalogService`
  - **Testes unitários (bUnit/xUnit)**: renderização do cartão afirmando presença correta de imagem/nome/descrição/preço/categoria/ações; formatação de preço e truncamento de descrição inalterados
  - **Testes baseados em propriedades (FsCheck — já usado no projeto)**: gerar produtos aleatórios e verificar que a renderização preserva imagem/nome/descrição truncada/preço/categoria; verificar em muitos cenários que o botão da Amazon e seus atributos de segurança são preservados; gerar payloads JSON com `videoUrl` extra e verificar idempotência do modelo na deserialização
  - **Testes de integração**: fluxo completo da página de produtos (carregamento via `ProductCatalogService`, renderização da grade); alternância de busca/filtro por categoria/filtro de favoritos; estados de carregamento/erro/vazio; abertura/fechamento do modal de detalhes
  - Executar todos os testes no código NÃO alterado
  - **RESULTADO ESPERADO**: testes PASSAM (confirma a linha de base a ser preservada)
  - Marcar a tarefa como concluída quando os testes estiverem escritos, executados e passando no código não alterado
  - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6_

- [x] 3. Remoção condicional de vestígios do recurso de vídeo

  - [x] 3.1 Remover cirurgicamente qualquer artefato de vídeo revelado pela verificação
    - **Aplicar SOMENTE se a tarefa 1 revelou um contraexemplo**; caso contrário (estado limpo confirmado), nenhuma alteração de código é necessária
    - Se `Product` (`Models/Product.cs`) declarar `VideoUrl`: remover a propriedade
    - Se `Shared/ProductCard.razor` contiver `<iframe>` de vídeo ou `product-card__video-wrapper`/`product-card__video`: remover a marcação
    - Se `wwwroot/css/produtos.css` definir `product-card__video`/`product-card__video-wrapper`: remover as regras
    - Se algum item de `wwwroot/data/products.json` contiver `videoUrl`: remover o campo
    - Se `.gitignore` contiver `*.mov`/`*.mp4` introduzidas para o recurso: remover as entradas
    - Não tocar em código, marcação ou estilos adjacentes não relacionados ao recurso de vídeo
    - _Bug_Condition: isBugCondition(input) — presença de qualquer artefato de vídeo (VideoUrl no modelo, iframe/product-card__video no cartão, CSS product-card__video*, videoUrl no JSON, *.mov/*.mp4 no .gitignore)_
    - _Expected_Behavior: expectedBehavior(result) — Property 1 do design: NOT isBugCondition(result) e cartão sem `<iframe>`/player_
    - _Preservation: Preservation Requirements do design — imagem/nome/descrição/preço/categoria, botão Amazon com atributos de segurança, favoritos, detalhes, filtros e estados_
    - _Requirements: 2.1, 2.2, 2.3_

  - [x] 3.2 Verificar que o teste de exploração da condição do bug passa
    - **Property 1: Expected Behavior** - Ausência total de artefatos de vídeo
    - **IMPORTANTE**: Re-executar o MESMO teste da tarefa 1 — não escrever um novo teste
    - O teste da tarefa 1 codifica o comportamento esperado; sua passagem confirma que nenhum artefato de vídeo permanece
    - **RESULTADO ESPERADO**: teste PASSA (confirma `NOT isBugCondition` e cartão sem `<iframe>`)
    - _Requirements: 2.1, 2.2, 2.3_

  - [x] 3.3 Verificar que os testes de preservação continuam passando
    - **Property 2: Preservation** - Comportamento do produto inalterado
    - **IMPORTANTE**: Re-executar os MESMOS testes da tarefa 2 — não escrever novos testes
    - Confirmar que todos os testes de preservação (unitários, baseados em propriedades e de integração) continuam passando (sem regressões)
    - **RESULTADO ESPERADO**: testes PASSAM (comportamento do produto inalterado)
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6_

- [x] 4. Checkpoint - Garantir que todos os testes passem
  - Executar a suíte completa (`dotnet test`) e garantir que todos os testes passem
  - Confirmar que a fase exploratória (Property 1) e a de preservação (Property 2) estão verdes
  - Em caso de dúvidas ou resultados inesperados, consultar o usuário antes de prosseguir
