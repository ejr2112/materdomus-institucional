# Documento de Requisitos

## Introdução

A página de vitrine de produtos é uma nova seção do site institucional da MaterDomus que exibe os produtos comercializados pela empresa na Amazon. O visitante poderá navegar pelo catálogo, visualizar detalhes de cada produto, ser redirecionado à listagem da Amazon para efetuar a compra, e favoritar produtos para referência futura durante a sessão.

## Glossário

- **Vitrine**: A página do site que exibe o catálogo de produtos da MaterDomus disponíveis na Amazon.
- **Produto**: Item comercializado pela MaterDomus na Amazon, contendo nome, descrição, imagem, categoria, preço de referência e link da Amazon.
- **Card_Produto**: Componente visual que exibe as informações resumidas de um produto na vitrine.
- **Link_Amazon**: URL que direciona o usuário à página do produto na loja da Amazon.
- **Favorito**: Marcação de preferência que o usuário pode aplicar a um produto durante a sessão de navegação.
- **Lista_Favoritos**: Conjunto de produtos marcados como favorito pelo usuário na sessão atual.
- **Filtro**: Mecanismo que permite ao usuário restringir os produtos exibidos por critérios como categoria ou texto de busca.
- **Catalogo**: Conjunto de todos os produtos cadastrados e exibidos na Vitrine.

---

## Requisitos

### Requisito 1: Exibição da Vitrine de Produtos

**User Story:** Como visitante do site, quero visualizar os produtos da MaterDomus disponíveis na Amazon, para que eu possa conhecer o catálogo e decidir quais produtos me interessam.

#### Critérios de Aceite

1. WHEN a página da Vitrine é carregada e o Catalogo contém ao menos um produto, THE Vitrine SHALL exibir todos os produtos do Catalogo em uma grade de Card_Produto.
2. WHEN a página da Vitrine é carregada, THE Vitrine SHALL exibir um indicador de carregamento e renderizar os Card_Produto em no máximo 2 segundos; IF o carregamento exceder 10 segundos, THEN THE Vitrine SHALL exibir uma mensagem de erro e um botão para tentar novamente.
3. THE Card_Produto SHALL exibir a imagem do produto, o nome (com no máximo 100 caracteres), a categoria e o preço de referência no formato "R$ 0,00".
4. THE Card_Produto SHALL exibir a descrição do produto truncada em 120 caracteres seguidos de reticências ("...") quando o texto ultrapassar esse limite.
5. IF a imagem do produto não estiver disponível, THEN THE Card_Produto SHALL exibir uma imagem padrão de substituição no lugar da imagem ausente.
6. IF o Catalogo estiver vazio, THEN THE Vitrine SHALL exibir uma mensagem informando que nenhum produto está disponível no momento.

---

### Requisito 2: Redirecionamento para Compra na Amazon

**User Story:** Como visitante interessado em comprar, quero ser direcionado à loja da Amazon pelo site da MaterDomus, para que eu possa efetuar a compra do produto diretamente no marketplace.

#### Critérios de Aceite

1. WHEN o Link_Amazon de um produto estiver presente e não for uma string vazia, THE Card_Produto SHALL exibir um botão com o texto "Comprar na Amazon".
2. WHEN o usuário clica no botão "Comprar na Amazon", THE Vitrine SHALL abrir o Link_Amazon do produto em uma nova aba do navegador, mantendo a aba original do site da MaterDomus aberta.
3. THE Link_Amazon SHALL ser uma URL pertencente ao domínio amazon.com.br, sem redirecionamentos intermediários gerenciados pelo site.
4. IF o Link_Amazon de um produto estiver ausente ou for uma string vazia, THEN THE Card_Produto SHALL ocultar o botão "Comprar na Amazon", preservando o layout do card sem espaço em branco residual.

---

### Requisito 3: Favoritar Produtos

**User Story:** Como visitante, quero poder favoritar produtos de meu interesse, para que eu possa identificá-los facilmente durante minha navegação na vitrine.

#### Critérios de Aceite

