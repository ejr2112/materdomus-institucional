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
/// img; produto com duas ambientadas empilha três slides e troca pelo ponto.
/// </summary>
public class ProductCardLifestyleGalleryTests
{
    private const string AltCozinha =
        "Organizador de parede branco instalado na parede da cozinha, acima da bancada";

    private const string AltLavanderia =
        "Organizador de parede branco instalado na lavanderia, acima da máquina de lavar";

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
        Assert.Equal(3, slides.Count);

        Assert.Equal(product.ImageUrl, slides[0].GetAttribute("src"));
        Assert.Equal(product.Name, slides[0].GetAttribute("alt"));
        Assert.Contains("product-card__image", slides[0].GetAttribute("class"));
        Assert.Contains("product-card__slide--active", slides[0].GetAttribute("class"));
        Assert.Contains("images/placeholder-product.png", slides[0].GetAttribute("onerror"));
        Assert.Null(slides[0].GetAttribute("loading"));
        Assert.Equal("1200", slides[0].GetAttribute("width"));
        Assert.Equal("1200", slides[0].GetAttribute("height"));

        AssertLifestyleSlide(slides[1], product.LifestyleImages![0], hoverTarget: true);
        AssertLifestyleSlide(slides[2], product.LifestyleImages[1], hoverTarget: false);

        var dots = cut.FindAll("button.product-card__dot");
        Assert.Equal(3, dots.Count);
        Assert.Equal("Ver foto 1 de 3", dots[0].GetAttribute("aria-label"));
        Assert.Equal("Ver foto 2 de 3", dots[1].GetAttribute("aria-label"));
        Assert.Equal("Ver foto 3 de 3", dots[2].GetAttribute("aria-label"));
        Assert.Equal("true", dots[0].GetAttribute("aria-pressed"));
        Assert.Equal("true", dots[0].GetAttribute("aria-current"));
        Assert.Equal("button", dots[2].GetAttribute("type"));

        dots[2].Click();

        Assert.Equal(0, detailCalls);
        slides = cut.FindAll("img.product-card__slide");
        Assert.DoesNotContain("product-card__slide--active", slides[0].GetAttribute("class"));
        Assert.Contains("product-card__slide--active", slides[2].GetAttribute("class"));
        dots = cut.FindAll("button.product-card__dot");
        Assert.Equal("true", dots[2].GetAttribute("aria-pressed"));
        Assert.Equal("true", dots[2].GetAttribute("aria-current"));
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

    private static void AssertLifestyleSlide(AngleSharp.Dom.IElement img, ProductLifestyleImage expected, bool hoverTarget)
    {
        Assert.Equal(expected.Url, img.GetAttribute("src"));
        Assert.Equal(expected.Alt, img.GetAttribute("alt"));
        Assert.Equal("lazy", img.GetAttribute("loading"));
        Assert.Equal("async", img.GetAttribute("decoding"));
        Assert.Equal("1200", img.GetAttribute("width"));
        Assert.Equal("1200", img.GetAttribute("height"));
        Assert.Null(img.GetAttribute("onerror"));
        var cls = img.GetAttribute("class") ?? "";
        Assert.Contains("product-card__slide", cls);
        if (hoverTarget)
            Assert.Contains("product-card__slide--hover-target", cls);
        else
            Assert.DoesNotContain("product-card__slide--hover-target", cls);
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
