# Atualização do catálogo (Linha Flow)

O catálogo institucional publicado é o arquivo estático `wwwroot/data/products.json`.
Não há caminho provisório, draft ou unpublished: `/produtos` lê esse JSON.
A vitrine lista **somente** os SKUs da loja Mater Domus no Seller Central
(`A20TN3HCSY6KZV`). Não invente produto, ASIN, estoque ou combinação.

Última curadoria Seller Central: **2026-10-20** (~16:00 BRT).
Metadados: `wwwroot/data/catalog-meta.json`.
No `products.json`, todos os itens com `comingSoon: false` vêm primeiro; Em breve
fica depois. Dentro de cada grupo a ordem relativa anterior é mantida.

## Regra de liberação (FBA Available > 0)

Liberar **Comprar na Amazon** somente quando o listing está buyable
(FBA Available > 0). Listing **Active com Available = 0** continua **Em breve**
— não é estoque. Não invente quantidade.

| Amazon title | ASIN | SKU Seller | Seller 2026-10-20 | Site |
|---|---|---|---|---|
| Ou Dispenser Quadrado 1L Branco Linha Flow | `B0GKPPS5YH` | MD-OU-0001 | FBA Available 4 | `comingSoon=false`, `amazonUrl` com `?m=A20TN3HCSY6KZV` |
| Ou Dispenser Quadrado 1,5L Branco Linha Flow | `B0GKPZMCYZ` | MD-OU-B0GKPZMCYZ | FBA Available 0 | Em breve, `amazonUrl` vazia |
| Ou Rodo Bege Linha Flow | `B0F8PWY3M5` | 6L-N5BY-LSRC | FBA Available 0 | Em breve, `amazonUrl` vazia |
| Ou Rodo Multiuso Bege Linha Flow | `B0CZTTVLWK` | MD-OU-B0CZTTVLWK | FBA Available 0 | Em breve, `amazonUrl` vazia |
| Ou Pano para Chão de Microfibra Chumbo Linha Flow | `B0FXBN7SCB` | 2L-J74H-R1IC | FBA Available 0 | Em breve, `amazonUrl` vazia |
| Ou Kit 3 Panos Microfibra Multiuso Mesclado Linha Flow | `B0G634V2NB` | MD-OU-PMW100MESC | FBA Available 0 | Em breve, `amazonUrl` vazia |
| Ou Borrifador Multiuso 500ml Bege Linha Flow | `B0CZTTSHB7` | MD-OU-BFM600BGF | FBA Available 1 | `comingSoon=false`, `amazonUrl` com `?m=A20TN3HCSY6KZV` |
| Ou Escova de limpeza multiuso Bege Linha Flow | `B0CZTTRFQR` | MD-OU-96039000 | FBA Available 0 | Em breve, `amazonUrl` vazia |
| Ou Organizador de Parede e Armário Branco Linha Flow | `B0GKQ4VVPQ` | BU-E3AW-GGZS | FBA Available 0 | Em breve, `amazonUrl` vazia |
| Ou Organizador de Parede para Sacolas Bege Linha Flow | `B0GKPQ99R8` | OSW100BGF | FBA Available 0 | Em breve, `amazonUrl` vazia. É o organizador de sacolas, não um trilho de ganchos |

Não listar: escova de piso, dispenser 2,3L, cesto 16L, organizador 8L genérico,
kit borrifador + panos.

Merchant URL dos itens buyable (`Available > 0`):
`https://www.amazon.com.br/dp/{ASIN}?m=A20TN3HCSY6KZV`
(`B0GKPPS5YH`, `B0CZTTSHB7`). Os três SKUs sem ASIN continuam Em breve.

## Quando o estoque NorthLog / Amazon voltar

Para um item que hoje aparece como **Em breve** (inclui Active com Available 0):

1. Confirme **FBA Available > 0** no Seller Central da loja Mater Domus
   (`A20TN3HCSY6KZV`). Active sozinho **não** libera CTA. **Não invente ASIN
   nem estoque.**
