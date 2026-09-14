using System.Text.Json;
using Bunit;
using MaterDomus.Web.Helpers;
using MaterDomus.Web.Models;
using MaterDomus.Web.Services;
using MaterDomus.Web.Shared;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MaterDomus.Tests.Unit;

/// <summary>
/// Comportamento "Em breve" da Linha Flow: catálogo completo visível,
/// CTA Amazon só para itens disponíveis, flag <c>comingSoon</c> reativa a compra.
/// </summary>
public class ComingSoonCatalogTests
{
    private static readonly JsonSerializerOptions WebJsonOptions = new(JsonSerializerDefaults.Web);

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

    private static List<Product> LoadCatalog()
    {
        var jsonPath = Path.Combine(FindRepoRoot(), "wwwroot", "data", "products.json");
        var products = JsonSerializer.Deserialize<List<Product>>(File.ReadAllText(jsonPath), WebJsonOptions);
        Assert.NotNull(products);
        return products!;
    }

    private static Bunit.TestContext CreateContext()
    {
        var ctx = new Bunit.TestContext();
        ctx.Services.AddScoped<FavoritesService>();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        return ctx;
    }

    private static Product Make(
        string amazonUrl = "https://www.amazon.com.br/dp/B0GKPPS5YH?m=A20TN3HCSY6KZV",
        bool comingSoon = false,
        string id = "prod-1",
        string name = "Produto Teste") =>
        new(
            Id: id,
            Name: name,
            Description: "Descrição de teste.",
            ImageUrl: "images/placeholder-product.png",
            Category: "Lavanderia",
            Price: 39.90m,
            AmazonUrl: amazonUrl,
            ComingSoon: comingSoon);

    private static readonly string[] SellerAmazonTitles =
    {
        "Ou Dispenser Quadrado 1L Branco Linha Flow",
        "Ou Dispenser Quadrado 1,5L Branco Linha Flow",
        "Ou Rodo Bege Linha Flow",
        "Ou Rodo Multiuso Bege Linha Flow",
        "Ou Pano para Chão de Microfibra Chumbo Linha Flow",
        "Ou Kit 3 Panos Microfibra Multiuso Mesclado Linha Flow",
        "Ou Borrifador Multiuso 500ml Bege Linha Flow",
        "Ou Escova de limpeza multiuso Bege Linha Flow",
        "Ou Organizador de Parede e Armário Branco Linha Flow",
        "Ou Organizador de Parede Multiuso Bege Linha Flow"
    };

    private static readonly string[] ForbiddenCatalogNames =
    {
        "Escova de piso",
        "2,3L",
        "Cesto",
        "Organizador multiuso Ou 8L",
        "Kit borrifador e panos"
    };

