using System.Globalization;
using System.Text;

namespace MaterDomus.VitrineSeoGen;

public static partial class VitrineSeo
{
    private readonly record struct CatalogMarkupResult(string Html, string? LcpImage);

    private static CatalogMarkupResult BuildCatalogMarkup(List<CatalogProduct> products)
    {
        var available = products.Where(product => !product.ComingSoon).ToList();
        var comingSoon = products.Where(product => product.ComingSoon).ToList();
        var sb = new StringBuilder();

        sb.AppendLine("""
            <section class="produtos-page">
                <header class="produtos-page__intro">
                    <h1>Vitrine</h1>
                    <p class="page-subtitle">Linha Flow e Ou — disponíveis na Amazon ou em breve.</p>
                </header>
                <div class="filter-bar">
                    <input class="filter-bar__search" type="search" placeholder="Buscar produtos..." aria-label="Buscar produtos" />
                    <select class="filter-bar__select" aria-label="Filtrar por categoria">
                        <option value="">Todas as categorias</option>
            """);

        foreach (var category in products
                     .Select(product => product.Category)
                     .Where(category => !string.IsNullOrWhiteSpace(category))
                     .Distinct()
                     .OrderBy(category => category, StringComparer.CurrentCulture))
        {
            sb.Append("            <option value=\"")
                .Append(HtmlAttribute(category))
                .Append("\">")
                .Append(HtmlAttribute(category))
                .AppendLine("</option>");
        }

        sb.AppendLine("""
                    </select>
                    <button class="filter-bar__favorites-btn" type="button" aria-pressed="false">♥ Favoritos</button>
                </div>
                <div class="favorites-badge" id="favorites-badge" hidden>
                    <span class="favorites-badge__count">0</span>
                    <span class="favorites-badge__label">produtos favoritados</span>
                </div>
            """);

        string? lcp = null;
        if (available.Count == 0 && comingSoon.Count == 0)
        {
            sb.AppendLine("""
                <div class="produtos-state produtos-empty">
                    <div class="produtos-state__icon">📦</div>
                    <p class="produtos-state__title">Nenhum produto disponível no momento.</p>
                    <p class="produtos-state__message">Volte em breve para conferir novidades.</p>
                </div>
                """);
        }
        else
        {
            if (available.Count > 0)
                AppendSection(sb, available, "produtos-disponiveis-heading", "Disponíveis agora", "products-section--available", eagerFirst: true, ref lcp);

            if (comingSoon.Count > 0)
                AppendSection(sb, comingSoon, "produtos-em-breve-heading", "Em breve", "products-section--soon", eagerFirst: lcp is null, ref lcp);
        }

        sb.AppendLine("""
                <div class="produtos-state produtos-empty" id="empty-favorites" hidden>
                    <div class="produtos-state__icon">♡</div>
                    <p class="produtos-state__title">Nenhum produto favoritado.</p>
                    <p class="produtos-state__message">Clique no ícone de coração em um produto para adicioná-lo aos favoritos.</p>
                </div>
                <div class="produtos-state produtos-empty" id="empty-search" hidden>
                    <div class="produtos-state__icon">🔍</div>
                    <p class="produtos-state__title">Nenhum produto encontrado.</p>
                    <p class="produtos-state__message">Tente ajustar os filtros ou o termo de busca.</p>
                </div>
                <div class="produtos-detail-unavailable" id="detail-unavailable" role="alert" hidden>
                    Detalhes do produto indisponíveis.
                </div>
                <aside class="starlink-promo" aria-label="Indicação Starlink">
                    <div class="starlink-promo__copy">
                        <p class="starlink-promo__eyebrow">Parceiro Starlink</p>
                        <p class="starlink-promo__title">Ganhe um mês de Starlink grátis</p>
                        <p class="starlink-promo__text">
                            Cadastre-se pelo nosso link e aproveite internet rápida e confiável em casa e em qualquer lugar.
                        </p>
                    </div>
                    <a class="starlink-promo__cta"
                       href="https://starlink.com/?referral=RC-1711398-31153-58&amp;app_source=share"
                       target="_blank"
                       rel="noopener noreferrer">
                        Ativar desconto
                    </a>
                </aside>
            </section>
            <div class="product-detail__backdrop" id="product-detail-backdrop" hidden>
                <div class="product-detail" role="dialog" aria-modal="true" aria-label="" tabindex="-1">
                    <button type="button" class="product-detail__close-btn" aria-label="Fechar detalhes">&times;</button>
                    <div class="product-detail__body">
                        <div class="product-detail__image-wrapper">
                            <img id="detail-image" class="product-detail__image" alt="" width="1200" height="1200"
                                 src="images/placeholder-product.png"
                                 onerror="this.onerror=null;this.src='images/placeholder-product.png'" />
                        </div>
                        <div class="product-detail__meta">
                            <span class="product-detail__category" id="detail-category"></span>
                            <span class="product-detail__badge" id="detail-badge" hidden>Em breve</span>
                        </div>
                        <h2 class="product-detail__name" id="detail-name"></h2>
                        <p class="product-detail__price" id="detail-price"></p>
                        <p class="product-detail__coming-soon" id="detail-soon" hidden>Disponível em breve na Amazon</p>
                        <p class="product-detail__copy" id="detail-copy"></p>
                        <a id="detail-amazon" class="product-detail__amazon-btn" href="https://www.amazon.com.br/" target="_blank" rel="noopener noreferrer" hidden>Comprar na Amazon</a>
                    </div>
                </div>
            </div>
            """);

        return new CatalogMarkupResult(sb.ToString().TrimEnd(), lcp);
    }

