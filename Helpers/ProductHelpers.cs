using System.Globalization;
using MaterDomus.Web.Models;
using MaterDomus.Web.Services;

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

    /// <summary>
    /// CTA "Comprar na Amazon" somente quando o item NÃO está marcado como
    /// "Em breve" e a <c>AmazonUrl</c> é canônica. Ao reestocar, basta
    /// <c>comingSoon: false</c> com URL válida.
    /// </summary>
    public static bool ShowAmazonCta(Product? product) =>
        product is not null
        && !product.ComingSoon
        && ProductCatalogService.IsValidAmazonUrl(product.AmazonUrl);

    /// <summary>
    /// Imagem do cartão/modal. URL vazia usa o placeholder — <c>onerror</c> no
    /// markup continua como rede de segurança se o arquivo falhar.
    /// </summary>
    public static string DisplayImageUrl(string? imageUrl) =>
        string.IsNullOrWhiteSpace(imageUrl)
            ? "images/placeholder-product.png"
            : imageUrl;
}
