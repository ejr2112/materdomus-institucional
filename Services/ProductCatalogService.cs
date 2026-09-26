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

            // amazonUrl vazia é permitida (itens "Em breve" sem ASIN confirmado).
            // URL preenchida precisa ser canônica — link quebrado não entra na vitrine.
            if (!string.IsNullOrEmpty(p.AmazonUrl) && !IsValidAmazonUrl(p.AmazonUrl))
                continue;

            // Req 6.2: keep the first occurrence of each id, discard duplicates
            if (!seen.Add(p.Id))
            {
                Console.Error.WriteLine($"[ProductCatalogService] WARN — ID duplicado descartado: {p.Id}");
                continue;
            }

            valid.Add(NormalizeIdentifiers(p));
        }

        return valid;
    }

    /// <summary>
    /// Prefere <c>asin</c> explícito. Sem ele, usa o ASIN da URL canônica ou o
    /// sufixo de 10 caracteres do id (itens Em breve). GTIN só permanece se
    /// <c>gtin</c> ou <c>ean</c> já vier no JSON, com 8, 12, 13 ou 14 dígitos.
    /// </summary>
    private static Product NormalizeIdentifiers(Product product)
    {
        var asin = ResolveAsin(product);
        var gtin = NormalizeGtin(product.Gtin) ?? NormalizeGtin(product.Ean);
        var ean = NormalizeGtin(product.Ean);
        var lifestyle = NormalizeLifestyleImages(product.LifestyleImages);
        if (asin == product.Asin &&
            gtin == product.Gtin &&
            ean == product.Ean &&
            ReferenceEquals(lifestyle, product.LifestyleImages))
            return product;

        return product with { Asin = asin, Gtin = gtin, Ean = ean, LifestyleImages = lifestyle };
    }

    /// <summary>
    /// Descarta fotos ambientadas sem URL. Uma lista vazia após o filtro vira
    /// <c>null</c>, para o cartão continuar igual ao de um produto sem o campo.
    /// O produto em si não é removido.
    /// </summary>
    private static IReadOnlyList<ProductLifestyleImage>? NormalizeLifestyleImages(
        IReadOnlyList<ProductLifestyleImage>? images)
    {
        if (images is null)
            return null;

        var kept = 0;
        foreach (var image in images)
        {
            if (image is not null && !string.IsNullOrWhiteSpace(image.Url))
                kept++;
        }

        if (kept == images.Count)
            return images;

        if (kept == 0)
            return null;

        var valid = new List<ProductLifestyleImage>(kept);
        foreach (var image in images)
        {
            if (image is not null && !string.IsNullOrWhiteSpace(image.Url))
                valid.Add(image);
        }

        return valid;
    }

    private static readonly Regex AsinCapturePattern =
        new(@"^https://www\.amazon\.com\.br/dp/([A-Z0-9]{10})(\?m=[A-Z0-9]+)?$", RegexOptions.Compiled);

    private static readonly Regex IdAsinPattern =
        new(@"(?:^|-)([A-Za-z0-9]{10})$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static string? ResolveAsin(Product product)
    {
        var explicitAsin = NormalizeAsin(product.Asin);
        if (explicitAsin is not null)
            return explicitAsin;

        if (!string.IsNullOrEmpty(product.AmazonUrl))
        {
            var fromUrl = AsinCapturePattern.Match(product.AmazonUrl);
            if (fromUrl.Success)
                return fromUrl.Groups[1].Value;
        }

        var fromId = IdAsinPattern.Match(product.Id);
        if (!fromId.Success)
            return null;

        var token = fromId.Groups[1].Value;
        if (!token.Any(char.IsDigit))
            return null;

        return token.ToUpperInvariant();
    }

    private static string? NormalizeAsin(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var token = value.Trim().ToUpperInvariant();
        if (token.Length != 10)
            return null;

        foreach (var c in token)
        {
            if (c is not ((>= 'A' and <= 'Z') or (>= '0' and <= '9')))
                return null;
        }

        return token;
    }

    private static string? NormalizeGtin(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var compact = value.Trim().Replace(" ", "", StringComparison.Ordinal).Replace("-", "", StringComparison.Ordinal);
        if (compact.Length is not (8 or 12 or 13 or 14))
            return null;

        foreach (var c in compact)
        {
            if (c is < '0' or > '9')
                return null;
        }

        return compact;
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