    private static void AppendSection(
        StringBuilder sb,
        List<CatalogProduct> products,
        string headingId,
        string heading,
        string sectionClass,
        bool eagerFirst,
        ref string? lcp)
    {
        sb.Append("    <section class=\"products-section ")
            .Append(sectionClass)
            .Append("\" aria-labelledby=\"")
            .Append(headingId)
            .AppendLine("\">");
        sb.Append("        <h2 id=\"").Append(headingId).Append("\" class=\"products-section__title\">")
            .Append(HtmlAttribute(heading)).AppendLine("</h2>");
        sb.AppendLine("        <div class=\"products-grid products-grid--fade-in\">");

        for (var i = 0; i < products.Count; i++)
        {
            var eager = eagerFirst && i == 0;
            AppendCard(sb, products[i], eager);
            if (eager)
                lcp = PrimaryImage(products[i]);
        }

        sb.AppendLine("        </div>");
        sb.AppendLine("    </section>");
    }

    private static void AppendCard(StringBuilder sb, CatalogProduct product, bool eager)
    {
        var price = FormatPrice(product.Price);
        var copy = BuildPersuasiveCopy(product) ?? product.Description;
        var image = PrimaryImage(product);
        var alt = PrimaryAlt(product);
        var cardClass = product.ComingSoon ? "product-card product-card--coming-soon" : "product-card product-card--available";

        sb.Append("            <article role=\"article\" class=\"").Append(cardClass).Append('"');
        sb.Append(" aria-label=\"").Append(HtmlAttribute(product.Name)).Append('"');
        sb.Append(" data-id=\"").Append(HtmlAttribute(product.Id)).Append('"');
        sb.Append(" data-name=\"").Append(HtmlAttribute(product.Name)).Append('"');
        sb.Append(" data-description=\"").Append(HtmlAttribute(product.Description)).Append('"');
        sb.Append(" data-category=\"").Append(HtmlAttribute(product.Category)).Append('"');
        sb.Append(" data-price=\"").Append(HtmlAttribute(price)).Append('"');
        sb.Append(" data-copy=\"").Append(HtmlAttribute(copy)).Append('"');
        sb.Append(" data-image=\"").Append(HtmlAttribute(image)).Append('"');
        sb.Append(" data-alt=\"").Append(HtmlAttribute(alt)).Append('"');
        sb.Append(" data-amazon=\"").Append(HtmlAttribute(product.AmazonUrl)).Append('"');
        sb.Append(" data-coming-soon=\"").Append(product.ComingSoon ? "true" : "false").AppendLine("\">");
        sb.AppendLine("                <div class=\"product-card__image-wrapper\">");

        if (product.Lifestyle.Count > 1)
            AppendGallery(sb, product, eager);
        else
        {
            sb.Append("                    <img class=\"product-card__image\" width=\"1200\" height=\"1200\"");
            sb.Append(" src=\"").Append(HtmlAttribute(image)).Append('"');
            sb.Append(" alt=\"").Append(HtmlAttribute(product.Name)).Append('"');
            if (eager)
                sb.Append(" fetchpriority=\"high\" decoding=\"async\"");
            else
                sb.Append(" loading=\"lazy\" decoding=\"async\"");
            sb.AppendLine(" onerror=\"this.onerror=null;this.src='images/placeholder-product.png'\" />");
        }

        if (product.ComingSoon)
            sb.AppendLine("                    <span class=\"product-card__badge\">Em breve</span>");

        sb.AppendLine("                </div>");
        sb.AppendLine("                <div class=\"product-card__body\">");
        sb.Append("                    <span class=\"product-card__category\">").Append(HtmlAttribute(product.Category)).AppendLine("</span>");
        sb.Append("                    <h3 class=\"product-card__name\">").Append(HtmlAttribute(product.Name)).AppendLine("</h3>");
        sb.Append("                    <p class=\"product-card__price\">").Append(HtmlAttribute(price)).AppendLine("</p>");
        if (product.ComingSoon)
            sb.AppendLine("                    <p class=\"product-card__coming-soon\">Disponível em breve na Amazon</p>");
        sb.Append("                    <p class=\"product-card__description\">").Append(HtmlAttribute(Truncate(product.Description))).AppendLine("</p>");
        sb.AppendLine("                </div>");
        sb.AppendLine("                <div class=\"product-card__actions\">");
        if (IsBuyable(product))
        {
            sb.Append("                    <a class=\"product-card__amazon-btn\" target=\"_blank\" rel=\"noopener noreferrer\" href=\"")
                .Append(HtmlAttribute(product.AmazonUrl))
                .AppendLine("\">Comprar na Amazon</a>");
        }

        sb.Append("                    <button class=\"product-card__details-btn\" type=\"button\" aria-label=\"Ver detalhes ")
            .Append(HtmlAttribute(product.Name))
            .AppendLine("\">Ver detalhes</button>");
        sb.AppendLine("                    <button class=\"product-card__favorite-btn\" type=\"button\" aria-pressed=\"false\" aria-label=\"Favoritar produto\">♡</button>");
        sb.AppendLine("                </div>");
        sb.AppendLine("            </article>");
    }

