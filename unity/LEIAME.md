# Ritual Reversal no Unity

O jogo do protótipo roda no Unity, jogável contra bots e online, **igual à versão do navegador** (a v6): o mapa com a
catedral em cruz no promontório, o desfiladeiro com a Ponte Grande, a Ponte Caída e o Aqueduto, e a mata com os
reagentes; o estilo ilustrado (luz em faixas, contorno a nanquim, névoa rasteira, luz de recorte nas silhuetas); o
clima de cada noite (chuva, tempestade com relâmpagos, névoa); a primeira pessoa com mãos articuladas, recargas,
inspeções e as 9 skins; o Arsenal; o Grande Ritual; e toda a interface do navegador (HUD, bússola, legendas dos sons,
caderno dos altares, frases rápidas, tutorial, dicas, desafio da noite, perfil e conquistas, resumo, resultado, Rever a
noite e o relatório do playtest), com as mesmas fontes e as mesmas medidas. Nada disso usa arquivos de arte: é tudo
montado por código no Play, a partir de `Resources/mapa.json` e dos dados exportados do jogo oficial.

A simulação (`Scripts/Simulacao`) é um porte linha a linha de `shared/sim.js`, conferido contra o protótipo passo a
passo: com o mesmo sorteio, o C# e o JavaScript jogam partidas idênticas bit a bit (`node tools/comparar-cs.js`).

## Novidades desta versão (24/10)

- **Fotos de referência: F10** numa partida contra bots. O jogo passa sozinho pelos mesmos 17 pontos do mapa que o
  Claude fotografa no navegador (noite limpa, sem HUD, uns 45 s) e grava um PNG de cada um. No fim aparece a pasta
  (no Windows, algo como `C:\Users\<você>\AppData\LocalLow\<empresa>\<projeto>\referencia-<data>`). Mande a pasta
  zipada: ela vira uma comparação lado a lado, navegador × Unity, de cada ponto. F10 de novo interrompe.
- Culto controlado pelo jogo revida quem atira nele enquanto canaliza (v6.1).
- O build leva todas as variantes dos shaders (chão e texturas certos no jogo compilado).

## Novidades da versão de 23/10

- Interface conferida pixel a pixel com o navegador: HUD, menus, sala, loja, pausa, perfil, escolha dos altares,
  Rever a noite e relatório com as mesmas medidas, alturas de linha e fontes.
- Texto nítido em qualquer resolução (1440p, 4K, celular), inclusive botões e campos de texto.
- Símbolos (◆ ✓ ✕ ✚ ✶ ☉...) com a mesma fonte do navegador (DejaVu Sans, que vem junto): não viram quadradinhos.
- Legendas dos sons com a seta do navegador; bússola em itálico; anúncios, interação e disputa do ritual no tamanho
  certo no celular.
- Controles de toque, HUD compacto do celular e resolução adaptativa (Ajustes).
- Vitrine das animações: **F9** numa partida contra bots (arma, clipe, velocidade, vista, luz, linha do tempo).

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
3. Na tela inicial, escolha o lado (**Caça**, **Celebra** ou **Sorteia**) e a força dos bots e clique em **Jogar sozinho,
   com bots**. Como Cultista, você escolhe os 3 altares. Depois escolha classe e feitiço e clique em **Estou pronto**.
   Na primeira vez, **Aprender a jogar** é o tutorial guiado; **Arsenal** mostra as armas e as skins.

Para jogar **online** com amigos (o mesmo servidor do protótipo, dá até para misturar quem joga no navegador com
quem joga no Unity):

1. Quem hospeda roda o servidor (precisa do Node.js 18 ou mais novo): abra um terminal na pasta `server` deste pacote e
   rode `npm start` (porta 8080; a pasta já vem com as dependências). Quem joga pelo navegador abre `http://localhost:8080`.
   Para amigos de fora, `cloudflared tunnel --url http://localhost:8080` dá um endereço `https://...`.
2. Na tela inicial, em **Jogar online**, escreva o endereço do servidor (`localhost:8080`, um IP da rede de casa
   como `192.168.0.10:8080`, ou o `https://...` do túnel) e seu nome.
3. **Criar sala** mostra um código de 4 letras; os outros usam **Entrar na sala** com esse código. Vagas vazias
   ficam com bots. O anfitrião clica em **Começar a partida**.
4. Se a conexão cair no meio da noite, o jogo tenta voltar sozinho por 2 minutos; enquanto isso um bot joga por você.

Na pausa (Esc) e na tela inicial ficam os **ajustes**: sensibilidade do mouse, volume, campo de visão, brilho,
qualidade (Alto, Médio, Baixo, Mínimo; vale a partir do próximo Play), mira, legendas, as teclas, controles de toque e
resolução adaptativa. Ficam salvos.

Controles (os mesmos do protótipo; dá para trocar as teclas nos Ajustes):

