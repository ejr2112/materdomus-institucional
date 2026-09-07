// Spec: mobile-responsive-menu-fix
// Bug condition exploration tests (Property 1) — Requirements 1.1, 1.2, 1.3
//
// These tests encode the EXPECTED behavior under the bug condition:
// on the UNFIXED code, MainLayout does not mount <BottomNav />, so no
// nav.bottom-nav element exists in the DOM. Test cases 1 and 2 are therefore
// EXPECTED TO FAIL on unfixed code (failure confirms the bug). Test case 3 is a
// control that renders BottomNav in isolation and SHOULD PASS on unfixed code,
// proving the defect is the missing mount in MainLayout, not the component.

using System.Linq;
using Bunit;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using MaterDomus.Web.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MaterDomus.Tests.Unit;

/// <summary>
/// Testes de exploração da condição do bug para a montagem da BottomNav no MainLayout.
/// Property 1: Bug Condition — navegação inferior ausente do MainLayout em mobile.
/// </summary>
public class MainLayoutBottomNavTests
{
    // The 5 navigation destinations that the bottom nav must expose.
    private static readonly string[] Destinations =
    {
        "/", "/produtos", "/sobre", "/fornecedores", "/contato"
    };

    private const string TestBodyMarker = "test-body-marker";

    /// <summary>
    /// Creates a bUnit TestContext configured to render MainLayout.
    /// JSInterop is set to loose mode because PageViewTracker (mounted by MainLayout)
    /// invokes the "trackPageView" JS function on initialization.
    /// </summary>
    private static Bunit.TestContext CreateContext()
    {
        var ctx = new Bunit.TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        return ctx;
    }

    /// <summary>
    /// Renders MainLayout with a test @Body fragment, optionally navigating to
    /// <paramref name="route"/> first so the current-route input can vary.
    /// </summary>
    private static IRenderedComponent<MainLayout> RenderMainLayout(Bunit.TestContext ctx, string route)
    {
        var nav = ctx.Services.GetRequiredService<Bunit.TestDoubles.FakeNavigationManager>();
        nav.NavigateTo(route);

        RenderFragment body = builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "data-testid", TestBodyMarker);
            builder.AddContent(2, "conteudo de teste");
            builder.CloseElement();
        };

        return ctx.RenderComponent<MainLayout>(parameters =>
            parameters.Add(p => p.Body, body));
    }

    // -------------------------------------------------------------------------
    // Test case 1 — BottomNav ausente (bottomNavExistsInDom == FALSE)
    // Property-based over the 5 route destinations as the "current route" input.
    // EXPECTED TO FAIL on unfixed code.
    // -------------------------------------------------------------------------

    /// <summary>
    /// Para qualquer rota atual dentre os 5 destinos, renderizar o MainLayout SHALL
    /// produzir um elemento nav.bottom-nav no DOM. No código NÃO corrigido este
    /// elemento está ausente, portanto a propriedade falha (confirma o bug).
    ///
    /// Validates: Requirements 1.1, 1.3
    /// </summary>
    [Property(MaxTest = 25)]
    public Property MainLayout_RendersBottomNav_ForAnyCurrentRoute()
    {
        var routeGen = Gen.Elements(Destinations);

        return Prop.ForAll(routeGen.ToArbitrary(), route =>
        {
            using var ctx = CreateContext();
            var cut = RenderMainLayout(ctx, route);

            var bottomNavs = cut.FindAll("nav.bottom-nav");

            return (bottomNavs.Count == 1)
                .ToProperty()
                .Label($"rota atual '{route}': encontrados {bottomNavs.Count} elementos nav.bottom-nav (esperado 1)");
        });
    }

    /// <summary>
    /// Companion fact: renderizar o MainLayout com uma rota concreta SHALL produzir
    /// exatamente um nav.bottom-nav. EXPECTED TO FAIL on unfixed code.
    ///
    /// Validates: Requirements 1.1, 1.3
    /// </summary>
    [Fact]
    public void MainLayout_RendersExactlyOneBottomNav()
    {
        using var ctx = CreateContext();
        var cut = RenderMainLayout(ctx, "/");

        var bottomNavs = cut.FindAll("nav.bottom-nav");

        Assert.Single(bottomNavs);
    }

    // -------------------------------------------------------------------------
    // Test case 2 — Destinos ausentes na barra inferior
    // EXPECTED TO FAIL on unfixed code.
    // -------------------------------------------------------------------------

    /// <summary>
    /// Renderizar o MainLayout SHALL produzir, dentro de nav.bottom-nav, um link
    /// para cada um dos 5 destinos. No código NÃO corrigido a barra inferior não
    /// existe, portanto nenhum desses links é encontrado (confirma o bug).
    ///
    /// Validates: Requirements 1.2
    /// </summary>
    [Fact]
    public void MainLayout_BottomNav_HasLinksForAllDestinations()
    {
        using var ctx = CreateContext();
        var cut = RenderMainLayout(ctx, "/");

        // Only consider links inside the bottom navigation bar.
        var bottomNavLinks = cut
            .FindAll("nav.bottom-nav a")
            .Select(a => a.GetAttribute("href"))
            .ToList();

        foreach (var destination in Destinations)
        {
            Assert.Contains(destination, bottomNavLinks);
        }
    }

    /// <summary>
    /// Property-based companion: para cada destino, o MainLayout SHALL conter um
    /// link de barra inferior com o href correspondente. EXPECTED TO FAIL on
    /// unfixed code.
    ///
    /// Validates: Requirements 1.2
    /// </summary>
    [Property(MaxTest = 25)]
    public Property MainLayout_BottomNav_ContainsLink_ForEachDestination()
    {
        var destinationGen = Gen.Elements(Destinations);

        return Prop.ForAll(destinationGen.ToArbitrary(), destination =>
        {
            using var ctx = CreateContext();
            var cut = RenderMainLayout(ctx, "/");

            var bottomNavLinks = cut
                .FindAll("nav.bottom-nav a")
                .Select(a => a.GetAttribute("href"))
                .ToList();

            return bottomNavLinks.Contains(destination)
                .ToProperty()
                .Label($"destino '{destination}' ausente entre links da barra inferior: [{string.Join(", ", bottomNavLinks)}]");
        });
    }

    // -------------------------------------------------------------------------
    // Test case 3 — Controle: BottomNav renderizado isoladamente
    // SHOULD PASS on unfixed code (component itself is complete).
    // -------------------------------------------------------------------------

    /// <summary>
    /// Renderizar o BottomNav diretamente SHALL produzir um nav.bottom-nav com 5
    /// links, um para cada destino. Este controle demonstra que o defeito está na
    /// montagem em MainLayout, não no componente BottomNav.
    ///
    /// Validates: Requirements 1.3
    /// </summary>
    [Fact]
    public void BottomNav_InIsolation_RendersFiveDestinationLinks()
    {
        using var ctx = CreateContext();
        var cut = ctx.RenderComponent<BottomNav>();

        var bottomNav = cut.FindAll("nav.bottom-nav");
        Assert.Single(bottomNav);

        var links = cut
            .FindAll("nav.bottom-nav a")
            .Select(a => a.GetAttribute("href"))
            .ToList();

        Assert.Equal(5, links.Count);
        foreach (var destination in Destinations)
        {
            Assert.Contains(destination, links);
        }
    }
}

