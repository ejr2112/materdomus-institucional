using Bunit;
using MaterDomus.Web.Pages;
using Xunit;

namespace MaterDomus.Tests.Unit;

/// <summary>
/// Garante que a página Contato mantém e-mail/WhatsApp e expõe o Microsoft Forms
/// a partir de um único valor de configuração.
/// </summary>
public class ContatoPageTests
{
    private static IRenderedComponent<Contato> RenderContato()
    {
        var ctx = new Bunit.TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        return ctx.RenderComponent<Contato>();
    }

    [Fact]
    public void Contato_KeepsEmailAndWhatsAppChannels()
    {
        var cut = RenderContato();
        var markup = cut.Markup;

        Assert.Contains("mailto:contato@materdomus.com.br", markup);
        Assert.Contains("https://wa.me/5519993491775", markup);
        Assert.Contains("contato@materdomus.com.br", markup);
        Assert.Contains("WhatsApp", markup);
    }

    [Fact]
    public void Contato_UsesSingleMicrosoftFormsUrlForIframeAndOpenButton()
    {
        var cut = RenderContato();

        Assert.Equal(
            "https://forms.office.com/r/PLACEHOLDER",
            Contato.MicrosoftFormsContactUrl);

        var iframe = cut.Find("iframe.forms-embed");
        Assert.Equal(Contato.MicrosoftFormsContactUrl, iframe.GetAttribute("src"));
        Assert.Equal("Formulário de contato para fornecedores e parceiros", iframe.GetAttribute("title"));

        var openButton = cut.Find("a.forms-open-btn");
        Assert.Equal(Contato.MicrosoftFormsContactUrl, openButton.GetAttribute("href"));
        Assert.Equal("_blank", openButton.GetAttribute("target"));
        Assert.Equal("Abrir formulário", openButton.TextContent.Trim());
    }

    [Fact]
    public void Contato_TracksMicrosoftFormsWhenOpenButtonIsClicked()
    {
        var ctx = new Bunit.TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = ctx.RenderComponent<Contato>();

        cut.Find("a.forms-open-btn").Click();

        var invocation = ctx.JSInterop.Invocations
            .First(call => call.Identifier == "trackEvent");

        Assert.Equal("trackEvent", invocation.Identifier);
        Assert.Equal("supplier_contact_click", invocation.Arguments[0]);

        var payload = invocation.Arguments[1];
        Assert.NotNull(payload);

        var json = System.Text.Json.JsonSerializer.Serialize(payload);
        Assert.Contains("microsoft_forms", json);
        Assert.Contains("contato", json);
    }
}
