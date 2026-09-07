# Requirements Document

## Introduction

Esta funcionalidade adiciona uma visão de detalhes de produto ao site vitrine MaterDomus (Blazor WebAssembly, .NET 9). Hoje cada produto é exibido em um `ProductCard` dentro da grade em `Pages/Produtos.razor`, com nome, categoria, descrição truncada em 120 caracteres, preço e o botão "Comprar na Amazon".

O objetivo é permitir que o usuário, antes de decidir pela compra, acione uma ação "Ver detalhes" em um produto e visualize um texto persuasivo (copywriting) gerado a partir das características do próprio produto (nome, categoria, preço e descrição). O texto deve destacar benefícios e incentivar a ação, mantendo o call-to-action existente de compra na Amazon dentro da visão de detalhes.

A geração do texto será **determinística e baseada em template** (montada a partir dos campos do produto no cliente), sem chamadas a serviços externos de IA, garantindo previsibilidade, testabilidade e funcionamento offline no WebAssembly.

## Glossary

- **Sistema_Detalhes**: Componente da interface responsável por exibir a visão de detalhes de um produto, incluindo o texto persuasivo e o call-to-action de compra.
- **Gerador_Copy**: Função pura, determinística e baseada em template que produz o texto persuasivo a partir das características de um `Produto`.
- **Produto**: Registro de dados definido em `Models/Product.cs`, com os campos Id, Name, Description, ImageUrl, Category, Price e AmazonUrl.
- **Texto_Persuasivo**: Texto de marketing (copywriting) gerado pelo Gerador_Copy, orientado a benefícios e com incentivo à ação.
- **Card_Produto**: Componente `ProductCard` que representa um produto na grade da página de produtos.
- **Acao_Ver_Detalhes**: Controle acionável (botão) no Card_Produto que abre a visão de detalhes do produto correspondente.
- **CTA_Amazon**: Link "Comprar na Amazon" que direciona à URL de compra do produto (`AmazonUrl`).

## Requirements

### Requirement 1: Acionar a visão de detalhes

**User Story:** Como usuário navegando pela vitrine, quero acionar "Ver detalhes" em um produto, para conhecer mais sobre ele antes de decidir comprar.

#### Acceptance Criteria

1. THE Card_Produto SHALL exibir um controle Acao_Ver_Detalhes rotulado com o texto visível "Ver detalhes".
2. WHEN o usuário aciona a Acao_Ver_Detalhes de um Produto, THE Sistema_Detalhes SHALL exibir, em até 1 segundo, a visão de detalhes correspondente ao mesmo Produto identificado pelo seu Id.
3. THE Acao_Ver_Detalhes SHALL expor um nome acessível composto pelo texto "Ver detalhes" seguido do nome do Produto correspondente, de modo que cada controle da grade seja distinguível por leitores de tela.
4. WHILE a visão de detalhes está aberta, THE Sistema_Detalhes SHALL disponibilizar um controle de fechamento acionável por clique e por teclado (tecla Enter, Espaço ou Esc).
5. WHEN o usuário aciona o controle de fechamento, THE Sistema_Detalhes SHALL fechar a visão de detalhes, retornar à grade de produtos e restaurar o foco para a Acao_Ver_Detalhes do Produto que originou a abertura.
6. IF o Produto correspondente à Acao_Ver_Detalhes acionada não puder ser resolvido pelo seu Id, THEN THE Sistema_Detalhes SHALL manter a grade de produtos visível e exibir uma mensagem indicando que os detalhes do produto estão indisponíveis, sem abrir a visão de detalhes.

### Requirement 2: Exibir o texto persuasivo gerado

**User Story:** Como usuário interessado em um produto, quero ler um texto que destaque os benefícios do produto, para me sentir mais seguro na decisão de compra.

#### Acceptance Criteria

1. WHEN a visão de detalhes de um produto é exibida, THE Sistema_Detalhes SHALL apresentar o Texto_Persuasivo gerado pelo Gerador_Copy para aquele produto em uma área de texto visível, com no mínimo 1 e no máximo 600 caracteres.
2. THE Gerador_Copy SHALL compor o Texto_Persuasivo utilizando o nome, a categoria, o preço e a descrição do Produto, incluindo o valor de cada um desses campos que não esteja vazio.
3. IF a descrição do Produto estiver vazia, THEN THE Gerador_Copy SHALL compor o Texto_Persuasivo utilizando apenas o nome, a categoria e o preço, sem inserir marcadores de campo ausente no texto exibido.
4. THE Texto_Persuasivo SHALL conter uma frase distinta de incentivo à ação de compra, apresentada como a última frase do texto.
5. THE Sistema_Detalhes SHALL exibir o nome do produto (com no máximo 100 caracteres), a categoria e o preço no formato "R$ 0,00" na visão de detalhes.
6. IF o Gerador_Copy não conseguir compor o Texto_Persuasivo para o produto, THEN THE Sistema_Detalhes SHALL exibir a descrição original do Produto no lugar do Texto_Persuasivo e manter visíveis o nome, a categoria e o preço do produto.

### Requirement 3: Geração determinística do texto (Gerador_Copy)

**User Story:** Como desenvolvedor, quero que o texto seja gerado de forma determinística a partir dos dados do produto, para que o comportamento seja previsível e testável.

#### Acceptance Criteria

