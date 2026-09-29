# Ritual Reversal — passagem para outra engine

Este documento é para quem for refazer o jogo em Unreal, Godot ou outra engine. A regra de ouro:
**`shared/sim.js` é a especificação executável.** Tudo que decide o jogo (quem acertou, quem selou, qual altar
é verdadeiro, o que cada equipe sabe) está lá, sem gráfico nem rede. A versão nova deve reproduzir esse
comportamento; os testes de `tools/` dizem se reproduziu.

Não é para portar o código linha a linha. É para portar as regras e manter os números, e então gastar o
esforço da engine nova no que o navegador não faz bem: modelos, animação, luz, som, efeitos e a sensação das armas.

## 1. Como o protótipo está organizado

| Protótipo | Papel | Unreal 5 | Godot 4 | Unity |
|---|---|---|--- | --- |
| `shared/sim.js` `createGame`, `step`, `playStep` | estado e regra da partida, 30 passos por segundo | `AGameMode` (só no servidor) + `AGameState` replicado | nó autoritativo no servidor + `MultiplayerSynchronizer` | `GameManager` só no servidor + estado replicado (Netcode for GameObjects: `NetworkBehaviour` e `NetworkVariable`) |
| `g.actors` / `makeActor` | jogador ou bot, com vida, sanidade, reagentes, recargas | `ACharacter` + `APlayerState` | `CharacterBody3D` + recurso de estado | `CharacterController` + `NetworkObject` por jogador |
| `useAbility`, `FEITICOS` | feitiços com recarga | Gameplay Ability System (uma `GameplayAbility` por feitiço, recarga como `GameplayEffect`) | nós de habilidade com `Timer` | `ScriptableObject` por feitiço (dados) + componente de habilidade com recarga |
| `traceShot` + `posAt` (volta no tempo até 0,5 s) | tiro instantâneo com compensação de atraso | line trace no servidor com rewind do histórico de posições | `PhysicsRayQueryParameters3D` + histórico próprio | `Physics.Raycast` no servidor com histórico de posições para voltar no tempo |
| `castSigil`, `updateProjectiles` | sigilo dos Cultistas (projétil) | `AActor` com `UProjectileMovementComponent` | `Area3D`/`RigidBody3D` movido pelo servidor | `Rigidbody` ou projétil movido pelo servidor (`NetworkTransform`) |
| `contexts`, `holdStep`, `doAction` | "segure E para…": consagrar, iniciar, selar, purgar, reanimar, executar | componente de interação + barra de progresso replicada | `Area3D` + estado de interação | componente de interação com `Physics.OverlapSphere` + barra de progresso |
| `snapshot(g, role)` | o que cada equipe recebe | replicação por equipe: `IsNetRelevantFor` e condições de replicação por dono/equipe | filtros de visibilidade do `MultiplayerSynchronizer` por par | `NetworkObject.CheckObjectVisibility` / `NetworkShow`-`NetworkHide` por cliente |
| `NAV`, `findPath`, `aEstrela` | navegação dos bots em grade de 2 m | NavMesh + `AAIController` com Behavior Tree ou StateTree | `NavigationRegion3D` + `NavigationAgent3D` | NavMesh (pacote AI Navigation) + `NavMeshAgent` |
| `aiHunter`, `aiCult`, `botNoite` | cérebro dos bots | Behavior Tree/StateTree por papel | máquina de estados em script | máquina de estados em C# (ou Behavior do Unity) |
| `client/index.html` | tudo que é visual e sonoro | Lumen, Niagara, MetaSounds, UMG | ambiente, partículas GPU, `AudioStreamPlayer3D`, UI em `Control` | URP (celular) ou HDRP (PC), VFX Graph/partículas, `AudioSource` 3D, UI Toolkit ou uGUI |

## 2. A partida

- **Uma noite de 18 minutos** (`CFG.roundTime` 1080 s), em três momentos:
  Crepúsculo até 3:00, Vigília até 10:00, Hora Morta até 18:00 (`CFG.momentos`).
