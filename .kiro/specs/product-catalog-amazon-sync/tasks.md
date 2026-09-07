# Implementation Plan: product-catalog-amazon-sync

## Overview

Substituir o catálogo de demonstração do site institucional MaterDomus por dados reais dos produtos vendidos na Amazon Brasil (seller `A20TN3HCSY6KZV`). As mudanças abrangem: validação de integridade no cliente Blazor, novo serviço `CatalogMetaService`, ajuste de UI para exibir metadados de sincronização, arquivos estáticos de catálogo e guia de atualização editorial.

---

## Tasks

- [ ] 1. Adicionar validação de integridade ao `ProductCatalogService`
  - [x] 1.1 Implementar método estático `Validate` em `ProductCatalogService`
    - Criar método `private static IReadOnlyList<Product> Validate(List<Product>? raw)` conforme o design
    - Filtrar registros com `id` ou `name` nulos/vazios após trim (Req 6.1)
    - Filtrar registros com `price` ≤ 0 (Req 6.1, 2.7)
    - Filtrar registros com `imageUrl` nula ou vazia (Req 2.8)
    - Filtrar registros com `amazonUrl` que não corresponda ao padrão `^https://www\.amazon\.com\.br/dp/[A-Z0-9]{10}$` (Req 1.5, 3.1)
    - Descartar duplicatas de `id` mantendo o de menor índice e logando aviso via `Console.Warn` (Req 6.2)
    - Adicionar campo `private static readonly Regex AsinPattern` compilado
    - Adicionar método `internal static bool IsValidAmazonUrl(string? url)`
    - Invocar `Validate` em `GetProductsAsync` antes de atribuir ao `_cache`
    - _Requirements: 1.5, 2.7, 2.8, 3.1, 3.4, 6.1, 6.2, 6.3_

  - [x] 1.2 Escrever testes de propriedade FsCheck — Property 1 (Validate filtra exatamente os inválidos)
    - Criar `MaterDomus.Tests/Unit/ProductValidationTests.cs`
    - Implementar geradores `ValidProductGen()`, `InvalidProductGen()` (uma violação por instância) e `MixedProductListGen()`
    - **Property 1: Validate filtra exatamente os inválidos, preserva todos os válidos**
    - Anotar `[Property(MaxTest = 100)]`
    - Tag: `Feature: product-catalog-amazon-sync, Property 1`
    - _Requirements: 1.5, 2.7, 2.8, 6.1, 6.3_

  - [x] 1.3 Escrever testes de propriedade FsCheck — Property 2 (deduplicação preserva o primeiro)
    - Reusar `ProductValidationTests.cs`
    - **Property 2: Deduplicação preserva o registro de menor índice e descarta os demais**
    - Anotar `[Property(MaxTest = 100)]`
    - Tag: `Feature: product-catalog-amazon-sync, Property 2`
    - _Requirements: 6.2_

  - [x] 1.4 Escrever testes de propriedade FsCheck — Property 5 (Validate é idempotente)
    - Reusar `ProductValidationTests.cs`
    - **Property 5: `Validate(Validate(list))` ≡ `Validate(list)`**
    - Anotar `[Property(MaxTest = 100)]`
    - Tag: `Feature: product-catalog-amazon-sync, Property 5`
    - _Requirements: 6.1, 6.3_

  - [x] 1.5 Escrever testes de propriedade FsCheck — Property 3 (IsValidAmazonUrl fronteira do formato)
    - Criar `MaterDomus.Tests/Unit/AmazonUrlValidationTests.cs`
    - Implementar gerador `AmazonUrlGen()` cobrindo: formato correto, ASIN com 9 chars, ASIN com 11 chars, letras minúsculas no ASIN, parâmetros de query extras, esquemas diferentes, domínios diferentes, string vazia
    - **Property 3: `IsValidAmazonUrl` retorna `true` se e somente se a URL for exatamente `https://www.amazon.com.br/dp/[A-Z0-9]{10}`**
    - Anotar `[Property(MaxTest = 200)]`
    - Tag: `Feature: product-catalog-amazon-sync, Property 3`
    - _Requirements: 1.5, 3.1, 3.4_

  - [-] 1.6 Escrever testes de integração para os novos cenários de validação em `ProductCatalogServiceTests`
    - Adicionar casos em `MaterDomus.Tests/Integration/ProductCatalogServiceTests.cs`
    - Cenário: JSON com `amazonUrl` inválida → produto filtrado
    - Cenário: JSON com `price = 0` → produto filtrado
    - Cenário: JSON com `imageUrl` vazia → produto filtrado
    - Cenário: JSON com IDs duplicados → apenas o primeiro mantido
    - _Requirements: 1.5, 2.7, 2.8, 6.1, 6.2_

