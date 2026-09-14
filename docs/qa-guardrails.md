# QA guardrails — vitrine `/produtos`

## Incidente

Uma alteração de SEO/`HeadOutlet` removeu o `<link>` para `css/produtos.css`. O arquivo continuava no ar (HTTP 200), mas `/produtos` carregava só `site.css` e a grade ficava sem estilo.

## Garantia em duas camadas

1. **`wwwroot/index.html`** sempre inclui `<link href="css/produtos.css" rel="stylesheet" />` (e `css/site.css`).
2. **`Pages/Produtos.razor`** declara `SeoMeta Stylesheet="css/produtos.css"`. O `Stylesheet` é renderizado no **mesmo** `HeadContent` que title/description/canonical. Os testes montam `HeadOutlet` + página e assertam o markup **final do outlet** — um segundo `HeadContent` na página é ignorado pelo Blazor e volta a deixar a grade sem estilo.

Não voltar a um segundo `<HeadContent>` em `Produtos.razor`: ele substitui o `HeadContent` do `SeoMeta` e pode dropar o CSS ou as tags SEO.

## CI antes do merge

- O workflow Azure Static Web Apps roda `dotnet test MaterDomus.sln` em push e em PRs abertos.
- Os testes em `MaterDomus.Tests/Unit/ProdutosCssGuardrailTests.cs` falham se o link de `produtos.css` for removido de `index.html`, de `Produtos.razor`/`SeoMeta`, ou se o `SeoMeta` deixar de renderizar `<link rel="stylesheet">` quando `Stylesheet` está definido.
- **Elias faz o merge manual só com os checks verdes.**

## Preview `/produtos`

O job de PR valida `dotnet test` e o `dotnet publish`; o deploy de preview Azure SWA está desligado neste repositório para não esgotar ambientes de staging. Depois do merge, conferir produção em `https://www.materdomus.com.br/produtos` (grade `.products-grid` / `.product-card` estilizada, boot screen e GTM intactos). Se um preview SWA estiver disponível de novo, repetir o mesmo check em `/produtos`.
