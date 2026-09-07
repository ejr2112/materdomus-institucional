// Feature: product-detail-view

using System.Globalization;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using MaterDomus.Web.Helpers;
using MaterDomus.Web.Models;

namespace MaterDomus.Tests.Unit;

/// <summary>
/// Testes de propriedade (FsCheck) para <see cref="ProductCopyGenerator.Generate"/>.
/// Cada propriedade de correção do design "product-detail-view" é implementada por
/// exatamente um teste de propriedade, anotado no formato
/// <c>// Feature: product-detail-view, Property {n}: {texto}</c>.
///
/// Validates: Requirements 2.1, 2.2, 2.3, 2.4, 3.1, 3.2, 3.3, 3.4, 4.1, 4.2, 4.3, 4.4, 4.5
/// </summary>
public class ProductCopyGeneratorPropertyTests
{
    // -------------------------------------------------------------------------
    // Constantes espelhando o contrato observável do gerador
    // -------------------------------------------------------------------------

    /// <summary>Frase de CTA sempre presente como última frase (Req 2.4).</summary>
    private const string Cta = "Garanta o seu agora e aproveite!";

    /// <summary>Comprimento máximo da saída não-nula (Req 2.1).</summary>
    private const int MaxLength = 600;

    // -------------------------------------------------------------------------
    // Geradores de componentes de Product
    // -------------------------------------------------------------------------

    private static Gen<string> NonEmptyTextGen() =>
        Gen.Choose(1, 40)
           .SelectMany(len => Gen.ArrayOf(Gen.Elements("abcdefghijklmnopqrstuvwxyzáéíóúçãõ ".ToCharArray()), len))
           .Select(chars => new string(chars).Trim())
           .Where(s => s.Length > 0);

    /// <summary>Nome com ao menos um caractere não-branco.</summary>
    private static Gen<string> ValidNameGen() =>
        Gen.Choose(1, 30)
           .SelectMany(len => Gen.ArrayOf(Gen.Elements("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 ".ToCharArray()), len))
           .Select(chars => new string(chars))
           .Where(s => s.Trim().Length >= 1);

    /// <summary>Nome em branco: nulo, vazio ou só espaços/whitespace.</summary>
    private static Gen<string?> BlankNameGen() =>
        Gen.OneOf(
            Gen.Constant((string?)null),
            Gen.Constant((string?)""),
            Gen.Elements(" ", "   ", "\t", "\n", " \t \n ").Select(s => (string?)s));

    /// <summary>Categoria não-branca (não contém a frase-âncora de categoria).</summary>
    private static Gen<string> NonBlankCategoryGen() =>
        NonEmptyTextGen();

    private static Gen<string?> BlankGen() =>
        Gen.OneOf(
            Gen.Constant((string?)null),
            Gen.Constant((string?)""),
            Gen.Elements(" ", "   ", "\t", "\n").Select(s => (string?)s));

    private static Gen<decimal> PositivePriceGen() =>
        Gen.Choose(1, 500000).Select(cents => cents / 100m);

    private static Gen<decimal> NonPositivePriceGen() =>
        Gen.OneOf(
            Gen.Constant(0m),
            Gen.Choose(1, 500000).Select(cents => -(cents / 100m)));

    // -------------------------------------------------------------------------
    // Geradores de Product completos
    // -------------------------------------------------------------------------

    private static Product Build(string? name, string? description, string? category, decimal price) =>
        new Product(
            Id: "id-1",
            Name: name!,
            Description: description!,
            ImageUrl: "",
            Category: category!,
            Price: price,
            AmazonUrl: "");

    /// <summary>Product totalmente arbitrário (nome válido/branco, campos variados).</summary>
    private static Gen<Product> AnyProductGen()
    {
        var nameGen = Gen.OneOf(
            ValidNameGen().Select(s => (string?)s),
            BlankNameGen());
        var descGen = Gen.OneOf(
            NonEmptyTextGen().Select(s => (string?)s),
            BlankGen());
        var catGen = Gen.OneOf(
            NonBlankCategoryGen().Select(s => (string?)s),
            BlankGen());
        var priceGen = Gen.OneOf(PositivePriceGen(), NonPositivePriceGen());

        return nameGen.SelectMany(name =>
               descGen.SelectMany(desc =>
               catGen.SelectMany(cat =>
               priceGen.Select(price => Build(name, desc, cat, price)))));
    }

