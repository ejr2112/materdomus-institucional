# Design do Bugfix — Remoção do Vídeo do Produto

## Overview

O recurso de vídeo do produto (commit `acc44a5` — "Adiciona video do produto via embed do OneDrive") introduziu um player embutido (`<iframe>` de embed do OneDrive/SharePoint) no cartão de produto, alimentado por um campo opcional `VideoUrl` no modelo `Product`. O player não funcionou corretamente na vitrine e o usuário solicitou a remoção completa do recurso.

O recurso já foi objeto de um revert (commit `a646466`), que desfez as alterações versionadas. A inspeção do estado atual do repositório (HEAD) confirma que os artefatos de vídeo já não estão presentes no código-fonte:
- `Models/Product.cs` — o record `Product` **não** possui a propriedade `VideoUrl`.
- `Shared/ProductCard.razor` — **não** contém marcação `<iframe>` de vídeo nem o wrapper `product-card__video`.
- `wwwroot/css/produtos.css` — **não** contém as regras `product-card__video` / `product-card__video-wrapper`.
- `wwwroot/data/products.json` — **não** contém o campo `videoUrl` (o único produto, "Dispenser Quadrado Flow 1L", tem dados limpos).
- `.gitignore` — **não** contém entradas `*.mov` / `*.mp4` introduzidas para o recurso.

A estratégia deste bugfix, portanto, **não** é remover código (já removido pelo revert), mas **formalizar e verificar** a ausência total de vestígios do recurso e **prevenir regressões** em todo o restante do comportamento do produto. A condição do bug é definida sobre a *presença de qualquer artefato de vídeo*; se o estado atual satisfaz a condição do bug em qualquer ponto, ele deve ser eliminado. A verificação garante que a superfície de código está limpa e que imagem, nome, descrição, preço, categoria, favoritos, CTA da Amazon e detalhes continuam funcionando.

## Glossary

- **Bug_Condition (C)**: A condição que caracteriza o defeito — a presença de **qualquer** artefato do recurso de vídeo do produto no código, nos dados ou na renderização (propriedade `VideoUrl` no modelo, `<iframe>`/wrapper de vídeo no cartão, CSS `product-card__video*`, campo `videoUrl` no JSON, entradas `*.mov`/`*.mp4` no `.gitignore`).
- **Property (P)**: O comportamento correto desejado — o cartão de produto renderiza sem nenhum player de vídeo e nenhum vestígio do recurso permanece na base de código nem nos dados.
- **Preservation**: O comportamento existente do produto que deve permanecer inalterado — imagem, nome, descrição truncada, preço formatado, categoria, botão da Amazon (com atributos de segurança), favoritos, "Ver detalhes" (modal) e busca/filtros/estados de carregamento.
- **Product**: O record em `Models/Product.cs` que descreve um produto da vitrine (`Id`, `Name`, `Description`, `ImageUrl`, `Category`, `Price`, `AmazonUrl`). Não possui `VideoUrl`.
- **ProductCard.razor**: O componente em `Shared/ProductCard.razor` que renderiza um cartão de produto (imagem, corpo textual, ações).
- **ProductCatalogService**: O serviço em `Services/ProductCatalogService.cs` que carrega `wwwroot/data/products.json` via `HttpClient.GetFromJsonAsync<List<Product>>` (política camelCase) e valida o catálogo.
- **F (função original)**: O estado com o recurso de vídeo presente (antes do revert / hipotético estado defeituoso).
- **F' (função corrigida)**: O estado sem nenhum artefato de vídeo (estado atual verificado).

## Bug Details

### Bug Condition

O bug se manifesta quando **qualquer** artefato do recurso de vídeo do produto está presente na base de código, nos dados ou na renderização do cartão. Isto inclui: a propriedade `VideoUrl` no record `Product`, a marcação `<iframe>` (ou wrapper `product-card__video-wrapper`) em `ProductCard.razor`, as regras CSS `product-card__video`/`product-card__video-wrapper`, o campo `videoUrl` em `products.json`, ou entradas `*.mov`/`*.mp4` no `.gitignore` introduzidas exclusivamente para o recurso.

Enquanto qualquer um desses artefatos existe, a vitrine pode renderizar (ou vir a renderizar) o player embutido do OneDrive/SharePoint, que não reproduz o conteúdo corretamente.

**Formal Specification:**
```
FUNCTION isBugCondition(input)
  INPUT: input of type CodebaseState
         (arquivos-fonte + dados + marcação renderizada do cartão)
  OUTPUT: boolean

  RETURN input.productModel.hasProperty("VideoUrl")
         OR input.productCardMarkup.contains("<iframe")
         OR input.productCardMarkup.contains("product-card__video")
         OR input.css.definesRule("product-card__video")
         OR input.css.definesRule("product-card__video-wrapper")
         OR input.productsJson.anyProduct.hasField("videoUrl")
         OR input.gitignore.containsEntry("*.mov")
         OR input.gitignore.containsEntry("*.mp4")
END FUNCTION
```

