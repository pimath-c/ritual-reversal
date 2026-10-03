# Ritual Reversal no Unity

O jogo do protótipo roda no Unity, jogável contra bots, com o **visual e o som do protótipo**: a catedral com vitrais,
raios de luz, lustres, velas, tapetes, estandartes e corvos; a floresta com carvalhos, samambaias, trilhas, clareiras
e luar; céu, lua, névoa por momento e o Grande Ritual (céu de sangue, sino, coro, tambor); os altares com círculo de
runas, feixe e brasas; Cultistas e Caçadores com animação procedural (andar, mirar, recarregar, canalizar, cair,
morrer e se dissolver), armas na mão em primeira pessoa, lanterna, partículas, efeitos das habilidades, mercadores,
pontos de tarefa, pegadas e sal; pós-processamento (bloom, ACES, vinheta, granulado, aberração com a sanidade) e
todos os sons sintetizados na hora, como no navegador. Nada disso usa arquivos de arte: é tudo montado por código
no Play, a partir de `Resources/mapa.json`.

A simulação (`Scripts/Simulacao`) é um porte linha a linha de `shared/sim.js`, conferido contra o protótipo passo a
passo: com o mesmo sorteio, o C# e o JavaScript jogam partidas idênticas bit a bit (`node tools/comparar-cs.js`).

## Instalar ou atualizar

1. No **Unity Hub**, crie um projeto com o modelo **Universal 3D** (URP). Unity 6 ou 2022.3 servem.
   Se você já tem o projeto da versão anterior, use o mesmo.
2. Copie a pasta `Assets/RitualReversal` deste pacote para dentro da pasta `Assets` do projeto, **pelo Explorer do Windows**
   (no Unity: botão direito em Assets > Show in Explorer). Se perguntar, escolha **substituir os arquivos**.
3. Volte ao Unity e espere compilar. O menu **Ritual Reversal** aparece no topo.

## Jogar

1. Menu **Ritual Reversal > Criar cena do jogo**. Isso abre uma cena nova só com a câmera e o objeto **Partida**.
   (Se preferir a cena antiga com o blockout, ela também serve: no Play as caixas somem e o mundo do protótipo
   aparece no lugar.)
2. Salve a cena (Ctrl+S) e aperte **Play**. Os primeiros segundos são o mundo sendo montado (texturas e malhas).
3. Escolha **Caçar** ou **Celebrar**. Como Cultista, você escolhe os 3 altares. Depois escolha classe e feitiço e clique em **Estou pronto**.

Controles (os mesmos do protótipo):

| Tecla | Ação |
|---|---|
| WASD, Shift | andar, correr |
| Mouse, clique esquerdo | mirar, atacar |
| Clique direito | feitiço da classe |
| E (segurar) | interagir: coletar, consagrar, iniciar, selar, purgar, reanimar, executar, tarefas; perto de um mercador, abre a loja |
| T (segurar) | Transferência (Cultista) |
| Q, F, G, R | Sensor Áurico, lanterna, sinalizador, recarregar (Caçador) |
| Tab (segurar) | planta com os altares como a sua equipe os conhece |
| Esc | pausa |

Para só passear pelo blockout, sem partida, importe o mapa (menu **Importar mapa (blockout)**) e desative o objeto **Partida** antes do Play.

## O que tem em cada pasta

- `Scripts/Simulacao/` — a simulação, em C# puro (não usa nada do Unity). `Sim.cs` (regras), `Bots.cs`, `Mapa.cs`
  (colisão, visão, navegação A*), `Tipos.cs` (estado e tabelas: CFG, classes, feitiços, itens, tarefas),
  `Base.cs` e `Trig.cs` (contas iguais às do navegador, para o resultado bater bit a bit).
- `Resources/simulacao.json` — a planta que a simulação usa, gerada por `node tools/exportar-mapa.js`.
- `Scripts/PartidaLocal.cs`, `PartidaVisual.cs`, `PartidaHud.cs` — a partida no Unity: laço, entrada, eventos
  (efeito e som de cada um) e o HUD com as telas (IMGUI, sem assets): marcadores de aliados e rituais, direção do dano,
  marca de acerto, avisos das habilidades.
- `Scripts/Visual/` — o cliente do protótipo portado: `Geo.cs` (formas do three.js), `Texturas.cs` (texturas por
  ruído e desenho), `Materiais.cs` e `Resources/RitualReversalBrilho.shader` (brilhos, sprites, céu),
  `Mundo.cs` + `MundoFloresta/MundoCatedral/MundoAltares/MundoVida/MundoAtores.cs` (cenário, luzes, névoa, altares,
  bonecos, partículas e efeitos), `Personagens.cs` (Cultistas, Caçadores, armas, cetro), `PrimeiraPessoa.cs`
  (armas na mão, lanterna, câmera), `Pos.cs` (pós-processamento) e `Som.cs` (som sintetizado).
- `Scripts/EntradaUnity.cs` — teclado e mouse (Input System novo ou antigo).
- `Editor/ImportarMapa.cs` — os menus (cena do jogo e importador do blockout); `Resources/mapa.json` é a planta visual.
- `Scripts/GerarTextura.cs` — texturas geradas por código (pedra, lajota, madeira, casca, terra, grama, folhagem, osso).
  O importador salva em `Texturas/` e aplica com 1 repetição a cada 2 m, sem esticar. Para trocar por texturas de
  verdade, basta arrastar outra imagem para o campo Base Map do material em `Materiais/`.
- `Scripts/JogadorFPS.cs`, `MapaDados.cs`, `OlharParaCamera.cs` — o passeio livre pelo mapa e utilidades.

## Se algo der errado

- **Erro vermelho no Console**: copie a mensagem e mande para o Claude.
- **"falta Resources/simulacao.json"**: a pasta `Resources` não foi copiada; copie a pasta `RitualReversal` inteira de novo.
- **Tudo rosa**: o projeto não é URP. Crie o projeto com o modelo Universal 3D.
- **Brilhos (velas, runas, céu) quadrados ou rosa**: o shader `Resources/RitualReversalBrilho.shader` não compilou;
  mande o erro dele (clique no arquivo, aparece no Inspector).
- **Sem bloom nem vinheta**: na câmera, marque **Post Processing** (o jogo tenta ligar sozinho).
- **O mouse não gira**: clique em Continuar na tela de pausa (ou clique dentro da janela Game).
- **Lento**: mude `Qualidade.nivel` (3 alto, 2 médio, 1 baixo, 0 mínimo) em `Scripts/Visual/Qualidade.cs`.

## Ainda falta

- Contorno a nanquim e névoa rasteira da etapa final de imagem do protótipo, e a luz de recorte nas silhuetas.
- Jogar online (cliente WebSocket para o servidor Node) e a tela de ajustes (sensibilidade, volume, qualidade).
