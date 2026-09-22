using Xunit;

namespace MaterDomus.Tests.Unit;

/// <summary>
/// The vitrine grid keeps every product photo inside the same square frame.
/// Contain (not cover) shows the full product; the cream mat is the letterbox.
/// </summary>
public class ProductCardImageFrameTests
{
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
            "Não foi possível localizar a raiz do repositório (MaterDomus.Web.csproj).");
    }

    private static string RuleBlock(string css, string selector)
    {
        var marker = selector + " {";
        var start = css.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(start >= 0, $"Falta a regra {selector}.");
        var end = css.IndexOf('}', start);
        Assert.True(end > start, $"Regra {selector} sem fechamento.");
        return css.Substring(start, end - start);
    }

    [Fact]
    public void ProdutosCss_ProductCardsShareSquareContainFrame()
    {
        var css = File.ReadAllText(Path.Combine(FindRepoRoot(), "wwwroot", "css", "produtos.css"));

        Assert.Contains("--product-frame-bg: #f3efe8", css);

        var wrapper = RuleBlock(css, ".product-card__image-wrapper");
        Assert.Contains("aspect-ratio: 1 / 1", wrapper);
        Assert.DoesNotContain("4 / 3", wrapper);
        Assert.Contains("background: var(--product-frame-bg)", wrapper);
        Assert.DoesNotContain("#fff", wrapper);
        Assert.DoesNotContain("#ffffff", wrapper);

        var image = RuleBlock(css, ".product-card__image");
        Assert.Contains("object-fit: contain", image);
        Assert.Contains("object-position: center", image);
        Assert.DoesNotContain("object-fit: cover", image);
        Assert.DoesNotContain("object-fit: fill", image);
        Assert.Contains("width: 100%", image);
        Assert.Contains("height: 100%", image);
        Assert.Contains("max-width: 100%", image);
        Assert.Contains("max-height: 100%", image);

        var skeleton = RuleBlock(css, ".skeleton-card__image");
        Assert.Contains("aspect-ratio: 1 / 1", skeleton);
    }
}
