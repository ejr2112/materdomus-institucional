// Feature: menu-redesign

using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace MaterDomus.Tests.Unit;

/// <summary>
/// Testes unitários que verificam regras CSS críticas em wwwroot/css/site.css.
/// Lê o arquivo CSS como texto e aplica asserções string/regex.
/// Valida: Requisitos 1.1, 3.7
/// </summary>
public class CssCriticalRulesTests
{
    // Navega de bin/Debug/net9.0/ até a raiz do projeto, depois até o CSS
    private static readonly string CssFilePath = Path.GetFullPath(
        Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "..", "..", "..", "..",
            "wwwroot", "css", "site.css"
        )
    );

    private static string ReadCss() => File.ReadAllText(CssFilePath);

    // -------------------------------------------------------------------------
    // Requirement 1.1 / 3.7 — .bottom-nav display: none fora do media query
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifica que existe uma regra de nível superior (fora de @media) que define
    /// .bottom-nav { display: none }.
    /// Isso garante que a BottomNav esteja oculta por padrão no desktop.
    /// Validates: Requirements 3.7
    /// </summary>
    [Fact]
    public void BottomNav_HasDisplayNone_OutsideMediaQuery()
    {
        var css = ReadCss();

        // Extrai apenas o conteúdo fora de blocos @media removendo todos os blocos @media {...}
        // Usamos uma abordagem de remoção de blocos @media com contagem de chaves
        var cssWithoutMediaQueries = RemoveMediaQueryBlocks(css);

        // Confirma que fora dos media queries existe ".bottom-nav" com "display: none"
        var bottomNavBlockMatch = Regex.Match(
            cssWithoutMediaQueries,
            @"\.bottom-nav\s*\{[^}]*\}",
            RegexOptions.Singleline
        );

        Assert.True(
            bottomNavBlockMatch.Success,
            "Esperado encontrar uma regra '.bottom-nav { ... }' fora de @media queries no site.css"
        );

        var blockContent = bottomNavBlockMatch.Value;
        Assert.Matches(
            @"display\s*:\s*none",
            blockContent
        );
    }

    // -------------------------------------------------------------------------
    // Requirement 1.1 — nav a.active tem background-color e color corretos
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifica que a regra nav a.active contém background-color: #1f1f1f.
    /// Validates: Requirements 1.1
    /// </summary>
    [Fact]
    public void NavActiveLink_HasCorrectBackgroundColor()
    {
        var css = ReadCss();

        var activeBlockMatch = Regex.Match(
            css,
            @"nav\s+a\.active\s*\{([^}]*)\}",
            RegexOptions.Singleline
        );

        Assert.True(
            activeBlockMatch.Success,
            "Esperado encontrar uma regra 'nav a.active { ... }' no site.css"
        );

        var blockContent = activeBlockMatch.Groups[1].Value;
        Assert.Matches(
            @"background-color\s*:\s*#1f1f1f",
            blockContent
        );
    }

    /// <summary>
    /// Verifica que a regra nav a.active contém color: #ffffff.
    /// Validates: Requirements 1.1
    /// </summary>
    [Fact]
    public void NavActiveLink_HasCorrectTextColor()
    {
        var css = ReadCss();

        var activeBlockMatch = Regex.Match(
            css,
            @"nav\s+a\.active\s*\{([^}]*)\}",
            RegexOptions.Singleline
        );

        Assert.True(
            activeBlockMatch.Success,
            "Esperado encontrar uma regra 'nav a.active { ... }' no site.css"
        );

        var blockContent = activeBlockMatch.Groups[1].Value;
        Assert.Matches(
            @"color\s*:\s*#ffffff",
            blockContent
        );
    }

    // -------------------------------------------------------------------------
    // Helper — remove blocos @media para isolar regras de nível superior
    // -------------------------------------------------------------------------

    /// <summary>
    /// Remove todos os blocos @media { ... } do CSS usando contagem de chaves,
    /// retornando apenas as regras de nível superior.
    /// </summary>
    private static string RemoveMediaQueryBlocks(string css)
    {
        var result = new System.Text.StringBuilder();
        int i = 0;

        while (i < css.Length)
        {
            // Detecta início de bloco @media
            int mediaIndex = css.IndexOf("@media", i, StringComparison.OrdinalIgnoreCase);
            if (mediaIndex == -1)
            {
                // Não há mais blocos @media — adiciona o restante
                result.Append(css, i, css.Length - i);
                break;
            }

            // Adiciona o que vem antes do @media
            result.Append(css, i, mediaIndex - i);

            // Avança até encontrar o '{' de abertura do bloco @media
            int braceStart = css.IndexOf('{', mediaIndex);
            if (braceStart == -1)
                break;

            // Conta chaves para encontrar o '}' de fechamento correspondente
            int depth = 0;
            int j = braceStart;
            while (j < css.Length)
            {
                if (css[j] == '{') depth++;
                else if (css[j] == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        i = j + 1;
                        break;
                    }
                }
                j++;
            }

            if (depth != 0)
                break; // CSS malformado — interrompe
        }

        return result.ToString();
    }
}
