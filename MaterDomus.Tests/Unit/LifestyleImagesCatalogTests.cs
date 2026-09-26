using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MaterDomus.VitrineSeoGen;
using MaterDomus.Web.Models;
using MaterDomus.Web.Services;
using Xunit;

namespace MaterDomus.Tests.Unit;

/// <summary>
/// Fotos ambientadas da vitrine: só os dois organizadores, com arquivos reais
/// e sem vazar para JSON-LD / og:image.
/// </summary>
public class LifestyleImagesCatalogTests
{
    private static readonly JsonSerializerOptions WebJsonOptions = new(JsonSerializerDefaults.Web);

    private static readonly LifestyleExpectation[] Expected =
    {
        new(
            "B0GKQ4VVPQ",
            "organizador-parede-armario-branco-b0gkq4vvpq",
            58.49m,
            "images/products/organizador-parede-armario-branco-b0gkq4vvpq.png",
            new[]
            {
                new LifestyleFile(
                    "images/products/organizador-parede-armario-branco-b0gkq4vvpq-ambientada-1.webp",
                    "Organizador de parede branco instalado na parede da cozinha, acima da bancada",
                    "178e989085f9c340a6921d31ad509f7ff59dc529fa79d59f96bd62024ea52f00",
                    26934),
                new LifestyleFile(
                    "images/products/organizador-parede-armario-branco-b0gkq4vvpq-ambientada-2.webp",
                    "Organizador de parede branco instalado na lavanderia, acima da máquina de lavar",
                    "1923d15bce382d608d52cd4c80c24afbee134b80f4246d9aba072c7ade52820e",
                    21260)
            }),
        new(
            "B0GKPQ99R8",
            "organizador-parede-multiuso-bege-b0gkpq99r8",
            56.69m,
            "images/products/organizador-parede-multiuso-bege-b0gkpq99r8.png",
            new[]
            {
                new LifestyleFile(
                    "images/products/organizador-parede-multiuso-bege-b0gkpq99r8-ambientada-1.webp",
                    "Organizador de parede multiuso bege instalado em parede neutra, ao lado de um vaso de cerâmica",
                    "1048b03bfdbe1d513f80e78f44cabcc221765c9c7668677f7451be66e27fc5ff",
                    34394),
                new LifestyleFile(
                    "images/products/organizador-parede-multiuso-bege-b0gkpq99r8-ambientada-2.webp",
                    "Organizador de parede multiuso bege instalado na lavanderia de parede verde, acima de toalhas dobradas",
                    "dfbb82ceb9cd4ec46b7d8023cfd1485a25014e00a618c0c3297a606469e2104f",
                    26022)
            })
    };

    [Fact]
    public void ProductsJson_OnlyTwoOrganizersHaveTwoLifestyleImages()
    {
        using var doc = JsonDocument.Parse(ReadRepo("wwwroot", "data", "products.json"));
        var rows = doc.RootElement.EnumerateArray().ToList();
        var withLifestyle = rows.Where(row => row.TryGetProperty("lifestyleImages", out _)).ToList();

        Assert.Equal(2, withLifestyle.Count);
        Assert.Equal(
            Expected.Select(item => item.Asin).OrderBy(asin => asin, StringComparer.Ordinal),
            withLifestyle.Select(row => row.GetProperty("asin").GetString()).OrderBy(asin => asin, StringComparer.Ordinal));

        foreach (var expected in Expected)
        {
            var row = rows.Single(item => item.GetProperty("asin").GetString() == expected.Asin);
            var names = row.EnumerateObject().Select(property => property.Name).ToList();
            Assert.Equal("lifestyleImages", names[^1]);
            Assert.Equal(expected.Id, row.GetProperty("id").GetString());
            Assert.Equal(expected.Price, row.GetProperty("price").GetDecimal());
            Assert.Equal(expected.ImageUrl, row.GetProperty("imageUrl").GetString());
            Assert.False(row.GetProperty("comingSoon").GetBoolean());

            var images = row.GetProperty("lifestyleImages").EnumerateArray().ToList();
            Assert.Equal(2, images.Count);
            for (var i = 0; i < expected.Files.Length; i++)
            {
                var image = images[i];
                Assert.Equal(expected.Files[i].Url, image.GetProperty("url").GetString());
                var alt = image.GetProperty("alt").GetString();
                Assert.False(string.IsNullOrWhiteSpace(alt));
                Assert.Equal(expected.Files[i].Alt, alt);
                Assert.Matches(@"[A-Za-zÀ-ÿ]", alt);

                var physical = RepoPath("wwwroot", expected.Files[i].Url.Replace('/', Path.DirectorySeparatorChar));
                Assert.True(File.Exists(physical), $"Arquivo ausente: {physical}");
                var info = new FileInfo(physical);
                Assert.Equal(expected.Files[i].Size, info.Length);
                Assert.Equal(expected.Files[i].Sha256, Sha256(physical));
            }
        }

        var products = JsonSerializer.Deserialize<List<Product>>(
            ReadRepo("wwwroot", "data", "products.json"),
            WebJsonOptions);
        Assert.NotNull(products);
        Assert.Equal(2, products!.Count(product => product.LifestyleImages is { Count: > 0 }));
        foreach (var product in products.Where(product => product.Asin is not ("B0GKQ4VVPQ" or "B0GKPQ99R8")))
            Assert.Null(product.LifestyleImages);
    }

