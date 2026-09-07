// Feature: remove-product-video (bugfix)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Bunit;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using MaterDomus.Web.Helpers;
using MaterDomus.Web.Models;
using MaterDomus.Web.Pages;
using MaterDomus.Web.Services;
using MaterDomus.Web.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Xunit;

namespace MaterDomus.Tests.Unit;

/// <summary>
/// Testes de PRESERVAÇÃO da spec bugfix "remove-product-video".
///
/// Property 2: Preservation — Comportamento do produto inalterado.
///
/// Metodologia observation-first: observamos o comportamento no código NÃO alterado
/// (estado atual, pós-revert a646466), registramos esse comportamento e o travamos
/// em testes. Estes testes devem PASSAR contra o código atual (linha de base) e
/// continuar passando após a remoção condicional de vestígios de vídeo (Task 3),
/// demonstrando que a exibição de imagem/nome/descrição truncada/preço/categoria, o
/// botão "Comprar na Amazon" com atributos de segurança, favoritos, "Ver detalhes"
/// (modal) e busca/filtros/estados permanecem inalterados.
///
/// Complementam (sem duplicar) a cobertura existente:
/// - ProductCardRenderTests (visibilidade do botão Amazon, ARIA de favorito);
/// - PriceFormattingTests / DescriptionTruncationTests (helpers puros);
/// - ProductFilterTests (busca/categoria/composição);
/// - ProductDetailRenderTests / ProductDetailIntegrationTests (modal, CTA, invariância de filtro);
/// - ProductCatalogServiceTests (carregamento/erro/vazio/validação).
///
/// Validates: Requirements 3.1, 3.2, 3.3, 3.4, 3.5, 3.6
/// </summary>
public class ProductPreservationTests
{
    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static readonly JsonSerializerOptions WebJsonOptions =
        new(JsonSerializerDefaults.Web); // camelCase + case-insensitive, igual a GetFromJsonAsync

    /// <summary>
    /// Contexto bUnit com FavoritesService registrado e JSInterop em modo Loose
    /// (o ProductDetail importa um módulo JS em OnAfterRenderAsync; o Loose evita
    /// que a importação lance, mantendo o modal funcional via C#).
    /// </summary>
    private static Bunit.TestContext CreateContext()
    {
        var ctx = new Bunit.TestContext();
        ctx.Services.AddScoped<FavoritesService>();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        return ctx;
    }

    private static Product MakeProduct(
        string id = "prod-1",
        string name = "Produto Teste",
        string description = "Descrição de teste para o produto.",
        string imageUrl = "images/products/prod-1.jpg",
        string category = "Cozinha",
        decimal price = 49.90m,
        string amazonUrl = "https://www.amazon.com.br/dp/AAAAAAAAAA") =>
        new Product(id, name, description, imageUrl, category, price, amazonUrl);

    // =========================================================================
    // UNIT TESTS — renderização do cartão (bUnit/xUnit)
    // Req 3.1, 3.2
    // =========================================================================

    /// <summary>
    /// Renderização completa do cartão: imagem (com nome no alt e fallback
    /// placeholder-product.png via onerror), categoria, nome, descrição truncada,
    /// preço formatado pt-BR e as ações (Ver detalhes, favoritar). Trava a presença
    /// simultânea de todos os elementos preservados. (Req 3.1)
    /// </summary>
    [Fact]
    public void ProductCard_RendersAllPreservedElements()
    {
        using var ctx = CreateContext();
        var product = MakeProduct(
            name: "Dispenser Quadrado Flow 1L",
            description: "Dispenser quadrado com dosador pump.",
            imageUrl: "images/products/dispenser.jpg",
            category: "Lavanderia",
            price: 39.90m,
            amazonUrl: "https://www.amazon.com.br/dp/B0GKPPS5YH?m=A20TN3HCSY6KZV");

        var cut = ctx.RenderComponent<ProductCard>(
            parameters => parameters.Add(p => p.Product, product));

        // Imagem: src, alt e fallback via onerror.
        var img = cut.Find("img.product-card__image");
        Assert.Equal("images/products/dispenser.jpg", img.GetAttribute("src"));
        Assert.Equal("Dispenser Quadrado Flow 1L", img.GetAttribute("alt"));
        Assert.Contains("images/placeholder-product.png", img.GetAttribute("onerror"));

        // Categoria, nome, descrição truncada e preço formatado.
        Assert.Equal("Lavanderia", cut.Find("span.product-card__category").TextContent.Trim());
        Assert.Equal("Dispenser Quadrado Flow 1L", cut.Find("h3.product-card__name").TextContent.Trim());
        Assert.Equal(
            ProductHelpers.TruncateDescription(product.Description),
            cut.Find("p.product-card__description").TextContent.Trim());
        Assert.Equal(
            ProductHelpers.FormatPrice(product.Price),
            cut.Find("p.product-card__price").TextContent.Trim());

        // Ações: "Ver detalhes", botão de favoritar e "Comprar na Amazon".
        Assert.Single(cut.FindAll("button.product-card__details-btn"));
        Assert.Single(cut.FindAll("button.product-card__favorite-btn"));
        Assert.Single(cut.FindAll("a.product-card__amazon-btn"));
    }

