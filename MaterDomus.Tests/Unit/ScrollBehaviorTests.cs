// Feature: menu-redesign

using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace MaterDomus.Tests.Unit;

/// <summary>
/// Testes de propriedade para a lógica de ScrollBehavior.
/// Modela em C# o comportamento definido em wwwroot/js/scrollBehavior.js.
/// </summary>
public class ScrollBehaviorTests
{
    // -------------------------------------------------------------------------
    // C# mirror of scrollBehavior.js logic
    // -------------------------------------------------------------------------

    private const int ScrollThreshold = 10;
    private const int MobileBreakpoint = 768;

    /// <summary>
    /// Determina se o handler de scroll deve adicionar a classe header--hidden.
    /// Mirrors the logic inside the scrollHandler closure in scrollBehavior.js.
    /// </summary>
    /// <param name="lastScrollY">Posição de scroll anterior.</param>
    /// <param name="currentScrollY">Posição de scroll atual.</param>
    /// <param name="innerWidth">Largura da viewport (window.innerWidth).</param>
    /// <returns>true se header--hidden deve ser adicionada; false caso contrário.</returns>
    private static bool ShouldAddHiddenClass(int lastScrollY, int currentScrollY, int innerWidth)
    {
        // Disable on mobile — never hide
        if (innerWidth <= MobileBreakpoint)
            return false;

        // Always show at top of page
        if (currentScrollY <= ScrollThreshold)
            return false;

        var delta = currentScrollY - lastScrollY;

        // Scrolling down by more than threshold: hide
        if (delta > ScrollThreshold)
            return true;

        // Scrolling up or small movement: do not hide
        return false;
    }

    // -------------------------------------------------------------------------
    // Property 3: Topo da página mantém header sempre visível
    // -------------------------------------------------------------------------

    /// <summary>
    /// Para qualquer evento de scroll com currentScrollY ≤ 10, independentemente da posição
    /// anterior (lastScrollY) ou da largura da viewport, o handler NUNCA deve adicionar
    /// a classe header--hidden — o header permanece sempre visível no topo da página.
    ///
    /// Validates: Requirements 2.5
    /// </summary>
    [Property(MaxTest = 1000)]
    public Property AtTopOfPage_HeaderIsAlwaysVisible()
    {
        // currentScrollY: 0..10 (at or near top)
        var currentScrollYGen = Gen.Choose(0, ScrollThreshold);

        // lastScrollY: any previous position 0..5000
        var lastScrollYGen = Gen.Choose(0, 5000);

        // innerWidth: any viewport width 100..2560
        var innerWidthGen = Gen.Choose(100, 2560);

        var gen = currentScrollYGen.SelectMany(currentScrollY =>
            lastScrollYGen.SelectMany(lastScrollY =>
                innerWidthGen.Select(innerWidth =>
                    (lastScrollY, currentScrollY, innerWidth)
                )
            )
        );

        return Prop.ForAll(gen.ToArbitrary(), tuple =>
        {
            var (lastScrollY, currentScrollY, innerWidth) = tuple;

            bool addedHidden = ShouldAddHiddenClass(lastScrollY, currentScrollY, innerWidth);

            return (!addedHidden)
                .ToProperty()
                .Label($"lastScrollY={lastScrollY}, currentScrollY={currentScrollY}, " +
                       $"innerWidth={innerWidth}: ShouldAddHiddenClass={addedHidden} (expected false)");
        });
    }
}
