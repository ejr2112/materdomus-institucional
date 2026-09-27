using Bunit;
using MaterDomus.Web.Models;
using MaterDomus.Web.Services;
using MaterDomus.Web.Shared;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MaterDomus.Tests.Unit;

/// <summary>
/// Mini-galeria do cartão: produto sem fotos ambientadas permanece com um único
/// img; produto com ambientadas navega só entre elas e usa a foto de estúdio
/// apenas como fallback de carregamento.
/// </summary>
public class ProductCardLifestyleGalleryTests
{
    private const string AltCozinha =
        "Organizador de parede branco fixado no azulejo da cozinha, acima da bancada de madeira";

    private const string AltLavanderia =
        "Organizador de parede branco fixado no azulejo verde-sálvia da lavanderia, acima do tanque";

    private static Bunit.TestContext CreateContext()
    {
        var ctx = new Bunit.TestContext();
        ctx.Services.AddScoped<FavoritesService>();
        return ctx;
    }

    private static Product MakeProduct(params ProductLifestyleImage[] lifestyle)
    {
        var product = new Product(
            Id: "organizador-parede-armario-branco-b0gkq4vvpq",
            Name: "Ou Organizador de Parede e Armário Branco Linha Flow",
            Description: "Organizador branco da Linha Flow para parede ou interior de armário.",
            ImageUrl: "images/products/organizador-parede-armario-branco-b0gkq4vvpq.png",
            Category: "Organização",
            Price: 58.49m,
            AmazonUrl: "https://www.amazon.com.br/dp/B0GKQ4VVPQ?m=A20TN3HCSY6KZV",
            ComingSoon: false);

        if (lifestyle.Length == 0)
            return product;

        return product with { LifestyleImages = lifestyle };
    }

    [Fact]
    public void ProductCard_WithoutLifestyleImages_RendersSingleImage()
    {
        using var ctx = CreateContext();
        var product = MakeProduct();
        var cut = ctx.RenderComponent<ProductCard>(parameters => parameters.Add(p => p.Product, product));

        Assert.DoesNotContain("product-card__gallery", cut.Markup);
        Assert.DoesNotContain("product-card__dot", cut.Markup);
        Assert.DoesNotContain("product-card__slide", cut.Markup);
        Assert.Empty(cut.FindAll("button.product-card__arrow"));

        var img = Assert.Single(cut.FindAll("img"));
        Assert.Equal("product-card__image", img.GetAttribute("class"));
        Assert.Equal(product.ImageUrl, img.GetAttribute("src"));
        Assert.Equal(product.Name, img.GetAttribute("alt"));
        Assert.Contains("images/placeholder-product.png", img.GetAttribute("onerror"));
        Assert.Null(img.GetAttribute("loading"));
        Assert.Null(img.GetAttribute("width"));
    }

    [Fact]
    public void ProductCard_WithTwoLifestyleImages_RendersGalleryAndDotDoesNotOpenDetails()
    {
        using var ctx = CreateContext();
        var product = MakeProduct(
            new ProductLifestyleImage(
                "images/products/organizador-parede-armario-branco-b0gkq4vvpq-ambientada-1.webp",
                AltCozinha),
            new ProductLifestyleImage(
                "images/products/organizador-parede-armario-branco-b0gkq4vvpq-ambientada-2.webp",
                AltLavanderia));

        var detailCalls = 0;
        var cut = ctx.RenderComponent<ProductCard>(parameters => parameters
            .Add(p => p.Product, product)
            .Add(p => p.OnVerDetalhes, (Product _) => detailCalls++));

        var slides = cut.FindAll("img.product-card__slide");
        Assert.Equal(2, slides.Count);

        AssertLifestyleSlide(slides[0], product.LifestyleImages![0], hoverTarget: false, eager: true);
        Assert.Contains("product-card__slide--active", slides[0].GetAttribute("class"));
        AssertLifestyleSlide(slides[1], product.LifestyleImages[1], hoverTarget: true, eager: false);
        Assert.DoesNotContain("product-card__slide--active", slides[1].GetAttribute("class"));

        var gallery = cut.Find(".product-card__gallery");
        Assert.Equal("0", gallery.GetAttribute("data-active-index"));
        Assert.DoesNotContain("is-browsing", gallery.GetAttribute("class") ?? "");

        var dots = cut.FindAll("button.product-card__dot");
        Assert.Equal(2, dots.Count);
        Assert.Equal("Ver foto 1 de 2", dots[0].GetAttribute("aria-label"));
        Assert.Equal("Ver foto 2 de 2", dots[1].GetAttribute("aria-label"));
        Assert.Contains("product-card__dot--active", dots[0].GetAttribute("class"));
        Assert.Equal("true", dots[0].GetAttribute("aria-pressed"));
        Assert.Equal("true", dots[0].GetAttribute("aria-current"));
        Assert.Equal("button", dots[1].GetAttribute("type"));

        dots[1].Click();

        Assert.Equal(0, detailCalls);
        slides = cut.FindAll("img.product-card__slide");
        Assert.DoesNotContain("product-card__slide--active", slides[0].GetAttribute("class"));
        Assert.Contains("product-card__slide--active", slides[1].GetAttribute("class"));
        dots = cut.FindAll("button.product-card__dot");
        Assert.Equal("true", dots[1].GetAttribute("aria-pressed"));
        Assert.Equal("true", dots[1].GetAttribute("aria-current"));
        Assert.Equal("false", dots[0].GetAttribute("aria-pressed"));
        Assert.Null(dots[0].GetAttribute("aria-current"));

        cut.Find("button.product-card__arrow--next").Click();
        Assert.Equal(0, detailCalls);
        Assert.Contains(
            "product-card__slide--active",
            cut.FindAll("img.product-card__slide")[0].GetAttribute("class"));

        Assert.Equal("Foto anterior", cut.Find("button.product-card__arrow--prev").GetAttribute("aria-label"));
        Assert.Equal("Próxima foto", cut.Find("button.product-card__arrow--next").GetAttribute("aria-label"));

        cut.Find("button.product-card__details-btn").Click();
        Assert.Equal(1, detailCalls);
    }

