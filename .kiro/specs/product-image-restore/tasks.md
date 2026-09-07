# Implementation Plan: Restauração da imagem do produto "Dispenser Quadrado Flow 1L - Tampa Branca"

## Overview

Este plano corrige o asset de imagem ausente do produto "Dispenser Quadrado Flow 1L - Tampa Branca", cujo `imageUrl` (`images/products/dispenser-flow-quadrado-branco-001.jpg`) aponta para um arquivo inexistente em `wwwroot/`, resultando em imagem quebrada. Seguindo a metodologia de condição de bug, primeiro escrevemos um teste exploratório (Property 1: Bug Condition) que falha no código não corrigido, depois testes de preservação (Property 2: Preservation) que confirmam o comportamento a manter, aplicamos a correção recriando o asset a partir da imagem de origem BCF e, por fim, validamos que o bug foi resolvido sem regressões.

## Task Dependency Graph

A ordem de execução é obrigatória e deve respeitar as seguintes dependências:

- **Task 1 (Property 1: Bug Condition)** e **Task 2 (Property 2: Preservation)** devem ser executadas ANTES da correção (Task 3). Ambas rodam sobre o código NÃO corrigido: a Task 1 deve FALHAR (confirma o bug) e a Task 2 deve PASSAR (estabelece a linha de base).
- Task 1 e Task 2 são independentes entre si e podem ser escritas em qualquer ordem, mas ambas precedem a Task 3.
- **Task 3.1 (aplicar a correção)** depende da conclusão das Tasks 1 e 2, e deve ocorrer ANTES das sub-tarefas de verificação 3.2 e 3.3.
- **Task 3.2 (Fix Checking)** e **Task 3.3 (Preservation Checking)** dependem de 3.1 e re-executam, respectivamente, os testes das Tasks 1 e 2.
- **Task 4 (Checkpoint)** é a última e depende da conclusão de todas as tarefas anteriores.

Resumo do ordenamento: `1, 2 → 3.1 → 3.2, 3.3 → 4`.

```json
{
  "waves": [
    { "id": 0, "tasks": ["1", "2"] },
    { "id": 1, "tasks": ["3.1"] },
    { "id": 2, "tasks": ["3.2", "3.3"] },
    { "id": 3, "tasks": ["4"] }
  ]
}
```

## Tasks

- [x] 1. Escrever o teste exploratório da condição do bug (ANTES de recriar o asset)
  - **Property 1: Bug Condition** - Imagem do produto resolve para arquivo existente
  - **CRÍTICO**: Este teste DEVE FALHAR no código não corrigido — a falha confirma que o bug existe (asset ausente)
  - **NÃO tente corrigir o teste nem o código quando ele falhar** nesta etapa
  - **NOTA**: Este teste codifica o comportamento esperado — ele validará a correção quando passar após a implementação (Task 3)
  - **OBJETIVO**: Revelar counterexamples que demonstram o bug — o `imageUrl` aponta para um arquivo inexistente em `wwwroot/`
  - **Abordagem PBT escopada (Scoped PBT)**: como o bug é determinístico (asset ausente), escope a propriedade ao(s) caso(s) concreto(s) de falha para garantir reprodutibilidade
  - Escrever teste baseado em propriedade que, para TODO produto carregado de `wwwroot/data/products.json`, verifique que o arquivo apontado por `imageUrl` existe em `wwwroot/` (invariante do catálogo)
  - Codificar a condição do bug do design: `isBugCondition(X) = NOT fileExists(wwwrootPath(X.imageUrl))`
  - A asserção do teste deve corresponder à Expected Behavior Property (Property 1 do design): para todo produto, `fileExists(wwwrootPath(imageUrl))` é verdadeiro
  - Caso concreto (counterexample esperado): `dispenser-flow-quadrado-branco-001` → `wwwroot/images/products/dispenser-flow-quadrado-branco-001.jpg` NÃO existe
  - Adicionar também a verificação de que a origem BCF existe: `wwwroot/images/products/DFW300/DFW300_DISPENSER QUADRADO Flow 1L_BCF.jpg` (deve passar — confirma que a origem está disponível)
  - Executar o teste no código NÃO corrigido
  - **RESULTADO ESPERADO**: o teste FALHA (correto — prova que o bug existe: arquivo do produto ausente)
  - Documentar os counterexamples encontrados (ex.: "imageUrl `images/products/dispenser-flow-quadrado-branco-001.jpg` não resolve para um arquivo existente em wwwroot")
  - Marcar a tarefa como concluída quando o teste estiver escrito, executado e a falha documentada
  - _Requirements: 1.1, 1.2, 2.1, 2.2_

- [x] 2. Escrever os testes de preservação baseados em propriedades (ANTES de recriar o asset)
  - **Property 2: Preservation** - Entradas sem bug permanecem idênticas
  - **IMPORTANTE**: Seguir a metodologia observation-first — observar o comportamento no código NÃO corrigido antes de escrever as asserções
  - Observar: um produto cujo `imageUrl` aponta para um arquivo existente (`isBugCondition` retorna `false`) exibe sua imagem normalmente e resolve para o mesmo caminho antes e depois da correção (Req 3.1)
  - Observar: o markup de `Shared/ProductCard.razor` mantém o handler `onerror` apontando para `images/placeholder-product.png` (Req 3.2)
  - Observar: os demais campos do produto (nome, descrição, categoria, preço, `amazonUrl`) são renderizados sem alteração (Req 3.3)
  - Escrever testes baseados em propriedades capturando os padrões observados a partir da seção Preservation Requirements do design (gerar produtos variados; para os que NÃO satisfazem a condição do bug, `F(X) = F'(X)`)
  - Testes baseados em propriedades geram muitos casos automaticamente, fornecendo garantia mais forte de que o comportamento é inalterado
  - Guarda de preservação de código: assertar que o `onerror` do `ProductCard.razor` permanece `images/placeholder-product.png` (o código não será tocado pela correção)
  - Executar os testes no código NÃO corrigido
  - **RESULTADO ESPERADO**: os testes PASSAM (confirma a linha de base a preservar)
  - Marcar a tarefa como concluída quando os testes estiverem escritos, executados e passando no código não corrigido
  - _Requirements: 3.1, 3.2, 3.3_

