# Arte do jogo: de onde vem cada textura e modelo

Tudo o que está aqui pode ser usado em jogo comercial. As texturas e os modelos escaneados são CC0 (domínio público) do
[Poly Haven](https://polyhaven.com): não exigem crédito, mas o jogo dá crédito aos autores mesmo assim. O altar e a estátua do
monge foram gerados no Higgsfield (Tripo, texto para 3D), com uso comercial liberado pelo dono do projeto.

Como remontar:

- Texturas: `python3 tools/montar-texturas.py`, a partir de `assets/texturas/fontes.json`. Grava `client/texturas.js` e
  `unity/Assets/RitualReversal/Resources/Texturas/*.bytes`. Os originais de 1K ficam em `assets/texturas/fontes/`, fora do git; o script baixa de novo se faltarem.
- Modelos: `python3 tools/montar-modelos.py`, a partir de `assets/modelos/fontes.json`. Grava `client/modelos.js` e
  `unity/Assets/RitualReversal/Resources/Modelos/*.bytes`. Precisa do `pyfqmr` (`pip install pyfqmr`) para reduzir a malha.

Sem o arquivo de uma textura ou de um modelo, o jogo volta a gerar aquela peça por código, como antes da v6.4.

## Texturas (Poly Haven, CC0)

| No jogo | Onde aparece | Textura | Autor |
| --- | --- | --- | --- |
| `floor` | chão de pedra da catedral (nave, coro, claustro) | [rock_tile_floor](https://polyhaven.com/a/rock_tile_floor) | Charlotte Baglioni |
| `wall` | paredes de pedra (catedral, mosteiro, ruínas) | [castle_wall_slates](https://polyhaven.com/a/castle_wall_slates) | Rob Tuytel |
| `wood` | madeira (bancos, vigas, caixotes, portas) | [rough_wood](https://polyhaven.com/a/rough_wood) | Rob Tuytel |
| `marble` | mármore (altares, colunas) | [marble_01](https://polyhaven.com/a/marble_01) | Rob Tuytel |
| `metal` | metal enferrujado (grades, ferragens) | [rusty_metal_04](https://polyhaven.com/a/rusty_metal_04) | Amal Kumar |
| `cloth` | pano (tendas, estandartes; em cinza, a cor vem do material) | [rough_linen](https://polyhaven.com/a/rough_linen) | colormass, Rico Cilliers |
| `leather` | couro (em cinza, a cor vem do material) | [brown_leather](https://polyhaven.com/a/brown_leather) | Rob Tuytel |
| `rocha` | rocha do desfiladeiro e do promontório | [rock_face_03](https://polyhaven.com/a/rock_face_03) | Dario Barresi, Rico Cilliers |
| `chao` | chão da mata | [forest_leaves_02](https://polyhaven.com/a/forest_leaves_02) | Rob Tuytel |
| `reboco` | Asilo: reboco das paredes | [worn_plaster_wall](https://polyhaven.com/a/worn_plaster_wall) | Dimitrios Savva |
| `lambri` | Asilo: azulejo do lambri | [grey_tiles](https://polyhaven.com/a/grey_tiles) | Amal Kumar |
| `ladrilho` | Asilo: ladrilho xadrez do piso | [floor_tiles_06](https://polyhaven.com/a/floor_tiles_06) | Rob Tuytel |
| `tabua` | Asilo: assoalho de tábuas | [old_wood_floor](https://polyhaven.com/a/old_wood_floor) | Guillaume Monsergent |
| `teto` | Asilo: teto | [ceiling_interior](https://polyhaven.com/a/ceiling_interior) | Dimitrios Savva |
| `grama` | Asilo: gramado | [grass_path_2](https://polyhaven.com/a/grass_path_2) | Rob Tuytel |
| `terra` | Asilo: terra da horta | [brown_mud_02](https://polyhaven.com/a/brown_mud_02) | Rob Tuytel |
| `cascalho` | Asilo: cascalho das trilhas | [gravel_floor](https://polyhaven.com/a/gravel_floor) | Jenelle van Heerden, Matterfield |

## Modelos

| No jogo | Onde aparece | Origem | Autor |
| --- | --- | --- | --- |
| `altar` | altar dos rituais | Higgsfield/Tripo (gerado) | — |
| `estatua` | estátuas do monge | Higgsfield/Tripo (gerado) | — |
| `rocha` | rochas da mata e do promontório (kind rocha) | [rock_09](https://polyhaven.com/a/rock_09), CC0 | Jenelle van Heerden |
| `pedra` | pedra menor ao lado das rochas | [rock_07](https://polyhaven.com/a/rock_07), CC0 | Jenelle van Heerden |
| `tronco` | troncos caídos e raízes (kind tronco/raiz) | [dead_tree_trunk](https://polyhaven.com/a/dead_tree_trunk), CC0 | Rob Tuytel |
| `caixote` | caixotes do acampamento (kind caixote) | [wooden_crate_02](https://polyhaven.com/a/wooden_crate_02), CC0 | James Ray Cock, Jurita Burger |
| `leito` | Asilo: camas de ferro da enfermaria e dos quartos (kind leito) | [old_bed_frame](https://polyhaven.com/a/old_bed_frame), CC0 | Luca B |
| `banco_asilo` | Asilo: bancos (kind banco) | [painted_wooden_bench](https://polyhaven.com/a/painted_wooden_bench), CC0 | Kirill Sannikov |
| `mesa_asilo` | Asilo: mesas (kind mesa) | [wooden_table_02](https://polyhaven.com/a/wooden_table_02), CC0 | Serhii Khromov |

Nos dois clientes, a foto entra no lugar da textura gerada com a mesma cor média da antiga (o campo `media` do
`fontes.json`), porque os materiais foram calibrados para esse brilho. O relevo das fotos vai pela metade, para a luz em
faixas do estilo ilustrado não exagerar.