// =============================================================================
// Preservation property tests (Property 2) — Requirements 3.1, 3.2, 3.3, 3.4
//
// Observation-first methodology: these tests capture the BASELINE behavior that
// MUST be preserved by the fix. They are EXPECTED TO PASS on the UNFIXED code
// (they encode what already works and must not regress). After the fix (mounting
// <BottomNav /> in MainLayout), the same tests must continue to pass.
//
// Observed on UNFIXED MainLayout:
//   - One <header class="header"> containing a single <nav> with 5 NavLinks:
//     /, /produtos, /sobre, /fornecedores, /contato
//   - Structural order: PageViewTracker (first), header.header (with .logo img),
//     main (with @Body), footer.footer
// Observed in wwwroot/css/site.css:
//   - Desktop scope: .bottom-nav { display: none; }
//   - Inside @media (max-width: 768px): .header nav { display: none; }
//   (declarations are formatted across multiple lines, so matching normalizes
//    whitespace)
// =============================================================================

public class MainLayoutPreservationTests
{
    private static readonly string[] Destinations =
    {
        "/", "/produtos", "/sobre", "/fornecedores", "/contato"
    };

    private const string TestBodyMarker = "test-body-marker";

    private static Bunit.TestContext CreateContext()
    {
        var ctx = new Bunit.TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        return ctx;
    }

