# Ritual Reversal — contexto para novas sessões

FPS de terror competitivo 2v2 no navegador (Three.js r128 + servidor Node). Dois Cultistas celebram rituais em
segredo; dois Caçadores investigam e selam. O dono do projeto fala português: responda, comente o código e escreva
commits em português.

## Links
- Protótipo publicado (claude.ai, mesmo link a cada versão): https://claude.ai/artifact/8C4MNiGeURccg8E2ZzoNLY
  Publique `dist/ritual-reversal.html` nesse link depois de `node tools/build.js`.
- Documento de design: https://claude.ai/artifact/9tDbnxCM1xtjJZToVaQYXB
- Branch de trabalho: `claude/youthful-feynman-tott4g` (PR aberto para `main`).

## Onde fica cada coisa
- `shared/sim.js` — simulação compartilhada (navegador e servidor): regras, mapa, navegação A* em grade, bots,
  snapshot. Mapa compacto: coordenadas da floresta escritas na planta original (`HALF0`) e mapeadas por `mataXZ`.
- `client/index.html` — cliente inteiro (render, HUD, áudio, rede, controles de toque, níveis de qualidade).
- `client/mundo.js` — a catedral, as pontes, o desfiladeiro e as peças do mapa da v6 (`RR_MUNDO`); `client/arma.js` — primeira
  pessoa (mãos, clipes, inspeções); `client/skins.js` — skins das armas (`RR_SKINS`, ver `docs/SKINS.md`). Os três são
  embutidos por build.js/build-mobile.js e servidos pelo servidor.
- `docs/DIRECAO.md` — avaliação de direção da v6 (problemas por impacto, decisões, medições).
- `server/server.js` — servidor autoritativo (30 Hz, snapshots a 20 Hz), salas de 4 letras.
- `mobile/` — app Android (Capacitor 8). `.github/workflows/android.yml` compila o APK a cada envio
  (Actions → "APK Android" → Artifacts).
- `notas/` — patch notes. `tools/postar-discord.js` posta no Discord pelo webhook em `DISCORD_WEBHOOK`
  (credencial do ambiente ou segredo do GitHub; nunca no repositório).

## Testes (rode antes de cada commit que mexe no jogo)
    node tools/testar.js 6          # grafo, caminhadas até todos os alvos, partidas de bots com invariantes
    node tools/protocolo.js         # servidor + clientes falsos
    python3 tools/montar-sons.py    # só se mudar uma gravação ou receita (precisa de ffmpeg ou pip install imageio-ffmpeg numpy)
    node tools/build.js             # gera dist/ritual-reversal.html
    THREE_DIR=<three@0.128.0> NODE_PATH=$(npm root -g) node tools/navegador.js   # Chromium, 17 vistas, Arsenal, velas, reagente, execução
    node tools/fluxo.js                     # mapa: distâncias pelo A*, exposição, invariantes do design (grava dist/fluxo)
    THREE_DIR=<three@0.128.0> NODE_PATH=$(npm root -g) node tools/alturas.js     # nenhuma peça alta parece mais baixa do que bloqueia
    THREE_DIR=<three@0.128.0> NODE_PATH=$(npm root -g) node tools/celular.js     # celular emulado com toque
    THREE_DIR=<three@0.128.0> NODE_PATH=$(npm root -g) node tools/vitrine.js [pasta] [regex]   # folhas de quadros das animações
    node tools/build-mobile.js && NODE_PATH=$(npm root -g) node tools/app.js     # app sem internet + servidor
    node tools/comparar-cs.js 2 1 40000 H   # porte C# (Unity) igual ao sim.js, bit a bit (precisa do Mono)
    node tools/rede-cs.js                   # cliente online do Unity (Rede.cs) contra o servidor Node
    THREE_DIR=<three@0.128.0> NODE_PATH=$(npm root -g) node tools/unity-ver.js [pasta] [regex]   # mundo do Unity ao lado do oficial, sem o Unity (Mono)
    THREE_DIR=<three@0.128.0> NODE_PATH=$(npm root -g) node tools/exportar-primeira-pessoa.js   # mudou arma.js/skins.js? regrava Resources/primeira-pessoa.*
    THREE_DIR=<three@0.128.0> NODE_PATH=$(npm root -g) node tools/unity-fp.js   # primeira pessoa do Unity igual à do navegador, nó a nó (Mono)
    [THREE_DIR=<three@0.128.0>] NODE_PATH=$(npm root -g) node tools/unity-noite.js   # fim da noite do Unity (planta, gráfico, placar) numa noite de bots (Mono)
    [FOTOS=dist/telas/unity] node tools/unity-telas.js   # telas do Unity (IMGUI) por todo o jogo e online, layout do Unity em miniatura; FOTOS= grava PNG (Mono, Pillow)
    THREE_DIR=<three@0.128.0> NODE_PATH=$(npm root -g) node tools/telas-navegador.js [pasta] [regex]   # as mesmas telas no navegador, 1920×1080, com as fontes
