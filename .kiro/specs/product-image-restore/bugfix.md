# Documento de Requisitos de Correção de Bug

## Introduction

O único produto do catálogo, "Dispenser Quadrado Flow 1L - Tampa Branca" (id `dispenser-flow-quadrado-branco-001`), deixou de exibir sua imagem. O arquivo de imagem físico referenciado por `imageUrl` (`images/products/dispenser-flow-quadrado-branco-001.jpg`) foi apagado acidentalmente e não existe mais em `wwwroot/`.

Como consequência, ao carregar o produto o navegador não encontra o arquivo, a imagem falha ao carregar e a interface fica com uma imagem quebrada ou tenta o fallback de placeholder (`images/placeholder-product.png`), que também não está presente no diretório. As imagens de origem do mesmo produto continuam disponíveis em `wwwroot/images/products/DFW300/` (variações BGF, CHF e BCF do "Dispenser Quadrado Flow 1L"), o que permite restaurar a exibição correta.

Esta correção deve fazer o produto voltar a mostrar sua imagem correta, sem alterar o comportamento de nenhum outro aspecto do catálogo ou do carregamento de imagens.

## Bug Analysis

### Current Behavior (Defect)

O que acontece atualmente quando o bug é acionado:

1.1 WHEN o produto `dispenser-flow-quadrado-branco-001` é renderizado com `imageUrl` apontando para `images/products/dispenser-flow-quadrado-branco-001.jpg` THEN o sistema tenta carregar um arquivo inexistente e a imagem falha ao carregar
1.2 WHEN a imagem do produto falha ao carregar e o fallback `onerror` tenta usar `images/placeholder-product.png` THEN o sistema não exibe a imagem correta do produto (placeholder ausente ou imagem quebrada)

### Expected Behavior (Correct)

O que deveria acontecer:

2.1 WHEN o produto `dispenser-flow-quadrado-branco-001` é renderizado THEN o sistema SHALL carregar e exibir uma imagem existente e válida correspondente ao "Dispenser Quadrado Flow 1L"
2.2 WHEN o produto é renderizado THEN o sistema SHALL resolver o `imageUrl` para um arquivo de imagem que exista em `wwwroot/`, sem acionar o fallback de erro

### Unchanged Behavior (Regression Prevention)

Comportamento existente que deve ser preservado:

3.1 WHEN um produto possui um `imageUrl` que aponta para um arquivo existente THEN o sistema SHALL CONTINUE TO exibir essa imagem normalmente
3.2 WHEN a imagem de um produto realmente falha ao carregar THEN o sistema SHALL CONTINUE TO acionar o fallback `onerror` para `images/placeholder-product.png` em `ProductCard.razor`
3.3 WHEN os demais campos do produto (nome, descrição, categoria, preço, `amazonUrl`) são exibidos THEN o sistema SHALL CONTINUE TO renderizá-los sem alteração

## Condição do Bug (Bug Condition)

```pascal
FUNCTION isBugCondition(X)
  INPUT: X of type Product
  OUTPUT: boolean

  // Verdadeiro quando o imageUrl do produto aponta para um arquivo que não existe em wwwroot
  RETURN NOT fileExists(wwwrootPath(X.imageUrl))
END FUNCTION
```

Instância concreta que demonstra o bug (counterexample):

```
Product.id       = "dispenser-flow-quadrado-branco-001"
Product.imageUrl = "images/products/dispenser-flow-quadrado-branco-001.jpg"
wwwrootPath(...) = "wwwroot/images/products/dispenser-flow-quadrado-branco-001.jpg"  // NÃO existe
```

## Especificação da Propriedade (Property Specification)

```pascal
// Property: Fix Checking - a imagem do produto deve resolver para um arquivo existente
FOR ALL X WHERE isBugCondition(X) DO
  result ← resolveImage'(X)
  ASSERT fileExists(wwwrootPath(result.imageUrl)) AND imageRendersCorrectly(result)
END FOR
```

```pascal
// Property: Preservation Checking - entradas sem bug permanecem idênticas
FOR ALL X WHERE NOT isBugCondition(X) DO
  ASSERT F(X) = F'(X)
END FOR
```

- **F**: comportamento atual (antes da correção) de resolução/exibição da imagem do produto
- **F'**: comportamento após a correção
