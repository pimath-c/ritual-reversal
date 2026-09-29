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
O CDN do three.js pode estar bloqueado: instale `three@0.128.0` numa pasta temporária e passe em `THREE_DIR`.

## Estado atual (v5)
- Números: selar sob fogo 70%/45%, executar 4,5 s, reanimar 5 s, momentos 3:00 / 10:00 / 18:00.
- Classes: Caçadores Soldado, Exorcista e Rastreador (pegadas e marcas de arrasto); Cultistas Ritualista e Guardião.
- Véu é silhueta (some a mais de 8 m), Transferência deixa rastro, sinais dos santuários descartam altares.
- Qualidade Alto/Médio/Baixo/Mínimo, resolução adaptativa, controles de toque, app Android.
- Equilíbrio entre bots: Cultistas vencem cerca de 83% (limitação da IA dos bots; falta playtest com pessoas).

## Pendências
- Postar `notas/v5.md` no Discord: precisa de `discord.com` liberado na rede do ambiente e da credencial
  `DISCORD_WEBHOOK`; então `node tools/postar-discord.js notas/v5.md`.
- Playtests com 4 pessoas antes de mais conteúdo.
- Play Store: falta APK/AAB assinado e conta de desenvolvedor.
