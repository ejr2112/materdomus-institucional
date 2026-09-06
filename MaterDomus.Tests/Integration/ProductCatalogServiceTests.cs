// Feature: amazon-product-showcase

using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using MaterDomus.Web.Models;
using MaterDomus.Web.Services;
using Xunit;

namespace MaterDomus.Tests.Integration;

/// <summary>
/// Testes unitários para <see cref="ProductCatalogService"/>.
/// Cobre: carregamento bem-sucedido, cache na segunda chamada e JSON inválido.
/// Requisitos: 1.1, 1.2
/// </summary>
public class ProductCatalogServiceTests
{
    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Stub de <see cref="HttpMessageHandler"/> que retorna uma resposta fixa.
    /// </summary>
    private sealed class StubHttpHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;
        public int CallCount { get; private set; }

        public StubHttpHandler(HttpResponseMessage response)
        {
            _response = response;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(_response);
        }
    }

    /// <summary>
    /// Cria um <see cref="HttpClient"/> que retorna o JSON informado com status 200.
    /// </summary>
    private static (HttpClient Client, StubHttpHandler Handler) CreateHttpClient(string jsonContent)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
        };
        var handler = new StubHttpHandler(response);
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        return (client, handler);
    }

    /// <summary>
    /// Cria um <see cref="HttpClient"/> cujo handler lança uma <see cref="HttpRequestException"/>.
    /// </summary>
    private static HttpClient CreateFailingHttpClient()
    {
        var handler = new ThrowingHttpHandler();
        return new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
    }

    private sealed class ThrowingHttpHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            throw new HttpRequestException("Falha simulada de rede");
        }
    }

    // ---------------------------------------------------------------------------
    // Dados de teste
    // ---------------------------------------------------------------------------

    private static readonly string ValidProductsJson = JsonSerializer.Serialize(new[]
    {
        new
        {
            id = "prod-001",
            name = "Organizador de Gaveta",
            description = "Organizador com 6 divisórias ajustáveis.",
            imageUrl = "images/products/prod-001.jpg",
            category = "Organização",
            price = 34.90m,
            amazonUrl = "https://www.amazon.com.br/dp/AAAAAAAAAA"  // 10-char ASIN (Req 3.1)
        },
        new
        {
            id = "prod-002",
            name = "Porta-Temperos",
            description = "Porta-temperos giratório para bancada.",
            imageUrl = "images/products/prod-002.jpg",
            category = "Cozinha",
            price = 59.90m,
            amazonUrl = "https://www.amazon.com.br/dp/BBBBBBBBBB"  // 10-char ASIN (Req 3.1)
        }
    });

    // ---------------------------------------------------------------------------
    // Testes
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Requisito 1.1 — Carregamento bem-sucedido deve retornar a lista de produtos
    /// com os dados corretos deserializados do JSON.
    /// </summary>
    [Fact]
    public async Task GetProductsAsync_SuccessfulResponse_ReturnsPopulatedList()
    {
        // Arrange
        var (client, _) = CreateHttpClient(ValidProductsJson);
        var service = new ProductCatalogService(client);

        // Act
        var products = await service.GetProductsAsync();

        // Assert
        Assert.NotNull(products);
        Assert.Equal(2, products.Count);

        var first = products[0];
        Assert.Equal("prod-001", first.Id);
        Assert.Equal("Organizador de Gaveta", first.Name);
        Assert.Equal("Organização", first.Category);
        Assert.Equal(34.90m, first.Price);
        Assert.Equal("https://www.amazon.com.br/dp/AAAAAAAAAA", first.AmazonUrl);

        var second = products[1];
        Assert.Equal("prod-002", second.Id);
        Assert.Equal("Cozinha", second.Category);
    }

    /// <summary>
    /// Requisito 1.2 — A segunda chamada deve retornar o resultado do cache sem
    /// acionar o <see cref="HttpClient"/> novamente.
    /// </summary>
    [Fact]
    public async Task GetProductsAsync_SecondCall_ReturnsCachedResult()
    {
        // Arrange
        var (client, handler) = CreateHttpClient(ValidProductsJson);
        var service = new ProductCatalogService(client);

        // Act
        var firstCall = await service.GetProductsAsync();
        var secondCall = await service.GetProductsAsync();

        // Assert — mesma referência em memória (cache)
        Assert.Same(firstCall, secondCall);

        // Assert — HttpClient chamado exatamente uma vez
        Assert.Equal(1, handler.CallCount);
    }

    /// <summary>
    /// Requisito 1.1 / tratamento de erro — JSON inválido deve ser capturado e
    /// retornar uma lista vazia em vez de propagar a exceção.
    /// </summary>
    [Fact]
    public async Task GetProductsAsync_InvalidJson_ReturnsEmptyList()
    {
        // Arrange
        var (client, _) = CreateHttpClient("this is not valid json {{{{");
        var service = new ProductCatalogService(client);

        // Act
        var products = await service.GetProductsAsync();

        // Assert — lista vazia, sem exceção
        Assert.NotNull(products);
        Assert.Empty(products);
    }

    /// <summary>
    /// Requisito 1.1 / tratamento de erro — exceção de rede (HTTP failure) deve
    /// ser capturada e retornar uma lista vazia.
    /// </summary>
    [Fact]
    public async Task GetProductsAsync_NetworkException_ReturnsEmptyList()
    {
        // Arrange
        var client = CreateFailingHttpClient();
        var service = new ProductCatalogService(client);

        // Act
        var products = await service.GetProductsAsync();

        // Assert — lista vazia, sem exceção propagada
        Assert.NotNull(products);
        Assert.Empty(products);
    }

    /// <summary>
    /// Requisito 1.1 — JSON com array vazio deve retornar lista vazia (não nula).
    /// </summary>
    [Fact]
    public async Task GetProductsAsync_EmptyJsonArray_ReturnsEmptyList()
    {
        // Arrange
        var (client, _) = CreateHttpClient("[]");
        var service = new ProductCatalogService(client);

        // Act
        var products = await service.GetProductsAsync();

        // Assert
        Assert.NotNull(products);
        Assert.Empty(products);
    }

    /// <summary>
    /// Requisito 1.2 — Após uma falha (retorna lista vazia), a chamada seguinte
    /// deve usar o cache da lista vazia (não tentar recarregar).
    /// </summary>
    [Fact]
    public async Task GetProductsAsync_AfterFailure_SecondCallUsesCachedEmptyList()
    {
        // Arrange
        var client = CreateFailingHttpClient();
        var service = new ProductCatalogService(client);

        // Act
        var firstCall = await service.GetProductsAsync();
        // O serviço deve ter cacheado a lista vazia; segunda chamada não deve lançar exceção
        var secondCall = await service.GetProductsAsync();

        // Assert — mesma referência cacheada
        Assert.Same(firstCall, secondCall);
        Assert.Empty(secondCall);
    }

    // ---------------------------------------------------------------------------
    // Cenários de validação de integridade (Req 1.5, 2.7, 2.8, 6.1, 6.2)
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Requisito 1.5 / 3.1 — Produto com `amazonUrl` que não corresponde ao padrão
    /// canônico deve ser filtrado; produtos válidos presentes no mesmo JSON são mantidos.
    /// </summary>
    [Fact]
    public async Task GetProductsAsync_InvalidAmazonUrl_ProductFiltered()
    {
        // Arrange — um produto inválido (URL sem ASIN de 10 chars) e um válido
        var json = JsonSerializer.Serialize(new[]
        {
            new
            {
                id = "prod-bad-url",
                name = "Produto URL Inválida",
                description = "Desc.",
                imageUrl = "images/products/prod-bad-url.jpg",
                category = "Casa",
                price = 29.90m,
                amazonUrl = "https://www.amazon.com.br/dp/SHORT"  // ASIN com menos de 10 chars
            },
            new
            {
                id = "prod-valid",
                name = "Produto Válido",
                description = "Desc.",
                imageUrl = "images/products/prod-valid.jpg",
                category = "Casa",
                price = 49.90m,
                amazonUrl = "https://www.amazon.com.br/dp/AAAAAAAAAA"  // ASIN válido
            }
        });

        var (client, _) = CreateHttpClient(json);
        var service = new ProductCatalogService(client);

        // Act
        var products = await service.GetProductsAsync();

        // Assert — apenas o produto com URL válida é retornado
        Assert.Single(products);
        Assert.Equal("prod-valid", products[0].Id);
    }

    /// <summary>
    /// Requisito 2.7 / 6.1 — Produto com `price = 0` deve ser filtrado;
    /// produtos válidos presentes no mesmo JSON são mantidos.
    /// </summary>
    [Fact]
    public async Task GetProductsAsync_ZeroPrice_ProductFiltered()
    {
        // Arrange — um produto com preço zero e um válido
        var json = JsonSerializer.Serialize(new[]
        {
            new
            {
                id = "prod-zero-price",
                name = "Produto Preço Zero",
                description = "Desc.",
                imageUrl = "images/products/prod-zero-price.jpg",
                category = "Organização",
                price = 0m,
                amazonUrl = "https://www.amazon.com.br/dp/AAAAAAAAAA"
            },
            new
            {
                id = "prod-valid",
                name = "Produto Válido",
                description = "Desc.",
                imageUrl = "images/products/prod-valid.jpg",
                category = "Organização",
                price = 19.90m,
                amazonUrl = "https://www.amazon.com.br/dp/BBBBBBBBBB"
            }
        });

        var (client, _) = CreateHttpClient(json);
        var service = new ProductCatalogService(client);

        // Act
        var products = await service.GetProductsAsync();

        // Assert — apenas o produto com preço válido é retornado
        Assert.Single(products);
        Assert.Equal("prod-valid", products[0].Id);
    }

    /// <summary>
    /// Requisito 2.8 / 6.1 — Produto com `imageUrl` vazia deve ser filtrado;
    /// produtos válidos presentes no mesmo JSON são mantidos.
    /// </summary>
    [Fact]
    public async Task GetProductsAsync_EmptyImageUrl_ProductFiltered()
    {
        // Arrange — um produto com imageUrl vazia e um válido
        var json = JsonSerializer.Serialize(new[]
        {
            new
            {
                id = "prod-no-image",
                name = "Produto Sem Imagem",
                description = "Desc.",
                imageUrl = "",
                category = "Limpeza",
                price = 12.50m,
                amazonUrl = "https://www.amazon.com.br/dp/AAAAAAAAAA"
            },
            new
            {
                id = "prod-valid",
                name = "Produto Válido",
                description = "Desc.",
                imageUrl = "images/products/prod-valid.jpg",
                category = "Limpeza",
                price = 25.00m,
                amazonUrl = "https://www.amazon.com.br/dp/BBBBBBBBBB"
            }
        });

        var (client, _) = CreateHttpClient(json);
        var service = new ProductCatalogService(client);

        // Act
        var products = await service.GetProductsAsync();

        // Assert — apenas o produto com imagem é retornado
        Assert.Single(products);
        Assert.Equal("prod-valid", products[0].Id);
    }

    /// <summary>
    /// Requisito 6.2 — IDs duplicados: apenas o primeiro registro (menor índice)
    /// deve ser mantido; os demais com o mesmo id devem ser descartados.
    /// </summary>
    [Fact]
    public async Task GetProductsAsync_DuplicateIds_OnlyFirstKept()
    {
        // Arrange — dois produtos com o mesmo id; o primeiro deve ser mantido
        var json = JsonSerializer.Serialize(new[]
        {
            new
            {
                id = "prod-dup",
                name = "Produto Original",
                description = "Primeiro registro.",
                imageUrl = "images/products/prod-dup.jpg",
                category = "Cozinha",
                price = 39.90m,
                amazonUrl = "https://www.amazon.com.br/dp/AAAAAAAAAA"
            },
            new
            {
                id = "prod-dup",
                name = "Produto Duplicado",
                description = "Segundo registro com mesmo id.",
                imageUrl = "images/products/prod-dup-2.jpg",
                category = "Cozinha",
                price = 99.90m,
                amazonUrl = "https://www.amazon.com.br/dp/BBBBBBBBBB"
            }
        });

        var (client, _) = CreateHttpClient(json);
        var service = new ProductCatalogService(client);

        // Act
        var products = await service.GetProductsAsync();

        // Assert — apenas um produto retornado e é o primeiro (nome original)
        Assert.Single(products);
        Assert.Equal("prod-dup", products[0].Id);
        Assert.Equal("Produto Original", products[0].Name);
    }
}
