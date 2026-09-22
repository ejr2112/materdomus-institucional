using System.Text.Json;
using Bunit;
using MaterDomus.Web.Helpers;
using MaterDomus.Web.Models;
using MaterDomus.Web.Pages;
using MaterDomus.Web.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MaterDomus.Tests.Unit;

/// <summary>
/// A vitrine lista primeiro os itens compráveis (<c>comingSoon: false</c>)
/// e os destaca, sem depender de ASIN fixo.
/// </summary>
public class AvailableFirstListingTests
{
    private static Product Make(
        string id,
        bool comingSoon,
        string name,
        string category = "Limpeza") =>
        new(
            Id: id,
            Name: name,
            Description: $"Descrição de {name}.",
            ImageUrl: "images/placeholder-product.png",
            Category: category,
            Price: 19.90m,
            AmazonUrl: comingSoon ? "" : "https://www.amazon.com.br/dp/B0GKPPS5YH",
            ComingSoon: comingSoon);

    [Fact]
    public void Partition_PutsAvailableFirst_AndKeepsRelativeOrder()
    {
        var catalog = new[]
        {
            Make("soon-a", comingSoon: true, name: "Rodo"),
            Make("buy-b", comingSoon: false, name: "Escova"),
            Make("soon-c", comingSoon: true, name: "Pano"),
            Make("buy-d", comingSoon: false, name: "Organizador"),
        };

        var (available, comingSoon) = ProductHelpers.PartitionByAvailability(catalog);

        Assert.Equal(new[] { "buy-b", "buy-d" }, available.Select(p => p.Id));
        Assert.Equal(new[] { "soon-a", "soon-c" }, comingSoon.Select(p => p.Id));
    }

    [Fact]
    public void Partition_EmptyAndNull_ReturnEmptyGroups()
    {
        var (available, comingSoon) = ProductHelpers.PartitionByAvailability(Array.Empty<Product>());
        Assert.Empty(available);
        Assert.Empty(comingSoon);

        (available, comingSoon) = ProductHelpers.PartitionByAvailability(null);
        Assert.Empty(available);
        Assert.Empty(comingSoon);
    }

    [Fact]
    public void ProdutosPage_RendersAvailableSectionBeforeComingSoon()
    {
        var products = new List<Product>
        {
            Make("soon-a", comingSoon: true, name: "Rodo bege"),
            Make("buy-b", comingSoon: false, name: "Escova multiuso"),
            Make("soon-c", comingSoon: true, name: "Pano chumbo"),
            Make("buy-d", comingSoon: false, name: "Organizador branco"),
        };

        using var ctx = new Bunit.TestContext();
        ctx.Services.AddSingleton<IProductCatalogService>(new FakeCatalog(products));
        ctx.Services.AddScoped<FavoritesService>();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var cut = ctx.RenderComponent<Produtos>();

        cut.WaitForAssertion(() =>
        {
            var headings = cut.FindAll("h2.products-section__title")
                .Select(h => h.TextContent.Trim())
                .ToList();
            Assert.Equal(new[] { "Disponíveis agora", "Em breve" }, headings);

            var names = cut.FindAll("article.product-card")
                .Select(card => card.GetAttribute("aria-label"))
                .ToList();
            Assert.Equal(
                new[] { "Escova multiuso", "Organizador branco", "Rodo bege", "Pano chumbo" },
                names);
        });

        var availableCards = cut.FindAll("section.products-section--available article.product-card");
        Assert.Equal(2, availableCards.Count);
        Assert.All(availableCards, card =>
        {
            Assert.Contains("product-card--available", card.GetAttribute("class"));
            Assert.Equal("Disponível", card.QuerySelector(".product-card__badge--available")!.TextContent.Trim());
        });

        var soonCards = cut.FindAll("section.products-section--soon article.product-card");
        Assert.Equal(2, soonCards.Count);
        Assert.All(soonCards, card =>
            Assert.Equal("Em breve", card.QuerySelector(".product-card__badge")!.TextContent.Trim()));
    }

