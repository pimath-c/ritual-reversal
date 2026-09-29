# Ritual Reversal no Unity

O jogo inteiro do protótipo já roda no Unity, jogável contra bots: regras, bots, navegação, rituais, selamento,
tarefas, loja, habilidades e fim de noite. A arte ainda é de caixas (blockout), e ainda não há som nem rede
(veja `docs/PORTE.md`).

A simulação (`Scripts/Simulacao`) é um porte linha a linha de `shared/sim.js`. Ela foi conferida contra o protótipo
passo a passo: com o mesmo sorteio, o C# e o JavaScript jogam partidas idênticas bit a bit (posições, tiros,
decisões dos bots, eventos). O teste é `node tools/comparar-cs.js`.

## Instalar ou atualizar

1. No **Unity Hub**, crie um projeto com o modelo **Universal 3D** (URP). Unity 6 ou 2022.3 servem.
   Se você já tem o projeto da versão anterior, use o mesmo.
2. Copie a pasta `Assets/RitualReversal` deste pacote para dentro da pasta `Assets` do projeto, **pelo Explorer do Windows**
   (no Unity: botão direito em Assets > Show in Explorer). Se perguntar, escolha **substituir os arquivos**.
3. Volte ao Unity e espere compilar. O menu **Ritual Reversal** aparece no topo.

## Jogar

1. Menu **Ritual Reversal > Importar mapa (blockout)**. Isso refaz o mapa e cria o objeto **Partida**.
   (Se você importou o mapa na versão anterior, importe de novo: a versão antiga não tem a Partida.)
2. Salve a cena (Ctrl+S) e aperte **Play**.
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

Para só passear pelo mapa, sem partida, desative o objeto **Partida** (dentro de "Mapa Ritual Reversal") antes do Play.

## O que tem em cada pasta

- `Scripts/Simulacao/` — a simulação, em C# puro (não usa nada do Unity). `Sim.cs` (regras), `Bots.cs`, `Mapa.cs`
  (colisão, visão, navegação A*), `Tipos.cs` (estado e tabelas: CFG, classes, feitiços, itens, tarefas),
  `Base.cs` e `Trig.cs` (contas iguais às do navegador, para o resultado bater bit a bit).
- `Resources/simulacao.json` — a planta que a simulação usa, gerada por `node tools/exportar-mapa.js`.
- `Scripts/PartidaLocal.cs`, `PartidaVisual.cs`, `PartidaHud.cs` — a partida no Unity: laço, câmera, entrada,
  bonecos, altares, efeitos e o HUD com as telas (IMGUI, sem assets).
- `Scripts/EntradaUnity.cs` — teclado e mouse (Input System novo ou antigo).
- `Editor/ImportarMapa.cs` — o importador do mapa (blockout); `Dados/mapa.json` é a planta visual.
- `Scripts/JogadorFPS.cs`, `MapaDados.cs`, `OlharParaCamera.cs` — o passeio livre pelo mapa e utilidades.

## Se algo der errado

- **Erro vermelho no Console**: copie a mensagem e mande para o Claude.
- **"falta Resources/simulacao.json"**: a pasta `Resources` não foi copiada; copie a pasta `RitualReversal` inteira de novo.
- **Tudo rosa**: o projeto não é URP. Crie o projeto com o modelo Universal 3D.
- **O mouse não gira**: clique em Continuar na tela de pausa (ou clique dentro da janela Game).
- **Está escuro demais**: é a noite do protótipo. A lanterna (F) ajuda o Caçador; as trilhas e clareiras pegam luar.

## Próximos passos (na ordem do `docs/PORTE.md`)

1. Jogar algumas partidas e anotar o que parece diferente do protótipo.
2. Som: o protótipo gera tudo por código (tiros, coro do ritual, sino, passos); dá para portar do mesmo jeito.
3. Rede: servidor autoritativo (a mesma simulação) e cada equipe recebendo só o que pode saber (o `snapshot`).
4. Arte por cima das caixas: catedral, floresta, personagens, luz e névoa. A colisão continua a mesma.
