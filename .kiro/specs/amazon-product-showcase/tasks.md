# Plano de Implementação: amazon-product-showcase

## Visão Geral

Implementação incremental da Vitrine de Produtos MaterDomus em Blazor WebAssembly (.NET 9), partindo do modelo de dados, passando pelos serviços, componentes e assets estáticos, até o projeto de testes. Cada etapa integra o que foi construído nas anteriores, garantindo que nenhum código fique órfão.

## Tarefas

- [x] 1. Criar o modelo de dados `Product`
  - Criar `Models/Product.cs` com o record `Product(string Id, string Name, string Description, string ImageUrl, string Category, decimal Price, string AmazonUrl)` conforme definido no design
  - Usar namespace `MaterDomus.Web.Models`
  - _Requisitos: 1.3, 1.4, 2.1, 2.4_

- [x] 2. Implementar os serviços de catálogo e favoritos
  - [x] 2.1 Criar `Services/IProductCatalogService.cs` com a interface `IProductCatalogService` (método `Task<IReadOnlyList<Product>> GetProductsAsync()`)
    - Usar namespace `MaterDomus.Web.Services`
    - _Requisitos: 1.1, 1.2, 1.6_

  - [x] 2.2 Criar `Services/ProductCatalogService.cs` com a classe `ProductCatalogService : IProductCatalogService`
    - Receber `HttpClient` via construtor
    - Implementar cache em memória com `_cache` (`IReadOnlyList<Product>?`)
    - Carregar `data/products.json` via `GetFromJsonAsync`; capturar exceções no `try/catch` e retornar lista vazia em caso de falha
    - _Requisitos: 1.1, 1.2, 1.6_

  - [x] 2.3 Escrever testes unitários para `ProductCatalogService`
    - Cobrir: carregamento bem-sucedido retorna lista, segunda chamada usa cache, JSON inválido retorna lista vazia
    - Arquivo: `MaterDomus.Tests/Integration/ProductCatalogServiceTests.cs`
    - _Requisitos: 1.1, 1.2_

  - [x] 2.4 Criar `Services/FavoritesService.cs` com a classe `FavoritesService`
    - Implementar `HashSet<string> _favorites`, propriedades `Favorites`, `Count`, evento `OnChanged` e métodos `IsFavorite(string)` e `Toggle(string)` conforme design
    - _Requisitos: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6_

  - [x] 2.5 Escrever teste de propriedade para `FavoritesService` — toggle round-trip
    - **Propriedade 5: Toggle round-trip**
    - **Valida: Requisitos 3.2, 3.3**
    - Arquivo: `MaterDomus.Tests/Unit/FavoritesServiceTests.cs`

  - [x] 2.6 Escrever teste de propriedade para `FavoritesService` — contagem exata
    - **Propriedade 6: Contagem exata de favoritos**
    - **Valida: Requisito 3.4**
    - Arquivo: `MaterDomus.Tests/Unit/FavoritesServiceTests.cs`

  - [x] 2.7 Escrever teste de propriedade para `FavoritesService` — filtro retorna subconjunto exato
    - **Propriedade 7: Filtro de favoritos retorna subconjunto exato**
    - **Valida: Requisito 3.6**
    - Arquivo: `MaterDomus.Tests/Unit/FavoritesServiceTests.cs`

- [x] 3. Registrar serviços em `Program.cs`
  - Adicionar `builder.Services.AddSingleton<IProductCatalogService, ProductCatalogService>()` e `builder.Services.AddScoped<FavoritesService>()` no `Program.cs` existente
  - Adicionar os `using` necessários (`MaterDomus.Web.Services`)
  - _Requisitos: 1.1, 3.1_

- [x] 4. Criar os dados estáticos e assets
  - [x] 4.1 Criar `wwwroot/data/products.json` com ao menos 6 produtos realistas para MaterDomus nas categorias Organização, Cozinha, Limpeza e Casa
    - Cada produto deve ter todos os campos do record `Product` preenchidos, incluindo `amazonUrl` válido com domínio `amazon.com.br`
    - _Requisitos: 1.1, 2.3_

  - [x] 4.2 Criar `wwwroot/css/produtos.css` com:
    - Grid responsivo: 1 coluna `< 600px`, 2 colunas entre `600px` e `1023px`, 3+ colunas `>= 1024px` usando `CSS Grid` e `@media`
    - Estilos dos cards (sombra, borda arredondada, hover), botão "Comprar na Amazon" e ícone de favorito
    - Usar CSS custom properties consistentes com o estilo existente do projeto
    - _Requisitos: 5.3_

  - [x] 4.3 Adicionar comentário/placeholder em `wwwroot/images/` indicando que o arquivo `placeholder-product.png` deve ser adicionado manualmente
    - Criar um arquivo de texto `wwwroot/images/README-placeholder.txt` com instrução para adicionar `placeholder-product.png`
    - _Requisitos: 1.5_

