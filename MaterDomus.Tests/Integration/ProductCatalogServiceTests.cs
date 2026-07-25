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
            amazonUrl = "https://www.amazon.com.br/dp/AAAAA"
        },
        new
        {
            id = "prod-002",
            name = "Porta-Temperos",
            description = "Porta-temperos giratório para bancada.",
            imageUrl = "images/products/prod-002.jpg",
            category = "Cozinha",
            price = 59.90m,
            amazonUrl = "https://www.amazon.com.br/dp/BBBBB"
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
        Assert.Equal("https://www.amazon.com.br/dp/AAAAA", first.AmazonUrl);

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
}
