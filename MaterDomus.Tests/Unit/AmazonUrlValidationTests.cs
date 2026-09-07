// Feature: product-catalog-amazon-sync

using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using MaterDomus.Web.Services;

namespace MaterDomus.Tests.Unit;

/// <summary>
/// Property-based tests for <see cref="ProductCatalogService.IsValidAmazonUrl"/>.
/// Verifies the boundary of the canonical Amazon product URL format.
///
/// Validates: Requirements 1.5, 3.1, 3.4
/// </summary>
public class AmazonUrlValidationTests
{
    // -------------------------------------------------------------------------
    // Generator helpers
    // -------------------------------------------------------------------------

    private static readonly char[] UpperAlphanumChars =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();

    private static readonly char[] LowerAlphaChars =
        "abcdefghijklmnopqrstuvwxyz".ToCharArray();

    /// <summary>
    /// Generates exactly 10 uppercase alphanumeric characters — a valid ASIN.
    /// </summary>
    private static Gen<string> ValidAsinGen() =>
        Gen.ArrayOf(Gen.Elements(UpperAlphanumChars), 10)
           .Select(chars => new string(chars));

    /// <summary>
    /// Generates a seller/store id: 1..14 uppercase alphanumeric characters
    /// (e.g. Master Domus is "A20TN3HCSY6KZV").
    /// </summary>
    private static Gen<string> SellerIdGen() =>
        Gen.Choose(1, 14)
           .SelectMany(len => Gen.ArrayOf(Gen.Elements(UpperAlphanumChars), len))
           .Select(chars => new string(chars));

    /// <summary>
    /// Generates the canonical valid Amazon URL: https://www.amazon.com.br/dp/{ASIN10},
    /// optionally followed by the store parameter ?m={sellerId}.
    /// All of these MUST return true from IsValidAmazonUrl.
    /// </summary>
    private static Gen<(string Url, bool ShouldBeValid)> ValidUrlGen() =>
        Gen.OneOf(
            // Bare canonical URL
            ValidAsinGen()
                .Select(asin => ($"https://www.amazon.com.br/dp/{asin}", true)),

            // Canonical URL with the store/seller parameter
            ValidAsinGen()
                .SelectMany(asin => SellerIdGen()
                    .Select(seller => ($"https://www.amazon.com.br/dp/{asin}?m={seller}", true)))
        );

    /// <summary>
    /// Generates URLs that deviate from the canonical format in one specific way.
    /// Each variant MUST return false from IsValidAmazonUrl.
    /// </summary>
    private static Gen<(string Url, bool ShouldBeValid)> InvalidUrlGen() =>
        Gen.OneOf(
            // ASIN too short (9 chars)
            Gen.ArrayOf(Gen.Elements(UpperAlphanumChars), 9)
               .Select(chars => ($"https://www.amazon.com.br/dp/{new string(chars)}", false)),

            // ASIN too long (11 chars)
            Gen.ArrayOf(Gen.Elements(UpperAlphanumChars), 11)
               .Select(chars => ($"https://www.amazon.com.br/dp/{new string(chars)}", false)),

            // Lowercase letters in ASIN (10 lowercase chars)
            Gen.ArrayOf(Gen.Elements(LowerAlphaChars), 10)
               .Select(chars => ($"https://www.amazon.com.br/dp/{new string(chars)}", false)),

            // Mixed case — first char lowercase, rest valid uppercase (9 chars)
            Gen.ArrayOf(Gen.Elements(UpperAlphanumChars), 9)
               .Select(chars => ($"https://www.amazon.com.br/dp/a{new string(chars)}", false)),

            // Query parameters appended to otherwise-valid URL
            ValidAsinGen()
               .Select(asin => ($"https://www.amazon.com.br/dp/{asin}?tag=test-20", false)),

            // Store parameter followed by volatile session params (ref/qid/sr/dib) — must be rejected
            ValidAsinGen()
               .Select(asin => ($"https://www.amazon.com.br/dp/{asin}?m=A20TN3HCSY6KZV&ref=sr_1_1&qid=1788779701", false)),

            // Store parameter with lowercase value — must be rejected
            ValidAsinGen()
               .Select(asin => ($"https://www.amazon.com.br/dp/{asin}?m=abc123", false)),

            // Path suffix (slug) before /dp — must be rejected
            ValidAsinGen()
               .Select(asin => ($"https://www.amazon.com.br/Dispenser-Flow/dp/{asin}", false)),

            // Path suffix appended
            ValidAsinGen()
               .Select(asin => ($"https://www.amazon.com.br/dp/{asin}/ref=sr_1_1", false)),

            // Wrong scheme (http instead of https)
            ValidAsinGen()
               .Select(asin => ($"http://www.amazon.com.br/dp/{asin}", false)),

            // Wrong domain (amazon.com instead of amazon.com.br)
            ValidAsinGen()
               .Select(asin => ($"https://www.amazon.com/dp/{asin}", false)),

            // Wrong domain (amazon.es)
            ValidAsinGen()
               .Select(asin => ($"https://www.amazon.es/dp/{asin}", false)),

            // Completely different domain
            ValidAsinGen()
               .Select(asin => ($"https://example.com/dp/{asin}", false)),

            // Empty string
            Gen.Constant(("", false)),

            // Null (represented as literal null-cast)
            Gen.Constant(((string?)null!, false)),

            // Random non-URL string
            ArbMap.Default.ArbFor<NonEmptyString>().Generator
               .Select(s => (s.Get, false))
        );

