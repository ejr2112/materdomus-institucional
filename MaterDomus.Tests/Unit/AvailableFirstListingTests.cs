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

    private sealed class FakeCatalog : IProductCatalogService
    {
        private readonly IReadOnlyList<Product> _products;

        public FakeCatalog(IReadOnlyList<Product> products) => _products = products;

        public Task<IReadOnlyList<Product>> GetProductsAsync() => Task.FromResult(_products);
    }
}
