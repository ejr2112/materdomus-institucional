# Documento de Requisitos de Correção de Bug

## Introdução

O site institucional da Mater Domus (aplicação Blazor) apresenta dois problemas de interface em telas mobile:

1. **Botões da seção hero quebrando no mobile:** os botões "Seja nosso fornecedor" (`.btn-primary`) e "Conheça a Mater Domus" (`.btn-secondary`), renderizados dentro de `.hero-actions` em `Pages/Index.razor`, não possuem tratamento responsivo. Em telas estreitas os dois links inline com `padding: 14px 26px` não cabem lado a lado e quebram de forma inconsistente, resultando em um layout incorreto.

2. **Ícones do menu inferior com fundo preto quando selecionados:** a regra genérica `nav a.active` em `wwwroot/css/site.css` aplica `background-color: #1f1f1f`. Como os links do menu inferior (`.bottom-nav a`) também correspondem ao seletor `nav a`, ao ficarem ativos herdam esse fundo escuro. A regra `.bottom-nav a.active` altera apenas a cor do texto/ícone, sem reverter o fundo, deixando o ícone selecionado praticamente invisível sobre o fundo escuro.

Ambos os problemas afetam somente a experiência mobile e não devem alterar o comportamento em desktop.

## Análise do Bug

### Comportamento Atual (Defeito)

1.1 QUANDO a seção hero é exibida em uma tela mobile estreita ENTÃO o sistema quebra/desalinha os botões "Seja nosso fornecedor" e "Conheça a Mater Domus", resultando em layout incorreto

1.2 QUANDO um item do menu inferior (`.bottom-nav`) está selecionado (`.active`) em mobile ENTÃO o sistema aplica um fundo preto (`#1f1f1f`) herdado da regra `nav a.active`, tornando o ícone selecionado ilegível sobre o fundo escuro

### Comportamento Esperado (Correto)

2.1 QUANDO a seção hero é exibida em uma tela mobile estreita ENTÃO o sistema DEVE exibir os botões "Seja nosso fornecedor" e "Conheça a Mater Domus" de forma organizada e legível, sem quebra incorreta (por exemplo, empilhados ou dispostos com espaçamento adequado)

2.2 QUANDO um item do menu inferior (`.bottom-nav`) está selecionado (`.active`) em mobile ENTÃO o sistema DEVE exibir o ícone selecionado sem fundo preto, mantendo-o visível e destacado apenas pela cor de destaque do texto/ícone

### Comportamento Inalterado (Prevenção de Regressão)

3.1 QUANDO a seção hero é exibida em desktop (largura maior que 768px) ENTÃO o sistema DEVE CONTINUAR A exibir os botões "Seja nosso fornecedor" e "Conheça a Mater Domus" lado a lado com o espaçamento atual

3.2 QUANDO um item da navegação de topo (`.header nav a`) está selecionado (`.active`) em desktop ENTÃO o sistema DEVE CONTINUAR A exibir o fundo escuro (`#1f1f1f`) com texto branco

3.3 QUANDO um item do menu inferior está inativo em mobile ENTÃO o sistema DEVE CONTINUAR A exibir o ícone e o rótulo na cor cinza padrão (`#888`) sem fundo

3.4 QUANDO os botões da hero são exibidos em qualquer viewport ENTÃO o sistema DEVE CONTINUAR A preservar os estilos visuais existentes (cores, bordas, raio de borda e destinos dos links)

---

## Derivação da Condição de Bug

### Bug 1 — Quebra dos botões da hero no mobile

```pascal
FUNCTION isBugCondition(X)
  INPUT: X of type ViewportContext  // contém viewportWidth e o componente renderizado
  OUTPUT: boolean

  // Bug ocorre na renderização de .hero-actions em telas mobile
  RETURN X.component = "hero-actions" AND X.viewportWidth <= 768
END FUNCTION
```

```pascal
// Property: Fix Checking - Layout responsivo dos botões da hero
FOR ALL X WHERE isBugCondition(X) DO
  result ← renderHeroActions'(X)
  ASSERT buttonsLegible(result) AND NOT brokenLayout(result)
END FOR
```

### Bug 2 — Fundo preto no ícone do menu inferior selecionado

```pascal
FUNCTION isBugCondition(X)
  INPUT: X of type NavLinkState  // contém a área de navegação e o estado ativo
  OUTPUT: boolean

  // Bug ocorre em links do bottom-nav quando ativos em mobile
  RETURN X.navArea = "bottom-nav" AND X.isActive = true AND X.viewportWidth <= 768
END FUNCTION
```

```pascal
// Property: Fix Checking - Ícone selecionado visível no menu inferior
FOR ALL X WHERE isBugCondition(X) DO
  result ← renderBottomNavLink'(X)
  ASSERT backgroundColor(result) = "transparent" AND iconVisible(result)
END FOR
```

### Objetivo de Preservação (ambos os bugs)

```pascal
// Property: Preservation Checking
FOR ALL X WHERE NOT isBugCondition(X) DO
  ASSERT F(X) = F'(X)
END FOR
```

Ou seja, para qualquer entrada que não seja `.hero-actions` em mobile (Bug 1) nem um link ativo do `.bottom-nav` em mobile (Bug 2) — incluindo a navegação de topo em desktop e os links inativos —, o comportamento após a correção deve ser idêntico ao original.

**Definições:**
- **F**: função/renderização original (antes da correção)
- **F'**: função/renderização corrigida (após a correção)
