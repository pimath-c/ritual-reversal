# Ritual Reversal no Unity — primeiro passo: o mapa em caixas

Esta pasta traz o começo do porte para o Unity: o mapa inteiro do protótipo em caixas cinzas, com a mesma
colisão do jogo no navegador, e um jogador em primeira pessoa para andar por ele. Arte, regras e rede vêm
depois (veja `docs/PORTE.md`).

## Instalar

1. No **Unity Hub**, crie um projeto novo com o modelo **Universal 3D** (URP). Unity 6 ou 2022.3 servem.
2. Copie a pasta `Assets/RitualReversal` deste pacote para dentro da pasta `Assets` do projeto.
   (No Unity, dá para arrastar a pasta do Explorer para a janela Project.)
3. Espere o Unity compilar. Deve aparecer um menu novo no topo: **Ritual Reversal**.

## Montar o mapa

1. Abra uma cena vazia (File > New Scene, Basic) ou use a SampleScene.
2. Menu **Ritual Reversal > Importar mapa (blockout)**. Leva alguns segundos.
3. Salve a cena (Ctrl+S).
4. Aperte **Play**. Você nasce no acampamento dos Caçadores, olhando para a catedral.

Controles: WASD anda, Shift corre, mouse olha. Esc solta o mouse; clique prende de novo.
No canto da tela aparece a posição em coordenadas do protótipo, para comparar com o jogo no navegador.

## O que foi criado

- `Mapa Ritual Reversal` na cena, com:
  - Estático: chão, trilhas, clareiras, 2.117 peças de colisão, 880 árvores, pilares e bancos
    (marcados como estáticos, para o Unity juntar os desenhos).
  - Objetivos: os 6 altares com o círculo de 3 m, reagentes, mercadores, pontos de tarefa, nomes dos lugares e spawns.
  - Luzes: lua, velas, lanternas, fogueira e braseiros; névoa ligada.
  - Jogador: `CharacterController` com o mesmo raio (0,4 m) e velocidades (4,6 e 6 m/s) do protótipo.
- `Assets/RitualReversal/Materiais`: um material por tipo de peça. Trocar a cor ou a textura de um muda todas as peças daquele tipo.

Rodar o importador de novo apaga o mapa anterior e monta outro (Ctrl+Z desfaz).

## Arquivos

- `Dados/mapa.json` — a planta, gerada por `node tools/exportar-mapa.js` a partir de `shared/sim.js`.
- `Editor/ImportarMapa.cs` — o importador (só roda no editor).
- `Scripts/MapaDados.cs` — formato do JSON e conversão de coordenadas (protótipo x, y, z → Unity x, y, −z).
- `Scripts/JogadorFPS.cs` — o jogador em primeira pessoa (Input System novo ou antigo).
- `Scripts/OlharParaCamera.cs` — rótulos virados para a câmera.

## Se algo der errado

- **Erro de compilação no Console**: copie a mensagem e mande para o Claude.
- **O menu não aparece**: confira se `ImportarMapa.cs` ficou dentro de uma pasta chamada `Editor`.
- **Tudo rosa**: o projeto não é URP. Crie o projeto com o modelo Universal 3D, ou troque o shader dos materiais.
- **O mouse não gira a câmera**: clique dentro da janela Game para prender o mouse.

## Próximos passos (na ordem do `docs/PORTE.md`)

1. Andar do spawn até cada altar e comparar o tempo com o protótipo.
2. Regras: altares, rituais, selamento, momentos da noite e vitória, portando `shared/sim.js` para C#.
3. Multiplayer com Netcode for GameObjects, cada equipe recebendo só o que pode saber.
4. Bots simples, sensação das armas, atmosfera, e a arte por cima das caixas.
