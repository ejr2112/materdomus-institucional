# Requirements Document

## Introduction

Esta feature substitui o catálogo estático de demonstração do site institucional da MaterDomus por dados reais dos produtos disponíveis na vitrine oficial da loja MaterDomus na Amazon Brasil (seller ID `A20TN3HCSY6KZV`). O site é uma aplicação Blazor WebAssembly hospedada como site estático (Azure Static Web Apps) e não possui backend próprio; os dados de produto são servidos como arquivo JSON estático em `wwwroot/data/products.json`. A sincronização é, portanto, um processo editorial (curadoria e atualização manual do JSON e das imagens) acompanhado dos requisitos técnicos que garantem que o site exiba corretamente os produtos reais, incluindo preços, imagens, categorias e links de compra válidos.

## Glossary

- **Vitrine_Amazon**: Página pública da loja MaterDomus na Amazon Brasil, acessível via `https://www.amazon.com.br/s?me=A20TN3HCSY6KZV&marketplaceID=A2Q3Y263D00KWC`.
- **Catalogo**: Conjunto de registros de `Produto` presentes em `wwwroot/data/products.json` e exibidos na página `/produtos` do site.
- **Produto**: Registro individual com os campos `id`, `name`, `description`, `imageUrl`, `category`, `price` e `amazonUrl`, mapeados para o modelo `Product` em C#.
- **ASIN**: Identificador único de produto da Amazon, composto por exatamente 10 caracteres alfanuméricos maiúsculos (`[A-Z0-9]{10}`), usado para compor a `amazonUrl` no formato `https://www.amazon.com.br/dp/{ASIN}`.
- **Link_Produto_Amazon**: URL canônica de um produto na Amazon Brasil no formato `https://www.amazon.com.br/dp/{ASIN}`, sem parâmetros de rastreamento ou redirecionamento intermediário gerenciado pelo site.
- **Imagem_Produto**: Arquivo de imagem estático armazenado em `wwwroot/images/products/` e referenciado em `imageUrl` com caminho relativo (ex.: `images/products/{id}.webp` ou `images/products/{id}.jpg`).
- **Preco_Referencia**: Preço de venda corrente do produto na Amazon Brasil no momento da curadoria do Catalogo, expresso em reais (BRL) como valor decimal.
- **Categoria_Produto**: Classificação editorial atribuída a cada produto para agrupamento no filtro da Vitrine. Valores válidos: "Organização", "Cozinha", "Casa", "Limpeza".
- **Curadoria**: Processo manual de coleta, verificação e atualização dos dados do Catalogo a partir da Vitrine_Amazon.
- **Data_Curadoria**: Data de referência (ISO 8601) registrada no campo `curatedAt` do arquivo de metadados `wwwroot/data/catalog-meta.json`, indicando quando o Catalogo foi sincronizado pela última vez.
- **Catalogo_Meta**: Arquivo `wwwroot/data/catalog-meta.json` que registra metadados da última sincronização: `curatedAt`, `sourceUrl` e `productCount`.

---

## Requirements

### Requisito 1: Catálogo Composto Exclusivamente por Produtos Reais da Vitrine Amazon

**User Story:** Como visitante do site, quero visualizar apenas os produtos realmente vendidos pela MaterDomus na Amazon, para que eu possa clicar e comprar sem encontrar itens inexistentes ou links inválidos.

#### Critérios de Aceite

1. WHEN a Curadoria é concluída com sucesso, THE Catalogo SHALL conter somente produtos cujo ASIN foi confirmado como ativo na Vitrine_Amazon durante essa execução de Curadoria.
2. WHEN a Curadoria é concluída com sucesso, THE Catalogo SHALL conter zero produtos com `amazonUrl` apontando para ASINs que não estejam ativos na Vitrine_Amazon conforme verificado nessa execução de Curadoria.
3. WHEN o Catalogo é atualizado, THE Catalogo SHALL remover todos os registros de `Produto` cujo ASIN não conste mais como produto ativo na Vitrine_Amazon.
4. IF a execução de Curadoria resultaria em um Catalogo com zero produtos válidos, THEN THE Sistema SHALL rejeitar a atualização, preservar o Catalogo existente sem modificações e indicar erro informando que nenhum produto ativo foi encontrado na Vitrine_Amazon.
5. WHEN o arquivo `products.json` é carregado pelo site, THE Sistema SHALL rejeitar registros cujo campo `amazonUrl` não esteja no formato `https://www.amazon.com.br/dp/{ASIN}`, onde `{ASIN}` é uma sequência de exatamente 10 caracteres alfanuméricos maiúsculos (`[A-Z0-9]{10}`), e não os exibir na Vitrine.
6. IF a Curadoria falha antes de ser concluída, THEN THE Sistema SHALL preservar o Catalogo existente sem modificações e indicar erro informando que a atualização não foi aplicada.

