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
/// Testes de propriedade para <see cref="FavoritesService"/>.
/// Valida: Requisitos 3.2, 3.3, 3.4, 3.6
/// </summary>
public class FavoritesServiceTests
{
    // -------------------------------------------------------------------------
    // Property 5: Toggle round-trip
    // -------------------------------------------------------------------------

    // Feature: amazon-product-showcase, Property 5: Toggle de favorito — round-trip
    /// <summary>
    /// Para qualquer produto e qualquer estado inicial de favorito, chamar Toggle
    /// duas vezes consecutivas deve restaurar o estado original.
    ///
    /// Validates: Requirements 3.2, 3.3
    /// </summary>
    [Property(MaxTest = 100)]
    public bool ToggleRoundTrip_RestoresOriginalState(NonEmptyString idNes, bool startAsFavorite)
    {
        var id = idNes.Get;
        var service = new FavoritesService();

        if (startAsFavorite)
            service.Toggle(id);

        var stateBefore = service.IsFavorite(id);

        service.Toggle(id);
        service.Toggle(id);

        return stateBefore == service.IsFavorite(id);
    }

    // -------------------------------------------------------------------------
    // Property 6: Exact favorites count matches set cardinality
    // -------------------------------------------------------------------------

    // Feature: amazon-product-showcase, Property 6: Contagem exata de favoritos
    /// <summary>
    /// Para qualquer lista de IDs e contagens de toggles, FavoritesService.Count
    /// deve ser igual ao número de IDs com número ímpar de toggles.
    ///
    /// Validates: Requirement 3.4
    /// </summary>
    [Property(MaxTest = 100)]
    public Property FavoritesCount_MatchesSetCardinality()
    {
        var strGen = ArbMap.Default.ArbFor<NonEmptyString>().Generator.Select(s => s.Get);
        var toggleCountGen = Gen.Choose(1, 3);

        var gen = Gen.ListOf(strGen, 5)
            .Select(ids => ids.Distinct().ToList())
            .SelectMany(ids =>
                Gen.ListOf(toggleCountGen, ids.Count)
                    .Select(toggles => (ids, toggles: toggles.ToList()))
            );

        return Prop.ForAll(gen.ToArbitrary(), tuple =>
        {
            var (ids, toggles) = tuple;
            var service = new FavoritesService();

            for (int i = 0; i < ids.Count; i++)
                for (int t = 0; t < toggles[i]; t++)
                    service.Toggle(ids[i]);

            var expectedCount = ids.Where((_, i) => toggles[i] % 2 == 1).Count();

            return (service.Count == expectedCount)
                .ToProperty()
                .Label($"Count ({service.Count}) should equal expected cardinality ({expectedCount})");
        });
    }

    // -------------------------------------------------------------------------
    // Property 7: Favorites filter returns exact subset
    // -------------------------------------------------------------------------

    // Feature: amazon-product-showcase, Property 7: Filtro de favoritos retorna subconjunto exato
    /// <summary>
    /// Para qualquer lista de produtos e qualquer subconjunto de IDs marcados como
    /// favoritos, ativar o filtro showFavoritesOnly deve retornar exatamente os
    /// produtos cujo Id está no conjunto de favoritos — nem mais, nem menos.
    ///
    /// Validates: Requirement 3.6
    /// </summary>
    [Property(MaxTest = 100)]
    public Property FavoritesFilter_ReturnsExactSubset()
    {
        var strGen = ArbMap.Default.ArbFor<NonEmptyString>().Generator.Select(s => s.Get);

        var gen = Gen.ListOf(strGen, 5)
            .Select(ids => ids.Distinct().ToList())
            .SelectMany(uniqueIds =>
            {
                var products = uniqueIds.Select((id, i) => new Product(
                    Id: id,
                    Name: $"Produto {i}",
                    Description: $"Descrição {i}",
                    ImageUrl: "",
                    Category: "Teste",
                    Price: (decimal)(i + 1) * 9.99m,
                    AmazonUrl: "https://www.amazon.com.br/dp/TEST"
                )).ToList();

                return Gen.Choose(0, uniqueIds.Count)
                    .SelectMany(subsetSize =>
                        Gen.Shuffle(uniqueIds.ToArray())
                            .Select(shuffled => (products, favoriteIds: shuffled.Take(subsetSize).ToList()))
                    );
            });

        return Prop.ForAll(gen.ToArbitrary(), tuple =>
        {
            var (products, favoriteIds) = tuple;
            var service = new FavoritesService();

            foreach (var id in favoriteIds)
                service.Toggle(id);

            var filtered = products.Where(p => service.IsFavorite(p.Id)).ToList();
            var expectedIds = new HashSet<string>(favoriteIds);
            var filteredIds = new HashSet<string>(filtered.Select(p => p.Id));

            return filteredIds.SetEquals(expectedIds)
                .ToProperty()
                .Label($"Filtered [{string.Join(", ", filteredIds)}] != expected [{string.Join(", ", expectedIds)}]");
        });
    }
}
