// Feature: amazon-product-showcase

using System.Collections.Generic;
using System.Linq;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using MaterDomus.Web.Models;
using MaterDomus.Web.Services;

namespace MaterDomus.Tests.Unit;

/// <summary>
/// Testes de propriedade para a lógica de filtragem de produtos.
/// Valida: Requisitos 1.1, 4.2, 4.3, 4.4, 4.6
/// </summary>
public class ProductFilterTests
{
    // -------------------------------------------------------------------------
    // Pure filter function (mirrors Produtos.razor FilteredProducts logic)
    // -------------------------------------------------------------------------

    private static IEnumerable<Product> ApplyFilters(
        IEnumerable<Product> products,
        string searchText,
        string selectedCategory,
        bool showFavoritesOnly,
        FavoritesService favoritesService)
    {
        return products
            .Where(p => string.IsNullOrEmpty(searchText) ||
                        p.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                        p.Description.Contains(searchText, StringComparison.OrdinalIgnoreCase))
            .Where(p => string.IsNullOrEmpty(selectedCategory) ||
                        p.Category == selectedCategory)
            .Where(p => !showFavoritesOnly || favoritesService.IsFavorite(p.Id));
    }

    // -------------------------------------------------------------------------
    // Generator helpers
    // -------------------------------------------------------------------------

    private static readonly string[] FixedCategories = { "Organização", "Cozinha", "Limpeza", "Casa" };

    private static FsCheck.Gen<List<Product>> ProductListGen()
    {
        var strGen = ArbMap.Default.ArbFor<NonEmptyString>().Generator.Select(s => s.Get);
        var catGen = Gen.Elements(FixedCategories);

        return Gen.ListOf(strGen, 5)
            .Select(ids => ids.Distinct().ToList())
            .SelectMany(uniqueIds =>
                Gen.ListOf(catGen, uniqueIds.Count)
                .SelectMany(cats =>
                    Gen.ListOf(strGen, uniqueIds.Count)
                    .SelectMany(names =>
                        Gen.ListOf(strGen, uniqueIds.Count)
                        .Select(descs =>
                            uniqueIds.Select((id, i) => new Product(
                                Id: id,
                                Name: names[i],
                                Description: descs[i],
                                ImageUrl: "",
                                Category: cats[i],
                                Price: (decimal)(i + 1) * 9.99m,
                                AmazonUrl: "https://www.amazon.com.br/dp/TEST"
                            )).ToList()
                        )
                    )
                )
            );
    }

    // -------------------------------------------------------------------------
    // Property 1: Full catalog without filters
    // -------------------------------------------------------------------------

    // Feature: amazon-product-showcase, Property 1: Catálogo completo sem filtros
    /// <summary>
    /// Para qualquer lista não-vazia de produtos com todos os filtros desativados,
    /// o conjunto de produtos exibidos deve ser igual ao catálogo completo.
    ///
    /// Validates: Requirements 1.1, 4.6
    /// </summary>
    [Property(MaxTest = 100)]
    public Property FullCatalog_WithNoFilters_ReturnsAllProducts()
    {
        return Prop.ForAll(ProductListGen().ToArbitrary(), products =>
        {
            var service = new FavoritesService();
            var result = ApplyFilters(products, "", "", false, service).ToList();

            var resultIds = new HashSet<string>(result.Select(p => p.Id));
            var expectedIds = new HashSet<string>(products.Select(p => p.Id));

            return resultIds.SetEquals(expectedIds)
                .ToProperty()
                .Label($"Expected {expectedIds.Count} products, got {resultIds.Count}");
        });
    }

    // -------------------------------------------------------------------------
    // Property 8: Text filter correctness
    // -------------------------------------------------------------------------

