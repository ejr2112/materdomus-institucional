namespace MaterDomus.Web.Models;

/// <summary>
/// Representa um produto exibido na vitrine, com link direto para compra na Amazon.
/// </summary>
/// <param name="Id">GUID ou slug único do produto.</param>
/// <param name="Name">Nome do produto (máx. 100 caracteres).</param>
/// <param name="Description">Descrição livre; a UI trunca a exibição em 120 chars.</param>
/// <param name="ImageUrl">URL relativa (wwwroot/images/) ou absoluta; vazio usa imagem placeholder.</param>
/// <param name="Category">Categoria do produto, ex: "Organização", "Cozinha", "Limpeza".</param>
/// <param name="Price">Preço de referência; formatado como "R$ X,XX" na UI.</param>
/// <param name="AmazonUrl">URL amazon.com.br para compra; vazio oculta o botão "Comprar na Amazon".</param>
/// <param name="VideoUrl">URL relativa (wwwroot/images/) ou absoluta de um vídeo do produto; vazio/omitido oculta o player.</param>
public record Product(
    string Id,
    string Name,
    string Description,
    string ImageUrl,
    string Category,
    decimal Price,
    string AmazonUrl,
    string VideoUrl = ""
);
