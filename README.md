# Ritual Reversal — protótipo v3 (multiplayer)

Terror competitivo 2v2 com troca de lados. O servidor é autoritativo: as regras rodam nele (`shared/sim.js`),
e o navegador só desenha e envia comandos. O mesmo `sim.js` roda no modo solo, então as regras são idênticas.

## Rodar

Precisa de Node.js 18 ou mais novo.

    cd server
    npm install
    npm start

Abra http://localhost:8080. Na tela inicial aparece "Jogar online": crie uma sala e mande o código de 4 letras.

## Chamar amigos de fora da sua rede

Instale o cloudflared e, com o servidor rodando, em outro terminal:

    cloudflared tunnel --url http://localhost:8080

Ele imprime um link https://....trycloudflare.com. Seus amigos abrem ESSE link (não o link do claude.ai)
e entram com o código da sala. O link muda cada vez que você roda o comando.

## O mapa

A catedral fica no centro e uma floresta a envolve por todos os lados (310 × 216 m, tamanho para 2v2; a planta
original de 350 × 260 m continua descrita em `HALF0` e volta com `HALF=HALF0` para um 4v4). Os dois lados nascem na
mata: o acampamento dos Caçadores a oeste, o Círculo de Pedras dos Cultistas a leste. A catedral tem portas nos
quatro lados e duas brechas.

- Dentro: o anel de capelas (Capela Oeste, Abside, Capela Leste, Nártex) e o Claustro a céu aberto, único lugar
  com reagentes livres. A Cripta e o Ossuário perderam o altar e viraram esconderijos.
- Fora: dois altares em clareiras (Menires ao norte, Carvalho Oco ao sul), santuários com pistas, ervas-noturnas,
  o cemitério com a Carpideira, a cabana do Ermitão e a Encruzilhada do Mercador sem Rosto.
- Luz: trilhas e clareiras pegam luar (Caçadores ficam sãos); a mata fechada é breu (bom para Cultistas).
- Fora das trilhas principais a mata é fechada: árvores e espinheiros formam paredes, e trilhas estreitas e escuras
  levam às tarefas, às ruínas e de volta. Espinheiros bloqueiam passagem e visão.
- A catedral por dentro: abóbadas com nervuras, pilastras, arcada no Claustro, estandartes, lustres, velas, a cor dos
  vitrais no chão, tapetes, hera e corvos que levantam voo quando alguém se aproxima (tudo visual; a colisão é a mesma).
- A mata é antiga e sombria, no espírito do Shaded Woods: árvores enormes cobertas de musgo, copa fechada, ruínas,
  arcos de pedra sobre as trilhas, estátuas de fiéis petrificados e névoa rasteira. Dentro da mata a névoa corta a
  visão: os bots não enxergam além de 26 m (`CFG.nevoaAlcance`).

## Classes e investigação

- Caçadores: Soldado (mais munição), Exorcista (sela mais rápido) e Rastreador (vê pegadas frescas dos Cultistas por
  20 s e, ao chegar às marcas de arrasto de uma Transferência, descobre o altar de destino).
- Cada ferramenta responde uma pergunta: o sinal dos santuários diz onde o ritual NÃO está (descarta um altar não
  escolhido), o Sensor diz se há algo por perto, o EVP diz se um altar foi consagrado, a Lanterna diz se é verdadeiro
  ou Chamariz, e as pegadas dizem por onde o culto passou.
- A Transferência deixa marcas de arrasto no altar de origem por 3 min e um lamento audível nos dois altares.
- O Véu não deixa invisível: o Cultista vira uma silhueta tênue, que some a mais de 8 m e reaparece sob a lanterna.
- Momentos da noite: Crepúsculo até 3:00, Vigília até 10:00, Hora Morta até 18:00. Selar sob fogo cai para 70%
  (um atacante) e 45% (dois). Executar leva 4,5 s e reanimar 5 s.

A floresta é gerada com semente fixa, então é igual para todos. Os papéis trocam a cada rodada e cada papel
sempre nasce do mesmo lado, então as duas equipes vivem o mesmo mapa.