1. THE Card_Produto SHALL exibir um ícone de favorito interativo, visualmente distinguível entre o estado ativo (favoritado) e o estado inativo (não favoritado).
2. WHEN o usuário clica no ícone de favorito de um produto não favoritado, THE Vitrine SHALL adicionar o produto à Lista_Favoritos e atualizar o ícone para o estado ativo em até 300ms.
3. WHEN o usuário clica no ícone de favorito de um produto já favoritado, THE Vitrine SHALL remover o produto da Lista_Favoritos e atualizar o ícone para o estado inativo em até 300ms.
4. WHILE a Lista_Favoritos contém ao menos um produto, THE Vitrine SHALL exibir um indicador com a contagem numérica exata de produtos favoritados, atualizado a cada adição ou remoção.
5. WHEN a sessão de navegação é encerrada ou a página é recarregada, THE Vitrine SHALL redefinir a Lista_Favoritos para vazia, sem persistir o estado de favoritos.
6. WHEN o usuário seleciona o filtro de favoritos, THE Vitrine SHALL exibir apenas os Card_Produto presentes na Lista_Favoritos.
7. WHEN o usuário seleciona o filtro de favoritos e a Lista_Favoritos está vazia, THE Vitrine SHALL exibir uma mensagem informando que nenhum produto foi favoritado.

---

### Requisito 4: Filtragem e Busca de Produtos

**User Story:** Como visitante, quero filtrar e buscar produtos na vitrine, para que eu possa encontrar rapidamente os itens de meu interesse sem precisar percorrer todo o catálogo.

#### Critérios de Aceite

1. THE Vitrine SHALL exibir um campo de busca por texto e filtros de seleção por categoria, sempre visíveis e acessíveis ao usuário.
2. WHEN o usuário digita no campo de busca, THE Vitrine SHALL atualizar os Card_Produto exibidos em até 300ms para incluir apenas os produtos cujo nome ou descrição contenha o termo buscado, sem diferenciar maiúsculas de minúsculas.
3. WHEN o usuário seleciona uma categoria no Filtro, THE Vitrine SHALL exibir apenas os Card_Produto pertencentes à categoria selecionada.
4. WHEN busca por texto e filtro por categoria estão ativos simultaneamente, THE Vitrine SHALL exibir apenas os produtos que satisfaçam ambas as condições.
5. IF nenhum produto corresponde aos critérios do Filtro ativos, THEN THE Vitrine SHALL exibir uma mensagem informando que nenhum produto foi encontrado, mantendo o campo de busca e os filtros de categoria visíveis e interativos.
6. WHEN todos os filtros ativos são removidos ou limpos, THE Vitrine SHALL restaurar a exibição de todos os produtos do Catalogo.

---

### Requisito 5: Navegação e Acessibilidade

**User Story:** Como visitante, quero que a vitrine seja acessível e responsiva, para que eu possa utilizá-la em qualquer dispositivo e com tecnologias assistivas.

#### Critérios de Aceite

1. THE Vitrine SHALL ser acessível via rota `/produtos` no site da MaterDomus.
2. THE Vitrine SHALL exibir um link de navegação "Produtos" no menu principal do site.
3. THE Vitrine SHALL adaptar a grade de Card_Produto ao tamanho da tela: 1 coluna em telas com largura inferior a 600px, 2 colunas entre 600px e 1023px, e no mínimo 3 colunas em telas com largura igual ou superior a 1024px.
4. THE Card_Produto SHALL ter `role="article"` e `aria-label` contendo o nome do produto para leitores de tela.
5. THE Vitrine SHALL ser navegável por teclado, permitindo que todos os controles interativos sejam alcançáveis via Tab e ativáveis via Enter ou Espaço.
6. WHEN o produto não está favoritado, THE ícone de favorito SHALL ter `aria-label="Favoritar produto"` e `aria-pressed="false"`.
7. WHEN o produto está favoritado, THE ícone de favorito SHALL ter `aria-label="Remover dos favoritos"` e `aria-pressed="true"`.
