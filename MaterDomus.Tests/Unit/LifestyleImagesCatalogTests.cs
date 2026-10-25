using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MaterDomus.VitrineSeoGen;
using MaterDomus.Web.Models;
using MaterDomus.Web.Services;
using Xunit;

namespace MaterDomus.Tests.Unit;

/// <summary>
/// Fotos da vitrine: os 10 anúncios com ASIN têm três cenas cada.
/// Os três SKUs Martiplast sem ASIN abrem com a foto v4 aprovada;
/// a foto de fundo claro fica por último. A primeira foto também entra no JSON-LD.
/// </summary>
public class LifestyleImagesCatalogTests
{
    private static readonly JsonSerializerOptions WebJsonOptions = new(JsonSerializerDefaults.Web);

    private static readonly LifestyleExpectation[] Expected =
    {
        new("B0GKPPS5YH", "dispenser-flow-quadrado-branco-001", 34.99m, false,
            "images/products/dispenser-flow-quadrado-branco-001-ambiente-v2-1.webp",
            new[]
            {
                new LifestyleFile("images/products/dispenser-flow-quadrado-branco-001-ambiente-v2-1.webp",
                    "Dispenser Flow 1L em bancada de lavanderia com revestimento verde-sálvia",
                    "a755ec96978e1735df35c9d30234a41d7c1ff2d61f2f6bf22b260941ade1bbe4", 90638),
                new LifestyleFile("images/products/dispenser-flow-quadrado-branco-001-ambiente-v2-2.webp",
                    "Dispenser Flow 1L em bancada de madeira com toalhas ao lado",
                    "828791df4da46d409b4b565bd158cdbb502d0ea2c767b11fda7e3f88a60b02c7", 111636),
                new LifestyleFile("images/products/dispenser-flow-quadrado-branco-001-ambiente-v2-3.webp",
                    "Dispenser Flow 1L em bancada cinza com revestimento azul",
                    "dcf5119071c84e58d5739ce409609cfd6cd86139be3e01f639a6aeaef675c162", 113218)
            }),
        new("B0GKPZMCYZ", "dispenser-quadrado-1-5l-branco-b0gkpzmcyz", 36.00m, false,
            "images/products/dispenser-quadrado-1-5l-branco-b0gkpzmcyz-ambiente-v2-1.webp",
            new[]
            {
                new LifestyleFile("images/products/dispenser-quadrado-1-5l-branco-b0gkpzmcyz-ambiente-v2-1.webp",
                    "Dispenser Flow 1,5L em bancada clara de lavanderia",
                    "6ada1ac63eb3c9d994f0d779bdcd56d1917f3c0d1b9f18f6e52066602e56a742", 93262),
                new LifestyleFile("images/products/dispenser-quadrado-1-5l-branco-b0gkpzmcyz-ambiente-v2-2.webp",
                    "Dispenser Flow 1,5L em prateleira de madeira na lavanderia",
                    "5277f27055ad0e54a5250e8bde5a7fecef2c5bb28bb7d8cabb3e0cf7dec10a0a", 110016),
                new LifestyleFile("images/products/dispenser-quadrado-1-5l-branco-b0gkpzmcyz-ambiente-v2-3.webp",
                    "Dispenser Flow 1,5L em bancada escura acima da máquina de lavar",
                    "92e98def6dda81f667d591f9ed25df933097f89aaca05a07bcbdf2499005d1b2", 124880)
            }),
        new("B0CZTTSHB7", "borrifador-500ml-bege-b0czttshb7", 29.74m, true,
            "images/products/borrifador-500ml-bege-b0czttshb7-ambiente-v2-1.webp",
            new[]
            {
                new LifestyleFile("images/products/borrifador-500ml-bege-b0czttshb7-ambiente-v2-1.webp",
                    "Borrifador Flow 500ml em bancada de cozinha junto à janela",
                    "11c346befbf3dedc879a71185853f8a9a5f7253530ec1ddb03cb1a8a1f85ad44", 81876),
                new LifestyleFile("images/products/borrifador-500ml-bege-b0czttshb7-ambiente-v2-2.webp",
                    "Borrifador Flow 500ml em bancada de madeira na lavanderia",
                    "b2f7776370b958d064c948e227548c79f65dc1de02749085e2d0498f982caca1", 90900),
                new LifestyleFile("images/products/borrifador-500ml-bege-b0czttshb7-ambiente-v2-3.webp",
                    "Borrifador Flow 500ml em bancada de banheiro com revestimento azul",
                    "d209d2f0a56b807b88b57fde1329a43bebad866c2b01f6d75f8bac9e7efdb599", 82162)
            }),
        new("B0GKQ4VVPQ", "organizador-parede-armario-branco-b0gkq4vvpq", 58.49m, true,
            "images/products/organizador-parede-armario-branco-b0gkq4vvpq-ambiente-v2-1.webp",
            new[]
            {
                new LifestyleFile("images/products/organizador-parede-armario-branco-b0gkq4vvpq-ambiente-v2-1.webp",
                    "Organizador branco nivelado na parede verde da lavanderia, visto de frente",
                    "dfabad516d0087ab320e8fe0037c037f735e70a295cfdf55c8a4201f1baccd20", 60290),
                new LifestyleFile("images/products/organizador-parede-armario-branco-b0gkq4vvpq-ambiente-v2-2.webp",
                    "Organizador branco fixado na face interna da porta do armário, visto de frente",
                    "de9572ed7a7d4e6627c4758376a15d7d473efff0006d698111e9fa0efb02c3b7", 121414),
                new LifestyleFile("images/products/organizador-parede-armario-branco-b0gkq4vvpq-ambiente-v2-3.webp",
                    "Organizador branco fixado na parede bege do banheiro, visto de frente",
                    "c5cccf3288e469de87c69751f3feb9bb27372ff361c8a75017f9b65a5640a826", 116238)
            }),
        new("B0CZTTRFQR", "escova-limpeza-multiuso-bege-b0czttrfqr", 20.69m, true,
            "images/products/escova-limpeza-multiuso-bege-b0czttrfqr-ambiente-v2-1.webp",
            new[]
            {
                new LifestyleFile("images/products/escova-limpeza-multiuso-bege-b0czttrfqr-ambiente-v2-1.webp",
                    "Escova multiuso apoiada em bancada de cozinha, vista de cima",
                    "d288ff74468e88e469933760fccb42c41e653390116c7c2ec7544ddd6830e1d9", 265372),
                new LifestyleFile("images/products/escova-limpeza-multiuso-bege-b0czttrfqr-ambiente-v2-2.webp",
                    "Escova multiuso com cabo apoiado sobre pano em bancada de madeira",
                    "3a977ba030278ad4c07f9c6264b61fac017894a6447a96805f8fffd03d6e492a", 364124),
                new LifestyleFile("images/products/escova-limpeza-multiuso-bege-b0czttrfqr-ambiente-v2-3.webp",
                    "Escova multiuso pendurada pelo laço em gancho na parede da cozinha",
                    "51d32983506b00e2cb9674f1d3a705ade603219ed8d14eda13d3e06f3f1c05cd", 83926)
            }),
        new("B0GKPQ99R8", "organizador-parede-multiuso-bege-b0gkpq99r8", 56.69m, true,
            "images/products/organizador-parede-multiuso-bege-b0gkpq99r8-ambiente-v2-1.webp",
            new[]
            {
                new LifestyleFile("images/products/organizador-parede-multiuso-bege-b0gkpq99r8-ambiente-v2-1.webp",
                    "Organizador de parede para sacolas bege da Linha Flow",
                    "b21c2f5c48babecebbe6133c8b4d429f305577bc074de684865c1a4f840126aa", 11248),
                new LifestyleFile("images/products/organizador-parede-multiuso-bege-b0gkpq99r8-ambiente-v2-2.webp",
                    "Organizador de sacolas da Linha Flow fixado na parede, com uma sacola na abertura inferior",
                    "9e6d8513e0f4ecb650898b6094bdc5fe81c2421954af8de5b10b1225a501e6e3", 16090),
                new LifestyleFile("images/products/organizador-parede-multiuso-bege-b0gkpq99r8-ambiente-v2-3.webp",
                    "Detalhe da abertura do organizador de sacolas da Linha Flow",
                    "d4cf09cce2e2d608b4450ea292a8b8b3812fd9a762581a8bad637e7773ef9515", 17586)
            }),
        new("B0CZTTVLWK", "rodo-multiuso-bege-b0czttvlwk", 17.09m, true,
            "images/products/rodo-multiuso-bege-b0czttvlwk-ambiente-v2-1.webp",
            new[]
            {
                new LifestyleFile("images/products/rodo-multiuso-bege-b0czttvlwk-ambiente-v2-1.webp",
                    "Rodo multiuso pendurado pelo laço no box do banheiro",
                    "ef9ba182aeff063870aa4b4634f32fcee502929131b7126c271031cb4a63dd2c", 149064),
                new LifestyleFile("images/products/rodo-multiuso-bege-b0czttvlwk-ambiente-v2-2.webp",
                    "Rodo multiuso apoiado no parapeito de pedra da janela",
                    "482db7ecba269886860ffee052c573facbf71f9f23e1f123419c302f90942b55", 169766),
                new LifestyleFile("images/products/rodo-multiuso-bege-b0czttvlwk-ambiente-v2-3.webp",
                    "Rodo multiuso apoiado na bancada do banheiro",
                    "dd1b9efabbe5c723a20e6f31c277d7bb733ee9f14a0cb9536b59deff7f0da945", 100012)
            }),
        new("B0F8PWY3M5", "rodo-bege-b0f8pwy3m5", 51.99m, true,
            "images/products/rodo-bege-b0f8pwy3m5-ambiente-v2-1.webp",
            new[]
            {
                new LifestyleFile("images/products/rodo-bege-b0f8pwy3m5-ambiente-v2-1.webp",
                    "Rodo de cabo longo apoiado na parede verde da lavanderia",
                    "cda03762f67f4b4b3c4b31465b5dd6f4daf8c6e8d15e5ffb3c049b22005e0172", 114900),
                new LifestyleFile("images/products/rodo-bege-b0f8pwy3m5-ambiente-v2-2.webp",
                    "Rodo de cabo longo na área de serviço ao lado de cesto",
                    "fecac86bcc1ad75b26e6a7ba6cf0e45d4fcc08515949b3d4f57b622a49f7a907", 120732),
                new LifestyleFile("images/products/rodo-bege-b0f8pwy3m5-ambiente-v2-3.webp",
                    "Rodo de cabo longo junto à porta de vidro da varanda",
                    "e50e1e02c5160434d012175ee4210c1aac21995da85d1cebde52f7deedc24b0f", 161642)
            }),
        new("B0FXBN7SCB", "pano-chao-microfibra-chumbo-b0fxbn7scb", 19.79m, true,
            "images/products/pano-chao-microfibra-chumbo-b0fxbn7scb-ambiente-v2-1.webp",
            new[]
            {
                new LifestyleFile("images/products/pano-chao-microfibra-chumbo-b0fxbn7scb-ambiente-v2-1.webp",
                    "Pano de chão chumbo com encaixe circular sobre bancada de madeira",
                    "226f9040e060d078606034965c6c016b17ed9dbcf6747faa62d269179830982c", 273298),
                new LifestyleFile("images/products/pano-chao-microfibra-chumbo-b0fxbn7scb-ambiente-v2-2.webp",
                    "Pano de chão chumbo dobrado no piso ao lado de balde",
                    "f5242b91d286e8e6e74d3eef1de264318a66d38895b8869aeb3527e39cdcc389", 206826),
                new LifestyleFile("images/products/pano-chao-microfibra-chumbo-b0fxbn7scb-ambiente-v2-3.webp",
                    "Pano de chão chumbo sobre bancada acima da máquina de lavar",
                    "0f75332540d4b12656ce3c702b0465c26050562adc776bbfd6001edc93fad13c", 247358)
            }),
        new("B0G634V2NB", "kit-3-panos-microfibra-b0g634v2nb", 18.69m, true,
            "images/products/kit-3-panos-microfibra-b0g634v2nb-ambiente-v2-1.webp",
            new[]
            {
                new LifestyleFile("images/products/kit-3-panos-microfibra-b0g634v2nb-ambiente-v2-1.webp",
                    "Kit com três panos de microfibra na bancada da cozinha",
                    "52b94403435d64df78be2f88a08dcc8b5f6d91822dfe1f8ba34334b05d05cf38", 228104),
                new LifestyleFile("images/products/kit-3-panos-microfibra-b0g634v2nb-ambiente-v2-2.webp",
                    "Kit com três panos de microfibra em prateleira de madeira",
                    "4f5e4f18c18cac49e4e7ec8269dbc4e648bddf1ae8c211743a173c016666a16b", 275294),
                new LifestyleFile("images/products/kit-3-panos-microfibra-b0g634v2nb-ambiente-v2-3.webp",
                    "Kit com três panos de microfibra na bancada do banheiro",
                    "4cb7e570f0e6667a656818a4cbeacf42774f0699a588c1da4de8adcf0fe6b6f2", 246824)
            }),
    };

