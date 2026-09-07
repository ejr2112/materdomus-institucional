// Spec: menu-redesign

using Bunit;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using MaterDomus.Web.Shared;
using Xunit;

namespace MaterDomus.Tests.Unit;

/// <summary>
/// Testes de propriedade para o componente BottomNav.razor.
/// Valida a estrutura SVG + label de cada item de navegação.
/// </summary>
public class BottomNavRenderTests
{
    // -------------------------------------------------------------------------
    // Property 4: BottomNav items têm estrutura SVG + label
    // -------------------------------------------------------------------------

    /// <summary>
    /// Para qualquer item da BottomNav (dos 5 destinos de navegação), o HTML
    /// renderizado contém um SVG com aria-hidden="true" e dimensões 20×20,
    /// seguido de um &lt;span&gt; com rótulo textual não-vazio.
    ///
    /// Validates: Requirements 3.3, 4.5
    /// </summary>
    [Property(MaxTest = 50)]
    public Property BottomNav_EachItem_HasSvgWithAriaHiddenAndLabel()
    {
        // Generate random indexes 0..4 to verify any of the 5 items
        var indexGen = Gen.Choose(0, 4);

        return Prop.ForAll(indexGen.ToArbitrary(), index =>
        {
            using var ctx = new Bunit.TestContext();
            var cut = ctx.RenderComponent<BottomNav>();

            var links = cut.FindAll("a");

            // There must be exactly 5 NavLinks
            if (links.Count != 5)
                return false
                    .ToProperty()
                    .Label($"Expected 5 NavLinks, found {links.Count}");

            var link = links[index];

            // Each link must contain an SVG with aria-hidden="true" and 20×20 dimensions
            var svg = link.QuerySelector("svg");
            if (svg is null)
                return false
                    .ToProperty()
                    .Label($"Item [{index}]: no <svg> found");

            var ariaHidden = svg.GetAttribute("aria-hidden");
            if (ariaHidden != "true")
                return false
                    .ToProperty()
                    .Label($"Item [{index}]: svg aria-hidden='{ariaHidden}', expected 'true'");

            var width = svg.GetAttribute("width");
            if (width != "20")
                return false
                    .ToProperty()
                    .Label($"Item [{index}]: svg width='{width}', expected '20'");

            var height = svg.GetAttribute("height");
            if (height != "20")
                return false
                    .ToProperty()
                    .Label($"Item [{index}]: svg height='{height}', expected '20'");

            // Each link must contain a <span> with non-empty text
            var span = link.QuerySelector("span");
            if (span is null)
                return false
                    .ToProperty()
                    .Label($"Item [{index}]: no <span> found");

            if (string.IsNullOrWhiteSpace(span.TextContent))
                return false
                    .ToProperty()
                    .Label($"Item [{index}]: span text is empty or whitespace");

            return true
                .ToProperty()
                .Label($"Item [{index}]: svg(aria-hidden={ariaHidden}, {width}x{height}) span='{span.TextContent}'");
        });
    }

    // -------------------------------------------------------------------------
    // Fact-based companion: verifies all 5 items exhaustively in a single pass
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifica de forma exaustiva que todos os 5 itens da BottomNav possuem
    /// a estrutura SVG (aria-hidden="true", width="20", height="20") + span com label.
    ///
    /// Validates: Requirements 3.3, 4.5
    /// </summary>
    [Fact]
    public void BottomNav_AllItems_HaveSvgAndLabel()
    {
        using var ctx = new Bunit.TestContext();
        var cut = ctx.RenderComponent<BottomNav>();

        var links = cut.FindAll("a");
        Assert.Equal(5, links.Count);

        foreach (var link in links)
        {
            var svg = link.QuerySelector("svg");
            Assert.NotNull(svg);
            Assert.Equal("true", svg!.GetAttribute("aria-hidden"));
            Assert.Equal("20", svg.GetAttribute("width"));
            Assert.Equal("20", svg.GetAttribute("height"));

            var span = link.QuerySelector("span");
            Assert.NotNull(span);
            Assert.False(string.IsNullOrWhiteSpace(span!.TextContent));
        }
    }
}
