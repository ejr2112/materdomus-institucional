// Feature: mobile-buttons-menu-icons-fix (bugfix)

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace MaterDomus.Tests.Unit;

/// <summary>
/// Teste de EXPLORAÇÃO da condição de bug (Property 1: Bug Condition).
///
/// Lê wwwroot/css/site.css como texto (mesmo padrão de CssCriticalRulesTests)
/// e codifica o COMPORTAMENTO ESPERADO após a correção. Portanto, no código
/// NÃO corrigido, estes testes DEVEM FALHAR — a falha prova que os bugs existem.
///
/// Condições de bug (do design):
///   Bug 1: area = "hero-actions"  AND viewportWidth &lt;= 768
///   Bug 2: area = "bottom-nav"    AND isActive = true AND viewportWidth &lt;= 768
///
/// Validates: Requirements 1.1, 1.2, 2.1, 2.2
/// </summary>
public class MobileButtonsMenuBugConditionTests
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
    // Bug 1 — .hero-actions deve ter layout flex/coluna dentro de @media 768px
    //         (condição de bug: area = "hero-actions" AND viewportWidth <= 768)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Property 1 (Bug 1): Dentro do bloco @media (max-width: 768px) deve existir
    /// uma regra .hero-actions contendo display: flex e flex-direction: column,
    /// garantindo os botões da hero organizados/empilhados no mobile.
    /// No código não corrigido NÃO há regra .hero-actions no @media 768px → FALHA.
    /// Validates: Requirements 1.1, 2.1
    /// </summary>
    [Fact]
    public void HeroActions_HasStackedFlexLayout_WithinMobileMediaQuery()
    {
        var css = ReadCss();

        // Concatena o conteúdo de todos os blocos @media (max-width: 768px)
        var mobileCss = ExtractMax768MediaBlocks(css);

        var heroActionsMatch = Regex.Match(
            mobileCss,
            @"\.hero-actions\s*\{([^}]*)\}",
            RegexOptions.Singleline
        );

        Assert.True(
            heroActionsMatch.Success,
            "Contraexemplo (Bug 1): não existe regra '.hero-actions { ... }' dentro do " +
            "@media (max-width: 768px). Os botões da hero ficam inline e quebram no mobile."
        );

        var block = heroActionsMatch.Groups[1].Value;

        Assert.Matches(@"display\s*:\s*flex", block);
        Assert.Matches(@"flex-direction\s*:\s*column", block);
    }

    // -------------------------------------------------------------------------
    // Bug 2 — .bottom-nav a.active deve reverter o fundo escuro herdado
    //         (condição de bug: area = "bottom-nav" AND isActive AND viewportWidth <= 768)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Property 1 (Bug 2): A regra .bottom-nav a.active deve conter
    /// background-color: transparent, revertendo o fundo #1f1f1f herdado de
    /// nav a.active e mantendo o ícone ativo visível no menu inferior.
    /// No código não corrigido a regra define apenas color → FALHA.
    /// Validates: Requirements 1.2, 2.2
    /// </summary>
    [Fact]
    public void ActiveBottomNavLink_HasTransparentBackground()
    {
        var css = ReadCss();

        var activeMatch = Regex.Match(
            css,
            @"\.bottom-nav\s+a\.active\s*\{([^}]*)\}",
            RegexOptions.Singleline
        );

        Assert.True(
            activeMatch.Success,
            "Contraexemplo (Bug 2): não foi possível localizar a regra " +
            "'.bottom-nav a.active { ... }' no site.css."
        );

        var block = activeMatch.Groups[1].Value;

        Assert.Matches(
            @"background-color\s*:\s*transparent",
            block
        );
    }

    // -------------------------------------------------------------------------
    // Helper — extrai e concatena o conteúdo de todos os blocos
    //          @media (max-width: 768px) { ... }
    // -------------------------------------------------------------------------

    private static string ExtractMax768MediaBlocks(string css)
    {
        var sb = new System.Text.StringBuilder();

        // Casa o cabeçalho @media (max-width: 768px), tolerante a espaços
        var headerRegex = new Regex(
            @"@media[^\{]*\(\s*max-width\s*:\s*768px\s*\)[^\{]*\{",
            RegexOptions.IgnoreCase
        );

        foreach (Match header in headerRegex.Matches(css))
        {
            int braceStart = header.Index + header.Length - 1; // posição do '{'
            int depth = 0;

            for (int j = braceStart; j < css.Length; j++)
            {
                if (css[j] == '{')
                {
                    depth++;
                }
                else if (css[j] == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        // Conteúdo interno do bloco @media (sem as chaves externas)
                        sb.Append(css, braceStart + 1, j - braceStart - 1);
                        sb.Append('\n');
                        break;
                    }
                }
            }
        }

        return sb.ToString();
    }
}