Quando `isBugCondition(input)` retorna `true`, existe um vestígio do recurso e ele deve ser eliminado. Quando retorna `false`, a base de código está limpa (estado desejado — F').

### Examples

- **Presença do campo no modelo (defeito)**: `Product` declarado com `string? VideoUrl` → superfície de código do recurso persiste. Esperado: propriedade ausente. Estado atual verificado: **ausente** ✓
- **Iframe no cartão (defeito)**: `ProductCard.razor` contém `<iframe src="@Product.VideoUrl" ...>` dentro de `product-card__video-wrapper` → player defeituoso renderizado. Esperado: nenhuma marcação de vídeo. Estado atual verificado: **ausente** ✓
- **Campo nos dados (defeito)**: item em `products.json` contém `"videoUrl": "https://onedrive.live.com/embed?..."` para o "Dispenser Quadrado Flow 1L". Esperado: campo ausente. Estado atual verificado: **ausente** ✓
- **Entrada de mídia no .gitignore (defeito)**: `.gitignore` contém `*.mov` / `*.mp4` adicionados para o recurso. Esperado: ausentes. Estado atual verificado: **ausentes** ✓
- **Caso limite — produto sem qualquer dado de vídeo**: o único produto do catálogo renderiza normalmente (imagem, nome, descrição, preço, categoria, ações) sem nenhum player. Comportamento esperado (correto).

## Expected Behavior

### Preservation Requirements

**Unchanged Behaviors (o que deve continuar funcionando exatamente como antes):**
- Exibição de imagem (com fallback `placeholder-product.png` via `onerror`), nome, descrição truncada (120 chars via `ProductHelpers.TruncateDescription`), preço formatado (`R$ X,XX`, pt-BR) e categoria no cartão.
- Botão "Comprar na Amazon" renderizado apenas quando `AmazonUrl` está preenchida, com `target="_blank"` e `rel="noopener noreferrer"`.
- Botão de favoritar: alterna estado via `FavoritesService.Toggle`, reflete `aria-pressed`/`aria-label` corretos e atualiza o contador de favoritos.
- "Ver detalhes": abre o modal de detalhes (nome, preço, texto persuasivo, CTA da Amazon) e fecha por clique, Enter/Espaço e Escape.
- Busca, filtro por categoria e filtro de favoritos continuam filtrando a grade corretamente.
- Estados da página de produtos: carregamento (skeleton), erro (com "Tentar novamente") e vazio.
- Carregamento e validação do catálogo em `ProductCatalogService` (deserialização `List<Product>` de `data/products.json`, deduplicação por `Id`, validação de `AmazonUrl`/`ImageUrl`/`Price`).

**Scope:**
Todas as entradas onde `isBugCondition` é `false` (ou seja, qualquer produto e qualquer interação que não envolva artefatos de vídeo) devem permanecer completamente inalteradas por este bugfix. Isto inclui:
- Todos os produtos do catálogo, cujos dados não contêm `videoUrl`.
- Todas as interações do usuário no cartão (clique nos botões, teclado, favoritar, ver detalhes).
- Todas as regras CSS não relacionadas a vídeo em `produtos.css`.

**Nota:** O comportamento correto esperado para a condição do bug está formalizado na seção Correctness Properties (Property 1). Esta seção foca no que **não** deve mudar.

## Hypothesized Root Cause

Com base na descrição do bug e na inspeção do repositório, a origem do defeito e do seu tratamento é:

1. **Recurso de embed não funcional (causa original do bug)**: O player de vídeo usava um `<iframe>` apontando para um embed do OneDrive/SharePoint, que não reproduz corretamente quando incorporado na vitrine (restrições de embed/autenticação/headers do provedor). A abordagem de embed era inadequada para o caso de uso, motivando a remoção completa em vez de correção do player.

2. **Superfície de código distribuída em múltiplos pontos**: O recurso tocava cinco locais distintos — modelo (`Product.VideoUrl`), componente (`ProductCard.razor`), estilos (`produtos.css`), dados (`products.json`) e `.gitignore`. A remoção incompleta em qualquer um desses pontos deixaria um vestígio (condição do bug).

3. **Remoção via revert já aplicada**: O commit `a646466` reverteu as alterações versionadas. A hipótese principal a confirmar é que o revert foi **completo** — nenhum dos cinco artefatos permanece. A inspeção atual confirma essa hipótese; a estratégia de teste existe para verificar/refutar formalmente essa completude e travar a ausência contra regressões futuras.

4. **Risco de regressão latente**: Como o recurso compartilhava o mesmo componente e folha de estilos do restante do cartão, há risco de que a remoção tenha afetado inadvertidamente marcação/estilos adjacentes (imagem, ações, favoritar, detalhes). Esta hipótese é endereçada pelo preservation checking.

## Correctness Properties

Property 1: Bug Condition — Ausência total de artefatos de vídeo

_For any_ estado da base de código onde a condição do bug se aplica (`isBugCondition` retorna `true` — algum artefato de vídeo presente), o estado corrigido (F') SHALL eliminar esse artefato de modo que: o cartão de produto **não** renderize nenhum `<iframe>`/player de vídeo, o modelo `Product` **não** possua a propriedade `VideoUrl`, `ProductCard.razor` **não** contenha marcação de vídeo, `produtos.css` **não** contenha as regras `product-card__video`/`product-card__video-wrapper`, `products.json` **não** contenha o campo `videoUrl`, e o `.gitignore` **não** contenha entradas `*.mov`/`*.mp4` introduzidas para o recurso.

**Validates: Requirements 2.1, 2.2, 2.3**

Property 2: Preservation — Comportamento do produto inalterado

_For any_ entrada onde a condição do bug **não** se aplica (`isBugCondition` retorna `false` — nenhum artefato de vídeo envolvido), o estado corrigido (F') SHALL produzir exatamente o mesmo resultado que o estado original (F), preservando a exibição de imagem/nome/descrição truncada/preço formatado/categoria, o botão "Comprar na Amazon" com atributos de segurança, o comportamento de favoritar e contador, o modal "Ver detalhes" (com abertura/fechamento por clique, Enter/Espaço e Escape), a busca/filtros e os estados de carregamento/erro/vazio.

**Validates: Requirements 3.1, 3.2, 3.3, 3.4, 3.5, 3.6**

## Fix Implementation

### Changes Required

Assumindo que a análise de causa raiz está correta — o revert `a646466` removeu completamente os artefatos versionados — o estado atual (F') **já satisfaz** `isBugCondition == false`. A implementação deste bugfix consiste em (a) confirmar a ausência de cada artefato e (b) remover pontualmente qualquer vestígio que a verificação venha a revelar.

**Ações condicionais (aplicar somente se a verificação revelar o artefato):**

1. **Modelo `Product` — `Models/Product.cs`**: Garantir que o record **não** declare `VideoUrl`. Estado verificado: ausente. Ação necessária: nenhuma.

2. **Componente `ProductCard.razor` — `Shared/ProductCard.razor`**: Garantir que não exista `<iframe>` de vídeo nem `product-card__video-wrapper`/`product-card__video`. Estado verificado: ausente. Ação necessária: nenhuma.

3. **CSS `produtos.css` — `wwwroot/css/produtos.css`**: Garantir que não existam regras `product-card__video` e `product-card__video-wrapper`. Estado verificado: ausentes. Ação necessária: nenhuma.

4. **Dados `products.json` — `wwwroot/data/products.json`**: Garantir que nenhum item contenha o campo `videoUrl`. Estado verificado: ausente (produto "Dispenser Quadrado Flow 1L" com dados limpos). Ação necessária: nenhuma.

5. **`.gitignore`**: Garantir que não existam entradas `*.mov`/`*.mp4` introduzidas para o recurso. Estado verificado: ausentes. Ação necessária: nenhuma.

Caso a verificação (testes de exploração) surpreenda com qualquer artefato remanescente, a correção será a remoção cirúrgica desse artefato específico, sem tocar em código adjacente, seguida de nova rodada de verificação.

## Testing Strategy

### Validation Approach

A estratégia segue duas fases: primeiro, expor contraexemplos que demonstrem a condição do bug (presença de qualquer artefato de vídeo); segundo, verificar que o estado corrigido elimina esses artefatos e preserva todo o restante do comportamento do produto. Como o revert já foi aplicado, espera-se que a fase exploratória **não** encontre artefatos — a ausência de contraexemplos confirma a hipótese de remoção completa; qualquer contraexemplo refuta-a e reabre a implementação.

### Exploratory Bug Condition Checking

**Goal**: Surface counterexamples que demonstrem a condição do bug ANTES de (possíveis) alterações. Confirmar ou refutar a hipótese de que o revert removeu todos os artefatos. Se refutarmos (algum artefato aparece), re-hipotetizamos e removemos o vestígio.

**Test Plan**: Escrever verificações que inspecionam cada superfície do recurso e afirmam a **ausência** do artefato. Executar contra o estado atual (pós-revert) para observar que nenhuma condição de bug se mantém; se alguma se mantiver, o teste falha e expõe o vestígio exato.

**Test Cases**:
1. **Modelo sem VideoUrl**: Afirmar via reflexão que o tipo `Product` não expõe uma propriedade `VideoUrl` (falharia se o vestígio existisse).
2. **Cartão sem iframe/vídeo**: Renderizar `ProductCard` com um produto de teste e afirmar que a marcação não contém `<iframe>` nem classes `product-card__video`/`product-card__video-wrapper` (falharia se o vestígio existisse).
3. **Dados sem videoUrl**: Deserializar `products.json` e afirmar que nenhum item contém o campo `videoUrl`; adicionalmente, afirmar que uma entrada JSON com `videoUrl` extra é ignorada e não altera o `Product` deserializado (falharia se o modelo reintroduzisse o campo).
4. **Caso limite — `.gitignore`**: Afirmar que `.gitignore` não contém `*.mov`/`*.mp4` (pode falhar se o vestígio existisse).

**Expected Counterexamples**:
- Nenhum esperado no estado atual (pós-revert `a646466`).
- Se surgir algum: propriedade `VideoUrl` presente, marcação `<iframe>`/`product-card__video*` presente, campo `videoUrl` nos dados, regra CSS de vídeo, ou entradas de mídia no `.gitignore`. Causas possíveis: revert incompleto, reintrodução acidental, ou artefato em local não versionado.

### Fix Checking

**Goal**: Verificar que, para todas as entradas onde a condição do bug se aplica, o estado corrigido produz o comportamento esperado (nenhum artefato de vídeo; cartão sem player).

**Pseudocode:**
```
FOR ALL input WHERE isBugCondition(input) DO
  result := stateAfterFix(input)
  ASSERT NOT isBugCondition(result)          // nenhum artefato remanescente
  ASSERT NOT result.productCardMarkup.contains("<iframe")
END FOR
```

### Preservation Checking

**Goal**: Verificar que, para todas as entradas onde a condição do bug **não** se aplica, o estado corrigido produz o mesmo resultado que o original.

**Pseudocode:**
```
FOR ALL input WHERE NOT isBugCondition(input) DO
  ASSERT originalBehavior(input) = fixedBehavior(input)
END FOR
```

**Testing Approach**: Testes baseados em propriedades (property-based testing, FsCheck — já usado no projeto) são recomendados para o preservation checking porque:
- Geram muitos casos automaticamente sobre o domínio de entrada (produtos variados).
- Capturam casos-limite que testes unitários manuais poderiam não cobrir.
- Fornecem garantia forte de que o comportamento é inalterado para todas as entradas não-defeituosas.

**Test Plan**: Observar o comportamento no estado atual para renderização do cartão, botão da Amazon, favoritos, detalhes e filtros; então escrever testes baseados em propriedades que capturam esse comportamento e o travam contra regressões.

**Test Cases**:
1. **Preservação da renderização do cartão**: Para produtos gerados aleatoriamente, verificar que imagem, nome, descrição truncada, preço formatado e categoria são renderizados corretamente.
2. **Preservação do botão da Amazon**: Verificar que o botão "Comprar na Amazon" aparece somente com `AmazonUrl` válida e mantém `target="_blank"` e `rel="noopener noreferrer"`.
3. **Preservação de favoritos e detalhes**: Verificar que favoritar alterna estado/contador e que "Ver detalhes" abre/fecha o modal por clique, Enter/Espaço e Escape.

### Unit Tests

- Verificação por reflexão de que `Product` não possui `VideoUrl`.
- Renderização de `ProductCard` (bUnit) afirmando ausência de `<iframe>`/`product-card__video*` e presença correta de imagem, nome, descrição, preço, categoria e ações.
- Formatação de preço (`R$ X,XX`) e truncamento de descrição (120 chars) inalterados.

### Property-Based Tests

- Gerar produtos aleatórios e verificar que a renderização do cartão nunca produz marcação de vídeo, preservando os demais elementos.
- Gerar payloads JSON com um campo `videoUrl` extra e verificar que a deserialização em `List<Product>` o ignora sem afetar os campos válidos (idempotência do modelo).
- Verificar através de muitos cenários que o botão da Amazon e seus atributos de segurança são preservados.

### Integration Tests

- Fluxo completo da página de produtos: carregamento do catálogo via `ProductCatalogService`, renderização da grade sem qualquer player de vídeo.
- Alternância de busca/filtro por categoria/filtro de favoritos preservada.
- Estados de carregamento (skeleton), erro (com "Tentar novamente") e vazio preservados; abertura/fechamento do modal de detalhes preservado.
