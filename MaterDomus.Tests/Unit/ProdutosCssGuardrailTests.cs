using System.Text.RegularExpressions;
using Bunit;
using MaterDomus.Web.Models;
using MaterDomus.Web.Pages;
using MaterDomus.Web.Services;
using MaterDomus.Web.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MaterDomus.Tests.Unit;

/// <summary>
/// Guardrails against the /produtos unstyled-grid regression: produtos.css existed
/// (HTTP 200) but was not linked after SEO/HeadOutlet edits. These tests fail if
/// the stylesheet link is removed from index.html, Produtos.razor, or SeoMeta, or
/// if a second HeadContent steals the HeadOutlet section.
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
        Assert.DoesNotMatch(@"<\s*(?:[\w.-]+[:.])?HeadContent\b", razor);
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
        using var ctx = CreateHeadContext();
        var host = RenderSeoMetaInOutlet(ctx, stylesheet: "css/produtos.css");
        var head = HeadOutletMarkup(host);

        Assert.True(
            HasStylesheetLink(head, "css/produtos.css"),
            "SeoMeta must render <link rel=\"stylesheet\" href=\"css/produtos.css\"> into HeadOutlet when Stylesheet is set.");
    }

    [Fact]
    public void SeoMeta_OmitsStylesheetLinkWhenStylesheetIsMissing()
    {
        using var ctx = CreateHeadContext();
        var host = RenderSeoMetaInOutlet(ctx, stylesheet: null);
        var head = HeadOutletMarkup(host);

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
        using var ctx = CreateHeadContext();
        var host = RenderSeoMetaInOutlet(ctx, stylesheet: "css/produtos.css");
        var seo = host.FindComponent<SeoMeta>();

        seo.SetParametersAndRender(ps => ps
            .Add(p => p.Title, "Título atualizado | Mater Domus")
            .Add(p => p.Description, "Descrição atualizada para SEO.")
            .Add(p => p.Canonical, "https://www.materdomus.com.br/produtos?ref=seo"));

        var head = HeadOutletMarkup(host);
        Assert.Equal("css/produtos.css", seo.Instance.Stylesheet);
        Assert.True(
            HasStylesheetLink(head, "css/produtos.css"),
            "Updating Title/Description/Canonical must not drop the Stylesheet <link> from HeadOutlet.");
        Assert.Contains("name=\"description\"", head);
        Assert.Contains("rel=\"canonical\"", head);
        Assert.Contains("https://www.materdomus.com.br/produtos?ref=seo", head);
        Assert.Contains("Descrição atualizada para SEO.", head);
        Assert.Single(Regex.Matches(ReadRepoFile("Shared", "SeoMeta.razor"), @"<\s*HeadContent\b"));
    }

    [Fact]
    public void ProdutosPage_HeadOutletKeepsProdutosStylesheet()
    {
        using var ctx = CreateHeadContext();
        ctx.Services.AddSingleton<IProductCatalogService>(new EmptyCatalogService());
        ctx.Services.AddScoped<FavoritesService>();

        var host = ctx.RenderComponent<HeadOutletHost>(ps => ps.AddChildContent<Produtos>());
        var seo = host.FindComponent<SeoMeta>();
        var head = HeadOutletMarkup(host);

        Assert.Equal("css/produtos.css", seo.Instance.Stylesheet);
        Assert.Single(host.FindComponents<HeadContent>());
        Assert.True(
            HasStylesheetLink(head, "css/produtos.css"),
            "HeadOutlet must keep css/produtos.css for /produtos. A second HeadContent on the page replaces this section and unstyles the grid.");
        Assert.Contains("rel=\"canonical\"", head);
        Assert.Contains("https://www.materdomus.com.br/produtos", head);
        Assert.Contains("name=\"description\"", head);
    }

    [Fact]
    public void HeadOutlet_KeepsFirstHeadContent_AndDropsLaterStylesheet()
    {
        using var ctx = CreateHeadContext();
        var host = ctx.RenderComponent<CompetingHeadContentHost>();
        var head = HeadOutletMarkup(host);

        Assert.Equal(2, host.FindComponents<HeadContent>().Count);
        Assert.Contains("rel=\"canonical\"", head);
        Assert.Contains("name=\"description\"", head);
        Assert.DoesNotContain("name=\"competitor\"", head);
        Assert.False(
            HasStylesheetLink(head, "css/produtos.css"),
            "A later HeadContent with css/produtos.css is ignored by HeadOutlet. " +
            "The stylesheet must live in SeoMeta's HeadContent (the first/active section).");
    }

    private static Bunit.TestContext CreateHeadContext()
    {
        var ctx = new Bunit.TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.JSInterop
            .Setup<string>("Blazor._internal.PageTitle.getAndRemoveExistingTitle")
            .SetResult(string.Empty);
        return ctx;
    }

    private static IRenderedComponent<HeadOutletHost> RenderSeoMetaInOutlet(
        Bunit.TestContext ctx,
        string? stylesheet) =>
        ctx.RenderComponent<HeadOutletHost>(ps => ps.AddChildContent<SeoMeta>(seo => seo
            .Add(p => p.Title, "Produtos Ou e Linha Flow | Mater Domus")
            .Add(p => p.Description, "Conheça os produtos Mater Domus.")
            .Add(p => p.Canonical, "https://www.materdomus.com.br/produtos")
            .Add(p => p.Stylesheet, stylesheet)));

    private static string HeadOutletMarkup(IRenderedFragment host)
    {
        var outlet = host.FindComponent<HeadOutlet>();
        return outlet.Markup;
    }

    /// <summary>
    /// Mirrors Program.cs: HeadOutlet is a sibling root, not a child of the page.
    /// Rendering both in one tree lets tests assert the section HeadOutlet actually keeps.
    /// </summary>
    private sealed class HeadOutletHost : ComponentBase
    {
        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<HeadOutlet>(0);
            builder.CloseComponent();
            builder.AddContent(1, ChildContent);
        }
    }

    /// <summary>
    /// Recreates the live bug: SeoMeta HeadContent (SEO tags only) plus a later
    /// HeadContent with produtos.css. HeadOutlet keeps the first section and drops the CSS.
    /// </summary>
    private sealed class CompetingHeadContentHost : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<HeadOutlet>(0);
            builder.CloseComponent();

            builder.OpenComponent<SeoMeta>(1);
            builder.AddComponentParameter(2, nameof(SeoMeta.Title), "Produtos Ou e Linha Flow | Mater Domus");
            builder.AddComponentParameter(3, nameof(SeoMeta.Description), "Conheça os produtos Mater Domus.");
            builder.AddComponentParameter(4, nameof(SeoMeta.Canonical), "https://www.materdomus.com.br/produtos");
            builder.CloseComponent();

            builder.OpenComponent<HeadContent>(5);
            builder.AddComponentParameter(6, "ChildContent", (RenderFragment)(b =>
            {
                b.OpenElement(0, "link");
                b.AddAttribute(1, "rel", "stylesheet");
                b.AddAttribute(2, "href", "css/produtos.css");
                b.CloseElement();

                b.OpenElement(3, "meta");
                b.AddAttribute(4, "name", "competitor");
                b.CloseElement();
            }));
            builder.CloseComponent();
        }
    }

    private sealed class EmptyCatalogService : IProductCatalogService
    {
        public Task<IReadOnlyList<Product>> GetProductsAsync() =>
            Task.FromResult<IReadOnlyList<Product>>(Array.Empty<Product>());
    }
}
