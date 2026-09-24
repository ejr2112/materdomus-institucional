using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MaterDomus.VitrineSeoGen;

/// <summary>
/// Gera o JSON-LD (ItemList de Product) e o HTML inicial de /produtos
/// a partir de wwwroot/data/products.json e wwwroot/index.html.
/// A compra continua na Amazon: a Offer.url de item comprável é a amazonUrl.
/// </summary>
public static class VitrineSeo
{
    public const string Origin = "https://www.materdomus.com.br";
    public const string Title = "Vitrine Ou e Linha Flow | Mater Domus";
    public const string Description =
        "Conheça os produtos Mater Domus da Linha Flow e Ou, disponíveis na Amazon ou em breve. Utilidades domésticas e organização para o lar.";
    public const string PageUrl = Origin + "/produtos";

    /// <summary>
    /// Mesma regra de <c>ProductCatalogService.IsValidAmazonUrl</c>.
    /// O grupo 1 captura o ASIN; a URL inteira continua no mesmo formato.
    /// </summary>
    internal const string AmazonUrlPattern =
        @"^https://www\.amazon\.com\.br/dp/([A-Z0-9]{10})(\?m=[A-Z0-9]+)?$";

    private static readonly Regex AmazonUrlRegex = new(AmazonUrlPattern, RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Sufixo de 10 caracteres no id, com ao menos um dígito (ASIN já gravado no slug).
    /// Não inventa GTIN.
    /// </summary>
    private static readonly Regex IdAsinRegex = new(
        @"(?:^|-)([A-Za-z0-9]{10})$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public sealed record Artifacts(
        string ItemListJson,
        string MetaJson,
        string ProdutosHtml,
        string OgImageAbsoluteUrl);

    public static int Write(string[] args)
    {
        if (args.Length != 1 || string.IsNullOrWhiteSpace(args[0]))
        {
            Console.Error.WriteLine("Uso: VitrineSeoGen <raiz-do-repositorio>");
            return 1;
        }

        try
        {
            Write(args[0]);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    public static void Write(string repoRoot)
    {
        var productsPath = Path.Combine(repoRoot, "wwwroot", "data", "products.json");
        var indexPath = Path.Combine(repoRoot, "wwwroot", "index.html");
        var artifacts = Build(File.ReadAllText(productsPath), File.ReadAllText(indexPath));
        var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        File.WriteAllText(
            Path.Combine(repoRoot, "wwwroot", "data", "vitrine-itemlist.json"),
            artifacts.ItemListJson + "\n",
            utf8);
        File.WriteAllText(
            Path.Combine(repoRoot, "wwwroot", "data", "vitrine-meta.json"),
            artifacts.MetaJson + "\n",
            utf8);
        File.WriteAllText(
            Path.Combine(repoRoot, "wwwroot", "produtos.html"),
            artifacts.ProdutosHtml,
            utf8);
    }

    public static string BuildItemListJson(string productsJson) =>
        Build(productsJson, IndexShell).ItemListJson;

    public static Artifacts Build(string productsJson, string indexHtml)
    {
        var products = SelectProducts(productsJson);
        var og = SelectOgImage(products);
        var itemList = Serialize(BuildItemList(products));
        var meta = Serialize(new Dictionary<string, object>
        {
            ["title"] = Title,
            ["description"] = Description,
            ["url"] = PageUrl,
            ["image"] = og.ImageUrl,
            ["imageAlt"] = og.ImageAlt
        });

        return new Artifacts(itemList, meta, BuildProdutosHtml(indexHtml, itemList, og), og.ImageUrl);
    }

    private static string IndexShell =>
        """
        <!DOCTYPE html>
        <html><head>
        <title>placeholder</title>
        <meta name="description" content="placeholder" />
        <link rel="canonical" href="https://www.materdomus.com.br/" />
        <meta property="og:title" content="placeholder" />
        <meta property="og:description" content="placeholder" />
        <meta property="og:url" content="https://www.materdomus.com.br/" />
        <meta property="og:image" content="https://www.materdomus.com.br/images/logo.png" />
        <meta property="og:image:alt" content="placeholder" />
        <meta name="twitter:title" content="placeholder" />
        <meta name="twitter:description" content="placeholder" />
        <meta name="twitter:image" content="https://www.materdomus.com.br/images/logo.png" />
        <meta name="twitter:image:alt" content="placeholder" />
        <script src="js/seo.js"></script>
        </head><body></body></html>
        """;

    private sealed record CatalogProduct(
        string Id,
        string Name,
        string Description,
        string ImageUrl,
        string Category,
        decimal Price,
        string AmazonUrl,
        bool ComingSoon,
        string? Asin,
        string? Gtin);

    private sealed record OgImage(string ImageUrl, string ImageAlt);

    private static List<CatalogProduct> SelectProducts(string productsJson)
    {
        using var doc = JsonDocument.Parse(productsJson);
        if (doc.RootElement.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("products.json precisa ser um array.");

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var products = new List<CatalogProduct>();

        foreach (var node in doc.RootElement.EnumerateArray())
        {
            var id = ReadString(node, "id");
            var name = ReadString(node, "name");
            var imageUrl = ReadString(node, "imageUrl");
            var amazonUrl = ReadString(node, "amazonUrl");
            var price = ReadPrice(node);

            if (string.IsNullOrWhiteSpace(id) ||
                string.IsNullOrWhiteSpace(name) ||
                price is null ||
                price <= 0m ||
                string.IsNullOrEmpty(imageUrl))
                continue;

            if (!string.IsNullOrEmpty(amazonUrl) && !AmazonUrlRegex.IsMatch(amazonUrl))
                continue;

            if (!seen.Add(id))
                continue;

            products.Add(new CatalogProduct(
                id,
                name,
                ReadString(node, "description"),
                imageUrl,
                ReadString(node, "category"),
                price.Value,
                amazonUrl,
                ReadBool(node, "comingSoon"),
                ResolveAsin(node, amazonUrl, id),
                ResolveGtin(node)));
        }

        return products;
    }

    private static OgImage SelectOgImage(IReadOnlyList<CatalogProduct> products)
    {
        var buyable = products.FirstOrDefault(p => IsBuyable(p) && !string.IsNullOrWhiteSpace(p.ImageUrl));
        if (buyable is null)
            throw new InvalidOperationException("Nenhum produto comprável com imagem para og:image da vitrine.");

        return new OgImage(AbsoluteImage(buyable.ImageUrl), buyable.Name);
    }

    private static Dictionary<string, object> BuildItemList(IReadOnlyList<CatalogProduct> products)
    {
        var elements = new List<Dictionary<string, object>>(products.Count);
        for (var i = 0; i < products.Count; i++)
        {
            elements.Add(new Dictionary<string, object>
            {
                ["@type"] = "ListItem",
                ["position"] = i + 1,
                ["item"] = BuildProduct(products[i])
            });
        }

        return new Dictionary<string, object>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "ItemList",
            ["@id"] = PageUrl + "#catalog",
            ["name"] = "Vitrine Ou e Linha Flow",
            ["description"] = Description,
            ["url"] = PageUrl,
            ["inLanguage"] = "pt-BR",
            ["numberOfItems"] = products.Count,
            ["itemListElement"] = elements
        };
    }

    private static Dictionary<string, object> BuildProduct(CatalogProduct product)
    {
        var node = new Dictionary<string, object>
        {
            ["@type"] = "Product",
            ["name"] = product.Name,
            ["image"] = AbsoluteImage(product.ImageUrl),
            ["sku"] = product.Id
        };

        if (!string.IsNullOrWhiteSpace(product.Description))
            node["description"] = product.Description;

        if (!string.IsNullOrWhiteSpace(product.Category))
            node["category"] = product.Category;

        if (!string.IsNullOrWhiteSpace(product.Asin))
            node["asin"] = product.Asin;

        if (!string.IsNullOrWhiteSpace(product.Gtin))
            node["gtin"] = product.Gtin;

        node["offers"] = BuildOffer(product);
        return node;
    }

    /// <summary>
    /// Offer só aponta para a Amazon quando o item é comprável na vitrine.
    /// Em breve não tem URL (nem no site, nem inventada): não está à venda.
    /// </summary>
    private static Dictionary<string, object> BuildOffer(CatalogProduct product)
    {
        var offer = new Dictionary<string, object>
        {
            ["@type"] = "Offer",
            ["priceCurrency"] = "BRL",
            ["price"] = product.Price.ToString("0.00", CultureInfo.InvariantCulture),
            ["availability"] = IsBuyable(product)
                ? "https://schema.org/InStock"
                : "https://schema.org/OutOfStock"
        };

        if (IsBuyable(product))
            offer["url"] = product.AmazonUrl;

        return offer;
    }

    private static bool IsBuyable(CatalogProduct product) =>
        !product.ComingSoon && AmazonUrlRegex.IsMatch(product.AmazonUrl);

    /// <summary>
    /// ASIN explícito do JSON tem prioridade. Sem ele, vale a URL canônica
    /// ou o sufixo do id. Não inventa ASIN fora desses três lugares.
    /// </summary>
    private static string? ResolveAsin(JsonElement node, string amazonUrl, string id)
    {
        var explicitAsin = NormalizeAsin(ReadString(node, "asin"));
        if (explicitAsin is not null)
            return explicitAsin;

        return ExtractAsin(amazonUrl, id);
    }

    /// <summary>
    /// GTIN/EAN só entra se o JSON já trouxer <c>gtin</c> ou <c>ean</c>
    /// com 8, 12, 13 ou 14 dígitos. Campo ausente não gera código.
    /// </summary>
    private static string? ResolveGtin(JsonElement node)
    {
        var gtin = NormalizeGtin(ReadString(node, "gtin"));
        return gtin ?? NormalizeGtin(ReadString(node, "ean"));
    }

    private static string? NormalizeAsin(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var token = value.Trim().ToUpperInvariant();
        if (token.Length != 10)
            return null;

        foreach (var c in token)
        {
            if (c is not ((>= 'A' and <= 'Z') or (>= '0' and <= '9')))
                return null;
        }

        return token;
    }

    private static string? NormalizeGtin(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var compact = value.Trim().Replace(" ", "", StringComparison.Ordinal).Replace("-", "", StringComparison.Ordinal);
        if (compact.Length is not (8 or 12 or 13 or 14))
            return null;

        foreach (var c in compact)
        {
            if (c is < '0' or > '9')
                return null;
        }

        return compact;
    }

    private static string? ExtractAsin(string amazonUrl, string id)
    {
        var fromUrl = AmazonUrlRegex.Match(amazonUrl);
        if (fromUrl.Success)
            return fromUrl.Groups[1].Value;

        var fromId = IdAsinRegex.Match(id);
        if (!fromId.Success)
            return null;

        var token = fromId.Groups[1].Value;
        if (!token.Any(char.IsDigit))
            return null;

        return token.ToUpperInvariant();
    }

    private static string AbsoluteImage(string imageUrl)
    {
        if (imageUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            imageUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            return imageUrl;

        return Origin + "/" + imageUrl.TrimStart('/');
    }

    private static string BuildProdutosHtml(string indexHtml, string itemListJson, OgImage og)
    {
        var html = indexHtml;
        html = ReplaceOne(
            html,
            new Regex(@"(<title>)(.*?)(</title>)", RegexOptions.Singleline),
            match => match.Groups[1].Value + HtmlAttribute(Title) + match.Groups[3].Value,
            "title");
        html = ReplaceMetaContent(html, "name", "description", Description);
        html = ReplaceOne(
            html,
            new Regex(@"(<link\s+rel=""canonical""\s+href="")([^""]*)("")", RegexOptions.IgnoreCase),
            match => match.Groups[1].Value + PageUrl + match.Groups[3].Value,
            "canonical");
        html = ReplaceMetaContent(html, "property", "og:title", Title);
        html = ReplaceMetaContent(html, "property", "og:description", Description);
        html = ReplaceMetaContent(html, "property", "og:url", PageUrl);
        html = ReplaceMetaContent(html, "property", "og:image", og.ImageUrl);
        html = ReplaceMetaContent(html, "property", "og:image:alt", og.ImageAlt);
        html = ReplaceMetaContent(html, "name", "twitter:title", Title);
        html = ReplaceMetaContent(html, "name", "twitter:description", Description);
        html = ReplaceMetaContent(html, "name", "twitter:image", og.ImageUrl);
        html = ReplaceMetaContent(html, "name", "twitter:image:alt", og.ImageAlt);

        var nl = html.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var json = itemListJson.Replace("\n", nl, StringComparison.Ordinal);
        var seoTag = "<script src=\"js/seo.js\"></script>";
        var block =
            "<script type=\"application/ld+json\" id=\"vitrine-itemlist\">" + nl +
            json + nl +
            "    </script>" + nl +
            nl +
            "    " + seoTag;

        var occurrences = CountOf(html, seoTag);
        if (occurrences != 1)
            throw new InvalidOperationException($"Esperava 1 ocorrência de {seoTag} em index.html, achei {occurrences}.");

        return html.Replace(seoTag, block, StringComparison.Ordinal);
    }

    private static string HtmlAttribute(string value) =>
        value.Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal);

    private static string ReplaceMetaContent(string html, string attribute, string name, string content)
    {
        var regex = new Regex(
            $@"(<meta\s+{attribute}\s*=\s*""{Regex.Escape(name)}""\s+content\s*=\s*"")([^""]*)("")",
            RegexOptions.IgnoreCase);
        var encoded = HtmlAttribute(content);
        return ReplaceOne(
            html,
            regex,
            match => match.Groups[1].Value + encoded + match.Groups[3].Value,
            $"meta {attribute}={name}");
    }

    private static string ReplaceOne(string html, Regex regex, Func<Match, string> replacer, string label)
    {
        var matches = regex.Matches(html);
        if (matches.Count != 1)
            throw new InvalidOperationException($"Esperava 1 ocorrência de {label} em index.html, achei {matches.Count}.");

        var match = matches[0];
        return string.Concat(html.AsSpan(0, match.Index), replacer(match), html.AsSpan(match.Index + match.Length));
    }

    private static int CountOf(string haystack, string needle)
    {
        var count = 0;
        var index = 0;
        while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }

        return count;
    }

    private static string Serialize(object value)
    {
        var json = JsonSerializer.Serialize(value, JsonOptions);
        return json.Replace("<", "\\u003c", StringComparison.Ordinal);
    }

    private static string ReadString(JsonElement node, string name)
    {
        if (!node.TryGetProperty(name, out var value) || value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return "";

        return value.ValueKind == JsonValueKind.String ? value.GetString() ?? "" : value.ToString();
    }

    private static bool ReadBool(JsonElement node, string name)
    {
        if (!node.TryGetProperty(name, out var value))
            return false;

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => false
        };
    }

    private static decimal? ReadPrice(JsonElement node)
    {
        if (!node.TryGetProperty("price", out var value))
            return null;

        if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var price))
            return price;

        if (value.ValueKind == JsonValueKind.String &&
            decimal.TryParse(value.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out price))
            return price;

        return null;
    }
}