O CDN do three.js pode estar bloqueado: instale `three@0.128.0` numa pasta temporária e passe em `THREE_DIR`.

## Estado atual (v6)
- Números: selar sob fogo 70%/45%, executar 4,5 s (dano recomeça), reanimar 5 s, momentos 3:00 / 10:00 / 18:00, eco aos 25%, ataque pausa a canalização por 0,6 s.
- Classes: Caçadores Soldado, Exorcista e Rastreador (pegadas e marcas de arrasto); Cultistas Ritualista e Guardião.
- Véu é silhueta (some a mais de 8 m), Transferência deixa rastro, sinais dos santuários descartam altares.
- Qualidade Alto/Médio/Baixo/Mínimo, resolução adaptativa, controles de toque, app Android.
- Rituais (v5.1): no máximo 3 por noite, um de cada vez, sem altar substituto; Cultistas vencem com 2 Fendas. `CFG.formatoNoite`: 'melhorDeTres' (padrão desde a v5.4: 2 rituais de um lado encerram) ou 'placar' (a noite vai até o amanhecer). A regra de início fica em `bloqueioRitual(g)`, usada pelo jogador e pelos bots.
- v5.1a: localização mais tardia (0,55/0,5), dano base menor, bots usam Transferência; snapshots escondem inimigos
  sem linha de visão (`CFG.visaoRede` 75 m, 0,6 s de tolerância; o cliente guarda o boneco invisível e não interpola
  desde a origem). O teste 3 de `tools/testar.js` confere isso.
- Equilíbrio entre bots (v5.1a, 30 partidas): Cultistas vencem 77%; Chamariz quase nunca é usado pelos bots.
  Falta playtest com pessoas.

- v5.2: Grande Ritual cinematográfico (céu e vitrais de sangue, sino, coro e tambor, corvos, névoa carmim;
  `atualizarGrande` no cliente, `grande` no snapshot do altar); um bot Cultista planta até dois Chamarizes.
- v5.3 (notas em `notas/v5.3.md`): jogo solo com escolha de papel e dificuldade (`opts.dificuldade` treino/facil/
  normal/dificil, `difDe(g,b)` só vale para os bots do lado contrário ao dos humanos; normal = comportamento antigo bit a
  bit), ping (`act` 'ping', evento com `team`, só a equipe recebe; servidor filtra) e frases rápidas (`act` 'fala'),
  bot aliado atende ping/ajuda (`ai.pedido`, `botAtendePing`), tutorial guiado e desafio da noite (no cliente),
  legendas dos sons, bússola, espectador, clima por partida (`climaDe` do matchId, cliente), linha do tempo e
  destaques, perfil/conquistas (localStorage), dicas, controle (gamepad), salas públicas (`/api/salas`) e chat do
  lobby e na partida (`t:'chat'`, Enter), dificuldade dos bots online (`t:'opcoes'`, só o anfitrião), caderno dos
  altares (J), Rever a noite (`REP`, posições a cada 2 s em `LT.rod[r].pos`), placar por jogador (do registro), falas
  automáticas do bot aliado (`atualizarFalasBots`, só cliente), música do menu (`MUS`), acessibilidade (`SET.suave`,
  `SET.dalt`) e Novidades. Tudo de regra portado para o C# (comparar-cs com o roteiro usando ping e frases; `DIF=`
  testa a dificuldade). `__T.perfil` é o desempenho; o perfil do jogador é `__T.perfilJogador`.
