# Restauração da Imagem do Produto — Design de Correção de Bug

## Overview

O produto "Dispenser Quadrado Flow 1L - Tampa Branca" (id `dispenser-flow-quadrado-branco-001`) não exibe mais sua imagem. O campo `imageUrl` em `wwwroot/data/products.json` aponta para `images/products/dispenser-flow-quadrado-branco-001.jpg`, mas esse arquivo físico foi apagado acidentalmente e não existe mais em `wwwroot/`. Como o placeholder de fallback (`images/placeholder-product.png`) também está ausente, a interface fica com imagem quebrada.

As imagens de origem do mesmo produto continuam disponíveis em `wwwroot/images/products/DFW300/`, nas variações `BGF`, `CHF` e `BCF` do "Dispenser Quadrado Flow 1L". Como o produto é a variante "Tampa Branca" (tampa branca), a origem apropriada é a variação `BCF` (branca).

**Estratégia de correção (Opção A — recriar o arquivo esperado):** copiar uma imagem de origem existente do `DFW300/` para o caminho exato que o `imageUrl` já referencia — `wwwroot/images/products/dispenser-flow-quadrado-branco-001.jpg`. Isso faz o produto voltar a exibir a imagem correta **sem alterar `products.json` nem nenhum código**.

**Por que a Opção A e não a Opção B (atualizar o `imageUrl` para apontar direto ao arquivo em `DFW300/`):**
- Mantém a referência de dados estável — o `imageUrl` continua idêntico, então nenhum outro consumidor do JSON é afetado.
- Evita problemas de codificação de URL: os nomes dos arquivos em `DFW300/` contêm **espaços** (ex.: `DFW300_DISPENSER QUADRADO Flow 1L_BCF.jpg`), o que exigiria URL-encoding (`%20`) e mistura de maiúsculas/minúsculas propensa a erro no navegador e em servidores case-sensitive.
- A mudança é mínima, localizada e reversível — apenas um arquivo de asset é adicionado.

## Glossary

- **Bug_Condition (C)**: A condição que aciona o bug — o `imageUrl` do produto aponta para um arquivo que não existe em `wwwroot/`.
- **Property (P)**: O comportamento desejado — o `imageUrl` do produto resolve para um arquivo de imagem existente e válido, exibido sem acionar o fallback de erro.
- **Preservation**: O comportamento existente que deve permanecer inalterado — resolução de imagens que já apontam para arquivos existentes, o fallback `onerror` do `ProductCard.razor`, e a renderização dos demais campos do produto.
- **imageUrl**: Campo em cada entrada de `wwwroot/data/products.json` que define o caminho relativo (a partir de `wwwroot/`) da imagem do produto.
- **ProductCard.razor**: Componente em `Shared/ProductCard.razor` que renderiza a `<img>` do produto com um handler `onerror` que troca a `src` pelo placeholder.
- **wwwrootPath(imageUrl)**: A resolução do `imageUrl` relativo para o caminho físico dentro de `wwwroot/`.

## Bug Details

### Bug Condition

O bug se manifesta quando o `imageUrl` de um produto aponta para um arquivo que não existe fisicamente em `wwwroot/`. No caso concreto, o arquivo `dispenser-flow-quadrado-branco-001.jpg` foi apagado, mas o `imageUrl` continua referenciando-o. O navegador solicita o arquivo, recebe 404, dispara `onerror` e tenta o placeholder — que também está ausente — resultando em imagem quebrada.

**Formal Specification:**
```
FUNCTION isBugCondition(input)
  INPUT: input of type Product
  OUTPUT: boolean

  RETURN NOT fileExists(wwwrootPath(input.imageUrl))
END FUNCTION
```

### Examples

- **Instância defeituosa (counterexample):** `Product.id = "dispenser-flow-quadrado-branco-001"`, `Product.imageUrl = "images/products/dispenser-flow-quadrado-branco-001.jpg"`. O caminho físico `wwwroot/images/products/dispenser-flow-quadrado-branco-001.jpg` NÃO existe → imagem quebrada. **Esperado:** exibir a imagem do "Dispenser Quadrado Flow 1L" (variante branca).
- **Fallback também falho:** ao acionar `onerror`, a `src` muda para `images/placeholder-product.png`, mas `wwwroot/images/placeholder-product.png` também NÃO existe (apenas `README-placeholder.txt`) → imagem quebrada persiste.
- **Origem disponível:** `wwwroot/images/products/DFW300/DFW300_DISPENSER QUADRADO Flow 1L_BCF.jpg` existe e corresponde à variante "Tampa Branca".
- **Caso não-bug (edge case):** um produto cujo `imageUrl` aponta para um arquivo existente → `isBugCondition` retorna `false`, comportamento inalterado.

## Expected Behavior

### Preservation Requirements

