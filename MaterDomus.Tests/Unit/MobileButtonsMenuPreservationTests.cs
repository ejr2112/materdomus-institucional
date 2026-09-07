// Feature: mobile-buttons-menu-icons-fix (bugfix)

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Xunit;

namespace MaterDomus.Tests.Unit;

/// <summary>
/// Testes de PRESERVAÇÃO da spec bugfix "mobile-buttons-menu-icons-fix".
///
/// Property 2: Preservation — Comportamento inalterado fora da condição de bug.
///
/// Metodologia observation-first: observamos o comportamento no código NÃO corrigido
/// (estado atual de wwwroot/css/site.css), registramos esse comportamento e o travamos
/// em testes. Estes testes DEVEM PASSAR contra o código atual (linha de base) e continuar
/// passando após a correção dos dois bugs de CSS (Task 3), demonstrando que:
///   - o menu de topo ativo (nav a.active) mantém fundo escuro #1f1f1f e texto branco;
///   - os botões da hero em desktop continuam lado a lado (.hero-actions fora do
///     @media (max-width: 768px) mantém apenas margin-top: 30px, sem display: flex);
///   - .btn-primary mantém margin-right: 10px no nível superior;
///   - links inativos do menu inferior (.bottom-nav a) mantêm color: #888 sem fundo;
///   - os estilos visuais dos botões (.btn-primary, .btn-secondary: cores, bordas,
///     border-radius) permanecem inalterados.
///
/// Lê o arquivo CSS como texto e aplica asserções string/regex, seguindo o mesmo
/// padrão de CssCriticalRulesTests.
///
/// Validates: Requirements 3.1, 3.2, 3.3, 3.4
/// </summary>
public class MobileButtonsMenuPreservationTests
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
    // Helpers — extração de regras e isolamento de blocos @media
    // -------------------------------------------------------------------------

    /// <summary>
    /// Remove todos os blocos @media { ... } do CSS usando contagem de chaves,
    /// retornando apenas as regras de nível superior (fora de @media).
    /// Mesma abordagem de CssCriticalRulesTests.
    /// </summary>
    private static string RemoveMediaQueryBlocks(string css)
    {
        var result = new StringBuilder();
        int i = 0;

        while (i < css.Length)
        {
            int mediaIndex = css.IndexOf("@media", i, StringComparison.OrdinalIgnoreCase);
            if (mediaIndex == -1)
            {
                result.Append(css, i, css.Length - i);
                break;
            }

            result.Append(css, i, mediaIndex - i);

            int braceStart = css.IndexOf('{', mediaIndex);
            if (braceStart == -1)
                break;

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
                break;
        }

        return result.ToString();
    }

    /// <summary>
    /// Extrai o conteúdo (entre chaves) da primeira regra cujo seletor casa com
    /// <paramref name="selectorPattern"/>. Retorna null se a regra não existir.
    /// </summary>
    private static string? ExtractRuleBody(string css, string selectorPattern)
    {
        var match = Regex.Match(
            css,
            selectorPattern + @"\s*\{([^}]*)\}",
            RegexOptions.Singleline
        );
        return match.Success ? match.Groups[1].Value : null;
    }

    // =========================================================================
    // UNIT TESTS — baseline (linha de base) do código não corrigido
    // =========================================================================

    // ----- Req 3.2 — nav a.active preserva fundo escuro e texto branco ---------

    /// <summary>
    /// nav a.active mantém background-color: #1f1f1f (menu de topo ativo em desktop). (Req 3.2)
    /// </summary>
    [Fact]
    public void NavActive_PreservesDarkBackground()
    {
        var css = ReadCss();
        var body = ExtractRuleBody(css, @"nav\s+a\.active");

        Assert.NotNull(body);
        Assert.Matches(@"background-color\s*:\s*#1f1f1f", body!);
    }

    /// <summary>
    /// nav a.active mantém color: #ffffff (texto branco do menu de topo ativo). (Req 3.2)
    /// </summary>
    [Fact]
    public void NavActive_PreservesWhiteText()
    {
        var css = ReadCss();
        var body = ExtractRuleBody(css, @"nav\s+a\.active");

        Assert.NotNull(body);
        Assert.Matches(@"color\s*:\s*#ffffff", body!);
    }

    // ----- Req 3.1 — .hero-actions no desktop mantém apenas margin-top ---------

    /// <summary>
    /// Fora do @media (max-width: 768px), a regra .hero-actions mantém apenas
    /// margin-top: 30px e NÃO declara display: flex (botões lado a lado em desktop). (Req 3.1)
    /// </summary>
    [Fact]
    public void HeroActions_TopLevel_HasOnlyMarginTop_NoFlex()
    {
        var css = ReadCss();
        var topLevelCss = RemoveMediaQueryBlocks(css);
        var body = ExtractRuleBody(topLevelCss, @"\.hero-actions");

        Assert.NotNull(body);
        Assert.Matches(@"margin-top\s*:\s*30px", body!);
        Assert.DoesNotMatch(@"display\s*:\s*flex", body!);
    }

    // ----- Req 3.1, 3.4 — .btn-primary mantém margin-right no nível superior ---

    /// <summary>
    /// A regra .btn-primary de nível superior mantém margin-right: 10px
    /// (espaçamento lado a lado em desktop). (Req 3.1, 3.4)
    /// </summary>
    [Fact]
    public void BtnPrimary_TopLevel_PreservesMarginRight()
    {
        var css = ReadCss();
        var topLevelCss = RemoveMediaQueryBlocks(css);
        var body = ExtractRuleBody(topLevelCss, @"\.btn-primary");

        Assert.NotNull(body);
        Assert.Matches(@"margin-right\s*:\s*10px", body!);
    }

    // ----- Req 3.3 — .bottom-nav a (inativo) mantém #888 sem fundo -------------

    /// <summary>
    /// A regra .bottom-nav a (link inativo do menu inferior) mantém color: #888
    /// e NÃO declara background/background-color. (Req 3.3)
    /// </summary>
    [Fact]
    public void BottomNavInactive_PreservesGrayColor_NoBackground()
    {
        var css = ReadCss();
        // .bottom-nav a (mas não .bottom-nav a.active): casa a regra base do link.
        var match = Regex.Match(
            css,
            @"\.bottom-nav\s+a\s*\{([^}]*)\}",
            RegexOptions.Singleline
        );

        Assert.True(match.Success, "Esperado encontrar a regra '.bottom-nav a { ... }' no site.css");
        var body = match.Groups[1].Value;

        Assert.Matches(@"color\s*:\s*#888", body);
        Assert.DoesNotMatch(@"background(-color)?\s*:", body);
    }

    // ----- Req 3.4 — estilos visuais dos botões inalterados --------------------

    /// <summary>
    /// .btn-primary preserva os estilos visuais: background #1f1f1f, color white,
    /// padding 14px 26px e border-radius 6px. (Req 3.4)
    /// </summary>
    [Fact]
    public void BtnPrimary_PreservesVisualStyles()
    {
        var css = ReadCss();
        var body = ExtractRuleBody(css, @"\.btn-primary");

        Assert.NotNull(body);
        Assert.Matches(@"background\s*:\s*#1f1f1f", body!);
        Assert.Matches(@"color\s*:\s*white", body!);
        Assert.Matches(@"padding\s*:\s*14px\s+26px", body!);
        Assert.Matches(@"border-radius\s*:\s*6px", body!);
    }

    /// <summary>
    /// .btn-secondary preserva os estilos visuais: border 1px solid #1f1f1f,
    /// color #1f1f1f, padding 14px 26px e border-radius 6px. (Req 3.4)
    /// </summary>
    [Fact]
    public void BtnSecondary_PreservesVisualStyles()
    {
        var css = ReadCss();
        var body = ExtractRuleBody(css, @"\.btn-secondary");

        Assert.NotNull(body);
        Assert.Matches(@"border\s*:\s*1px\s+solid\s+#1f1f1f", body!);
        Assert.Matches(@"color\s*:\s*#1f1f1f", body!);
        Assert.Matches(@"padding\s*:\s*14px\s+26px", body!);
        Assert.Matches(@"border-radius\s*:\s*6px", body!);
    }

    // =========================================================================
    // PROPERTY-BASED TESTS (FsCheck)
    // =========================================================================

    /// <summary>
    /// Modela uma área de navegação com estado ativo/inativo, para gerar as
    /// combinações relevantes de (área, isActive).
    /// </summary>
    public enum NavArea
    {
        TopNav,     // menu de topo (nav a) — desktop
        BottomNav,  // menu inferior (.bottom-nav a) — mobile
    }

    private static Gen<(NavArea area, bool isActive)> NavContextGen()
    {
        Gen<NavArea> areaGen = Gen.Elements(NavArea.TopNav, NavArea.BottomNav);
        Gen<bool> activeGen = ArbMap.Default.ArbFor<bool>().Generator;
        return areaGen.SelectMany(area => activeGen.Select(isActive => (area, isActive)));
    }

    // Feature: mobile-buttons-menu-icons-fix, Property 2: Preservation
    /// <summary>
    /// Para QUALQUER combinação de (área de navegação, estado ativo/inativo), o fundo
    /// escuro #1f1f1f aparece SOMENTE no menu de topo ativo (nav a.active), nunca no
    /// ícone ativo do menu inferior (.bottom-nav a.active), que no código não corrigido
    /// não declara background próprio.
    ///
    /// Esta propriedade trava a linha de base a ser preservada: o fundo escuro pertence
    /// exclusivamente ao menu de topo ativo em desktop.
    ///
    /// Validates: Requirements 3.2, 3.3
    /// </summary>
    [Property(MaxTest = 100)]
    public Property Preservation_DarkBackground_OnlyOnTopNavActive()
    {
        var css = ReadCss();

        // nav a.active — fundo escuro esperado (menu de topo).
        var topNavActiveBody = ExtractRuleBody(css, @"nav\s+a\.active") ?? string.Empty;

        // .bottom-nav a.active — não deve conter fundo escuro #1f1f1f (herança tratada
        // apenas em desktop pelo seletor genérico; a regra própria não o declara).
        var bottomNavActiveBody = ExtractRuleBody(css, @"\.bottom-nav\s+a\.active") ?? string.Empty;

        return Prop.ForAll(NavContextGen().ToArbitrary(), ctx =>
        {
            bool topNavHasDark = Regex.IsMatch(topNavActiveBody, @"background-color\s*:\s*#1f1f1f");
            bool bottomNavHasDark = Regex.IsMatch(bottomNavActiveBody, @"background-color\s*:\s*#1f1f1f");

            // Invariante independente do gerador: fundo escuro no topo, ausente no rodapé.
            bool baseline = topNavHasDark && !bottomNavHasDark;

            // A afirmação específica por contexto: apenas TopNav+ativo carrega o fundo escuro
            // como estilo próprio declarado.
            bool contextConsistent = ctx switch
            {
                (NavArea.TopNav, true) => topNavHasDark,
                (NavArea.TopNav, false) => true,   // link inativo do topo não é alvo desta propriedade
                (NavArea.BottomNav, true) => !bottomNavHasDark,
                (NavArea.BottomNav, false) => !bottomNavHasDark,
                _ => true,
            };

            return (baseline && contextConsistent)
                .ToProperty()
                .Label($"ctx={ctx}, topNavDark={topNavHasDark}, bottomNavDark={bottomNavHasDark}");
        });
    }

    // Feature: mobile-buttons-menu-icons-fix, Property 2: Preservation
    /// <summary>
    /// Para QUALQUER botão da hero (.btn-primary ou .btn-secondary), os estilos visuais
    /// permanecem constantes: padding 14px 26px e border-radius 6px em ambos; a cor de
    /// destaque #1f1f1f (fundo no primary, borda/texto no secondary).
    ///
    /// Validates: Requirements 3.4
    /// </summary>
    [Property(MaxTest = 100)]
    public Property Preservation_ButtonVisualStyles_AreConstant()
    {
        var css = ReadCss();
        var primaryBody = ExtractRuleBody(css, @"\.btn-primary") ?? string.Empty;
        var secondaryBody = ExtractRuleBody(css, @"\.btn-secondary") ?? string.Empty;

        Gen<string> buttonGen = Gen.Elements("primary", "secondary");

        return Prop.ForAll(buttonGen.ToArbitrary(), button =>
        {
            var body = button == "primary" ? primaryBody : secondaryBody;

            bool paddingOk = Regex.IsMatch(body, @"padding\s*:\s*14px\s+26px");
            bool radiusOk = Regex.IsMatch(body, @"border-radius\s*:\s*6px");
            bool accentOk = button == "primary"
                ? Regex.IsMatch(body, @"background\s*:\s*#1f1f1f")
                : Regex.IsMatch(body, @"border\s*:\s*1px\s+solid\s+#1f1f1f")
                    && Regex.IsMatch(body, @"color\s*:\s*#1f1f1f");

            return (paddingOk && radiusOk && accentOk)
                .ToProperty()
                .Label($"button={button}, padding={paddingOk}, radius={radiusOk}, accent={accentOk}");
        });
    }
}
