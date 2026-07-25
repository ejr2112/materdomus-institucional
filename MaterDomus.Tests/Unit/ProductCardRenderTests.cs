// Feature: amazon-product-showcase

using System.Collections.Generic;
using Bunit;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using MaterDomus.Web.Models;
using MaterDomus.Web.Services;
using MaterDomus.Web.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace MaterDomus.Tests.Unit;

/// <summary>
/// Testes de propriedade baseados em componentes para ProductCard.razor.
/// Usa bUnit para renderização e FsCheck para geração de dados.
/// Valida: Requisitos 2.1, 2.4, 5.4, 5.6, 5.7
/// </summary>
public class ProductCardRenderTests
{
    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static Bunit.TestContext CreateContext()
    {
        var ctx = new Bunit.TestContext();
        ctx.Services.AddScoped<FavoritesService>();
        return ctx;
    }

    private static Product MakeProduct(string id, string amazonUrl) =>
        new Product(
            Id: id,
            Name: "Produto Teste",
            Description: "Descrição de teste para o produto.",
            ImageUrl: "",
            Category: "Teste",
            Price: 49.90m,
            AmazonUrl: amazonUrl
        );

    // -------------------------------------------------------------------------
    // Property 4: Amazon button visibility
    // -------------------------------------------------------------------------

    // Feature: amazon-product-showcase, Property 4: Visibilidade do botão Amazon
    /// <summary>
    /// Para qualquer produto, o botão "Comprar na Amazon" deve ser visível
    /// se e somente se o campo AmazonUrl for uma string não-vazia.
    ///
    /// Validates: Requirements 2.1, 2.4
    /// </summary>
    [Property(MaxTest = 100)]
    public Property AmazonButton_VisibleIffAmazonUrlIsNonEmpty()
    {
        var strGen = ArbMap.Default.ArbFor<NonEmptyString>().Generator.Select(s => s.Get);

        var gen = strGen.SelectMany(id =>
            ArbMap.Default.ArbFor<bool>().Generator.SelectMany(hasUrl =>
                strGen.Select(suffix => (
                    id,
                    amazonUrl: hasUrl ? $"https://www.amazon.com.br/dp/{suffix}" : "",
                    hasUrl
                ))
            )
        );

        return Prop.ForAll(gen.ToArbitrary(), tuple =>
        {
            var (id, amazonUrl, hasUrl) = tuple;

            using var ctx = CreateContext();
            var product = MakeProduct(id, amazonUrl);
            var cut = ctx.RenderComponent<ProductCard>(
                parameters => parameters.Add(p => p.Product, product));

            var buttons = cut.FindAll("a.product-card__amazon-btn");
            bool result = hasUrl ? buttons.Count == 1 : buttons.Count == 0;

            return result
                .ToProperty()
                .Label($"AmazonUrl='{amazonUrl}', hasUrl={hasUrl}: found {buttons.Count} buttons");
        });
    }

    // -------------------------------------------------------------------------
    // Property 11: ARIA attributes on favorite button
    // -------------------------------------------------------------------------

    // Feature: amazon-product-showcase, Property 11: Atributos ARIA do botão de favorito
    /// <summary>
    /// Para qualquer produto e qualquer estado de favorito, o botão de favorito deve ter:
    /// - aria-pressed="true" e aria-label="Remover dos favoritos" quando favoritado
    /// - aria-pressed="false" e aria-label="Favoritar produto" quando não favoritado
    ///
    /// Validates: Requirements 5.4, 5.6, 5.7
    /// </summary>
    [Property(MaxTest = 100)]
    public Property FavoriteButton_HasCorrectAriaAttributes()
    {
        var strGen = ArbMap.Default.ArbFor<NonEmptyString>().Generator.Select(s => s.Get);

        var gen = strGen.SelectMany(id =>
            ArbMap.Default.ArbFor<bool>().Generator
                .Select(isFavorited => (id, isFavorited))
        );

        return Prop.ForAll(gen.ToArbitrary(), tuple =>
        {
            var (id, isFavorited) = tuple;

            using var ctx = CreateContext();

            var favService = ctx.Services.GetRequiredService<FavoritesService>();
            if (isFavorited)
                favService.Toggle(id);

            var product = MakeProduct(id, "https://www.amazon.com.br/dp/TEST");
            var cut = ctx.RenderComponent<ProductCard>(
                parameters => parameters.Add(p => p.Product, product));

            var btn = cut.Find("button.product-card__favorite-btn");
            var ariaPressed = btn.GetAttribute("aria-pressed");
            var ariaLabel = btn.GetAttribute("aria-label");

            bool result;
            string description;

            if (isFavorited)
            {
                result = ariaPressed == "true" && ariaLabel == "Remover dos favoritos";
                description = $"Favorited: aria-pressed='{ariaPressed}', aria-label='{ariaLabel}'";
            }
            else
            {
                result = ariaPressed == "false" && ariaLabel == "Favoritar produto";
                description = $"Not favorited: aria-pressed='{ariaPressed}', aria-label='{ariaLabel}'";
            }

            return result.ToProperty().Label(description);
        });
    }
}
