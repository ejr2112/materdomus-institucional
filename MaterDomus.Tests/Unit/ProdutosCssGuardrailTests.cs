using System.Text.RegularExpressions;
using Bunit;
using MaterDomus.Web.Models;
using MaterDomus.Web.Pages;
using MaterDomus.Web.Services;
using MaterDomus.Web.Shared;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MaterDomus.Tests.Unit;

/// <summary>
/// Guardrails against the /produtos unstyled-grid regression: produtos.css existed
/// (HTTP 200) but was not linked after SEO/HeadOutlet edits. These tests fail if
/// the stylesheet link is removed from index.html, Produtos.razor, or SeoMeta.
/// </summary>
public class ProdutosCssGuardrailTests
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

    private static string RepoFile(params string[] segments) =>
        Path.Combine(new[] { FindRepoRoot() }.Concat(segments).ToArray());

    private static string ReadRepoFile(params string[] segments) =>
        File.ReadAllText(RepoFile(segments));

    private static bool HasStylesheetLink(string html, string href)
    {
        var links = Regex.Matches(html, @"<link\b[^>]*>", RegexOptions.IgnoreCase);
        return links
            .Select(m => m.Value)
            .Any(tag =>
                Regex.IsMatch(tag, @"rel\s*=\s*[""']stylesheet[""']", RegexOptions.IgnoreCase) &&
                Regex.IsMatch(tag, $@"href\s*=\s*[""']{Regex.Escape(href)}[""']", RegexOptions.IgnoreCase));
    }

    [Fact]
    public void IndexHtml_LinksSiteCssAndProdutosCss()
    {
        var html = ReadRepoFile("wwwroot", "index.html");

        Assert.True(
            HasStylesheetLink(html, "css/site.css"),
            "wwwroot/index.html must keep a stylesheet link to css/site.css.");
        Assert.True(
            HasStylesheetLink(html, "css/produtos.css"),
            "wwwroot/index.html must keep a stylesheet link to css/produtos.css. " +
            "Removing it unstyles the product grid even when the file still returns 200.");
    }

    [Fact]
    public void ProdutosRazor_DeclaresSeoMetaStylesheetForProdutosCss()
    {
        var razor = ReadRepoFile("Pages", "Produtos.razor");

        Assert.Contains("SeoMeta", razor);
        Assert.Contains("Stylesheet=\"css/produtos.css\"", razor);
        Assert.DoesNotContain("<HeadContent>", razor);
    }

    [Fact]
    public void ProdutosCss_FileExistsWithKnownProductClasses()
    {
        var cssPath = RepoFile("wwwroot", "css", "produtos.css");

        Assert.True(File.Exists(cssPath), "wwwroot/css/produtos.css must exist.");

        var css = File.ReadAllText(cssPath);
        Assert.Contains(".product-card", css);
        Assert.Contains(".products-grid", css);
    }

    [Fact]
    public void SeoMeta_RendersStylesheetLinkWhenStylesheetIsSet()
    {
        using var ctx = new Bunit.TestContext();
        var cut = RenderSeoMeta(ctx, stylesheet: "css/produtos.css");
        var head = HeadContentMarkup(ctx, cut);

        Assert.True(
            HasStylesheetLink(head, "css/produtos.css"),
            "SeoMeta must render <link rel=\"stylesheet\" href=\"css/produtos.css\"> when Stylesheet is set.");
    }

    [Fact]
    public void SeoMeta_OmitsStylesheetLinkWhenStylesheetIsMissing()
    {
        using var ctx = new Bunit.TestContext();
        var cut = RenderSeoMeta(ctx, stylesheet: null);
        var head = HeadContentMarkup(ctx, cut);

        Assert.False(
            HasStylesheetLink(head, "css/produtos.css"),
            "SeoMeta must not emit a produtos.css link when Stylesheet is unset.");
        Assert.DoesNotContain("rel=\"stylesheet\"", head, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("rel=\"canonical\"", head);
        Assert.Contains("name=\"description\"", head);
    }

    [Fact]
    public void SeoMeta_KeepsStylesheetWhenTitleDescriptionAndCanonicalChange()
    {
        using var ctx = new Bunit.TestContext();
        var cut = RenderSeoMeta(ctx, stylesheet: "css/produtos.css");

        cut.SetParametersAndRender(ps => ps
            .Add(p => p.Title, "Título atualizado | Mater Domus")
            .Add(p => p.Description, "Descrição atualizada para SEO.")
            .Add(p => p.Canonical, "https://www.materdomus.com.br/produtos?ref=seo"));

        var head = HeadContentMarkup(ctx, cut);
        Assert.Equal("css/produtos.css", cut.Instance.Stylesheet);
        Assert.True(
            HasStylesheetLink(head, "css/produtos.css"),
            "Updating Title/Description/Canonical must not drop the Stylesheet <link>.");
        Assert.Contains("name=\"description\"", head);
        Assert.Contains("rel=\"canonical\"", head);
        Assert.Contains("https://www.materdomus.com.br/produtos?ref=seo", head);
        Assert.Contains("Descrição atualizada para SEO.", head);
        Assert.Single(Regex.Matches(ReadRepoFile("Shared", "SeoMeta.razor"), "<HeadContent>"));
    }

    [Fact]
    public void ProdutosPage_SeoMetaHeadContentKeepsProdutosStylesheet()
    {
        using var ctx = new Bunit.TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton<IProductCatalogService>(new EmptyCatalogService());
        ctx.Services.AddScoped<FavoritesService>();

        var cut = ctx.RenderComponent<Produtos>();
        var seo = cut.FindComponent<SeoMeta>();

        Assert.Equal("css/produtos.css", seo.Instance.Stylesheet);
        var head = HeadContentMarkup(ctx, seo);
        Assert.True(
            HasStylesheetLink(head, "css/produtos.css"),
            "Produtos.razor must keep css/produtos.css in the same SeoMeta HeadContent as title/description/canonical.");
        Assert.Contains("rel=\"canonical\"", head);
        Assert.Contains("https://www.materdomus.com.br/produtos", head);
        Assert.Contains("name=\"description\"", head);
    }

    private static IRenderedComponent<SeoMeta> RenderSeoMeta(Bunit.TestContext ctx, string? stylesheet) =>
        ctx.RenderComponent<SeoMeta>(ps => ps
            .Add(p => p.Title, "Produtos Ou e Linha Flow | Mater Domus")
            .Add(p => p.Description, "Conheça os produtos Mater Domus.")
            .Add(p => p.Canonical, "https://www.materdomus.com.br/produtos")
            .Add(p => p.Stylesheet, stylesheet));

    /// <summary>
    /// HeadContent paints into HeadOutlet, so its own markup is empty in bUnit.
    /// Render the ChildContent fragment to assert the actual &lt;link&gt; tags.
    /// </summary>
    private static string HeadContentMarkup(Bunit.TestContext ctx, IRenderedFragment cut)
    {
        var heads = cut.FindComponents<HeadContent>();
        Assert.True(heads.Count > 0, "Expected SeoMeta to render a HeadContent section.");
        Assert.Single(heads);

        var child = heads[0].Instance.ChildContent;
        Assert.NotNull(child);

        var rendered = ctx.Render(child);
        return rendered.Markup;
    }

    private sealed class EmptyCatalogService : IProductCatalogService
    {
        public Task<IReadOnlyList<Product>> GetProductsAsync() =>
            Task.FromResult<IReadOnlyList<Product>>(Array.Empty<Product>());
    }
}
