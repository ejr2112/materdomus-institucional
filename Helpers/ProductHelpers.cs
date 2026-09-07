using System.Globalization;

namespace MaterDomus.Web.Helpers;

/// <summary>
/// Funções puras para formatação e truncagem de dados de produto.
/// Compartilhadas entre ProductCard.razor e os testes unitários.
/// </summary>
public static class ProductHelpers
{
    /// <summary>
    /// Truncates the description to 120 characters followed by "..." if longer.
    /// Returns the original string unchanged if length is 120 or less.
    /// Returns null if the input is null.
    /// </summary>
    public static string? TruncateDescription(string? description)
    {
        if (description == null) return null;
        if (description.Length > 120)
            return description.Substring(0, 120) + "...";
        return description;
    }

    /// <summary>
    /// Formats a decimal price as currency using the pt-BR culture ("R$ X,XX").
    /// </summary>
    public static string FormatPrice(decimal price)
        => price.ToString("C", new CultureInfo("pt-BR"));
}