- [ ] 2. Criar modelo `CatalogMeta` e serviço `CatalogMetaService`
  - [x] 2.1 Criar record `CatalogMeta` em `Models/CatalogMeta.cs`
    - Campos: `CuratedAt` (string), `SourceUrl` (string), `ProductCount` (int)
    - Anotar cada campo com `[JsonPropertyName("curatedAt")]`, `[JsonPropertyName("sourceUrl")]`, `[JsonPropertyName("productCount")]`
    - _Requirements: 4.1, 4.2, 4.3, 4.4_

  - [x] 2.2 Criar interface `ICatalogMetaService` em `Services/ICatalogMetaService.cs`
    - Método `Task<CatalogMeta?> GetMetaAsync()`
    - _Requirements: 4.1, 4.5_

  - [x] 2.3 Implementar `CatalogMetaService` em `Services/CatalogMetaService.cs`
    - Injetar `HttpClient`
    - Fazer GET em `data/catalog-meta.json` via `_http.GetFromJsonAsync<CatalogMeta>`
    - Validar que `CuratedAt` e `SourceUrl` não são nulos/vazios e `ProductCount >= 0`; retornar `null` se inválido
    - Capturar todas as exceções (404, JSON inválido, rede) e retornar `null` sem propagar (Req 4.5)
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5_

  - [-] 2.4 Registrar `CatalogMetaService` no DI em `Program.cs`
    - Adicionar `builder.Services.AddSingleton<ICatalogMetaService, CatalogMetaService>()`
    - _Requirements: 4.1_

  - [-] 2.5 Escrever testes de propriedade FsCheck — Property 4 (CatalogMetaService nunca propaga exceção)
    - Criar `MaterDomus.Tests/Integration/CatalogMetaServiceTests.cs`
    - **Property 4: para qualquer resposta HTTP (404, 500, corpo vazio, JSON inválido, campos ausentes, `productCount` negativo, exceção de rede), `GetMetaAsync` retorna `null` sem lançar exceção**
    - Implementar gerador `HttpResponseGen()` com as variações listadas acima
    - Anotar `[Property(MaxTest = 100)]`
    - Tag: `Feature: product-catalog-amazon-sync, Property 4`
    - _Requirements: 4.5_

  - [-] 2.6 Escrever testes de integração xUnit para `CatalogMetaService` — exemplos determinísticos
    - Reusar `MaterDomus.Tests/Integration/CatalogMetaServiceTests.cs`
    - Cenário: resposta 200 com JSON válido → retorna `CatalogMeta` com campos corretos
    - Cenário: resposta 404 → retorna `null`
    - Cenário: JSON malformado → retorna `null`
    - Cenário: campos obrigatórios ausentes no JSON → retorna `null`
    - Cenário: `productCount` negativo → retorna `null`
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5_

- [~] 3. Checkpoint — Validar lógica de serviços
  - Garantir que todos os testes de `ProductValidationTests`, `AmazonUrlValidationTests` e `CatalogMetaServiceTests` passam.
  - Executar: `dotnet test MaterDomus.Tests/MaterDomus.Tests.csproj`
  - Perguntar ao usuário se surgirem dúvidas.

- [ ] 4. Atualizar `Produtos.razor` para consumir `CatalogMetaService`
  - [~] 4.1 Injetar `ICatalogMetaService` em `Produtos.razor` e carregar metadados
    - Adicionar `@inject ICatalogMetaService CatalogMetaService`
    - Adicionar campo `private CatalogMeta? _catalogMeta`
    - Carregar `_catalogMeta = await CatalogMetaService.GetMetaAsync()` em `OnInitializedAsync`, em paralelo com `LoadProductsAsync` (ou após)
    - _Requirements: 4.2, 4.3, 4.4, 4.5_

  - [~] 4.2 Renderizar seção de metadados de sincronização quando `_catalogMeta` não for `null`
    - Exibir `curatedAt`, `sourceUrl` e `productCount` em bloco com `@if (_catalogMeta is not null)`
    - Omitir completamente a seção quando `_catalogMeta` for `null` (sem mensagem de erro) (Req 4.5)
    - _Requirements: 4.2, 4.3, 4.4, 4.5_

  - [~] 4.3 Escrever testes bunit para `Produtos.razor` — seção de metadados
    - Criar `MaterDomus.Tests/Unit/ProdutosMetaRenderTests.cs`
    - Cenário: `CatalogMeta` válido → seção de metadados é renderizada com os valores corretos
    - Cenário: `CatalogMeta` nulo → seção de metadados não está presente no markup
    - _Requirements: 4.5_