    /// <summary>
    /// Mixed generator: 50% valid URLs, 50% invalid URLs, in random order.
    ///
    /// **Validates: Requirements 1.5, 3.1, 3.4**
    /// </summary>
    private static Gen<(string? Url, bool ShouldBeValid)> AmazonUrlGen() =>
        Gen.Frequency(
            (1, ValidUrlGen().Select(t => ((string?)t.Url, t.ShouldBeValid))),
            (1, InvalidUrlGen().Select(t => ((string?)t.Url, t.ShouldBeValid)))
        );

    // -------------------------------------------------------------------------
    // Property 3: IsValidAmazonUrl — correctness at the boundary of the canonical format
    // -------------------------------------------------------------------------

    // Feature: product-catalog-amazon-sync, Property 3
    /// <summary>
    /// For any generated URL (valid or invalid), <c>IsValidAmazonUrl</c> must return
    /// <c>true</c> if and only if the URL is exactly
    /// <c>https://www.amazon.com.br/dp/[A-Z0-9]{10}</c>, optionally followed by the
    /// store parameter <c>?m=[A-Z0-9]+</c>, and nothing more.
    ///
    /// This covers: valid ASINs (10 uppercase alphanumeric), ASINs with 9 or 11 chars,
    /// lowercase ASINs, query parameters, path suffixes, wrong schemes, wrong domains,
    /// empty string, null, and random strings.
    ///
    /// **Validates: Requirements 1.5, 3.1, 3.4**
    /// </summary>
    [Property(MaxTest = 200)]
    public Property IsValidAmazonUrl_ReturnsTrueIfAndOnlyIfCanonicalFormat()
    {
        return Prop.ForAll(AmazonUrlGen().ToArbitrary(), tuple =>
        {
            var (url, shouldBeValid) = tuple;
            var actual = ProductCatalogService.IsValidAmazonUrl(url);

            return (actual == shouldBeValid)
                .ToProperty()
                .Label($"URL='{url ?? "(null)"}', expected={shouldBeValid}, actual={actual}");
        });
    }

    // Feature: product-catalog-amazon-sync, Property 3 — deterministic true case
    /// <summary>
    /// A canonical valid URL must always return true.
    ///
    /// **Validates: Requirements 1.5, 3.1**
    /// </summary>
    [Property(MaxTest = 200)]
    public Property IsValidAmazonUrl_ValidCanonicalUrls_AlwaysReturnTrue()
    {
        return Prop.ForAll(ValidUrlGen().ToArbitrary(), tuple =>
        {
            var (url, _) = tuple;
            return ProductCatalogService.IsValidAmazonUrl(url)
                .ToProperty()
                .Label($"Valid canonical URL '{url}' should return true");
        });
    }

    // Feature: product-catalog-amazon-sync, Property 3 — deterministic false cases
    /// <summary>
    /// Every invalid URL variant must always return false.
    ///
    /// **Validates: Requirements 1.5, 3.1, 3.4**
    /// </summary>
    [Property(MaxTest = 200)]
    public Property IsValidAmazonUrl_InvalidUrlVariants_AlwaysReturnFalse()
    {
        return Prop.ForAll(InvalidUrlGen().ToArbitrary(), tuple =>
        {
            var (url, _) = tuple;
            return (!ProductCatalogService.IsValidAmazonUrl(url))
                .ToProperty()
                .Label($"Invalid URL '{url ?? "(null)"}' should return false");
        });
    }
}