    [Fact]
    public void ProductCard_AfterSecondDot_DisablesHoverOverrideOfSelectedSlide()
    {
        using var ctx = CreateContext();
        var product = MakeProduct(
            new ProductLifestyleImage(
                "images/products/organizador-parede-armario-branco-b0gkq4vvpq-ambientada-1.webp",
                AltCozinha),
            new ProductLifestyleImage(
                "images/products/organizador-parede-armario-branco-b0gkq4vvpq-ambientada-2.webp",
                AltLavanderia));

        var cut = ctx.RenderComponent<ProductCard>(parameters => parameters.Add(p => p.Product, product));

        var slides = cut.FindAll("img.product-card__slide");
        Assert.Contains("product-card__slide--hover-target", slides[1].GetAttribute("class"));
        Assert.DoesNotContain("product-card__slide--hover-target", slides[0].GetAttribute("class"));
        Assert.Equal(product.LifestyleImages![1].Url, slides[1].GetAttribute("src"));

        var gallery = cut.Find(".product-card__gallery");
        Assert.Equal("0", gallery.GetAttribute("data-active-index"));
        Assert.DoesNotContain("is-browsing", gallery.GetAttribute("class") ?? "");

        cut.FindAll("button.product-card__dot")[1].Click();

        gallery = cut.Find(".product-card__gallery");
        Assert.Contains("is-browsing", gallery.GetAttribute("class") ?? "");
        Assert.Equal("1", gallery.GetAttribute("data-active-index"));
        Assert.Contains(
            "product-card__slide--active",
            cut.FindAll("img.product-card__slide")[1].GetAttribute("class"));

        var css = File.ReadAllText(RepoPath("wwwroot", "css", "produtos.css"));
        const string hoverMedia = "@media (hover: hover) and (pointer: fine)";
        const string hoverHide =
            ".product-card__gallery[data-active-index=\"0\"]:not(.is-browsing):hover .product-card__slide";
        const string hoverShow =
            ".product-card__gallery[data-active-index=\"0\"]:not(.is-browsing):hover .product-card__slide--hover-target";
        const string reducedKeepActive =
            ".product-card__gallery[data-active-index=\"0\"]:not(.is-browsing):hover .product-card__slide--active";
        Assert.Contains(hoverMedia, css, StringComparison.Ordinal);
        Assert.Contains(hoverHide, css, StringComparison.Ordinal);
        Assert.Contains(hoverShow, css, StringComparison.Ordinal);
        Assert.Contains("@media (prefers-reduced-motion: reduce)", css, StringComparison.Ordinal);
        Assert.Contains(reducedKeepActive, css, StringComparison.Ordinal);
        Assert.DoesNotContain(
            ".product-card__gallery:hover .product-card__slide",
            css,
            StringComparison.Ordinal);

        var reducedAt = css.LastIndexOf("@media (prefers-reduced-motion: reduce)", StringComparison.Ordinal);
        var keepAt = css.LastIndexOf(reducedKeepActive, StringComparison.Ordinal);
        Assert.True(reducedAt >= 0 && keepAt > reducedAt);
    }