    /// <summary>
    /// O JSON publicado intercala disponíveis e Em breve. A página tem de
    /// reordenar pela flag, mesmo se a lista chegar invertida — sem ASIN fixo.
    /// </summary>
    [Fact]
    public void ProdutosPage_RealCatalog_ListsAvailableBeforeComingSoon_EvenWhenReversed()
    {
        var catalog = LoadCatalog();
        Assert.Contains(catalog, p => p.ComingSoon);
        Assert.Contains(catalog, p => !p.ComingSoon);
        Assert.True(
            catalog.Zip(catalog.Skip(1), (a, b) => a.ComingSoon && !b.ComingSoon).Any(),
            "O catálogo de teste precisa continuar intercalado para provar o sort da página.");

        var reversed = catalog.AsEnumerable().Reverse().ToList();
        var expected = reversed.Where(p => !p.ComingSoon)
            .Concat(reversed.Where(p => p.ComingSoon))
            .Select(p => p.Id)
            .ToList();

        using var ctx = new Bunit.TestContext();
        ctx.Services.AddSingleton<IProductCatalogService>(new FakeCatalog(reversed));
        ctx.Services.AddScoped<FavoritesService>();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var cut = ctx.RenderComponent<Produtos>();

        cut.WaitForAssertion(() =>
        {
            var ids = cut.FindAll("article.product-card")
                .Select(card => card.GetAttribute("aria-label"))
                .ToList();
            var names = expected.Select(id => reversed.Single(p => p.Id == id).Name);
            Assert.Equal(names, ids);
        });

        var availableIds = reversed.Where(p => !p.ComingSoon).Select(p => p.Name).ToList();
        var soonIds = reversed.Where(p => p.ComingSoon).Select(p => p.Name).ToList();

        var availableCards = cut.FindAll("section.products-section--available article.product-card");
        Assert.Equal(availableIds, availableCards.Select(c => c.GetAttribute("aria-label")));
        Assert.All(availableCards, card =>
        {
            Assert.Contains("product-card--available", card.GetAttribute("class"));
            Assert.Equal("Disponível", card.QuerySelector(".product-card__badge--available")!.TextContent.Trim());
            Assert.NotEmpty(card.QuerySelectorAll("a.product-card__amazon-btn"));
        });

        var soonCards = cut.FindAll("section.products-section--soon article.product-card");
        Assert.Equal(soonIds, soonCards.Select(c => c.GetAttribute("aria-label")));
        Assert.All(soonCards, card =>
        {
            Assert.Equal("Em breve", card.QuerySelector(".product-card__badge")!.TextContent.Trim());
            Assert.Empty(card.QuerySelectorAll("a.product-card__amazon-btn"));
        });
    }

    [Fact]
    public void ProdutosCss_AvailableHighlightIsHighContrast_AndSectionsKeepVitrineWidth()
    {
        var css = File.ReadAllText(Path.Combine(FindRepoRoot(), "wwwroot", "css", "produtos.css"));

        var marker = ".product-card__badge--available {";
        var start = css.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(start >= 0, "Falta a regra do selo Disponível.");
        var block = css.Substring(start, css.IndexOf('}', start) - start);
        Assert.Contains("background: #1a1a1a", block);
        Assert.Contains("color: #fff", block);
        Assert.DoesNotContain("background: #fff", block);

        Assert.Contains("main section.products-section", css);
        Assert.Contains("max-width: none", css);
        Assert.Contains("border: 2px solid #1a1a1a", css);
    }

    private static List<Product> LoadCatalog()
    {
        var jsonPath = Path.Combine(FindRepoRoot(), "wwwroot", "data", "products.json");
        var products = JsonSerializer.Deserialize<List<Product>>(
            File.ReadAllText(jsonPath),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(products);
        return products!;
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "MaterDomus.Web.csproj")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Raiz do repositório não encontrada.");
    }

    private sealed class FakeCatalog : IProductCatalogService
    {
        private readonly IReadOnlyList<Product> _products;

        public FakeCatalog(IReadOnlyList<Product> products) => _products = products;

        public Task<IReadOnlyList<Product>> GetProductsAsync() => Task.FromResult(_products);
    }
}