    private static readonly ComingSoonGallery[] ComingSoon =
    {
        new("borrifador-350ml-bege-bfm500bgf", "7899620682399",
            "Ou Borrifador Multiuso 350ml Bege Linha Flow", 24.99m,
            new[]
            {
                new LifestyleFile("images/products/borrifador-350ml-bege-bfm500bgf-ambiente-v4-1.webp",
                    "Borrifador Flow 350ml bege borrifando sobre a bancada da cozinha",
                    "a722b011f09582fe85b9cbcb8e8f6b378e3ee3af99ed444308de23bfa1389370", 109976),
                new LifestyleFile("images/products/borrifador-350ml-bege-bfm500bgf-ambiente-v2-2.webp",
                    "Borrifador Flow 350ml bege na bancada da cozinha",
                    "9a0fb1e84df8be9b1691550b6ddb8c05f7c5373bfaa9b2703e55937cb1cf1ee7", 22048),
                new LifestyleFile("images/products/borrifador-350ml-bege-bfm500bgf-ambiente-v2-3.webp",
                    "Borrifador Flow 350ml bege borrifando uma planta",
                    "e2f521baa4275fb30850ec9fa1abe71ffc448b8320aaf8c2b4d170b7e134bdae", 31718),
                new LifestyleFile("images/products/borrifador-350ml-bege-bfm500bgf-ambiente-v2-1.webp",
                    "Borrifador Flow 350ml bege sobre fundo claro",
                    "75eee5b950636c5c1e64ba8e54624abad78cf371419ce17fef661e01ba48eb60", 4546)
            }),
        new("borrifador-750ml-bege-bfm700bgf", "7899620682405",
            "Ou Borrifador Multiuso 750ml Bege Linha Flow", 34.99m,
            new[]
            {
                new LifestyleFile("images/products/borrifador-750ml-bege-bfm700bgf-ambiente-v4-1.webp",
                    "Borrifador Flow 750ml bege borrifando sobre a bancada da cozinha",
                    "1e8162016985329dd6843f9e2c30ab25549328e33734b70405186e1e07ba9aac", 109224),
                new LifestyleFile("images/products/borrifador-750ml-bege-bfm700bgf-ambiente-v2-2.webp",
                    "Borrifador Flow 750ml bege na bancada da cozinha",
                    "17ef89203176cea6212747072b5d053d02d98f1c37fcd1e8e83c20a537b4e89d", 21214),
                new LifestyleFile("images/products/borrifador-750ml-bege-bfm700bgf-ambiente-v2-3.webp",
                    "Borrifador Flow 750ml bege em uso na bancada",
                    "641206cfc7fdbfec17897a612f171a406d02a2f70564a9aa0b9e78f3e03701bf", 49398),
                new LifestyleFile("images/products/borrifador-750ml-bege-bfm700bgf-ambiente-v2-1.webp",
                    "Borrifador Flow 750ml bege sobre fundo claro",
                    "5dbcb8f0a0fa5ba75003442f2908cacf111145b020028fc94cae8b81347e617d", 4722)
            }),
        new("organizador-parede-sacolas-chumbo-osw100chf", "7899620684980",
            "Ou Organizador de Parede para Sacolas Chumbo Linha Flow", 69.99m,
            new[]
            {
                new LifestyleFile("images/products/organizador-parede-sacolas-chumbo-osw100chf-ambiente-v4-1.webp",
                    "Organizador de sacolas chumbo da Linha Flow na parede da lavanderia, com uma sacola na abertura inferior",
                    "57aa62de008d4b7e1cfb773c27ec6b245140fc9affb97f30c384067fd86307d2", 134308),
                new LifestyleFile("images/products/organizador-parede-sacolas-chumbo-osw100chf-ambiente-v2-1.webp",
                    "Organizador de parede para sacolas chumbo da Linha Flow",
                    "cef197625b86a9785c7aebc31bb280893f3ac126f2741cc46c6cff36c940dbcf", 10668)
            }),
    };