    [Fact]
    public void ProductCard_ComingSoonWithLifestyle_ShowsBadgeOnFirstStagedSlide()
    {
        using var ctx = CreateContext();
        var product = MakeProduct(
            new ProductLifestyleImage("images/products/a.webp", AltCozinha),
            new ProductLifestyleImage("images/products/b.webp", AltLavanderia)) with
        {
            ComingSoon = true
        };

        var cut = ctx.RenderComponent<ProductCard>(parameters => parameters.Add(p => p.Product, product));

        Assert.Equal("Em breve", cut.Find(".product-card__badge").TextContent.Trim());
        Assert.Contains("product-card--coming-soon", cut.Find("article").GetAttribute("class"));
        var slides = cut.FindAll("img.product-card__slide");
        Assert.Contains("product-card__slide--active", slides[0].GetAttribute("class"));
        Assert.Equal("images/products/a.webp", slides[0].GetAttribute("src"));
        Assert.Empty(cut.FindAll("a.product-card__amazon-btn"));
    }

    [Fact]
    public void ProductCard_WhenProductChanges_ResetsToFirstLifestyleSlide()
    {
        using var ctx = CreateContext();
        var first = MakeProduct(
            new ProductLifestyleImage("images/products/a.webp", AltCozinha),
            new ProductLifestyleImage("images/products/b.webp", AltLavanderia));
        var second = first with
        {
            Id = "outro-produto",
            Name = "Outro produto",
            LifestyleImages = new[]
            {
                new ProductLifestyleImage("images/products/c.webp", "Segunda ambientada visível"),
                new ProductLifestyleImage("images/products/d.webp", "Segunda ambientada seguinte")
            }
        };

        var cut = ctx.RenderComponent<ProductCard>(parameters => parameters.Add(p => p.Product, first));
        cut.FindAll("button.product-card__dot")[1].Click();
        Assert.Contains(
            "product-card__slide--active",
            cut.FindAll("img.product-card__slide")[1].GetAttribute("class"));

        cut.SetParametersAndRender(parameters => parameters.Add(p => p.Product, second));

        var slides = cut.FindAll("img.product-card__slide");
        Assert.Contains("product-card__slide--active", slides[0].GetAttribute("class"));
        Assert.Equal("images/products/c.webp", slides[0].GetAttribute("src"));
        Assert.Equal("0", cut.Find(".product-card__gallery").GetAttribute("data-active-index"));
        Assert.DoesNotContain("is-browsing", cut.Find(".product-card__gallery").GetAttribute("class") ?? "");
    }

    [Fact]
    public void ProductCard_SwipeLeft_AdvancesSlideWithoutOpeningDetails()
    {
        using var ctx = CreateContext();
        var product = MakeProduct(
            new ProductLifestyleImage("images/products/a.webp", AltCozinha),
            new ProductLifestyleImage("images/products/b.webp", AltLavanderia));

        var detailCalls = 0;
        var cut = ctx.RenderComponent<ProductCard>(parameters => parameters
            .Add(p => p.Product, product)
            .Add(p => p.OnVerDetalhes, (Product _) => detailCalls++));

        var gallery = cut.Find(".product-card__gallery");
        gallery.TriggerEvent("ontouchstart", Touch("start", 180, 40));
        gallery.TriggerEvent("ontouchend", Touch("end", 40, 48));

        Assert.Equal(0, detailCalls);
        Assert.Contains(
            "product-card__slide--active",
            cut.FindAll("img.product-card__slide")[1].GetAttribute("class"));
    }

    private static void AssertLifestyleSlide(
        AngleSharp.Dom.IElement img,
        ProductLifestyleImage expected,
        bool hoverTarget,
        bool eager)
    {
        Assert.Equal(expected.Url, img.GetAttribute("src"));
        Assert.Equal(expected.Alt, img.GetAttribute("alt"));
        if (eager)
        {
            Assert.Null(img.GetAttribute("loading"));
            Assert.Null(img.GetAttribute("decoding"));
        }
        else
        {
            Assert.Equal("lazy", img.GetAttribute("loading"));
            Assert.Equal("async", img.GetAttribute("decoding"));
        }

        Assert.Equal("1200", img.GetAttribute("width"));
        Assert.Equal("1200", img.GetAttribute("height"));
        var onerror = img.GetAttribute("onerror") ?? "";
        Assert.Contains("images/placeholder-product.png", onerror);
        Assert.Contains("organizador-parede-armario-branco-b0gkq4vvpq.png", onerror);
        var cls = img.GetAttribute("class") ?? "";
        Assert.Contains("product-card__slide", cls);
        Assert.Contains("product-card__image", cls);
        if (hoverTarget)
            Assert.Contains("product-card__slide--hover-target", cls);
        else
            Assert.DoesNotContain("product-card__slide--hover-target", cls);
    }

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

    private static TouchEventArgs Touch(string phase, double x, double y)
    {
        var point = new TouchPoint
        {
            Identifier = 1,
            ClientX = x,
            ClientY = y
        };

        return new TouchEventArgs
        {
            Touches = phase == "start" ? new[] { point } : Array.Empty<TouchPoint>(),
            ChangedTouches = phase == "end" ? new[] { point } : Array.Empty<TouchPoint>()
        };
    }
}
