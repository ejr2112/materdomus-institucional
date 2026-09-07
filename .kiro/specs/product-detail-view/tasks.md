# Implementation Plan: Visão de Detalhes de Produto

## Overview

Este plano converte o design em passos incrementais de código para o projeto MaterDomus (Blazor WebAssembly, .NET 9). A ordem segue as dependências naturais: primeiro o núcleo puro determinístico (`ProductCopyGenerator`) e seus testes de propriedade, depois os componentes de UI e sua fiação (`ProductCard`, `ProductDetail`, `Produtos.razor`), em seguida o interop de JS e a acessibilidade, o CSS, e por fim os testes de render/integração complementares.

Todas as tarefas são atividades de código construíveis e testáveis com `dotnet build` e `dotnet test` a partir da solução `MaterDomus.sln`. Cada tarefa referencia os requisitos e/ou propriedades de correção que implementa. Os testes de propriedade usam **FsCheck** (mínimo 100 iterações), reutilizam os geradores de `AmazonUrlValidationTests` para `AmazonUrl`, e cada propriedade de correção é implementada por **exatamente um** teste de propriedade, anotado no formato `// Feature: product-detail-view, Property {n}: {texto}`.

## Tasks

- [x] 1. Criar o gerador de copy puro determinístico
  - Criar `Helpers/ProductCopyGenerator.cs` como `static class` no namespace `MaterDomus.Web.Helpers`.
  - Implementar `public static string? Generate(Product product)` como função pura: sem `HttpClient`/I/O, sem `Random`, sem `DateTime.Now`, sem dependência de `CultureInfo.CurrentCulture`.
  - Compor o texto por segmentos condicionais: abertura usando o nome; categoria quando não em branco; preço quando `Price > 0` (formatado por `ProductHelpers.FormatPrice`); descrição completa quando não em branco; CTA de incentivo à compra como última frase.
  - Retornar `null` quando `Name` é nulo/vazio/só espaços; omitir campos ausentes sem placeholder, token ou espaço reservado; garantir limite de 1 a 600 caracteres na saída não-nula.
  - _Requirements: 2.1, 2.2, 2.3, 2.4, 3.1, 3.2, 3.3, 3.4, 4.1, 4.2, 4.3, 4.4, 4.5_

- [x] 2. Testes de propriedade do gerador de copy
  - [x]* 2.1 Escrever teste de propriedade de determinismo
    - **Property 1: Determinismo da geração** — chamar `Generate` duas vezes (inclusive alternando `CultureInfo.CurrentCulture` pt-BR/en-US) produz saída idêntica.
    - **Validates: Requirements 3.1, 3.3**
    - Anotar: `// Feature: product-detail-view, Property 1: Determinismo da geração`; usar `[Property(MaxTest = 100)]` ou superior.

  - [x]* 2.2 Escrever teste de propriedade de saída bem-formada
    - **Property 2: Saída bem-formada quando há nome** — nome com ao menos um caractere não-branco produz string de 1 a 600 chars, com caractere não-branco e sem tokens/placeholders (`{`, `}`, `{{`, `}}`).
    - **Validates: Requirements 2.1, 4.4**
    - Anotar: `// Feature: product-detail-view, Property 2: Saída bem-formada quando há nome`.

  - [x]* 2.3 Escrever teste de propriedade de nome em branco
    - **Property 3: Nome em branco produz ausência de texto** — nome nulo/vazio/só espaços faz `Generate` retornar `null`.
    - **Validates: Requirements 4.5**
    - Anotar: `// Feature: product-detail-view, Property 3: Nome em branco produz ausência de texto`.

  - [x]* 2.4 Escrever teste de propriedade de inclusão de campos válidos
    - **Property 4: Inclusão de campos válidos** — categoria não-branca aparece no texto; `Price > 0` aparece como `ProductHelpers.FormatPrice(Price)`; descrição não-branca aparece completa (não truncada).
    - **Validates: Requirements 2.2, 2.3, 3.2, 3.4**
    - Anotar: `// Feature: product-detail-view, Property 4: Inclusão de campos válidos`.

  - [x]* 2.5 Escrever teste de propriedade de omissão de descrição
    - **Property 5: Omissão limpa de descrição ausente** — descrição em branco não deixa resíduo nem token de descrição.
    - **Validates: Requirements 4.1**
    - Anotar: `// Feature: product-detail-view, Property 5: Omissão limpa de descrição ausente`.

  - [x]* 2.6 Escrever teste de propriedade de omissão de categoria
    - **Property 6: Omissão limpa de categoria ausente** — categoria em branco não deixa referência nem token de categoria.
    - **Validates: Requirements 4.2**
    - Anotar: `// Feature: product-detail-view, Property 6: Omissão limpa de categoria ausente`.

  - [x]* 2.7 Escrever teste de propriedade de omissão de preço
    - **Property 7: Omissão de preço não-positivo** — `Price <= 0` não gera referência de preço (nem valor formatado, nem placeholder).
    - **Validates: Requirements 4.3**
    - Anotar: `// Feature: product-detail-view, Property 7: Omissão de preço não-positivo`.

  - [x]* 2.8 Escrever teste de propriedade do CTA como última frase
    - **Property 8: CTA é a última frase** — a última frase da saída não-nula é a frase de incentivo à compra, distinta do restante.
    - **Validates: Requirements 2.4**
    - Anotar: `// Feature: product-detail-view, Property 8: CTA é a última frase`.