    /// <summary>
    /// Descrição com mais de 120 caracteres é exibida truncada no cartão (primeiros
    /// 120 chars + "..."), espelhando ProductHelpers.TruncateDescription. (Req 3.1)
    /// </summary>
    [Fact]
    public void ProductCard_LongDescription_IsTruncatedTo120CharsPlusEllipsis()
    {
        using var ctx = CreateContext();
        var longDescription = new string('a', 150);
        var product = MakeProduct(description: longDescription);

        var cut = ctx.RenderComponent<ProductCard>(
            parameters => parameters.Add(p => p.Product, product));

        var expected = new string('a', 120) + "...";
        Assert.Equal(expected, cut.Find("p.product-card__description").TextContent.Trim());
    }

    /// <summary>
    /// O botão "Comprar na Amazon" é renderizado com href correto e os atributos de
    /// segurança target="_blank" e rel="noopener noreferrer". (Req 3.2)
    /// </summary>
    [Fact]
    public void ProductCard_AmazonButton_HasSecurityAttributes()
    {
        using var ctx = CreateContext();
        var url = "https://www.amazon.com.br/dp/AAAAAAAAAA";
        var product = MakeProduct(amazonUrl: url);

        var cut = ctx.RenderComponent<ProductCard>(
            parameters => parameters.Add(p => p.Product, product));

        var btn = cut.Find("a.product-card__amazon-btn");
        Assert.Equal(url, btn.GetAttribute("href"));
        Assert.Equal("_blank", btn.GetAttribute("target"));
        Assert.Equal("noopener noreferrer", btn.GetAttribute("rel"));
        Assert.Equal("Comprar na Amazon", btn.TextContent.Trim());
    }

    /// <summary>
    /// Com AmazonUrl vazia o botão "Comprar na Amazon" NÃO é renderizado. (Req 3.2)
    /// </summary>
    [Fact]
    public void ProductCard_EmptyAmazonUrl_HidesAmazonButton()
    {
        using var ctx = CreateContext();
        var product = MakeProduct(amazonUrl: "");

        var cut = ctx.RenderComponent<ProductCard>(
            parameters => parameters.Add(p => p.Product, product));

        Assert.Empty(cut.FindAll("a.product-card__amazon-btn"));
    }