    private static void AppendGallery(StringBuilder sb, CatalogProduct product, bool eager)
    {
        var slides = product.Lifestyle;
        var fallback = string.IsNullOrWhiteSpace(product.ImageUrl)
            ? "images/placeholder-product.png"
            : product.ImageUrl;
        var onerror =
            "this.onerror=function(){this.onerror=null;this.src='images/placeholder-product.png'};this.src='"
            + fallback.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("'", "\\'", StringComparison.Ordinal)
            + "'";

        sb.AppendLine("                    <div class=\"product-card__gallery\" data-active-index=\"0\">");
        for (var i = 0; i < slides.Count; i++)
        {
            var slide = slides[i];
            var active = i == 0;
            sb.Append("                        <img class=\"product-card__image product-card__slide");
            if (active)
                sb.Append(" product-card__slide--active");
            if (i == 1)
                sb.Append(" product-card__slide--hover-target");
            sb.Append("\" width=\"1200\" height=\"1200\" alt=\"").Append(HtmlAttribute(slide.Alt)).Append('"');
            if (active)
                sb.Append(" src=\"").Append(HtmlAttribute(slide.Url)).Append('"');
            else
            {
                // Sem src vazio: alguns browsers buscam o HTML da página como imagem.
                sb.Append(" src=\"data:image/gif;base64,R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7\"");
                sb.Append(" data-src=\"").Append(HtmlAttribute(slide.Url)).Append('"');
            }
            if (eager && active)
                sb.Append(" fetchpriority=\"high\" decoding=\"async\"");
            else
                sb.Append(" loading=\"lazy\" decoding=\"async\"");
            if (!active)
                sb.Append(" aria-hidden=\"true\"");
            sb.Append(" onerror=\"").Append(HtmlAttribute(onerror)).AppendLine("\" />");
        }

        sb.AppendLine("                        <div class=\"product-card__dots\" role=\"group\" aria-label=\"Fotos do produto\">");
        for (var i = 0; i < slides.Count; i++)
        {
            sb.Append("                            <button type=\"button\" class=\"product-card__dot");
            if (i == 0)
                sb.Append(" product-card__dot--active");
            sb.Append("\" aria-label=\"Ver foto ").Append(i + 1).Append(" de ").Append(slides.Count).Append('"');
            sb.Append(" aria-pressed=\"").Append(i == 0 ? "true" : "false").Append('"');
            if (i == 0)
                sb.Append(" aria-current=\"true\"");
            sb.AppendLine("></button>");
        }

        sb.AppendLine("                        </div>");
        sb.AppendLine("                        <button type=\"button\" class=\"product-card__arrow product-card__arrow--prev\" aria-label=\"Foto anterior\">‹</button>");
        sb.AppendLine("                        <button type=\"button\" class=\"product-card__arrow product-card__arrow--next\" aria-label=\"Próxima foto\">›</button>");
        sb.AppendLine("                    </div>");
    }

