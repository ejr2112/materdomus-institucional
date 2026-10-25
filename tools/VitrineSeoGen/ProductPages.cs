using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace MaterDomus.VitrineSeoGen;

/// <summary>
/// Uma página estática por produto, em /produtos/{id}. O HTML já nasce com
/// Open Graph e JSON-LD: o crawler do catálogo da Meta não executa o site.
/// O rewrite do Azure Static Web Apps entrega essa página com HTTP 200
/// na URL limpa (não é redirect).
/// </summary>
public static partial class VitrineSeo
{
    public const string ProductSitemapLastMod = "2026-10-25";
    public const string ProductSitemapPriority = "0.8";

    public sealed record ProductPageDocument(
        string Id,
        string PublicPath,
        string FileName,
        string CanonicalUrl,
        string Html);

    private static readonly Regex ProductSlugRegex = new(
        "^[a-z0-9]+(?:-[a-z0-9]+)*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly (string Loc, string LastMod, string Priority)[] InstitutionalSitemap =
    [
        (Origin + "/", "2026-10-20", "1.0"),
        (Origin + "/produtos", "2026-10-20", "0.9"),
        (Origin + "/fornecedores", "2026-10-20", "0.9"),
        (Origin + "/sobre", "2026-10-20", "0.7"),
        (Origin + "/contato", "2026-10-20", "0.6"),
        (Origin + "/privacidade", "2026-10-20", "0.4"),
        (Origin + "/frete-e-trocas", "2026-10-20", "0.4")
    ];

    /// <summary>
    /// Slug estável: o id já publicado em products.json. Não usa o título,
    /// que pode mudar, nem o ASIN, que alguns itens ainda não têm.
    /// </summary>
    public static string? TryProductPublicPath(string? id)
    {
        if (string.IsNullOrWhiteSpace(id) || !ProductSlugRegex.IsMatch(id))
            return null;

        return "/produtos/" + id;
    }

    public static IReadOnlyList<ProductPageDocument> BuildProductPages(string productsJson, string indexHtml) =>
        SelectProducts(productsJson).Select(product => RenderProductPage(indexHtml, product)).ToList();

    public static string BuildSitemap(string productsJson) =>
        RenderSitemap(SelectProducts(productsJson));

    public static string UpsertProductRoutes(string configJson, string productsJson) =>
        RenderRoutes(configJson, SelectProducts(productsJson));

    private static void WriteProductPages(string repoRoot, string productsJson, string indexHtml, Encoding encoding)
    {
        var products = SelectProducts(productsJson);
        var pages = products.Select(product => RenderProductPage(indexHtml, product)).ToList();
        var dir = Path.Combine(repoRoot, "wwwroot", "produtos");
        Directory.CreateDirectory(dir);

        var expected = new HashSet<string>(pages.Select(page => page.FileName), StringComparer.Ordinal);
        if (Directory.Exists(dir))
        {
            foreach (var existing in Directory.EnumerateFiles(dir, "*.html"))
            {
                if (!expected.Contains(Path.GetFileName(existing)))
                    File.Delete(existing);
            }
        }

        foreach (var page in pages)
            File.WriteAllText(Path.Combine(dir, page.FileName), page.Html, encoding);

        File.WriteAllText(
            Path.Combine(repoRoot, "wwwroot", "sitemap.xml"),
            RenderSitemap(products),
            encoding);

        var configPath = Path.Combine(repoRoot, "wwwroot", "staticwebapp.config.json");
        File.WriteAllText(
            configPath,
            RenderRoutes(File.ReadAllText(configPath), products),
            encoding);
    }

    private static string RenderSitemap(IReadOnlyList<CatalogProduct> products)
    {
        var sb = new StringBuilder();
        sb.AppendLine("""<?xml version="1.0" encoding="UTF-8"?>""");
        sb.AppendLine("""<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">""");
        foreach (var entry in InstitutionalSitemap)
            AppendSitemapUrl(sb, entry.Loc, entry.LastMod, entry.Priority);

        foreach (var product in products)
            AppendSitemapUrl(sb, ProductCanonical(product), ProductSitemapLastMod, ProductSitemapPriority);

        sb.AppendLine("</urlset>");
        return sb.ToString();
    }

    private static void AppendSitemapUrl(StringBuilder sb, string loc, string lastMod, string priority)
    {
        sb.AppendLine("    <url>");
        sb.Append("        <loc>").Append(XmlText(loc)).AppendLine("</loc>");
        sb.Append("        <lastmod>").Append(XmlText(lastMod)).AppendLine("</lastmod>");
        sb.Append("        <priority>").Append(XmlText(priority)).AppendLine("</priority>");
        sb.AppendLine("    </url>");
    }

    private static string XmlText(string value) =>
        value.Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal);

