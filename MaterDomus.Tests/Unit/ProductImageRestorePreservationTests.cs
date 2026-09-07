// Feature: product-image-restore (bugfix)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bunit;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using MaterDomus.Web.Helpers;
using MaterDomus.Web.Models;
using MaterDomus.Web.Services;
using MaterDomus.Web.Shared;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MaterDomus.Tests.Unit;

/// <summary>
/// Testes de PRESERVAÇÃO da spec bugfix "product-image-restore".
///
/// Property 2: Preservation — Entradas SEM a condição do bug permanecem idênticas.
///
/// Metodologia observation-first: observamos o comportamento no código NÃO corrigido
/// (asset ausente) para produtos cujo <c>imageUrl</c> aponta para um arquivo que EXISTE
/// em <c>wwwroot/</c> (isto é, <c>isBugCondition(X) == false</c>) e travamos esse
/// comportamento em asserções. Como a correção (Task 3) apenas RECRIA um arquivo de
/// asset físico — sem tocar em código (<c>ProductCard.razor</c>) nem dados
/// (<c>products.json</c>) — o comportamento de resolução/renderização para essas
/// entradas deve ser exatamente o mesmo antes e depois da correção:
/// <c>F(X) = F'(X)</c>.
///
/// A condição do bug é definida no design como:
/// <code>isBugCondition(X) = NOT fileExists(wwwrootPath(X.imageUrl))</code>
///
/// Estes testes devem PASSAR contra o código NÃO corrigido (linha de base) e continuar
/// passando após a correção, demonstrando que:
///   - Req 3.1: um produto com <c>imageUrl</c> apontando para arquivo existente exibe a
///     imagem normalmente e resolve para o MESMO caminho (a <c>src</c> == <c>imageUrl</c>);
///   - Req 3.2: o markup de <c>ProductCard.razor</c> mantém o handler <c>onerror</c>
///     apontando para <c>images/placeholder-product.png</c> (guarda de preservação de
///     código — o código não será tocado pela correção);
///   - Req 3.3: os demais campos (nome, descrição, categoria, preço, <c>amazonUrl</c>)
///     são renderizados sem alteração.
///
/// Validates: Requirements 3.1, 3.2, 3.3
/// </summary>
public class ProductImageRestorePreservationTests
{
    // -------------------------------------------------------------------------
    // Localização de arquivos-fonte do repositório em tempo de teste
    // -------------------------------------------------------------------------