- [x] 3. Checkpoint - Garantir que os testes do gerador passam
  - Garantir que todos os testes passam (`dotnet build` e `dotnet test`); em caso de dúvidas, perguntar ao usuário.

- [x] 4. Adicionar a ação "Ver detalhes" ao ProductCard
  - Modificar `Shared/ProductCard.razor` adicionando o parâmetro `[Parameter] public EventCallback<Product> OnVerDetalhes { get; set; }`.
  - Adicionar, na região `product-card__actions`, um `<button type="button">` com texto visível "Ver detalhes" e `aria-label="@($"Ver detalhes {Product.Name}")"`, disparando `OnVerDetalhes.InvokeAsync(Product)`.
  - Manter o CTA_Amazon e o botão de favorito existentes inalterados.
  - _Requirements: 1.1, 1.3_

- [x] 5. Criar o componente modal ProductDetail
  - [x] 5.1 Criar a estrutura e a apresentação do diálogo
    - Criar `Shared/ProductDetail.razor` com parâmetros `Product? Product`, `bool IsOpen`, `EventCallback OnClose`.
    - Renderizar backdrop e `<div role="dialog" aria-modal="true" aria-label="@Product.Name">`; exibir nome (≤ 100 chars), categoria e preço "R$ 0,00" via `ProductHelpers.FormatPrice`.
    - Chamar `ProductCopyGenerator.Generate(Product)` em `OnParametersSet`; exibir o texto persuasivo, ou a descrição original completa como fallback quando o texto é nulo com nome válido, ou indicação de "Conteúdo indisponível" quando o gerador retorna `null` por nome em branco.
    - _Requirements: 2.1, 2.5, 2.6, 3.4, 4.5, 6.5_

  - [x] 5.2 Adicionar o CTA_Amazon guardado por validação
    - Renderizar `<a target="_blank" rel="noopener noreferrer">` rotulado "Comprar na Amazon", visível somente quando `ProductCatalogService.IsValidAmazonUrl(Product.AmazonUrl)` é verdadeiro.
    - Tratar falha ao abrir a nova aba exibindo mensagem "Não foi possível abrir a compra." sem fechar o modal.
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5_

  - [x] 5.3 Adicionar controle de fechamento e ciclo de vida
    - Adicionar botão de fechar acionável por clique e teclado (Enter/Espaço/Esc) que dispara `OnClose`.
    - Implementar `IAsyncDisposable` para liberar `DotNetObjectReference` e remover listeners JS.
    - Adicionar método `[JSInvokable] OnEscapePressed()` que fecha o modal via callback de JS.
    - _Requirements: 1.4, 6.3_

- [x] 6. Criar o módulo de interop JS de foco e acessibilidade
  - Criar `wwwroot/js/detailModal.js` como ES module (convenção de `scrollBehavior.js`) exportando `open(dialogEl, dotNetRef)`, `close()` e `restoreFocus(triggerSelector, fallbackSelector)`.
  - `open`: selecionar focáveis (`a[href], button:not([disabled]), input, [tabindex]:not([tabindex="-1"])`), focar o primeiro (≤ 500 ms), instalar focus trap (Tab/Shift+Tab) e listener de Escape chamando `dotNetRef.invokeMethodAsync('OnEscapePressed')`.
  - `close`: remover listeners. `restoreFocus`: focar o gatilho original; se ausente, focar o fallback estável da grade.
  - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.6_

- [x] 7. Fiar o interop JS ao componente ProductDetail
  - Injetar `IJSRuntime` em `ProductDetail.razor` e importar o módulo `detailModal.js`.
  - Em `OnAfterRenderAsync` (primeira renderização com modal aberto), invocar `detailModal.open` passando o elemento do diálogo e o `DotNetObjectReference`; envolver as chamadas de interop em `try/catch` para degradação graciosa.
  - No fechamento, invocar `detailModal.close` e `detailModal.restoreFocus` para restaurar o foco ao gatilho ou ao fallback da grade.
  - _Requirements: 6.1, 6.2, 6.4, 6.6_

