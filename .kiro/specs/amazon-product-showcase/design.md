# Documento de Design: amazon-product-showcase

## Visão Geral

A Vitrine de Produtos é uma nova página do site institucional da MaterDomus que exibe o catálogo de produtos vendidos no marketplace da Amazon. O usuário pode navegar pelo catálogo, buscar por texto, filtrar por categoria, favoritar produtos durante a sessão e ser redirecionado à Amazon para efetuar a compra.

A implementação é inteiramente client-side: Blazor WebAssembly (WASM) com .NET 9, sem backend próprio. Os produtos são servidos como dados estáticos (uma lista C# hardcoded ou um arquivo JSON carregado via `HttpClient` do diretório `wwwroot`). Nenhuma chamada a APIs externas é feita para dados de produto; o link Amazon é uma URL direta.

### Decisões de design relevantes

| Decisão | Escolha | Justificativa |
|---|---|---|
| Fonte de dados | JSON estático em `wwwroot` carregado via `HttpClient` | Permite atualizar o catálogo sem recompilar; compatível com hospedagem estática (Azure Static Web Apps) |
| Estado de favoritos | `FavoritesService` com `HashSet<string>` (scoped) | Sem necessidade de persistência; escopo scoped garante reset ao recarregar a página |
| Serviço de catálogo | `ProductCatalogService` singleton com cache em memória | O catálogo não muda durante a sessão; singleton evita múltiplos carregamentos do JSON |
| Filtragem | Computada em memória no componente `Produtos.razor` | Catálogo pequeno/médio; lógica simples e sem necessidade de servidor |
| CSS | Plain CSS com CSS custom properties e media queries | Consistente com o projeto existente (sem Bootstrap, sem Tailwind) |

---

## Arquitetura

```mermaid
graph TD
    subgraph "Blazor WASM Client"
        ML[MainLayout.razor<br/>+ NavLink /produtos]
        P[Produtos.razor<br/>@page /produtos]
        PC[ProductCard.razor]
        FS[FavoritesService<br/>scoped]
        PCS[ProductCatalogService<br/>singleton]
        HC[HttpClient]
        JSON[(wwwroot/data/products.json)]
    end

    ML --> P
    P --> PC
    P --> FS
    P --> PCS
    PCS --> HC
    HC --> JSON
    PC --> FS
```

**Fluxo de dados:**
1. `ProductCatalogService` carrega `products.json` via `HttpClient` na primeira chamada (lazy load com cache).
2. `Produtos.razor` obtém a lista completa e aplica os filtros ativos (busca, categoria, favoritos) via LINQ, produzindo a lista filtrada.
3. Cada `ProductCard.razor` recebe um `Product` como parâmetro e lê/escreve no `FavoritesService` para o estado de favorito.
4. `FavoritesService` dispara um evento `OnChanged` para que `Produtos.razor` possa atualizar o contador de favoritos.

---

## Componentes e Interfaces

### `ProductCatalogService`

```csharp
public interface IProductCatalogService
{
    Task<IReadOnlyList<Product>> GetProductsAsync();
}

public class ProductCatalogService : IProductCatalogService
{
    private readonly HttpClient _http;
    private IReadOnlyList<Product>? _cache;

    public ProductCatalogService(HttpClient http) { _http = http; }

    public async Task<IReadOnlyList<Product>> GetProductsAsync()
    {
        if (_cache is not null) return _cache;
        _cache = await _http.GetFromJsonAsync<List<Product>>("data/products.json")
                 ?? new List<Product>();
        return _cache;
    }
}
```

Registro em `Program.cs`: `builder.Services.AddSingleton<IProductCatalogService, ProductCatalogService>();`

---

### `FavoritesService`

```csharp
public class FavoritesService
{
    private readonly HashSet<string> _favorites = new();

    public IReadOnlyCollection<string> Favorites => _favorites;
    public int Count => _favorites.Count;

    public event Action? OnChanged;

    public bool IsFavorite(string productId) => _favorites.Contains(productId);

    public void Toggle(string productId)
    {
        if (!_favorites.Remove(productId))
            _favorites.Add(productId);
        OnChanged?.Invoke();
    }
}
```

Registro em `Program.cs`: `builder.Services.AddScoped<FavoritesService>();`

> **Nota de escopo:** `AddScoped` em WASM equivale a singleton por sessão de browser tab. Ao recarregar a página, a instância é recriada — garantindo o requisito de reset de favoritos (Requisito 3.5).

---

### `Produtos.razor` (`@page "/produtos"`)

Responsabilidades:
- Carregar o catálogo via `IProductCatalogService` no `OnInitializedAsync`.
- Manter o estado dos filtros: `searchText`, `selectedCategory`, `showFavoritesOnly`.
- Calcular `filteredProducts` como propriedade computada (LINQ sobre o catálogo).
- Exibir barra de filtros, grade de `ProductCard`, estados de loading/empty.
- Exibir o contador de favoritos quando `FavoritesService.Count > 0`.

**Estado do componente:**
```csharp
private IReadOnlyList<Product> _allProducts = Array.Empty<Product>();
private bool _isLoading = true;
private bool _hasError = false;
private string _searchText = "";
private string _selectedCategory = "";
private bool _showFavoritesOnly = false;

private IEnumerable<Product> FilteredProducts =>
    _allProducts
        .Where(p => string.IsNullOrEmpty(_searchText) ||
                    p.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                    p.Description.Contains(_searchText, StringComparison.OrdinalIgnoreCase))
        .Where(p => string.IsNullOrEmpty(_selectedCategory) ||
                    p.Category == _selectedCategory)
        .Where(p => !_showFavoritesOnly || FavoritesService.IsFavorite(p.Id));
```

---

### `ProductCard.razor`

Parâmetros:
```csharp
[Parameter] public Product Product { get; set; } = default!;
```

Responsabilidades:
- Renderizar imagem (com fallback), nome (truncado a 100 chars se necessário, embora o modelo de dados já restrinja o campo), descrição (truncada a 120 chars + "..."), categoria, preço formatado.
- Exibir botão "Comprar na Amazon" condicionalmente.
- Exibir/alternar o ícone de favorito com os atributos ARIA corretos.
- Aplicar `role="article"` e `aria-label` com o nome do produto.

---

## Modelos de Dados

### `Product`

```csharp
public record Product(
    string Id,           // GUID ou slug único
    string Name,         // máx. 100 caracteres
    string Description,  // texto livre; exibição trunca a 120 chars
    string ImageUrl,     // URL relativa (wwwroot/images/) ou absoluta; vazio = fallback
    string Category,     // ex: "Organização", "Cozinha", "Limpeza"
    decimal Price,       // preço de referência; formato "R$ 0,00" na UI
    string AmazonUrl     // URL amazon.com.br; vazio = oculta botão
);
```

### `products.json` (exemplo)

```json
[
  {
    "id": "porta-talher-001",
    "name": "Porta-Talheres Organizador 6 Divisórias",
    "description": "Organizador de gaveta em plástico resistente com 6 divisórias ajustáveis. Ideal para talheres, utensílios de cozinha e acessórios.",
    "imageUrl": "images/products/porta-talher-001.jpg",
    "category": "Organização",
    "price": 34.90,
    "amazonUrl": "https://www.amazon.com.br/dp/XXXXX"
  }
]
```

### `FilterState` (estado inline em `Produtos.razor`, não uma classe separada)

| Campo | Tipo | Valor inicial |
|---|---|---|
| `_searchText` | `string` | `""` |
| `_selectedCategory` | `string` | `""` (= sem filtro) |
| `_showFavoritesOnly` | `bool` | `false` |

---

## Propriedades de Correção

*Uma propriedade é uma característica ou comportamento que deve ser verdadeiro em todas as execuções válidas do sistema — essencialmente, uma afirmação formal sobre o que o sistema deve fazer. Propriedades servem como ponte entre especificações legíveis por humanos e garantias de correção verificáveis por máquina.*

---

### Propriedade 1: Catálogo completo sem filtros

*Para qualquer* lista não-vazia de produtos com todos os filtros desativados (searchText vazio, nenhuma categoria selecionada, showFavoritesOnly = false), o conjunto de produtos exibidos deve ser igual ao catálogo completo.

**Valida: Requisito 1.1, Requisito 4.6**

---

### Propriedade 2: Truncagem de descrição

*Para qualquer* string de descrição, a função de truncagem deve: (a) retornar os primeiros 120 caracteres seguidos de "..." se o comprimento for maior que 120; (b) retornar a string original sem modificação se o comprimento for menor ou igual a 120.

**Valida: Requisito 1.4**

---

### Propriedade 3: Formatação de preço

*Para qualquer* valor decimal não-negativo de preço, a função de formatação deve produzir uma string no formato "R$ X,XX" onde X,XX é o valor com separador decimal vírgula e separador de milhar ponto (cultura `pt-BR`).

**Valida: Requisito 1.3**

---

### Propriedade 4: Visibilidade do botão Amazon

*Para qualquer* produto, o botão "Comprar na Amazon" deve ser visível se e somente se o campo `AmazonUrl` for uma string não-vazia.

**Valida: Requisito 2.1, Requisito 2.4**

---

### Propriedade 5: Toggle de favorito — round-trip

*Para qualquer* produto e qualquer estado inicial de favorito, chamar `Toggle` duas vezes consecutivas deve restaurar o estado original (idempotência do duplo toggle).

**Valida: Requisito 3.2, Requisito 3.3**

---

### Propriedade 6: Contagem exata de favoritos

*Para qualquer* sequência de operações `Toggle` sobre produtos distintos, a contagem exibida (`FavoritesService.Count`) deve ser igual ao número de produtos atualmente favoritados (cardinalidade do conjunto `_favorites`).

**Valida: Requisito 3.4**

---

### Propriedade 7: Filtro de favoritos retorna subconjunto exato

*Para qualquer* lista de produtos e qualquer subconjunto de IDs marcados como favoritos, ativar o filtro `showFavoritesOnly` deve retornar exatamente os produtos cujo `Id` está no conjunto de favoritos — nem mais, nem menos.

**Valida: Requisito 3.6**

---

### Propriedade 8: Filtro de texto — correção do resultado

*Para qualquer* lista de produtos e qualquer string de busca, o conjunto filtrado deve ser exatamente `{p ∈ products | p.Name.Contains(term, OrdinalIgnoreCase) || p.Description.Contains(term, OrdinalIgnoreCase)}`.

**Valida: Requisito 4.2**

---

### Propriedade 9: Filtro de categoria — correção do resultado

*Para qualquer* lista de produtos e qualquer string de categoria, o conjunto filtrado deve ser exatamente `{p ∈ products | p.Category == selectedCategory}`.

**Valida: Requisito 4.3**

---

### Propriedade 10: Composição de filtros (interseção)

*Para qualquer* lista de produtos, string de busca e categoria selecionada (ambos ativos), o resultado do filtro combinado deve ser igual à interseção do resultado do filtro de texto com o resultado do filtro de categoria aplicados independentemente.

**Valida: Requisito 4.4**

---

### Propriedade 11: Atributos ARIA do botão de favorito

*Para qualquer* produto e qualquer estado de favorito (favoritado ou não), o botão de favorito renderizado deve ter `aria-pressed="true"` e `aria-label="Remover dos favoritos"` quando favoritado, e `aria-pressed="false"` e `aria-label="Favoritar produto"` quando não favoritado.

**Valida: Requisito 5.4, Requisito 5.6, Requisito 5.7**

---

## Tratamento de Erros

| Cenário | Comportamento |
|---|---|
| Falha ao carregar `products.json` (HTTP error / timeout) | `_hasError = true`; exibe mensagem de erro + botão "Tentar novamente" que chama `LoadProductsAsync()` novamente |
| `products.json` retorna array vazio `[]` | `_allProducts` vazio; exibe mensagem "Nenhum produto disponível no momento" |
| `ImageUrl` vazia ou nula | Exibe `images/placeholder-product.png` via `onerror` no `<img>` + atributo `src` fallback |
| `AmazonUrl` vazio ou nulo | Oculta o botão "Comprar na Amazon"; layout do card não deixa espaço em branco (CSS `display: none` / condicional Razor `@if`) |
| Nenhum produto corresponde aos filtros | Exibe mensagem contextual: "Nenhum produto encontrado para a busca" ou "Nenhum produto favoritado" |
| JSON malformado / desserialização falha | Capturado no `try/catch` em `GetProductsAsync`; retorna lista vazia e loga no console |

**Implementação do loading com timeout:**

```csharp
// Em Produtos.razor — OnInitializedAsync
private CancellationTokenSource _cts = new();

protected override async Task OnInitializedAsync()
{
    _isLoading = true;
    try
    {
        var timeout = Task.Delay(TimeSpan.FromSeconds(10), _cts.Token);
        var load = LoadProductsAsync();
        if (await Task.WhenAny(load, timeout) == timeout)
        {
            _hasError = true;
        }
    }
    catch (Exception)
    {
        _hasError = true;
    }
    finally
    {
        _isLoading = false;
    }
}
```

---

## Estratégia de Testes

### Abordagem dual

A estratégia combina testes baseados em exemplos (cobrindo casos específicos, edge cases e integrações) com testes baseados em propriedades (cobrindo correção universal das funções puras de filtragem, formatação e estado).

### Testes baseados em propriedades

Biblioteca recomendada: **[FsCheck](https://fscheck.github.io/FsCheck/)** (versão 3.x) — madura, bem integrada com .NET e xUnit/NUnit, suporta geradores customizados para tipos de domínio.

Cada teste de propriedade deve executar no mínimo **100 iterações**.

Tag de cada teste:
```
// Feature: amazon-product-showcase, Property {N}: {texto da propriedade}
```

| Propriedade | Tipo de teste | Geradores necessários |
|---|---|---|
| Prop 1: Catálogo completo sem filtros | Property | `Gen.listOf(Arb.product)`, estado de filtro vazio |
| Prop 2: Truncagem de descrição | Property | `Arb.string` (qualquer comprimento) |
| Prop 3: Formatação de preço | Property | `Gen.decimal` não-negativo |
| Prop 4: Visibilidade botão Amazon | Property | `Arb.product` com AmazonUrl varia entre vazio e não-vazio |
| Prop 5: Toggle round-trip | Property | `Arb.productId`, `Arb.bool` (estado inicial) |
| Prop 6: Contagem de favoritos | Property | Sequências de `Toggle` sobre listas de IDs |
| Prop 7: Filtro de favoritos | Property | `Gen.listOf(Arb.product)`, subconjunto de IDs favoritos |
| Prop 8: Filtro de texto | Property | `Gen.listOf(Arb.product)`, `Arb.string` como searchTerm |
| Prop 9: Filtro de categoria | Property | `Gen.listOf(Arb.product)`, categoria gerada da lista |
| Prop 10: Composição de filtros | Property | `Gen.listOf(Arb.product)`, searchTerm + categoria |
| Prop 11: ARIA do favorito | Property | `Arb.product`, `Arb.bool` (isFavorite) |

### Testes baseados em exemplos (xUnit)

| Cenário | Tipo |
|---|---|
| Catálogo vazio → mensagem de estado vazio | Exemplo / edge case |
| ImageUrl vazia → imagem placeholder | Edge case |
| AmazonUrl vazia → botão oculto | Edge case |
| Filtros ativos + sem resultados → mensagem "nenhum produto encontrado" | Edge case |
| Filtro de favoritos + favoritos vazios → mensagem "nenhum favoritado" | Edge case |
| NavLink "Produtos" presente no MainLayout | Exemplo |
| Botão Amazon abre `target="_blank"` com o URL correto | Exemplo |
| Toggle em produto não-favoritado → adiciona ao set | Exemplo |
| Toggle em produto favoritado → remove do set | Exemplo |

### Testes de fumaça (smoke)

- `@page "/produtos"` existe em `Produtos.razor`
- `FavoritesService` inicializa com conjunto vazio
- Todos os produtos do catálogo têm `AmazonUrl` com domínio `amazon.com.br`
- CSS contém media queries para 600px e 1024px

### Estrutura sugerida do projeto de testes

```
MaterDomus.Tests/
├── Unit/
│   ├── ProductFilterTests.cs          // Props 1, 8, 9, 10 (lógica de filtragem pura)
│   ├── DescriptionTruncationTests.cs  // Prop 2
│   ├── PriceFormattingTests.cs        // Prop 3
│   ├── FavoritesServiceTests.cs       // Props 5, 6, 7
│   └── ProductCardRenderTests.cs      // Props 4, 11 (bunit ou render tests)
└── Integration/
    └── ProductCatalogServiceTests.cs  // Carregamento do JSON, smoke tests
```

> **Nota:** O projeto de testes deverá referenciar `MaterDomus.Web` e incluir `FsCheck` + `FsCheck.Xunit` + `bUnit` (para testes de componente Blazor) como dependências.