    [Fact]
    public void ProductsJson_AllTenProductsHaveThreeLifestyleImages()
    {
        using var doc = JsonDocument.Parse(ReadRepo("wwwroot", "data", "products.json"));
        var rows = doc.RootElement.EnumerateArray().ToList();
        Assert.Equal(13, rows.Count);

        var withLifestyle = rows.Where(row => row.TryGetProperty("lifestyleImages", out _)).ToList();
        Assert.Equal(13, withLifestyle.Count);
        var withAsin = rows.Where(row => row.TryGetProperty("asin", out _)).ToList();
        Assert.Equal(
            Expected.Select(item => item.Asin),
            withAsin.Select(row => row.GetProperty("asin").GetString()));

        var firstComingSoon = rows.FindIndex(row => row.GetProperty("comingSoon").GetBoolean());
        Assert.True(firstComingSoon > 0);
        Assert.All(rows.Take(firstComingSoon), row => Assert.False(row.GetProperty("comingSoon").GetBoolean()));
        Assert.All(rows.Skip(firstComingSoon), row => Assert.True(row.GetProperty("comingSoon").GetBoolean()));

        foreach (var expected in Expected)
        {
            var row = rows.Single(item =>
                item.TryGetProperty("asin", out var asin) && asin.GetString() == expected.Asin);
            var names = row.EnumerateObject().Select(property => property.Name).ToList();
            Assert.Equal("lifestyleImages", names[^1]);
            Assert.Equal(expected.Id, row.GetProperty("id").GetString());
            Assert.Equal(expected.Price, row.GetProperty("price").GetDecimal());
            Assert.Equal(expected.ImageUrl, row.GetProperty("imageUrl").GetString());
            Assert.Equal(expected.Files[0].Url, expected.ImageUrl);
            Assert.Equal(expected.ComingSoon, row.GetProperty("comingSoon").GetBoolean());

            var images = row.GetProperty("lifestyleImages").EnumerateArray().ToList();
            Assert.Equal(3, images.Count);
            for (var i = 0; i < expected.Files.Length; i++)
            {
                var image = images[i];
                Assert.Equal(expected.Files[i].Url, image.GetProperty("url").GetString());
                var alt = image.GetProperty("alt").GetString();
                Assert.False(string.IsNullOrWhiteSpace(alt));
                Assert.Equal(expected.Files[i].Alt, alt);
                Assert.Matches(@"[A-Za-zÀ-ÿ]", alt);

                var physical = RepoPath("wwwroot", expected.Files[i].Url.Replace('/', Path.DirectorySeparatorChar));
                Assert.True(File.Exists(physical), $"Arquivo ausente: {physical}");
                var info = new FileInfo(physical);
                Assert.Equal(expected.Files[i].Size, info.Length);
                Assert.Equal(expected.Files[i].Sha256, Sha256(physical));
            }
        }

        var products = JsonSerializer.Deserialize<List<Product>>(
            ReadRepo("wwwroot", "data", "products.json"),
            WebJsonOptions);
        Assert.NotNull(products);
        Assert.Equal(13, products!.Count);
        Assert.Equal(Expected.Length, products.Count(product => product.LifestyleImages is { Count: 3 }));
        foreach (var expected in Expected)
        {
            var product = products.Single(item => item.Asin == expected.Asin);
            Assert.Equal(expected.ImageUrl, product.ImageUrl);
            Assert.Equal(expected.Id, product.Id);
            Assert.Equal(3, product.LifestyleImages!.Count);
        }

        foreach (var expected in ComingSoon)
        {
            var row = rows.Single(item => item.GetProperty("id").GetString() == expected.Id);
            Assert.False(row.TryGetProperty("asin", out _));
            Assert.Equal("", row.GetProperty("amazonUrl").GetString());
            Assert.True(row.GetProperty("comingSoon").GetBoolean());
            Assert.Equal(expected.Ean, row.GetProperty("ean").GetString());
            Assert.Equal(expected.Name, row.GetProperty("name").GetString());
            Assert.Equal(expected.Price, row.GetProperty("price").GetDecimal());
            Assert.Equal(expected.Files[0].Url, row.GetProperty("imageUrl").GetString());
            AssertGallery(row.GetProperty("lifestyleImages"), expected.Files);

            var product = products.Single(item => item.Id == expected.Id);
            Assert.Null(product.Asin);
            Assert.Equal("", product.AmazonUrl);
            Assert.True(product.ComingSoon);
            Assert.Equal(expected.Ean, product.Ean);
            Assert.Equal(expected.Name, product.Name);
            Assert.Equal(expected.Price, product.Price);
            Assert.Equal(expected.Files[0].Url, product.ImageUrl);
            Assert.Equal(expected.Files.Length, product.LifestyleImages!.Count);
        }

        foreach (var wrong in new[]
        {
            "images/products/organizador-parede-sacolas-chumbo-osw100chf-ambiente-v2-2.webp",
            "images/products/organizador-parede-sacolas-chumbo-osw100chf-ambiente-v2-3.webp"
        })
        {
            Assert.DoesNotContain(wrong, ReadRepo("wwwroot", "data", "products.json"), StringComparison.Ordinal);
            Assert.False(File.Exists(RepoPath("wwwroot", wrong.Replace('/', Path.DirectorySeparatorChar))));
        }
    }

