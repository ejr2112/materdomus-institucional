using System.Text.Json.Serialization;

namespace MaterDomus.Web.Models;

/// <summary>
/// Metadados da última curadoria do catálogo de produtos.
/// Carregado de <c>wwwroot/data/catalog-meta.json</c> pelo <c>CatalogMetaService</c>.
/// </summary>
/// <param name="CuratedAt">Data da curadoria no formato ISO 8601, ex: "2025-01-15".</param>
/// <param name="SourceUrl">URL da Vitrine Amazon usada como fonte da curadoria.</param>
/// <param name="ProductCount">Número de produtos ativos no products.json no momento da curadoria.</param>
public record CatalogMeta(
    [property: JsonPropertyName("curatedAt")]   string CuratedAt,
    [property: JsonPropertyName("sourceUrl")]   string SourceUrl,
    [property: JsonPropertyName("productCount")] int ProductCount
);