- **Fases**: `pick` (Cultistas escolhem 3 de 6 altares) → `intro` (cada um escolhe classe e feitiço) →
  `play` → `summary` → `final`. Com `CFG.noites` 2 há uma segunda rodada com os papéis trocados.
- **Rituais**: no máximo 3 por noite, um de cada vez (`bloqueioRitual`). O primeiro só a partir da Vigília;
  com 2 rituais já resolvidos, o terceiro (o Grande Ritual) só na Hora Morta.
- **Vitória** (uma noite): Cultistas vencem com 2 rituais completos (Fendas). Caçadores vencem selando 2
  (Faróis) ou chegando ao amanhecer. `CFG.formatoNoite` 'placar' deixa a noite correr até o fim;
  'melhorDeTres' encerra num 2 a 0.

### Estados de um altar

```
dormente ──consagrar (1 reagente, 3 s)──▶ desperto ──iniciar (1 reagente, 1,2 s)──▶ ativo ──▶ Fenda (ritual completo)
    │                                        │                                        └──▶ Farol (selado)
    └──Chamariz (2 s, altar não escolhido)──▶ desperto falso (decoy)
Caçador purga um desperto (6 s): volta a dormente. Transferência (12 s, 1 reagente, uma por noite):
um altar escolhido ainda dormente passa a escolha para outro, deixando marcas de arrasto.
```

### O ritual ativo (`updateAltars`)

- Duração base: 45 s com um canalizador, 34 s com dois (`CFG.rit`). Máximo de 2 canalizadores.
- Canalizador atingido há menos de 1 s, cego ou atordoado rende metade. Runa: +20%.
- Hora Morta: +25%. Grande Ritual: 0,6 da velocidade (mais longo, e todos sabem onde).
- Maré Profana: +5% por minuto sem ritual resolvido, até +20% (`mareFactor`).
- Checkpoints em 25% e 75%: com o círculo vazio o progresso recua até o último checkpoint.
- **Localização**: o ritual é revelado aos Caçadores quando passa de 55% (um canalizador) ou 50% (dois), ou antes
  se um Caçador chegar perto enquanto alguém canaliza (14 m ou 24 m, maior com Runa e na Hora Morta).
  O Grande Ritual é revelado na hora.
- **Selamento**: 8 s parado no círculo (6,5 s com o Exorcista); dois selando ×1,35; nunca menos de 5 s.
  Sob fogo: 70% com um atacante, 45% com dois. Sair por mais de 3 s faz o selo cair.

## 3. O que cada equipe sabe (a mecânica central)

O Caçador não vê onde estão os altares consagrados; ele vê o que já descobriu. O estado fica em `g.know` por altar:

| Valor | Significa | Como se chega |
|---|---|---|
| `?` | nada se sabe | início |
| `limpo` | não está consagrado agora | passar a menos de 10 m de um dormente; sinal de santuário (altar não escolhido) |
| `desperto` | consagrado (pode ser Chamariz) | EVP a menos de 10 m; sentinela acesa a 24 m; Rastreador nas marcas de arrasto |
| `confirmado` | verdadeiro | Lanterna apontada a menos de 16 m |
| `chamariz` | falso | Lanterna apontada num Chamariz |
| `ativo` | ritual em andamento e revelado | localização (acima) |

`snapshot(g,'H')` mascara como dormente todo altar desperto que o Caçador ainda não conhece, e **não envia a
posição de inimigos** que ninguém da equipe vê: linha de visão (paredes, árvores e espinheiros cortam) até
75 m (`CFG.visaoRede`), com 0,6 s de tolerância; o Sal revela por 6 s. Na engine nova, isso é relevância de
rede por equipe; não confie no cliente para esconder.

Cada ferramenta responde uma pergunta diferente:

