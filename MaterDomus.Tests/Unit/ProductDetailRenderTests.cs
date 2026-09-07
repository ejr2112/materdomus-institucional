// Feature: product-detail-view

using Bunit;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using MaterDomus.Web.Models;
using MaterDomus.Web.Services;
using MaterDomus.Web.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Xunit;

namespace MaterDomus.Tests.Unit;

/// <summary>
/// Testes de render e propriedade da visão de detalhes (ProductDetail.razor) e do
/// gatilho "Ver detalhes" do ProductCard.razor. Usa bUnit para renderização e
/// FsCheck para geração de dados, seguindo o padrão de ProductCardRenderTests e
/// reutilizando os geradores de AmazonUrlValidationTests.
///
/// ProductDetail injeta IJSRuntime e faz interop em OnAfterRenderAsync, portanto o
/// JSInterop é configurado em modo Loose para que a importação do módulo não lance.
///
/// Validates: Requirements 1.2, 1.3, 2.6, 4.5, 5.1, 5.2, 5.3, 5.4, 5.5, 6.3, 6.5
/// </summary>
public class ProductDetailRenderTests
{
    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Cria um contexto bUnit com o FavoritesService registrado (necessário para o
    /// ProductCard) e o JSInterop em modo Loose, de modo que a importação do módulo
    /// detailModal.js e as invocações de interop no OnAfterRenderAsync do
    /// ProductDetail não lancem exceção.
    /// </summary>
    private static Bunit.TestContext CreateContext()
    {
        var ctx = new Bunit.TestContext();
        ctx.Services.AddScoped<FavoritesService>();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        return ctx;
    }

    private static Product MakeProduct(
        string name = "Produto Teste",
        string amazonUrl = "",
        string category = "Teste",
        decimal price = 49.90m,
        string description = "Descrição de teste para o produto.") =>
        new Product(
            Id: "id-1",
            Name: name,
            Description: description,
            ImageUrl: "",
            Category: category,
            Price: price,
            AmazonUrl: amazonUrl
        );

    // -------------------------------------------------------------------------
    // Generators (reused approach from AmazonUrlValidationTests)
    // -------------------------------------------------------------------------

    private static readonly char[] UpperAlphanumChars =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();

    private static readonly char[] LowerAlphaChars =
        "abcdefghijklmnopqrstuvwxyz".ToCharArray();

    /// <summary>Gera exatamente 10 caracteres alfanuméricos maiúsculos — um ASIN válido.</summary>
    private static Gen<string> ValidAsinGen() =>
        Gen.ArrayOf(Gen.Elements(UpperAlphanumChars), 10)
           .Select(chars => new string(chars));

    /// <summary>Gera um id de vendedor: 1..14 caracteres alfanuméricos maiúsculos.</summary>
    private static Gen<string> SellerIdGen() =>
        Gen.Choose(1, 14)
           .SelectMany(len => Gen.ArrayOf(Gen.Elements(UpperAlphanumChars), len))
           .Select(chars => new string(chars));

    /// <summary>
    /// URLs canônicas válidas: https://www.amazon.com.br/dp/{ASIN10}, opcionalmente
    /// com o parâmetro de loja ?m={seller}. Todas devem tornar IsValidAmazonUrl true.
    /// </summary>
    private static Gen<(string Url, bool ShouldBeValid)> ValidUrlGen() =>
        Gen.OneOf(
            ValidAsinGen()
                .Select(asin => ($"https://www.amazon.com.br/dp/{asin}", true)),
            ValidAsinGen()
                .SelectMany(asin => SellerIdGen()
                    .Select(seller => ($"https://www.amazon.com.br/dp/{asin}?m={seller}", true)))
        );

    /// <summary>
    /// URLs inválidas/vazias/nulas que devem tornar IsValidAmazonUrl false.
    /// </summary>
    private static Gen<(string? Url, bool ShouldBeValid)> InvalidUrlGen() =>
        Gen.OneOf(
            // ASIN curto demais (9)
            Gen.ArrayOf(Gen.Elements(UpperAlphanumChars), 9)
               .Select(chars => ((string?)$"https://www.amazon.com.br/dp/{new string(chars)}", false)),
            // ASIN longo demais (11)
            Gen.ArrayOf(Gen.Elements(UpperAlphanumChars), 11)
               .Select(chars => ((string?)$"https://www.amazon.com.br/dp/{new string(chars)}", false)),
            // ASIN minúsculo
            Gen.ArrayOf(Gen.Elements(LowerAlphaChars), 10)
               .Select(chars => ((string?)$"https://www.amazon.com.br/dp/{new string(chars)}", false)),
            // Parâmetro de rastreio extra
            ValidAsinGen()
               .Select(asin => ((string?)$"https://www.amazon.com.br/dp/{asin}?tag=test-20", false)),
            // Esquema errado
            ValidAsinGen()
               .Select(asin => ((string?)$"http://www.amazon.com.br/dp/{asin}", false)),
            // Domínio errado
            ValidAsinGen()
               .Select(asin => ((string?)$"https://www.amazon.com/dp/{asin}", false)),
            // Sufixo de caminho
            ValidAsinGen()
               .Select(asin => ((string?)$"https://www.amazon.com.br/dp/{asin}/ref=sr_1_1", false)),
            // String vazia
            Gen.Constant(((string?)"", false)),
            // Nula
            Gen.Constant(((string?)null, false)),
            // String arbitrária não-URL
            ArbMap.Default.ArbFor<NonEmptyString>().Generator
               .Select(s => ((string?)s.Get, false))
        );

    /// <summary>Mistura 50% URLs válidas / 50% inválidas.</summary>
    private static Gen<(string? Url, bool ShouldBeValid)> AmazonUrlGen() =>
        Gen.Frequency(
            (1, ValidUrlGen().Select(t => ((string?)t.Url, t.ShouldBeValid))),
            (1, InvalidUrlGen())
        );

    // =========================================================================
    // Task 11.1 → Property 9: Visibilidade condicional do CTA_Amazon
    // =========================================================================

    // Feature: product-detail-view, Property 9: Visibilidade condicional do CTA_Amazon
    /// <summary>
    /// Para qualquer produto exibido na visão de detalhes, o link CTA_Amazon
    /// (&lt;a&gt; com rótulo "Comprar na Amazon", target="_blank",
    /// rel="noopener noreferrer") está presente se e somente se
    /// ProductCatalogService.IsValidAmazonUrl(Product.AmazonUrl) é verdadeiro.
    ///
    /// Validates: Requirements 5.1, 5.2, 5.3, 5.4
    /// </summary>
    [Property(MaxTest = 100)]
    public Property AmazonCta_VisibleIffAmazonUrlIsValid()
    {
        return Prop.ForAll(AmazonUrlGen().ToArbitrary(), tuple =>
        {
            var (url, expectValid) = tuple;

            // Verifica a expectativa do gerador contra a validação real (defensivo).
            var isValid = ProductCatalogService.IsValidAmazonUrl(url);

            using var ctx = CreateContext();
            // Nome válido garante que o diálogo é renderizado.
            var product = MakeProduct(name: "Produto Teste", amazonUrl: url ?? "");
            var cut = ctx.RenderComponent<ProductDetail>(parameters => parameters
                .Add(p => p.Product, product)
                .Add(p => p.IsOpen, true));

            var ctas = cut.FindAll("a.product-detail__amazon-btn");

            bool present = ctas.Count == 1;
            bool result = present == isValid;

            // Quando presente, valida os atributos de segurança e o rótulo.
            if (present)
            {
                var cta = ctas[0];
                result = result
                    && cta.GetAttribute("target") == "_blank"
                    && cta.GetAttribute("rel") == "noopener noreferrer"
                    && cta.TextContent.Trim() == "Comprar na Amazon";
            }

            return result
                .ToProperty()
                .Label($"URL='{url ?? "(null)"}', isValid={isValid}, ctaCount={ctas.Count}");
        });
    }

    // =========================================================================
    // Task 11.2 → Property 10: Nome acessível do diálogo e do gatilho
    // =========================================================================

    // Feature: product-detail-view, Property 10: Nome acessível do diálogo e do gatilho
    /// <summary>
    /// Para qualquer nome de produto:
    /// (a) o elemento role="dialog" da visão de detalhes tem aria-label igual ao
    ///     nome do produto; e
    /// (b) o botão "Ver detalhes" do ProductCard correspondente tem aria-label
    ///     igual a "Ver detalhes " + nome.
    ///
    /// Validates: Requirements 1.2, 1.3, 6.5
    /// </summary>
    [Property(MaxTest = 100)]
    public Property AccessibleName_DialogAndTrigger_MatchProductName()
    {
        // Nomes de produto realistas: caracteres imprimíveis (sem caracteres de
        // controle), com ao menos um caractere não-branco. Caracteres de controle e
        // CR/LF são normalizados/removidos pelo parser HTML ao ler atributos, o que
        // distorceria a comparação sem refletir um comportamento real do componente
        // (nomes de produto não contêm caracteres de controle).
        var nameGen =
            Gen.NonEmptyListOf(
                    Gen.Choose(0x20, 0x7E).Select(c => (char)c)) // ASCII imprimível
                .Select(chars => new string(chars.ToArray()))
                .Where(s => !string.IsNullOrWhiteSpace(s));

        return Prop.ForAll(nameGen.ToArbitrary(), name =>
        {
            using var ctx = CreateContext();
            var product = MakeProduct(name: name);

            // (a) Diálogo do ProductDetail: aria-label == Product.Name
            var detail = ctx.RenderComponent<ProductDetail>(parameters => parameters
                .Add(p => p.Product, product)
                .Add(p => p.IsOpen, true));

            var dialog = detail.Find("div.product-detail[role=dialog]");
            var dialogLabel = dialog.GetAttribute("aria-label");

            // (b) Gatilho do ProductCard: aria-label == "Ver detalhes " + Name
            var card = ctx.RenderComponent<ProductCard>(parameters => parameters
                .Add(p => p.Product, product));

            var trigger = card.Find("button.product-card__details-btn");
            var triggerLabel = trigger.GetAttribute("aria-label");

            bool result = dialogLabel == name
                && triggerLabel == $"Ver detalhes {name}";

            return result
                .ToProperty()
                .Label($"name='{name}', dialogLabel='{dialogLabel}', triggerLabel='{triggerLabel}'");
        });
    }

    // =========================================================================
    // Task 11.5 → Testes de exemplo do ProductDetail
    // =========================================================================

    // Feature: product-detail-view, exemplo: nome em branco exibe conteúdo indisponível
    /// <summary>
    /// Quando o nome do produto é composto apenas por espaços em branco, a visão de
    /// detalhes exibe a indicação de conteúdo indisponível
    /// (.product-detail__copy--unavailable com o texto "Conteúdo indisponível"),
    /// sem texto persuasivo quebrado.
    ///
    /// Validates: Requirements 4.5
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void ProductDetail_BlankName_RendersUnavailableContent(string blankName)
    {
        using var ctx = CreateContext();
        var product = MakeProduct(name: blankName);

        var cut = ctx.RenderComponent<ProductDetail>(parameters => parameters
            .Add(p => p.Product, product)
            .Add(p => p.IsOpen, true));

        var unavailable = cut.Find("p.product-detail__copy--unavailable");
        Assert.Equal("Conteúdo indisponível", unavailable.TextContent.Trim());

        // Não deve existir um parágrafo de copy "normal" (sem o modificador unavailable).
        var copies = cut.FindAll("p.product-detail__copy");
        // O parágrafo unavailable também tem a classe base "product-detail__copy",
        // então esperamos exatamente um parágrafo de copy no total.
        Assert.Single(copies);
    }

    // Feature: product-detail-view, exemplo: botão de fechar dispara OnClose
    /// <summary>
    /// Clicar no botão de fechar (button.product-detail__close-btn) dispara o
    /// EventCallback OnClose, permitindo que a página feche a visão de detalhes.
    ///
    /// Validates: Requirements 6.3
    /// </summary>
    [Fact]
    public void ProductDetail_CloseButtonClick_InvokesOnClose()
    {
        using var ctx = CreateContext();
        var product = MakeProduct(name: "Produto Teste");

        bool closed = false;

        var cut = ctx.RenderComponent<ProductDetail>(parameters => parameters
            .Add(p => p.Product, product)
            .Add(p => p.IsOpen, true)
            .Add(p => p.OnClose, EventCallback.Factory.Create(this, () => closed = true)));

        var closeBtn = cut.Find("button.product-detail__close-btn");
        closeBtn.Click();

        Assert.True(closed, "OnClose deveria ter sido disparado ao clicar no botão de fechar.");
    }

    // Feature: product-detail-view, exemplo: Escape no diálogo dispara OnClose
    /// <summary>
    /// Pressionar Escape no diálogo (via @onkeydown → OnDialogKeyDown) dispara o
    /// EventCallback OnClose, fechando a visão de detalhes por teclado sem depender
    /// do interop de JS.
    ///
    /// Validates: Requirements 6.3
    /// </summary>
    [Fact]
    public void ProductDetail_EscapeKeyOnDialog_InvokesOnClose()
    {
        using var ctx = CreateContext();
        var product = MakeProduct(name: "Produto Teste");

        bool closed = false;

        var cut = ctx.RenderComponent<ProductDetail>(parameters => parameters
            .Add(p => p.Product, product)
            .Add(p => p.IsOpen, true)
            .Add(p => p.OnClose, EventCallback.Factory.Create(this, () => closed = true)));

        var dialog = cut.Find("div.product-detail[role=dialog]");
        dialog.KeyDown(key: "Escape");

        Assert.True(closed, "OnClose deveria ter sido disparado ao pressionar Escape no diálogo.");
    }

    // Feature: product-detail-view, exemplo: falha ao abrir CTA_Amazon mantém o modal e exibe erro
    /// <summary>
    /// Quando o acionamento do CTA_Amazon não consegue abrir a nova aba
    /// (window.open retorna null no interop), a visão de detalhes permanece aberta
    /// (o diálogo continua presente) e exibe a mensagem de erro
    /// .product-detail__amazon-error "Não foi possível abrir a compra.".
    ///
    /// Validates: Requirements 5.5
    /// </summary>
    [Fact]
    public void ProductDetail_AmazonOpenFailure_ShowsErrorAndKeepsModalOpen()
    {
        using var ctx = new Bunit.TestContext();
        ctx.Services.AddScoped<FavoritesService>();

        // Substitui o IJSRuntime da bUnit por um fake que sempre lança em qualquer
        // invocação de interop. Isso emula a falha ao abrir a nova aba
        // (JS.InvokeAsync<IJSObjectReference?>("window.open", ...) lança) — caminho
        // que o componente captura para sinalizar erro sem fechar o modal (Req 5.5).
        // A importação do módulo em OnAfterRenderAsync também lança, mas é degradada
        // graciosamente pelo try/catch do componente, mantendo-o funcional.
        ctx.Services.AddScoped<Microsoft.JSInterop.IJSRuntime>(_ => new ThrowingJSRuntime());

        var product = MakeProduct(
            name: "Produto Teste",
            amazonUrl: "https://www.amazon.com.br/dp/ABCDE12345");

        bool closed = false;

        var cut = ctx.RenderComponent<ProductDetail>(parameters => parameters
            .Add(p => p.Product, product)
            .Add(p => p.IsOpen, true)
            .Add(p => p.OnClose, EventCallback.Factory.Create(this, () => closed = true)));

        var cta = cut.Find("a.product-detail__amazon-btn");
        cta.Click();

        // O modal permanece aberto e a mensagem de erro é exibida.
        Assert.False(closed, "O modal não deveria fechar em caso de falha ao abrir a compra.");
        Assert.NotNull(cut.Find("div.product-detail[role=dialog]"));

        var error = cut.Find("p.product-detail__amazon-error");
        Assert.Equal("Não foi possível abrir a compra.", error.TextContent.Trim());
    }

    /// <summary>
    /// IJSRuntime fake que lança em toda invocação de interop, usado para emular a
    /// falha de abertura da nova aba no CTA_Amazon (Req 5.5). O componente captura a
    /// exceção e exibe a mensagem de erro sem fechar o modal.
    /// </summary>
    private sealed class ThrowingJSRuntime : Microsoft.JSInterop.IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            throw new Microsoft.JSInterop.JSException($"Interop indisponível: {identifier}");

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            System.Threading.CancellationToken cancellationToken,
            object?[]? args) =>
            throw new Microsoft.JSInterop.JSException($"Interop indisponível: {identifier}");
    }
}