    /// <summary>
    /// Mantém as rotas institucionais e reescreve /produtos/{slug} para o HTML.
    /// Rewrite (não redirect) para a URL limpa responder 200.
    /// </summary>
    private static string RenderRoutes(string configJson, IReadOnlyList<CatalogProduct> products)
    {
        var root = JsonNode.Parse(configJson) as JsonObject
            ?? throw new InvalidOperationException("staticwebapp.config.json inválido.");
        if (root["routes"] is not JsonArray routes)
            throw new InvalidOperationException("staticwebapp.config.json sem routes.");

        var kept = new JsonArray();
        foreach (var route in routes)
        {
            var path = route?["route"]?.GetValue<string>() ?? "";
            if (path.StartsWith("/produtos/", StringComparison.Ordinal))
                continue;

            kept.Add(route!.DeepClone());
        }

        foreach (var product in products)
        {
            var path = ProductPublicPath(product);
            kept.Add(new JsonObject
            {
                ["route"] = path,
                ["rewrite"] = path + ".html"
            });
        }

        root["routes"] = kept;
        return root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + "\n";
    }

    private static ProductPageDocument RenderProductPage(string indexHtml, CatalogProduct product)
    {
        var path = ProductPublicPath(product);
        var canonical = Origin + path;
        var images = Gallery(product);
        var primary = images[0];
        var buyable = IsBuyable(product);
        var priceAmount = product.Price.ToString("0.00", CultureInfo.InvariantCulture);
        var priceLabel = FormatPrice(product.Price);
        var retailerSku = string.IsNullOrWhiteSpace(product.Sku) ? product.Id : product.Sku!;
        var availabilityMeta = buyable ? "in stock" : "out of stock";
        var availabilityLabel = buyable ? "Em estoque" : "Em breve";
        var documentTitle = product.Name + " | Mater Domus";
        var description = string.IsNullOrWhiteSpace(product.Description)
            ? product.Name + ", da marca Ou, na vitrine Mater Domus."
            : product.Description.Trim();

        var html = ApplyDocumentMeta(indexHtml, documentTitle, description, canonical);
        html = ReplaceMetaContent(html, "property", "og:title", product.Name);
        html = ReplaceMetaContent(html, "name", "twitter:title", product.Name);
        html = ReplaceMetaContent(html, "property", "og:image", AbsoluteImage(primary.Url));
        html = ReplaceMetaContent(html, "property", "og:image:alt", primary.Alt);
        html = ReplaceMetaContent(html, "name", "twitter:image", AbsoluteImage(primary.Url));
        html = ReplaceMetaContent(html, "name", "twitter:image:alt", primary.Alt);
        html = ReplaceMetaContent(html, "property", "og:type", "product");

        if (html.Contains("<html lang=\"pt-br\">", StringComparison.Ordinal))
        {
            html = html.Replace(
                "<html lang=\"pt-br\">",
                "<html lang=\"pt-br\" prefix=\"og: https://ogp.me/ns# product: https://ogp.me/ns/product#\">",
                StringComparison.Ordinal);
        }

        var nl = html.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var head = new StringBuilder();
        foreach (var image in images.Skip(1))
        {
            head.Append("    <meta property=\"og:image\" content=\"")
                .Append(HtmlAttribute(AbsoluteImage(image.Url)))
                .AppendLine("\" />");
        }

        head.Append("    <meta property=\"product:brand\" content=\"Ou\" />").Append(nl);
        head.Append("    <meta property=\"product:availability\" content=\"").Append(availabilityMeta).Append("\" />").Append(nl);
        head.Append("    <meta property=\"product:condition\" content=\"new\" />").Append(nl);
        head.Append("    <meta property=\"product:price:amount\" content=\"").Append(priceAmount).Append("\" />").Append(nl);
        head.Append("    <meta property=\"product:price:currency\" content=\"BRL\" />").Append(nl);
        head.Append("    <meta property=\"product:retailer_item_id\" content=\"")
            .Append(HtmlAttribute(retailerSku)).Append("\" />").Append(nl);
        head.Append("    <meta property=\"og:price:amount\" content=\"").Append(priceAmount).Append("\" />").Append(nl);
        head.Append("    <meta property=\"og:price:currency\" content=\"BRL\" />").Append(nl);
        head.Append("    <meta property=\"og:availability\" content=\"").Append(availabilityMeta).Append("\" />").Append(nl);
        head.Append("    <link rel=\"preload\" as=\"image\" href=\"")
            .Append(HtmlAttribute(primary.Url))
            .Append("\" fetchpriority=\"high\" />").Append(nl);

        html = InsertOnce(html, "</head>", head.ToString() + "</head>", "fechamento de head");

        var json = Serialize(BuildProductPageJsonLd(product, canonical, images, retailerSku, buyable, priceAmount)).ReplaceLineEndings(nl);
        var seoTag = "<script defer src=\"js/seo.js\"></script>";
        var block =
            "<script type=\"application/ld+json\" id=\"product-jsonld\">" + nl +
            json + nl +
            "    </script>" + nl +
            nl +
            "    " + seoTag;
        html = InsertOnce(html, seoTag, block, "js/seo.js");

        const string asyncCss = "<link href=\"css/produtos.css\" rel=\"stylesheet\" media=\"print\" onload=\"this.media='all'\" />";
        const string blockingCss = "<link href=\"css/produtos.css\" rel=\"stylesheet\" />";
        if (html.Contains(asyncCss, StringComparison.Ordinal))
            html = html.Replace(asyncCss, blockingCss, StringComparison.Ordinal);

        html = ReplacePageMain(html, RenderProductMain(product, images, primary, buyable, priceAmount, priceLabel, retailerSku, availabilityLabel, availabilityMeta, description));
        html = ActivateNav(html, "produtos");
        // ActivateNav limpa aria-current="page" no documento inteiro e só
        // devolve o atributo aos links data-nav. A trilha recebe o seu depois.
        html = html.Replace(
            "class=\"product-page__current\"",
            "class=\"product-page__current\" aria-current=\"page\"",
            StringComparison.Ordinal);

        if (!html.Contains("js/product-page.js", StringComparison.Ordinal))
            html = InsertOnce(html, "</body>", "    <script defer src=\"js/product-page.js\"></script>\n</body>", "fechamento de body");

        return new ProductPageDocument(product.Id, path, product.Id + ".html", canonical, html);
    }

