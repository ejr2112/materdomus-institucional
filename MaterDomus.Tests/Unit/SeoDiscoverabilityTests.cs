using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Bunit;
using MaterDomus.Web.Models;
using MaterDomus.Web.Pages;
using MaterDomus.Web.Services;
using MaterDomus.Web.Shared;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MaterDomus.Tests.Unit;

/// <summary>
/// SEO técnico gratuito: sitemap, robots, tags do index.html e títulos por rota.
/// Não cobre ferramentas pagas (Ahrefs, etc.).
/// </summary>
public class SeoDiscoverabilityTests
{
    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "MaterDomus.Web.csproj")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException(
            "Não foi possível localizar a raiz do repositório (MaterDomus.Web.csproj).");
    }

    private static string WwwrootFile(params string[] segments) =>
        Path.Combine(new[] { FindRepoRoot(), "wwwroot" }.Concat(segments).ToArray());

    private static string ReadWwwroot(params string[] segments) =>
        File.ReadAllText(WwwrootFile(segments));

    [Fact]
    public void Sitemap_IncludesProdutosAndKeepsExistingRoutes()
    {
        var xml = XDocument.Load(WwwrootFile("sitemap.xml"));
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

        var urls = xml.Root!
            .Elements(ns + "url")
            .Select(url => (
                Loc: url.Element(ns + "loc")?.Value,
                Priority: url.Element(ns + "priority")?.Value,
                Lastmod: url.Element(ns + "lastmod")?.Value
            ))
            .ToList();

        Assert.Contains(urls, u => u.Loc == "https://www.materdomus.com.br/" && u.Priority == "1.0");
        Assert.Contains(urls, u => u.Loc == "https://www.materdomus.com.br/produtos" && u.Priority == "0.9");
        Assert.Contains(urls, u => u.Loc == "https://www.materdomus.com.br/fornecedores" && u.Priority == "0.9");
        Assert.Contains(urls, u => u.Loc == "https://www.materdomus.com.br/sobre" && u.Priority == "0.7");
        Assert.Contains(urls, u => u.Loc == "https://www.materdomus.com.br/contato" && u.Priority == "0.6");
        Assert.All(urls, u => Assert.False(string.IsNullOrWhiteSpace(u.Lastmod)));
    }

    [Fact]
    public void RobotsTxt_AllowsAllAndPointsToSitemap()
    {
        var robots = ReadWwwroot("robots.txt");

        Assert.Contains("User-agent: *", robots);
        Assert.Contains("Allow: /", robots);
        Assert.Contains("Sitemap: https://www.materdomus.com.br/sitemap.xml", robots);
        Assert.DoesNotContain("Disallow: /", robots);
    }

    [Fact]
    public void IndexHtml_HasCanonicalOpenGraphTwitterAndJsonLd()
    {
        var html = ReadWwwroot("index.html");

        Assert.Contains("<title>Mater Domus | Utilidades domésticas Ou e Linha Flow</title>", html);
        Assert.Contains("Linha Flow", html);
        Assert.Contains("Ou", html);
        Assert.Contains("Amazon", html);
        Assert.Contains("<link rel=\"canonical\" href=\"https://www.materdomus.com.br/\" />", html);

        Assert.Contains("property=\"og:title\"", html);
        Assert.Contains("property=\"og:description\"", html);
        Assert.Contains("property=\"og:url\"", html);
        Assert.Contains("property=\"og:type\" content=\"website\"", html);
        Assert.Contains("property=\"og:locale\" content=\"pt_BR\"", html);
        Assert.Contains("property=\"og:image\" content=\"https://www.materdomus.com.br/images/logo.png\"", html);

        Assert.Contains("name=\"twitter:card\" content=\"summary_large_image\"", html);
        Assert.Contains("name=\"twitter:title\"", html);
        Assert.Contains("name=\"twitter:description\"", html);
        Assert.Contains("name=\"twitter:image\" content=\"https://www.materdomus.com.br/images/logo.png\"", html);

        Assert.Contains("href=\"css/site.css\"", html);
        Assert.Contains("href=\"css/produtos.css\"", html);
        Assert.Contains("src=\"js/seo.js\"", html);

        Assert.Contains("type=\"application/ld+json\"", html);
        var jsonMatch = Regex.Match(
            html,
            @"<script type=""application/ld\+json"">\s*(\{.*?\})\s*</script>",
            RegexOptions.Singleline);
        Assert.True(jsonMatch.Success, "JSON-LD não encontrado em index.html");

        using var doc = JsonDocument.Parse(jsonMatch.Groups[1].Value);
        var graph = doc.RootElement.GetProperty("@graph");
        var types = graph.EnumerateArray().Select(n => n.GetProperty("@type").GetString()).ToList();
        Assert.Contains("Organization", types);
        Assert.Contains("WebSite", types);

        var org = graph.EnumerateArray().First(n => n.GetProperty("@type").GetString() == "Organization");
        Assert.Equal("Mater Domus", org.GetProperty("name").GetString());
        Assert.Equal("https://www.materdomus.com.br/", org.GetProperty("url").GetString());
        Assert.Equal("https://www.materdomus.com.br/images/logo.png", org.GetProperty("logo").GetString());
        Assert.Equal("contato@materdomus.com.br", org.GetProperty("email").GetString());
        Assert.Contains(
            "https://www.materdomus.com.br/",
            org.GetProperty("sameAs").EnumerateArray().Select(x => x.GetString()));
        Assert.Equal(
            "+55-19-99349-1775",
            org.GetProperty("contactPoint").GetProperty("telephone").GetString());
    }

    [Fact]
    public void IndexHtml_KeepsGtmAndBootScreen()
    {
        var html = ReadWwwroot("index.html");

        Assert.Contains("GTM-5FJ38X8C", html);
        Assert.Contains("www.googletagmanager.com/gtm.js", html);
        Assert.Contains("www.googletagmanager.com/ns.html?id=GTM-5FJ38X8C", html);

        Assert.Contains("id=\"app\"", html);
        Assert.Contains("class=\"boot-screen\"", html);
        Assert.Contains("id=\"boot-screen\"", html);
        Assert.Contains("images/logo.png", html);
        Assert.Contains("alt=\"MaterDomus\"", html);
        Assert.Contains("class=\"boot-screen__spinner\"", html);
        Assert.Contains("aria-hidden=\"true\"", html);
        Assert.Contains("class=\"boot-screen__message\"", html);
        Assert.Contains("Um instante…", html);
        Assert.Contains("id=\"boot-error\"", html);
        Assert.Contains("BOOT_TIMEOUT_MS = 80000", html);
        Assert.Contains("blazor.webassembly.js", html);
    }

    [Fact]
    public void IndexHtml_HasCrawlableFallbackLinksWithoutPaidSeoScripts()
    {
        var html = ReadWwwroot("index.html");

        Assert.Contains("<noscript>", html);
        Assert.Contains("href=\"/produtos\"", html);
        Assert.Contains("href=\"/sobre\"", html);
        Assert.Contains("href=\"/contato\"", html);
        Assert.Contains("href=\"/fornecedores\"", html);
        Assert.Contains("id=\"seo-crawl\"", html);
        Assert.Contains("mailto:contato@materdomus.com.br", html);
        Assert.Contains("https://wa.me/5519993491775", html);

        Assert.DoesNotContain("ahrefs", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("semrush", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("moz.com", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("screamingfrog", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Program_RegistersHeadOutlet()
    {
        var program = File.ReadAllText(Path.Combine(FindRepoRoot(), "Program.cs"));
        Assert.Contains("HeadOutlet", program);
        Assert.Contains("head::after", program);
    }

    [Fact]
    public void Pages_HaveUniqueTitlesAndDescriptions()
    {
        using var indexCtx = CreateJsContext();
        var home = indexCtx.RenderComponent<MaterDomus.Web.Pages.Index>().FindComponent<SeoMeta>().Instance;

        using var sobreCtx = new Bunit.TestContext();
        var sobre = sobreCtx.RenderComponent<Sobre>().FindComponent<SeoMeta>().Instance;

        using var contatoCtx = CreateJsContext();
        var contato = contatoCtx.RenderComponent<Contato>().FindComponent<SeoMeta>().Instance;

        using var fornecedoresCtx = CreateJsContext();
        var fornecedores = fornecedoresCtx.RenderComponent<Fornecedores>().FindComponent<SeoMeta>().Instance;

        var titles = new[] { home.Title, sobre.Title, contato.Title, fornecedores.Title };
        var descriptions = new[] { home.Description, sobre.Description, contato.Description, fornecedores.Description };
        var canonicals = new[] { home.Canonical, sobre.Canonical, contato.Canonical, fornecedores.Canonical };

        Assert.Equal(titles.Length, titles.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(descriptions.Length, descriptions.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(canonicals.Length, canonicals.Distinct(StringComparer.Ordinal).Count());

        Assert.Contains("Ou", home.Title);
        Assert.Contains("Linha Flow", home.Title);
        Assert.StartsWith("https://www.materdomus.com.br/", home.Canonical);
        Assert.Equal("https://www.materdomus.com.br/sobre", sobre.Canonical);
        Assert.Equal("https://www.materdomus.com.br/contato", contato.Canonical);
        Assert.Equal("https://www.materdomus.com.br/fornecedores", fornecedores.Canonical);
    }

    [Fact]
    public void ProdutosRazor_DeclaresUniqueSeoMeta()
    {
        var razor = File.ReadAllText(Path.Combine(FindRepoRoot(), "Pages", "Produtos.razor"));

        Assert.Contains("SeoMeta", razor);
        Assert.Contains("Produtos Ou e Linha Flow | Mater Domus", razor);
        Assert.Contains("https://www.materdomus.com.br/produtos", razor);
        Assert.Contains("Stylesheet=\"css/produtos.css\"", razor);
        Assert.DoesNotContain("<HeadContent>", razor);
        Assert.Contains("produtos-page__intro", razor);
        Assert.Contains("starlink-promo", razor);
        Assert.True(
            razor.IndexOf("products-grid", StringComparison.Ordinal) <
            razor.IndexOf("starlink-promo", StringComparison.Ordinal),
            "A faixa Starlink deve ficar depois da grade para não dominar a vitrine.");
    }

    [Fact]
    public void SeoMeta_KeepsStylesheetInTheSameHeadContent()
    {
        var razor = File.ReadAllText(Path.Combine(FindRepoRoot(), "Shared", "SeoMeta.razor"));

        Assert.Contains("<HeadContent>", razor);
        Assert.Contains("<link rel=\"canonical\" href=\"@Canonical\" />", razor);
        Assert.Contains("rel=\"stylesheet\"", razor);
        Assert.Contains("href=\"@Stylesheet\"", razor);
        Assert.Contains("public string? Stylesheet { get; set; }", razor);
        Assert.Single(Regex.Matches(razor, "<HeadContent>"));
    }

    [Fact]
    public void ProdutosPage_RendersSeoMetaWithProdutosStylesheet()
    {
        using var ctx = CreateJsContext();
        ctx.Services.AddSingleton<IProductCatalogService>(new EmptyCatalogService());
        ctx.Services.AddScoped<FavoritesService>();

        var cut = ctx.RenderComponent<Produtos>();
        var seo = cut.FindComponent<SeoMeta>().Instance;

        Assert.Equal("https://www.materdomus.com.br/produtos", seo.Canonical);
        Assert.Equal("css/produtos.css", seo.Stylesheet);
        Assert.Contains("Produtos", seo.Title, StringComparison.Ordinal);
    }

    [Fact]
    public void ProdutosCss_KeepsCardGridAndComingSoonRules()
    {
        var css = ReadWwwroot("css", "produtos.css");

        Assert.Contains(".products-grid", css);
        Assert.Contains(".product-card", css);
        Assert.Contains(".product-card__badge", css);
        Assert.Contains(".filter-bar", css);
        Assert.Contains(".product-card--coming-soon", css);
        Assert.Contains(".product-detail__meta", css);
        Assert.Contains(".product-detail__image-wrapper", css);
        Assert.Contains("main section.produtos-page", css);
        Assert.Contains("--produtos-max-width: 1240px", css);
        Assert.Contains("grid-template-columns: repeat(4, minmax(0, 1fr))", css);
        Assert.Contains("min-height: 44px", css);
    }

    [Fact]
    public void SeoJs_RewritesCanonicalFromTheCurrentPath()
    {
        var seoJs = ReadWwwroot("js", "seo.js");

        Assert.Contains("https://www.materdomus.com.br", seoJs);
        Assert.Contains("canonicalForPath", seoJs);
        Assert.Contains("link[rel=\"canonical\"]", seoJs);
        Assert.Contains("meta[property=\"og:url\"]", seoJs);
        Assert.Contains("history.pushState", seoJs);
        Assert.Contains("history.replaceState", seoJs);
        Assert.Contains("popstate", seoJs);
        Assert.Contains("location.pathname", seoJs);
    }

    [Fact]
    public void Manifest_IsValidJsonWithThemeColor()
    {
        var json = ReadWwwroot("manifest.webmanifest");
        using var doc = JsonDocument.Parse(json);

        Assert.Equal("Mater Domus", doc.RootElement.GetProperty("name").GetString());
        Assert.Equal("#1f1f1f", doc.RootElement.GetProperty("theme_color").GetString());
        Assert.Equal("pt-BR", doc.RootElement.GetProperty("lang").GetString());
        Assert.Contains(
            "/images/icon-192.png",
            doc.RootElement.GetProperty("icons").EnumerateArray().Select(i => i.GetProperty("src").GetString()));

        var html = ReadWwwroot("index.html");
        Assert.Contains("rel=\"manifest\" href=\"manifest.webmanifest\"", html);
        Assert.Contains("name=\"theme-color\" content=\"#1f1f1f\"", html);

        var swa = ReadWwwroot("staticwebapp.config.json");
        Assert.Contains("/manifest.webmanifest", swa);
        Assert.Contains("/sitemap.xml", swa);
        Assert.Contains("/robots.txt", swa);
    }

    private static Bunit.TestContext CreateJsContext()
    {
        var ctx = new Bunit.TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        return ctx;
    }

    private sealed class EmptyCatalogService : IProductCatalogService
    {
        public Task<IReadOnlyList<Product>> GetProductsAsync() =>
            Task.FromResult<IReadOnlyList<Product>>(Array.Empty<Product>());
    }
}