- [x] 8. Integrar o modal na página Produtos
  - Modificar `Pages/Produtos.razor` adicionando o estado `_selectedProduct`, `_isDetailOpen` e `_detailUnavailable`.
  - Implementar `AbrirDetalhes(Product)` resolvendo o produto por `Id` em `_allProducts` (se não resolvido, definir `_detailUnavailable = true` sem abrir o modal) e `FecharDetalhes()` zerando o estado.
  - Passar `OnVerDetalhes="AbrirDetalhes"` ao `<ProductCard>`; hospedar `<ProductDetail Product="_selectedProduct" IsOpen="_isDetailOpen" OnClose="FecharDetalhes" />` fora da grade; exibir faixa de aviso quando `_detailUnavailable`.
  - Não alterar a lógica de busca, filtro por categoria nem favoritos.
  - _Requirements: 1.2, 1.5, 1.6, 6.7_

- [x] 9. Adicionar o CSS do modal
  - Criar as regras de estilo do modal (backdrop e diálogo) seguindo a convenção de `produtos.css`, incluindo o botão "Ver detalhes" do card e a faixa de indisponibilidade.
  - _Requirements: 1.1, 1.4_

- [x] 10. Checkpoint - Garantir build e testes após a fiação de UI
  - Garantir que `dotnet build` e `dotnet test` passam; em caso de dúvidas, perguntar ao usuário.

- [x] 11. Testes de render e propriedade da UI
  - [x]* 11.1 Escrever teste de propriedade de visibilidade do CTA_Amazon
    - **Property 9: Visibilidade condicional do CTA_Amazon** — o link "Comprar na Amazon" (`target="_blank"`, `rel="noopener noreferrer"`) está presente se e somente se `IsValidAmazonUrl(Product.AmazonUrl)` é verdadeiro. Reutilizar os geradores de `AmazonUrlValidationTests`.
    - **Validates: Requirements 5.1, 5.2, 5.3, 5.4**
    - Teste bUnit + FsCheck; anotar: `// Feature: product-detail-view, Property 9: Visibilidade condicional do CTA_Amazon`.

  - [x]* 11.2 Escrever teste de propriedade de nome acessível
    - **Property 10: Nome acessível do diálogo e do gatilho** — o `role="dialog"` tem `aria-label` igual ao nome do produto e o botão "Ver detalhes" tem `aria-label` igual a `"Ver detalhes " + Nome`.
    - **Validates: Requirements 1.2, 1.3, 6.5**
    - Teste bUnit + FsCheck; anotar: `// Feature: product-detail-view, Property 10: Nome acessível do diálogo e do gatilho`.

  - [x]* 11.3 Escrever teste de propriedade de invariância da filtragem
    - **Property 11: Invariância da filtragem da grade** — para qualquer lista de produtos e combinação de busca/categoria/somente-favoritos, o resultado da filtragem em `Produtos.razor` é idêntico ao da lógica original.
    - **Validates: Requirements 6.7**
    - Anotar: `// Feature: product-detail-view, Property 11: Invariância da filtragem da grade`.

  - [x]* 11.4 Escrever testes de exemplo do ProductCard e Produtos.razor
    - Verificar botão "Ver detalhes" presente com texto visível e disparo de `OnVerDetalhes` com o produto correto (Req 1.1).
    - Verificar que `Id` não resolvível não abre o modal e exibe a faixa de indisponibilidade (Req 1.6).
    - _Requirements: 1.1, 1.6_

  - [x]* 11.5 Escrever testes de exemplo do ProductDetail
    - Verificar fallback para descrição original quando o texto é nulo com nome válido (Req 2.6); indicação de indisponível quando o nome é branco (Req 4.5); mensagem de erro do CTA sem fechar o modal (Req 5.5); `OnEscapePressed` fecha o modal (Req 6.3).
    - _Requirements: 2.6, 4.5, 5.5, 6.3_

- [x] 12. Checkpoint final - Garantir que todos os testes passam
  - Garantir que `dotnet build` e `dotnet test` passam; em caso de dúvidas, perguntar ao usuário.

## Notes

- Tarefas marcadas com `*` são opcionais (testes) e podem ser puladas para um MVP mais rápido.
- Cada tarefa referencia requisitos específicos para rastreabilidade.
- Os checkpoints garantem validação incremental.
- Testes de propriedade validam invariantes universais de correção (FsCheck, mínimo 100 iterações); testes unitários/bUnit validam exemplos e casos de borda.
- Cada propriedade de correção do design é implementada por exatamente um teste de propriedade, anotado com `// Feature: product-detail-view, Property {n}: {texto}`.
- Foco/focus trap/restauração de foco (Req 6.1, 6.2, 6.4, 6.6) e ausência de rede (Req 3.3) não são cobertos por PBT; são garantidos estruturalmente e/ou por verificação de integração/manual, conforme a estratégia de testes do design.

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1"] },
    { "id": 1, "tasks": ["2.1", "2.2", "2.3", "2.4", "2.5", "2.6", "2.7", "2.8", "4", "6"] },
    { "id": 2, "tasks": ["5.1"] },
    { "id": 3, "tasks": ["5.2", "5.3"] },
    { "id": 4, "tasks": ["7"] },
    { "id": 5, "tasks": ["8", "9"] },
    { "id": 6, "tasks": ["11.1", "11.2", "11.3", "11.4", "11.5"] }
  ]
}
```
