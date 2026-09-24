using Bunit;
using MaterDomus.Web.Pages;
using MaterDomus.Web.Shared;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace MaterDomus.Tests.Unit;

/// <summary>
/// LGPD baseline: cookie banner, GTM only after consent, /privacidade and footer links.
/// This is UX/compliance hygiene — not a legal certification.
/// </summary>
public class CookieConsentAndPrivacyTests
{
    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "MaterDomus.Web.csproj")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException(
            "Não foi possível localizar a raiz do repositório (MaterDomus.Web.csproj).");
    }

    private static string ReadRepo(params string[] segments) =>
        File.ReadAllText(Path.Combine(new[] { FindRepoRoot() }.Concat(segments).ToArray()));

    [Fact]
    public void CookieConsentJs_LoadsGtmOnlyAfterAcceptAndPersistsChoice()
    {
        var js = ReadRepo("wwwroot", "js", "cookie-consent.js");

        Assert.Contains("GTM-5FJ38X8C", js);
        Assert.Contains("www.googletagmanager.com/gtm.js", js);
        Assert.Contains("localStorage", js);
        Assert.Contains("materdomus.cookieConsent", js);
        Assert.Contains("accepted", js);
        Assert.Contains("rejected", js);
        Assert.Contains("materDomusOpenCookiePreferences", js);
        Assert.Contains("cookie-accept", js);
        Assert.Contains("cookie-reject", js);
        Assert.DoesNotContain("www.googletagmanager.com/ns.html", js);
    }

    [Fact]
    public void IndexHtml_HasAccessibleCookieBannerWithoutAutoGtm()
    {
        var html = ReadRepo("wwwroot", "index.html");

        Assert.Contains("id=\"cookie-banner\"", html);
        Assert.Contains("role=\"region\"", html);
        Assert.Contains("aria-labelledby=\"cookie-banner-title\"", html);
        Assert.Contains("aria-describedby=\"cookie-banner-text\"", html);
        Assert.Contains("Política de Privacidade", html);
        Assert.Contains("href=\"/privacidade\"", html);
        Assert.Contains("id=\"cookie-accept\"", html);
        Assert.Contains(">Aceitar<", html);
        Assert.Contains("id=\"cookie-reject\"", html);
        Assert.Contains(">Recusar<", html);
        Assert.Contains("src=\"js/cookie-consent.js\"", html);

        Assert.DoesNotContain("googletagmanager.com/gtm.js", html);
        Assert.DoesNotContain("googletagmanager.com/ns.html", html);
        Assert.DoesNotContain("dataLayer','GTM-5FJ38X8C'", html);
    }

    [Fact]
    public void SiteCss_StylesCookieBannerBelowBootScreen()
    {
        var css = ReadRepo("wwwroot", "css", "site.css");

        Assert.Contains(".cookie-banner", css);
        Assert.Contains("z-index: 300", css);
        Assert.Contains(".boot-screen", css);
        Assert.Contains("z-index: 1000", css);
        Assert.Contains(".cookie-banner[hidden]", css);
        Assert.Contains("min-height: 44px", css);
        Assert.Contains(".footer-links", css);
        Assert.DoesNotContain(".privacy-page__note", css);
    }

    [Fact]
    public void PrivacidadePage_HasSeoMetaControllerRightsAndCookiesSection()
    {
        var razor = ReadRepo("Pages", "Privacidade.razor");

        Assert.Contains("@page \"/privacidade\"", razor);
        Assert.Contains("SeoMeta", razor);
        Assert.Contains("Política de Privacidade | Mater Domus", razor);
        Assert.Contains("https://www.materdomus.com.br/privacidade", razor);
        Assert.Contains("MASTER DOMUS LTDA", razor);
        Assert.Contains("Mater Domus", razor);
        Assert.Contains("contato@materdomus.com.br", razor);
        Assert.Contains("wa.me/5519993491775", razor);
        Assert.Contains("Google Tag Manager", razor);
        Assert.Contains("GTM-5FJ38X8C", razor);
        Assert.Contains("Microsoft Forms", razor);
        Assert.Contains("Amazon", razor);
        Assert.Contains("consentimento", razor);
        Assert.Contains("Última atualização", razor);
        Assert.Contains("Preferências de cookies", razor);
        Assert.DoesNotContain("privacy-page__note", razor);
        Assert.DoesNotContain("modelo de transparência", razor);
        Assert.DoesNotContain("aconselhamento jurídico", razor);
        Assert.DoesNotContain("<HeadContent>", razor);
    }

    [Fact]
    public void PrivacidadePage_RendersUniqueSeoMeta()
    {
        using var ctx = new Bunit.TestContext();
        var cut = ctx.RenderComponent<Privacidade>();
        var seo = cut.FindComponent<SeoMeta>().Instance;

        Assert.Equal("Política de Privacidade | Mater Domus", seo.Title);
        Assert.Equal("https://www.materdomus.com.br/privacidade", seo.Canonical);
        Assert.Contains("LGPD", seo.Description);
        Assert.Null(seo.Stylesheet);

        Assert.Contains("MASTER DOMUS LTDA", cut.Markup);
        Assert.Contains("mailto:contato@materdomus.com.br", cut.Markup);
        Assert.DoesNotContain("aconselhamento jurídico", cut.Markup);
        var heading = cut.Find("h1");
        Assert.Equal("Política de Privacidade", heading.TextContent.Trim());
    }

    [Fact]
    public void MainLayout_Footer_HasPrivacyLinkAndCookiePreferences()
    {
        using var ctx = new Bunit.TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        RenderFragment body = builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddContent(1, "corpo");
            builder.CloseElement();
        };

        var cut = ctx.RenderComponent<MainLayout>(parameters =>
            parameters.Add(p => p.Body, body));

        var privacy = cut.Find("footer.footer a[href='/privacidade']");
        Assert.Contains("Política de Privacidade", privacy.TextContent);

        var shipping = cut.Find("footer.footer a[href='/frete-e-trocas']");
        Assert.Equal("Frete e trocas", shipping.TextContent.Trim());

        var cookiesBtn = cut.Find("footer.footer button.footer-cookies-btn");
        Assert.Equal("Preferências de cookies", cookiesBtn.TextContent.Trim());

        cookiesBtn.Click();

        Assert.Contains(
            ctx.JSInterop.Invocations,
            call => call.Identifier == "materDomusOpenCookiePreferences");
    }

    [Fact]
    public void FreteETrocasPage_StatesAmazonHandlesPurchaseShippingAndReturns()
    {
        var razor = ReadRepo("Pages", "FreteETrocas.razor");

        Assert.Contains("@page \"/frete-e-trocas\"", razor);
        Assert.Contains("https://www.materdomus.com.br/frete-e-trocas", razor);
        Assert.Contains("Frete e trocas | Mater Domus", razor);
        Assert.DoesNotContain("<HeadContent>", razor);
        Assert.DoesNotContain("frete grátis", razor, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("checkout", razor, StringComparison.OrdinalIgnoreCase);

        using var ctx = new Bunit.TestContext();
        var cut = ctx.RenderComponent<FreteETrocas>();
        var seo = cut.FindComponent<SeoMeta>().Instance;

        Assert.Equal("Frete e trocas | Mater Domus", seo.Title);
        Assert.Equal("https://www.materdomus.com.br/frete-e-trocas", seo.Canonical);
        Assert.Contains("Amazon", seo.Description);
        Assert.Null(seo.Stylesheet);

        var markup = cut.Markup;
        Assert.Equal("Frete, trocas e devoluções", cut.Find("h1").TextContent.Trim());
        Assert.Contains("vitrine", markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Amazon Brasil", markup);
        Assert.Contains("Pagamento, frete, entrega, troca e devolução", markup);
        Assert.Contains("https://www.amazon.com.br/gp/help/customer/display.html", markup);
        Assert.Contains("href=\"/contato\"", markup);
        Assert.Contains("href=\"/privacidade\"", markup);
        Assert.Contains("24 de setembro de 2026", markup);
        Assert.DoesNotContain("materdomus.com.br/checkout", markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("frete grátis", markup, StringComparison.OrdinalIgnoreCase);

        var privacy = ReadRepo("Pages", "Privacidade.razor");
        Assert.Contains("href=\"/frete-e-trocas\"", privacy);
        var contato = ReadRepo("Pages", "Contato.razor");
        Assert.Contains("href=\"/frete-e-trocas\"", contato);
    }
}
