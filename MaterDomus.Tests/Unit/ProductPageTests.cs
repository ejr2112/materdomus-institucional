using System.Text.Json;
using System.Text.RegularExpressions;
using MaterDomus.VitrineSeoGen;
using Xunit;

namespace MaterDomus.Tests.Unit;

/// <summary>
/// Páginas individuais para o catálogo da Meta. A URL limpa reescreve
/// para o HTML estático (HTTP 200 no Azure Static Web Apps) e o documento
/// já traz preço, disponibilidade, Open Graph e JSON-LD.
/// </summary>
public class ProductPageTests
{
    private const string Dispenser1L = "dispenser-flow-quadrado-branco-001";
    private const string Dispenser15L = "dispenser-quadrado-1-5l-branco-b0gkpzmcyz";

    [Fact]
    public void EveryCatalogProduct_HasAPageWithOpenGraphJsonLdAndCleanUrl()
    {
        var productsJson = ReadRepo("wwwroot", "data", "products.json");
        var indexHtml = ReadRepo("wwwroot", "index.html");
        var pages = VitrineSeo.BuildProductPages(productsJson, indexHtml);

        using var catalog = JsonDocument.Parse(productsJson);
        var ids = catalog.RootElement.EnumerateArray()
            .Select(row => row.GetProperty("id").GetString())
            .ToList();
        Assert.Equal(ids.Count, pages.Count);
        Assert.Equal(13, pages.Count);

        foreach (var id in ids)
        {
            Assert.NotNull(id);
            var page = pages.Single(item => item.Id == id);
            Assert.Equal("/produtos/" + id, page.PublicPath);
            Assert.Equal("https://www.materdomus.com.br/produtos/" + id, page.CanonicalUrl);
            Assert.Equal(id + ".html", page.FileName);
            Assert.Equal(page.Html, ReadRepo("wwwroot", "produtos", page.FileName));
            Assert.Contains($"<link rel=\"canonical\" href=\"{page.CanonicalUrl}\" />", page.Html);
            Assert.Contains("property=\"og:type\" content=\"product\"", page.Html);
            Assert.Contains("property=\"og:title\"", page.Html);
            Assert.Contains("property=\"og:image\" content=\"https://www.materdomus.com.br/images/", page.Html);
            Assert.Contains("property=\"product:price:amount\"", page.Html);
            Assert.Contains("property=\"product:price:currency\" content=\"BRL\"", page.Html);
            Assert.Contains("property=\"og:price:amount\"", page.Html);
            Assert.Contains("property=\"og:price:currency\" content=\"BRL\"", page.Html);
            Assert.Contains("property=\"product:availability\"", page.Html);
            Assert.Contains("property=\"product:brand\" content=\"Ou\"", page.Html);
            Assert.Contains("id=\"product-jsonld\"", page.Html);
            Assert.Contains("\"@type\": \"Product\"", page.Html);
            Assert.Contains("\"name\": \"Ou\"", page.Html);
            Assert.Contains("class=\"product-page__photo\"", page.Html);
            Assert.Contains("object-fit: contain", ReadRepo("wwwroot", "css", "produtos.css"));
            Assert.DoesNotContain("property=\"og:url\" content=\"https://www.amazon.com.br", page.Html);
            Assert.DoesNotContain("property=\"og:type\" content=\"website\"", page.Html);
        }
    }