**Unchanged Behaviors:**
- Produtos cujo `imageUrl` aponta para um arquivo existente CONTINUAM exibindo essa imagem normalmente (Req 3.1).
- Quando uma imagem de produto realmente falha ao carregar, o fallback `onerror` para `images/placeholder-product.png` no `ProductCard.razor` CONTINUA sendo acionado (Req 3.2).
- Os demais campos do produto (nome, descrição, categoria, preço, `amazonUrl`) CONTINUAM sendo renderizados sem alteração (Req 3.3).

**Scope:**
Todas as entradas que NÃO satisfazem a condição do bug (isto é, `imageUrl` já aponta para um arquivo existente) devem ser completamente inalteradas por esta correção. Isso inclui:
- Resolução de `imageUrl` de produtos com arquivo presente.
- O mecanismo de fallback `onerror` do `ProductCard.razor` (código não é tocado).
- Toda a lógica de carregamento, filtragem e exibição de produtos.

> **Nota:** o comportamento correto esperado é definido na seção Correctness Properties (Property 1). Esta seção foca no que NÃO deve mudar.

## Hypothesized Root Cause

Com base na descrição do bug e na verificação do sistema de arquivos, a causa é única e confirmada:

1. **Asset ausente (causa confirmada)**: o arquivo de imagem `wwwroot/images/products/dispenser-flow-quadrado-branco-001.jpg` foi apagado acidentalmente. O `imageUrl` em `products.json` permanece correto do ponto de vista de dados, mas o arquivo físico correspondente não existe mais.
   - Verificado: o diretório `wwwroot/images/products/` contém apenas o subdiretório `DFW300/`, sem o `.jpg` esperado.
   - As imagens de origem existem em `DFW300/` (`BGF`, `CHF`, `BCF`).

2. **Placeholder de fallback ausente (agravante)**: `wwwroot/images/placeholder-product.png` não existe (apenas `README-placeholder.txt`), portanto o `onerror` do `ProductCard.razor` não consegue mascarar a falha, deixando a imagem visivelmente quebrada.
   - Este agravante NÃO é o alvo desta correção (o requisito 3.2 exige preservar o fallback como está), mas explica por que o bug é totalmente visível.

3. **Descartadas**: não há erro de lógica em `ProductCard.razor` nem no carregamento do JSON — a `<img src>` e o handler `onerror` estão corretos; o problema é puramente a ausência do asset físico.

## Correctness Properties

Property 1: Bug Condition - Imagem do produto resolve para arquivo existente

_For any_ produto onde a condição do bug se aplica (`isBugCondition` retorna `true`, ou seja, o `imageUrl` aponta para um arquivo inexistente), após a correção o sistema SHALL resolver esse `imageUrl` para um arquivo de imagem que exista em `wwwroot/` e exiba a imagem correta do "Dispenser Quadrado Flow 1L" sem acionar o fallback de erro.

**Validates: Requirements 2.1, 2.2**

Property 2: Preservation - Entradas sem bug permanecem idênticas

_For any_ produto onde a condição do bug NÃO se aplica (`isBugCondition` retorna `false`, ou seja, o `imageUrl` já aponta para um arquivo existente), o sistema após a correção SHALL produzir exatamente o mesmo resultado que antes da correção, preservando a resolução da imagem, o fallback `onerror` do `ProductCard.razor` e a renderização de nome, descrição, categoria, preço e `amazonUrl`.

**Validates: Requirements 3.1, 3.2, 3.3**

## Fix Implementation

### Changes Required

Assumindo que a análise de causa raiz está correta (asset físico ausente):

**Arquivo a ser criado**: `wwwroot/images/products/dispenser-flow-quadrado-branco-001.jpg`

**Origem**: `wwwroot/images/products/DFW300/DFW300_DISPENSER QUADRADO Flow 1L_BCF.jpg`

**Mudanças específicas**:
1. **Recriar o asset esperado**: copiar a imagem de origem `BCF` (variante branca, correspondente à "Tampa Branca") para o caminho exato referenciado pelo `imageUrl`, `wwwroot/images/products/dispenser-flow-quadrado-branco-001.jpg`.
   - Escolha da variação `BCF` porque o produto é a "Tampa Branca"; `BGF` e `CHF` são variações de cor/acabamento diferentes.
   - O nome do arquivo de destino segue exatamente o `imageUrl` já existente — sem espaços, seguro para URL.

2. **Não alterar `products.json`**: o `imageUrl` permanece `images/products/dispenser-flow-quadrado-branco-001.jpg`. Referência de dados intacta.

3. **Não alterar `ProductCard.razor`**: a `<img src>` e o handler `onerror` continuam inalterados, preservando o comportamento de fallback.

4. **(Opcional, fora de escopo desta correção)**: a ausência de `images/placeholder-product.png` é um problema separado do fallback e NÃO deve ser corrigida aqui para não violar o requisito de preservação 3.2. Pode ser tratada em uma correção futura, se desejado.

## Testing Strategy

### Validation Approach