    /// <summary>
    /// Favoritar via clique alterna o estado no FavoritesService, reflete
    /// aria-pressed/aria-label no botão e atualiza a contagem de favoritos. (Req 3.3)
    /// </summary>
    [Fact]
    public void ProductCard_ToggleFavorite_UpdatesStateAriaAndCounter()
    {
        using var ctx = CreateContext();
        var favService = ctx.Services.GetRequiredService<FavoritesService>();
        var product = MakeProduct(id: "fav-1");

        var cut = ctx.RenderComponent<ProductCard>(
            parameters => parameters.Add(p => p.Product, product));

        var btn = cut.Find("button.product-card__favorite-btn");

        // Estado inicial: não favoritado.
        Assert.Equal("false", btn.GetAttribute("aria-pressed"));
        Assert.Equal("Favoritar produto", btn.GetAttribute("aria-label"));
        Assert.Equal(0, favService.Count);
        Assert.False(favService.IsFavorite("fav-1"));

        // Primeiro clique: favorita.
        btn.Click();
        btn = cut.Find("button.product-card__favorite-btn");
        Assert.Equal("true", btn.GetAttribute("aria-pressed"));
        Assert.Equal("Remover dos favoritos", btn.GetAttribute("aria-label"));
        Assert.Equal(1, favService.Count);
        Assert.True(favService.IsFavorite("fav-1"));

        // Segundo clique: desfavorita.
        btn.Click();
        btn = cut.Find("button.product-card__favorite-btn");
        Assert.Equal("false", btn.GetAttribute("aria-pressed"));
        Assert.Equal("Favoritar produto", btn.GetAttribute("aria-label"));
        Assert.Equal(0, favService.Count);
        Assert.False(favService.IsFavorite("fav-1"));
    }

    /// <summary>
    /// "Ver detalhes" aciona o EventCallback OnVerDetalhes com o produto correto,
    /// que a página usa para abrir o modal de detalhes. (Req 3.4)
    /// </summary>
    [Fact]
    public void ProductCard_VerDetalhesClick_InvokesCallbackWithProduct()
    {
        using var ctx = CreateContext();
        var product = MakeProduct(id: "det-1", name: "Organizador");

        Product? captured = null;
        var cut = ctx.RenderComponent<ProductCard>(parameters => parameters
            .Add(p => p.Product, product)
            .Add(p => p.OnVerDetalhes, (Product p) => captured = p));

        cut.Find("button.product-card__details-btn").Click();

        Assert.NotNull(captured);
        Assert.Same(product, captured);
    }

    // =========================================================================
    // PROPERTY-BASED TESTS (FsCheck)
    // Req 3.1, 3.2
    // =========================================================================

    /// <summary>
    /// Gerador de produtos com campos textuais realistas (imprimíveis, sem controle),
    /// preço positivo e AmazonUrl válida/vazia.
    /// </summary>
    private static Gen<Product> ProductGen()
    {
        // Texto imprimível sem caracteres de controle nem CR/LF (que o parser HTML
        // normalizaria ao ler atributos/conteúdo), com pelo menos um char não-branco.
        Gen<string> textGen =
            Gen.NonEmptyListOf(Gen.Choose(0x20, 0x7E).Select(c => (char)c))
               .Select(chars => new string(chars.ToArray()))
               .Where(s => !string.IsNullOrWhiteSpace(s));

        Gen<decimal> priceGen = Gen.Choose(1, 500000).Select(cents => cents / 100m);
        Gen<bool> hasAmazon = ArbMap.Default.ArbFor<bool>().Generator;

        return textGen.SelectMany(id =>
               textGen.SelectMany(name =>
               textGen.SelectMany(desc =>
               textGen.SelectMany(cat =>
               textGen.SelectMany(img =>
               priceGen.SelectMany(price =>
               hasAmazon.Select(has => new Product(
                   Id: id,
                   Name: name,
                   Description: desc,
                   ImageUrl: img,
                   Category: cat,
                   Price: price,
                   AmazonUrl: has ? "https://www.amazon.com.br/dp/AAAAAAAAAA" : ""
               ))))))));
    }

