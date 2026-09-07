// Feature: amazon-product-showcase

using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using MaterDomus.Web.Helpers;

namespace MaterDomus.Tests.Unit;

/// <summary>
/// Testes de propriedade para a função de truncagem de descrição.
/// Valida: Requisito 1.4
/// </summary>
public class DescriptionTruncationTests
{
    // Feature: amazon-product-showcase, Property 2: Truncagem de descrição
    /// <summary>
    /// Para qualquer string de descrição:
    /// (a) se comprimento > 120, retorna os primeiros 120 chars + "..."
    /// (b) se comprimento &lt;= 120, retorna a string original sem modificação.
    ///
    /// Validates: Requirement 1.4
    /// </summary>
    [Property(MaxTest = 100)]
    public Property TruncateDescription_CorrectnessForAnyString()
    {
        return Prop.ForAll(ArbMap.Default.ArbFor<string>(), description =>
        {
            var result = ProductHelpers.TruncateDescription(description);

            if (description == null)
            {
                return (result == null)
                    .ToProperty()
                    .Label("Null input should return null");
            }

            if (description.Length > 120)
            {
                var expected = description.Substring(0, 120) + "...";
                return (result == expected)
                    .ToProperty()
                    .Label($"Length {description.Length}: expected truncated+ellipsis");
            }
            else
            {
                return (result == description)
                    .ToProperty()
                    .Label($"Length {description.Length}: expected unchanged");
            }
        });
    }
}