    private static Dictionary<string, object> BuildProductPageJsonLd(
        CatalogProduct product,
        string canonical,
        IReadOnlyList<GalleryImage> images,
        string retailerSku,
        bool buyable,
        string priceAmount)
    {
        var node = new Dictionary<string, object>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "Product",
            ["@id"] = canonical + "#product",
            ["url"] = canonical,
            ["name"] = product.Name,
            ["image"] = images.Select(image => AbsoluteImage(image.Url)).ToArray(),
            ["sku"] = retailerSku,
            ["productID"] = product.Id,
            ["brand"] = new Dictionary<string, object>
            {
                ["@type"] = "Brand",
                ["name"] = "Ou",
                ["url"] = "https://ou.com.br/"
            }
        };

        if (!string.IsNullOrWhiteSpace(product.Description))
            node["description"] = product.Description.Trim();

        if (!string.IsNullOrWhiteSpace(product.Category))
            node["category"] = product.Category;

        if (!string.IsNullOrWhiteSpace(product.Asin))
            node["asin"] = product.Asin;

        if (!string.IsNullOrWhiteSpace(product.Gtin))
            node["gtin"] = product.Gtin;

        var offer = new Dictionary<string, object>
        {
            ["@type"] = "Offer",
            ["url"] = canonical,
            ["priceCurrency"] = "BRL",
            ["price"] = priceAmount,
            ["availability"] = buyable
                ? "https://schema.org/InStock"
                : "https://schema.org/OutOfStock",
            ["itemCondition"] = "https://schema.org/NewCondition",
            ["seller"] = new Dictionary<string, object>
            {
                ["@id"] = Origin + "/#organization"
            }
        };
        node["offers"] = offer;
        return node;
    }

    private readonly record struct GalleryImage(string Url, string Alt);

    private static IReadOnlyList<GalleryImage> Gallery(CatalogProduct product)
    {
        var images = new List<GalleryImage>();
        foreach (var image in product.Lifestyle)
        {
            if (string.IsNullOrWhiteSpace(image.Url))
                continue;

            var alt = string.IsNullOrWhiteSpace(image.Alt) ? product.Name : image.Alt;
            images.Add(new GalleryImage(image.Url, alt));
        }

        if (images.Count == 0)
        {
            var url = string.IsNullOrWhiteSpace(product.ImageUrl)
                ? "images/placeholder-product.png"
                : product.ImageUrl;
            images.Add(new GalleryImage(url, product.Name));
        }

        return images;
    }

    private static string RenderProductMain(
        CatalogProduct product,
        IReadOnlyList<GalleryImage> images,
        GalleryImage primary,
        bool buyable,
        string priceAmount,
        string priceLabel,
        string retailerSku,
        string availabilityLabel,
        string availabilityMeta,
        string description)
    {
        var sb = new StringBuilder();
        sb.Append("<section class=\"product-page\"");
        sb.Append(" data-product-id=\"").Append(HtmlAttribute(product.Id)).Append('"');
        sb.Append(" data-sku=\"").Append(HtmlAttribute(retailerSku)).Append('"');
        if (!string.IsNullOrWhiteSpace(product.Asin))
            sb.Append(" data-asin=\"").Append(HtmlAttribute(product.Asin)).Append('"');
        sb.AppendLine(">");

        sb.AppendLine("""
                <nav class="product-page__crumb" aria-label="Trilha de navegação">
                    <a href="/">Início</a>
                    <span aria-hidden="true"> / </span>
                    <a href="/produtos">Vitrine</a>
                    <span aria-hidden="true"> / </span>
            """);
        sb.Append("        <span class=\"product-page__current\">").Append(HtmlAttribute(product.Name)).AppendLine("</span>");
        sb.AppendLine("    </nav>");
        sb.AppendLine("    <div class=\"product-page__layout\">");
        sb.AppendLine("        <div class=\"product-page__gallery\">");
        sb.AppendLine("            <div class=\"product-page__frame\">");
        sb.Append("                <img id=\"product-photo\" class=\"product-page__photo\" width=\"1200\" height=\"1200\"");
        sb.Append(" src=\"").Append(HtmlAttribute(primary.Url)).Append('"');
        sb.Append(" alt=\"").Append(HtmlAttribute(primary.Alt)).Append('"');
        sb.AppendLine(" fetchpriority=\"high\" decoding=\"async\"");
        sb.AppendLine("                     onerror=\"this.onerror=null;this.src='images/placeholder-product.png'\" />");
        sb.AppendLine("            </div>");

        if (images.Count > 1)
        {
            sb.AppendLine("            <div class=\"product-page__thumbs\" role=\"group\" aria-label=\"Fotos do produto\">");
            for (var i = 0; i < images.Count; i++)
            {
                var image = images[i];
                var active = i == 0;
                sb.Append("                <button type=\"button\" class=\"product-page__thumb");
                if (active)
                    sb.Append(" is-active");
                sb.Append("\" data-full=\"").Append(HtmlAttribute(image.Url)).Append('"');
                sb.Append(" data-alt=\"").Append(HtmlAttribute(image.Alt)).Append('"');
                sb.Append(" aria-label=\"Ver foto ").Append(i + 1).Append(" de ").Append(images.Count);
                sb.Append(": ").Append(HtmlAttribute(image.Alt)).Append('"');
                if (active)
                    sb.Append(" aria-current=\"true\"");
                sb.AppendLine(">");
                sb.Append("                    <img width=\"1200\" height=\"1200\" src=\"")
                    .Append(HtmlAttribute(image.Url))
                    .Append("\" alt=\"\" />");
                sb.AppendLine();
                sb.AppendLine("                </button>");
            }

            sb.AppendLine("            </div>");
        }

        sb.AppendLine("        </div>");
        sb.AppendLine("        <div class=\"product-page__info\">");
        if (!string.IsNullOrWhiteSpace(product.Category))
        {
            sb.Append("            <p class=\"product-page__category\">")
                .Append(HtmlAttribute(product.Category)).AppendLine("</p>");
        }

        if (!buyable)
            sb.AppendLine("            <span class=\"product-detail__badge\">Em breve</span>");

        sb.Append("            <h1>").Append(HtmlAttribute(product.Name)).AppendLine("</h1>");
        sb.AppendLine("            <p class=\"product-page__brand\">Marca: <strong>Ou</strong></p>");
        if (!string.IsNullOrWhiteSpace(product.Sku))
        {
            sb.Append("            <p class=\"product-page__sku\">SKU: <strong>")
                .Append(HtmlAttribute(product.Sku)).AppendLine("</strong></p>");
        }

        if (!string.IsNullOrWhiteSpace(product.Asin))
        {
            sb.Append("            <p class=\"product-page__asin\">ASIN: <strong>")
                .Append(HtmlAttribute(product.Asin)).AppendLine("</strong></p>");
        }

        sb.Append("            <p class=\"product-page__price\" data-price=\"").Append(priceAmount)
            .Append("\" data-currency=\"BRL\">").Append(HtmlAttribute(priceLabel)).AppendLine("</p>");
        sb.Append("            <p class=\"product-page__availability");
        sb.Append(buyable ? " product-page__availability--in" : " product-page__availability--soon");
        sb.Append("\" data-availability=\"").Append(availabilityMeta).Append("\">Disponibilidade: <strong>")
            .Append(availabilityLabel).AppendLine("</strong></p>");
        sb.AppendLine("            <h2 class=\"product-page__description-title\">Descrição</h2>");
        sb.Append("            <p class=\"product-page__description\">")
            .Append(HtmlAttribute(description)).AppendLine("</p>");

        if (buyable)
        {
            var track = "{\"event\":\"cta_click\",\"page\":\"produto\",\"cta_name\":\"comprar_amazon\",\"product_id\":\""
                + product.Id + "\",\"asin\":\"" + (product.Asin ?? "") + "\"}";
            sb.Append("            <a class=\"product-page__buy product-detail__amazon-btn\" target=\"_blank\" rel=\"noopener noreferrer\" href=\"")
                .Append(HtmlAttribute(product.AmazonUrl)).Append('"');
            sb.Append(" aria-label=\"Comprar na Amazon: ").Append(HtmlAttribute(product.Name)).Append('"');
            sb.Append(" data-track=\"").Append(HtmlAttribute(track)).AppendLine("\">Comprar na Amazon</a>");
            sb.AppendLine("""
                        <p class="product-page__note">A compra é concluída na Amazon Brasil, na loja da Mater Domus. Pagamento, frete e trocas ficam com a Amazon.</p>
                """);
        }
        else
        {
            sb.AppendLine("""
                        <p class="product-page__note">Disponível em breve na Amazon. Quando o item entrar em estoque, o botão de compra aparece nesta página.</p>
                """);
        }

        sb.AppendLine("""
                    <p class="product-page__back"><a href="/produtos">Voltar à vitrine</a></p>
                </div>
            </div>
            </section>
            """);

        return sb.ToString().TrimEnd();
    }

    private static string ProductPublicPath(CatalogProduct product)
    {
        var path = TryProductPublicPath(product.Id);
        if (path is null)
            throw new InvalidOperationException($"id de produto não é um slug de URL: {product.Id}");

        return path;
    }

    private static string ProductCanonical(CatalogProduct product) => Origin + ProductPublicPath(product);

    private static string InsertOnce(string html, string needle, string replacement, string label)
    {
        var count = 0;
        var index = 0;
        while ((index = html.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }

        if (count != 1)
            throw new InvalidOperationException($"Esperava 1 ocorrência de {label} em index.html, achei {count}.");

        return html.Replace(needle, replacement, StringComparison.Ordinal);
    }
}
