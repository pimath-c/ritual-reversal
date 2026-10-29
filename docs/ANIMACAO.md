# Animação da primeira pessoa

Tudo fica em `client/arma.js` (embutido no arquivo único por `tools/build.js`, no app por `tools/build-mobile.js` e servido
pelo servidor). O jogo chama `RR_ARMA({...})` em `buildViewmodels` e recebe a API (`ARMA`).

## O que existe

| Peça | Como é feita |
| --- | --- |
| Mãos | Esqueleto de 16 ossos (pulso, três falanges por dedo, polegar com três) e uma malha com pele rígida por falange: uma chamada de desenho por mão. Dedos posados por oito números (`DEDOS`: indicador, médio, anelar, mínimo, polegar aberto, oposto, dobrado, abertura). |
| Braços | IK de dois ossos (lei dos cossenos) do ombro ao punho, cotovelo no plano do vetor de polo. Mangas afinam para o punho. |
| Carabina | Alavanca (pivô), cão, ferrolho que recua com a alavanca, portinhola de carga com mola, cartucho na câmara (aparece com a alavanca aberta), cápsula ejetada. |
| Revólver | Oito câmaras (antes seis, com oito tiros no jogo), guindaste que gira o tambor para fora, extrator (vareta e estrela) que empurra as cápsulas, municiador de oito, cão, cruz do cabo que brilha. |
| Cetro | Vértebras com material próprio (acendem em sequência), brasa, runas em órbita que nascem da brasa quando o fervor volta. |
| Luz de leitura | Os materiais da primeira pessoa ganham uma luz própria no espaço da câmera e um contorno frio (injetados no shader). A noite continua escura; a arma continua legível. Nada a mais para o resto do mundo. |

## Clipes

Um clipe é escrito à mão em `montarClipes*` com trilhas de chaves `[tempo, valor, curva]`:

- `a`: a arma (posição e rotação XYZ, relativas à pose pronta).
- `D` e `E`: as mãos. Cada chave diz em que **quadro** a mão está (`corpo`, `arma`, `alavanca`, `guindaste`, `tambor`), onde
  fica o centro da palma, para onde apontam os dedos esticados e para onde olha o dorso, e os dedos (`'pinca'`, `'coronha'`…).
  Mão no quadro da alavanca desce com a alavanca; mão no quadro da arma acompanha o coice. Entre chaves de quadros
  diferentes a mão viaja no corpo. `{reta:1}` faz o trecho em linha reta (empurrar um cartucho).
- `c`: peças (`alav`, `port`, `guin`, `ext`, `giro`, `cao`, `cruz`, `acende`, `brasa`, `orbita`).
- `pr`: objetos de cena (cartuchos `c0`/`c1` na mão ou na portinhola, `mun` na mão ou no eixo do tambor, `cam` com o
  conteúdo das câmaras). Eles trocam de pai no instante da chave, na mesma posição: nada pula.
- `cam`: empurrão de câmera. `ev`: eventos no tempo (`'som'`, `'cam'`, `'ejetar'`, `'soltar'`, `'camara'`).

Curvas (`CURVA`): `l` linear, `s` suave, `w` suave forte (pose a pose), `o` sai rápido e assenta, `q` estalo, `i` antecipação
(entra devagar e termina rápido), `b` passa do ponto e volta, `h` degrau. As posições usam Catmull-Rom entre chaves (arcos).

Helpers: `chave(t, POSE, {p,d,c,dd,e,f,reta})` reaproveita uma pose de referência (`P.cD`, `P.rE`…); `maoPorPinca` e
`maoPorPonto` põem a mão de modo que o ponto de pinça caia no objeto; `qNoQuadro` converte uma orientação pensada no corpo
para o quadro de uma peça num instante do clipe (a mão do municiador vem pela direita sem cobrir o tambor).

## Camadas (por cima do clipe)

1. Coice e ciclo de cada arma (`PERFIL_ARMA` em `index.html`): carabina abre a alavanca e a mão vai junto; revólver gira o
   tambor 1/8 e o polegar arma o cão; cetro recolhe as runas.
2. Respiração e oscilação lentas; tremor dos dedos ao selar; balanço da corrida no ritmo do passo.
3. Corpo: balanço do passo, atraso do mouse e do começo do passo (vindo do jogo), tranco de golpe (mola).
4. Brilho de acerto confirmado (prata do revólver, vértebras do cetro).

## Estados

`pronta`, `sacar`, `recarga`, `inspecao`, `ajuste` (pegada no ocioso), `gesto` (habilidade), `corrida`, `selar`,
`abaixada` (segurar E), `canalizar` (culto no círculo). Trocas misturam a pose anterior com a nova (0,07 a 0,22 s).
Atirar interrompe sacar, inspeção, ajuste e corrida na hora (o tiro é do servidor; a animação nunca atrasa um disparo).
A recarga começa quando o servidor diz (`reloadT` sobe), dura exatamente o que o servidor diz e termina quando ele termina.
Inspeção: tecla I (Ajustes, Controles) ou sozinha depois de ~30 s parado e fora de combate; qualquer tiro, golpe ou
movimento de combate a corta.

## Vitrine

- No jogo: F9 numa partida local, ou abra o jogo com `#vitrine`. Escolhe arma, clipe, velocidade (1× a 1/10×), vista
  (jogador, lado, cima, frente, direita, peça), luz (do jogo ou estúdio) e arrasta a linha do tempo.
- Folhas de quadros: `THREE_DIR=... NODE_PATH=$(npm root -g) node tools/vitrine.js [pasta] [filtro]` (`ESTUDIO=1`,
  `QUADROS=12`, `VISTAS=jogador,peca`). A vista `peca` segue a portinhola ou o tambor.
