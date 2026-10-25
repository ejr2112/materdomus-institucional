using System.Globalization;
using System.Text;
using System.Text.Json;

namespace MaterDomus.VitrineSeoGen;

/// <summary>
/// /llms.txt e /llms-full.txt em português. Fatos da empresa vêm do site
/// e do catálogo; não há declaração de loja oficial ou autorizada.
/// </summary>
public static class LlmsTxt
{
    public const string AmazonStoreUrl =
        "https://www.amazon.com.br/s?me=A20TN3HCSY6KZV&marketplaceID=A2Q3Y263D00KWC";

    public static void WriteAll(string repoRoot, string productsJson, Encoding encoding)
    {
        var (brief, full) = Build(productsJson);
        File.WriteAllText(Path.Combine(repoRoot, "wwwroot", "llms.txt"), brief, encoding);
        File.WriteAllText(Path.Combine(repoRoot, "wwwroot", "llms-full.txt"), full, encoding);
    }

    public static (string Brief, string Full) Build(string productsJson)
    {
        var products = ReadProducts(productsJson);
        var categories = products
            .Select(p => p.Category)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var brief = new StringBuilder();
        brief.AppendLine("# Mater Domus");
        brief.AppendLine();
        brief.AppendLine("> A MATER DOMUS LTDA (CNPJ 64.258.511/0001-90, São Paulo/SP) revende utilidades domésticas das marcas Ou e Linha Flow, principalmente pela Amazon Brasil (FBA). O site https://www.materdomus.com.br/ é uma vitrine. Não é a empresa do domínio materdomus.com e não é uma loja de informática.");
        brief.AppendLine();
        brief.AppendLine("## Quem é");
        brief.AppendLine();
        brief.AppendLine("- Razão social: MATER DOMUS LTDA");
        brief.AppendLine("- Marca: Mater Domus");
        brief.AppendLine("- CNPJ: 64.258.511/0001-90");
        brief.AppendLine("- Cidade: São Paulo/SP, Brasil");
        brief.AppendLine("- Site: https://www.materdomus.com.br/");
        brief.AppendLine("- E-mail: contato@materdomus.com.br");
        brief.AppendLine("- WhatsApp: https://wa.me/5519993491775");
        brief.AppendLine("- A Mater Domus não fabrica os produtos.");
        brief.AppendLine();
        brief.AppendLine("## Marcas revendidas");
        brief.AppendLine();
        brief.AppendLine("Ou e Linha Flow são marcas do fabricante Martiplast (Caxias do Sul/RS), que também vende direto em https://ou.com.br/. A Mater Domus revende esses produtos e não é a fabricante.");
        brief.AppendLine();
        brief.AppendLine("## Como comprar");
        brief.AppendLine();
        brief.AppendLine("O site é uma vitrine. Quando o produto está disponível, o botão Comprar na Amazon abre o anúncio na Amazon Brasil, na loja da Mater Domus, com envio pela Amazon (FBA). Pagamento, frete, entrega, troca e devolução dessa compra são tratados pela Amazon Brasil. Itens marcados como Em breve ainda não têm link de compra. A Mater Domus não calcula frete neste site.");
        brief.AppendLine();
        brief.AppendLine($"Loja na Amazon Brasil: {AmazonStoreUrl}");
        brief.AppendLine();
        brief.AppendLine("## Categorias");
        brief.AppendLine();
        brief.AppendLine(categories.Count == 0
            ? "Utilidades domésticas e organização para o lar."
            : string.Join(", ", categories) + ".");
        brief.AppendLine();
        brief.AppendLine("## Páginas");
        brief.AppendLine();
        brief.AppendLine("- [Início](https://www.materdomus.com.br/): apresentação da Mater Domus e perguntas frequentes.");
        brief.AppendLine("- [Vitrine](https://www.materdomus.com.br/produtos): catálogo Ou e Linha Flow, com link de compra na Amazon quando o item está disponível.");
        brief.AppendLine("- [Sobre](https://www.materdomus.com.br/sobre): razão social, CNPJ, cidade e relação com as marcas.");
        brief.AppendLine("- [Fornecedores](https://www.materdomus.com.br/fornecedores): como apresentar um portfólio.");
        brief.AppendLine("- [Contato](https://www.materdomus.com.br/contato): e-mail, WhatsApp e formulário para fornecedores e parceiros.");
        brief.AppendLine("- [Frete e trocas](https://www.materdomus.com.br/frete-e-trocas): frete, troca e devolução ficam com a Amazon Brasil.");
        brief.AppendLine("- [Política de Privacidade](https://www.materdomus.com.br/privacidade): dados pessoais, cookies e LGPD.");
        brief.AppendLine("- [Perguntas frequentes](https://www.materdomus.com.br/#perguntas-frequentes): onde comprar, frete e trocas, origem das marcas.");
        brief.AppendLine("- [Texto completo](https://www.materdomus.com.br/llms-full.txt): esta ficha com a lista de produtos.");
        brief.AppendLine();

        var full = new StringBuilder(brief.ToString());
        full.AppendLine("## Produtos");
        full.AppendLine();
        foreach (var product in products)
        {
            full.Append("- ").Append(product.Name);
            if (!string.IsNullOrWhiteSpace(product.Category))
                full.Append(" (").Append(product.Category).Append(')');
            var pagePath = VitrineSeo.TryProductPublicPath(product.Id);
            if (pagePath is not null)
                full.Append(". Página: https://www.materdomus.com.br").Append(pagePath);
            full.Append(". Preço de vitrine: ").Append(FormatPrice(product.Price)).Append(". ");
            if (product.ComingSoon || string.IsNullOrWhiteSpace(product.AmazonUrl))
                full.AppendLine("Em breve, sem link de compra.");
            else
                full.Append("À venda na Amazon Brasil: ").AppendLine(product.AmazonUrl);
        }

        full.AppendLine();
        full.AppendLine("Marca dos produtos listados: Ou. Linha: Linha Flow. Fabricante das marcas: Martiplast (Caxias do Sul/RS). Revendedora: MATER DOMUS LTDA (São Paulo/SP).");
        full.AppendLine();

        return (brief.ToString(), full.ToString());
    }

    private static string FormatPrice(decimal price) =>
        price.ToString("C", CultureInfo.GetCultureInfo("pt-BR"));

    private static List<CatalogRow> ReadProducts(string productsJson)
    {
        using var doc = JsonDocument.Parse(productsJson);
        var rows = new List<CatalogRow>();
        foreach (var node in doc.RootElement.EnumerateArray())
        {
            var name = ReadString(node, "name");
            if (string.IsNullOrWhiteSpace(name))
                continue;

            rows.Add(new CatalogRow(
                ReadString(node, "id").Trim(),
                name.Trim(),
                ReadString(node, "category").Trim(),
                ReadPrice(node),
                ReadString(node, "amazonUrl").Trim(),
                ReadBool(node, "comingSoon")));
        }

        return rows;
    }

    private static string ReadString(JsonElement node, string name)
    {
        if (!node.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.String)
            return "";

        return value.GetString() ?? "";
    }

    private static bool ReadBool(JsonElement node, string name)
    {
        return node.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.True;
    }

    private static decimal ReadPrice(JsonElement node)
    {
        if (!node.TryGetProperty("price", out var value))
            return 0m;

        if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var price))
            return price;

        if (value.ValueKind == JsonValueKind.String &&
            decimal.TryParse(value.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out price))
            return price;

        return 0m;
    }

    private sealed record CatalogRow(string Id, string Name, string Category, decimal Price, string AmazonUrl, bool ComingSoon);
}
