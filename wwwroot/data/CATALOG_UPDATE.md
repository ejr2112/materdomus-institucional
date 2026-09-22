# Atualização do catálogo (Linha Flow)

O catálogo institucional publicado é o arquivo estático `wwwroot/data/products.json`.
Não há caminho provisório, draft ou unpublished: `/produtos` lê esse JSON.
A vitrine lista **somente** os SKUs da loja Mater Domus no Seller Central
(`A20TN3HCSY6KZV`). Não invente produto, ASIN, estoque ou combinação.

Última curadoria Seller Central: **2026-09-22** (~10:46 BRT).
Metadados: `wwwroot/data/catalog-meta.json`.
No `products.json`, todos os itens com `comingSoon: false` vêm primeiro; Em breve
fica depois. Dentro de cada grupo a ordem relativa anterior é mantida.

## Regra de liberação (FBA Available > 0)

Liberar **Comprar na Amazon** somente quando o listing está buyable
(FBA Available > 0). Listing **Active com Available = 0** continua **Em breve**
— não é estoque. Não invente quantidade.

| Amazon title | ASIN | SKU Seller | Seller 2026-09-22 | Site |
|---|---|---|---|---|
| Ou Dispenser Quadrado 1L Branco Linha Flow | `B0GKPPS5YH` | MD-OU-0001 | FBA Available 7 | `comingSoon=false`, `amazonUrl` com `?m=A20TN3HCSY6KZV` |
| Ou Dispenser Quadrado 1,5L Branco Linha Flow | `B0GKPZMCYZ` | MD-OU-B0GKPZMCYZ | FBA Available 6 | `comingSoon=false`, `amazonUrl` com `?m=A20TN3HCSY6KZV` |
| Ou Rodo Bege Linha Flow | `B0F8PWY3M5` | 6L-N5BY-LSRC | Available 0 | Em breve, `amazonUrl` vazia |
| Ou Rodo Multiuso Bege Linha Flow | `B0CZTTVLWK` | MD-OU-B0CZTTVLWK | FBA Available 17 | `comingSoon=false`, `amazonUrl` com `?m=A20TN3HCSY6KZV` |
| Ou Pano para Chão de Microfibra Chumbo Linha Flow | `B0FXBN7SCB` | 2L-J74H-R1IC | Available 0 | Em breve, `amazonUrl` vazia |
| Ou Kit 3 Panos Microfibra Multiuso Mesclado Linha Flow | `B0G634V2NB` | MD-OU-PMW100MESC | Available 0 | Em breve, `amazonUrl` vazia |
| Ou Borrifador Multiuso 500ml Bege Linha Flow | `B0CZTTSHB7` | MD-OU-BFM600BGF | Available 0 | Em breve, `amazonUrl` vazia |
| Ou Escova de limpeza multiuso Bege Linha Flow | `B0CZTTRFQR` | MD-OU-96039000 | FBA Available 10 | `comingSoon=false`, `amazonUrl` com `?m=A20TN3HCSY6KZV` |
| Ou Organizador de Parede e Armário Branco Linha Flow | `B0GKQ4VVPQ` | BU-E3AW-GGZS | FBA Available 6 | `comingSoon=false`, `amazonUrl` com `?m=A20TN3HCSY6KZV` |
| Ou Organizador de Parede Multiuso Bege Linha Flow | `B0GKPQ99R8` | MD-OU-39269090 | FBA Available 3 | `comingSoon=false`, `amazonUrl` com `?m=A20TN3HCSY6KZV` |

Não listar: escova de piso, dispenser 2,3L, cesto 16L, organizador 8L genérico,
kit borrifador + panos.

Merchant URL dos itens buyable (`Available > 0`):
`https://www.amazon.com.br/dp/{ASIN}?m=A20TN3HCSY6KZV`
(`B0GKPPS5YH`, `B0GKPZMCYZ`, `B0CZTTVLWK`, `B0CZTTRFQR`, `B0GKQ4VVPQ`, `B0GKPQ99R8`).

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

## Regras do serviço

- `id`, `name` e `imageUrl` obrigatórios; `price` > 0.
- `amazonUrl` vazia é válida (item visível, sem CTA).
- `amazonUrl` preenchida fora do padrão canônico: o registro é descartado.
- IDs duplicados: permanece o primeiro.

## Preços de itens "Em breve"

Os preços dos itens em reposição são **preços de referência da Ou** (fabricante),
não preços vivos da Amazon. Os seis listings buyable em 2026-09-22 mantêm o
preço já publicado em `products.json` (não inventar preço na liberação):
`B0GKPPS5YH` R$ 39,90, `B0GKPZMCYZ` R$ 49,49, `B0CZTTVLWK` R$ 17,09,
`B0CZTTRFQR` R$ 20,69, `B0GKQ4VVPQ` R$ 58,49 e `B0GKPQ99R8` R$ 56,69.