- [x] 5. Implementar o componente `ProductCard.razor`
  - [x] 5.1 Criar `Shared/ProductCard.razor` com parâmetro `[Parameter] public Product Product { get; set; }`
    - Renderizar imagem com `onerror` apontando para `images/placeholder-product.png`
    - Exibir nome, categoria e preço formatado em `pt-BR` ("R$ X,XX")
    - Exibir descrição truncada a 120 chars + "..." se necessário
    - Exibir botão "Comprar na Amazon" com `target="_blank"` condicionalmente (`@if (!string.IsNullOrEmpty(Product.AmazonUrl))`)
    - Exibir botão de favorito com `role="article"` no card, `aria-pressed` e `aria-label` corretos, usando `FavoritesService` via `[Inject]`
    - Adicionar link `href` para `produtos.css` na página (a referência ao CSS será feita no `Produtos.razor`)
    - _Requisitos: 1.3, 1.4, 1.5, 2.1, 2.2, 2.4, 3.1, 3.2, 3.3, 5.4, 5.5, 5.6, 5.7_

  - [x] 5.2 Escrever teste de propriedade para truncagem de descrição
    - **Propriedade 2: Truncagem de descrição**
    - **Valida: Requisito 1.4**
    - Arquivo: `MaterDomus.Tests/Unit/DescriptionTruncationTests.cs`

  - [x] 5.3 Escrever teste de propriedade para formatação de preço
    - **Propriedade 3: Formatação de preço**
    - **Valida: Requisito 1.3**
    - Arquivo: `MaterDomus.Tests/Unit/PriceFormattingTests.cs`

  - [x] 5.4 Escrever teste de propriedade para visibilidade do botão Amazon
    - **Propriedade 4: Visibilidade do botão Amazon**
    - **Valida: Requisitos 2.1, 2.4**
    - Arquivo: `MaterDomus.Tests/Unit/ProductCardRenderTests.cs`

  - [x] 5.5 Escrever teste de propriedade para atributos ARIA do botão de favorito
    - **Propriedade 11: Atributos ARIA do botão de favorito**
    - **Valida: Requisitos 5.4, 5.6, 5.7**
    - Arquivo: `MaterDomus.Tests/Unit/ProductCardRenderTests.cs`

- [x] 6. Implementar a página `Produtos.razor`
  - [x] 6.1 Criar `Pages/Produtos.razor` com diretiva `@page "/produtos"`
    - Injetar `IProductCatalogService` e `FavoritesService`
    - Implementar estado interno: `_allProducts`, `_isLoading`, `_hasError`, `_searchText`, `_selectedCategory`, `_showFavoritesOnly`
    - Implementar `OnInitializedAsync` com timeout de 10s via `CancellationTokenSource` conforme design
    - Implementar `FilteredProducts` como propriedade computada com LINQ (busca case-insensitive, categoria, favoritos)
    - Renderizar barra de filtros: campo de busca, dropdown de categorias derivado de `_allProducts`, toggle de favoritos
    - Renderizar grade de `ProductCard` usando `FilteredProducts`
    - Exibir estados: loading spinner, mensagem de erro + botão "Tentar novamente", catálogo vazio, sem resultados de filtro, sem favoritos
    - Exibir indicador de contagem de favoritos quando `FavoritesService.Count > 0`
    - Inscrever-se no evento `FavoritesService.OnChanged` para `StateHasChanged`; desinscrever no `Dispose`
    - Adicionar `<link rel="stylesheet" href="css/produtos.css" />` no `<HeadContent>`
    - _Requisitos: 1.1, 1.2, 1.6, 3.4, 3.6, 3.7, 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 5.1_

  - [x] 6.2 Escrever teste de propriedade para catálogo completo sem filtros
    - **Propriedade 1: Catálogo completo sem filtros**
    - **Valida: Requisitos 1.1, 4.6**
    - Arquivo: `MaterDomus.Tests/Unit/ProductFilterTests.cs`

  - [x] 6.3 Escrever teste de propriedade para filtro de texto
    - **Propriedade 8: Filtro de texto — correção do resultado**
    - **Valida: Requisito 4.2**
    - Arquivo: `MaterDomus.Tests/Unit/ProductFilterTests.cs`

  - [x] 6.4 Escrever teste de propriedade para filtro de categoria
    - **Propriedade 9: Filtro de categoria — correção do resultado**
    - **Valida: Requisito 4.3**
    - Arquivo: `MaterDomus.Tests/Unit/ProductFilterTests.cs`

  - [x] 6.5 Escrever teste de propriedade para composição de filtros
    - **Propriedade 10: Composição de filtros (interseção)**
    - **Valida: Requisito 4.4**
    - Arquivo: `MaterDomus.Tests/Unit/ProductFilterTests.cs`

