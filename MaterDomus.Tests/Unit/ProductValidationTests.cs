// Feature: product-catalog-amazon-sync

using System.Net;
using System.Text;
using System.Text.Json;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using MaterDomus.Web.Models;
using MaterDomus.Web.Services;
using Xunit;

namespace MaterDomus.Tests.Unit;

/// <summary>
/// Property-based tests (FsCheck) for the integrity-validation logic inside
/// <see cref="ProductCatalogService"/>.
///
/// <c>Validate</c> is private; it is exercised indirectly through
/// <see cref="ProductCatalogService.GetProductsAsync"/> using a stub
/// <see cref="HttpMessageHandler"/> that returns a controlled JSON payload.
///
/// Validates: Requirements 1.5, 2.7, 2.8, 6.1, 6.2, 6.3
/// </summary>
public class ProductValidationTests
{
    // -------------------------------------------------------------------------
    // HttpClient stub
    // -------------------------------------------------------------------------

    private sealed class StubHttpHandler(string jsonBody) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
            });
    }

    private static readonly JsonSerializerOptions CamelCaseOptions =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    /// <summary>
    /// Serialises <paramref name="products"/> to JSON using camelCase, wires up a
    /// stub HTTP client and runs <c>GetProductsAsync</c> synchronously.
    /// </summary>
    private static IReadOnlyList<Product> RunValidate(IEnumerable<Product> products)
    {
        var json    = JsonSerializer.Serialize(products, CamelCaseOptions);
        var handler = new StubHttpHandler(json);
        var client  = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var service = new ProductCatalogService(client);
        return service.GetProductsAsync().GetAwaiter().GetResult();
    }

    // -------------------------------------------------------------------------
    // Generators
    // -------------------------------------------------------------------------

    private static readonly char[] UpperAlphanumChars =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();

    /// <summary>Generates a non-empty, non-whitespace-only string.</summary>
    private static Gen<string> NonBlankStringGen() =>
        ArbMap.Default.ArbFor<NonEmptyString>().Generator
            .Select(s => s.Get.Trim())
            .Where(s => s.Length > 0);

    /// <summary>
    /// Generates a canonical Amazon BR product URL:
    /// <c>https://www.amazon.com.br/dp/[A-Z0-9]{10}</c>
    /// </summary>
    private static Gen<string> ValidAmazonUrlGen() =>
        Gen.Elements(UpperAlphanumChars)
           .ListOf(10)
           .Select(chars => $"https://www.amazon.com.br/dp/{new string(chars.ToArray())}");

    /// <summary>
    /// Generates a <see cref="Product"/> that satisfies every integrity rule
    /// (Requirements 1.5, 2.7, 2.8, 6.1, 3.1).
    /// </summary>
    internal static Gen<Product> ValidProductGen() =>
        from id       in NonBlankStringGen()
        from name     in NonBlankStringGen()
        from desc     in NonBlankStringGen()
        from imgUrl   in NonBlankStringGen()
        from category in Gen.Elements("Organização", "Cozinha", "Casa", "Limpeza")
        from cents    in Gen.Choose(1, 100_000)     // 0.01 BRL … 1 000.00 BRL
        from url      in ValidAmazonUrlGen()
        select new Product(id, name, desc, imgUrl, category, cents / 100m, url);

    /// <summary>
    /// Generates a <see cref="Product"/> where exactly one field violates an
    /// integrity rule.
    /// </summary>
    internal static Gen<Product> InvalidProductGen()
    {
        var badAsinGen =
            Gen.OneOf(Gen.Constant(9), Gen.Constant(11))
               .SelectMany(len =>
                   Gen.Elements(UpperAlphanumChars)
                      .ListOf(len)
                      .Select(chars => $"https://www.amazon.com.br/dp/{new string(chars.ToArray())}"));

        return
            from valid     in ValidProductGen()
            from violation in Gen.Choose(0, 4)
            from badAsin   in badAsinGen
            select violation switch
            {
                0 => valid with { Id       = "" },
                1 => valid with { Name     = "" },
                2 => valid with { Price    = 0m },
                3 => valid with { ImageUrl = "" },
                _ => valid with { AmazonUrl = badAsin }
            };
    }

    /// <summary>
    /// Generates a list that mixes valid and invalid products in random order.
    /// Valid products are deduplicated by <c>Id</c> to ensure none are wrongly
    /// filtered due to the deduplication rule.
    /// </summary>
    private static Gen<(List<Product> ValidOnes, List<Product> Mixed)> MixedListGen()
    {
        return
            from validCount   in Gen.Choose(1, 8)
            from invalidCount in Gen.Choose(1, 8)
            from validRaw     in ValidProductGen().ListOf(validCount)
            from invalidOnes  in InvalidProductGen().ListOf(invalidCount)
            // Deduplicate valid products by id so none are dropped by the dedup rule
            let validOnes = validRaw.GroupBy(p => p.Id).Select(g => g.First()).ToList()
            from seed     in ArbMap.Default.ArbFor<int>().Generator
            let mixed = validOnes.Cast<Product>()
                                 .Concat(invalidOnes)
                                 .OrderBy(_ => (seed ^ _.GetHashCode()))
                                 .ToList()
            select (validOnes, mixed);
    }

    // -------------------------------------------------------------------------
    // Property 1: Validate filters exactly the invalid records, preserves all valid ones
    // Feature: product-catalog-amazon-sync, Property 1
    // Validates: Requirements 1.5, 2.7, 2.8, 6.1, 6.3
    // -------------------------------------------------------------------------

    /// <summary>
    /// For any mixed list of products (valid and invalid records interleaved in
    /// any order and proportion), <c>ProductCatalogService.GetProductsAsync</c>
    /// must return exactly the valid subset — no more, no less.
    ///
    /// <b>Validates: Requirements 1.5, 2.7, 2.8, 6.1, 6.3</b>
    /// </summary>
    [Property(MaxTest = 100)]
    public Property Validate_FiltersExactlyTheInvalid_PreservesAllValid()
    {
        return Prop.ForAll(
            MixedListGen().ToArbitrary(),
            data =>
            {
                var (validOnes, mixed) = data;
                var result    = RunValidate(mixed);
                var resultIds = new HashSet<string>(result.Select(p => p.Id));

                // Every returned product must satisfy all integrity rules
                foreach (var p in result)
                {
                    if (string.IsNullOrWhiteSpace(p.Id))
                        return false.ToProperty().Label("Returned product has empty id");
                    if (string.IsNullOrWhiteSpace(p.Name))
                        return false.ToProperty().Label($"Product '{p.Id}' has empty name");
                    if (p.Price <= 0m)
                        return false.ToProperty().Label($"Product '{p.Id}' price={p.Price}");
                    if (string.IsNullOrEmpty(p.ImageUrl))
                        return false.ToProperty().Label($"Product '{p.Id}' has empty imageUrl");
                    if (!ProductCatalogService.IsValidAmazonUrl(p.AmazonUrl))
                        return false.ToProperty().Label($"Product '{p.Id}' has invalid amazonUrl");
                }

                // Every originally valid product (unique id) must appear in the result
                foreach (var v in validOnes)
                {
                    if (!resultIds.Contains(v.Id))
                        return false.ToProperty().Label($"Valid product '{v.Id}' was dropped");
                }

                return true.ToProperty();
            });
    }

    // -------------------------------------------------------------------------
    // Property 2: Deduplication preserves the first occurrence
    // Feature: product-catalog-amazon-sync, Property 2
    // Validates: Requirement 6.2
    // -------------------------------------------------------------------------

    /// <summary>
    /// For any list containing duplicate IDs, <c>Validate</c> keeps only the
    /// record at the lowest array index and discards all later copies.
    ///
    /// <b>Validates: Requirement 6.2</b>
    /// </summary>
    [Property(MaxTest = 100)]
    public Property Validate_WithDuplicateIds_KeepsFirstOccurrence()
    {
        var gen =
            from original   in ValidProductGen()
            from extraCount in Gen.Choose(1, 5)
            select (original, extraCount);

        return Prop.ForAll(gen.ToArbitrary(), data =>
        {
            var (original, extraCount) = data;

            // Build a list: [original, dup1, dup2, ...]
            var inputList = new List<Product> { original };
            for (var i = 0; i < extraCount; i++)
                inputList.Add(original with { Name = $"Duplicate {i + 1}" });

            var result = RunValidate(inputList);
            var hits   = result.Where(p => p.Id == original.Id).ToList();

            if (hits.Count != 1)
                return false.ToProperty()
                    .Label($"id='{original.Id}': expected 1 occurrence, got {hits.Count}");

            return (hits[0].Name == original.Name)
                .ToProperty()
                .Label($"id='{original.Id}': kept Name='{hits[0].Name}', " +
                       $"expected='{original.Name}'");
        });
    }

    // -------------------------------------------------------------------------
    // Property 5: Validate is idempotent
    // Feature: product-catalog-amazon-sync, Property 5
    // Validates: Requirements 6.1, 6.3
    // -------------------------------------------------------------------------

    /// <summary>
    /// Property 5: <c>Validate(Validate(list)) ≡ Validate(list)</c>.
    ///
    /// Tested via <see cref="ProductCatalogService.GetProductsAsync"/>:
    /// the output of a first validation pass is serialised back to JSON and fed
    /// into a fresh service instance. The second pass must return the exact same
    /// result, proving that every record surviving the first pass is already fully
    /// valid — a second pass neither removes more records nor reorders them.
    ///
    /// <b>Validates: Requirements 6.1, 6.3</b>
    /// </summary>
    [Property(MaxTest = 100)]
    public Property Validate_IsIdempotent()
    {
        return Prop.ForAll(
            MixedListGen().ToArbitrary(),
            data =>
            {
                var (_, mixed) = data;

                // --- First pass ---
                var r1 = RunValidate(mixed);

                // --- Second pass: re-serialise R1, run through a fresh service ---
                var json2    = JsonSerializer.Serialize(r1, CamelCaseOptions);
                var handler2 = new StubHttpHandler(json2);
                var client2  = new HttpClient(handler2) { BaseAddress = new Uri("http://localhost/") };
                var service2 = new ProductCatalogService(client2);
                var r2       = service2.GetProductsAsync().GetAwaiter().GetResult();

                if (r1.Count != r2.Count)
                    return false.ToProperty()
                        .Label($"Count mismatch after second pass: R1={r1.Count}, R2={r2.Count}");

                for (int i = 0; i < r1.Count; i++)
                {
                    if (r1[i] != r2[i])
                        return false.ToProperty()
                            .Label($"Index {i} differs: R1.Id={r1[i].Id}, R2.Id={r2[i].Id}");
                }

                return true.ToProperty();
            });
    }
}
