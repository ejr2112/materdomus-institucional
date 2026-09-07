using System.Text;
using MaterDomus.Web.Models;

namespace MaterDomus.Web.Helpers;

/// <summary>
/// Gerador puro e determinístico do texto persuasivo (copywriting) exibido na
/// visão de detalhes de produto. Compõe o texto a partir dos campos do próprio
/// <see cref="Product"/> por segmentos condicionais, usando técnicas de copy
/// orientadas a benefício e apelo emocional, encerrando com um call-to-action
/// de compra.
///
/// Contrato (ver design da feature "product-detail-view"):
/// - Determinístico (Req 3.1): mesma entrada produz sempre a mesma saída;
///   sem <c>DateTime.Now</c>, <c>Random</c> ou dependência de
///   <c>CultureInfo.CurrentCulture</c>.
/// - Sem rede/I-O (Req 3.3): função pura, sem <c>HttpClient</c> nem side effects.
/// - Preço formatado por <see cref="ProductHelpers.FormatPrice"/> (pt-BR fixo,
///   "R$ 0,00") (Req 3.2).
/// - Retorna <c>null</c> quando o nome é nulo/vazio/só espaços (Req 4.5).
/// - Omite campos ausentes sem placeholder, token ou espaço reservado
///   (Req 4.1–4.3).
/// - Saída não-nula tem de 1 a 600 caracteres (Req 2.1).
/// - Última frase é sempre o CTA de incentivo à compra (Req 2.4).
/// </summary>
public static class ProductCopyGenerator
{
    /// <summary>Comprimento máximo permitido para a saída (Req 2.1).</summary>
    private const int MaxLength = 600;

    /// <summary>
    /// Gera o texto persuasivo determinístico a partir dos campos do produto.
    /// Retorna <c>null</c> quando não há base mínima (nome ausente/em branco),
    /// sinalizando ao componente que deve exibir conteúdo indisponível (Req 4.5).
    /// </summary>
    /// <param name="product">Produto de origem. Não deve ser nulo.</param>
    /// <returns>
    /// Texto persuasivo de 1 a 600 caracteres, ou <c>null</c> quando o nome é
    /// nulo/vazio/só espaços.
    /// </returns>
    public static string? Generate(Product product)
    {
        if (product is null) return null;

        // Req 4.5 / Req 4.4: sem nome válido não há base para o texto.
        if (string.IsNullOrWhiteSpace(product.Name)) return null;

        var name = product.Name.Trim();
        var category = product.Category;
        var description = product.Description;
        var price = product.Price;

        var hasCategory = !string.IsNullOrWhiteSpace(category);
        var hasDescription = !string.IsNullOrWhiteSpace(description);
        var hasPrice = price > 0m;

        // Segmentos condicionais montados como frases completas, cada uma incluída
        // somente quando seu campo é válido. Nenhum segmento omitido deixa
        // placeholder, token ou espaço reservado (Req 4.1–4.3).
        var sentences = new List<string>();

        // Abertura — usa o nome, com apelo emocional/benefício (Req 4.4, 2.2).
        if (hasCategory)
        {
            // Categoria incluída na própria abertura (Req 2.2, 4.2).
            sentences.Add(
                $"Conheça {name}, uma escolha especial em {category.Trim()} para transformar o seu dia a dia com mais praticidade e bem-estar.");
        }
        else
        {
            sentences.Add(
                $"Conheça {name}, uma escolha especial para transformar o seu dia a dia com mais praticidade e bem-estar.");
        }

        // Descrição completa, não truncada (Req 2.2, 2.3, 3.4).
        if (hasDescription)
        {
            sentences.Add(description.Trim());
        }

        // Preço formatado em pt-BR quando positivo (Req 2.2, 3.2, 4.3).
        if (hasPrice)
        {
            var formattedPrice = ProductHelpers.FormatPrice(price);
            sentences.Add(
                $"Tudo isso por apenas {formattedPrice}, um investimento que vale a pena.");
        }

        // CTA — sempre a última frase, distinta do restante (Req 2.4).
        sentences.Add("Garanta o seu agora e aproveite!");

        var result = string.Join(" ", sentences);

        // Garante o limite superior (Req 2.1). Ao truncar, preserva o CTA como
        // última frase para não violar Req 2.4.
        if (result.Length > MaxLength)
        {
            result = TrimToLimitPreservingCta(sentences);
        }

        return result;
    }

    /// <summary>
    /// Reconstrói o texto respeitando <see cref="MaxLength"/>, mantendo a abertura
    /// e o CTA (última frase) e descartando/encurtando os segmentos intermediários
    /// conforme necessário. Não introduz placeholders nem tokens.
    /// </summary>
    private static string TrimToLimitPreservingCta(IReadOnlyList<string> sentences)
    {
        var opening = sentences[0];
        var cta = sentences[^1];

        // Espaço disponível para os segmentos do meio, contando os separadores.
        // Formato final: opening + " " + [middle...] + " " + cta
        var middle = sentences.Skip(1).Take(sentences.Count - 2).ToList();

        var builder = new StringBuilder(opening);
        var closing = " " + cta;
        var budget = MaxLength - opening.Length - closing.Length;

        foreach (var segment in middle)
        {
            var candidate = " " + segment;
            if (candidate.Length <= budget)
            {
                builder.Append(candidate);
                budget -= candidate.Length;
            }
        }

        builder.Append(closing);

        var result = builder.ToString();

        // Salvaguarda defensiva: se ainda exceder (ex.: abertura muito longa por
        // um nome extenso), corta de forma dura mantendo o texto dentro do limite.
        if (result.Length > MaxLength)
        {
            result = result.Substring(0, MaxLength).TrimEnd();
        }

        return result;
    }
}
