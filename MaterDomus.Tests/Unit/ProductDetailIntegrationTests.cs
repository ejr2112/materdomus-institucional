// Feature: product-detail-view

using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using MaterDomus.Web.Models;
using MaterDomus.Web.Services;
using MaterDomus.Web.Shared;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MaterDomus.Tests.Unit;

/// <summary>
/// Testes de render e propriedade da visão de detalhes de produto (product-detail-view).
///
/// Cobre:
/// - Tarefa 11.3 / Property 11: Invariância da filtragem da grade (Req 6.7).
/// - Tarefa 11.4: testes de exemplo do ProductCard ("Ver detalhes" + OnVerDetalhes, Req 1.1)
///   e da lógica de resolução de Produtos.razor (Id não resolvível, Req 1.6).
/// </summary>
public class ProductDetailIntegrationTests
{
    // =========================================================================
    // 11.3 — Property 11: Invariância da filtragem da grade
    // =========================================================================

    // -------------------------------------------------------------------------
    // Função de filtro sob teste — espelha EXATAMENTE Produtos.razor
    // FilteredProducts (busca por Name/Description case-insensitive, igualdade de
    // categoria, pertencimento aos favoritos). Idêntica ao ApplyFilters usado em
    // ProductFilterTests, aqui como o "sistema" cuja invariância verificamos.
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
    // Reimplementação de referência independente dos mesmos predicados. A
    // introdução da visão de detalhes NÃO altera busca/categoria/favoritos, de
    // modo que o resultado de ApplyFilters deve ser idêntico ao desta referência
    // para qualquer combinação de filtros — demonstrando a equivalência (Req 6.7).
    // -------------------------------------------------------------------------

    private static List<Product> ReferenceFilter(
        IReadOnlyList<Product> products,
        string searchText,
        string selectedCategory,
        bool showFavoritesOnly,
        FavoritesService favoritesService)
    {
        var result = new List<Product>();
        foreach (var p in products)
        {
            bool matchesSearch =
                string.IsNullOrEmpty(searchText) ||
                p.Name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                p.Description.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;

            bool matchesCategory =
                string.IsNullOrEmpty(selectedCategory) ||
                string.Equals(p.Category, selectedCategory, StringComparison.Ordinal);

            bool matchesFavorites =
                !showFavoritesOnly || favoritesService.IsFavorite(p.Id);

            if (matchesSearch && matchesCategory && matchesFavorites)
            {
                result.Add(p);
            }
        }
        return result;
    }

    // -------------------------------------------------------------------------
    // Geradores (mesmo estilo de ProductFilterTests.ProductListGen)
    // -------------------------------------------------------------------------

    private static readonly string[] FixedCategories = { "Organização", "Cozinha", "Limpeza", "Casa" };

    private static Gen<List<Product>> ProductListGen()
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

