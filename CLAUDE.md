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
O CDN do three.js pode estar bloqueado: instale `three@0.128.0` numa pasta temporária e passe em `THREE_DIR`.

## Estado atual (v5.1a)
- Números: selar sob fogo 70%/45%, executar 4,5 s, reanimar 5 s, momentos 3:00 / 10:00 / 18:00.
- Classes: Caçadores Soldado, Exorcista e Rastreador (pegadas e marcas de arrasto); Cultistas Ritualista e Guardião.
- Véu é silhueta (some a mais de 8 m), Transferência deixa rastro, sinais dos santuários descartam altares.
- Qualidade Alto/Médio/Baixo/Mínimo, resolução adaptativa, controles de toque, app Android.
- Rituais (v5.1): no máximo 3 por noite, um de cada vez, sem altar substituto; Cultistas vencem com 2 Fendas. `CFG.formatoNoite`: 'placar' (padrão, a noite sempre chega à Hora Morta) ou 'melhorDeTres' (2 a 0 encerra). A regra de início fica em `bloqueioRitual(g)`, usada pelo jogador e pelos bots.
- v5.1a: localização mais tardia (0,55/0,5), dano base menor, bots usam Transferência; snapshots escondem inimigos
  sem linha de visão (`CFG.visaoRede` 75 m, 0,6 s de tolerância; o cliente guarda o boneco invisível e não interpola
  desde a origem). O teste 3 de `tools/testar.js` confere isso.
- Equilíbrio entre bots (v5.1a, 30 partidas): Cultistas vencem 77%; Chamariz quase nunca é usado pelos bots.
  Falta playtest com pessoas.

- v5.2: Grande Ritual cinematográfico (céu e vitrais de sangue, sino, coro e tambor, corvos, névoa carmim;
  `atualizarGrande` no cliente, `grande` no snapshot do altar); um bot Cultista planta até dois Chamarizes.
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
  Faltam: contorno a nanquim e névoa rasteira da etapa GRADE, luz de recorte (RIMLUZ), rede e tela de ajustes.

## Pendências
- Postar `notas/v5.md` no Discord: precisa de `discord.com` liberado na rede do ambiente e da credencial
  `DISCORD_WEBHOOK`; então `node tools/postar-discord.js notas/v5.md`.
- Playtests com 4 pessoas antes de mais conteúdo.
- Unity: o dono testar o visual portado (personagens e efeitos nunca rodaram num Unity de verdade); depois rede.
- Play Store: falta APK/AAB assinado e conta de desenvolvedor.