---

### Requisito 2: Dados Exatos de Cada Produto

**User Story:** Como visitante, quero que o nome, a descrição, o preço e a imagem exibidos correspondam ao produto real na Amazon, para que eu tenha a expectativa correta antes de ir para a página de compra.

#### Critérios de Aceite

1. THE Produto SHALL ter o campo `name` preenchido com o nome do produto conforme exibido na página do ASIN na Amazon Brasil, com no mínimo 1 e no máximo 100 caracteres.
2. THE Produto SHALL ter o campo `description` preenchido com uma descrição legível do produto derivada das informações da página do ASIN, com no mínimo 1 e no máximo 500 caracteres.
3. THE Produto SHALL ter o campo `price` preenchido com o `Preco_Referencia` vigente na Amazon Brasil no momento da Curadoria, expresso como valor decimal em BRL com precisão de duas casas decimais, no intervalo de 0,01 a 999.999,99.
4. WHEN a `Imagem_Produto` local estiver disponível em `wwwroot/images/products/`, THE Produto SHALL ter o campo `imageUrl` preenchido com o caminho relativo dessa imagem local.
5. IF a `Imagem_Produto` local não estiver disponível em `wwwroot/images/products/`, THEN THE Produto SHALL ter o campo `imageUrl` preenchido com a URL absoluta da imagem oficial do produto na Amazon.
6. THE Produto SHALL ter o campo `category` preenchido com exatamente um dos seguintes valores: "Organização", "Cozinha", "Casa" ou "Limpeza".
7. IF o campo `price` de um `Produto` no `products.json` for nulo, ausente, zero ou negativo, THEN THE Sistema SHALL não exibir esse produto na Vitrine.
8. IF o campo `imageUrl` de um `Produto` no `products.json` for nulo ou ausente, THEN THE Sistema SHALL não exibir esse produto na Vitrine.

---

### Requisito 3: Link de Compra Direto e Rastreável

**User Story:** Como visitante pronto para comprar, quero que o botão "Comprar na Amazon" me leve diretamente ao produto certo na Amazon, para que eu não perca tempo procurando o item manualmente.

#### Critérios de Aceite

1. THE Produto SHALL ter o campo `amazonUrl` preenchido com o `Link_Produto_Amazon` canônico no formato `https://www.amazon.com.br/dp/{ASIN}`, onde `{ASIN}` é uma sequência de exatamente 10 caracteres alfanuméricos maiúsculos (`[A-Z0-9]{10}`).
2. WHEN o usuário clica em "Comprar na Amazon", THE Vitrine SHALL abrir uma nova aba do navegador com o `href` igual ao valor exato do campo `amazonUrl` do produto, sem modificação pelo site.
3. IF o ASIN de um produto não corresponde a um produto vendido pela conta seller `A20TN3HCSY6KZV` na Amazon Brasil, THEN THE Catalogo SHALL excluir esse produto antes de publicar a atualização.
4. IF o campo `amazonUrl` de um `Produto` estiver ausente, for uma string vazia, ou não estiver no formato canônico com ASIN válido (`[A-Z0-9]{10}`), THEN THE Catalogo SHALL remover esse `Produto` antes de publicar a atualização.

---

### Requisito 4: Arquivo de Metadados de Sincronização

**User Story:** Como mantenedor do site, quero saber quando o catálogo foi sincronizado pela última vez e quantos produtos estão ativos, para que eu possa identificar rapidamente se o catálogo está desatualizado.

#### Critérios de Aceite

1. THE Catalogo_Meta SHALL ser um arquivo JSON em `wwwroot/data/catalog-meta.json` com os campos `curatedAt` (string ISO 8601), `sourceUrl` (string) e `productCount` (inteiro não-negativo).
2. WHEN o Catalogo é atualizado, THE Catalogo_Meta SHALL registrar a `Data_Curadoria` da sincronização no campo `curatedAt` com precisão de dia (formato `YYYY-MM-DD`).
3. THE Catalogo_Meta SHALL registrar no campo `sourceUrl` a URL da Vitrine_Amazon utilizada como referência (`https://www.amazon.com.br/s?me=A20TN3HCSY6KZV&marketplaceID=A2Q3Y263D00KWC`).
4. THE Catalogo_Meta SHALL registrar no campo `productCount` o número de produtos com status ativo presentes no `products.json` após a atualização do catálogo.
5. IF o arquivo `catalog-meta.json` não existe ou contém JSON sintaticamente inválido ou ausência de qualquer campo obrigatório (`curatedAt`, `sourceUrl`, `productCount`), THEN THE Sistema SHALL carregar o `products.json` normalmente e omitir a exibição de qualquer informação de metadados de sincronização para o usuário.