- [ ] 5. Criar arquivos estáticos de catálogo e guia de atualização
  - [~] 5.1 Criar `wwwroot/data/catalog-meta.json` com dados reais da curadoria
    - Preencher `curatedAt` com a data da curadoria no formato `YYYY-MM-DD`
    - Preencher `sourceUrl` com `https://www.amazon.com.br/s?me=A20TN3HCSY6KZV&marketplaceID=A2Q3Y263D00KWC`
    - Preencher `productCount` com o número de produtos válidos que serão inseridos no `products.json`
    - _Requirements: 4.1, 4.2, 4.3, 4.4_

  - [~] 5.2 Atualizar `wwwroot/data/products.json` com produtos reais da Vitrine Amazon
    - Substituir completamente o arquivo existente (não patch incremental) (Req 7.2)
    - Cada produto deve ter `id`, `name` (1–100 chars), `description` (1–500 chars), `imageUrl`, `category` (um de "Organização", "Cozinha", "Casa", "Limpeza"), `price` (> 0), `amazonUrl` no formato `https://www.amazon.com.br/dp/[A-Z0-9]{10}` (Req 2.1–2.6, 3.1)
    - Verificar que cada ASIN responde HTTP 200 antes de incluir (Req 7.4)
    - Excluir produtos cujo link não responde HTTP 200 e registrar em relatório de validação (Req 7.5)
    - _Requirements: 1.1, 1.2, 1.3, 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 3.1, 7.2, 7.4, 7.5_

  - [~] 5.3 Criar diretório `wwwroot/images/products/` e adicionar imagens dos produtos
    - Armazenar cada imagem com nome `{id}.webp` (preferencial) ou `{id}.jpg` (Req 5.1)
    - Garantir largura máxima de 800 px e tamanho ≤ 200 KB por arquivo (Req 5.4)
    - Atualizar `imageUrl` de cada produto no `products.json` com caminho relativo `images/products/{id}.webp` ou `images/products/{id}.jpg` (Req 5.2)
    - Verificar que o arquivo `wwwroot/images/placeholder-product.png` existe (necessário para fallback do Req 5.3)
    - _Requirements: 2.4, 5.1, 5.2, 5.4_

  - [~] 5.4 Criar `wwwroot/data/CATALOG_UPDATE.md` com guia de atualização editorial
    - Incluir obrigatoriamente as seções: **Pré-requisitos**, **Substituição do products.json**, **Substituição das imagens**, **Procedimento de redeploy** (Req 7.1)
    - Mencionar o processo de verificação HTTP 200 dos ASINs antes de incluir (Req 7.4)
    - Mencionar que a substituição é sempre completa (nunca patch incremental) (Req 7.2)
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5_

- [~] 6. Checkpoint final — Garantir que todos os testes passam
  - Executar `dotnet test MaterDomus.Tests/MaterDomus.Tests.csproj`
  - Verificar que os produtos reais aparecem na vitrine após reload da página
  - Perguntar ao usuário se surgirem dúvidas.

---

## Notes

- Tasks marcadas com `*` são opcionais e podem ser puladas para entrega mais rápida do MVP
- Cada task referencia os requisitos específicos para rastreabilidade
- As Properties 1–5 mapeiam diretamente para as propriedades de correção definidas em `design.md`
- O `ProductCatalogService` existente já trata falhas de rede e JSON inválido — a task 1.1 apenas adiciona a chamada a `Validate` antes de cachear
- `CatalogMetaService` deve ser registrado como `Singleton` (análogo ao `ProductCatalogService`), pois o arquivo de meta não muda durante a sessão
- A seção de metadados em `Produtos.razor` deve ser renderizada sem alterar a lógica de filtro ou grid existentes
- O fallback de imagem via `onerror` já está implementado em `ProductCard.razor` — nenhuma mudança necessária nesse componente
- Tasks 5.2 e 5.3 são tarefas editoriais (curadoria manual); uma automação via API Amazon está fora do escopo deste design

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "2.1", "2.2"] },
    { "id": 1, "tasks": ["1.2", "1.3", "1.4", "1.5", "2.3"] },
    { "id": 2, "tasks": ["1.6", "2.4", "2.5", "2.6"] },
    { "id": 3, "tasks": ["4.1", "5.1"] },
    { "id": 4, "tasks": ["4.2", "5.2"] },
    { "id": 5, "tasks": ["4.3", "5.3"] },
    { "id": 6, "tasks": ["5.4"] }
  ]
}
```