    [Fact]
    public void CatalogService_SkipsLifestyleItemsWithEmptyUrl_WithoutDroppingProduct()
    {
        const string json = """
            [
              {
                "id": "prod-misto",
                "name": "Produto misto",
                "description": "Desc.",
                "imageUrl": "images/products/x.jpg",
                "category": "Casa",
                "price": 10.0,
                "amazonUrl": "https://www.amazon.com.br/dp/B0GKQ4VVPQ",
                "lifestyleImages": [
                  { "url": " ", "alt": "sem arquivo" },
                  { "url": "", "alt": "vazio" },
                  { "url": "images/products/a.webp", "alt": "Foto ambientada válida" }
                ]
              },
              {
                "id": "prod-so-vazio",
                "name": "Produto só vazio",
                "description": "Desc.",
                "imageUrl": "images/products/y.jpg",
                "category": "Casa",
                "price": 12.0,
                "amazonUrl": "https://www.amazon.com.br/dp/B0GKPQ99R8",
                "lifestyleImages": [
                  { "url": "", "alt": "nada" }
                ]
              },
              {
                "id": "prod-sem-campo",
                "name": "Produto sem campo",
                "description": "Desc.",
                "imageUrl": "images/products/z.jpg",
                "category": "Casa",
                "price": 14.0,
                "amazonUrl": "https://www.amazon.com.br/dp/B0GKPPS5YH"
              }
            ]
            """;

        var products = LoadViaCatalogService(json);

        Assert.Equal(3, products.Count);

        var mixed = products.Single(product => product.Id == "prod-misto");
        Assert.NotNull(mixed.LifestyleImages);
        var kept = Assert.Single(mixed.LifestyleImages!);
        Assert.Equal("images/products/a.webp", kept.Url);
        Assert.Equal("Foto ambientada válida", kept.Alt);

        Assert.Null(products.Single(product => product.Id == "prod-so-vazio").LifestyleImages);
        Assert.Null(products.Single(product => product.Id == "prod-sem-campo").LifestyleImages);
    }