2. Preencha `amazonUrl` no formato canônico:
   `https://www.amazon.com.br/dp/{ASIN10}?m=A20TN3HCSY6KZV`
3. Altere **`comingSoon` para `false`**.
4. Ajuste `price` só se a curadoria confirmar o preço vigente na Amazon.
5. Mantenha `imageUrl` apontando para a foto oficial em `wwwroot/images/products/`.
   As fotos vêm do catálogo oficial Ou (`ou.com.br` / materiais autorizados).
6. Atualize `catalog-meta.json` (`curatedAt`, `productCount`) e faça o redeploy
   dos arquivos estáticos (merge em `main`).

O botão **Comprar na Amazon** reaparece automaticamente quando
`comingSoon === false` **e** `amazonUrl` é canônica. Se `comingSoon`
continuar `true`, o selo "Em breve" permanece e o CTA de compra fica oculto
mesmo com URL preenchida (útil para pré-cadastrar o ASIN sem abrir listing vazio).

## Identificadores

Cada anúncio Amazon publica `asin` em maiúsculas. A fonte é a `amazonUrl`
(`/dp/{ASIN}`) ou, nos itens Em breve que já têm ASIN e URL vazia, o sufixo
de 10 caracteres do `id`.

Exceção aprovada pelo dono: itens Em breve que ainda não têm anúncio na
Amazon não recebem `asin` nem `amazonUrl`. O `id` é um slug terminado no
SKU Martiplast (9 caracteres, para não ser lido como ASIN). A origem é a
NF-e 107157 da Martiplast (13/10/2026), emitida para MATER DOMUS LTDA.
O EAN dessa nota entra no campo `ean` só desses SKUs. Não inventar GTIN
para os demais.

| Nome no site | SKU | EAN | Preço de referência Ou |
|---|---|---|---|
| Ou Borrifador Multiuso 350ml Bege Linha Flow | BFM500BGF | 7899620682399 | R$ 24,99 |
| Ou Borrifador Multiuso 750ml Bege Linha Flow | BFM700BGF | 7899620682405 | R$ 34,99 |
| Ou Organizador de Parede para Sacolas Chumbo Linha Flow | OSW100CHF | 7899620684980 | R$ 69,99 |

O modelo exige `price` > 0, então esses três mostram o preço de tabela da Ou
em ou.com.br. Não é preço de venda na Amazon.

## Correção do organizador bege (`B0GKPQ99R8`)

A NF-e e a página da Ou identificam o SKU OSW100BGF (EAN 7899620684973)
como organizador de parede para sacolas, bege. O texto antigo, de trilho
com quatro ganchos, estava errado. O preço publicado (R$ 56,69) não muda.
As fotos passaram a ser as da Ou para esse produto.

O organizador branco `B0GKQ4VVPQ` não é o mesmo item. Na Ou ele corresponde
ao Organizador de Parede e Armário (SKU OFW100BCF): parede ou interior de
armário, adesivo Ultra Click, até dois dispensers Flow. A descrição atual
permanece.

## Regras do serviço

- `id`, `name` e `imageUrl` obrigatórios; `price` > 0.
- `amazonUrl` vazia é válida (item visível, sem CTA).
- `amazonUrl` preenchida fora do padrão canônico: o registro é descartado.
- IDs duplicados: permanece o primeiro.

## Preços de itens "Em breve"

Os preços dos itens em reposição são **preços de referência da Ou** (fabricante),
não preços vivos da Amazon. Os dois listings buyable em 2026-10-20 (~16:00 BRT)
mantêm o preço já publicado em `products.json` (não inventar preço na liberação):
`B0GKPPS5YH` R$ 39,90 e `B0CZTTSHB7` R$ 29,74.
O dispenser 1,5L (`B0GKPZMCYZ`, R$ 49,49), a escova (`B0CZTTRFQR`, R$ 20,69),
o organizador branco (`B0GKQ4VVPQ`, R$ 58,49) e o organizador bege
(`B0GKPQ99R8`, R$ 56,69) estão Em breve com FBA Available 0; o preço publicado
não muda.