- v5.4 (notas em `notas/v5.4.md`, resposta a uma análise de especialista; preparação para o playtest com pessoas):
  a noite acaba quando um lado leva 2 rituais (`CFG.formatoNoite` 'melhorDeTres'); laboratório de playtest
  (`opts.lab` → `g.lab` via `labDe`: crepusculo, fogo, execucao, economia, formato; menu solo `SET.lab`, lobby
  `t:'opcoes'` com `lab`, vai no snapshot); relatório do playtest (`Sim.relatorio(log,r)`, tela no fim da noite,
  JSON com respostas; `tools/analisar.js` usa o mesmo); chat de equipe na partida (`para:'equipe'|'todos'`, o
  servidor filtra por papel; `SET.mudoAdv`); bots Caçadores conferem altar desperto com a Lanterna (sem sorteio).
  `tools/simular.js` e `tools/comparar-cs.js` aceitam `LAB=fogo=a,execucao=6,...`. Bots, 210 noites: culto 78% no
  normal, 67% com selo sob fogo 80%/60%; sem economia, 75%.
- v5.5 (notas em `notas/v5.5.md`): disputa no círculo. Eco do coro (`CFG.ecoProg` .25, `SETORES` oeste/centro/leste
  com dois altares cada, `g.eco`, evento 'eco' só para a caça); quem lançou sigilo há menos de `CFG.ataqueCanal` (.6 s)
  não canaliza; dano reinicia execução (`CFG.danoInterrompeExecucao`); o culto é avisado do selo (evento 'selando',
  só para o culto, `A.avisoSeloT`); Brecha Norte (PORTAIS, trilha e LUGARES; reexportar o mapa). Bots: `CORRIDA_BOT`
  1.3 com ritual em andamento, seguem o eco, `g.suspeitaC` (o culto só sabe o que viu ou o aviso de purga), não
  compram com ritual em andamento, histerese no mercador, `_segurou/_semSegurar` (0,5 s sem segurar zera o E),
  `botRasteja`. `tools/simular.js` aceita `CFG=`. Bots, 200 noites: culto 42–46% (v5.4: 78%).
- v5.6 (notas em `notas/v5.6.md`): auditoria e integridade. `Sim.eventoPara(g,e,me,role)` decide quem recebe cada
  evento (servidor e jogo local): sons/efeitos por `ALCANCE_SOM` ou emissor à vista (`_visto`), 'hit' só a quem atirou,
  'hurt' sem posição do atacante para os outros, eco/presságio só caça, 'selando' só culto. O snapshot da caça mascara o
  ritual não localizado; inimigo escondido vai sem hold/sealing/reag; tarefas, Chamarizes e Transferência só para a
  própria equipe; 'restart' só do anfitrião. Regras: Empurrão pausa a canalização (`ataqueT`, não `lastShotT`), Caçador
  no círculo localiza ('circulo'), `acaoNegada` confere a ação na hora (bots). Registro 2 (`VERSAO`, `REGISTRO`):
  `caca_perto`, `altar_errado`, `canal_saiu`, `acao_negada`; `relatorio` classifica a caça por ritual (`caca`,
  `conversao`). `tools/simular.js` com `SEMENTE=` é reproduzível byte a byte. Mostrador do círculo no chão (shader em
  `buildAltars`). Bots, sementes 1000–1199: culto 50%. v5.6a: `g.know` sem 'limpo': 'visto' (passou perto, nada desperto;
  não descarta), 'descartado' (sinal do santuário ou Chamariz queimado), 'purgado' (era do culto); `semAura(k)`.
- v5.7 (notas em `notas/v5.7.md`): recompensa e reconhecimento, nenhuma regra muda (partidas iguais à v5.6a passo a
  passo). Eventos novos: 'interrupcao' (`interrupcao(g,tipo,causa,src,t,A)` em `goDown` e no Empurrão; canalização de
  ritual não localizado só para o culto) e 'lance' (`lance(g,tipo,quem,A,team,v)`: selo_limite, achou_cedo, defesa,
  resgate, transferencia, chamariz; limiares `CFG.iscaLance/defesaLance/limiteLance`); 'doom'/'sealed' levam `altar` e
  `por`. Registro 3: `interrupcao`, `lance`, `disputa` (por ator no fim do ritual, `medirDisputa`/`disputaDano`, em
  `A.obs.d`), `purge.isca` (`A.iscaT`, segundos de Caçador a 12 m do Chamariz). `relatorio` traz `rituais[].disputa`,
  `interrupcoes`, `lances` e `engano.segundosPresosEmChamariz`; `analisar.js` compara a disputa dos rituais selados
  com os perdidos. Cliente: faixa `#lance`, `piscarMostrador` (uniform `fl`), `progressoInvestigacao`, dedução no eco,
  "Lances da noite" no resultado. Bots, sementes 1000–1199: culto 42% (a queda de 50% veio da v5.6a).