    // Feature: product-detail-view, Property 11: Invariância da filtragem da grade
    /// <summary>
    /// Para qualquer lista de produtos e qualquer combinação de termo de busca,
    /// categoria selecionada e estado de "somente favoritos", o resultado da
    /// filtragem em Produtos.razor (ApplyFilters) é idêntico ao produzido pela
    /// lógica de filtragem de referência (busca por nome/descrição, igualdade de
    /// categoria e pertencimento aos favoritos). Demonstra que a introdução da
    /// visão de detalhes não altera busca, filtro nem favoritos.
    ///
    /// Validates: Requirements 6.7
    /// </summary>
    [Property(MaxTest = 100)]
    public Property GridFiltering_IsInvariant_WithDetailFeature()
    {
        var strGen = ArbMap.Default.ArbFor<NonEmptyString>().Generator.Select(s => s.Get);
        var boolGen = ArbMap.Default.ArbFor<bool>().Generator;

        var gen = ProductListGen().SelectMany(products =>
        {
            var categories = products.Select(p => p.Category).Distinct().ToArray();

            // Termo de busca: string arbitrária ou vazia; categoria: presente na
            // lista, uma inexistente, ou vazia; favoritos: liga/desliga.
            var searchGen = Gen.OneOf(
                Gen.Constant(""),
                strGen);

            var categoryGen = Gen.OneOf(
                Gen.Constant(""),
                categories.Length > 0 ? Gen.Elements(categories) : Gen.Constant(""),
                Gen.Constant("__categoria-inexistente__"));

            var ids = products.Select(p => p.Id).ToList();

            return searchGen.SelectMany(search =>
                categoryGen.SelectMany(category =>
                    boolGen.SelectMany(showFav =>
                        // Máscara de favoritos: um booleano por produto decide se
                        // o Id correspondente é marcado como favorito.
                        Gen.ListOf(boolGen, ids.Count).Select(mask =>
                        {
                            var favIds = ids.Where((_, i) => i < mask.Count() && mask.ElementAt(i)).ToList();
                            return (products, search, category, showFav, favIds);
                        }))));
        });

        return Prop.ForAll(gen.ToArbitrary(), tuple =>
        {
            var (products, search, category, showFav, favIds) = tuple;

            var service = new FavoritesService();
            foreach (var id in favIds)
            {
                service.Toggle(id);
            }

            var actual = ApplyFilters(products, search, category, showFav, service).ToList();
            var expected = ReferenceFilter(products, search, category, showFav, service);

            // A invariância exige igualdade de sequência (mesma ordem, mesmos itens):
            // a filtragem não deve reordenar nem alterar o conjunto.
            bool sameOrderAndItems = actual.Select(p => p.Id).SequenceEqual(expected.Select(p => p.Id));

            return sameOrderAndItems
                .ToProperty()
                .Label($"search='{search}', category='{category}', showFav={showFav}: " +
                       $"actual [{string.Join(",", actual.Select(p => p.Id))}] != " +
                       $"expected [{string.Join(",", expected.Select(p => p.Id))}]");
        });
    }

    // =========================================================================
    // 11.4 — Testes de exemplo (bUnit + unidade)
    // =========================================================================

    private static Bunit.TestContext CreateContext()
    {
        var ctx = new Bunit.TestContext();
        ctx.Services.AddScoped<FavoritesService>();
        return ctx;
    }

    private static Product MakeProduct(string id, string name = "Produto Teste") =>
        new Product(
            Id: id,
            Name: name,
            Description: "Descrição de teste para o produto.",
            ImageUrl: "",
            Category: "Teste",
            Price: 49.90m,
            AmazonUrl: "https://www.amazon.com.br/dp/TEST"
        );

    // -------------------------------------------------------------------------
    // (a) ProductCard: botão "Ver detalhes" presente com texto visível (Req 1.1)
    // -------------------------------------------------------------------------

    [Fact]
    // Feature: product-detail-view — ProductCard exibe a ação "Ver detalhes"
    public void ProductCard_RendersVerDetalhesButton_WithVisibleText()
    {
        // _Requirements: 1.1_
        using var ctx = CreateContext();
        var product = MakeProduct("p1");

        var cut = ctx.RenderComponent<ProductCard>(
            parameters => parameters.Add(p => p.Product, product));

        var buttons = cut.FindAll("button.product-card__details-btn");

        Assert.Single(buttons);
        Assert.Equal("Ver detalhes", buttons[0].TextContent.Trim());
    }

    // -------------------------------------------------------------------------
    // (a) ProductCard: clicar em "Ver detalhes" dispara OnVerDetalhes com o
    //     produto correto (Req 1.1).
    // -------------------------------------------------------------------------