    /// <summary>
    /// Sobe a partir do diretório de saída dos testes até a raiz do repositório,
    /// identificada pela presença de "MaterDomus.Web.csproj".
    /// </summary>
    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "MaterDomus.Web.csproj")))
                return dir.FullName;
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException(
            "Não foi possível localizar a raiz do repositório (MaterDomus.Web.csproj) a partir de " +
            AppContext.BaseDirectory);
    }

    private static string RepoFile(params string[] segments) =>
        Path.Combine(new[] { FindRepoRoot() }.Concat(segments).ToArray());

    private static string WwwrootPath => RepoFile("wwwroot");

    /// <summary>
    /// Resolve um <c>imageUrl</c> relativo (a partir de <c>wwwroot/</c>) para o caminho
    /// físico dentro de <c>wwwroot/</c>. Espelha <c>wwwrootPath(imageUrl)</c> do design.
    /// </summary>
    private static string WwwrootPathOf(string imageUrl)
    {
        var relative = imageUrl.Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(WwwrootPath, relative);
    }

    /// <summary>
    /// Codifica a condição do bug do design:
    /// <c>isBugCondition(X) = NOT fileExists(wwwrootPath(X.imageUrl))</c>.
    /// Uma entrada com <c>isBugCondition == false</c> é uma entrada de PRESERVAÇÃO.
    /// </summary>
    private static bool IsBugCondition(Product product) =>
        !File.Exists(WwwrootPathOf(product.ImageUrl));

    // -------------------------------------------------------------------------
    // Helpers de contexto / produto
    // -------------------------------------------------------------------------

    private static Bunit.TestContext CreateContext()
    {
        var ctx = new Bunit.TestContext();
        ctx.Services.AddScoped<FavoritesService>();
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

    /// <summary>
    /// Descobre <c>imageUrl</c>s (relativos a <c>wwwroot/</c>) de arquivos de imagem que
    /// EXISTEM no repositório — portanto <c>isBugCondition == false</c>. Usa as imagens de
    /// origem em <c>images/products/DFW300/</c>, que já existem no código não corrigido e
    /// permanecem existentes após a correção (a correção só ADICIONA um asset, não remove).
    /// </summary>
    private static IReadOnlyList<string> ExistingImageUrls()
    {
        var productsDir = RepoFile("wwwroot", "images", "products");
        if (!Directory.Exists(productsDir))
            return Array.Empty<string>();

        var imageExts = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

        return Directory
            .EnumerateFiles(productsDir, "*", SearchOption.AllDirectories)
            .Where(p => imageExts.Contains(Path.GetExtension(p), StringComparer.OrdinalIgnoreCase))
            .Select(p => Path.GetRelativePath(WwwrootPath, p).Replace(Path.DirectorySeparatorChar, '/'))
            .OrderBy(u => u, StringComparer.Ordinal)
            .ToList();
    }

    // =========================================================================
    // OBSERVATION-FIRST — sanity checks da linha de base (não-corrigida)
    // =========================================================================

    /// <summary>
    /// Confirma que existem imagens de origem em <c>wwwroot/</c> para servir de entradas
    /// de preservação (<c>isBugCondition == false</c>) já no código NÃO corrigido. Sem
    /// isso, as propriedades de preservação não teriam domínio de entrada válido.
    /// </summary>
    [Fact]
    public void Baseline_ExistingProductImages_ArePresentInWwwroot()
    {
        var urls = ExistingImageUrls();

        Assert.NotEmpty(urls);
        Assert.All(urls, url =>
            Assert.False(
                IsBugCondition(MakeProduct(imageUrl: url)),
                $"Esperava-se que '{url}' apontasse para um arquivo existente (entrada de preservação)."));
    }

    // =========================================================================
    // UNIT TESTS — renderização do cartão para entradas de preservação
    // Req 3.1, 3.3
    // =========================================================================

    /// <summary>
    /// Para um produto cujo <c>imageUrl</c> aponta para um arquivo EXISTENTE
    /// (<c>isBugCondition == false</c>), o cartão resolve a imagem para o MESMO caminho
    /// (<c>src == imageUrl</c>) e renderiza todos os demais campos preservados: alt,
    /// categoria, nome, descrição truncada, preço formatado e o botão Amazon. (Req 3.1, 3.3)
    /// </summary>
    [Fact]
    public void ProductCard_ExistingImage_ResolvesToSamePath_AndPreservesFields()
    {
        var existingUrl = ExistingImageUrls().First();
        Assert.False(IsBugCondition(MakeProduct(imageUrl: existingUrl)));

        using var ctx = CreateContext();
        var product = MakeProduct(
            name: "Dispenser Quadrado Flow 1L",
            description: "Dispenser quadrado com dosador pump.",
            imageUrl: existingUrl,
            category: "Lavanderia",
            price: 39.90m,
            amazonUrl: "https://www.amazon.com.br/dp/B0GKPPS5YH?m=A20TN3HCSY6KZV");

        var cut = ctx.RenderComponent<ProductCard>(
            parameters => parameters.Add(p => p.Product, product));

        // Imagem resolve para o MESMO caminho (src == imageUrl) — Req 3.1.
        var img = cut.Find("img.product-card__image");
        Assert.Equal(existingUrl, img.GetAttribute("src"));
        Assert.Equal("Dispenser Quadrado Flow 1L", img.GetAttribute("alt"));

        // Demais campos inalterados — Req 3.3.
        Assert.Equal("Lavanderia", cut.Find("span.product-card__category").TextContent.Trim());
        Assert.Equal("Dispenser Quadrado Flow 1L", cut.Find("h3.product-card__name").TextContent.Trim());
        Assert.Equal(
            ProductHelpers.TruncateDescription(product.Description),
            cut.Find("p.product-card__description").TextContent.Trim());
        Assert.Equal(
            ProductHelpers.FormatPrice(product.Price),
            cut.Find("p.product-card__price").TextContent.Trim());

        var amazonBtn = cut.Find("a.product-card__amazon-btn");
        Assert.Equal(product.AmazonUrl, amazonBtn.GetAttribute("href"));
    }

    // =========================================================================
    // CODE-PRESERVATION GUARD — ProductCard.razor onerror inalterado
    // Req 3.2
    // =========================================================================

    /// <summary>
    /// Guarda de preservação de código: o markup renderizado de <c>ProductCard</c> mantém
    /// o handler <c>onerror</c> da imagem apontando para <c>images/placeholder-product.png</c>.
    /// A correção NÃO tocará o componente, portanto este fallback deve permanecer idêntico. (Req 3.2)
    /// </summary>
    [Fact]
    public void ProductCard_OnErrorFallback_PointsToPlaceholder_Markup()
    {
        using var ctx = CreateContext();
        var cut = ctx.RenderComponent<ProductCard>(
            parameters => parameters.Add(p => p.Product, MakeProduct()));

        var img = cut.Find("img.product-card__image");
        var onError = img.GetAttribute("onerror") ?? string.Empty;

        Assert.Contains("images/placeholder-product.png", onError);
    }

    /// <summary>
    /// Guarda de preservação de código (nível de fonte): o arquivo
    /// <c>Shared/ProductCard.razor</c> contém o handler <c>onerror</c> com
    /// <c>images/placeholder-product.png</c>. Trava o texto-fonte porque a correção é
    /// puramente de asset e NÃO deve editar o componente. (Req 3.2)
    /// </summary>
    [Fact]
    public void ProductCardSource_RetainsOnErrorPlaceholder()
    {
        var razorPath = RepoFile("Shared", "ProductCard.razor");
        Assert.True(File.Exists(razorPath), $"ProductCard.razor não encontrado em: {razorPath}");

        var source = File.ReadAllText(razorPath);

        Assert.Contains("onerror", source);
        Assert.Contains("images/placeholder-product.png", source);
    }

    // =========================================================================
    // PROPERTY-BASED TESTS (FsCheck)
    // Req 3.1, 3.3
    // =========================================================================

    /// <summary>
    /// Gerador de texto imprimível (sem controle nem CR/LF), com pelo menos um char
    /// não-branco, para nome/descrição/categoria/id.
    /// </summary>
    private static Gen<string> TextGen() =>
        Gen.NonEmptyListOf(Gen.Choose(0x20, 0x7E).Select(c => (char)c))
           .Select(chars => new string(chars.ToArray()))
           .Where(s => !string.IsNullOrWhiteSpace(s));

    /// <summary>
    /// Gera produtos de PRESERVAÇÃO: campos textuais variados, mas <c>imageUrl</c> sempre
    /// escolhido dentre imagens que EXISTEM em <c>wwwroot/</c>, garantindo
    /// <c>isBugCondition == false</c> por construção.
    /// </summary>
    private static Gen<Product> PreservationProductGen()
    {
        var existingUrls = ExistingImageUrls();

        Gen<string> textGen = TextGen();
        Gen<decimal> priceGen = Gen.Choose(1, 500000).Select(cents => cents / 100m);
        Gen<string> imageUrlGen = Gen.Elements<string>(existingUrls);
        Gen<bool> hasAmazon = ArbMap.Default.ArbFor<bool>().Generator;

        return textGen.SelectMany(id =>
               textGen.SelectMany(name =>
               textGen.SelectMany(desc =>
               textGen.SelectMany(cat =>
               imageUrlGen.SelectMany(img =>
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

    // Feature: product-image-restore, Property 2: Preservation — resolução de imagem existente
    /// <summary>
    /// Para QUALQUER produto de preservação (imageUrl aponta para arquivo existente,
    /// <c>isBugCondition == false</c>), a resolução da imagem é preservada: a <c>src</c>
    /// renderizada é EXATAMENTE o <c>imageUrl</c> e o arquivo continua existindo em
    /// <c>wwwroot/</c> — o mesmo resultado antes e depois da correção (F(X) = F'(X)).
    ///
    /// Validates: Requirements 3.1
    /// </summary>
    [Property(MaxTest = 100)]
    public Property Preservation_ExistingImage_ResolutionUnchanged()
    {
        return Prop.ForAll(PreservationProductGen().ToArbitrary(), product =>
        {
            // Pré-condição: entrada de preservação (sem bug).
            var notBug = !IsBugCondition(product);

            using var ctx = CreateContext();
            var cut = ctx.RenderComponent<ProductCard>(
                parameters => parameters.Add(p => p.Product, product));

            var img = cut.Find("img.product-card__image");
            var srcUnchanged = img.GetAttribute("src") == product.ImageUrl;
            var fileStillExists = File.Exists(WwwrootPathOf(product.ImageUrl));

            return (notBug && srcUnchanged && fileStillExists)
                .ToProperty()
                .Label($"notBug={notBug}, src='{img.GetAttribute("src")}' (esperado '{product.ImageUrl}'), fileExists={fileStillExists}");
        });
    }

    // Feature: product-image-restore, Property 2: Preservation — campos não-imagem inalterados
    /// <summary>
    /// Para QUALQUER produto de preservação, a renderização preserva os demais campos:
    /// alt == Name, categoria == Category, nome == Name, descrição == TruncateDescription,
    /// preço == FormatPrice e o botão Amazon aparece se e somente se AmazonUrl é não-vazia
    /// (com href == AmazonUrl). Independe de <c>imageUrl</c>.
    ///
    /// Validates: Requirements 3.3
    /// </summary>
    [Property(MaxTest = 100)]
    public Property Preservation_NonImageFields_Unchanged()
    {
        return Prop.ForAll(PreservationProductGen().ToArbitrary(), product =>
        {
            using var ctx = CreateContext();
            var cut = ctx.RenderComponent<ProductCard>(
                parameters => parameters.Add(p => p.Product, product));

            var img = cut.Find("img.product-card__image");
            var altOk = img.GetAttribute("alt") == product.Name;

            var categoryOk = cut.Find("span.product-card__category").TextContent == product.Category;
            var nameOk = cut.Find("h3.product-card__name").TextContent == product.Name;

            var expectedDesc = ProductHelpers.TruncateDescription(product.Description) ?? string.Empty;
            var descOk = cut.Find("p.product-card__description").TextContent == expectedDesc;

            var priceOk = cut.Find("p.product-card__price").TextContent == ProductHelpers.FormatPrice(product.Price);

            var buttons = cut.FindAll("a.product-card__amazon-btn");
            bool shouldShow = !string.IsNullOrEmpty(product.AmazonUrl);
            var amazonOk = buttons.Count == (shouldShow ? 1 : 0)
                && (!shouldShow || buttons[0].GetAttribute("href") == product.AmazonUrl);

            return (altOk && categoryOk && nameOk && descOk && priceOk && amazonOk)
                .ToProperty()
                .Label($"alt={altOk}, cat={categoryOk}, name={nameOk}, desc={descOk}, price={priceOk}, amazon={amazonOk}");
        });
    }

    // Feature: product-image-restore, Property 2: Preservation — onerror inalterado por entrada
    /// <summary>
    /// Para QUALQUER produto de preservação, o markup renderizado do cartão mantém o
    /// handler <c>onerror</c> apontando para <c>images/placeholder-product.png</c> — o
    /// mecanismo de fallback do <c>ProductCard.razor</c> é preservado independentemente
    /// dos dados do produto (a correção não toca o componente). (Req 3.2)
    ///
    /// Validates: Requirements 3.2
    /// </summary>
    [Property(MaxTest = 100)]
    public Property Preservation_OnErrorFallback_AlwaysPresent()
    {
        return Prop.ForAll(PreservationProductGen().ToArbitrary(), product =>
        {
            using var ctx = CreateContext();
            var cut = ctx.RenderComponent<ProductCard>(
                parameters => parameters.Add(p => p.Product, product));

            var img = cut.Find("img.product-card__image");
            var onError = img.GetAttribute("onerror") ?? string.Empty;
            var hasPlaceholder = onError.Contains("images/placeholder-product.png", StringComparison.Ordinal);

            return hasPlaceholder
                .ToProperty()
                .Label($"onerror='{onError}'");
        });
    }
}