| Ferramenta | Pergunta | Números |
|---|---|---|
| Sinal nos santuários (tarefa do Crepúsculo) | onde o ritual **não** está | descarta um altar não escolhido |
| Sensor Áurico (Q) | há algo por perto? | raio 58 m, recarga 15 s |
| EVP | este altar foi consagrado? | 10 m |
| Lanterna (F) | é verdadeiro ou Chamariz? | cone de 16 m, gasta carga |
| Sentinelas | alguém consagrou/iniciou aqui perto? | 24 m, avisa a equipe |
| Pegadas (Rastreador) | por onde o culto passou? | 20 s de vida |
| Marcas de arrasto | houve Transferência daqui? (o Rastreador vê o destino) | 180 s, a 9 m |
| Sal | quem está pisando aqui? | 60 s no chão, revela 6 s |

## 4. Classes, feitiços e combate

| Classe | Equipe | Vida | Vel. | Passiva |
|---|---|---|---|---|
| Soldado | Caçador | 170 | 4,6 | mais munição de reserva (64) |
| Exorcista | Caçador | 160 | 4,6 | sela em 6,5 s |
| Rastreador | Caçador | 155 | 4,8 | pegadas e destino da Transferência |
| Ritualista | Cultista | 150 | 4,6 | escudo de 15 para quem canaliza junto nos checkpoints |
| Guardião Profano | Cultista | 200 | 4,1 | mais vida |

Feitiços (um por jogador): Runa (20 s), Empurrão (12 s), Véu (24 s, silhueta por 5 s, some a mais de 8 m);
Flash (18 s, cega 1,5 s num cone de 14 m), Purificação (25 s, atordoa 1 s a 6 m e desfaz Runa/Véu),
Sal (16 s).

- **Caçadores**: tiro instantâneo, 14 no corpo e 26 na cabeça, queda de dano de 26 m a 55 m até 75%;
  8 balas, recarga 1,6 s, cadência 0,42 s.
- **Cultistas**: sigilo, projétil a 48 m/s, 17 de dano, gasta 14 de Fervor; o Fervor volta 8/s depois de 1,8 s sem atirar.
- **Caído**: 20 s sangrando; um aliado reanima em 5 s (uma vez por ciclo), o inimigo executa em 4,5 s.
  Renascimento: 8/14/22 s por momento, mais 2 s por morte seguida (até +6).
- **Sanidade**: luz sustenta os Caçadores e escuro sustenta os Cultistas; abaixo de 15 o jogador entra em pânico e se denuncia.

Todos os números estão em `CFG`, `CLASSES` e `FEITICOS` no topo de `shared/sim.js`. Mude lá primeiro,
meça com os bots e só então leve para a engine.

## 5. O mapa

`node tools/exportar-mapa.js` gera `docs/mapa.json` a partir da mesma semente do jogo:

- `pecas`: 2.117 caixas de colisão (`tipo`, `x1..x2`, `z1..z2`, `h`; `tall` bloqueia visão e tiro).
  O `tipo` diz qual peça de arte colocar ali (muro, coluna, espinheiro, lápide, menir, estátua...).
- `arvores` (880, com raio de tronco igual ao da colisão), `vegetacaoRasteira` (só visual), `trilhas`,
  `clareiras`, `arcos`, `luzes`, `altares`, `spawns`, `reagentes`, `mercadores`, `pontosDeTarefa`, `portais`, `lugares`.
- Metros; chão no plano x-z. **Unity**: X = x, Y = y, **Z = −z** (o Unity usa mão esquerda; sem inverter o z o mapa
  sai espelhado) e giros em y com sinal trocado. **Unreal**: X = x·100, Y = z·100, Z = y·100 (a troca de eixos já
  converte a mão). **Godot**: x, y, z direto.
- **Unity pronto**: `unity/Assets/RitualReversal` tem o importador (menu Ritual Reversal > Importar mapa) e um
  jogador em primeira pessoa. Veja `unity/LEIAME.md`.
- O mapa atual é o compacto de 2v2 (310 × 216 m). A planta original de 350 × 260 m, pensada para mais
  jogadores, volta com `HALF = HALF0` em `shared/sim.js` (e a exportação acompanha).