---

### Requisito 5: Imagens de Produto Armazenadas Localmente

**User Story:** Como visitante, quero que as imagens dos produtos carreguem rapidamente e sem dependência de servidores externos, para que a vitrine funcione mesmo com restrições de CDN ou políticas de hotlinking da Amazon.

#### Critérios de Aceite

1. THE Imagem_Produto SHALL ser armazenada como arquivo estático no diretório `wwwroot/images/products/` com nome correspondente ao `id` do produto; quando ambos os formatos estiverem disponíveis para o mesmo `id`, o formato `.webp` SHALL ter precedência sobre `.jpg`.
2. THE Produto SHALL referenciar a `Imagem_Produto` via `imageUrl` com caminho relativo (ex.: `images/products/{id}.webp` ou `images/products/{id}.jpg`), sem uso de URLs absolutas externas como fonte primária.
3. IF a `Imagem_Produto` local retornar HTTP 404 ou ausência de arquivo, OU se o carregamento da imagem não for concluído em até 5 segundos, THEN THE Card_Produto SHALL exibir a imagem de substituição `images/placeholder-product.png`.
4. THE Imagem_Produto SHALL ter largura máxima de 800px e tamanho de arquivo de no máximo 200 KB, para garantir carregamento eficiente no contexto de hospedagem estática.

---

### Requisito 6: Validação de Integridade do Catálogo

**User Story:** Como desenvolvedor/mantenedor, quero que o sistema detecte e sinalize problemas de integridade no `products.json` ao carregar, para que erros de curadoria não causem exibição silenciosa de dados inválidos.

#### Critérios de Aceite

1. WHEN o `products.json` é carregado, THE Sistema SHALL validar que cada `Produto` possui os campos obrigatórios `id`, `name` e `price` preenchidos com valores não-vazios (após remoção de espaços) e `price` com valor maior que zero.
2. IF um `Produto` no `products.json` tiver o campo `id` duplicado em outro registro, THEN THE Sistema SHALL descartar o registro duplicado com índice de array mais alto (mantendo o de menor índice) e registrar um aviso no console do navegador.
3. IF o `products.json` contiver registros inválidos segundo os critérios do Requisito 6.1, THEN THE Sistema SHALL omitir esses registros da Vitrine e exibir os demais normalmente.
4. WHEN o `products.json` é carregado com sucesso e todos os registros são válidos, THE Sistema SHALL exibir a contagem exata de produtos na Vitrine sem mensagem de erro.
5. IF o `products.json` estiver malformado (JSON inválido), THEN THE Vitrine SHALL exibir a mensagem de estado de erro e o botão de nova tentativa, sem exibir mensagem de exceção não tratada ou stack trace para o usuário.

---

### Requisito 7: Atualização Periódica do Catálogo

**User Story:** Como mantenedor do site, quero que o processo de atualização do catálogo seja simples e documentado, para que qualquer membro da equipe possa sincronizar os produtos sem conhecimento técnico aprofundado.

#### Critérios de Aceite

1. THE Catalogo SHALL ter um arquivo `wwwroot/data/CATALOG_UPDATE.md` que descreva o processo passo a passo para atualizar o `products.json` e as `Imagem_Produto` com os produtos reais da Vitrine_Amazon, contendo no mínimo as seguintes seções: pré-requisitos, instruções de substituição do `products.json`, instruções de substituição das `Imagem_Produto`, e procedimento de redeploy dos arquivos estáticos.
2. THE Catalogo SHALL ser atualizado por substituição completa do arquivo `products.json` (não por patch incremental), garantindo que nenhum produto cujo identificador não conste no novo `products.json` permaneça visível no Catalogo após cada sincronização.
3. WHEN o redeploy dos arquivos estáticos é concluído após a substituição do `products.json`, THE Vitrine SHALL exibir o Catalogo atualizado sem necessidade de recompilação do projeto, em até 60 segundos após o redeploy.
4. WHEN o Processo_Curadoria avalia um produto para inclusão no Catalogo, THE Processo_Curadoria SHALL verificar que o `Link_Produto_Amazon` correspondente responde com HTTP 200 antes de incluir o produto no Catalogo atualizado.
5. IF o `Link_Produto_Amazon` de um produto não responde com HTTP 200 durante a execução do Processo_Curadoria, THEN THE Processo_Curadoria SHALL excluir esse produto do Catalogo atualizado e registrar o URL e o status de resposta obtido em um relatório de validação.