    [Fact]
    public void DispenserPages_ExposeCatalogPriceSkuAmazonLinkAndInStock()
    {
        var pages = PagesById();
        var oneLiter = pages[Dispenser1L];
        var oneAndHalf = pages[Dispenser15L];

        AssertDispenser(
            oneLiter.Html,
            title: "Ou Dispenser Quadrado 1L Branco Linha Flow",
            priceLabel: "R$ 34,99",
            priceAmount: "34.99",
            sku: "MD-OU-0001",
            asin: "B0GKPPS5YH",
            image: "images/products/dispenser-flow-quadrado-branco-001-ambiente-v2-1.webp",
            canonical: oneLiter.CanonicalUrl);

        AssertDispenser(
            oneAndHalf.Html,
            title: "Ou Dispenser Quadrado 1,5L Branco Linha Flow",
            priceLabel: "R$ 36,00",
            priceAmount: "36.00",
            sku: "MD-OU-B0GKPZMCYZ",
            asin: "B0GKPZMCYZ",
            image: "images/products/dispenser-quadrado-1-5l-branco-b0gkpzmcyz-ambiente-v2-1.webp",
            canonical: oneAndHalf.CanonicalUrl);

        var css = ReadRepo("wwwroot", "css", "produtos.css");
        var productCss = css[css.IndexOf(".product-page__photo", StringComparison.Ordinal)..];
        Assert.Contains("object-fit: contain", productCss);
        Assert.DoesNotContain("object-fit: cover", productCss);
        Assert.DoesNotContain("overflow: hidden", productCss);
    }

    [Fact]
    public void ComingSoonPage_HasNoAmazonButton()
    {
        var page = PagesById()["borrifador-500ml-bege-b0czttshb7"];
        Assert.Contains("Disponibilidade: <strong>Em breve</strong>", page.Html);
        Assert.Contains("content=\"out of stock\"", page.Html);
        Assert.Contains("https://schema.org/OutOfStock", page.Html);
        Assert.DoesNotContain("Comprar na Amazon</a>", page.Html);
        Assert.DoesNotContain("https://www.amazon.com.br/dp/", page.Html);
    }

    [Fact]
    public void VitrineCards_LinkToProductPages()
    {
        var html = ReadRepo("wwwroot", "produtos.html");
        Assert.Contains("href=\"/produtos/dispenser-flow-quadrado-branco-001\"", html);
        Assert.Contains("href=\"/produtos/dispenser-quadrado-1-5l-branco-b0gkpzmcyz\"", html);
        Assert.Contains(">Ver produto</a>", html);
        Assert.Equal(13, Regex.Matches(html, "class=\"product-card__page-link\"").Count);
    }

    [Fact]
    public void StaticWebApp_RewritesProductUrlsWithoutRedirect()
    {
        var config = ReadRepo("wwwroot", "staticwebapp.config.json");
        using var doc = JsonDocument.Parse(config);
        Assert.False(doc.RootElement.TryGetProperty("navigationFallback", out _));

        var routes = doc.RootElement.GetProperty("routes").EnumerateArray().ToList();
        Assert.Equal("/produtos.html", routes.Single(route => route.GetProperty("route").GetString() == "/produtos").GetProperty("rewrite").GetString());

        foreach (var (slug, asin) in new[] { (Dispenser1L, "B0GKPPS5YH"), (Dispenser15L, "B0GKPZMCYZ") })
        {
            var route = routes.Single(item => item.GetProperty("route").GetString() == "/produtos/" + slug);
            Assert.Equal("/produtos/" + slug + ".html", route.GetProperty("rewrite").GetString());
            Assert.False(route.TryGetProperty("redirect", out _));
            Assert.False(route.TryGetProperty("statusCode", out _));
            Assert.True(File.Exists(RepoPath("wwwroot", "produtos", slug + ".html")), asin);
        }

        Assert.Equal(13, routes.Count(route => route.GetProperty("route").GetString()!.StartsWith("/produtos/", StringComparison.Ordinal)));
        Assert.DoesNotContain(routes, route => route.GetProperty("route").GetString()!.EndsWith('/'));
        Assert.Equal(config, VitrineSeo.UpsertProductRoutes(config, ReadRepo("wwwroot", "data", "products.json")));
    }

    [Fact]
    public void Sitemap_MatchesGenerator()
    {
        var productsJson = ReadRepo("wwwroot", "data", "products.json");
        var sitemap = ReadRepo("wwwroot", "sitemap.xml");
        Assert.Equal(VitrineSeo.BuildSitemap(productsJson), sitemap);
    }