- [x] 7. Checkpoint — Compilar a solução principal e verificar erros
  - Garantir que `dotnet build MaterDomus.Web.csproj` compila sem erros; resolver qualquer problema antes de prosseguir.

- [x] 8. Atualizar a navegação em `Shared/MainLayout.razor`
  - Adicionar `<NavLink href="/produtos">Produtos</NavLink>` após o NavLink "Início" no menu de navegação existente
  - _Requisitos: 5.1, 5.2_

- [x] 9. Configurar o projeto de testes `MaterDomus.Tests`
  - [x] 9.1 Criar o projeto `MaterDomus.Tests/MaterDomus.Tests.csproj` com:
    - `TargetFramework net9.0`
    - Pacotes: `xunit` (2.9.x), `xunit.runner.visualstudio` (2.8.x), `FsCheck` (3.x), `FsCheck.Xunit` (3.x), `bunit` (1.x), `Microsoft.NET.Test.Sdk` (17.x)
    - Referência ao projeto `MaterDomus.Web.csproj`
    - Criar `MaterDomus.Tests.sln` ou adicionar ao `MaterDomus.sln`
  - _Requisitos: (infraestrutura de testes)_

  - [x] 9.2 Criar os arquivos de teste listados nas tarefas 2.3, 2.5, 2.6, 2.7, 5.2, 5.3, 5.4, 5.5, 6.2, 6.3, 6.4, 6.5 conforme a estrutura do design:
    - `MaterDomus.Tests/Unit/ProductFilterTests.cs`
    - `MaterDomus.Tests/Unit/DescriptionTruncationTests.cs`
    - `MaterDomus.Tests/Unit/PriceFormattingTests.cs`
    - `MaterDomus.Tests/Unit/FavoritesServiceTests.cs`
    - `MaterDomus.Tests/Unit/ProductCardRenderTests.cs`
    - `MaterDomus.Tests/Integration/ProductCatalogServiceTests.cs`
    - Cada propriedade deve ter a tag de comentário: `// Feature: amazon-product-showcase, Property {N}: {texto}`
    - Cada teste de propriedade deve executar no mínimo 100 iterações via `FsCheck`
    - _Requisitos: todos (cobertura de testes)_

- [x] 10. Checkpoint final — Executar todos os testes e verificar cobertura
  - Garantir que `dotnet test` no projeto `MaterDomus.Tests` passa sem falhas; resolver qualquer problema antes de finalizar.

## Notas

- Tarefas marcadas com `*` são opcionais e podem ser puladas para um MVP mais rápido
- Cada tarefa referencia requisitos específicos para rastreabilidade
- Os checkpoints garantem validação incremental
- Os testes de propriedade validam correção universal usando FsCheck com mínimo de 100 iterações
- Os testes unitários com exemplos cobrem casos de borda e integrações específicas
- O projeto de testes usa bUnit para renderização de componentes Blazor
- `FavoritesService` usa `AddScoped` — em WASM isso equivale a singleton por aba; favoritos resetam ao recarregar (Requisito 3.5)

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1"] },
    { "id": 1, "tasks": ["2.1", "2.4"] },
    { "id": 2, "tasks": ["2.2"] },
    { "id": 3, "tasks": ["2.3", "2.5", "2.6", "2.7", "3"] },
    { "id": 4, "tasks": ["4.1", "4.2", "4.3", "9.1"] },
    { "id": 5, "tasks": ["5.1"] },
    { "id": 6, "tasks": ["5.2", "5.3", "5.4", "5.5", "6.1"] },
    { "id": 7, "tasks": ["6.2", "6.3", "6.4", "6.5", "8"] },
    { "id": 8, "tasks": ["9.2"] }
  ]
}
```