1. WHEN o Gerador_Copy recebe o mesmo Produto, THE Gerador_Copy SHALL produzir exatamente o mesmo Texto_Persuasivo, de forma independente da cultura ambiente, da hora ou de qualquer fonte aleatória.
2. THE Gerador_Copy SHALL formatar o preço no Texto_Persuasivo usando a cultura pt-BR no formato "R$ 0,00", com exatamente duas casas decimais.
3. THE Gerador_Copy SHALL produzir o Texto_Persuasivo sem realizar chamadas de rede ou a serviços externos.
4. WHERE a descrição do Produto excede 120 caracteres (limite exibido no Card_Produto), THE Sistema_Detalhes SHALL apresentar a descrição completa na visão de detalhes.

### Requirement 4: Comportamento com dados ausentes (fallback)

**User Story:** Como usuário, quero que a visão de detalhes continue legível mesmo quando algum dado do produto está ausente, para não encontrar textos quebrados ou vazios.

#### Acceptance Criteria

1. IF a descrição do Produto está vazia, nula ou contém apenas espaços em branco, THEN THE Gerador_Copy SHALL compor o Texto_Persuasivo utilizando os campos disponíveis entre nome, categoria e preço, sem inserir espaço reservado, marcador de posição ou token não resolvido referente à descrição.
2. IF a categoria do Produto está vazia, nula ou contém apenas espaços em branco, THEN THE Gerador_Copy SHALL compor o Texto_Persuasivo omitindo qualquer referência à categoria, sem inserir espaço reservado ou token não resolvido referente à categoria.
3. IF o preço do Produto é menor ou igual a zero, THEN THE Sistema_Detalhes SHALL exibir a visão de detalhes sem apresentar qualquer referência de preço no Texto_Persuasivo.
4. WHILE o nome do Produto contém ao menos um caractere que não seja espaço em branco, THE Gerador_Copy SHALL retornar um Texto_Persuasivo com pelo menos 1 caractere não branco e sem espaços reservados, marcadores de posição ou tokens não resolvidos.
5. IF o nome do Produto está vazio, nulo ou contém apenas espaços em branco, THEN THE Sistema_Detalhes SHALL exibir a visão de detalhes com uma indicação de conteúdo indisponível no lugar do Texto_Persuasivo, sem exibir texto quebrado, vazio ou token não resolvido.

### Requirement 5: Preservar o call-to-action de compra

**User Story:** Como usuário convencido pelo texto de detalhes, quero comprar o produto na Amazon a partir da própria visão de detalhes, para concluir a ação sem retornar à grade.

#### Acceptance Criteria

1. WHERE o Produto possui AmazonUrl preenchida e em formato canônico válido, THE Sistema_Detalhes SHALL exibir o CTA_Amazon rotulado exatamente como "Comprar na Amazon" e visível dentro da visão de detalhes em até 1 segundo após a abertura da visão.
2. WHEN o usuário aciona o CTA_Amazon, THE Sistema_Detalhes SHALL abrir a AmazonUrl do produto em uma nova aba com os atributos de segurança "noopener" e "noreferrer", mantendo a visão de detalhes atual aberta e inalterada.
3. IF a AmazonUrl do Produto está vazia ou nula, THEN THE Sistema_Detalhes SHALL ocultar o CTA_Amazon na visão de detalhes.
4. IF a AmazonUrl do Produto está preenchida porém em formato inválido, THEN THE Sistema_Detalhes SHALL ocultar o CTA_Amazon na visão de detalhes.
5. IF o acionamento do CTA_Amazon não consegue abrir a nova aba, THEN THE Sistema_Detalhes SHALL manter a visão de detalhes atual aberta e apresentar uma indicação de erro informando que a compra não pôde ser aberta.

### Requirement 6: Acessibilidade e integração com a grade

**User Story:** Como usuário que utiliza teclado ou leitor de tela, quero navegar e fechar a visão de detalhes de forma acessível, para usar a funcionalidade sem barreiras.

#### Acceptance Criteria

1. WHEN a visão de detalhes é exibida, THE Sistema_Detalhes SHALL mover, em até 500 ms, o foco do teclado para o primeiro elemento focável dentro da visão de detalhes.
2. WHILE a visão de detalhes está aberta, THE Sistema_Detalhes SHALL manter o foco do teclado contido dentro da visão de detalhes (focus trap).
3. WHEN o usuário pressiona a tecla Escape com a visão de detalhes aberta, THE Sistema_Detalhes SHALL fechar a visão de detalhes.
4. WHEN a visão de detalhes é fechada, THE Sistema_Detalhes SHALL retornar o foco do teclado para a Acao_Ver_Detalhes que a abriu.
5. THE Sistema_Detalhes SHALL expor a visão de detalhes com papel (role) de diálogo e um nome acessível igual ao nome do Produto exibido, de modo que leitores de tela identifiquem o conteúdo.
6. IF a Acao_Ver_Detalhes que originou a abertura não existe mais quando a visão de detalhes é fechada, THEN THE Sistema_Detalhes SHALL mover o foco do teclado para um elemento focável estável da grade de produtos.
7. THE Sistema_Detalhes SHALL preservar os recursos existentes da grade de produtos (busca, filtro por categoria e favoritos) de modo que seus resultados permaneçam equivalentes aos obtidos sem a funcionalidade de detalhes.
```