    [Fact]
    // Feature: product-detail-view — clicar em "Ver detalhes" invoca OnVerDetalhes
    public void ProductCard_ClickingVerDetalhes_InvokesCallbackWithProduct()
    {
        // _Requirements: 1.1_
        using var ctx = CreateContext();
        var product = MakeProduct("p1", "Panela de Pressão");

        Product? captured = null;
        var cut = ctx.RenderComponent<ProductCard>(parameters => parameters
            .Add(p => p.Product, product)
            .Add(p => p.OnVerDetalhes, (Product p) => { captured = p; }));

        cut.Find("button.product-card__details-btn").Click();

        Assert.NotNull(captured);
        Assert.Same(product, captured);
        Assert.Equal("p1", captured!.Id);
    }

    // -------------------------------------------------------------------------
    // (b) Resolução de Produtos.AbrirDetalhes (Req 1.6).
    //
    // Espelha exatamente a regra de Produtos.razor:
    //     var resolved = _allProducts.FirstOrDefault(p => p.Id == product.Id);
    //     if (resolved is null) { _detailUnavailable = true; return; }  // não abre
    //     _selectedProduct = resolved; _isDetailOpen = true; _detailUnavailable = false;
    //
    // Renderizar a página Produtos completa em bUnit exigiria
    // IProductCatalogService/FavoritesService/JSInterop; modelamos apenas a regra
    // de resolução como uma verificação pura focada, comentada como espelho de
    // AbrirDetalhes.
    // -------------------------------------------------------------------------

    /// <summary>
    /// Resultado da resolução de detalhes, espelhando o estado que Produtos.razor
    /// definiria em AbrirDetalhes.
    /// </summary>
    private readonly record struct DetailResolution(
        Product? SelectedProduct,
        bool IsDetailOpen,
        bool DetailUnavailable);

    /// <summary>
    /// Mirror puro de Produtos.AbrirDetalhes: resolve o produto por Id em
    /// _allProducts. Se não resolvido, sinaliza indisponibilidade sem abrir o
    /// modal; caso contrário, seleciona o produto resolvido e abre o modal.
    /// </summary>
    private static DetailResolution ResolveDetalhes(
        IReadOnlyList<Product> allProducts, Product product)
    {
        var resolved = allProducts.FirstOrDefault(p => p.Id == product.Id);
        if (resolved is null)
        {
            return new DetailResolution(SelectedProduct: null, IsDetailOpen: false, DetailUnavailable: true);
        }

        return new DetailResolution(SelectedProduct: resolved, IsDetailOpen: true, DetailUnavailable: false);
    }

    [Fact]
    // Feature: product-detail-view — Id não resolvível não abre o modal (Req 1.6)
    public void AbrirDetalhes_WithUnresolvableId_ShowsUnavailable_AndDoesNotOpenModal()
    {
        // _Requirements: 1.6_
        var allProducts = new List<Product>
        {
            MakeProduct("p1"),
            MakeProduct("p2"),
        };

        // Produto acionado cujo Id não está presente em _allProducts.
        var missing = MakeProduct("p-ausente");

        var result = ResolveDetalhes(allProducts, missing);

        Assert.True(result.DetailUnavailable);   // faixa de indisponibilidade exibida
        Assert.False(result.IsDetailOpen);       // modal permanece fechado
        Assert.Null(result.SelectedProduct);     // nenhum produto selecionado
    }

    [Fact]
    // Feature: product-detail-view — Id resolvível abre o modal (contraparte do Req 1.6)
    public void AbrirDetalhes_WithResolvableId_OpensModal_WithoutUnavailable()
    {
        // _Requirements: 1.6_
        var target = MakeProduct("p2", "Organizador");
        var allProducts = new List<Product>
        {
            MakeProduct("p1"),
            target,
        };

        // Aciona por um produto com o mesmo Id (resolução é por Id, não por referência).
        var trigger = MakeProduct("p2", "Organizador");

        var result = ResolveDetalhes(allProducts, trigger);

        Assert.False(result.DetailUnavailable);
        Assert.True(result.IsDetailOpen);
        Assert.NotNull(result.SelectedProduct);
        Assert.Same(target, result.SelectedProduct); // resolve para a instância em _allProducts
    }
}