    private static string PrimaryImage(CatalogProduct product)
    {
        if (product.Lifestyle.Count > 0 && !string.IsNullOrWhiteSpace(product.Lifestyle[0].Url))
            return product.Lifestyle[0].Url;

        return string.IsNullOrWhiteSpace(product.ImageUrl)
            ? "images/placeholder-product.png"
            : product.ImageUrl;
    }

    private static string PrimaryAlt(CatalogProduct product)
    {
        if (product.Lifestyle.Count > 1 && !string.IsNullOrWhiteSpace(product.Lifestyle[0].Alt))
            return product.Lifestyle[0].Alt;

        return product.Name;
    }

    private static string Truncate(string description) =>
        description.Length > 120 ? description[..120] + "..." : description;

    private static string FormatPrice(decimal price) =>
        price.ToString("C", CultureInfo.GetCultureInfo("pt-BR"));

    /// <summary>
    /// Mesmas frases de <c>ProductCopyGenerator</c>. Duplicado aqui para o HTML
    /// estático não depender do projeto Blazor (o alvo de build chamaria este gerador).
    /// </summary>
    private static string? BuildPersuasiveCopy(CatalogProduct product)
    {
        if (string.IsNullOrWhiteSpace(product.Name))
            return null;

        var name = product.Name.Trim();
        var sentences = new List<string>();
        if (!string.IsNullOrWhiteSpace(product.Category))
        {
            sentences.Add(
                $"Conheça {name}, uma escolha especial em {product.Category.Trim()} para transformar o seu dia a dia com mais praticidade e bem-estar.");
        }
        else
        {
            sentences.Add(
                $"Conheça {name}, uma escolha especial para transformar o seu dia a dia com mais praticidade e bem-estar.");
        }

        if (!string.IsNullOrWhiteSpace(product.Description))
            sentences.Add(product.Description.Trim());

        if (product.Price > 0m)
            sentences.Add($"Tudo isso por apenas {FormatPrice(product.Price)}, um investimento que vale a pena.");

        sentences.Add(product.ComingSoon
            ? "Disponível em breve na Amazon."
            : "Garanta o seu agora e aproveite!");

        var result = string.Join(" ", sentences);
        if (result.Length <= 600)
            return result;

        var opening = sentences[0];
        var cta = sentences[^1];
        var builder = new StringBuilder(opening);
        var closing = " " + cta;
        var budget = 600 - opening.Length - closing.Length;
        for (var i = 1; i < sentences.Count - 1; i++)
        {
            var candidate = " " + sentences[i];
            if (candidate.Length <= budget)
            {
                builder.Append(candidate);
                budget -= candidate.Length;
            }
        }

        builder.Append(closing);
        result = builder.ToString();
        return result.Length > 600 ? result[..600].TrimEnd() : result;
    }
}