- v5.8 (notas em `notas/v5.8.md`): sensação de jogo, sem regra nova. Cliente prevê o tiro com `Sim.CADENCIA_HUMANA`
  (antes 0,3 s contra 0,42 s do servidor); evento 'hit' leva `escudo` (porte no C#). Viewmodel por `PERFIL_ARMA`
  (coice/mola/ciclo: alavanca `vmCarabina.alav`, tambor `vmRevolver.tambor`, runas do cetro), `VM.pausa` (pausa de
  impacto), `camFx()`/`SET.camFx`; impacto por superfície (`impactoMundo`, `impactoCorpo`, `jato`); reação do alvo
  (`A.reac`, `A.pausa`, `A.quedaT`, `A.levantaT` em `animateActor`); ritual como evento (`animarRitual`,
  `ritualIniciado/Marco/Selado/Fenda/Interrompido`, `O.runas`, `O.rv`). Sons gravados: `assets/sons/fontes` (CC0 e
  CC BY 3.0, origem em `fontes.json`), `python3 tools/montar-sons.py` gera `assets/sons/jogo/*.mp3`, `client/sons.js`
  (embutido por build.js e build-mobile.js, servido pelo servidor) e `docs/AUDIO.md`; no cliente `tocar(nome,opções)`
  com variação, limite de vozes e reserva sintetizada. Ninguém ouviu os sons ainda (ambiente sem áudio).
- v5.8a (notas em `notas/v5.8a.md`): a v5.8 no Unity. `montar-sons.py` copia os MP3 para `Resources/Sons`; `Som.cs`
  (`Amostra`, `Tiro`, `Passo`, laços gravados do ritual), `PrimeiraPessoa.cs` (perfis de arma, `Acerto`, `Golpe`,
  `Tranco`, `AoPasso`, `camFx` em PlayerPrefs "rr-camfx"), `Personagens.cs` (`Arma.alav/cao/tambor`, `Boneco.reac`),
  `MundoAtores.cs` (`ImpactoMundo/Corpo`, `AoSom`), `MundoAltares.cs` (runas do ritual, `RitualSelado/Fenda/
  Interrompido`, `MarcaInvestigacao`, `AoTremor`). Discord: `postar-discord.js` divide em vários cartões; o workflow
  posta cada `notas/*.md` novo ou `[discord: notas/x.md]` da mensagem do commit (segredo DISCORD_WEBHOOK ou _URL).
- v5.8b (notas em `notas/v5.8b.md`, do primeiro registro de playtest humano): lance 'selo_limite' usa `A.maxProg`
  (o progresso recua até a etapa enquanto se sela sem canalização); `comprar` nega munição cheia (80) e 4 sinalizadores;
  registro baixado/servidor levam `dificuldade` (e `lab`, `papel` no cliente); `analisar.js` aceita um arquivo só.
- v5.9 (notas em `notas/v5.9.md`, sistema em `docs/ANIMACAO.md`): primeira pessoa em `client/arma.js` (fábrica
  `RR_ARMA(dependências)`, chamada em `buildViewmodels`; embutido por build.js/build-mobile.js e servido pelo servidor).
  Mãos com esqueleto (16 ossos, 1 malha), braços por IK de dois ossos, clipes com chaves em quadros (corpo, arma,
  alavanca, guindaste, tambor), mistura entre estados, camadas (PERFIL_ARMA, respiração, passo, tranco), luz de leitura
  no shader dos materiais da primeira pessoa. Recargas: carabina pela portinhola (vazia com alavanca, tática sem),
  revólver de 8 câmaras com guindaste, extrator e municiador; sacar, inspeção (tecla I), ajuste, gesto de habilidade,
  corrida, selar, canalizar. Vitrine: F9 numa partida local ou `#vitrine`; `tools/vitrine.js` faz folhas de quadros
  (`ESTUDIO=1`, `QUADROS=`, `VISTAS=jogador,lado,peca`). 12 sons novos em `montar-sons.py`. Terceira pessoa: recarga com a
  mesma coreografia. Sem regra nova.
- v6 (notas em `notas/v6.md`, avaliação em `docs/DIRECAO.md`): mapa novo em duas regiões (catedral em cruz no promontório,
  `PROMONTORIO`/`CATEDRAL`; desfiladeiro `ABISMO` com três `TRAVESSIAS`: Ponte Grande, Ponte Caída, Aqueduto; mata com subáreas
  e os reagentes nela), `ZONAS` com `luar`/`coberto` (`zonaDe`, `coberto`). Cobertura baixa bloqueia tiros e sigilos abaixo da
  altura da peça (`raioParede` com BOX, `alturaAlvo`: 1,95/1,15/0,6; cabeça nos últimos 45 cm). Agachar (`inp.ag`, C/botão B,
  ×0,5, `passoLeve` a 5 m; bots não agacham). Tudo portado ao C# e bit a bit no comparar-cs. Cliente: `client/mundo.js`
  (catedral, pontes, paredões), muros arruinados nunca mais baixos que a colisão (`node tools/alturas.js` confere), execução
  com pose/tremor/fecho por lado (`EXE`, `fxExecucao`, `#exeVeu`), reagentes por subárea (`REAG_TIPOS`, `modeloReagente`,
  `atualizarReagentes`), velas-relógio no altar (`animarVelas`: vermelho-escuro = etapa garantida), ajoelhar em coleta/queima/
  reanimar, Arsenal no menu (`ARS`, `startArsenal`), skins (`client/skins.js`, 9, três por arma; `vis` no hello e no lobby),
  três inspeções novas (mecânica, exibição, inquietante), 12 sons novos CC0 via conquest e ambiente por lugar (`ambientePorLugar`).
  `VERSAO` 6.0. Bots, 400 noites: culto 38–40% (alvo do design 45–55%).
- Outra engine: `docs/PORTE.md` (regras, números, equivalências Unity/Unreal/Godot, ordem do porte) e
  `docs/mapa.json` (`node tools/exportar-mapa.js`, que também grava `unity/Assets/RitualReversal/Resources/mapa.json` e `simulacao.json`).
- Porte para o Unity (escolhido pelo dono; ele já importou o blockout no Unity 6.6 e andou pelo mapa):
  `unity/Assets/RitualReversal`. `Scripts/Simulacao/` é o `sim.js` inteiro em C# puro (regras, bots, navegação),
  conferido bit a bit contra o JS por `node tools/comparar-cs.js` (precisa de `apt-get install mono-mcs`).
  `PartidaLocal/Visual/Hud.cs` jogam contra bots no Unity (IMGUI); o importador cria o objeto "Partida".
  `Resources/simulacao.json` sai de `node tools/exportar-mapa.js`. Eixos: Unity (x, y, −z), giro −yaw.
  Mudou regra no `sim.js`? Porte para o C# e rode o comparar-cs. Os scripts do Unity compilam aqui contra os
  assemblies do NuGet `UnityEngine.Modules` 2021.3 (mcs com -r:), mas só o dono roda o Unity de verdade.
- Visual do protótipo no Unity (`Scripts/Visual/`): o cliente three.js portado para URP, montado por código no Play.
  Tudo nas coordenadas do protótipo sob uma raiz espelhada (1, 1, −1); `Mundo.Q` = Euler XYZ do three.
  `Geo.cs` (primitivas iguais às do three), `Texturas.cs` (makeNoise/pbr/canvas), `Materiais.cs` (URP Lit e o
  shader `Resources/RitualReversalBrilho.shader` para brilhos e sprites), `Mundo*.cs` (buildWorld, floresta,
  catedral viva, altares, céu/luzes/névoa/Grande Ritual, atores/efeitos/partículas), `Personagens.cs`
  (makeCharacter/animateActor, armas, cetro), `PrimeiraPessoa.cs` (viewmodels, lanterna, câmera), `Pos.cs`
  (Volume do URP), `Som.cs` (todos os SFX e laços sintetizados em OnAudioFilterRead). Menu "Criar cena do jogo".
  Online: `Rede.cs` (WebSocket, snapshot -> Jogo) e `PartidaOnline.cs` (lobby, previsão, interpolação, reconexão),
  mesmo protocolo do navegador; `Ator.hidden` só existe no cliente. Ajustes na pausa e no título (PlayerPrefs).
  v6 no Unity: `MundoV6.cs` é o `client/mundo.js` (catedral, interior, pontes, desfiladeiro, pisos, peças novas, mesma
  ordem de `vr()`), `MundoReagentes.cs` (reagentes por tipo, relógio de velas, fecho da execução), poses de ajoelhar/
  executar/agachar e a recarga em terceira pessoa no `Boneco`, véu da vítima no HUD, ambiente por lugar no `Som.cs`,
  shader `Resources/RitualReversalMarco.shader` (torres e telhados com 0,07 da névoa; sem ele, URP/Lit).
  Sem abrir o Unity: `tools/unity-ver.js` compila o visual contra `tools/cs/UnityFalso.cs` (UnityEngine de mentira),
  roda `Mundo.Construir()` (`tools/cs/ExportarMundo.cs`), desenha a exportação no three com a câmera, névoa e luzes de
  cada vista do oficial e grava oficial | Unity lado a lado em `dist/unity-ver/`. Também pega exceções na montagem.
  Primeira pessoa da v5.9/v6 no Unity: `ArmaFP.cs` é o motor do `client/arma.js` (amostrar, misturar, quadros de
  referência, IK, dedos, camadas, peças, cartuchos, municiador, cetro) com as contas do three em double (`V3d`, `Qd`, `Md`);
  `SkinsFP.cs` é o `client/skins.js` (papéis, detalhe em três planos, acessórios, pendentes, ganchos). A árvore, as malhas,
  os materiais e os clipes já preparados vêm do jogo oficial: `tools/exportar-primeira-pessoa.js` grava
  `Resources/primeira-pessoa.json`, `primeira-pessoa-geo.bytes` (pele das mãos em pedaços rígidos, um por osso) e
  `Resources/Detalhes/*.bytes` (PNG das texturas de detalhe). Shader `Resources/RitualReversalPrimeiraPessoa.shader`
  (UniversalFragmentPBR + luz de leitura + detalhe da skin + varredura; relevo por derivadas de tela; sem ele, URP/Lit).
  `PrimeiraPessoa.cs` ficou com a câmera (altura 1,67), a lanterna, o clarão e as cápsulas, e monta o corpo e o contexto
  que o `ArmaFP.Quadro` recebe. Tecla I inspeciona; `ArmaFP.Forcar`/`LuzEstudio`/`TrocarSkin` são a vitrine e o Arsenal.
  `tools/unity-fp.js` roda os mesmos casos no navegador e no `ArmaFP` (Mono, `tools/cs/TesteFP.cs`) e compara cada nó:
  656 poses, diferença máxima 2e-7 m, e uma passada de fumaça por todas as skins e entradas.
  Estilo do navegador no Unity: `Resources/RitualReversalIlustrado.shader` substitui o URP/Lit no `Mat.Lit` (mesmas
  propriedades; a luz do three em faixas, especular recortado, `_Rim` = RIMLUZ nos personagens e mercadores, `_NevoaK` nos
  marcos; luz comum em `Resources/RitualReversalLuz.hlsl`, também usada pela primeira pessoa; PlayerPrefs "rr-ilustrado" = 0
  volta ao URP/Lit). Etapa GRADE: `Resources/RitualReversalGrade.shader` (contorno a nanquim e névoa rasteira, em sRGB depois
  do ACES), posta por `Pos.cs` no renderizador do URP como FullScreenPassRendererFeature, por reflexão, e tirada no fim.
  Interface do navegador no Unity (IMGUI, `partial class PartidaLocal`): `PartidaInterface.cs` tem legendas dos sons,
  bússola, faixa dos lances, tela de queda e espectador, caderno dos altares (J); `MundoAltares.cs` tem o mostrador do
  círculo (`Resources/RitualReversalMostrador.shader`, `PiscarMostrador`). `PartidaArsenal.cs` é o Arsenal do menu
  (partida local parada, `ArmaFP.Forcar`/`Vista`/`LuzEstudio`, enquadramento deslocado como o setViewOffset do three).
  Fim da noite: `Noite.cs` (sem IMGUI) anota a linha do tempo (`LinhaDoTempo`, a cada quadro em `PassoNoite`) e tira do
  registro os marcos, lances, destaques e o placar; `PartidaNoite.cs` são o resumo, o resultado e o Rever a noite; online,
  o registro vem do caminho `log` do snapshot final (UnityWebRequest). `Desenho.cs` rasteriza a planta fixa do navegador
  (`PlantaFixa`) e o gráfico da noite em textura; `Tracos.cs` desenha no IMGUI o que muda a cada quadro; `PartidaPlanta.cs`
  é a planta do Tab e a escolha dos altares. `tools/unity-noite.js` confere tudo no Mono (e fotografa a planta do navegador).
  Clima: `Visual/MundoClima.cs` (`CLIMAS`, `ClimaDe` com o mesmo hash do navegador, `ChaveClima` sem o que vem depois de #,
  riscos de chuva que não caem sob o telhado, dois clarões e o trovão via `AoTrovao`, névoa e estrelas por clima); o laço da
  chuva gravada fica no `Som.cs` (`Chuva`). `PartidaLab.cs` é o laboratório (`LAB_UI`, `LabMudancas`, `PainelLab`).
  Menu: `PartidaMenu.cs` (título com o sigilo, papel solo, dificuldade, laboratório, desafio, Aprender a jogar, Como jogar,
  Novidades, Ajustes; `IniciarSolo` = startLocal; tutorial `PassosTut` com alvo na tela e na bússola, N pula; dicas `DICAS`,
  PlayerPrefs "rr-dicas"), `PartidaPerfil.cs` (perfil e conquistas em PlayerPrefs "rr-perfil", o mesmo JSON do navegador;
  desafio da noite com pontos e recorde do dia), `PartidaSala.cs` (sala pública, salas abertas por /api/salas, dificuldade,
  laboratório e chat no lobby, chat da partida com Enter/Tab/Esc, silenciar adversários).
  Relatório do playtest: `Noite.Relatorio` é o `Sim.relatorio` (o `unity-noite.js` compara os dois sobre o mesmo registro,
  campo a campo); `PartidaRelatorio.cs` é a tela (tabelas, perguntas em PlayerPrefs "rr-resp-<partida>", copiar, baixar).
  Frases rápidas e falas do bot aliado: `PartidaFalas.cs` (V abre, 1 a 6). Música do menu e trilha de tensão: `Visual/SomMusica.cs`
  (sintetizadas no OnAudioFilterRead; a partida passa o nível em `som.Tensao`, `AtualizarTensao` no PartidaVisual).
  Kit e mira: `PartidaKit.cs` (fileira de ferramentas com recarga, mira cruz/ponto/círculo com cor e tamanho que abre com o
  tiro, linha de quadros/ping); brilho em `Pos.brilho`; pausa com a cola das teclas.
  Sem abrir o Unity, `tools/unity-telas.js` roda o `PartidaLocal` inteiro (Update e as duas passadas do OnGUI) contra
  `tools/cs/UnityFalso.cs` + `IMGUIFalso.cs` pelo roteiro de `tools/cs/TesteTelas.cs` (título, menus, noite de cada lado,
  resumo, resultado, relatório, replay, perfil, tutorial, desafio, Arsenal); botões "apertados" pelo texto, no MouseUp.
  O `IMGUIFalso.cs` tem o GUILayout do Unity em miniatura (o algoritmo do GUILayoutGroup: margens que se fundem, FlexibleSpace
  com peso 10000, rolagem, áreas) e mede o texto com os TTF de verdade (cmap/hmtx); acusa o "Getting control N's position..."
  quando o Repaint pede controles que o Layout não teve. Um clique que muda a tela no meio da passada é encerrado com
  `GUIUtility.ExitGUI()` (`cliqueNaPassada` no `Bt`, try/catch em `OnGUI`). Com `FOTOS=` o Repaint grava o que desenhou e
  `tools/foto-imgui.py` faz o PNG (texto rico sem `<u>`, que o Unity não tem; negrito e itálico sintéticos). Giros no IMGUI:
  `Tracos.Girar` (o `RotateAroundPivot` erra o pivô com o GUI.matrix escalado, fora de 1080p); só fora de áreas.
  HUD com as medidas do CSS "interface mais quieta" (`PartidaHudPecas.cs`: faixas escuras, `Topo`, `Faixa` de losangos,
  `Tarefas`, `FeedHud`, `Vitais`, `KitHud` com o sensor, `Disputa` na `Placa`, `Marcador` por classe .mk, `MarcaAcerto`,
  `DirecaoDano`, `AnuncioHud`, `NotaHabilidade`); `PartidaCss.cs` põe o texto como o navegador (`Css(peso, tamanho, cor)`
  com a Barlow em 400/500/600/700, `Linha` pela altura de linha do CSS, `Paragrafo` com quebra própria, `NaBase`, sombra,
  `Moldura`, `Degrade`); caderno, frases, tutorial, dica, bússola, faixa do lance, queda e rótulos de interação também.
  Telas com o CSS do navegador, sem GUILayout (posições calculadas, `medindo` mede antes de desenhar): `PartidaCssTelas.cs`
  (button.go/gold/ghost com as pontas chanfradas em textura nove-partes, opções .dif/.set/.labRow/.repCtl, link .lk, campo
  input.txt, caixinha, fundo .screen e do título, `BotaoCss` com o foco do controle, `P`/`H2`/`H3` com o max-width 64ch e as
  margens que se fundem), `PartidaTitulo.cs` (coluna de 470 px, laboratório como details, online), `PartidaTelasCss.cs`
  (`PainelTela` centra e rola a placa de 800 px; `Cartoes` (.choice), `FileiraBotoes` (.btnrow), entrada da noite, Ajustes
  com a grade .set e as teclas em .binds, pausa com a .cola). Ajustes sem as linhas que o Unity não tem (toque, resolução
  adaptativa, mouse bruto). `PartidaMenusCss.cs`: Aprender a jogar, Como jogar (.passo), Novidades, Perfil (`Tabela` =
  table.res, .conqs); `PartidaNoiteCss.cs`: resumo e resultado (.lances, .premios, table.placar, .ltBox, statsTable,
  .desafio; a .btnrow quebra linha).
  Controle na partida (Input System): `EntradaUnity` soma os botões às teclas (o PAD_BOTAO do navegador) e dá os analógicos.
  Teclas configuráveis: `EntradaUnity.ACOES`/`Binds` (PlayerPrefs "rr-binds", os códigos do navegador), `Capturar` e `Definir`
  (troca quando a tecla já está em uso); o HUD e a pausa mostram a tecla escolhida (`EntradaUnity.Nome`).
  Menus pelo controle: todo botão das telas passa por `Bt` (`PartidaNavPad.cs`): cima/baixo move o foco, A aperta, B volta.
  Fontes do navegador em `Resources/Fontes` (OFL, `LEIA.txt`): Barlow Condensed no corpo e nos botões, IM Fell English nos
  títulos, seções, anúncio, lances e nomes da planta (`Estilos`, `TextoSvg`).
  Faltam (Unity): ver tudo num Unity de verdade (o desenho do IMGUI e os shaders nunca apareceram numa tela); controles de toque.

## Pendências
- Discord: falta o dono criar o segredo `DISCORD_WEBHOOK` no GitHub (Settings → Secrets and variables → Actions);
  depois disso o workflow posta sozinho as notas novas. Daqui da sessão: `DISCORD_WEBHOOK=... node tools/postar-discord.js`.
- Sons: ouvir em fones e acertar volumes; trocar o tiro montado por gravação de arma quando Freesound/OpenGameArt/
  Kenney/Sonniss estiverem liberados na rede do ambiente.
- Playtests com 4 pessoas antes de mais conteúdo.
- Unity igual ao oficial (pedido do dono): feitos o mundo da v6, reagentes, velas, execução, ambiente, a primeira pessoa
  da v5.9, as skins, o Arsenal, GRADE/RIMLUZ e parte da interface; falta a lista em "Visual do protótipo no Unity".
- v6 sem gente: equilíbrio entre bots ficou em 38–40% para o culto; os sons novos não foram ouvidos (ambiente sem áudio).
- Unity: o dono testar o visual portado e o online (personagens, efeitos e rede nunca rodaram num Unity de verdade).
  Os shaders Ilustrado, PrimeiraPessoa, Grade e Mostrador nunca compilaram num Unity de verdade (só o C# é conferido aqui); se o
  mundo ficar rosa, PlayerPrefs "rr-ilustrado" = 0 volta ao URP/Lit.
- Play Store: falta APK/AAB assinado e conta de desenvolvedor.