    private static void AssertDispenser(
        string html,
        string title,
        string priceLabel,
        string priceAmount,
        string sku,
        string asin,
        string image,
        string canonical)
    {
        Assert.Contains($"<h1>{title}</h1>", html);
        Assert.Contains($"property=\"og:title\" content=\"{title}\"", html);
        Assert.Contains($"property=\"og:url\" content=\"{canonical}\"", html);
        Assert.Contains($"property=\"og:image\" content=\"https://www.materdomus.com.br/{image}\"", html);
        Assert.Contains($"property=\"product:price:amount\" content=\"{priceAmount}\"", html);
        Assert.Contains($"property=\"og:price:amount\" content=\"{priceAmount}\"", html);
        Assert.Contains("property=\"product:availability\" content=\"in stock\"", html);
        Assert.Contains("property=\"og:availability\" content=\"in stock\"", html);
        Assert.Contains("Disponibilidade: <strong>Em estoque</strong>", html);
        Assert.Contains(priceLabel, html);
        Assert.Contains($"data-price=\"{priceAmount}\"", html);
        Assert.Contains($"SKU: <strong>{sku}</strong>", html);
        Assert.Contains($"property=\"product:retailer_item_id\" content=\"{sku}\"", html);
        Assert.Contains("Marca: <strong>Ou</strong>", html);
        Assert.Contains($"href=\"https://www.amazon.com.br/dp/{asin}?m=A20TN3HCSY6KZV\"", html);
        Assert.Contains(">Comprar na Amazon</a>", html);
        Assert.Contains("class=\"product-page__photo\"", html);
        Assert.Contains("width=\"1200\" height=\"1200\"", html);
        Assert.Contains($"\"sku\": \"{sku}\"", html);
        Assert.Contains($"\"price\": \"{priceAmount}\"", html);
        Assert.Contains("https://schema.org/InStock", html);
        Assert.Contains($"\"url\": \"{canonical}\"", html);
        Assert.DoesNotContain("https://www.amazon.com.br/dp/" + asin, ExtractJsonLd(html).GetProperty("offers").GetProperty("url").GetString());

        var json = ExtractJsonLd(html);
        Assert.Equal(title, json.GetProperty("name").GetString());
        Assert.Equal(sku, json.GetProperty("sku").GetString());
        Assert.Equal("Ou", json.GetProperty("brand").GetProperty("name").GetString());
        Assert.Equal(priceAmount, json.GetProperty("offers").GetProperty("price").GetString());
        Assert.Equal("BRL", json.GetProperty("offers").GetProperty("priceCurrency").GetString());
        Assert.Equal(canonical, json.GetProperty("offers").GetProperty("url").GetString());
        Assert.Equal(asin, json.GetProperty("asin").GetString());
        var images = json.GetProperty("image").EnumerateArray().Select(item => item.GetString()).ToList();
        Assert.Contains("https://www.materdomus.com.br/" + image, images);
        Assert.All(images, item => Assert.StartsWith("https://www.materdomus.com.br/", item));
    }

    private static JsonElement ExtractJsonLd(string html)
    {
        var match = Regex.Match(
            html,
            "<script type=\"application/ld\\+json\" id=\"product-jsonld\">\\s*(\\{.*\\})\\s*</script>",
            RegexOptions.Singleline);
        Assert.True(match.Success, "JSON-LD do produto não encontrado");
        using var doc = JsonDocument.Parse(match.Groups[1].Value);
        return doc.RootElement.Clone();
    }

    private static Dictionary<string, VitrineSeo.ProductPageDocument> PagesById()
    {
        var pages = VitrineSeo.BuildProductPages(
            ReadRepo("wwwroot", "data", "products.json"),
            ReadRepo("wwwroot", "index.html"));
        return pages.ToDictionary(page => page.Id);
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
}