    [Fact]
    public void Catalog_MatchesSellerCentralListings_ExactlyTen()
    {
        var products = LoadCatalog();

        Assert.Equal(10, products.Count);
        Assert.Equal(SellerAmazonTitles, products.Select(p => p.Name).ToArray());

        foreach (var forbidden in ForbiddenCatalogNames)
        {
            Assert.DoesNotContain(products, p =>
                p.Name.Contains(forbidden, StringComparison.OrdinalIgnoreCase)
                || p.Id.Contains(forbidden.Replace(" ", "-"), StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void Catalog_OnlyActiveDispenserHasAmazonCta_ComingSoonHaveEmptyUrl()
    {
        var products = LoadCatalog();
        var available = products.Where(ProductHelpers.ShowAmazonCta).ToList();

        Assert.Single(available);
        Assert.Equal("dispenser-flow-quadrado-branco-001", available[0].Id);
        Assert.Equal("Ou Dispenser Quadrado 1L Branco Linha Flow", available[0].Name);
        Assert.Equal("https://www.amazon.com.br/dp/B0GKPPS5YH?m=A20TN3HCSY6KZV", available[0].AmazonUrl);
        Assert.False(available[0].ComingSoon);

        Assert.Equal(9, products.Count(p => p.ComingSoon));
        foreach (var comingSoon in products.Where(p => p.ComingSoon))
        {
            Assert.True(string.IsNullOrEmpty(comingSoon.AmazonUrl),
                $"Item '{comingSoon.Id}' está Em breve e não deve ter amazonUrl preenchida.");
            Assert.False(ProductHelpers.ShowAmazonCta(comingSoon));
        }
    }

    [Fact]
    public void Catalog_EveryProductHasDedicatedPhotoAsset()
    {
        var products = LoadCatalog();
        var wwwroot = Path.Combine(FindRepoRoot(), "wwwroot");

        Assert.NotEmpty(products);
        foreach (var product in products)
        {
            Assert.False(
                string.Equals(product.ImageUrl, "images/placeholder-product.png", StringComparison.OrdinalIgnoreCase),
                $"Produto '{product.Id}' ainda aponta para o placeholder genérico.");
            Assert.False(string.IsNullOrWhiteSpace(product.ImageUrl));

            var physical = Path.Combine(
                wwwroot,
                product.ImageUrl.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar));
            Assert.True(File.Exists(physical), $"Arquivo ausente para '{product.Id}': {physical}");
            Assert.True(new FileInfo(physical).Length > 4000, $"Imagem de '{product.Id}' parece vazia ({physical}).");
        }
    }

    [Fact]
    public void ProductDetail_RendersProductImage()
    {
        using var ctx = CreateContext();
        var product = Make(comingSoon: false, name: "Dispenser Quadrado Flow 1L");
        var cut = ctx.RenderComponent<ProductDetail>(p => p
            .Add(c => c.Product, product)
            .Add(c => c.IsOpen, true));

        var img = cut.Find("img.product-detail__image");
        Assert.Equal(product.ImageUrl, img.GetAttribute("src"));
        Assert.Equal(product.Name, img.GetAttribute("alt"));
    }

    [Fact]
    public void Catalog_InStockDispenserPriceUnchanged()
    {
        var dispenser = LoadCatalog().Single(p => p.Id == "dispenser-flow-quadrado-branco-001");
        Assert.Equal(39.90m, dispenser.Price);
    }

    [Fact]
    public void ProductCard_ComingSoon_ShowsBadgeAndHidesAmazonCta()
    {
        using var ctx = CreateContext();
        var product = Make(amazonUrl: "", comingSoon: true, name: "Rodo Linha Flow");
        var cut = ctx.RenderComponent<ProductCard>(p => p.Add(c => c.Product, product));

        Assert.Empty(cut.FindAll("a.product-card__amazon-btn"));
        Assert.Equal("Em breve", cut.Find(".product-card__badge").TextContent.Trim());
        Assert.Equal("Disponível em breve na Amazon",
            cut.Find(".product-card__coming-soon").TextContent.Trim());
    }

    [Fact]
    public void ProductCard_ComingSoonWithPrefillUrl_StillHidesAmazonCta()
    {
        using var ctx = CreateContext();
        var product = Make(comingSoon: true);
        var cut = ctx.RenderComponent<ProductCard>(p => p.Add(c => c.Product, product));

        Assert.Empty(cut.FindAll("a.product-card__amazon-btn"));
        Assert.Single(cut.FindAll(".product-card__badge"));
    }

    [Fact]
    public void ProductCard_Available_ShowsAmazonCtaWithoutBadge()
    {
        using var ctx = CreateContext();
        var product = Make(comingSoon: false);
        var cut = ctx.RenderComponent<ProductCard>(p => p.Add(c => c.Product, product));

        var cta = cut.Find("a.product-card__amazon-btn");
        Assert.Equal(product.AmazonUrl, cta.GetAttribute("href"));
        Assert.Equal("Comprar na Amazon", cta.TextContent.Trim());
        Assert.Empty(cut.FindAll(".product-card__badge"));
    }

    [Fact]
    public void ProductDetail_ComingSoon_ShowsBadgeAndHidesAmazonCta()
    {
        using var ctx = CreateContext();
        var product = Make(amazonUrl: "", comingSoon: true, name: "Borrifador Linha Flow");
        var cut = ctx.RenderComponent<ProductDetail>(p => p
            .Add(c => c.Product, product)
            .Add(c => c.IsOpen, true));

        Assert.Empty(cut.FindAll("a.product-detail__amazon-btn"));
        Assert.Equal("Em breve", cut.Find(".product-detail__badge").TextContent.Trim());
        Assert.Equal("Disponível em breve na Amazon",
            cut.Find(".product-detail__coming-soon").TextContent.Trim());
    }

    [Fact]
    public void ProductDetail_Available_ShowsAmazonCta()
    {
        using var ctx = CreateContext();
        var product = Make(comingSoon: false);
        var cut = ctx.RenderComponent<ProductDetail>(p => p
            .Add(c => c.Product, product)
            .Add(c => c.IsOpen, true));

        var cta = cut.Find("a.product-detail__amazon-btn");
        Assert.Equal("Comprar na Amazon", cta.TextContent.Trim());
        Assert.Empty(cut.FindAll(".product-detail__badge"));
    }

    [Fact]
    public void FlippingComingSoonFalse_WithValidAmazonUrl_ReenablesCta()
    {
        var restocked = Make(comingSoon: true) with { ComingSoon = false };

        Assert.True(ProductHelpers.ShowAmazonCta(restocked));

        using var ctx = CreateContext();
        var cut = ctx.RenderComponent<ProductCard>(p => p.Add(c => c.Product, restocked));
        Assert.Single(cut.FindAll("a.product-card__amazon-btn"));
        Assert.Empty(cut.FindAll(".product-card__badge"));
    }

    [Fact]
    public void DisplayImageUrl_UsesPlaceholderWhenMissing()
    {
        Assert.Equal("images/placeholder-product.png", ProductHelpers.DisplayImageUrl(null));
        Assert.Equal("images/placeholder-product.png", ProductHelpers.DisplayImageUrl(""));
        Assert.Equal("images/placeholder-product.png", ProductHelpers.DisplayImageUrl("   "));
        Assert.Equal("images/products/foo.jpg", ProductHelpers.DisplayImageUrl("images/products/foo.jpg"));
    }

    [Fact]
    public void ProductDetail_EmptyImageUrl_StillRendersPlaceholderImage()
    {
        using var ctx = CreateContext();
        var product = Make(name: "Produto sem foto") with { ImageUrl = "" };
        var cut = ctx.RenderComponent<ProductDetail>(p => p
            .Add(c => c.Product, product)
            .Add(c => c.IsOpen, true));

        var img = cut.Find("img.product-detail__image");
        Assert.Equal("images/placeholder-product.png", img.GetAttribute("src"));
        Assert.Contains("images/placeholder-product.png", img.GetAttribute("onerror"));
        Assert.NotNull(cut.Find(".product-detail__meta"));
    }

    [Fact]
    public void ProductCard_ComingSoon_UsesComingSoonModifierClass()
    {
        using var ctx = CreateContext();
        var cut = ctx.RenderComponent<ProductCard>(p =>
            p.Add(c => c.Product, Make(amazonUrl: "", comingSoon: true)));

        Assert.Contains("product-card--coming-soon", cut.Find("article.product-card").GetAttribute("class"));
    }

    [Fact]
    public void CopyGenerator_ComingSoon_EndsWithSoftAvailabilityCopy()
    {
        var comingSoon = ProductCopyGenerator.Generate(Make(comingSoon: true));
        var available = ProductCopyGenerator.Generate(Make(comingSoon: false));

        Assert.NotNull(comingSoon);
        Assert.EndsWith("Disponível em breve na Amazon.", comingSoon);
        Assert.DoesNotContain("Garanta o seu agora", comingSoon);

        Assert.NotNull(available);
        Assert.EndsWith("Garanta o seu agora e aproveite!", available);
    }
}
