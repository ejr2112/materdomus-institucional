namespace MaterDomus.Web.Models;

/// <summary>
/// Representa um produto exibido na vitrine, com link direto para compra na Amazon
/// quando o item está disponível.
/// </summary>
/// <param name="Id">GUID ou slug único do produto.</param>
/// <param name="Name">Nome do produto (máx. 100 caracteres).</param>
/// <param name="Description">Descrição livre; a UI trunca a exibição em 120 chars.</param>
/// <param name="ImageUrl">URL relativa (wwwroot/images/) ou absoluta; vazio usa imagem placeholder.</param>
/// <param name="Category">Categoria do produto, ex: "Organização", "Cozinha", "Limpeza".</param>
/// <param name="Price">Preço de referência; formatado como "R$ X,XX" na UI.</param>
/// <param name="AmazonUrl">
/// URL amazon.com.br canônica para compra. Pode ficar vazia enquanto o ASIN
/// não for confirmado. URL inválida (não-vazia e fora do padrão) faz o
/// catálogo descartar o registro.
/// </param>
/// <param name="ComingSoon">
/// Quando <c>true</c>, o produto permanece visível com o selo "Em breve" e
/// <b>sem</b> o botão "Comprar na Amazon". Ao reestocar: defina
/// <c>comingSoon</c> como <c>false</c> e preencha <c>amazonUrl</c> com o ASIN
/// confirmado da loja Mater Domus.
/// </param>
public record Product(
    string Id,
    string Name,
    string Description,
    string ImageUrl,
    string Category,
    decimal Price,
    string AmazonUrl,
    bool ComingSoon = false
);