    // Feature: remove-product-video, Property 2: Preservation — renderização do cartão
    /// <summary>
    /// Para QUALQUER produto gerado, a renderização do ProductCard preserva:
    /// imagem (src == ImageUrl, alt == Name), categoria, nome, descrição truncada
    /// (== ProductHelpers.TruncateDescription) e preço formatado (== FormatPrice).
    ///
    /// Validates: Requirements 3.1
    /// </summary>
    [Property(MaxTest = 100)]
    public Property Preservation_CardRendering_PreservesCoreFields()
    {
        return Prop.ForAll(ProductGen().ToArbitrary(), product =>
        {
            using var ctx = CreateContext();
            var cut = ctx.RenderComponent<ProductCard>(
                parameters => parameters.Add(p => p.Product, product));

            var img = cut.Find("img.product-card__image");
            var srcOk = img.GetAttribute("src") == product.ImageUrl;
            var altOk = img.GetAttribute("alt") == product.Name;

            var categoryOk = cut.Find("span.product-card__category").TextContent == product.Category;
            var nameOk = cut.Find("h3.product-card__name").TextContent == product.Name;

            var expectedDesc = ProductHelpers.TruncateDescription(product.Description) ?? string.Empty;
            var descOk = cut.Find("p.product-card__description").TextContent == expectedDesc;

            var priceOk = cut.Find("p.product-card__price").TextContent == ProductHelpers.FormatPrice(product.Price);

            return (srcOk && altOk && categoryOk && nameOk && descOk && priceOk)
                .ToProperty()
                .Label($"src={srcOk}, alt={altOk}, cat={categoryOk}, name={nameOk}, desc={descOk}, price={priceOk}");
        });
    }

    // Feature: remove-product-video, Property 2: Preservation — botão Amazon e atributos de segurança
    /// <summary>
    /// Para QUALQUER produto gerado, o botão "Comprar na Amazon" aparece se e somente
    /// se AmazonUrl é não-vazia; quando presente, mantém href == AmazonUrl,
    /// target="_blank" e rel="noopener noreferrer".
    ///
    /// Validates: Requirements 3.2
    /// </summary>
    [Property(MaxTest = 100)]
    public Property Preservation_AmazonButton_SecurityAttributesPreserved()
    {
        return Prop.ForAll(ProductGen().ToArbitrary(), product =>
        {
            using var ctx = CreateContext();
            var cut = ctx.RenderComponent<ProductCard>(
                parameters => parameters.Add(p => p.Product, product));

            var buttons = cut.FindAll("a.product-card__amazon-btn");
            bool shouldShow = !string.IsNullOrEmpty(product.AmazonUrl);

            bool result = buttons.Count == (shouldShow ? 1 : 0);
            if (shouldShow && buttons.Count == 1)
            {
                var btn = buttons[0];
                result = result
                    && btn.GetAttribute("href") == product.AmazonUrl
                    && btn.GetAttribute("target") == "_blank"
                    && btn.GetAttribute("rel") == "noopener noreferrer";
            }

            return result
                .ToProperty()
                .Label($"amazonUrl='{product.AmazonUrl}', shouldShow={shouldShow}, count={buttons.Count}");
        });
    }

    // Feature: remove-product-video, Property 2: Preservation — idempotência do modelo na deserialização
    /// <summary>
    /// Para QUALQUER payload JSON de produto, adicionar um campo extra "videoUrl"
    /// arbitrário NÃO altera o Product resultante: a deserialização é idêntica à do
    /// mesmo payload sem o campo (o modelo ignora campos desconhecidos, confirmando
    /// que nenhum vestígio de vídeo reentra no modelo).
    ///
    /// Validates: Requirements 3.1
    /// </summary>
    [Property(MaxTest = 100)]
    public Property Preservation_Deserialization_IgnoresExtraVideoUrlField()
    {
        // Campos seguros para embutir em JSON (sem aspas/barras/controle).
        Gen<string> safeText =
            Gen.NonEmptyListOf(Gen.Elements(
                "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 -".ToCharArray()))
               .Select(chars => new string(chars.ToArray()).Trim())
               .Where(s => s.Length > 0);

        Gen<decimal> priceGen = Gen.Choose(1, 500000).Select(cents => cents / 100m);

        var gen = safeText.SelectMany(id =>
                  safeText.SelectMany(name =>
                  safeText.SelectMany(desc =>
                  safeText.SelectMany(cat =>
                  safeText.SelectMany(videoUrl =>
                  priceGen.Select(price => (id, name, desc, cat, videoUrl, price)))))));

        return Prop.ForAll(gen.ToArbitrary(), t =>
        {
            var priceLiteral = t.price.ToString(System.Globalization.CultureInfo.InvariantCulture);

            string withoutVideo = $$"""
                {
                  "id": "{{t.id}}",
                  "name": "{{t.name}}",
                  "description": "{{t.desc}}",
                  "imageUrl": "images/x.jpg",
                  "category": "{{t.cat}}",
                  "price": {{priceLiteral}},
                  "amazonUrl": "https://www.amazon.com.br/dp/AAAAAAAAAA"
                }
                """;

            string withVideo = $$"""
                {
                  "id": "{{t.id}}",
                  "name": "{{t.name}}",
                  "description": "{{t.desc}}",
                  "imageUrl": "images/x.jpg",
                  "category": "{{t.cat}}",
                  "price": {{priceLiteral}},
                  "amazonUrl": "https://www.amazon.com.br/dp/AAAAAAAAAA",
                  "videoUrl": "{{t.videoUrl}}"
                }
                """;

            var a = JsonSerializer.Deserialize<Product>(withoutVideo, WebJsonOptions);
            var b = JsonSerializer.Deserialize<Product>(withVideo, WebJsonOptions);

            return (a is not null && b is not null && a == b)
                .ToProperty()
                .Label($"withoutVideo={a}, withVideo={b}");
        });
    }

