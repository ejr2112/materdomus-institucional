// Feature: amazon-product-showcase

using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using MaterDomus.Web.Helpers;

namespace MaterDomus.Tests.Unit;

/// <summary>
/// Testes de propriedade para a função de formatação de preço.
/// Valida: Requisito 1.3
/// </summary>
public class PriceFormattingTests
{
    // Feature: amazon-product-showcase, Property 3: Formatação de preço
    /// <summary>
    /// Para qualquer valor decimal não-negativo de preço, a função de formatação
    /// deve produzir uma string que contém "R$" (cultura pt-BR).
    ///
    /// Validates: Requirement 1.3
    /// </summary>
    [Property(MaxTest = 100)]
    public Property FormatPrice_StartsWithReaisSymbol_ForNonNegativeValues()
    {
        // Generate non-negative decimals (0.00 to 99999.99)
        var gen = Gen.Choose(0, 9999999).Select(i => (decimal)i / 100m);

        return Prop.ForAll(gen.ToArbitrary(), price =>
        {
            var result = ProductHelpers.FormatPrice(price);

            return result.Contains("R$")
                .ToProperty()
                .Label($"Price {price} formatted as '{result}' should contain 'R$'");
        });
    }

    // Feature: amazon-product-showcase, Property 3: Formatação de preço — separador decimal
    /// <summary>
    /// Para qualquer preço com centavos, o formato deve usar vírgula como separador decimal (pt-BR).
    ///
    /// Validates: Requirement 1.3
    /// </summary>
    [Property(MaxTest = 100)]
    public Property FormatPrice_UsesCommaAsDecimalSeparator()
    {
        // Generate values with meaningful cents (x.01 to x.99)
        var gen = Gen.Choose(0, 999)
            .SelectMany(intPart =>
                Gen.Choose(1, 99)
                    .Select(cents => (decimal)intPart + (decimal)cents / 100m)
            );

        return Prop.ForAll(gen.ToArbitrary(), price =>
        {
            var result = ProductHelpers.FormatPrice(price);

            return result.Contains(",")
                .ToProperty()
                .Label($"Price {price} formatted as '{result}' should use ',' as decimal separator");
        });
    }
}
