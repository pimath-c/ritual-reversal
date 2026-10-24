# Áudio do Ritual Reversal

Gerado por `python3 tools/montar-sons.py` (não edite à mão as tabelas). As gravações originais ficam em
`assets/sons/fontes/` e a origem de cada uma em `assets/sons/fontes.json`; o script monta os sons do jogo
(cortes, filtros, camadas, normalização) em `assets/sons/jogo/*.mp3` e em `client/sons.js`.

## Regras de licença

- Só entram gravações em **CC0 1.0**, **CC BY 3.0** ou **domínio público**. Nada de CC BY-SA, GPL, uso não comercial ou licença não declarada.
- CC BY 3.0 exige atribuição: os créditos abaixo também aparecem no jogo (Ajustes, Créditos de áudio).
- Os sites de áudio (Freesound, OpenGameArt, Pixabay, Kenney, Sonniss) estavam bloqueados pela rede do ambiente de
  desenvolvimento. As gravações vieram de repositórios públicos no GitHub que as redistribuem com a licença de cada arquivo
  documentada (Sonic Pi, Minetest Game, VoxeLibre; na v6, conquest); o link original do Freesound/OpenGameArt de cada uma está na tabela.
- v6: os arquivos `cq_*.mp3` são cortes (trecho e ganho) que o repositório hoffspot/conquest fez de gravações CC0 do Freesound e de
  uma gravação em domínio público do National Park Service (EUA). A gravação de cada um é CC0 ou domínio público; o corte vem com o
  repositório, sob Apache 2.0 (<https://www.apache.org/licenses/LICENSE-2.0>), cuja licença e aviso valem para esses arquivos.
- O tiro não é a gravação de uma arma: é montado com gravações reais em CC0 (explosão, impacto, estalo e mecanismo de metal).
  Quando os sites de áudio estiverem acessíveis, troque por gravações de armas reais e rode o script de novo.

## Como foi conferido

- Ninguém ouviu estes sons durante o desenvolvimento: o ambiente não tem saída de áudio. Cada arquivo foi conferido por
  medida (duração, pico, ataque, centroide e bandas do espectro) e por espectrograma, e todos decodificam no Chromium
  (o teste `tools/navegador.js` exige as 67 variações carregadas sem falha). **Falta ouvir em fones e ajustar volumes.**

## Sons do jogo

| Som | Uso no jogo | Gravações | Licenças |
| --- | --- | --- | --- |
| `tiro_carabina` | tiro da carabina de alavanca (Soldado, Rastreador) | perc_impact2.flac, perc_snap.flac, tnt_explode.ogg | CC0 1.0 |
| `tiro_revolver` | tiro do revólver (Exorcista) | perc_impact1.flac, perc_snap.flac, tnt_explode.ogg | CC0 1.0 |
| `alavanca` | ciclo da alavanca depois do tiro | default_dug_metal.1.ogg, default_place_node_metal.2.ogg | CC0 1.0 |
| `cao` | cão do revólver e tambor | default_dig_metal.ogg, perc_snap.flac | CC BY 3.0, CC0 1.0 |
| `recarga` | recarga (abrir, cartuchos, fechar) | default_dug_metal.2.ogg, mcl_armor_equip_chainmail.ogg, mcl_armor_equip_iron.ogg | CC0 1.0 |
| `sigilo` | sigilo lançado pelo cetro | ambi_dark_woosh.flac, mcl_throwing_throw.ogg | CC0 1.0 |
| `estouro` | sigilo estourando | mcl_mobs_mob_poof.ogg, perc_impact2.flac | CC0 1.0 |
| `impacto_pedra` | bala na pedra | default_dig_cracky.1.ogg, default_dig_cracky.2.ogg, default_dig_cracky.3.ogg, perc_snap.flac | CC BY 3.0, CC0 1.0 |
| `impacto_madeira` | bala na madeira e nos troncos | default_dig_choppy.1.ogg, default_dig_choppy.2.ogg, default_dig_choppy.3.ogg | CC0 1.0 |
| `impacto_terra` | bala no chão de terra | default_gravel_dig.1.ogg, default_gravel_dig.2.ogg | CC0 1.0 |
| `impacto_corpo` | acerto confirmado (golpe em alguém) | drum_heavy_kick.flac, mcl_throwing_egg_impact.ogg, player_damage.ogg | CC BY 3.0, CC0 1.0 |
| `derrubou` | queda confirmada (para quem derrubou) | drum_tom_lo_hard.flac, perc_impact1.flac | CC0 1.0 |
| `dor` | você levou dano | drum_heavy_kick.flac, player_damage.ogg | CC BY 3.0, CC0 1.0 |
| `empurrao` | Empurrão | ambi_swoosh.flac, drum_heavy_kick.flac, perc_swash.flac | CC0 1.0 |
| `passo_pedra` | passos na catedral e no Claustro | default_hard_footstep.1.ogg, default_hard_footstep.2.ogg, default_hard_footstep.3.ogg | CC BY 3.0 |
| `passo_terra` | passos nas trilhas | default_sand_footstep.1.ogg, default_sand_footstep.2.ogg, default_sand_footstep.3.ogg | CC0 1.0 |
| `passo_folhas` | passos na mata fechada | default_snow_footstep.1.ogg, default_snow_footstep.2.ogg, default_snow_footstep.3.ogg, default_snow_footstep.4.ogg, default_snow_footstep.5.ogg | CC0 1.0 |
| `passo_lama` | passos na chuva (camada por cima) | mud_footsteps.ogg | CC0 1.0 |
| `corvo` | corvos | misc_crow.flac | CC0 1.0 |
| `sino` | sino da catedral | mcl_bells_bell_stroke.ogg | CC0 1.0 |
| `sino_grande` | sino do Grande Ritual | mcl_bells_bell_stroke.ogg | CC0 1.0 |
| `gota` | gotas de água na nave | drippingwater_drip.1.ogg, drippingwater_drip.2.ogg, drippingwater_drip.3.ogg | CC0 1.0 |
| `flare` | flare aceso | tnt_ignite.ogg | CC0 1.0 |
| `coleta` | reagente coletado | mcl_potions_bottle_fill.ogg | CC0 1.0 |
| `assombro` | alucinação com sanidade baixa | cave1.ogg | CC BY 3.0 |
| `ritual_inicio` | ritual começando (ou aparecendo para a caça) | mcl_portals_open_end_portal.ogg, misc_cineboom.flac | CC0 1.0 |
| `ritual_marco` | ritual passando de 25%, 50% e 75% | cave4.ogg | CC0 1.0 |
| `fenda` | Fenda (ritual completo) | ambi_choir.flac, misc_cineboom.flac, tnt_explode.ogg | CC0 1.0 |
| `farol` | Farol (ritual selado) | ambi_choir.flac, perc_bell.flac | CC0 1.0 |
| `selo_quebra` | selo interrompido | default_break_glass.1.ogg, default_break_glass.2.ogg, default_break_glass.3.ogg, perc_impact2.flac | CC BY 3.0, CC0 1.0 |
| `canal_quebra` | canalização interrompida | ambi_choir.flac, mcl_mobs_mob_poof.ogg | CC0 1.0 |
| `selando` | a caça começou a selar | ambi_glass_rub.flac | CC0 1.0 |
| `coro_laco` | coro do ritual em andamento (laço) | ambi_choir.flac | CC0 1.0 |
| `zumbido_laco` | sussurros do ritual em andamento (laço) | ambi_haunted_hum.flac | CC0 1.0 |
| `evp` | EVP: vozes num altar desperto | ambi_glass_hum.flac | CC0 1.0 |
| `descoberta` | descoberta da investigação e lances da noite | perc_bell2.flac | CC0 1.0 |
| `chamariz_desfeito` | Lanterna desmascara um Chamariz | ambi_glass_hum.flac, mcl_mobs_mob_poof.ogg | CC0 1.0 |
| `transferencia` | Transferência | mcl_portals_teleport.ogg, perc_door.flac | CC BY 3.0, CC0 1.0 |
| `cartucho` | cartucho entrando pela portinhola da carabina | default_dig_metal.ogg, mcl_armor_equip_chainmail.ogg, perc_snap.flac | CC BY 3.0, CC0 1.0 |
| `cartucho_fim` | último cartucho assentado no tubo | default_dug_metal.1.ogg, perc_snap.flac | CC0 1.0 |
| `tambor_abre` | tambor do revólver abrindo | default_place_node_metal.2.ogg, perc_snap.flac | CC0 1.0 |
| `tambor_fecha` | tambor do revólver fechando | default_dug_metal.2.ogg, mcl_armor_equip_chainmail.ogg, perc_snap.flac | CC0 1.0 |
| `extrator` | extrator do revólver | default_dig_metal.ogg, mcl_armor_equip_chainmail.ogg | CC BY 3.0, CC0 1.0 |
| `capsulas` | cápsulas caindo no chão | default_dig_metal.ogg, perc_bell2.flac | CC BY 3.0, CC0 1.0 |
| `municiador` | municiador do revólver entrando e girando | default_dig_metal.ogg, mcl_armor_equip_chainmail.ogg, perc_snap.flac | CC BY 3.0, CC0 1.0 |
| `roupa` | braço e roupa se mexendo | perc_swash.flac | CC0 1.0 |
| `empunhar` | mão firmando a pegada | mcl_armor_equip_chainmail.ogg, perc_impact1.flac | CC0 1.0 |
| `cetro_sacar` | cetro acordando ao sacar | ambi_dark_woosh.flac, ambi_glass_hum.flac | CC0 1.0 |
| `runa_volta` | runa voltando a orbitar o cetro | ambi_glass_rub.flac, perc_bell2.flac | CC0 1.0 |
| `bencao` | Exorcista toca a cruz do revólver | ambi_glass_hum.flac, perc_bell2.flac | CC0 1.0 |
| `queda_corpo` | alguém derrubado ou executado cai no chão | cq_death-thud-mid-1.mp3, cq_death-thud-mid-2.mp3, cq_death-thud-mid-3.mp3 | CC0 1.0 |
| `sussurro` | inspeção do cetro e execução pelo culto | cq_ghost-call-1.mp3, cq_ghost-call-2.mp3, cq_ghost-call-3.mp3 | CC0 1.0 |
| `estalo_fogo` | reagente queimado pela caça | cq_crackle-1.mp3, cq_crackle-2.mp3, cq_crackle-3.mp3, cq_crackle-4.mp3 | CC0 1.0 |
| `folhas` | coleta de reagente na mata | cq_leaves-1.mp3, cq_leaves-2.mp3 | CC0 1.0 |
| `coruja` | mata à noite (avulso) | cq_owl-1.mp3, cq_owl-2.mp3, cq_owl-3.mp3 | CC0 1.0 |
| `rangido` | mata à noite (avulso) | cq_creak-tree-1.mp3, cq_creak-tree-2.mp3, cq_creak-tree-3.mp3 | CC0 1.0 |
| `mata_laco` | fundo da mata (laço) | cq_wind-forest-1.mp3 | CC0 1.0 |
| `noite_laco` | fundo a céu aberto (laço) | cq_night-loop-1.mp3 | CC0 1.0 |
| `cripta_laco` | fundo dentro da catedral (laço) | cq_crypt-loop-1.mp3 | CC0 1.0 |
| `charco_laco` | fundo perto do Charco (laço) | cq_frogs-loop-1.mp3 | Domínio público (NPS, EUA) |
| `fogo_laco` | fornos da Carvoaria (laço) | cq_brazier-loop-1.mp3 | CC0 1.0 |
| `chuva_laco` | chuva, no clima de chuva (laço) | cq_rain-loop-1.mp3 | CC0 1.0 |

## Créditos (gravações usadas)

| Autor | Licença | Origem | Arquivos |
| --- | --- | --- | --- |
| AlaskaRobotics | CC0 1.0 | http://freesound.org/people/AlaskaRobotics/sounds/221515/ | perc_bell.flac |
| Benboncan | CC BY 3.0 | https://freesound.org/people/Benboncan/sounds/71823/ | default_dig_cracky.1.ogg, default_dig_cracky.2.ogg, default_dig_cracky.3.ogg |
| EcoDTR | CC0 1.0 | http://freesound.org/people/EcoDTR/sounds/27281/ | ambi_dark_woosh.flac |
| Erdie | CC BY 3.0 | https://freesound.org/people/Erdie/sounds/41579/ | default_hard_footstep.1.ogg, default_hard_footstep.2.ogg, default_hard_footstep.3.ogg |
| Exsomniel | CC0 1.0 | http://freesound.org/people/Exsomniel/sounds/207809/ | ambi_choir.flac |
| FreqMan e Wuzzy | CC BY 3.0 | https://freesound.org/people/FreqMan/sounds/32541/ | mcl_portals_teleport.ogg |
| Gerent | CC0 1.0 | https://freesound.org/people/Gerent/sounds/558396/ | cq_owl-1.mp3 |
| Gerent | CC0 1.0 | https://freesound.org/people/Gerent/sounds/558397/ | cq_owl-2.mp3 |
| Gerent | CC0 1.0 | https://freesound.org/people/Gerent/sounds/558398/ | cq_owl-3.mp3 |
| Halgrimm | CC0 1.0 | http://freesound.org/people/Halgrimm/sounds/169867/ | ambi_swoosh.flac |
| Iwan Gabovitch (qubodup) | CC0 1.0 | http://opengameart.org/users/qubodup | default_dug_metal.1.ogg, default_dug_metal.2.ogg |
| Johnnie_Holiday | CC0 1.0 | https://freesound.org/people/Johnnie_Holiday/sounds/546571/ | mcl_portals_open_end_portal.ogg |
| NPS/Neal Herbert | Domínio público (NPS, EUA) | https://www.nps.gov/yell/learn/photosmultimedia/sounds-chorusfrogs.htm | cq_frogs-loop-1.mp3 |
| Northern_Monkey | CC0 1.0 | http://freesound.org/people/Northern_Monkey/sounds/177242/ (link marcado como desativado pelo Sonic Pi) | misc_cineboom.flac |
| Nox_Sound | CC0 1.0 | https://freesound.org/people/Nox_Sound/sounds/530908/ | cq_wind-forest-1.mp3 |
| Nox_Sound | CC0 1.0 | https://freesound.org/people/Nox_Sound/sounds/558967/ | cq_brazier-loop-1.mp3 |
| Nox_Sound | CC0 1.0 | https://freesound.org/people/Nox_Sound/sounds/637083/ | cq_night-loop-1.mp3 |
| Ogrebane | CC0 1.0 | http://opengameart.org/content/wood-and-metal-sound-effects-volume-2 | default_place_node_metal.2.ogg |
| Paolo D'Emilio | CC BY 3.0 | https://opengameart.org/content/scary-monster | cave1.ogg |
| Planman | CC0 1.0 | https://freesound.org/people/Planman/sounds/208111/ | mcl_mobs_mob_poof.ogg |
| Ryding | CC0 1.0 | http://freesound.org/people/Ryding/sounds/94337/ | default_snow_footstep.1.ogg, default_snow_footstep.2.ogg, default_snow_footstep.3.ogg, default_snow_footstep.4.ogg, default_snow_footstep.5.ogg |
| SKrafft | CC0 1.0 | https://freesound.org/people/SKrafft/sounds/681366/ | cq_crackle-1.mp3, cq_crackle-2.mp3, cq_crackle-3.mp3, cq_crackle-4.mp3 |
| Sheyvan | CC0 1.0 | https://freesound.org/people/Sheyvan/sounds/476113/ | default_dig_choppy.1.ogg, default_dig_choppy.2.ogg, default_dig_choppy.3.ogg |
| SoundCollectah | CC0 1.0 | http://www.freesound.org/people/SoundCollectah/sounds/109400/ | perc_snap.flac |
| TheRandomLegoBrick | CC0 1.0 | mods/ITEMS/mcl_mud/README.txt | mud_footsteps.ogg |
| Tomlija | CC BY 3.0 | http://www.freesound.org/people/Tomlija/sounds/97669/ | default_break_glass.2.ogg |
| Wigglesworth | CC0 1.0 | https://freesound.org/people/Wigglesworth/sounds/853591/ | cq_death-thud-mid-1.mp3, cq_death-thud-mid-2.mp3 |
| Zajo | CC0 1.0 | http://freesound.org/people/Zajo/sounds/4832/ | drum_heavy_kick.flac |
| ani_music | CC0 1.0 | http://freesound.org/people/ani_music/sounds/198403/ | ambi_glass_rub.flac |
| artisticdude | CC0 1.0 | https://opengameart.org/content/rpg-sound-pack | mcl_armor_equip_chainmail.ogg |
| bruno.auzet | CC0 1.0 | https://freesound.org/people/bruno.auzet/sounds/714265/ | cq_leaves-1.mp3, cq_leaves-2.mp3 |
| cmusounddesign | CC BY 3.0 | http://www.freesound.org/people/cmusounddesign/sounds/71947/ | default_break_glass.1.ogg |
| dav0r | CC0 1.0 | https://freesound.org/people/dav0r/sounds/176760/ | mcl_throwing_egg_impact.ogg |
| davidworksonline | CC0 1.0 | http://freesound.org/people/davidworksonline/sounds/54973/ | misc_crow.flac |
| edsward (modificado por sorcerykid) | CC0 1.0 | https://freesound.org/people/edsward/sounds/341866/ | mcl_bells_bell_stroke.ogg |
| felix.blume | CC0 1.0 | https://freesound.org/people/felix.blume/sounds/140047/ | cq_creak-tree-1.mp3, cq_creak-tree-2.mp3, cq_creak-tree-3.mp3 |
| geoneo0 | CC0 1.0 | https://freesound.org/people/geoneo0/sounds/193812/ | cq_ghost-call-1.mp3, cq_ghost-call-2.mp3, cq_ghost-call-3.mp3 |
| hullum | CC0 1.0 | https://freesound.org/people/hullum/sounds/415559/ | perc_bell2.flac |
| hullum | CC0 1.0 | https://freesound.org/people/hullum/sounds/415578/ | perc_impact1.flac |
| hullum | CC0 1.0 | https://freesound.org/people/hullum/sounds/415579/ | perc_door.flac |
| hullum | CC0 1.0 | https://freesound.org/people/hullum/sounds/415581/ | perc_impact2.flac |
| jmbphilmes | CC0 1.0 | https://freesound.org/people/jmbphilmes/sounds/200273/ | cq_rain-loop-1.mp3 |
| jmdh | CC0 1.0 | https://freesound.org/people/jmdh/sounds/181177/ | cq_death-thud-mid-3.mp3 |
| kaligari | CC0 1.0 | http://freesound.org/people/kaligari/sounds/35391/ | ambi_glass_hum.flac |
| kaligari | CC0 1.0 | http://freesound.org/people/kaligari/sounds/35392/ | ambi_haunted_hum.flac |
| kretopi | CC0 1.0 | https://freesound.org/people/kretopi/sounds/406405/ | mcl_throwing_throw.ogg |
| kyles | CC0 1.0 | https://freesound.org/people/kyles/sounds/451170/ | cq_crypt-loop-1.mp3 |
| lolamadeus | CC0 1.0 | https://freesound.org/people/lolamadeus/sounds/179341/ | default_gravel_dig.1.ogg, default_gravel_dig.2.ogg |
| lsprice | CC BY 3.0 | http://www.freesound.org/people/lsprice/sounds/88808/ | default_break_glass.3.ogg |
| mcl_dripping (VoxeLibre) | CC0 1.0 | mods/ENTITIES/mcl_dripping/README.md | drippingwater_drip.1.ogg, drippingwater_drip.2.ogg, drippingwater_drip.3.ogg |
| mcl_potions (VoxeLibre) | CC0 1.0 | mods/ITEMS/mcl_potions/README.txt (Sounds: CC0) | mcl_potions_bottle_fill.ogg |
| menegass | CC0 1.0 | http://www.freesound.org/people/menegass/sounds/100064/ | drum_tom_lo_hard.flac |
| mitchanary | CC0 1.0 | https://freesound.org/people/mitchanary/sounds/506148/ | mcl_armor_equip_iron.ogg |
| qubodup | CC0 1.0 | http://freesound.org/people/qubodup/sounds/60009/ | perc_swash.flac |
| rubberduck (gong_01) | CC0 1.0 | https://opengameart.org/content/100-cc0-sfx | cave4.ogg |
| sonictechtonic | CC BY 3.0 | https://www.freesound.org/people/sonictechtonic/sounds/241872/ | player_damage.ogg |
| steveygos93 (editado por TumeniNodes) | CC0 1.0 | https://freesound.org/s/80401/ | tnt_explode.ogg |
| theneedle.tv (editado por TumeniNodes) | CC0 1.0 | https://freesound.org/s/316682/ | tnt_ignite.ogg |
| worthahep88 | CC0 1.0 | https://freesound.org/people/worthahep88/sounds/319224/ | default_sand_footstep.1.ogg, default_sand_footstep.2.ogg, default_sand_footstep.3.ogg |
| yadronoff | CC BY 3.0 | https://www.freesound.org/people/yadronoff/sounds/320397/ | default_dig_metal.ogg |

Licenças: CC0 1.0 <https://creativecommons.org/publicdomain/zero/1.0/> · CC BY 3.0 <https://creativecommons.org/licenses/by/3.0/>
