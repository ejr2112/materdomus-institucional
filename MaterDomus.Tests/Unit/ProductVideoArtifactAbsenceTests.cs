// Feature: remove-product-video (bugfix)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
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
/// Teste de EXPLORAÇÃO da condição do bug do recurso "vídeo do produto"
/// (spec bugfix "remove-product-video").
///
/// Property 1: Bug Condition — Ausência total de artefatos de vídeo.
///
/// A condição do bug (`isBugCondition`) é a PRESENÇA de qualquer artefato do
/// recurso de vídeo em cinco superfícies concretas:
///   1. propriedade `VideoUrl` no record `Product`;
///   2. marcação `&lt;iframe&gt;` / classes `product-card__video`/`product-card__video-wrapper`
///      renderizadas por `ProductCard.razor`;
///   3. regras CSS `product-card__video`/`product-card__video-wrapper` em `produtos.css`;
///   4. campo `videoUrl` em `wwwroot/data/products.json`;
///   5. entradas `*.mov`/`*.mp4` no `.gitignore` introduzidas para o recurso.
///
/// Como a condição é determinística sobre superfícies concretas, a propriedade é
/// escopada aos casos concretos verificáveis. No estado atual (pós-revert a646466)
/// espera-se que TODAS as asserções PASSEM (confirmando remoção completa). Uma falha
/// aqui expõe o artefato exato remanescente — NÃO "corrigir" o teste.
///
/// Validates: Requirements 1.1, 1.2, 1.3, 2.1, 2.2, 2.3
/// </summary>
public class ProductVideoArtifactAbsenceTests
{
    // -------------------------------------------------------------------------
    // Localização de arquivos-fonte do repositório em tempo de teste
    // -------------------------------------------------------------------------

    /// <summary>
    /// Sobe a partir do diretório de saída dos testes (bin/Debug/netX) até a raiz
    /// do repositório, identificada pela presença de "MaterDomus.Web.csproj".
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

    private static readonly JsonSerializerOptions WebJsonOptions =
        new(JsonSerializerDefaults.Web); // camelCase + case-insensitive, igual a GetFromJsonAsync

    private static Bunit.TestContext CreateContext()
    {
        var ctx = new Bunit.TestContext();
        ctx.Services.AddScoped<FavoritesService>();
        return ctx;
    }

    private static Product MakeProduct() =>
        new Product(
            Id: "prod-video-test",
            Name: "Produto Teste",
            Description: "Descrição de teste para o produto.",
            ImageUrl: "images/products/x.jpg",
            Category: "Teste",
            Price: 49.90m,
            AmazonUrl: "https://www.amazon.com.br/dp/AAAAAAAAAA");

    // -------------------------------------------------------------------------
    // Superfície 1 — Modelo Product (reflexão)
    // -------------------------------------------------------------------------

    /// <summary>
    /// isBugCondition parte 1: `input.productModel.hasProperty("VideoUrl")`.
    /// Afirma via reflexão que o record `Product` NÃO expõe uma propriedade `VideoUrl`.
    /// </summary>
    [Fact]
    public void ProductModel_DoesNotExpose_VideoUrlProperty()
    {
        var videoUrlProp = typeof(Product).GetProperties()
            .FirstOrDefault(p => string.Equals(p.Name, "VideoUrl", StringComparison.OrdinalIgnoreCase));

        Assert.True(
            videoUrlProp is null,
            $"Vestígio do bug: o record Product expõe a propriedade '{videoUrlProp?.Name}' " +
            $"do tipo '{videoUrlProp?.PropertyType.Name}'. Deveria estar ausente.");
    }

    // -------------------------------------------------------------------------
    // Superfície 2 — Marcação renderizada do ProductCard (bUnit)
    // -------------------------------------------------------------------------

