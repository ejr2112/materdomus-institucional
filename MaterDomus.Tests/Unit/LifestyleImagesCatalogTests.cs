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
/// Fotos ambientadas da vitrine: os 10 produtos, com arquivos reais
/// e sem vazar para JSON-LD / og:image. A foto canônica continua em imageUrl.
/// </summary>
public class LifestyleImagesCatalogTests
{
    private static readonly JsonSerializerOptions WebJsonOptions = new(JsonSerializerDefaults.Web);

    private static readonly LifestyleExpectation[] Expected =
    {
        new("B0GKPPS5YH", "dispenser-flow-quadrado-branco-001", 39.90m, false,
            "images/products/dispenser-flow-quadrado-branco-001.jpg",
            new[]
            {
                new LifestyleFile(
                    "images/products/dispenser-flow-quadrado-branco-001-ambientada-1.webp",
                    "Dispenser quadrado 1L transparente com tampa branca sobre bancada de lavanderia com azulejos verde-sálvia",
                    "4e9384263ae99c4d721a5f7ef42cdf572f47a4966cb81355581f6b1e36116442",
                    36804),
                new LifestyleFile(
                    "images/products/dispenser-flow-quadrado-branco-001-ambientada-2.webp",
                    "Dispenser quadrado 1L sobre bancada de madeira clara em lavanderia com azulejos brancos",
                    "9874c32e95a48bb971aa22a599ecf35ec66baa8d072a26a251430af7debc18dc",
                    35484)
            }),
        new("B0GKPZMCYZ", "dispenser-quadrado-1-5l-branco-b0gkpzmcyz", 49.49m, false,
            "images/products/dispenser-quadrado-1-5l-branco-b0gkpzmcyz.jpg",
            new[]
            {
                new LifestyleFile(
                    "images/products/dispenser-quadrado-1-5l-branco-b0gkpzmcyz-ambientada-1.webp",
                    "Dispenser quadrado 1,5L transparente sobre bancada cinza da área de serviço, ao lado da cuba",
                    "aa9a57aab1b3e16b2ea5c2aa0d6930d782898170c6400310d2fa8d2d83f083f0",
                    20246),
                new LifestyleFile(
                    "images/products/dispenser-quadrado-1-5l-branco-b0gkpzmcyz-ambientada-2.webp",
                    "Dispenser quadrado 1,5L transparente sobre bancada de lavanderia com azulejos bege e tanque",
                    "cb03450cdb5131955b982c490525f077953ac4aaf539ad607378483e802a3bff",
                    20580)
            }),
        new("B0CZTTRFQR", "escova-limpeza-multiuso-bege-b0czttrfqr", 20.69m, false,
            "images/products/escova-limpeza-multiuso-bege-b0czttrfqr.jpg",
            new[]
            {
                new LifestyleFile(
                    "images/products/escova-limpeza-multiuso-bege-b0czttrfqr-ambientada-1.webp",
                    "Escova de limpeza multiuso bege sobre toalha de linho com tigelas de madeira",
                    "b5dd3c04a7f4d7b0c99ae986e243fa375c74238da297db257e837fb25e9f31c5",
                    46136),
                new LifestyleFile(
                    "images/products/escova-limpeza-multiuso-bege-b0czttrfqr-ambientada-2.webp",
                    "Escova de limpeza multiuso bege sobre bancada de mármore ao lado de prato branco",
                    "d0b305d994ab2b9361b936a9bafc433a6228ecb64dbf4b479fbbb8d28ddc17c9",
                    39252)
            }),
        new("B0GKQ4VVPQ", "organizador-parede-armario-branco-b0gkq4vvpq", 58.49m, false,
            "images/products/organizador-parede-armario-branco-b0gkq4vvpq.png",
            new[]
            {
                new LifestyleFile(
                    "images/products/organizador-parede-armario-branco-b0gkq4vvpq-ambientada-1.webp",
                    "Organizador de parede branco fixado no azulejo da cozinha, acima da bancada de madeira",
                    "e2145859b940f4d7b6b79ea967db6eb38b052536ab3cfa5c8272933e095e5fcd",
                    36826),
                new LifestyleFile(
                    "images/products/organizador-parede-armario-branco-b0gkq4vvpq-ambientada-2.webp",
                    "Organizador de parede branco fixado no azulejo verde-sálvia da lavanderia, acima do tanque",
                    "809ea2184656c82eae5b5239177c1e5b4c2789e76176ae7c801cf5b917ca7582",
                    34078)
            }),
        new("B0GKPQ99R8", "organizador-parede-multiuso-bege-b0gkpq99r8", 56.69m, false,
            "images/products/organizador-parede-multiuso-bege-b0gkpq99r8.png",
            new[]
            {
                new LifestyleFile(
                    "images/products/organizador-parede-multiuso-bege-b0gkpq99r8-ambientada-1.webp",
                    "Organizador de parede multiuso bege com ganchos fixado em azulejo branco da área de serviço",
                    "3e4711e547375871c24025634c78bf3349c083ec5739abddf76b1e48ff437e39",
                    29148),
                new LifestyleFile(
                    "images/products/organizador-parede-multiuso-bege-b0gkpq99r8-ambientada-2.webp",
                    "Organizador de parede multiuso bege com ganchos fixado em revestimento cinza acima de bancada de madeira",
                    "3e498792c7cbf9e9dbbb5e8c7fa1cc19106276b1ad1bc94b9cd400e322954311",
                    15304)
            }),
        new("B0CZTTVLWK", "rodo-multiuso-bege-b0czttvlwk", 17.09m, true,
            "images/products/rodo-multiuso-bege-b0czttvlwk.jpg",
            new[]
            {
                new LifestyleFile(
                    "images/products/rodo-multiuso-bege-b0czttvlwk-ambientada-1.webp",
                    "Rodo multiuso bege apoiado no parapeito da janela, ao lado de vasos de plantas",
                    "da6cb9ada9f36b5500e7e678d07578b108b6ebfeccbe714bb80c5c459292cf90",
                    52192),
                new LifestyleFile(
                    "images/products/rodo-multiuso-bege-b0czttvlwk-ambientada-2.webp",
                    "Rodo multiuso bege apoiado no nicho do box do banheiro com revestimento bege",
                    "73bf7be38db5e3dc5a1e0d8aa751feff0ceb5fc3a0f6306879f481ed45d5967a",
                    18154)
            }),
        new("B0F8PWY3M5", "rodo-bege-b0f8pwy3m5", 51.99m, true,
            "images/products/rodo-bege-b0f8pwy3m5.jpg",
            new[]
            {
                new LifestyleFile(
                    "images/products/rodo-bege-b0f8pwy3m5-ambientada-1.webp",
                    "Rodo bege apoiado na parede de azulejos brancos da área de serviço, ao lado da máquina de lavar",
                    "833cf3086a836c3433af7a130767956f825d0290e7037c99b7cd268a724696ba",
                    20934),
                new LifestyleFile(
                    "images/products/rodo-bege-b0f8pwy3m5-ambientada-2.webp",
                    "Rodo bege apoiado na parede bege da lavanderia, ao lado de um cesto de palha",
                    "082281fe222840749769039226d9ec66cc9ae1dba86e8456f402fcb61ba70155",
                    20414)
            }),
        new("B0FXBN7SCB", "pano-chao-microfibra-chumbo-b0fxbn7scb", 19.79m, true,
            "images/products/pano-chao-microfibra-chumbo-b0fxbn7scb.jpg",
            new[]
            {
                new LifestyleFile(
                    "images/products/pano-chao-microfibra-chumbo-b0fxbn7scb-ambientada-1.webp",
                    "Pano de chão de microfibra chumbo dobrado sobre a máquina de lavar",
                    "14c92a9338a711e1fee4c41e39ae8f004f527ffb2f34d94e7fcae45d955fa0ab",
                    48934),
                new LifestyleFile(
                    "images/products/pano-chao-microfibra-chumbo-b0fxbn7scb-ambientada-2.webp",
                    "Pano de chão de microfibra chumbo dobrado sobre o piso da área de serviço, ao lado de um balde",
                    "17eb57051a9cdd76ff0d31499ea62b162d4262eee992dc72e6694efd0a7b6481",
                    36412)
            }),
        new("B0G634V2NB", "kit-3-panos-microfibra-b0g634v2nb", 18.69m, true,
            "images/products/kit-3-panos-microfibra-b0g634v2nb.jpg",
            new[]
            {
                new LifestyleFile(
                    "images/products/kit-3-panos-microfibra-b0g634v2nb-ambientada-1.webp",
                    "Kit com três panos de microfibra mesclados sobre bancada branca da cozinha",
                    "e3b56bbb3172aa66197687b1a85099cb408aa8dcc3fa950af198982ff77ac931",
                    71626),
                new LifestyleFile(
                    "images/products/kit-3-panos-microfibra-b0g634v2nb-ambientada-2.webp",
                    "Kit com três panos de microfibra mesclados sobre bancada de madeira da cozinha",
                    "1de543bb51d9d707f004d27571ca0e96baa621083fc78ab588449670c240df75",
                    74100)
            }),
        new("B0CZTTSHB7", "borrifador-500ml-bege-b0czttshb7", 29.74m, true,
            "images/products/borrifador-500ml-bege-b0czttshb7.jpg",
            new[]
            {
                new LifestyleFile(
                    "images/products/borrifador-500ml-bege-b0czttshb7-ambientada-1.webp",
                    "Borrifador 500ml bege sobre bancada da cozinha junto à janela",
                    "3e437749a49826b74a9fa9e926a1a8bfda2e53659881ade722f8f1ede2f05cc2",
                    38600),
                new LifestyleFile(
                    "images/products/borrifador-500ml-bege-b0czttshb7-ambientada-2.webp",
                    "Borrifador 500ml bege sobre bancada com azulejos verde-sálvia, entre cuba branca e cesto de madeira",
                    "a75b2168af1e87d85663462a7160f2c2e51d2f6184975a1b15ec90ded4746740",
                    21364)
            })
    };