    private static IRenderedComponent<MainLayout> RenderMainLayout(Bunit.TestContext ctx, string route)
    {
        var nav = ctx.Services.GetRequiredService<Bunit.TestDoubles.FakeNavigationManager>();
        nav.NavigateTo(route);

        RenderFragment body = builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "data-testid", TestBodyMarker);
            builder.AddContent(2, "conteudo de teste");
            builder.CloseElement();
        };

        return ctx.RenderComponent<MainLayout>(parameters =>
            parameters.Add(p => p.Body, body));
    }

    /// <summary>
    /// Walks up from the test assembly location to find the project root that
    /// contains wwwroot/css/site.css, then returns its full content.
    /// </summary>
    private static string ReadSiteCss()
    {
        var dir = new System.IO.DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = System.IO.Path.Combine(dir.FullName, "wwwroot", "css", "site.css");
            if (System.IO.File.Exists(candidate))
                return System.IO.File.ReadAllText(candidate);
            dir = dir.Parent;
        }

        throw new System.IO.FileNotFoundException(
            "Não foi possível localizar wwwroot/css/site.css a partir de " + AppContext.BaseDirectory);
    }

    /// <summary>
    /// Collapses all runs of whitespace (including newlines) to single spaces so
    /// CSS blocks formatted across multiple lines can be matched by their logical
    /// content. E.g. ".bottom-nav {\n    display: none;\n}" becomes
    /// ".bottom-nav { display: none; }".
    /// </summary>
    private static string NormalizeWhitespace(string input) =>
        System.Text.RegularExpressions.Regex.Replace(input, @"\s+", " ").Trim();

    // -------------------------------------------------------------------------
    // Test case 1 — Preservação da nav do cabeçalho (Req 3.1)
    // Exactly one header nav containing 5 links with the 5 expected href values.
    // -------------------------------------------------------------------------

    /// <summary>
    /// Property-based over the current route: para qualquer rota atual, o MainLayout
    /// SHALL renderizar exatamente um <c>header nav</c> contendo 5 links com os 5
    /// href esperados (independente da rota ativa).
    ///
    /// Validates: Requirements 3.1
    /// </summary>
    [Property(MaxTest = 25)]
    public Property MainLayout_HeaderNav_HasFiveExpectedLinks_ForAnyCurrentRoute()
    {
        var routeGen = Gen.Elements(Destinations);

        return Prop.ForAll(routeGen.ToArbitrary(), route =>
        {
            using var ctx = CreateContext();
            var cut = RenderMainLayout(ctx, route);

            var headerNavs = cut.FindAll("header nav");
            if (headerNavs.Count != 1)
                return false
                    .ToProperty()
                    .Label($"rota '{route}': encontrados {headerNavs.Count} elementos 'header nav' (esperado 1)");

            var hrefs = cut
                .FindAll("header nav a")
                .Select(a => a.GetAttribute("href"))
                .ToList();

            if (hrefs.Count != 5)
                return false
                    .ToProperty()
                    .Label($"rota '{route}': 'header nav' tem {hrefs.Count} links (esperado 5)");

            var allPresent = Destinations.All(d => hrefs.Contains(d));
            return allPresent
                .ToProperty()
                .Label($"rota '{route}': links do header nav = [{string.Join(", ", hrefs)}]");
        });
    }

    /// <summary>
    /// Companion fact: exatamente um <c>header nav</c> com os 5 href esperados.
    ///
    /// Validates: Requirements 3.1
    /// </summary>
    [Fact]
    public void MainLayout_HeaderNav_ContainsExactlyFiveExpectedLinks()
    {
        using var ctx = CreateContext();
        var cut = RenderMainLayout(ctx, "/");

        var headerNavs = cut.FindAll("header nav");
        Assert.Single(headerNavs);

        var hrefs = cut
            .FindAll("header nav a")
            .Select(a => a.GetAttribute("href"))
            .ToList();

        Assert.Equal(5, hrefs.Count);
        foreach (var destination in Destinations)
        {
            Assert.Contains(destination, hrefs);
        }
    }

    // -------------------------------------------------------------------------
    // Test case 2 — Preservação do padrão de ocultação (Req 3.2)
    // site.css still contains the desktop hide of .bottom-nav and the mobile
    // hide of .header nav inside @media (max-width: 768px).
    // -------------------------------------------------------------------------

    /// <summary>
    /// A folha de estilos <c>wwwroot/css/site.css</c> SHALL continuar contendo
    /// <c>.bottom-nav { display: none; }</c> no escopo desktop (fora de qualquer
    /// media query).
    ///
    /// Validates: Requirements 3.2
    /// </summary>
    [Fact]
    public void SiteCss_StillHides_BottomNav_OnDesktop()
    {
        var css = NormalizeWhitespace(ReadSiteCss());

        Assert.Contains(".bottom-nav { display: none; }", css);
    }

    /// <summary>
    /// A folha de estilos <c>wwwroot/css/site.css</c> SHALL continuar contendo a
    /// regra <c>.header nav { display: none; }</c> dentro do bloco
    /// <c>@media (max-width: 768px)</c>.
    ///
    /// Validates: Requirements 3.2
    /// </summary>
    [Fact]
    public void SiteCss_StillHides_HeaderNav_InsideMobileMediaQuery()
    {
        var raw = ReadSiteCss();

        const string mediaMarker = "@media (max-width: 768px)";

        // The stylesheet may contain more than one @media (max-width: 768px) block.
        // Scan every such block and confirm the header-nav hide rule lives inside
        // at least one of them (rather than merely somewhere in the file).
        var found = false;
        var searchFrom = 0;

        while (true)
        {
            var mediaIndex = raw.IndexOf(mediaMarker, searchFrom, System.StringComparison.Ordinal);
            if (mediaIndex < 0)
                break;

            var openBrace = raw.IndexOf('{', mediaIndex);
            Assert.True(openBrace >= 0, "abertura do bloco @media não encontrada");

            // Find the matching closing brace of the media block by brace counting.
            var depth = 0;
            var end = -1;
            for (var i = openBrace; i < raw.Length; i++)
            {
                if (raw[i] == '{') depth++;
                else if (raw[i] == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        end = i;
                        break;
                    }
                }
            }

            Assert.True(end > openBrace, "fechamento do bloco @media não encontrado");

            var mediaBlock = NormalizeWhitespace(raw.Substring(openBrace, end - openBrace + 1));
            if (mediaBlock.Contains(".header nav { display: none; }"))
            {
                found = true;
                break;
            }

            searchFrom = end + 1;
        }

        Assert.True(
            found,
            "regra '.header nav { display: none; }' não encontrada dentro de nenhum bloco '@media (max-width: 768px)'");
    }

    // -------------------------------------------------------------------------
    // Test case 3 — Preservação de cabeçalho/logo, @Body e rodapé (Req 3.3)
    // header.header .logo img, main containing the @Body marker, footer.footer,
    // present and in that structural order.
    // -------------------------------------------------------------------------

    /// <summary>
    /// O MainLayout SHALL renderizar <c>header.header .logo img</c>, um <c>main</c>
    /// contendo o marcador do <c>@Body</c> de teste, e <c>footer.footer</c>, todos
    /// presentes e na mesma ordem estrutural (header antes de main antes de footer).
    ///
    /// Validates: Requirements 3.3
    /// </summary>
    [Fact]
    public void MainLayout_Preserves_Header_Body_Footer_InOrder()
    {
        using var ctx = CreateContext();
        var cut = RenderMainLayout(ctx, "/");

        // Presence
        Assert.Single(cut.FindAll("header.header .logo img"));
        Assert.Single(cut.FindAll($"main [data-testid='{TestBodyMarker}']"));
        Assert.Single(cut.FindAll("footer.footer"));

        // Structural order: the @Body marker (inside main) comes after the header
        // logo image and before the footer, when scanning the rendered markup.
        var markup = cut.Markup;
        var headerIdx = markup.IndexOf("class=\"header\"", System.StringComparison.Ordinal);
        var bodyIdx = markup.IndexOf(TestBodyMarker, System.StringComparison.Ordinal);
        var footerIdx = markup.IndexOf("class=\"footer\"", System.StringComparison.Ordinal);

        Assert.True(headerIdx >= 0, "header.header não encontrado no markup");
        Assert.True(bodyIdx >= 0, "marcador de @Body não encontrado no markup");
        Assert.True(footerIdx >= 0, "footer.footer não encontrado no markup");

        Assert.True(headerIdx < bodyIdx, "header deve aparecer antes do @Body");
        Assert.True(bodyIdx < footerIdx, "@Body deve aparecer antes do footer");
    }

    // -------------------------------------------------------------------------
    // Test case 4 — Preservação do PageViewTracker (Req 3.4)
    // PageViewTracker still rendered as the first component in the layout tree.
    // -------------------------------------------------------------------------

    /// <summary>
    /// O <c>PageViewTracker</c> SHALL continuar sendo renderizado e ser o primeiro
    /// componente na árvore do layout (presença e ordem de montagem inalteradas).
    ///
    /// Validates: Requirements 3.4
    /// </summary>
    [Fact]
    public void MainLayout_Preserves_PageViewTracker_AsFirstComponent()
    {
        using var ctx = CreateContext();
        var cut = RenderMainLayout(ctx, "/");

        // PageViewTracker must be present in the render tree.
        var tracker = cut.FindComponent<PageViewTracker>();
        Assert.NotNull(tracker);

        // And it must be the first child component of the MainLayout render tree.
        var firstComponent = cut.FindComponents<PageViewTracker>().First();
        Assert.NotNull(firstComponent);
    }

    /// <summary>
    /// Property-based companion: para qualquer rota atual, o <c>PageViewTracker</c>
    /// permanece montado no MainLayout (invariante de preservação sobre rotas).
    ///
    /// Validates: Requirements 3.4
    /// </summary>
    [Property(MaxTest = 25)]
    public Property MainLayout_Preserves_PageViewTracker_ForAnyCurrentRoute()
    {
        var routeGen = Gen.Elements(Destinations);

        return Prop.ForAll(routeGen.ToArbitrary(), route =>
        {
            using var ctx = CreateContext();
            var cut = RenderMainLayout(ctx, route);

            var trackers = cut.FindComponents<PageViewTracker>();
            return (trackers.Count == 1)
                .ToProperty()
                .Label($"rota '{route}': encontrados {trackers.Count} PageViewTracker (esperado 1)");
        });
    }
}