    // -------------------------------------------------------------------------
    // Property 1: Determinismo da geração
    // -------------------------------------------------------------------------

    // Feature: product-detail-view, Property 1: Determinismo da geração
    /// <summary>
    /// Para qualquer <c>Product</c>, chamar <c>Generate</c> duas vezes produz saída
    /// idêntica, inclusive quando a cultura ambiente difere entre as chamadas
    /// (pt-BR vs en-US), independente de hora ou aleatoriedade.
    ///
    /// **Validates: Requirements 3.1, 3.3**
    /// </summary>
    [Property(MaxTest = 200)]
    public Property Property1_Determinismo()
    {
        return Prop.ForAll(AnyProductGen().ToArbitrary(), product =>
        {
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("pt-BR");
                CultureInfo.CurrentUICulture = new CultureInfo("pt-BR");
                var first = ProductCopyGenerator.Generate(product);

                CultureInfo.CurrentCulture = new CultureInfo("en-US");
                CultureInfo.CurrentUICulture = new CultureInfo("en-US");
                var second = ProductCopyGenerator.Generate(product);

                return (first == second)
                    .ToProperty()
                    .Label($"Determinismo violado: '{first}' != '{second}'");
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }
        });
    }

    // -------------------------------------------------------------------------
    // Property 2: Saída bem-formada quando há nome
    // -------------------------------------------------------------------------

    // Feature: product-detail-view, Property 2: Saída bem-formada quando há nome
    /// <summary>
    /// Para qualquer <c>Product</c> cujo nome contenha ao menos um caractere não-branco,
    /// <c>Generate</c> retorna string de 1 a 600 caracteres, com pelo menos um caractere
    /// não-branco e sem tokens/placeholders não resolvidos (sem <c>{</c>, <c>}</c>).
    ///
    /// **Validates: Requirements 2.1, 4.4**
    /// </summary>
    [Property(MaxTest = 200)]
    public Property Property2_SaidaBemFormada()
    {
        var descGen = Gen.OneOf(NonEmptyTextGen().Select(s => (string?)s), BlankGen());
        var catGen = Gen.OneOf(NonBlankCategoryGen().Select(s => (string?)s), BlankGen());
        var priceGen = Gen.OneOf(PositivePriceGen(), NonPositivePriceGen());

        var gen = ValidNameGen().SelectMany(name =>
                  descGen.SelectMany(desc =>
                  catGen.SelectMany(cat =>
                  priceGen.Select(price => Build(name, desc, cat, price)))));

        return Prop.ForAll(gen.ToArbitrary(), product =>
        {
            var result = ProductCopyGenerator.Generate(product);

            if (result is null)
                return false.ToProperty().Label("Nome válido não deveria produzir null");

            var lengthOk = result.Length >= 1 && result.Length <= MaxLength;
            var hasNonWhitespace = result.Any(c => !char.IsWhiteSpace(c));
            var noTokens = !result.Contains('{') && !result.Contains('}');

            return (lengthOk && hasNonWhitespace && noTokens)
                .ToProperty()
                .Label($"len={result.Length}, nonWs={hasNonWhitespace}, noTokens={noTokens} :: '{result}'");
        });
    }

    // -------------------------------------------------------------------------
    // Property 3: Nome em branco produz ausência de texto
    // -------------------------------------------------------------------------

    // Feature: product-detail-view, Property 3: Nome em branco produz ausência de texto
    /// <summary>
    /// Para qualquer <c>Product</c> cujo nome seja nulo, vazio ou só espaços,
    /// <c>Generate</c> retorna <c>null</c>.
    ///
    /// **Validates: Requirements 4.5**
    /// </summary>
    [Property(MaxTest = 200)]
    public Property Property3_NomeEmBranco()
    {
        var descGen = Gen.OneOf(NonEmptyTextGen().Select(s => (string?)s), BlankGen());
        var catGen = Gen.OneOf(NonBlankCategoryGen().Select(s => (string?)s), BlankGen());
        var priceGen = Gen.OneOf(PositivePriceGen(), NonPositivePriceGen());

        var gen = BlankNameGen().SelectMany(name =>
                  descGen.SelectMany(desc =>
                  catGen.SelectMany(cat =>
                  priceGen.Select(price => Build(name, desc, cat, price)))));

        return Prop.ForAll(gen.ToArbitrary(), product =>
        {
            var result = ProductCopyGenerator.Generate(product);
            return (result is null)
                .ToProperty()
                .Label($"Nome em branco deveria produzir null, obteve: '{result}'");
        });
    }

    // -------------------------------------------------------------------------
    // Property 4: Inclusão de campos válidos
    // -------------------------------------------------------------------------

    // Feature: product-detail-view, Property 4: Inclusão de campos válidos
    /// <summary>
    /// Para qualquer <c>Product</c> com nome válido: categoria não-branca aparece no
    /// texto; <c>Price &gt; 0</c> aparece como <c>ProductHelpers.FormatPrice(Price)</c>;
    /// descrição não-branca aparece completa (não truncada). Entradas são limitadas
    /// para caber no limite de 600 chars, evitando truncagem defensiva do gerador.
    ///
    /// **Validates: Requirements 2.2, 2.3, 3.2, 3.4**
    /// </summary>
    [Property(MaxTest = 200)]
    public Property Property4_InclusaoDeCamposValidos()
    {
        // Nome/categoria/descrição limitados para que a composição completa fique <= 600 chars.
        var nameGen = Gen.Choose(1, 20)
            .SelectMany(len => Gen.ArrayOf(Gen.Elements("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz ".ToCharArray()), len))
            .Select(c => new string(c)).Where(s => s.Trim().Length >= 1);

        // Descrição possivelmente > 120 chars (Req 3.4) mas limitada para caber no total.
        var descGen = Gen.Choose(1, 200)
            .SelectMany(len => Gen.ArrayOf(Gen.Elements("abcdefghijklmnopqrstuvwxyz ".ToCharArray()), len))
            .Select(c => new string(c)).Where(s => s.Trim().Length >= 1);

        var catGen = Gen.Choose(1, 20)
            .SelectMany(len => Gen.ArrayOf(Gen.Elements("abcdefghijklmnopqrstuvwxyz ".ToCharArray()), len))
            .Select(c => new string(c)).Where(s => s.Trim().Length >= 1);

        var gen = nameGen.SelectMany(name =>
                  descGen.SelectMany(desc =>
                  catGen.SelectMany(cat =>
                  PositivePriceGen().Select(price =>
                      Build(name, desc, cat, price)))));

        return Prop.ForAll(gen.ToArbitrary(), product =>
        {
            var result = ProductCopyGenerator.Generate(product);
            if (result is null)
                return false.ToProperty().Label("Nome válido não deveria produzir null");

            var categoryPresent = result.Contains(product.Category.Trim());
            var formattedPrice = ProductHelpers.FormatPrice(product.Price);
            var pricePresent = result.Contains(formattedPrice);
            var descriptionPresent = result.Contains(product.Description.Trim());

            return (categoryPresent && pricePresent && descriptionPresent)
                .ToProperty()
                .Label($"cat={categoryPresent}, price={pricePresent} ('{formattedPrice}'), desc={descriptionPresent} :: '{result}'");
        });
    }

    // -------------------------------------------------------------------------
    // Property 5: Omissão limpa de descrição ausente
    // -------------------------------------------------------------------------

    // Feature: product-detail-view, Property 5: Omissão limpa de descrição ausente
    /// <summary>
    /// Para qualquer <c>Product</c> com nome válido cuja descrição seja nula/vazia/só
    /// espaços, o texto gerado não contém resíduo de descrição nem token/placeholder
    /// de descrição. Verificado comparando com a saída de um produto idêntico exceto
    /// pela descrição: os textos devem ser iguais (a descrição em branco não altera o
    /// resultado) e sem tokens.
    ///
    /// **Validates: Requirements 4.1**
    /// </summary>
    [Property(MaxTest = 200)]
    public Property Property5_OmissaoDeDescricao()
    {
        var nameGen = Gen.Choose(1, 20)
            .SelectMany(len => Gen.ArrayOf(Gen.Elements("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz ".ToCharArray()), len))
            .Select(c => new string(c)).Where(s => s.Trim().Length >= 1);
        var catGen = Gen.OneOf(NonBlankCategoryGen().Select(s => (string?)s), BlankGen());
        var priceGen = Gen.OneOf(PositivePriceGen(), NonPositivePriceGen());

        var gen = nameGen.SelectMany(name =>
                  BlankGen().SelectMany(blankDesc =>
                  catGen.SelectMany(cat =>
                  priceGen.Select(price => (Product: Build(name, blankDesc, cat, price), Name: name, Cat: cat, Price: price)))));

        return Prop.ForAll(gen.ToArbitrary(), t =>
        {
            var withBlankDesc = ProductCopyGenerator.Generate(t.Product);
            // Produto de referência com descrição totalmente ausente (null).
            var reference = ProductCopyGenerator.Generate(Build(t.Name, null, t.Cat, t.Price));

            if (withBlankDesc is null || reference is null)
                return false.ToProperty().Label("Nome válido não deveria produzir null");

            var noTokens = !withBlankDesc.Contains('{') && !withBlankDesc.Contains('}');
            var sameAsNoDescription = withBlankDesc == reference;

            return (noTokens && sameAsNoDescription)
                .ToProperty()
                .Label($"noTokens={noTokens}, sameAsNoDesc={sameAsNoDescription} :: '{withBlankDesc}' vs '{reference}'");
        });
    }

    // -------------------------------------------------------------------------
    // Property 6: Omissão limpa de categoria ausente
    // -------------------------------------------------------------------------

    // Feature: product-detail-view, Property 6: Omissão limpa de categoria ausente
    /// <summary>
    /// Para qualquer <c>Product</c> com nome válido cuja categoria seja nula/vazia/só
    /// espaços, o texto gerado não faz referência à categoria nem contém
    /// token/placeholder de categoria. Verificado pela ausência da frase-âncora de
    /// categoria (" em ") introduzida somente quando há categoria e pela ausência de
    /// tokens.
    ///
    /// **Validates: Requirements 4.2**
    /// </summary>
    [Property(MaxTest = 200)]
    public Property Property6_OmissaoDeCategoria()
    {
        // Nome sem a subsequência " em " para não gerar falso positivo na âncora.
        var nameGen = Gen.Choose(1, 20)
            .SelectMany(len => Gen.ArrayOf(Gen.Elements("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray()), len))
            .Select(c => new string(c)).Where(s => s.Trim().Length >= 1);
        var descGen = Gen.OneOf(
            Gen.Choose(1, 60).SelectMany(len => Gen.ArrayOf(Gen.Elements("abcdfghijklnopqrstuvwxyz".ToCharArray()), len)).Select(c => (string?)new string(c)),
            BlankGen());
        var priceGen = Gen.OneOf(PositivePriceGen(), NonPositivePriceGen());

        var gen = nameGen.SelectMany(name =>
                  descGen.SelectMany(desc =>
                  BlankGen().SelectMany(blankCat =>
                  priceGen.Select(price => Build(name, desc, blankCat, price)))));

        return Prop.ForAll(gen.ToArbitrary(), product =>
        {
            var result = ProductCopyGenerator.Generate(product);
            if (result is null)
                return false.ToProperty().Label("Nome válido não deveria produzir null");

            // A frase-âncora de categoria é "escolha especial em " — só aparece com categoria.
            var noCategoryAnchor = !result.Contains("escolha especial em ");
            var noTokens = !result.Contains('{') && !result.Contains('}');

            return (noCategoryAnchor && noTokens)
                .ToProperty()
                .Label($"noCategoryAnchor={noCategoryAnchor}, noTokens={noTokens} :: '{result}'");
        });
    }

    // -------------------------------------------------------------------------
    // Property 7: Omissão de preço não-positivo
    // -------------------------------------------------------------------------

    // Feature: product-detail-view, Property 7: Omissão de preço não-positivo
    /// <summary>
    /// Para qualquer <c>Product</c> com nome válido cujo <c>Price &lt;= 0</c>, o texto
    /// gerado não contém referência de preço (nem o valor formatado, nem placeholder).
    ///
    /// **Validates: Requirements 4.3**
    /// </summary>
    [Property(MaxTest = 200)]
    public Property Property7_OmissaoDePreco()
    {
        var nameGen = Gen.Choose(1, 20)
            .SelectMany(len => Gen.ArrayOf(Gen.Elements("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz ".ToCharArray()), len))
            .Select(c => new string(c)).Where(s => s.Trim().Length >= 1);
        var descGen = Gen.OneOf(
            Gen.Choose(1, 60).SelectMany(len => Gen.ArrayOf(Gen.Elements("abcdefghijklmnopqrstuvwxyz ".ToCharArray()), len)).Select(c => (string?)new string(c)),
            BlankGen());
        var catGen = Gen.OneOf(NonBlankCategoryGen().Select(s => (string?)s), BlankGen());

        var gen = nameGen.SelectMany(name =>
                  descGen.SelectMany(desc =>
                  catGen.SelectMany(cat =>
                  NonPositivePriceGen().Select(price => Build(name, desc, cat, price)))));

        return Prop.ForAll(gen.ToArbitrary(), product =>
        {
            var result = ProductCopyGenerator.Generate(product);
            if (result is null)
                return false.ToProperty().Label("Nome válido não deveria produzir null");

            // O símbolo de moeda "R$" só aparece via segmento de preço; e a frase-âncora de preço.
            var noCurrencySymbol = !result.Contains("R$");
            var noPriceAnchor = !result.Contains("Tudo isso por apenas");
            var noTokens = !result.Contains('{') && !result.Contains('}');

            return (noCurrencySymbol && noPriceAnchor && noTokens)
                .ToProperty()
                .Label($"noR$={noCurrencySymbol}, noPriceAnchor={noPriceAnchor}, price={product.Price} :: '{result}'");
        });
    }

    // -------------------------------------------------------------------------
    // Property 8: CTA é a última frase
    // -------------------------------------------------------------------------

    // Feature: product-detail-view, Property 8: CTA é a última frase
    /// <summary>
    /// Para qualquer <c>Product</c> com saída não-nula, a última frase do texto gerado
    /// é a frase de incentivo à compra (CTA), distinta do restante do conteúdo.
    ///
    /// **Validates: Requirements 2.4**
    /// </summary>
    [Property(MaxTest = 200)]
    public Property Property8_CtaUltimaFrase()
    {
        return Prop.ForAll(AnyProductGen().ToArbitrary(), product =>
        {
            var result = ProductCopyGenerator.Generate(product);
            if (result is null)
                return true.ToProperty().Label("Saída nula: propriedade não se aplica");

            var endsWithCta = result.TrimEnd().EndsWith(Cta, StringComparison.Ordinal);

            // Distinta do restante: removendo a última ocorrência do CTA, o texto anterior
            // (se houver) não é apenas o CTA repetido.
            var trimmed = result.TrimEnd();
            var lastCtaIndex = trimmed.LastIndexOf(Cta, StringComparison.Ordinal);
            var before = lastCtaIndex >= 0 ? trimmed.Substring(0, lastCtaIndex).TrimEnd() : trimmed;
            var ctaDistinct = !before.EndsWith(Cta, StringComparison.Ordinal);

            return (endsWithCta && ctaDistinct)
                .ToProperty()
                .Label($"endsWithCta={endsWithCta}, ctaDistinct={ctaDistinct} :: '{result}'");
        });
    }
}