    /// <summary>
    /// isBugCondition parte 2: `productCardMarkup.contains("&lt;iframe")` ou
    /// `productCardMarkup.contains("product-card__video")`.
    /// Renderiza `ProductCard` e afirma que a marcação NÃO contém iframe nem as
    /// classes do player de vídeo.
    /// </summary>
    [Fact]
    public void ProductCardMarkup_DoesNotContain_VideoPlayerArtifacts()
    {
        using var ctx = CreateContext();
        var cut = ctx.RenderComponent<ProductCard>(
            parameters => parameters.Add(p => p.Product, MakeProduct()));

        var markup = cut.Markup;

        Assert.False(
            markup.Contains("<iframe", StringComparison.OrdinalIgnoreCase),
            "Vestígio do bug: a marcação do ProductCard contém um <iframe> de vídeo.");
        Assert.False(
            markup.Contains("product-card__video-wrapper", StringComparison.Ordinal),
            "Vestígio do bug: a marcação do ProductCard contém a classe 'product-card__video-wrapper'.");
        Assert.False(
            markup.Contains("product-card__video", StringComparison.Ordinal),
            "Vestígio do bug: a marcação do ProductCard contém a classe 'product-card__video'.");
    }

    // -------------------------------------------------------------------------
    // Superfície 3 — CSS produtos.css (inspeção de arquivo)
    // -------------------------------------------------------------------------

    /// <summary>
    /// isBugCondition parte 3: `css.definesRule("product-card__video")` /
    /// `css.definesRule("product-card__video-wrapper")`.
    /// Inspeciona `wwwroot/css/produtos.css` e afirma que NÃO define as regras do player.
    /// </summary>
    [Fact]
    public void ProdutosCss_DoesNotDefine_VideoPlayerRules()
    {
        var cssPath = RepoFile("wwwroot", "css", "produtos.css");
        Assert.True(File.Exists(cssPath), $"CSS não encontrado em: {cssPath}");

        var css = File.ReadAllText(cssPath);

        Assert.False(
            css.Contains("product-card__video-wrapper", StringComparison.Ordinal),
            "Vestígio do bug: produtos.css define a regra 'product-card__video-wrapper'.");
        Assert.False(
            css.Contains("product-card__video", StringComparison.Ordinal),
            "Vestígio do bug: produtos.css define a regra 'product-card__video'.");
    }

    // -------------------------------------------------------------------------
    // Superfície 4 — Dados products.json (deserialização + inspeção do JSON cru)
    // -------------------------------------------------------------------------

    /// <summary>
    /// isBugCondition parte 4: `productsJson.anyProduct.hasField("videoUrl")`.
    /// Deserializa `wwwroot/data/products.json` em List&lt;Product&gt; (sucesso) e
    /// afirma, sobre o JSON cru, que nenhum item de produto contém o campo `videoUrl`.
    /// </summary>
    [Fact]
    public void ProductsJson_ContainsNo_VideoUrlField()
    {
        var jsonPath = RepoFile("wwwroot", "data", "products.json");
        Assert.True(File.Exists(jsonPath), $"products.json não encontrado em: {jsonPath}");

        var json = File.ReadAllText(jsonPath);

        // Deserializa com sucesso no modelo atual (mesmas opções de GetFromJsonAsync).
        var products = JsonSerializer.Deserialize<List<Product>>(json, WebJsonOptions);
        Assert.NotNull(products);

        // Inspeciona o JSON cru: nenhum objeto de produto deve possuir a propriedade "videoUrl".
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);

        var offenders = new List<string>();
        foreach (var element in doc.RootElement.EnumerateArray())
        {
            if (element.ValueKind != JsonValueKind.Object)
                continue;

            var videoField = element.EnumerateObject()
                .FirstOrDefault(p => string.Equals(p.Name, "videoUrl", StringComparison.OrdinalIgnoreCase));

            if (videoField.Value.ValueKind != JsonValueKind.Undefined)
            {
                var id = element.TryGetProperty("id", out var idProp) ? idProp.GetString() : "(sem id)";
                offenders.Add($"produto '{id}' possui campo '{videoField.Name}'='{videoField.Value}'");
            }
        }

