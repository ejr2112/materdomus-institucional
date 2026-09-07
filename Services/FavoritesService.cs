namespace MaterDomus.Web.Services;

/// <summary>
/// Gerencia o conjunto de produtos favoritados durante a sessão atual.
/// O estado é mantido em memória e é resetado ao recarregar a página.
/// </summary>
public class FavoritesService
{
    private readonly HashSet<string> _favorites = new();

    /// <summary>Coleção somente leitura dos IDs de produtos favoritados.</summary>
    public IReadOnlyCollection<string> Favorites => _favorites;

    /// <summary>Número de produtos atualmente favoritados.</summary>
    public int Count => _favorites.Count;

    /// <summary>Disparado sempre que a lista de favoritos é alterada.</summary>
    public event Action? OnChanged;

    /// <summary>Retorna true se o produto com o ID informado está nos favoritos.</summary>
    public bool IsFavorite(string productId) => _favorites.Contains(productId);

    /// <summary>
    /// Alterna o estado de favorito do produto: adiciona se não estiver,
    /// remove se já estiver. Dispara <see cref="OnChanged"/> ao final.
    /// </summary>
    public void Toggle(string productId)
    {
        if (!_favorites.Remove(productId))
            _favorites.Add(productId);
        OnChanged?.Invoke();
    }
}
