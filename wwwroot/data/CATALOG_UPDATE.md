# Atualização do catálogo (Linha Flow)

O catálogo institucional é o arquivo estático `wwwroot/data/products.json`.
A vitrine lista **somente** os SKUs da loja Mater Domus no Seller Central
(`A20TN3HCSY6KZV`). Não invente produto, ASIN ou combinação.

## Catálogo Seller (1:1)

| Amazon title | ASIN | SKU Seller | Status |
|---|---|---|---|
| Ou Dispenser Quadrado 1L Branco Linha Flow | `B0GKPPS5YH` | MD-OU-0001 | ACTIVE — CTA Amazon |
| Ou Dispenser Quadrado 1,5L Branco Linha Flow | `B0GKPZMCYZ` | MD-OU-B0GKPZMCYZ | OOS — Em breve |
| Ou Rodo Bege Linha Flow | `B0F8PWY3M5` | 6L-N5BY-LSRC | OOS — Em breve |
| Ou Rodo Multiuso Bege Linha Flow | `B0CZTTVLWK` | MD-OU-B0CZTTVLWK | OOS — Em breve |
| Ou Pano para Chão de Microfibra Chumbo Linha Flow | `B0FXBN7SCB` | 2L-J74H-R1IC | OOS — Em breve |
| Ou Kit 3 Panos Microfibra Multiuso Mesclado Linha Flow | `B0G634V2NB` | MD-OU-PMW100MESC | OOS — Em breve |
| Ou Borrifador Multiuso 500ml Bege Linha Flow | `B0CZTTSHB7` | MD-OU-BFM600BGF | OOS — Em breve |
| Ou Escova de limpeza multiuso Bege Linha Flow | `B0CZTTRFQR` | MD-OU-96039000 | OOS — Em breve |
| Ou Organizador de Parede e Armário Branco Linha Flow | `B0GKQ4VVPQ` | BU-E3AW-GGZS | OOS — Em breve |
| Ou Organizador de Parede Multiuso Bege Linha Flow | `B0GKPQ99R8` | MD-OU-39269090 | OOS — Em breve |

Não listar: escova de piso, dispenser 2,3L, cesto 16L, organizador 8L genérico,
kit borrifador + panos.

## Quando o estoque NorthLog / Amazon voltar

Para um item que hoje aparece como **Em breve**:

1. Confirme o ASIN real da loja Mater Domus (`A20TN3HCSY6KZV`). **Não invente ASIN.**
2. Preencha `amazonUrl` no formato canônico:
   `https://www.amazon.com.br/dp/{ASIN10}?m=A20TN3HCSY6KZV`
3. Altere **`comingSoon` para `false`**.
4. Ajuste `price` só se a curadoria confirmar o preço vigente na Amazon.
5. Mantenha `imageUrl` apontando para a foto oficial em `wwwroot/images/products/`.
   As fotos vêm do catálogo oficial Ou (`ou.com.br` / materiais autorizados).
6. Faça o redeploy dos arquivos estáticos.

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
não preços vivos da Amazon. O único listing ACTIVE no catálogo é
`B0GKPPS5YH` (Ou Dispenser Quadrado 1L Branco Linha Flow) — preço Amazon R$ 39,90.