    // =========================================================================
    // INTEGRATION TESTS — fluxo da página Produtos (bUnit)
    // Req 3.4, 3.5, 3.6
    // =========================================================================

    /// <summary>
    /// Fake de IProductCatalogService com resultado configurável e opção de atraso
    /// para observar o estado de carregamento (skeleton).
    /// </summary>
    private sealed class FakeCatalogService : IProductCatalogService
    {
        private readonly IReadOnlyList<Product> _products;
        private readonly TaskCompletionSource<bool>? _gate;

        public FakeCatalogService(IReadOnlyList<Product> products, TaskCompletionSource<bool>? gate = null)
        {
            _products = products;
            _gate = gate;
        }

        public async Task<IReadOnlyList<Product>> GetProductsAsync()
        {
            if (_gate is not null)
                await _gate.Task;
            return _products;
        }
    }

    private static Bunit.TestContext CreatePageContext(IProductCatalogService catalog)
    {
        var ctx = new Bunit.TestContext();
        ctx.Services.AddSingleton(catalog);
        ctx.Services.AddScoped<FavoritesService>();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        return ctx;
    }

    /// <summary>
    /// Fluxo completo: com catálogo carregado, a página Produtos renderiza a grade
    /// com um ProductCard por produto e sem estados de erro/vazio. (Req 3.5)
    /// </summary>
    [Fact]
    public void ProdutosPage_WithProducts_RendersGrid()
    {
        var products = new List<Product>
        {
            MakeProduct(id: "p1", name: "Produto Um", category: "Cozinha"),
            MakeProduct(id: "p2", name: "Produto Dois", category: "Limpeza"),
        };

        using var ctx = CreatePageContext(new FakeCatalogService(products));
        var cut = ctx.RenderComponent<Produtos>();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(cut.Find("div.products-grid"));
            Assert.Equal(2, cut.FindAll("article.product-card").Count);
        });

        Assert.Empty(cut.FindAll("div.produtos-error"));
        Assert.Empty(cut.FindAll("div.produtos-empty"));
    }

    /// <summary>
    /// Estado de carregamento: enquanto GetProductsAsync não conclui, a página exibe
    /// o skeleton (role="status"). Ao liberar o gate, transiciona para a grade. (Req 3.6)
    /// </summary>
    [Fact]
    public void ProdutosPage_WhileLoading_ShowsSkeleton_ThenGrid()
    {
        var gate = new TaskCompletionSource<bool>();
        var products = new List<Product> { MakeProduct(id: "p1") };

        using var ctx = CreatePageContext(new FakeCatalogService(products, gate));
        var cut = ctx.RenderComponent<Produtos>();

        // Ainda carregando: skeleton presente, grade ausente.
        Assert.NotNull(cut.Find("div.skeleton[role=status]"));
        Assert.Empty(cut.FindAll("div.products-grid"));

        // Libera o carregamento e aguarda a grade.
        gate.SetResult(true);
        cut.WaitForAssertion(() => Assert.NotNull(cut.Find("div.products-grid")));
    }

    /// <summary>
    /// Estado vazio: catálogo sem produtos exibe o estado "Nenhum produto disponível"
    /// e não renderiza a grade. (Req 3.6)
    /// </summary>
    [Fact]
    public void ProdutosPage_EmptyCatalog_ShowsEmptyState()
    {
        using var ctx = CreatePageContext(new FakeCatalogService(Array.Empty<Product>()));
        var cut = ctx.RenderComponent<Produtos>();

        cut.WaitForAssertion(() =>
        {
            var empty = cut.Find("div.produtos-empty");
            Assert.Contains("Nenhum produto disponível", empty.TextContent);
        });

        Assert.Empty(cut.FindAll("div.products-grid"));
    }

    /// <summary>
    /// Estado de erro: quando o carregamento lança, a página exibe o estado de erro
    /// com o botão "Tentar novamente". (Req 3.6)
    /// </summary>
    [Fact]
    public void ProdutosPage_LoadFailure_ShowsErrorWithRetry()
    {
        using var ctx = CreatePageContext(new ThrowingCatalogService());
        var cut = ctx.RenderComponent<Produtos>();

        cut.WaitForAssertion(() =>
        {
            var error = cut.Find("div.produtos-error");
            var retry = cut.Find("button.btn-retry");
            Assert.Equal("Tentar novamente", retry.TextContent.Trim());
            Assert.Contains("Não foi possível carregar os produtos", error.TextContent);
        });

        Assert.Empty(cut.FindAll("div.products-grid"));
    }

    private sealed class ThrowingCatalogService : IProductCatalogService
    {
        public Task<IReadOnlyList<Product>> GetProductsAsync() =>
            throw new InvalidOperationException("Falha simulada ao carregar o catálogo.");
    }

    /// <summary>
    /// Abertura/fechamento do modal de detalhes: clicar em "Ver detalhes" abre o
    /// diálogo (role="dialog") com o nome do produto; clicar em fechar retorna à
    /// grade sem o diálogo. (Req 3.4)
    /// </summary>
    [Fact]
    public void ProdutosPage_VerDetalhes_OpensAndClosesDetailModal()
    {
        var products = new List<Product>
        {
            MakeProduct(id: "p1", name: "Produto Detalhe", category: "Cozinha"),
        };

        using var ctx = CreatePageContext(new FakeCatalogService(products));
        var cut = ctx.RenderComponent<Produtos>();

        cut.WaitForAssertion(() => Assert.NotNull(cut.Find("button.product-card__details-btn")));

        // Modal inicialmente fechado.
        Assert.Empty(cut.FindAll("div.product-detail[role=dialog]"));

        // Abre o modal.
        cut.Find("button.product-card__details-btn").Click();
        var dialog = cut.Find("div.product-detail[role=dialog]");
        Assert.Equal("Produto Detalhe", dialog.GetAttribute("aria-label"));

        // Fecha o modal.
        cut.Find("button.product-detail__close-btn").Click();
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("div.product-detail[role=dialog]")));
    }

    /// <summary>
    /// Filtro de favoritos na página: favoritar um produto e ativar "Favoritos"
    /// restringe a grade aos favoritos e atualiza o contador. (Req 3.3, 3.5)
    /// </summary>
    [Fact]
    public void ProdutosPage_FavoritesFilter_RestrictsGrid_AndUpdatesCounter()
    {
        var products = new List<Product>
        {
            MakeProduct(id: "p1", name: "Produto Um", category: "Cozinha"),
            MakeProduct(id: "p2", name: "Produto Dois", category: "Limpeza"),
        };

        using var ctx = CreatePageContext(new FakeCatalogService(products));
        var cut = ctx.RenderComponent<Produtos>();

        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll("article.product-card").Count));

        // Favorita o primeiro produto (primeiro botão de favorito da grade).
        cut.FindAll("button.product-card__favorite-btn")[0].Click();

        // O contador de favoritos aparece com 1.
        cut.WaitForAssertion(() =>
            Assert.Equal("1", cut.Find("span.favorites-badge__count").TextContent.Trim()));

        // Ativa o filtro "Favoritos": apenas 1 cartão permanece na grade.
        cut.Find("button.filter-bar__favorites-btn").Click();
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("article.product-card")));
    }
}