- [x] 3. Correção para o asset de imagem ausente do "Dispenser Quadrado Flow 1L - Tampa Branca"

  - [x] 3.1 Recriar o asset esperado (Opção A)
    - Copiar a imagem de origem `wwwroot/images/products/DFW300/DFW300_DISPENSER QUADRADO Flow 1L_BCF.jpg` (variação BCF, "Tampa Branca") para o caminho exato referenciado pelo `imageUrl`: `wwwroot/images/products/dispenser-flow-quadrado-branco-001.jpg`
    - Escolher a variação `BCF` porque o produto é a "Tampa Branca"; `BGF` e `CHF` são variações de acabamento diferentes
    - NÃO alterar `wwwroot/data/products.json` — o `imageUrl` permanece `images/products/dispenser-flow-quadrado-branco-001.jpg`
    - NÃO alterar `Shared/ProductCard.razor` — a `<img src>` e o handler `onerror` permanecem inalterados
    - NÃO recriar `images/placeholder-product.png` (fora de escopo; preserva o requisito 3.2)
    - Confirmar que o arquivo de destino é uma imagem válida (não vazio, extensão `.jpg`) e está incluído no diretório publicado `wwwroot/`
    - _Bug_Condition: isBugCondition(input) = NOT fileExists(wwwrootPath(input.imageUrl)) — do design_
    - _Expected_Behavior: para todo input onde isBugCondition é verdadeiro, fileExists(wwwrootPath(result.imageUrl)) AND imageRendersCorrectly(result) — do design_
    - _Preservation: Preservation Requirements do design (Req 3.1, 3.2, 3.3)_
    - _Requirements: 2.1, 2.2_

  - [x] 3.2 Verificar que o teste exploratório da condição do bug agora passa (Fix Checking)
    - **Property 1: Expected Behavior** - Imagem do produto resolve para arquivo existente
    - **IMPORTANTE**: Re-executar o MESMO teste da Task 1 — NÃO escrever um teste novo
    - O teste da Task 1 codifica o comportamento esperado; quando ele passa, confirma que a Expected Behavior é satisfeita
    - Executar o teste exploratório da condição do bug da Task 1
    - **RESULTADO ESPERADO**: o teste PASSA — `wwwroot/images/products/dispenser-flow-quadrado-branco-001.jpg` agora existe e todo `imageUrl` do catálogo resolve para um arquivo existente (confirma que o bug foi corrigido)
    - _Requirements: 2.1, 2.2 (Expected Behavior Properties do design)_

  - [x] 3.3 Verificar que os testes de preservação continuam passando (Preservation Checking)
    - **Property 2: Preservation** - Entradas sem bug permanecem idênticas
    - **IMPORTANTE**: Re-executar os MESMOS testes da Task 2 — NÃO escrever testes novos
    - Executar os testes de preservação baseados em propriedades da Task 2
    - **RESULTADO ESPERADO**: os testes PASSAM (confirma ausência de regressões) — resolução de imagens já existentes, fallback `onerror` do `ProductCard.razor` e renderização de nome/descrição/categoria/preço/`amazonUrl` inalterados
    - Confirmar que todos os testes continuam passando após a correção (sem regressões)
    - _Requirements: 3.1, 3.2, 3.3_

- [x] 4. Checkpoint - Garantir que todos os testes passam
  - Executar a suíte completa de testes (`dotnet test`) e garantir que todos passam
  - Confirmar que o teste da condição do bug (Property 1) passa e os testes de preservação (Property 2) continuam passando
  - Verificar manualmente que o produto "Dispenser Quadrado Flow 1L - Tampa Branca" exibe sua imagem sem imagem quebrada
  - Se surgirem dúvidas ou algum teste falhar de forma inesperada, perguntar ao usuário antes de prosseguir

## Notes

- **Metodologia**: A ordem exploração → preservação → correção → validação é intencional. As Tasks 1 e 2 devem rodar sobre o código NÃO corrigido; a Task 1 falha (confirma o bug) e a Task 2 passa (estabelece a linha de base).
- **Escopo da correção**: A correção se limita a recriar o asset físico em `wwwroot/images/products/dispenser-flow-quadrado-branco-001.jpg`. Nenhum código (`ProductCard.razor`) nem dados (`products.json`) devem ser alterados.
- **Escolha da origem**: A variação `BCF` corresponde à "Tampa Branca"; as variações `BGF` e `CHF` são acabamentos diferentes e não devem ser usadas.
- **Fora de escopo**: Não recriar `images/placeholder-product.png` — isso preservaria o requisito 3.2 e está fora do escopo deste bugfix.
- **Referências**: Bug Condition, Expected Behavior e Preservation Requirements são derivados de `design.md` do spec `product-image-restore`.