    // Feature: amazon-product-showcase, Property 8: Filtro de texto — correção do resultado
    /// <summary>
    /// Para qualquer lista de produtos e qualquer string de busca, o conjunto filtrado
    /// deve ser exatamente os produtos cujo nome ou descrição contém o termo (case-insensitive).
    ///
    /// Validates: Requirement 4.2
    /// </summary>
    [Property(MaxTest = 100)]
    public Property TextFilter_ReturnsExactMatches()
    {
        var strGen = ArbMap.Default.ArbFor<NonEmptyString>().Generator.Select(s => s.Get);

        var gen = ProductListGen().SelectMany(products =>
            strGen.Select(searchTerm => (products, searchTerm))
        );

        return Prop.ForAll(gen.ToArbitrary(), tuple =>
        {
            var (products, searchTerm) = tuple;
            var service = new FavoritesService();

            var result = ApplyFilters(products, searchTerm, "", false, service).ToList();

            var expected = products
                .Where(p => p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                            p.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var resultIds = new HashSet<string>(result.Select(p => p.Id));
            var expectedIds = new HashSet<string>(expected.Select(p => p.Id));

            return resultIds.SetEquals(expectedIds)
                .ToProperty()
                .Label($"Text '{searchTerm}': expected {expectedIds.Count}, got {resultIds.Count}");
        });
    }

    // -------------------------------------------------------------------------
    // Property 9: Category filter correctness
    // -------------------------------------------------------------------------

    // Feature: amazon-product-showcase, Property 9: Filtro de categoria — correção do resultado
    /// <summary>
    /// Para qualquer lista de produtos e qualquer categoria presente nessa lista,
    /// o conjunto filtrado deve ser exatamente os produtos dessa categoria.
    ///
    /// Validates: Requirement 4.3
    /// </summary>
    [Property(MaxTest = 100)]
    public Property CategoryFilter_ReturnsExactMatches()
    {
        var gen = ProductListGen().SelectMany(products =>
        {
            var categories = products.Select(p => p.Category).Distinct().ToArray();
            return Gen.Elements(categories)
                .Select(cat => (products, selectedCategory: cat));
        });

        return Prop.ForAll(gen.ToArbitrary(), tuple =>
        {
            var (products, selectedCategory) = tuple;
            var service = new FavoritesService();

            var result = ApplyFilters(products, "", selectedCategory, false, service).ToList();
            var expected = products.Where(p => p.Category == selectedCategory).ToList();

            var resultIds = new HashSet<string>(result.Select(p => p.Id));
            var expectedIds = new HashSet<string>(expected.Select(p => p.Id));

            return resultIds.SetEquals(expectedIds)
                .ToProperty()
                .Label($"Category '{selectedCategory}': expected {expectedIds.Count}, got {resultIds.Count}");
        });
    }

    // -------------------------------------------------------------------------
    // Property 10: Filter composition (intersection)
    // -------------------------------------------------------------------------

    // Feature: amazon-product-showcase, Property 10: Composição de filtros (interseção)
    /// <summary>
    /// Com busca por texto e filtro por categoria ambos ativos, o resultado combinado
    /// deve ser igual à interseção dos dois filtros aplicados independentemente.
    ///
    /// Validates: Requirement 4.4
    /// </summary>
    [Property(MaxTest = 100)]
    public Property FilterComposition_IsIntersectionOfIndividualFilters()
    {
        var strGen = ArbMap.Default.ArbFor<NonEmptyString>().Generator.Select(s => s.Get);

        var gen = ProductListGen().SelectMany(products =>
        {
            var categories = products.Select(p => p.Category).Distinct().ToArray();
            return Gen.Elements(categories)
                .SelectMany(cat =>
                    strGen.Select(searchTerm => (products, searchTerm, selectedCategory: cat))
                );
        });

        return Prop.ForAll(gen.ToArbitrary(), tuple =>
        {
            var (products, searchTerm, selectedCategory) = tuple;
            var service = new FavoritesService();

            var combined = ApplyFilters(products, searchTerm, selectedCategory, false, service).ToList();

            var textOnly = ApplyFilters(products, searchTerm, "", false, service).ToList();
            var categoryOnly = ApplyFilters(products, "", selectedCategory, false, service).ToList();
            var intersection = textOnly.IntersectBy(categoryOnly.Select(p => p.Id), p => p.Id).ToList();

            var combinedIds = new HashSet<string>(combined.Select(p => p.Id));
            var intersectionIds = new HashSet<string>(intersection.Select(p => p.Id));

            return combinedIds.SetEquals(intersectionIds)
                .ToProperty()
                .Label($"Combined ({combinedIds.Count}) != intersection ({intersectionIds.Count})");
        });
    }
}
