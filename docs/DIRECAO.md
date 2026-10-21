# Ritual Reversal — avaliação de direção (v6)

Escrita antes das mudanças da v6, para decidir o que fazer primeiro. As fontes são o código, o documento de design v2,
o único registro de playtest humano (v5.8, uma noite, o dono como Caçador contra bots no Normal) e 400 noites de bots
com semente fixa. Um registro humano é indício, não conclusão: tudo que depende de gente jogando está marcado assim.

## O que já funciona
- **A disputa no círculo.** Selo que desacelera sob fogo, checkpoints, eco do setor, ataque que pausa a canalização e
  dano que recomeça a execução geram os lances que o documento de design pede ("selo no último instante", "defendeu até
  a Fenda"). Nas noites de bots, 58% dos rituais são selados e 16% perdidos com a caça já no círculo.
- **A dedução por sinais com origem.** Chamariz, purga, lanterna e sinais dos santuários: todo falso positivo é uma
  jogada do adversário, nunca sorteio. No playtest, os dois Chamarizes foram desmascarados pela lanterna.
- **Integridade.** O servidor decide tudo, o snapshot esconde o que a equipe não vê, e o C# do Unity confere bit a bit.

## Problemas, em ordem de impacto no jogador

1. **A cobertura baixa não protege** (código). Tiros e sigilos só param em peças altas (`raioParede` e os projéteis
   olham `TALL`). Túmulos, muretas, bancos, pedras, raízes e o próprio altar parecem cobertura e não são: o jogador se
   abaixa atrás de um túmulo e leva o tiro do mesmo jeito. É o pior tipo de defeito: o mapa mente.
2. **Não existe aproximação silenciosa** (código). Andar sempre faz passos audíveis a 16 m; correr, a 24 m. Chegar
   quieto e chegar rápido não são escolhas diferentes, então o som informa a posição de todos o tempo todo e nenhum lado
   pode emboscar, enganar ou fugir pelo barulho. Isso mata metade do jogo de dedução (diretriz 4: "aproximar em
   silêncio é diferente de correr?" Hoje, não).
3. **O mapa antigo não tinha regiões** (planta medida). A catedral era um retângulo de 192 × 116 m no meio da mata,
   cercada por todos os lados, sem silhueta de catedral; os dez reagentes ficavam num único pátio; a floresta era
   caminho entre portas. Refeito na v6 (ver abaixo), com medição antes e depois.
4. **Bots fracos como adversários** (indício, um registro). O dono derrubou os Cultistas 8 vezes e caiu nenhuma; o
   culto não ganhou a noite. Jogar sozinho é hoje o modo mais jogado (ainda não houve partida com quatro pessoas).
   Contra bots, o Caçador humano não tem medo, e sem medo não há terror.
5. **Tempo morto entre rituais** (registro humano): três trechos de 80 a 90 s em que só houve coleta e compra
   (2:44–4:05, 5:21–6:51, 7:29–9:00). Depois de um ritual resolvido, o culto refaz o estoque e a caça espera. Com
   reagentes num lugar só e som de passo sempre ligado, esse intervalo não tinha jogo: nem caçada, nem esconde-esconde.
6. **Toda ação é "segure E e olhe a barra"** (código). Coletar reagente, consagrar, purgar, queimar e reanimar têm a
   mesma linguagem; o reagente é um frasco sobre um toco. Funciona, mas nada é tátil nem memorável.
7. **A queda e a execução não têm momento** (código). Derrubar alguém mostra um marcador e um som; executar é uma
   barra de 4,5 s. É o auge de um confronto e passa como qualquer outra interação.
8. **Inspeções curtas e parecidas, sem acabamento de material** (v5.9). As três armas têm boa mecânica (recargas
   coreografadas), mas a inspeção é uma volta com um toque na peça, e as armas têm um material só, sem variações.

## O que não vamos fazer (e por quê)
- **Pular, escalar e quedas.** A simulação é plana (2D com alturas de peça); pulo pediria física vertical, colisão
  por altura e o porte inteiro no C#, sem uma decisão de jogo clara que o justifique agora.
- **Progressão, desbloqueios, loja.** As diretrizes pedem replay intrínseco primeiro. As skins da v6 são livres para
  escolher, sem raridade cobrada nem sorteio.
- **Sustos aleatórios e perigos de ambiente.** Nada que tire a agência do jogador.

## Decisões da v6, cada uma com o problema que resolve
| Mudança | Resolve | Como medir | Efeito no outro lado |
| --- | --- | --- | --- |
| Cobertura baixa bloqueia tiros e sigilos abaixo da altura dela | 1 | teste de regra + noites de bots | protege os dois lados igual |
| Andar agachado: metade da velocidade, passos só a 5 m, alvo mais baixo | 2, 5 | teste de regra + noites de bots | contra-jogo: lento, a luz ainda revela, a lanterna e o Sal pegam |
| Mapa em duas regiões com três travessias e reagentes na mata | 3, 5 | `tools/fluxo.js` (distâncias, exposição, invariantes) + 400 noites | travessias são caras para os dois lados |
| Acústica por lugar (eco de pedra na nave, mata abafada) | 2 | ouvir (não verificado aqui: sem áudio) | informação igual para os dois |
| Eliminação com peso, reagente físico, ritual por estados, inspeções e skins | 6, 7, 8 | folhas de quadros + jogo no navegador | só visual e som, servidor decide |

## Medições do mapa (antes → depois)
Preenchido com `node tools/fluxo.js` e `exp.js` (400 noites de bots, sementes 1000–1399); ver `notas/v6.md`.
