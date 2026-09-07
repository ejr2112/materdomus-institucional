using System.Net.Http.Json;
using System.Text.RegularExpressions;
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

    // Aceita a URL canônica do produto, opcionalmente com o parâmetro de vendedor
    // (?m={sellerId}) que atribui o clique à loja Master Domus na Amazon.
    // Parâmetros voláteis de sessão (ref=, qid=, dib=, sr=, ...) continuam rejeitados.
    private static readonly Regex AsinPattern =
        new(@"^https://www\.amazon\.com\.br/dp/[A-Z0-9]{10}(\?m=[A-Z0-9]+)?$", RegexOptions.Compiled);

    public ProductCatalogService(HttpClient http)
    {
        _http = http;
    }

    /// <summary>
    /// Returns <c>true</c> if <paramref name="url"/> matches the canonical Amazon
    /// product URL format <c>https://www.amazon.com.br/dp/{ASIN}</c> where ASIN is
    /// exactly 10 uppercase alphanumeric characters, optionally followed by the
    /// seller/store parameter <c>?m={sellerId}</c>.
    /// </summary>
    internal static bool IsValidAmazonUrl(string? url) =>
        !string.IsNullOrEmpty(url) && AsinPattern.IsMatch(url);

    /// <summary>
    /// Filters a raw deserialized list, removing invalid and duplicate records
    /// according to the integrity rules defined in Requirements 1.5, 2.7, 2.8,
    /// 3.1, 6.1, 6.2 and 6.3.
    /// </summary>
    private static IReadOnlyList<Product> Validate(List<Product>? raw)
    {
        if (raw is null) return Array.Empty<Product>();

        var seen  = new HashSet<string>(StringComparer.Ordinal);
        var valid = new List<Product>(raw.Count);

        foreach (var p in raw)
        {
            // Req 6.1: id and name must be non-empty after trim; price must be > 0
            if (string.IsNullOrWhiteSpace(p.Id)   ||
                string.IsNullOrWhiteSpace(p.Name)  ||
                p.Price <= 0m)
                continue;

            // Req 2.8: imageUrl must be non-empty
            if (string.IsNullOrEmpty(p.ImageUrl))
                continue;

            // Req 1.5 / 3.1: amazonUrl must match the canonical pattern
            if (!IsValidAmazonUrl(p.AmazonUrl))
                continue;

            // Req 6.2: keep the first occurrence of each id, discard duplicates
            if (!seen.Add(p.Id))
            {
                Console.Error.WriteLine($"[ProductCatalogService] WARN — ID duplicado descartado: {p.Id}");
                continue;
            }

            valid.Add(p);
        }

        return valid;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Product>> GetProductsAsync()
    {
        if (_cache is not null)
            return _cache;

        try
        {
            var raw = await _http.GetFromJsonAsync<List<Product>>("data/products.json");
            _cache = Validate(raw);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ProductCatalogService] Falha ao carregar products.json: {ex.Message}");
            _cache = Array.Empty<Product>();
        }

        return _cache;
    }
}
