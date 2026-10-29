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
| `casca` | casca das árvores, troncos e raízes | [bark_willow](https://polyhaven.com/a/bark_willow) | Dario Barresi, Dimitrios Savva |
| `pedraVelha` | pedra velha: ruínas, pilares, menires, muretas, santuários (em cinza, a cor vem do material) | [rough_block_wall](https://polyhaven.com/a/rough_block_wall) | Rob Tuytel |
| `telha` | telhados de ardósia da catedral | [grey_roof_tiles_02](https://polyhaven.com/a/grey_roof_tiles_02) | Rob Tuytel |
| `lapide` | lápides e tampas de túmulos | [rock_pitted_mossy](https://polyhaven.com/a/rock_pitted_mossy) | Dimitrios Savva, Rico Cilliers |
| `concreto` | Asilo: concreto (tanques, base das caldeiras) | [concrete_floor_worn_001](https://polyhaven.com/a/concrete_floor_worn_001) | Dimitrios Savva, Rico Cilliers |
| `ferrugem` | Asilo: ferro enferrujado (alavancas, caldeiras) | [rusty_metal_02](https://polyhaven.com/a/rusty_metal_02) | Rob Tuytel |
| `telhadoAsilo` | Asilo: telhados de ardósia | [roof_slates_02](https://polyhaven.com/a/roof_slates_02) | Rob Tuytel |
| `planalto` | chão do promontório da catedral (terra e capim ralo) | [grass_ground](https://polyhaven.com/a/grass_ground) | Charlotte Baglioni |
| `tabuas` | tábuas gastas: pilhas de madeira, telhado do ermitão | [weathered_planks](https://polyhaven.com/a/weathered_planks) | Dario Barresi, Dimitrios Savva |
| `portaAsilo` | Asilo: portas de tábuas pintadas e gastas (com a cor da foto) | [distressed_painted_planks](https://polyhaven.com/a/distressed_painted_planks) | Rob Tuytel |

## Modelos

| No jogo | Onde aparece | Origem | Autor |
| --- | --- | --- | --- |
| `altar` | altar dos rituais | Higgsfield/Tripo (gerado) | — |
| `estatua` | estátuas do monge | Higgsfield/Tripo (gerado) | — |
| `rocha` | rochas da mata e do promontório (kind rocha) | [rock_09](https://polyhaven.com/a/rock_09), CC0 | Jenelle van Heerden |
| `pedra` | pedra menor ao lado das rochas | [rock_07](https://polyhaven.com/a/rock_07), CC0 | Jenelle van Heerden |
| `tronco` | troncos caídos (kind tronco) | [dead_tree_trunk](https://polyhaven.com/a/dead_tree_trunk), CC0 | Rob Tuytel |
| `caixote` | caixotes do acampamento (kind caixote) | [wooden_crate_02](https://polyhaven.com/a/wooden_crate_02), CC0 | James Ray Cock, Jurita Burger |
| `leito` | Asilo: camas de ferro da enfermaria e dos quartos (kind leito) | [old_bed_frame](https://polyhaven.com/a/old_bed_frame), CC0 | Luca B |
| `banco_asilo` | Asilo: bancos (kind banco) | [painted_wooden_bench](https://polyhaven.com/a/painted_wooden_bench), CC0 | Kirill Sannikov |
| `mesa_asilo` | Asilo: mesas (kind mesa) | [wooden_table_02](https://polyhaven.com/a/wooden_table_02), CC0 | Serhii Khromov |
| `fogueira` | fogueiras do acampamento (o anel de pedras) | [stone_fire_pit](https://polyhaven.com/a/stone_fire_pit), CC0 | Sebastian Platen |
| `lanterna` | lanternas penduradas nos postes | [wooden_lantern_01](https://polyhaven.com/a/wooden_lantern_01), CC0 | James Ray Cock |
| `escombro` | escombros: pilhas de pedras com musgo | [rock_moss_set_02](https://polyhaven.com/a/rock_moss_set_02), CC0 | Kless Gyzen |
| `raiz` | raízes expostas na mata (kind raiz) | [root_cluster_01](https://polyhaven.com/a/root_cluster_01), CC0 | Jenelle van Heerden, Rico Cilliers |
| `canteiro` | Asilo: canteiros de madeira da horta (kind canteiro) | [planter_box_01](https://polyhaven.com/a/planter_box_01), CC0 | James Ray Cock |
| `luminaria` | Asilo: luminárias penduradas no teto | [hanging_industrial_lamp](https://polyhaven.com/a/hanging_industrial_lamp), CC0 | Kuutti Siitonen |
| `quadro` | Asilo: quadros de força das alas | [power_box_01](https://polyhaven.com/a/power_box_01), CC0 | Rico Cilliers, Yann Kervran |
| `samambaia` | samambaias do chão da mata (811 por noite) | [fern_02](https://polyhaven.com/a/fern_02), CC0 | Rob Tuytel, Rico Cilliers |
| `arca` | arca da catedral (kind arca) | [treasure_chest](https://polyhaven.com/a/treasure_chest), CC0 | Rico Cilliers |
| `rocha2` | rochas da mata e do promontório, segunda forma (kind rocha) | [boulder_01](https://polyhaven.com/a/boulder_01), CC0 | Rico Cilliers |
| `rocha3` | rochas da mata e do promontório, terceira forma, com musgo (kind rocha) | [rock_face_02](https://polyhaven.com/a/rock_face_02), CC0 | Dario Barresi, Rico Cilliers |
| `lampiao` | Asilo: lampiões em cima das mesas e da bancada | [vintage_oil_lamp](https://polyhaven.com/a/vintage_oil_lamp), CC0 | Monsta3D |
| `pintura1` | Asilo: pinturas nas paredes das salas | [fancy_picture_frame_01](https://polyhaven.com/a/fancy_picture_frame_01), CC0 | Rob Tuytel, Rico Cilliers |
| `pintura2` | Asilo: pinturas nas paredes das salas | [fancy_picture_frame_02](https://polyhaven.com/a/fancy_picture_frame_02), CC0 | Rob Tuytel, Rico Cilliers |
| `espelho` | Asilo: espelhos nas paredes das salas | [ornate_mirror_01](https://polyhaven.com/a/ornate_mirror_01), CC0 | James Ray Cock |
| `cadeira` | Asilo: a cadeira elétrica (kind cadeira_eletrica) | [WoodenChair_01](https://polyhaven.com/a/WoodenChair_01), CC0 | Jake Mobley |
| `armario` | Asilo: o balcão da recepção, uma fileira de armários (kind balcao) | [painted_wooden_cabinet](https://polyhaven.com/a/painted_wooden_cabinet), CC0 | Kirill Sannikov |

## Bonecos

Personagens em peças rígidas por junta (`tools/montar-bonecos.py`, `assets/bonecos/fontes.json`).

| No jogo | Classe | Pacote | Autor |
| --- | --- | --- | --- |
| `ritualista` | Cultista Ritualista: a roupa de ranger com capuz, em vermelho-sangue; o rosto fica escuro dentro do capuz | [modular-character-outfits-fantasy](https://quaternius.itch.io/modular-character-outfits-fantasy), CC0 | Quaternius |
| `guardiao` | Cultista Guardião: a mesma roupa, num vermelho mais escuro (o jogo põe a armadura de osso por cima) | [modular-character-outfits-fantasy](https://quaternius.itch.io/modular-character-outfits-fantasy), CC0 | Quaternius |
| `soldado` | Caçador Soldado: a roupa de ranger sem o capuz, em azul-aço, com a cabeça e o cabelo do corpo base (o chapéu é do jogo) | [universal-base-characters](https://quaternius.itch.io/universal-base-characters), CC0, [modular-character-outfits-fantasy](https://quaternius.itch.io/modular-character-outfits-fantasy), CC0 | Quaternius |
| `exorcista` | Caçador Exorcista: a roupa sem o capuz, em cinza-carvão | [universal-base-characters](https://quaternius.itch.io/universal-base-characters), CC0, [modular-character-outfits-fantasy](https://quaternius.itch.io/modular-character-outfits-fantasy), CC0 | Quaternius |
| `rastreador` | Caçadora Rastreadora: a roupa feminina de ranger sem o capuz, em couro marrom | [universal-base-characters](https://quaternius.itch.io/universal-base-characters), CC0, [modular-character-outfits-fantasy](https://quaternius.itch.io/modular-character-outfits-fantasy), CC0 | Quaternius |

## Armas

A forma do modelo no quadro da arma do jogo, pintada com os materiais por papel (`tools/montar-armas.py`, `assets/armas/fontes.json`).

| No jogo | Onde aparece | Origem | Autor |
| --- | --- | --- | --- |
| `carabina` | a carabina de alavanca do Soldado (primeira e terceira pessoa) | [hunting-rifle](https://opengameart.org/content/hunting-rifle), CC0 | Lucian Pavel |
| `revolver` | o revólver do Exorcista (primeira e terceira pessoa) | [old-revolver](https://opengameart.org/content/old-revolver), CC0 | Lucian Pavel |

Nos dois clientes, a foto entra no lugar da textura gerada com a mesma cor média da antiga (o campo `media` do
`fontes.json`), porque os materiais foram calibrados para esse brilho. O relevo das fotos vai pela metade, para a luz em
faixas do estilo ilustrado não exagerar.