A estratégia segue duas fases: primeiro, revelar counterexamples que demonstram o bug no código não corrigido (arquivo ausente); depois, verificar que a correção funciona e que o comportamento existente é preservado. Como a correção é a adição de um asset físico (e não uma mudança de código de função), os testes focam na existência e resolução do `imageUrl` para arquivos presentes em `wwwroot/`.

### Exploratory Bug Condition Checking

**Goal**: Revelar counterexamples que demonstram o bug ANTES de aplicar a correção. Confirmar ou refutar a análise de causa raiz. Se refutarmos, será necessário re-hipotetizar.

**Test Plan**: Escrever testes que verifiquem, para cada produto carregado de `products.json`, se o arquivo apontado por `imageUrl` existe em `wwwroot/`. Executar no código NÃO corrigido para observar a falha e confirmar a causa (asset ausente).

**Test Cases**:
1. **Existência do arquivo do produto**: para `dispenser-flow-quadrado-branco-001`, verificar que `wwwroot/images/products/dispenser-flow-quadrado-branco-001.jpg` existe (falha no código não corrigido).
2. **Existência da origem BCF**: verificar que `wwwroot/images/products/DFW300/DFW300_DISPENSER QUADRADO Flow 1L_BCF.jpg` existe (passa — confirma que a origem está disponível).
3. **Cobertura do catálogo**: para todo produto do `products.json`, verificar que `imageUrl` resolve para um arquivo existente (falha no código não corrigido, isolando o produto afetado).

**Expected Counterexamples**:
- O arquivo `dispenser-flow-quadrado-branco-001.jpg` não é encontrado em `wwwroot/images/products/`.
- Possíveis causas: asset apagado acidentalmente (confirmada), caminho incorreto no `imageUrl` (descartada — caminho é coerente), diretório errado (descartada).

### Fix Checking

**Goal**: Verificar que, para todas as entradas onde a condição do bug se aplica, a função corrigida produz o comportamento esperado.

**Pseudocode:**
```
FOR ALL input WHERE isBugCondition(input) DO
  result := resolveImage_fixed(input)
  ASSERT fileExists(wwwrootPath(result.imageUrl)) AND imageRendersCorrectly(result)
END FOR
```

### Preservation Checking

**Goal**: Verificar que, para todas as entradas onde a condição do bug NÃO se aplica, a função corrigida produz o mesmo resultado que a função original.

**Pseudocode:**
```
FOR ALL input WHERE NOT isBugCondition(input) DO
  ASSERT resolveImage_original(input) = resolveImage_fixed(input)
END FOR
```

**Testing Approach**: Testes baseados em propriedades são recomendados para a verificação de preservação porque:
- Geram muitos casos automaticamente ao longo do domínio de entrada (variando `imageUrl`, nome, descrição, categoria, preço, `amazonUrl`).
- Capturam edge cases que testes unitários manuais podem não cobrir.
- Fornecem forte garantia de que o comportamento é inalterado para todas as entradas não afetadas.

**Test Plan**: Observar o comportamento no código NÃO corrigido para produtos com imagem existente e para o fallback `onerror`, depois escrever testes baseados em propriedades que capturem esse comportamento e confirmem que permanece após a correção.

**Test Cases**:
1. **Preservação de imagem existente**: observar que um produto com `imageUrl` válido exibe sua imagem no código não corrigido; escrever teste garantindo que continua após a correção.
2. **Preservação do fallback `onerror`**: observar que o `ProductCard.razor` mantém `onerror` apontando para `images/placeholder-product.png`; garantir que o markup do componente permanece inalterado.
3. **Preservação dos demais campos**: observar renderização de nome, descrição, categoria, preço e `amazonUrl`; garantir que permanecem idênticos após a correção.

### Unit Tests

- Verificar que o `imageUrl` de `dispenser-flow-quadrado-branco-001` resolve para um arquivo existente em `wwwroot/` após a correção.
- Verificar que o arquivo de destino recriado é uma imagem válida (não vazio, extensão `.jpg`).
- Verificar que o markup de `ProductCard.razor` mantém o handler `onerror` inalterado (guarda de preservação de código).

### Property-Based Tests

- Gerar produtos variados e verificar que, quando `imageUrl` aponta para um arquivo existente, a resolução/renderização é idêntica antes e depois da correção (preservação).
- Gerar configurações de campos (nome, descrição, categoria, preço, `amazonUrl`) e verificar que a renderização desses campos permanece inalterada.
- Verificar a invariante: todo `imageUrl` presente em `products.json` resolve para um arquivo existente em `wwwroot/`.

### Integration Tests

- Carregar o catálogo completo a partir de `products.json` e renderizar o `ProductCard` do produto afetado, verificando que a imagem resolve para o asset recriado.
- Verificar o fluxo completo de exibição do produto (card + detalhes) exibindo a imagem correta sem imagem quebrada.
- Verificar que nenhum outro produto/comportamento do catálogo é afetado pela adição do asset.