    [Fact]
    public void CatalogService_SkipsLifestyleItemsWithEmptyUrl_WithoutDroppingProduct()
    {
        const string json = """
            [
              {
                "id": "prod-misto",
                "name": "Produto misto",
                "description": "Desc.",
                "imageUrl": "images/products/x.jpg",
                "category": "Casa",
                "price": 10.0,
                "amazonUrl": "https://www.amazon.com.br/dp/B0GKQ4VVPQ",
                "lifestyleImages": [
                  { "url": " ", "alt": "sem arquivo" },
                  { "url": "", "alt": "vazio" },
                  { "url": "images/products/a.webp", "alt": "Foto ambientada válida" }
                ]
              },
              {
                "id": "prod-so-vazio",
                "name": "Produto só vazio",
                "description": "Desc.",
                "imageUrl": "images/products/y.jpg",
                "category": "Casa",
                "price": 12.0,
                "amazonUrl": "https://www.amazon.com.br/dp/B0GKPQ99R8",
                "lifestyleImages": [
                  { "url": "", "alt": "nada" }
                ]
              },
              {
                "id": "prod-sem-campo",
                "name": "Produto sem campo",
                "description": "Desc.",
                "imageUrl": "images/products/z.jpg",
                "category": "Casa",
                "price": 14.0,
                "amazonUrl": "https://www.amazon.com.br/dp/B0GKPPS5YH"
              }
            ]
            """;

        var products = LoadViaCatalogService(json);

        Assert.Equal(3, products.Count);

        var mixed = products.Single(product => product.Id == "prod-misto");
        Assert.NotNull(mixed.LifestyleImages);
        var kept = Assert.Single(mixed.LifestyleImages!);
        Assert.Equal("images/products/a.webp", kept.Url);
        Assert.Equal("Foto ambientada válida", kept.Alt);

        Assert.Null(products.Single(product => product.Id == "prod-so-vazio").LifestyleImages);
        Assert.Null(products.Single(product => product.Id == "prod-sem-campo").LifestyleImages);
    }

    [Fact]
    public void SeoArtifacts_DoNotContainAmbientada()
    {
        var productsJson = ReadRepo("wwwroot", "data", "products.json");
        var artifacts = VitrineSeo.Build(productsJson, ReadRepo("wwwroot", "index.html"));

        Assert.DoesNotContain("ambientada", artifacts.ItemListJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ambientada", artifacts.MetaJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ambientada", artifacts.ProdutosHtml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ambientada", artifacts.OgImageAbsoluteUrl, StringComparison.OrdinalIgnoreCase);

        Assert.DoesNotContain("ambientada", ReadRepo("wwwroot", "data", "vitrine-itemlist.json"), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ambientada", ReadRepo("wwwroot", "data", "vitrine-meta.json"), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ambientada", ReadRepo("wwwroot", "produtos.html"), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ambientada", ReadRepo("wwwroot", "index.html"), StringComparison.OrdinalIgnoreCase);
    }

    private static List<Product> LoadViaCatalogService(string productsJson)
    {
        var handler = new StubHandler(productsJson);
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://www.materdomus.com.br/") };
        var service = new ProductCatalogService(client);
        return service.GetProductsAsync().GetAwaiter().GetResult().ToList();
    }

    private static string Sha256(string path)
    {
        var hash = SHA256.HashData(File.ReadAllBytes(path));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string ReadRepo(params string[] segments) =>
        File.ReadAllText(RepoPath(segments));

    private static string RepoPath(params string[] segments)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "MaterDomus.Web.csproj")))
                return Path.Combine(new[] { dir.FullName }.Concat(segments).ToArray());
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Não foi possível localizar a raiz do repositório.");
    }

    private sealed record LifestyleExpectation(
        string Asin,
        string Id,
        decimal Price,
        string ImageUrl,
        LifestyleFile[] Files);

    private sealed record LifestyleFile(string Url, string Alt, string Sha256, long Size);

    private sealed class StubHandler(string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
    }
}