| Tecla | Ação |
|---|---|
| WASD, Shift | andar, correr |
| C (segurar) | agachar: devagar e quase sem som |
| Mouse, clique esquerdo | mirar, atacar |
| Clique direito | feitiço da classe |
| E (segurar) | interagir: coletar, consagrar, iniciar, selar, purgar, reanimar, executar, tarefas; perto de um mercador, abre a loja |
| T (segurar) | Transferência (Cultista) |
| Q, F, G, R | Sensor Áurico, lanterna, sinalizador, recarregar (Caçador) |
| I | inspecionar a arma |
| Tab (segurar) | planta com os altares como a sua equipe os conhece |
| Z | marcar um ponto para a equipe |
| V, depois 1 a 6 | frases rápidas |
| J | caderno dos altares |
| Enter | chat da partida (Tab troca entre equipe e todos) |
| N | pular o passo do tutorial |
| F9 | vitrine das animações (só contra bots) |
| Esc | pausa |

Controle (com o Input System): analógico esquerdo anda, direito olha; RT ataca, RB feitiço, LT corre; A interage,
B agacha, X recarrega, Y lanterna, LB sensor; direcional para a esquerda transfere, para baixo sinalizador, para cima
as frases; clique no analógico direito marca; Select a planta; Start pausa. Nos menus, cima/baixo e A/B.

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
  (armas na mão com o perfil de cada uma, lanterna, câmera), `Pos.cs` (pós-processamento) e `Som.cs` (sons gravados
  de `Resources/Sons`, com o som sintetizado de reserva).
- `Resources/Sons/` — os 67 MP3 do jogo (licenças em `docs/AUDIO.md`), copiados por `python3 tools/montar-sons.py`.
- `Scripts/Rede.cs`, `PartidaOnline.cs` — o online: conexão, leitura do snapshot, lobby, previsão e interpolação.
- `Scripts/EntradaUnity.cs` — teclado e mouse (Input System novo ou antigo).
- `Editor/ImportarMapa.cs` — os menus (cena do jogo e importador do blockout); `Resources/mapa.json` é a planta visual.
- `Scripts/GerarTextura.cs` — texturas geradas por código (pedra, lajota, madeira, casca, terra, grama, folhagem, osso).
  O importador salva em `Texturas/` e aplica com 1 repetição a cada 2 m, sem esticar. Para trocar por texturas de
  verdade, basta arrastar outra imagem para o campo Base Map do material em `Materiais/`.
- `Scripts/JogadorFPS.cs`, `MapaDados.cs`, `OlharParaCamera.cs` — o passeio livre pelo mapa e utilidades.

## Se algo der errado

- **Erro vermelho no Console**: copie a mensagem e mande para o Claude.
- **"falta Resources/simulacao.json"**: a pasta `Resources` não foi copiada; copie a pasta `RitualReversal` inteira de novo.
- **Tudo rosa**: o projeto não é URP. Crie o projeto com o modelo Universal 3D. Se o projeto é URP e só o mundo ficou
  rosa, o shader `Resources/RitualReversalIlustrado.shader` não compilou: mande o erro dele (clique no arquivo, aparece
  no Inspector) e, para jogar enquanto isso, apague esse arquivo (o jogo volta ao URP/Lit, sem o estilo ilustrado).
- **Brilhos (velas, runas, céu) quadrados ou rosa**: o shader `Resources/RitualReversalBrilho.shader` não compilou;
  mande o erro dele (clique no arquivo, aparece no Inspector).
- **No editor fica certo, mas o jogo compilado (Build) fica sem chão ou com texturas erradas** (folhas quadradas, vidro
  opaco, sem brilho): o build jogou fora variantes dos shaders que só o código usa. O script `Editor/VariantesShaders.cs`
  grava, antes de cada build, os materiais de `Resources/Variantes` que seguram essas variantes. Se ainda acontecer, rode
  **Ritual Reversal > Gerar variantes dos shaders**, confira se a pasta `Resources/Variantes` tem 64 materiais e compile de
  novo. O primeiro build depois disso demora mais (compila as variantes); os seguintes usam o cache.
- **Sem bloom nem vinheta**: na câmera, marque **Post Processing** (o jogo tenta ligar sozinho).
- **O mouse não gira**: clique em Continuar na tela de pausa (ou clique dentro da janela Game). Com a vitrine (F9)
  aberta o mouse fica solto de propósito: F9 de novo fecha.
- **Duas cópias de um script ("already contains a definition")**: sobrou uma pasta antiga fora de
  `Assets/RitualReversal`; apague a cópia velha.
- **Sem som gravado** (só bipes sintetizados): confira se `Resources/Sons` tem os MP3; no Inspector de cada um,
  Load Type tem de ser Decompress On Load (o padrão).
- **Lento**: baixe a qualidade nos ajustes (Esc) e aperte Play de novo.
- **Online não conecta**: confira se o servidor está rodando (abra o endereço no navegador: o protótipo deve abrir) e
  se o firewall do Windows deixou o Node aceitar conexões.

## Ainda falta

- O jogo já compilou no seu Unity, mas a comparação visual com o navegador ainda não foi feita: é para isso o F10.
  Personagens, efeitos e o online também nunca foram vistos lado a lado. Qualquer erro vermelho no Console, mande para o Claude.
- O texto do Unity sai alguns pixels mais largo que o do navegador em linhas longas (o Unity avança cada letra em
  pixels inteiros), e letras de tamanho fracionário do CSS (14,5 e 11,5 px) saem no inteiro mais perto.
