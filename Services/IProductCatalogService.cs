using MaterDomus.Web.Models;

namespace MaterDomus.Web.Services;

/// <summary>
/// Contrato para o serviço que fornece o catálogo de produtos da vitrine.
/// </summary>
public interface IProductCatalogService
{
    /// <summary>
    /// Retorna a lista completa de produtos disponíveis no catálogo.
    /// </summary>
    Task<IReadOnlyList<Product>> GetProductsAsync();
}