Recomendação: importe as caixas como bloqueio (blockout) primeiro, jogue, e só depois substitua por arte.
A colisão tem que continuar idêntica à do protótipo até os testes de caminhada passarem na engine nova.

## 6. Rede

- Servidor autoritativo a 30 Hz; snapshots a 20 Hz por equipe; cliente prevê o próprio movimento e reconcilia.
- O tiro do Caçador é julgado no servidor voltando as posições até 0,5 s (`CFG.compensacaoMax`).
- Reconexão: a vaga fica guardada 2 minutos, com um bot jogando no lugar (`dropHuman`/`reclaimHuman`).
- Eventos (`g.events`) carregam o que o cliente precisa para som e efeitos; `evVisible` no cliente decide quem
  vê cada um. Na engine nova, prefira RPCs multicast filtrados por equipe no servidor.

## 7. Bots

Os bots servem para testar regras, não para substituir jogadores. Resumo do que fazem hoje:

- **Cultistas**: tarefas do Crepúsculo, compram reagente, consagram, esperam o parceiro e iniciam; um deles
  planta até dois Chamarizes; Transferência quando o altar escolhido foi descoberto ou a noite avançou.
- **Caçadores**: tarefas, patrulha pelos altares menos visitados, purgam o que descobrem, correm para o
  ritual revelado e selam; o Rastreador segue pegadas frescas.
- Números atuais (30 partidas só de bots): Cultistas vencem cerca de 77–81%. É limitação da IA dos bots.
  Equilíbrio de verdade só com playtest de pessoas.

## 8. 4v4

O protótipo é 2v2. Para 4v4 na engine nova:

- O protótipo assume 4 vagas, 2 por equipe, em alguns pontos: a sala do servidor (`newRoom`), o lobby do
  cliente (`slots.slice(0,2)`) e a posição de nascimento (`spawn`, dois lugares por lado). Fora isso, as
  regras trabalham com listas de atores. Também faltam classes por papel (`CLASS_BY_TEAM`) para 4 jogadores
  não repetirem classe.
- Mapa: começar da planta original (`HALF0`) e remedir as caminhadas.
- Rituais: `CFG.rit` só define 1 e 2 canalizadores; com 4 Cultistas é preciso decidir se o círculo aceita
  mais gente ou se o ritual tem limite de 2 (hoje o limite é 2).
- Selamento: `coSeal` vale para 2 ou mais; revisar para equipes maiores.
- Tempo: 18 minutos foram calibrados para 2v2; medir de novo.

## 9. Ordem sugerida para o porte

1. **Blockout jogável**: mapa por caixas vindas de `mapa.json`, movimento, tiro e colisão. Comparar tempos
   de caminhada com `tools/testar.js`.
2. **Regras**: altares, rituais, selamento, momentos da noite, condição de vitória, igual a `sim.js`.
3. **Conhecimento e rede por equipe**: `g.know`, altar mascarado, inimigo fora de vista não replicado.
4. **Bots mínimos** para testar sozinho.
5. **Sensação**: armas, câmera, som espacial, feedback de cada ação.
6. **Atmosfera**: catedral, floresta, névoa, luz, Grande Ritual (céu de sangue, vitrais, sino, coro e tambor que acelera).
7. **Arte final** por cima do blockout, sem mudar a colisão.

## 10. Como saber que o porte está certo

Os testes do protótipo viram critérios de aceite:

- `tools/testar.js`: todo ponto importante do mapa é alcançável dos dois spawns (154 caminhadas); partidas
  inteiras de bots sem ninguém dentro de parede, fora do mapa ou com valores inválidos; o snapshot não vaza
  inimigo fora de vista.
- `tools/protocolo.js`: servidor e clientes falsos (lobby, partida, reconexão, 20 snapshots/s).
- `tools/analisar.js` e `tools/simular.js`: métricas de partida (chegadas ao círculo, selamento sob fogo,
  rituais completos) para comparar a engine nova com o protótipo nas mesmas condições.
