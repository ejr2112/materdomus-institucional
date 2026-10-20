using System.Net;
using System.Text;
using System.Text.Json;
using MaterDomus.VitrineSeoGen;
using MaterDomus.Web.Models;
using MaterDomus.Web.Services;
using Xunit;

namespace MaterDomus.Tests.Unit;

/// <summary>
/// JSON-LD e Open Graph da vitrine, gerados de products.json.
/// A Offer de compra aponta para a Amazon, nunca para um checkout no site.
/// </summary>
public class VitrineSeoTests
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    [Fact]
    public void ItemList_MatchesCatalogServiceAndBuyableAmazonOffers()
    {
        var productsJson = ReadRepo("wwwroot", "data", "products.json");
        var indexHtml = ReadRepo("wwwroot", "index.html");
        var artifacts = VitrineSeo.Build(productsJson, indexHtml);

        using var schema = JsonDocument.Parse(artifacts.ItemListJson);
        var root = schema.RootElement;
        Assert.Equal("ItemList", root.GetProperty("@type").GetString());
        Assert.Equal("https://schema.org", root.GetProperty("@context").GetString());
        Assert.Equal(VitrineSeo.PageUrl, root.GetProperty("url").GetString());
        Assert.Equal(VitrineSeo.Description, root.GetProperty("description").GetString());

        var expected = LoadViaCatalogService(productsJson);
        var items = root.GetProperty("itemListElement").EnumerateArray().ToList();
        Assert.Equal(expected.Count, items.Count);
        Assert.Equal(expected.Count, root.GetProperty("numberOfItems").GetInt32());

        for (var i = 0; i < expected.Count; i++)
        {
            var product = expected[i];
            var listItem = items[i];
            Assert.Equal(i + 1, listItem.GetProperty("position").GetInt32());

            var item = listItem.GetProperty("item");
            Assert.Equal("Product", item.GetProperty("@type").GetString());
            Assert.Equal(product.Name, item.GetProperty("name").GetString());
            Assert.Equal(product.Description, item.GetProperty("description").GetString());
            Assert.Equal(product.Id, item.GetProperty("sku").GetString());
            Assert.Equal(AbsoluteImage(product.ImageUrl), item.GetProperty("image").GetString());
            Assert.StartsWith("https://www.materdomus.com.br/", item.GetProperty("image").GetString());
            Assert.Equal(product.Asin, item.GetProperty("asin").GetString());
            if (string.IsNullOrEmpty(product.Gtin))
            {
                Assert.False(item.TryGetProperty("gtin", out _));
                Assert.False(item.TryGetProperty("gtin13", out _));
            }
            else
            {
                Assert.Equal(product.Gtin, item.GetProperty("gtin").GetString());
            }

            var brand = item.GetProperty("brand");
            Assert.Equal("Brand", brand.GetProperty("@type").GetString());
            Assert.Equal("Ou", brand.GetProperty("name").GetString());
            Assert.Equal("https://ou.com.br/", brand.GetProperty("url").GetString());

            var offer = item.GetProperty("offers");
            Assert.Equal("Offer", offer.GetProperty("@type").GetString());
            Assert.Equal("BRL", offer.GetProperty("priceCurrency").GetString());
            Assert.Equal(product.Price.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), offer.GetProperty("price").GetString());

            var buyable = !product.ComingSoon && ProductCatalogService.IsValidAmazonUrl(product.AmazonUrl);
            if (buyable)
            {
                Assert.Equal("https://schema.org/InStock", offer.GetProperty("availability").GetString());
                Assert.Equal(product.AmazonUrl, offer.GetProperty("url").GetString());
                Assert.StartsWith("https://www.amazon.com.br/dp/", offer.GetProperty("url").GetString());
                Assert.Equal("https://www.materdomus.com.br/#organization", offer.GetProperty("seller").GetProperty("@id").GetString());
            }
            else
            {
                Assert.Equal("https://schema.org/OutOfStock", offer.GetProperty("availability").GetString());
                Assert.False(offer.TryGetProperty("url", out _));
                Assert.False(offer.TryGetProperty("seller", out _));
            }

            if (offer.TryGetProperty("url", out var offerUrl))
            {
                Assert.DoesNotContain("materdomus.com.br", offerUrl.GetString(), StringComparison.OrdinalIgnoreCase);
            }
        }

        Assert.DoesNotContain("materdomus.com.br/checkout", artifacts.ItemListJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/.well-known/ucp", artifacts.ItemListJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("gtin", artifacts.ItemListJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ItemList_ReadsAsinFromAmazonUrlOrProductId()
    {
        using var schema = JsonDocument.Parse(VitrineSeo.BuildItemListJson(ReadRepo("wwwroot", "data", "products.json")));
        var products = schema.RootElement.GetProperty("itemListElement").EnumerateArray()
            .Select(item => item.GetProperty("item"))
            .ToDictionary(item => item.GetProperty("sku").GetString()!, item => item);

        Assert.Equal("B0GKPPS5YH", products["dispenser-flow-quadrado-branco-001"].GetProperty("asin").GetString());
        Assert.Equal("B0F8PWY3M5", products["rodo-bege-b0f8pwy3m5"].GetProperty("asin").GetString());
        Assert.False(products["rodo-bege-b0f8pwy3m5"].GetProperty("offers").TryGetProperty("url", out _));

        using var raw = JsonDocument.Parse(ReadRepo("wwwroot", "data", "products.json"));
        var rows = raw.RootElement.EnumerateArray().ToList();
        Assert.All(rows, row =>
        {
            var asin = row.GetProperty("asin").GetString();
            Assert.Matches("^[A-Z0-9]{10}$", asin);
            Assert.False(row.TryGetProperty("gtin", out _));
            Assert.False(row.TryGetProperty("ean", out _));
        });
        Assert.Equal(
            "B0GKPPS5YH",
            rows.Single(row => row.GetProperty("id").GetString() == "dispenser-flow-quadrado-branco-001").GetProperty("asin").GetString());
        Assert.Equal(
            "B0F8PWY3M5",
            rows.Single(row => row.GetProperty("id").GetString() == "rodo-bege-b0f8pwy3m5").GetProperty("asin").GetString());
        Assert.DoesNotContain("7908466047085", ReadRepo("wwwroot", "data", "products.json"), StringComparison.Ordinal);
    }

    [Fact]
    public void ItemList_PrefersExplicitAsinAndEmitsGtinOnlyWhenPresent()
    {
        const string json = """
            [
              { "id": "override", "name": "Override", "description": "d", "imageUrl": "images/a.jpg", "category": "C", "price": 10, "amazonUrl": "https://www.amazon.com.br/dp/B0GKPPS5YH", "asin": "b0aaaaaaaa", "gtin": "7908466047085", "comingSoon": false },
              { "id": "ean-alias", "name": "Ean", "description": "d", "imageUrl": "images/b.jpg", "category": "C", "price": 11, "amazonUrl": "", "asin": "B0F8PWY3M5", "ean": "7891234567895", "comingSoon": true },
              { "id": "from-url", "name": "Url", "description": "d", "imageUrl": "images/c.jpg", "category": "C", "price": 12, "amazonUrl": "https://www.amazon.com.br/dp/B0CZTTVLWK", "comingSoon": false },
              { "id": "invalid-gtin", "name": "Ruim", "description": "d", "imageUrl": "images/d.jpg", "category": "C", "price": 9, "amazonUrl": "", "asin": "B0G634V2NB", "gtin": "123", "comingSoon": true }
            ]
            """;

        var loaded = LoadViaCatalogService(json).ToDictionary(product => product.Id);
        using var schema = JsonDocument.Parse(VitrineSeo.BuildItemListJson(json));
        var items = schema.RootElement.GetProperty("itemListElement").EnumerateArray()
            .Select(item => item.GetProperty("item"))
            .ToDictionary(item => item.GetProperty("sku").GetString()!);

        Assert.Equal("B0AAAAAAAA", loaded["override"].Asin);
        Assert.Equal("7908466047085", loaded["override"].Gtin);
        Assert.Equal("B0AAAAAAAA", items["override"].GetProperty("asin").GetString());
        Assert.Equal("7908466047085", items["override"].GetProperty("gtin").GetString());
        Assert.False(items["override"].TryGetProperty("gtin13", out _));
        Assert.Equal("https://www.amazon.com.br/dp/B0GKPPS5YH", items["override"].GetProperty("offers").GetProperty("url").GetString());

        Assert.Equal("B0F8PWY3M5", loaded["ean-alias"].Asin);
        Assert.Equal("7891234567895", loaded["ean-alias"].Gtin);
        Assert.Equal("7891234567895", loaded["ean-alias"].Ean);
        Assert.Equal("7891234567895", items["ean-alias"].GetProperty("gtin").GetString());
        Assert.False(items["ean-alias"].GetProperty("offers").TryGetProperty("url", out _));
        Assert.Equal("https://schema.org/OutOfStock", items["ean-alias"].GetProperty("offers").GetProperty("availability").GetString());

        Assert.Equal("B0CZTTVLWK", loaded["from-url"].Asin);
        Assert.Null(loaded["from-url"].Gtin);
        Assert.Equal("B0CZTTVLWK", items["from-url"].GetProperty("asin").GetString());
        Assert.False(items["from-url"].TryGetProperty("gtin", out _));

        Assert.Equal("B0G634V2NB", loaded["invalid-gtin"].Asin);
        Assert.Null(loaded["invalid-gtin"].Gtin);
        Assert.False(items["invalid-gtin"].TryGetProperty("gtin", out _));
    }

    [Fact]
    public void ItemList_DropsTheSameRecordsAsTheCatalogService()
    {
        const string json = """
            [
              { "id": "ok", "name": "Ok", "description": "d", "imageUrl": "images/a.jpg", "category": "C", "price": 10, "amazonUrl": "https://www.amazon.com.br/dp/B0GKPPS5YH", "comingSoon": false },
              { "id": "bad-url", "name": "Bad", "description": "d", "imageUrl": "images/a.jpg", "category": "C", "price": 10, "amazonUrl": "https://example.com/dp/B0GKPPS5YH", "comingSoon": false },
              { "id": "soon", "name": "Soon", "description": "d", "imageUrl": "images/b.jpg", "category": "C", "price": 12.5, "amazonUrl": "", "comingSoon": true },
              { "id": "dup", "name": "Dup", "description": "d", "imageUrl": "images/c.jpg", "category": "C", "price": 8, "amazonUrl": "", "comingSoon": true },
              { "id": "dup", "name": "Dup ignorado", "description": "d", "imageUrl": "images/c.jpg", "category": "C", "price": 8, "amazonUrl": "", "comingSoon": false }
            ]
            """;

        var expectedIds = LoadViaCatalogService(json).Select(p => p.Id).ToList();
        using var schema = JsonDocument.Parse(VitrineSeo.BuildItemListJson(json));
        var actualIds = schema.RootElement.GetProperty("itemListElement").EnumerateArray()
            .Select(item => item.GetProperty("item").GetProperty("sku").GetString()!)
            .ToList();

        Assert.Equal(expectedIds, actualIds);
        Assert.Equal(new[] { "ok", "soon", "dup" }, actualIds);
    }

    [Fact]
    public void AmazonUrlPattern_StaysAlignedWithTheCatalogService()
    {
        var service = ReadRepo("Services", "ProductCatalogService.cs");
        var generator = ReadRepo("tools", "VitrineSeoGen", "VitrineSeo.cs");
        const string servicePattern = @"^https://www\.amazon\.com\.br/dp/[A-Z0-9]{10}(\?m=[A-Z0-9]+)?$";

        Assert.Contains(servicePattern, service, StringComparison.Ordinal);
        Assert.Contains(VitrineSeo.AmazonUrlPattern, generator, StringComparison.Ordinal);
        Assert.Equal(
            "^https://www\\.amazon\\.com\\.br/dp/([A-Z0-9]{10})(\\?m=[A-Z0-9]+)?$",
            VitrineSeo.AmazonUrlPattern);
    }

    [Fact]
    public void VitrineMeta_UsesBuyableCatalogImageAndCommittedFilesMatchGenerator()
    {
        var productsJson = ReadRepo("wwwroot", "data", "products.json");
        var indexHtml = ReadRepo("wwwroot", "index.html");
        var artifacts = VitrineSeo.Build(productsJson, indexHtml);

        Assert.Equal(
            "https://www.materdomus.com.br/images/products/dispenser-flow-quadrado-branco-001-ambiente-v2-1.webp",
            artifacts.OgImageAbsoluteUrl);
        Assert.True(File.Exists(RepoPath("wwwroot", "images", "products", "dispenser-flow-quadrado-branco-001-ambiente-v2-1.webp")));

        using var meta = JsonDocument.Parse(artifacts.MetaJson);
        Assert.Equal(VitrineSeo.Title, meta.RootElement.GetProperty("title").GetString());
        Assert.Equal(VitrineSeo.Description, meta.RootElement.GetProperty("description").GetString());
        Assert.Equal(VitrineSeo.PageUrl, meta.RootElement.GetProperty("url").GetString());
        Assert.Equal(artifacts.OgImageAbsoluteUrl, meta.RootElement.GetProperty("image").GetString());
        Assert.Equal("Ou Dispenser Quadrado 1L Branco Linha Flow", meta.RootElement.GetProperty("imageAlt").GetString());

        Assert.Equal(artifacts.ItemListJson + "\n", ReadRepo("wwwroot", "data", "vitrine-itemlist.json"));
        Assert.Equal(artifacts.MetaJson + "\n", ReadRepo("wwwroot", "data", "vitrine-meta.json"));
        Assert.Equal(artifacts.ProdutosHtml, ReadRepo("wwwroot", "produtos.html"));
    }

    [Fact]
    public void ProdutosHtml_HasVitrineSocialTagsAndItemListInTheInitialDocument()
    {
        var html = ReadRepo("wwwroot", "produtos.html");

        Assert.Contains($"<title>{VitrineSeo.Title}</title>", html);
        Assert.Contains(VitrineSeo.Description, html);
        Assert.Contains("<link rel=\"canonical\" href=\"https://www.materdomus.com.br/produtos\" />", html);
        Assert.Contains("property=\"og:title\" content=\"Vitrine Ou e Linha Flow | Mater Domus\"", html);
        Assert.Contains("property=\"og:description\" content=\"" + VitrineSeo.Description + "\"", html);
        Assert.Contains("property=\"og:url\" content=\"https://www.materdomus.com.br/produtos\"", html);
        Assert.Contains(
            "property=\"og:image\" content=\"https://www.materdomus.com.br/images/products/dispenser-flow-quadrado-branco-001-ambiente-v2-1.webp\"",
            html);
        Assert.Contains("property=\"og:image:alt\" content=\"Ou Dispenser Quadrado 1L Branco Linha Flow\"", html);
        Assert.Contains("name=\"twitter:card\" content=\"summary_large_image\"", html);
        Assert.Contains(
            "name=\"twitter:image\" content=\"https://www.materdomus.com.br/images/products/dispenser-flow-quadrado-branco-001-ambiente-v2-1.webp\"",
            html);
        Assert.DoesNotContain("property=\"og:image\" content=\"https://www.materdomus.com.br/images/logo.png\"", html);

        Assert.Contains("id=\"vitrine-itemlist\"", html);
        Assert.Contains("\"@type\": \"ItemList\"", html);
        Assert.Contains("\"@type\": \"Product\"", html);
        Assert.Contains("https://schema.org/InStock", html);
        Assert.Contains("https://schema.org/OutOfStock", html);
        Assert.Contains("https://www.amazon.com.br/dp/B0GKPPS5YH?m=A20TN3HCSY6KZV", html);

        Assert.Contains("\"@type\": \"Organization\"", html);
        Assert.Contains("\"@type\": \"WebSite\"", html);
        Assert.Contains("id=\"app\"", html);
        Assert.Contains("blazor.webassembly.js", html);
        Assert.Contains("src=\"js/seo.js\"", html);
        Assert.Contains("id=\"seo-crawl\"", html);

        var home = ReadRepo("wwwroot", "index.html");
        Assert.DoesNotContain("id=\"vitrine-itemlist\"", home);
        Assert.Contains("property=\"og:image\" content=\"https://www.materdomus.com.br/images/logo.png\"", home);
    }

    [Fact]
    public void SeoJs_AppliesVitrineSocialTagsWithoutDroppingCanonicalRewrites()
    {
        var seoJs = ReadRepo("wwwroot", "js", "seo.js");

        Assert.Contains("canonicalForPath", seoJs);
        Assert.Contains("link[rel=\"canonical\"]", seoJs);
        Assert.Contains("meta[property=\"og:url\"]", seoJs);
        Assert.Contains("history.pushState", seoJs);
        Assert.Contains("history.replaceState", seoJs);
        Assert.Contains("popstate", seoJs);
        Assert.Contains("location.pathname", seoJs);

        Assert.Contains("'/produtos'", seoJs);
        Assert.Contains("meta[property=\"og:title\"]", seoJs);
        Assert.Contains("meta[property=\"og:description\"]", seoJs);
        Assert.Contains("meta[property=\"og:image\"]", seoJs);
        Assert.Contains("meta[name=\"twitter:image\"]", seoJs);
        Assert.Contains("'/data/vitrine-meta.json'", seoJs);
        Assert.Contains("'/data/vitrine-itemlist.json'", seoJs);
        Assert.Contains("vitrine-itemlist", seoJs);
        Assert.Contains("summary_large_image", ReadRepo("wwwroot", "produtos.html"));
        Assert.DoesNotContain("well-known/ucp", seoJs, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void StaticWebApp_RewritesProdutosToTheVitrineDocument()
    {
        var config = ReadRepo("wwwroot", "staticwebapp.config.json");
        using var doc = JsonDocument.Parse(config);
        var route = doc.RootElement.GetProperty("routes").EnumerateArray()
            .Single(r => r.GetProperty("route").GetString() == "/produtos");

        Assert.Equal("/produtos.html", route.GetProperty("rewrite").GetString());
        Assert.False(doc.RootElement.TryGetProperty("navigationFallback", out _));
        var notFound = doc.RootElement.GetProperty("responseOverrides").GetProperty("404");
        Assert.Equal("/404.html", notFound.GetProperty("rewrite").GetString());
        Assert.Equal(404, notFound.GetProperty("statusCode").GetInt32());
        Assert.Equal("text/plain; charset=utf-8", doc.RootElement.GetProperty("mimeTypes").GetProperty(".txt").GetString());

        foreach (var (path, rewrite) in new (string Path, string Rewrite)[]
        {
            ("/sobre", "/sobre.html"),
            ("/contato", "/contato.html"),
            ("/fornecedores", "/fornecedores.html"),
            ("/privacidade", "/privacidade.html"),
            ("/frete-e-trocas", "/frete-e-trocas.html")
        })
        {
            var match = doc.RootElement.GetProperty("routes").EnumerateArray()
                .Single(r => r.GetProperty("route").GetString() == path);
            Assert.Equal(rewrite, match.GetProperty("rewrite").GetString());
        }
    }

    [Fact]
    public void StaticPages_MatchTheCommittedHtml()
    {
        var indexHtml = ReadRepo("wwwroot", "index.html");
        var pages = StaticPages.Render(indexHtml);

        Assert.Equal(pages["sobre.html"], ReadRepo("wwwroot", "sobre.html"));
        Assert.Equal(pages["contato.html"], ReadRepo("wwwroot", "contato.html"));
        Assert.Equal(pages["fornecedores.html"], ReadRepo("wwwroot", "fornecedores.html"));
        Assert.Equal(pages["privacidade.html"], ReadRepo("wwwroot", "privacidade.html"));
        Assert.Equal(pages["frete-e-trocas.html"], ReadRepo("wwwroot", "frete-e-trocas.html"));
        Assert.Equal(pages["404.html"], ReadRepo("wwwroot", "404.html"));
        Assert.Contains("<h1>Quem é a Mater Domus</h1>", pages["sobre.html"]);
        Assert.Contains("MATER DOMUS LTDA", pages["sobre.html"]);
        Assert.Contains("64.258.511/0001-90", pages["sobre.html"]);
        Assert.Contains("São Paulo/SP", pages["sobre.html"]);
        Assert.Contains("Martiplast", pages["sobre.html"]);
        Assert.Contains("data-nav=\"sobre\" class=\"active\"", pages["sobre.html"]);
        Assert.DoesNotContain("Praticidade, organização e confiança para o lar", pages["sobre.html"]);
        Assert.Contains("noindex, nofollow", pages["404.html"]);
        Assert.Contains("<h1>Página não encontrada</h1>", pages["404.html"]);
        Assert.DoesNotContain("Praticidade, organização e confiança para o lar", pages["404.html"]);

        var titles = new HashSet<string>(StringComparer.Ordinal);
        var descriptions = new HashSet<string>(StringComparer.Ordinal);
        var canonicals = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in new[] { "index.html", "produtos.html", "sobre.html", "contato.html", "fornecedores.html", "privacidade.html", "frete-e-trocas.html", "404.html" })
        {
            var html = file == "index.html" ? indexHtml : pages.TryGetValue(file, out var generated) ? generated : ReadRepo("wwwroot", file);
            var title = System.Text.RegularExpressions.Regex.Match(html, "<title>(.*?)</title>").Groups[1].Value;
            var description = System.Text.RegularExpressions.Regex.Match(html, "name=\"description\"\\s+content=\"([^\"]*)\"").Groups[1].Value;
            var canonical = System.Text.RegularExpressions.Regex.Match(html, "rel=\"canonical\" href=\"([^\"]*)\"").Groups[1].Value;
            Assert.False(string.IsNullOrWhiteSpace(title), file);
            Assert.False(string.IsNullOrWhiteSpace(description), file);
            Assert.False(string.IsNullOrWhiteSpace(canonical), file);
            Assert.True(titles.Add(title), file + " title duplicado: " + title);
            Assert.True(descriptions.Add(description), file + " description duplicada");
            Assert.True(canonicals.Add(canonical), file + " canonical duplicado: " + canonical);
        }

        Assert.Equal("https://www.materdomus.com.br/sobre", System.Text.RegularExpressions.Regex.Match(pages["sobre.html"], "rel=\"canonical\" href=\"([^\"]*)\"").Groups[1].Value);
        Assert.DoesNotContain("<title>Mater Domus | Utilidades domésticas Ou e Linha Flow</title>", pages["sobre.html"]);
    }

    [Fact]
    public void LlmsTxt_DescribesTheResellerAndListsCatalogProducts()
    {
        var productsJson = ReadRepo("wwwroot", "data", "products.json");
        var (brief, full) = LlmsTxt.Build(productsJson);

        Assert.Equal(brief, ReadRepo("wwwroot", "llms.txt"));
        Assert.Equal(full, ReadRepo("wwwroot", "llms-full.txt"));
        Assert.Contains("MATER DOMUS LTDA", brief);
        Assert.Contains("64.258.511/0001-90", brief);
        Assert.Contains("São Paulo/SP", brief);
        Assert.Contains("Martiplast", brief);
        Assert.Contains("https://ou.com.br/", brief);
        Assert.Contains("contato@materdomus.com.br", brief);
        Assert.Contains("https://wa.me/5519993491775", brief);
        Assert.Contains("https://www.materdomus.com.br/produtos", brief);
        Assert.Contains("https://www.materdomus.com.br/sobre", brief);
        Assert.Contains("https://www.materdomus.com.br/frete-e-trocas", brief);
        Assert.Contains(LlmsTxt.AmazonStoreUrl, brief);
        Assert.DoesNotContain("materdomus.com/", brief.Replace("www.materdomus.com.br", "", StringComparison.Ordinal));
        Assert.DoesNotContain("loja oficial", brief, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("autorizad", brief, StringComparison.OrdinalIgnoreCase);

        using var catalog = JsonDocument.Parse(productsJson);
        foreach (var row in catalog.RootElement.EnumerateArray())
        {
            var name = row.GetProperty("name").GetString();
            Assert.NotNull(name);
            Assert.Contains(name, full);
        }
    }

    [Fact]
    public void ProdutosRazor_UsesTheSameVitrineTitleAndDescription()
    {
        var razor = ReadRepo("Pages", "Produtos.razor");
        Assert.Contains(VitrineSeo.Title, razor);
        Assert.Contains(VitrineSeo.Description, razor);
        Assert.Contains(VitrineSeo.PageUrl, razor);
    }

    private static List<Product> LoadViaCatalogService(string productsJson)
    {
        var handler = new StubHandler(productsJson);
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://www.materdomus.com.br/") };
        var service = new ProductCatalogService(client);
        return service.GetProductsAsync().GetAwaiter().GetResult().ToList();
    }

    private static string AbsoluteImage(string imageUrl) =>
        imageUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            ? imageUrl
            : "https://www.materdomus.com.br/" + imageUrl.TrimStart('/');

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
