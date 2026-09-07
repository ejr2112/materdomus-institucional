# Documento de Requisitos do Bugfix

## Introdução

O recurso de vídeo do produto foi adicionado à vitrine (commit `acc44a5` — "Adiciona video do produto via embed do OneDrive"), inserindo um `<iframe>` de reprodução no cartão do produto (`Shared/ProductCard.razor`) alimentado por um campo opcional `VideoUrl` no modelo `Product`, com o "Dispenser Quadrado Flow 1L" apontando para um embed do OneDrive/SharePoint. O player embutido não funcionou corretamente na vitrine, e o usuário solicitou a remoção completa do recurso de vídeo do produto.

O feature já foi objeto de um revert (commit `a646466`) que desfez as alterações versionadas. Este bugfix garante e formaliza que a vitrine de produtos não apresente nenhum player de vídeo e que nenhum vestígio do recurso (campo `VideoUrl`, `<iframe>`, CSS do player, URL de embed nos dados) permaneça, mantendo intacto todo o restante do comportamento do produto (imagem, nome, descrição, preço, categoria, favoritos, CTA da Amazon e detalhes).

## Análise do Bug

### Comportamento Atual (Defeito)

Quando o recurso de vídeo estava presente, o produto renderizava um player embutido que não funcionava corretamente.

1.1 QUANDO um produto possui `VideoUrl` preenchido ENTÃO o sistema renderiza um `<iframe>` de vídeo no cartão do produto que não reproduz o conteúdo corretamente (embed do OneDrive/SharePoint não funcional na vitrine)
1.2 QUANDO o catálogo (`products.json`) contém o campo `videoUrl` para um produto ENTÃO o sistema expõe esse campo e o utiliza para exibir o player defeituoso
1.3 QUANDO o modelo `Product` inclui a propriedade `VideoUrl` ENTÃO o sistema mantém a superfície de código do recurso de vídeo (marcação `<iframe>`, CSS `product-card__video`/`product-card__video-wrapper` e entradas de `.gitignore` para mídia `*.mov`/`*.mp4`)

### Comportamento Esperado (Correto)

O recurso de vídeo do produto deve ser completamente removido da vitrine e do código.

2.1 QUANDO um produto é exibido no cartão ENTÃO o sistema SHALL não renderizar nenhum `<iframe>` ou player de vídeo, independentemente de haver dados de vídeo
2.2 QUANDO o catálogo (`products.json`) é carregado ENTÃO o sistema SHALL não conter nenhum campo `videoUrl` nos dados de produto
2.3 QUANDO o código-fonte é inspecionado ENTÃO o sistema SHALL não conter a propriedade `VideoUrl` no modelo `Product`, nem a marcação de `<iframe>` de vídeo em `ProductCard.razor`, nem as regras CSS do player (`product-card__video`, `product-card__video-wrapper`), nem entradas de `.gitignore` introduzidas exclusivamente para o recurso de vídeo (`*.mov`, `*.mp4`)

### Comportamento Inalterado (Prevenção de Regressão)

Todo o restante da exibição e funcionalidade do produto deve permanecer funcionando.

3.1 QUANDO um produto é exibido no cartão ENTÃO o sistema SHALL CONTINUAR A mostrar a imagem, o nome, a descrição truncada, o preço formatado e a categoria corretamente
3.2 QUANDO um produto possui `AmazonUrl` válida ENTÃO o sistema SHALL CONTINUAR A exibir o botão "Comprar na Amazon" com os atributos de segurança (`target="_blank"`, `rel="noopener noreferrer"`)
3.3 QUANDO o usuário aciona o botão de favoritar ENTÃO o sistema SHALL CONTINUAR A adicionar/remover o produto dos favoritos e atualizar o contador
3.4 QUANDO o usuário aciona "Ver detalhes" ENTÃO o sistema SHALL CONTINUAR A abrir a visão de detalhes (modal) com nome, preço, texto persuasivo e CTA da Amazon, e a fechá-la por clique, Enter/Espaço e Escape
3.5 QUANDO o usuário utiliza busca, filtro por categoria ou o filtro de favoritos ENTÃO o sistema SHALL CONTINUAR A filtrar a grade de produtos corretamente
3.6 QUANDO a página de produtos carrega ENTÃO o sistema SHALL CONTINUAR A exibir os estados de carregamento (skeleton), erro (com "Tentar novamente") e vazio conforme já implementado
