# Atualização do catálogo (Linha Flow)

O catálogo institucional é o arquivo estático `wwwroot/data/products.json`.
Não há integração com Seller Central: a disponibilidade na vitrine é editorial.

## Quando o estoque NorthLog / Amazon voltar

Para um item que hoje aparece como **Em breve**:

1. Confirme o ASIN real da loja Mater Domus (`A20TN3HCSY6KZV`). **Não invente ASIN.**
2. Preencha `amazonUrl` no formato canônico:
   `https://www.amazon.com.br/dp/{ASIN10}?m=A20TN3HCSY6KZV`
3. Altere **`comingSoon` para `false`**.
4. Ajuste `price` só se a curadoria confirmar o preço vigente na Amazon.
5. Mantenha `imageUrl` apontando para a foto oficial em `wwwroot/images/products/{id}.jpg` (ou `.png`).
   As fotos atuais da Linha Flow vêm do catálogo oficial Ou (`ou.com.br` / materiais autorizados).
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
não preços vivos da Amazon. O único ASIN confirmado no repositório é
`B0GKPPS5YH` (Dispenser Quadrado Flow 1L — Tampa Branca).
