# Skins das armas (v6)

Tudo em `client/skins.js` (fábrica `RR_SKINS`, criada em `buildViewmodels` de `client/index.html` e passada a
`client/arma.js`). Só aparência: nenhuma skin muda dano, cadência, alcance, som de posição ou a silhueta da arma. A brasa do
cetro que os outros veem continua com a cor do culto.

## Como uma skin é feita
Uma skin é um registro de dados em `SKINS[arma]`:

```js
{id:'tempera', nome:'Têmpera de Forja', oficio:'mestre', texto:'…',
 papeis:{ caixa:{cor:0x6a6e78, rug:.24, met:1, det:'tempera', esc:9, mancha:1, manchaCor:0x2a3a78, inc:.85, incCor:0xc89a52, incMet:1, incRug:.28},
          madeira:{cor:0x7a4020, rug:.42, det:'xadrez', esc:11, sulco:.55, bump:.35} },
 acessorios:['selo'],                         // opcional: malhas presas à arma (só na primeira pessoa)
 insp:{revela:{cor:0xfff0c8, forca:1.5}, som:'bencao', cruz:[1,.78,.3], gotas:true} }   // opcional: ganchos da inspeção
```

- **Papéis**: cada material das armas é marcado com um papel quando é criado (`papel(m,'caixa')` em `index.html`,
  `userData.papel` em `arma.js`). Carabina: `caixa` (caixa, alavanca, cão, portinhola, ferrolho), `aco` (cano e tubo),
  `latao`, `madeira`. Revólver: `prata`, `escuro`, `madeira`, `cruz`. Cetro: `osso` (inclui as vértebras), `ferro`, `brasa`.
  Papel sem entrada na skin volta ao material original (guardado na primeira troca), então a skin de serviço é `papeis:{}`.
- **Acabamento por papel**: `cor`, `rug` (aspereza), `met` (metal) e um detalhe `det` com escala `esc` (repetições por
  metro). O detalhe é uma textura gerada no código (512 px, repete sem emenda) projetada em três planos no espaço da peça
  (não depende das UVs dos modelos):
  - canal R: **sulco** (`sulco`, força; `bump`, relevo pela derivada da altura): o fundo fica mais escuro e áspero;
  - canal G: **incrustação** (`inc`, `incCor`, `incMet`, `incRug`): outro material dentro do desenho (prata batida, osso,
    verdete, a cor de palha da têmpera);
  - canal B: **mancha** (`mancha`, `manchaCor`): o pontilhado do fundo da gravura, as manchas de fogo, os séculos no osso.
  Texturas que existem: `gravura` (folhagem com espirais, fundo pontilhado), `tempera`, `xadrez`, `cruzes`, `veio`
  (marfim), `rachado`, `crosta`. Uma nova é uma função em `GERA` que desenha os três canais.
- **Ofício** é a raridade expressa como trabalho: `oficio` (de serviço), `mestre` (o trabalho de um armeiro), `reliquia`
  (peça única, com acessório e reação própria na inspeção). Nada é cobrado nem sorteado.
- **Acessórios** (`ACESS`): selo de cera com fita, granadas, arame de cobre, amuletos, cracas. Os pendentes (`pendente`)
  procuram a gravidade no quadro do pai com mola e amortecimento: balançam quando a arma vira.
- **Ganchos de inspeção**: os clipes de `arma.js` marcam eventos `['skin','revela']`, `['skin','gotas']`,
  `['skin','respira']`. A skin decide o efeito: `revela` faz um brilho correr pela peça (mais forte nos sulcos e na
  incrustação), `cruz` é a cor do esmalte que acende quando o polegar traça a cruz do revólver, `gotas` pinga água do cetro,
  `som` toca junto da revelação.

## Onde aparece
- **Primeira pessoa**: tudo (detalhe no shader, acessórios, ganchos). O shader é encadeado com a luz de leitura dos
  materiais da arma; trocar de skin só muda valores (nenhuma recompilação depois da primeira).
- **Terceira pessoa**: cor, aspereza e metal (sem o detalhe nem os acessórios, que seriam poucos pixels). A escolha de
  cada jogador vai no `hello` (`vis`), o servidor só deixa passar nomes curtos em minúsculas (`limparVis`), o lobby a
  repassa (`players[].vis`) e o cliente confere contra o catálogo (`SKINS.limpar`). Bots usam uma por nome (`visDeBot`).
- **Arsenal** (menu): a partida local parada na nave, sem HUD, com a arma da primeira pessoa; troca de arma e de skin,
  inspeção, recarga, sacar e sigilo; vista do jogador, de lado ou de perto; luz da noite ou de estúdio (a luz de leitura
  mais forte, para não mexer nas luzes do mapa).
- Escolha salva em `localStorage['rr-skins']`.

## Conferir
- `node tools/vitrine.js pasta "skin-"`: folhas de quadros de cada skin (jogador, lado, perto) e das inspeções das
  relíquias. `ESTUDIO=1` usa a luz de estúdio.
- `tools/navegador.js`: abre o Arsenal pelo menu, troca skins da carabina e do revólver e confere que a escolha ficou e
  que a arma inspecionou.
- `tools/protocolo.js`: a skin chega aos outros pelo lobby e lixo é descartado.