    [Fact]
    public void ProductsJson_AllTenProductsHaveTwoLifestyleImages()
    {
        using var doc = JsonDocument.Parse(ReadRepo("wwwroot", "data", "products.json"));
        var rows = doc.RootElement.EnumerateArray().ToList();
        Assert.Equal(10, rows.Count);

        var withLifestyle = rows.Where(row => row.TryGetProperty("lifestyleImages", out _)).ToList();
        Assert.Equal(10, withLifestyle.Count);
        Assert.Equal(
            Expected.Select(item => item.Asin),
            rows.Select(row => row.GetProperty("asin").GetString()));

        var firstComingSoon = rows.FindIndex(row => row.GetProperty("comingSoon").GetBoolean());
        Assert.True(firstComingSoon > 0);
        Assert.All(rows.Take(firstComingSoon), row => Assert.False(row.GetProperty("comingSoon").GetBoolean()));
        Assert.All(rows.Skip(firstComingSoon), row => Assert.True(row.GetProperty("comingSoon").GetBoolean()));

        foreach (var expected in Expected)
        {
            var row = rows.Single(item => item.GetProperty("asin").GetString() == expected.Asin);
            var names = row.EnumerateObject().Select(property => property.Name).ToList();
            Assert.Equal("lifestyleImages", names[^1]);
            Assert.Equal(expected.Id, row.GetProperty("id").GetString());
            Assert.Equal(expected.Price, row.GetProperty("price").GetDecimal());
            Assert.Equal(expected.ImageUrl, row.GetProperty("imageUrl").GetString());
            Assert.DoesNotContain("ambientada", expected.ImageUrl, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(expected.ComingSoon, row.GetProperty("comingSoon").GetBoolean());

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
        Assert.Equal(10, products!.Count);
        Assert.Equal(10, products.Count(product => product.LifestyleImages is { Count: 2 }));
        foreach (var expected in Expected)
        {
            var product = products.Single(item => item.Asin == expected.Asin);
            Assert.Equal(expected.ImageUrl, product.ImageUrl);
            Assert.Equal(expected.Id, product.Id);
            Assert.Equal(2, product.LifestyleImages!.Count);
        }
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
        bool ComingSoon,
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
