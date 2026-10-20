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
- `server/server.js` — servidor autoritativo (30 Hz, snapshots a 20 Hz), salas de 4 letras.
- `mobile/` — app Android (Capacitor 8). `.github/workflows/android.yml` compila o APK a cada envio
  (Actions → "APK Android" → Artifacts).
- `notas/` — patch notes. `tools/postar-discord.js` posta no Discord pelo webhook em `DISCORD_WEBHOOK`
  (credencial do ambiente ou segredo do GitHub; nunca no repositório).

## Testes (rode antes de cada commit que mexe no jogo)
    node tools/testar.js 6          # grafo, caminhadas até todos os alvos, partidas de bots com invariantes
    node tools/protocolo.js         # servidor + clientes falsos
    node tools/build.js             # gera dist/ritual-reversal.html
    THREE_DIR=<three@0.128.0> NODE_PATH=$(npm root -g) node tools/navegador.js   # Chromium, 13 vistas, sem erros
    THREE_DIR=<three@0.128.0> NODE_PATH=$(npm root -g) node tools/celular.js     # celular emulado com toque
    node tools/build-mobile.js && NODE_PATH=$(npm root -g) node tools/app.js     # app sem internet + servidor
    node tools/comparar-cs.js 2 1 40000 H   # porte C# (Unity) igual ao sim.js, bit a bit (precisa do Mono)
    node tools/rede-cs.js                   # cliente online do Unity (Rede.cs) contra o servidor Node
O CDN do three.js pode estar bloqueado: instale `three@0.128.0` numa pasta temporária e passe em `THREE_DIR`.

## Estado atual (v5.6)
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
  `buildAltars`). Bots, sementes 1000–1199: culto 50%.
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
  Faltam: contorno a nanquim e névoa rasteira da etapa GRADE e a luz de recorte (RIMLUZ).

## Pendências
- Postar `notas/v5.md` no Discord: precisa de `discord.com` liberado na rede do ambiente e da credencial
  `DISCORD_WEBHOOK`; então `node tools/postar-discord.js notas/v5.md`.
- Playtests com 4 pessoas antes de mais conteúdo.
- Unity: o dono testar o visual portado e o online (personagens, efeitos e rede nunca rodaram num Unity de verdade).
- Play Store: falta APK/AAB assinado e conta de desenvolvedor.
