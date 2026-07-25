using System.Net.Http.Json;
using MaterDomus.Web.Models;

namespace MaterDomus.Web.Services;

/// <summary>
/// Implementação de <see cref="IProductCatalogService"/> que carrega o catálogo
/// a partir de um arquivo JSON estático servido pelo servidor e mantém os dados
/// em cache durante a sessão.
/// </summary>
public class ProductCatalogService : IProductCatalogService
{
    private readonly HttpClient _http;
    private IReadOnlyList<Product>? _cache;

    public ProductCatalogService(HttpClient http)
    {
        _http = http;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Product>> GetProductsAsync()
    {
        if (_cache is not null)
            return _cache;

        try
        {
            _cache = await _http.GetFromJsonAsync<List<Product>>("data/products.json")
                     ?? new List<Product>();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ProductCatalogService] Falha ao carregar products.json: {ex.Message}");
            _cache = new List<Product>();
        }

        return _cache;
    }
}