## Celular e aparelhos fracos

O jogo escolhe a qualidade pelo aparelho na primeira vez (celular: Mínimo; poucos núcleos ou pouca memória: Médio
ou Baixo) e dá para trocar nos Ajustes.

| Nível  | Luzes pontuais | Sombras | Brilho difuso | Pós-processamento | Resolução máx. | Distância de visão |
|--------|---------------:|---------|---------------|-------------------|---------------:|-------------------:|
| Alto   | 34 | sim (2048) | sim | sim | 1,5× | 220 m |
| Médio  | 16 | sim (1024) | não | sim | 1,25× | 160 m |
| Baixo  | 12 | não | não | sim | 1× | 130 m |
| Mínimo | 8  | não | não | não | 0,75× | 110 m |

Baixo e Mínimo também usam texturas com metade do tamanho, tiram os mapas de relevo, desenham menos poeira e
árvores de fundo e encurtam o alcance da vegetação. A névoa já apaga tudo além de ~90 m, então cortar a distância
de visão não muda o que se vê. A resolução adaptativa (ligada por padrão) baixa a resolução interna até a metade
quando o jogo cai abaixo de 28 quadros por segundo e sobe de novo quando sobra folga.

Em telas de toque aparecem os controles: analógico onde o polegar esquerdo encostar (empurrar até o fim corre),
arrastar à direita para olhar, e botões para atacar (arrastar sem soltar também mira), usar, habilidade, recarregar,
lanterna, sensor, flare, transferir, correr, mapa e pausa. O jogo pede tela cheia e orientação deitada. Os
controles podem ser ligados ou desligados nos Ajustes.

    node tools/celular.js   # celular emulado (844 × 390, toque): qualidade, analógico, olhar, atacar, pausa

## Ajustes

Botão "Ajustes" no menu inicial e na tela de pausa: sensibilidade, volume, campo de visão, qualidade,
movimento bruto do mouse e remapeamento de todas as teclas e botões do mouse. Fica salvo no navegador.

## Ler os registros das partidas

    node tools/analisar.js              # lê ./logs e compara com as metas do documento v2
    node tools/analisar.js logs --csv metricas.csv
    node tools/simular.js 40            # gera 40 partidas só de bots, para testar mudanças sem jogar
    node tools/varredura.js 16          # compara variações de parâmetros lado a lado

## Testes

    node tools/testar.js 6              # grafo de navegação, caminhada real até 77 pontos, partidas com invariantes
    node tools/protocolo.js             # servidor: sala, partida, lixo, reconexão (precisa de npm install em server/)
    node tools/build.js                 # gera dist/ritual-reversal.html, a versão de arquivo único publicada
    node tools/navegador.js             # abre o arquivo único no Chromium (Playwright), fotografa o mapa e roda uma partida

O modo solo publicado no claude.ai também baixa o registro pelo botão na tela final; jogue o arquivo
na pasta `logs/` e rode o analisador junto com os das partidas online.

## Estrutura

- `shared/sim.js`: regras, mapa, bots, classes, registro de eventos (navegador + Node)
- `server/server.js`: salas, lobby, simulação a 30 Hz, snapshots a 20 Hz por equipe
- `client/index.html`: renderização Three.js, HUD, áudio, predição e interpolação
- `tools/`: analisador de registros, gerador de partidas de bot, varredura de parâmetros, testes e build
- `logs/`: um JSON por partida, com todos os eventos e as estatísticas por rodada

## Reconexão

Se a conexão cair no meio da partida, um bot assume seu personagem e o jogo tenta voltar sozinho por até
2 minutos. Ao voltar, você retoma o mesmo personagem, com vida, reagentes e posição. Se fechar a aba ou
apertar F5, a tela inicial oferece "Voltar à sala" enquanto a vaga estiver guardada.

## Limitações conhecidas

- Posições de todos os jogadores chegam a todos os clientes (dá para fazer wallhack pelo console).
  Os altares escolhidos e os altares consagrados ainda não descobertos ficam escondidos dos Caçadores.
- Só entra gente no lobby; não dá para entrar no meio da partida.