        Assert.True(
            offenders.Count == 0,
            "Vestígio do bug: products.json contém campo videoUrl -> " + string.Join("; ", offenders));
    }

    /// <summary>
    /// Complemento da superfície 4: um payload JSON com um campo `videoUrl` extra deve
    /// ser IGNORADO na deserialização, sem alterar o `Product` resultante (o modelo não
    /// reintroduz o campo). Compara com o mesmo payload sem `videoUrl`.
    /// </summary>
    [Fact]
    public void Deserialization_IgnoresExtra_VideoUrlField_WithoutAlteringProduct()
    {
        const string withVideo = """
            {
              "id": "prod-x",
              "name": "Produto X",
              "description": "Desc.",
              "imageUrl": "images/products/x.jpg",
              "category": "Casa",
              "price": 19.90,
              "amazonUrl": "https://www.amazon.com.br/dp/AAAAAAAAAA",
              "videoUrl": "https://onedrive.live.com/embed?resid=EXTRA"
            }
            """;

        const string withoutVideo = """
            {
              "id": "prod-x",
              "name": "Produto X",
              "description": "Desc.",
              "imageUrl": "images/products/x.jpg",
              "category": "Casa",
              "price": 19.90,
              "amazonUrl": "https://www.amazon.com.br/dp/AAAAAAAAAA"
            }
            """;

        var productWithVideo = JsonSerializer.Deserialize<Product>(withVideo, WebJsonOptions);
        var productWithoutVideo = JsonSerializer.Deserialize<Product>(withoutVideo, WebJsonOptions);

        Assert.NotNull(productWithVideo);
        Assert.NotNull(productWithoutVideo);

        // O campo extra é ignorado: o record resultante é idêntico (igualdade estrutural de record).
        Assert.Equal(productWithoutVideo, productWithVideo);
    }

    // -------------------------------------------------------------------------
    // Superfície 5 — .gitignore (inspeção de arquivo)
    // -------------------------------------------------------------------------

    /// <summary>
    /// isBugCondition parte 5: `gitignore.containsEntry("*.mov")` /
    /// `gitignore.containsEntry("*.mp4")`.
    /// Inspeciona `.gitignore` e afirma que NÃO contém entradas de mídia introduzidas
    /// para o recurso de vídeo.
    /// </summary>
    [Fact]
    public void Gitignore_ContainsNo_VideoMediaEntries()
    {
        var gitignorePath = RepoFile(".gitignore");
        Assert.True(File.Exists(gitignorePath), $".gitignore não encontrado em: {gitignorePath}");

        var entries = File.ReadAllLines(gitignorePath)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0 && !l.StartsWith('#'))
            .ToArray();

        Assert.DoesNotContain("*.mov", entries);
        Assert.DoesNotContain("*.mp4", entries);
    }

    // -------------------------------------------------------------------------
    // Property 1 (escopada) — Ausência de artefatos de vídeo na renderização
    // -------------------------------------------------------------------------

    // Feature: remove-product-video, Property 1: Bug Condition — Ausência total de artefatos de vídeo
    /// <summary>
    /// Para QUALQUER produto gerado, a renderização de `ProductCard` nunca produz
    /// marcação de vídeo (sem `&lt;iframe&gt;`, sem `product-card__video*`),
    /// independentemente dos dados do produto — cobrindo a parte da condição do bug
    /// que depende da renderização (`productCardMarkup`), sobre muitas entradas.
    ///
    /// Validates: Requirements 2.1, 2.3
    /// </summary>
    [Property(MaxTest = 100)]
    public Property RenderedCard_NeverContainsVideoArtifacts_ForAnyProduct()
    {
        var strGen = ArbMap.Default.ArbFor<NonEmptyString>().Generator.Select(s => s.Get);

        var gen =
            strGen.SelectMany(id =>
            strGen.SelectMany(name =>
            strGen.SelectMany(desc =>
            strGen.SelectMany(cat =>
            ArbMap.Default.ArbFor<bool>().Generator.SelectMany(hasAmazon =>
            Gen.Choose(1, 500000).Select(cents => (
                id,
                name,
                desc,
                cat,
                price: cents / 100m,
                amazonUrl: hasAmazon ? "https://www.amazon.com.br/dp/AAAAAAAAAA" : ""
            )))))));

        return Prop.ForAll(gen.ToArbitrary(), tuple =>
        {
            var (id, name, desc, cat, price, amazonUrl) = tuple;

            using var ctx = CreateContext();
            var product = new Product(id, name, desc, "images/x.jpg", cat, price, amazonUrl);
            var cut = ctx.RenderComponent<ProductCard>(
                parameters => parameters.Add(p => p.Product, product));

            var markup = cut.Markup;
            var noIframe = !markup.Contains("<iframe", StringComparison.OrdinalIgnoreCase);
            var noVideoClass = !markup.Contains("product-card__video", StringComparison.Ordinal);

            return (noIframe && noVideoClass)
                .ToProperty()
                .Label($"noIframe={noIframe}, noVideoClass={noVideoClass}");
        });
    }
}
