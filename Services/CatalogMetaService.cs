using System.Net.Http.Json;
using MaterDomus.Web.Models;

namespace MaterDomus.Web.Services;

/// <summary>
/// Implementação de <see cref="ICatalogMetaService"/> que carrega os metadados
/// de curadoria a partir de <c>wwwroot/data/catalog-meta.json</c>.
/// Retorna <c>null</c> em caso de arquivo ausente, JSON inválido, campos
/// obrigatórios nulos/vazios ou <c>productCount</c> negativo (Req 4.5).
/// </summary>
public class CatalogMetaService : ICatalogMetaService
{
    private readonly HttpClient _http;

    public CatalogMetaService(HttpClient http)
    {
        _http = http;
    }

    /// <inheritdoc />
    public async Task<CatalogMeta?> GetMetaAsync()
    {
        try
        {
            var meta = await _http.GetFromJsonAsync<CatalogMeta>("data/catalog-meta.json");

            // Req 4.2 / 4.3 / 4.4: campos obrigatórios não-vazios e productCount >= 0
            if (meta is null                          ||
                string.IsNullOrEmpty(meta.CuratedAt)  ||
                string.IsNullOrEmpty(meta.SourceUrl)  ||
                meta.ProductCount < 0)
                return null;

            return meta;
        }
        catch
        {
            // Req 4.5: qualquer exceção (404, rede, JSON inválido) → retornar null
            return null;
        }
    }
}
