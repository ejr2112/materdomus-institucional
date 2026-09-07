// Feature: product-image-restore (bugfix)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using MaterDomus.Web.Models;
using Xunit;

namespace MaterDomus.Tests.Unit;

/// <summary>
/// Teste EXPLORATÓRIO da condição do bug do spec "product-image-restore".
///
/// Property 1: Bug Condition — a imagem do produto resolve para um arquivo existente.
///
/// A condição do bug do design é:
///   <c>isBugCondition(X) = NOT fileExists(wwwrootPath(X.imageUrl))</c>
///
/// A Expected Behavior Property (Property 1) afirma o inverso: para TODO produto
/// carregado de <c>wwwroot/data/products.json</c>, o arquivo apontado por
/// <c>imageUrl</c> DEVE existir em <c>wwwroot/</c>.
///
/// Como o bug é determinístico (asset físico ausente), a propriedade é ESCOPADA ao
/// conjunto concreto de produtos do catálogo, garantindo reprodutibilidade.
///
/// **CRÍTICO**: No código NÃO corrigido este teste DEVE FALHAR — a falha confirma
/// que o bug existe (o arquivo <c>dispenser-flow-quadrado-branco-001.jpg</c> está
/// ausente). NÃO corrigir o teste nem o código nesta etapa; após a correção
/// (Task 3, recriação do asset) o teste passará.
///
/// Validates: Requirements 1.1, 1.2, 2.1, 2.2
/// </summary>
public class ProductImageAssetExistenceTests
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

    private static string WwwrootDir() => Path.Combine(FindRepoRoot(), "wwwroot");

    /// <summary>
    /// Resolve um <c>imageUrl</c> relativo (a partir de <c>wwwroot/</c>) para o
    /// caminho físico dentro de <c>wwwroot/</c> — implementa <c>wwwrootPath(imageUrl)</c>.
    /// Normaliza separadores para o SO atual.
    /// </summary>
    private static string WwwrootPath(string imageUrl)
    {
        var normalized = imageUrl.Replace('\\', '/').TrimStart('/');
        var segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return Path.Combine(new[] { WwwrootDir() }.Concat(segments).ToArray());
    }

    private static readonly JsonSerializerOptions WebJsonOptions =
        new(JsonSerializerDefaults.Web); // camelCase + case-insensitive, igual a GetFromJsonAsync

    /// <summary>Carrega os produtos reais do catálogo (<c>wwwroot/data/products.json</c>).</summary>
    private static List<Product> LoadCatalogProducts()
    {
        var jsonPath = Path.Combine(WwwrootDir(), "data", "products.json");
        Assert.True(File.Exists(jsonPath), $"products.json não encontrado em: {jsonPath}");

        var json = File.ReadAllText(jsonPath);
        var products = JsonSerializer.Deserialize<List<Product>>(json, WebJsonOptions);
        Assert.NotNull(products);
        return products!;
    }

    /// <summary>Codifica a condição do bug do design: imageUrl NÃO resolve para arquivo existente.</summary>
    private static bool IsBugCondition(Product product) =>
        !File.Exists(WwwrootPath(product.ImageUrl));

    // -------------------------------------------------------------------------
    // Property 1 (escopada) — todo imageUrl do catálogo resolve para arquivo existente
    // -------------------------------------------------------------------------

    // Feature: product-image-restore, Property 1: Bug Condition — imagem resolve para arquivo existente
    /// <summary>
    /// Para TODO produto carregado de <c>wwwroot/data/products.json</c>, o arquivo
    /// referenciado por <c>imageUrl</c> DEVE existir em <c>wwwroot/</c>
    /// (invariante do catálogo — negação da condição do bug).
    ///
    /// No código não corrigido, o produto <c>dispenser-flow-quadrado-branco-001</c>
    /// aponta para <c>wwwroot/images/products/dispenser-flow-quadrado-branco-001.jpg</c>,
    /// que NÃO existe — logo esta propriedade FALHA (confirma o bug).
    ///
    /// **Validates: Requirements 1.1, 1.2, 2.1, 2.2**
    /// </summary>
    [Property]
    public Property EveryCatalogImageUrl_ResolvesToExistingFile()
    {
        var products = LoadCatalogProducts();
        var productGen = Gen.Elements<Product>(products);

        return Prop.ForAll(productGen.ToArbitrary(), product =>
        {
            var physicalPath = WwwrootPath(product.ImageUrl);
            var exists = File.Exists(physicalPath);

            return exists
                .ToProperty()
                .Label(
                    $"Counterexample (condição do bug): produto '{product.Id}' com imageUrl " +
                    $"'{product.ImageUrl}' NÃO resolve para um arquivo existente em wwwroot " +
                    $"(esperado em '{physicalPath}').");
        });
    }

    // -------------------------------------------------------------------------
    // Caso concreto — o produto afetado (counterexample esperado no código não corrigido)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Caso concreto do design: o produto <c>dispenser-flow-quadrado-branco-001</c>
    /// deve ter seu <c>imageUrl</c> resolvido para um arquivo existente. No código
    /// não corrigido este teste FALHA (o asset foi apagado) — confirma o bug.
    ///
    /// **Validates: Requirements 1.1, 1.2, 2.1, 2.2**
    /// </summary>
    [Fact]
    public void AffectedProduct_ImageUrl_ResolvesToExistingFile()
    {
        var products = LoadCatalogProducts();
        var affected = products.FirstOrDefault(p => p.Id == "dispenser-flow-quadrado-branco-001");

        Assert.NotNull(affected);

        var physicalPath = WwwrootPath(affected!.ImageUrl);

        Assert.True(
            File.Exists(physicalPath),
            $"Counterexample (condição do bug): imageUrl '{affected.ImageUrl}' não resolve " +
            $"para um arquivo existente em wwwroot (esperado em '{physicalPath}').");
    }

    // -------------------------------------------------------------------------
    // Origem BCF — confirma que a imagem de origem para a correção está disponível
    // -------------------------------------------------------------------------

    /// <summary>
    /// A imagem de origem BCF ("Tampa Branca") deve existir — confirma que a origem
    /// para a correção (Task 3) está disponível. Este teste PASSA no código não
    /// corrigido.
    ///
    /// **Validates: Requirements 2.1, 2.2**
    /// </summary>
    [Fact]
    public void BcfSourceImage_Exists()
    {
        var sourcePath = Path.Combine(
            WwwrootDir(),
            "images", "products", "DFW300",
            "DFW300_DISPENSER QUADRADO Flow 1L_BCF.jpg");

        Assert.True(
            File.Exists(sourcePath),
            $"A imagem de origem BCF ('Tampa Branca') não foi encontrada em '{sourcePath}'. " +
            "Ela é necessária como origem para recriar o asset ausente na correção.");
    }
}