    [Fact]
    public void SeoArtifacts_UseCurrentPrimaryImages()
    {
        var artifacts = VitrineSeo.Build(ReadRepo("wwwroot", "data", "products.json"), ReadRepo("wwwroot", "index.html"));
        foreach (var expected in Expected)
        {
            Assert.Contains(expected.ImageUrl, artifacts.ItemListJson);
            Assert.Contains(expected.ImageUrl, artifacts.ProdutosHtml);
            foreach (var secondary in expected.Files.Skip(1))
            {
                Assert.DoesNotContain(secondary.Url, artifacts.ItemListJson);
                Assert.DoesNotContain(secondary.Url, artifacts.MetaJson);
            }
        }

        foreach (var expected in ComingSoon)
        {
            Assert.Contains(expected.Files[0].Url, artifacts.ItemListJson);
            Assert.Contains(expected.Files[0].Url, artifacts.ProdutosHtml);
            foreach (var secondary in expected.Files.Skip(1))
            {
                Assert.DoesNotContain(secondary.Url, artifacts.ItemListJson);
                Assert.Contains(secondary.Url, artifacts.ProdutosHtml);
            }
        }

        Assert.EndsWith(Expected[0].ImageUrl, artifacts.OgImageAbsoluteUrl);
    }

    private static void AssertGallery(JsonElement images, LifestyleFile[] expected)
    {
        var slides = images.EnumerateArray().ToList();
        Assert.Equal(expected.Length, slides.Count);
        Assert.EndsWith("ambiente-v4-1.webp", expected[0].Url, StringComparison.Ordinal);
        Assert.EndsWith("ambiente-v2-1.webp", expected[^1].Url, StringComparison.Ordinal);
        for (var i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i].Url, slides[i].GetProperty("url").GetString());
            Assert.Equal(expected[i].Alt, slides[i].GetProperty("alt").GetString());
            var physical = RepoPath("wwwroot", expected[i].Url.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(physical), $"Arquivo ausente: {physical}");
            var info = new FileInfo(physical);
            Assert.Equal(expected[i].Size, info.Length);
            Assert.Equal(expected[i].Sha256, Sha256(physical));
        }
    }

    private static List<Product> LoadViaCatalogService(string productsJson)
    {
        var handler = new StubHandler(productsJson);
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://www.materdomus.com.br/") };
        var service = new ProductCatalogService(client);
        return service.GetProductsAsync().GetAwaiter().GetResult().ToList();
    }

    private static string Sha256(string path)
    {
        var hash = SHA256.HashData(File.ReadAllBytes(path));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string ReadRepo(params string[] segments) =>
        File.ReadAllText(RepoPath(segments));

    private static string RepoPath(params string[] segments)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "MaterDomus.Web.csproj")))
                return Path.Combine(new[] { dir.FullName }.Concat(segments).ToArray());
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Não foi possível localizar a raiz do repositório.");
    }

    private sealed record LifestyleExpectation(
        string Asin,
        string Id,
        decimal Price,
        bool ComingSoon,
        string ImageUrl,
        LifestyleFile[] Files);

    private sealed record ComingSoonGallery(
        string Id,
        string Ean,
        string Name,
        decimal Price,
        LifestyleFile[] Files);

    private sealed record LifestyleFile(string Url, string Alt, string Sha256, long Size);

    private sealed class StubHandler(string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
    }
}
