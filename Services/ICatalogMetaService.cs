using MaterDomus.Web.Models;

namespace MaterDomus.Web.Services;

/// <summary>
/// Contrato para o serviço que fornece os metadados da última curadoria do catálogo.
/// </summary>
public interface ICatalogMetaService
{
    /// <summary>
    /// Retorna os metadados da última curadoria, ou <c>null</c> se o arquivo
    /// estiver ausente, inválido ou inacessível.
    /// </summary>
    Task<CatalogMeta?> GetMetaAsync();
}
