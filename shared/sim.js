/* Ritual Reversal — simulação compartilhada (navegador e servidor).
   Sem THREE, sem DOM, sem áudio: só regras, mapa, bots e registro de eventos. */
(function(root,factory){ if(typeof module==='object'&&module.exports) module.exports=factory(); else root.Sim=factory(); })(typeof self!=='undefined'?self:this,function(){
'use strict';
const clamp=(v,a,b)=>Math.max(a,Math.min(b,v));
const lerp=(a,b,t)=>a+(b-a)*t;
const rand=(a,b)=>a+Math.random()*(b-a);
const dist=(a,b)=>Math.hypot(a.x-b.x,a.z-b.z);
const r2=v=>Math.round(v*100)/100;

// ============ CONFIG (v2 escalada para 104 × 72 m e 2v2) ============
const CFG={
  roundTime:1080, noites:1, obolosInicio:3, respawnMomento:[8,14,22],
  formatoNoite:'melhorDeTres', // 'melhorDeTres': quem leva dois rituais vence e a noite acaba ali; 'placar': a noite sempre passa pelos três momentos
  momentos:[{id:'crepusculo',nome:'Crepúsculo',ate:180},{id:'vigilia',nome:'Vigília',ate:600},{id:'horamorta',nome:'Hora Morta',ate:1080}],
  rit:{1:{dur:45,rad:14,loc:0.55},2:{dur:34,rad:24,loc:0.5}},
  circleR:3, seal:8, sealExo:6.5, coSeal:1.35, fire1:0.7, fire2:0.45, sealMin:5, cps:[0.25,0.75],
  carry:2, collect:3.5, consecrate:3, start:1.2, purge:6, burn:6, decoy:2, transfer:12, revive:5, execute:4.5, downTime:20,
  sigilDano:17, sigilVel:48, sigilCusto:14, fervorRegen:8, fervorEspera:1.8, quedaDano:14, quedaPerto:26, quedaLonge:55, quedaMin:.75,
  compensacaoMax:.5, // quanto o servidor volta no tempo para julgar um tiro: ida e volta + 100 ms de interpolação, com folga
  regrow:40, sensorRaio:58, pegadaVida:20, visaoRede:75, /* além de ~75 m a névoa apaga qualquer vulto */ nevoaAlcance:26, respawn:[10,3,18], speed:4.6, sprint:6, botSpeed:3.9, spawnSafe:7,
  // v6: andar agachado. Metade da velocidade, sem corrida, passos que só se ouvem de perto e um alvo mais baixo (a cobertura
  // baixa passa a proteger quem se abaixa atrás dela). É a escolha entre chegar quieto e chegar rápido.
  agachadoVel:.5, agachadoAltura:1.15, agachadoOlho:.95, // abaixado atrás de um túmulo (1,1 m) fica coberto; para atirar, levanta
  timers:{pick:35,intro:25,summary:15},
  // v5.5: o coro ecoa o setor do ritual (oeste, centro ou leste) quando ele passa deste progresso, antes de revelar o altar
  ecoProg:0.25,
  // lances da noite (só reconhecimento, não mudam regra): Chamariz que prendeu a caça por 20 s, ritual defendido com o selo
  // passando de 50%, selo com o ritual em 85% ou mais, ritual achado antes do eco já em andamento (8%; quem estava parado
  // no altar quando ele começou não descobriu nada)
  iscaLance:20, defesaLance:.5, limiteLance:.85, achouLance:.08,
  // v5.5: quem lançou um sigilo há menos disto (s) não canaliza: o Cultista escolhe entre defender e celebrar.
  // 0,6 s calibrado em 200 noites de bots por valor (1 s: culto 38%; 0,8 s: 42%; 0,6 s: 46%)
  ataqueCanal:.6,
  // v5.5: levar dano reinicia a execução de um caído
  danoInterrompeExecucao:true,
  // v6.1 (bots do culto, do playtest de 24/10): canalizando, o bot revida quem o acertou há menos disto (s) ou quem está a
  // menos de botRevidaPerto m (antes só a 8 m: Caçadores humanos derrubavam o culto de longe sem levar um tiro).
  // 200 noites de bots: culto 30% → 37%. Testado e descartado: o Guardião sair do círculo para vigiar depois do eco (26%)
  botRevida:2.5, botRevidaPerto:20,
};
// setores do eco: cada setor tem dois altares (um no promontório e um na mata)
const SETORES=[{id:'oeste',nome:'a oeste',altares:[0,3]},{id:'centro',nome:'no centro',altares:[1,4]},{id:'leste',nome:'a leste',altares:[2,5]}];
const setorDoAltar=i=>SETORES.find(s=>s.altares.includes(i));
// Laboratório de playtest: variações que o anfitrião escolhe para comparar noites (o padrão é a regra normal).
// crepusculo: segundos até a Vigília; fogo: quanto o selo anda sob fogo de 1 e de 2 inimigos; execucao: segundos para
// executar um caído; economia: óbolos, tarefas pagas e mercadores; formato: como a noite termina.
const FOGO={atual:[CFG.fire1,CFG.fire2],a:[.8,.6],b:[.9,.7]};
const LAB_OPCOES={crepusculo:[180,120,90],fogo:['atual','a','b'],execucao:[4.5,6,7.5],economia:[true,false],formato:['melhorDeTres','placar']};
function labDe(o){ o=o||{}; const L={};
  for(const k in LAB_OPCOES){ const ops=LAB_OPCOES[k]; L[k]=ops.includes(o[k])?o[k]:(k==='formato'&&ops.includes(CFG.formatoNoite)?CFG.formatoNoite:ops[0]); }
  L.fire1=FOGO[L.fogo][0]; L.fire2=FOGO[L.fogo][1]; return L; }
const CLASSES={
  ritualista:{team:'C',name:'Ritualista',hp:150,speed:4.6,arma:'Cetro de vértebras',
    text:'Cetro de vértebras. Passiva: quem canaliza com você ganha escudo ao atingir 25% e 75% do ritual.'},
  guardiao:{team:'C',name:'Guardião Profano',hp:200,speed:4.1,arma:'Cetro de vértebras',
    text:'Cetro de vértebras. Passiva: 200 de vida e armadura de osso, mas mais lento.'},
  soldado:{team:'H',name:'Soldado',hp:170,speed:4.6,arma:'Carabina de alavanca',
    text:'Carabina de alavanca. Passiva: carrega mais munição de reserva.'},
  exorcista:{team:'H',name:'Exorcista',hp:160,speed:4.6,arma:'Revólver de prata',
    text:'Revólver de prata. Passiva: sela rituais mais depressa.'},
  rastreador:{team:'H',name:'Rastreador',hp:155,speed:4.8,arma:'Carabina de alavanca',
    text:'Carabina de alavanca. Passiva: enxerga pegadas frescas dos Cultistas e lê marcas de arrasto: sabe para onde a Transferência levou o ritual.'},
};
const FEITICOS={
  runa:{team:'C',nome:'Runa',cd:20,text:'Canalização 20% mais rápida por 8 s, mas o ritual fica audível de mais longe.'},
  empurrao:{team:'C',nome:'Empurrão',cd:12,text:'Arremessa quem está à sua frente e interrompe o selamento.'},
  veu:{team:'C',nome:'Véu das Sombras',cd:24,text:'Por 5 s você vira uma silhueta tênue: some a mais de 8 m e reaparece sob a lanterna. Atacar ou levar dano desfaz o véu.'},
  flash:{team:'H',nome:'Flash',cd:18,text:'Cega os inimigos no cone por 1,5 s. Cultista cego canaliza pela metade.'},
  purificacao:{team:'H',nome:'Purificação',cd:25,text:'Atordoa inimigos a 6 m por 1 s e desfaz Runa e Véu.'},
  sal:{team:'H',nome:'Círculo de Sal',cd:16,text:'Deixa sal no chão por 60 s. O primeiro Cultista que pisar fica lento e aparece para a sua equipe.'},
};
const FEIT_BY_TEAM={H:['flash','purificacao','sal'],C:['runa','empurrao','veu']};
const CLASS_BY_TEAM={H:['soldado','exorcista','rastreador'],C:['ritualista','guardiao']};

// ============ MAPA ============
// v6: duas regiões com identidade própria, ligadas por travessias que dão peso à chegada.
// Ao norte, o Promontório: um maciço de rocha cercado por um desfiladeiro em U. Nele, a catedral em cruz latina
// (fachada ao sul com duas torres desiguais, nártex, nave com naves laterais, cruzeiro, transepto, coro atrás de um jubé
// e abside na beira do penhasco), o Claustro, a Sala do Capítulo, a Sacristia, o Tesouro desabado, o Cemitério dos
// Monges, o Ossuário e as ruínas do mosteiro.
// A oeste, ao sul e a leste, a Floresta: trilhas de luar entre mata fechada e clareiras com função (Pedreira, Charco,
// Encruzilhada, Calvário, mirantes), dois altares antigos (Menires e Encapuzados), a Portaria na cabeça da ponte e todos
// os reagentes. Os dois lados nascem na mata: Caçadores a oeste, Cultistas a leste.
// Três travessias, cada uma com seu custo: a Ponte Grande no eixo (larga, exposta, chega na fachada), a Ponte Caída a
// oeste (um tronco gigante, sem cobertura, chega no mosteiro) e o Aqueduto a leste (entre muretas, chega no Ossuário).
// O desfiladeiro bloqueia a passagem, mas não a visão nem os tiros.
// Coordenadas: x para leste, z para o sul (o norte é z negativo). Metros. Sem espelhamento: os papéis trocam a cada
// rodada e cada papel nasce sempre do mesmo lado, então as duas equipes vivem o mesmo mapa.
const HALF={x:145,z:100};
const PROMONTORIO={x1:-82,x2:82,z1:-100,z2:-22};   // o maciço da catedral (dentro do desfiladeiro)
const DESFILADEIRO={x1:-98,x2:98,z1:-100,z2:-6};   // promontório + desfiladeiro: tudo que não é mata
const CATEDRAL={x1:-44,x2:44,z1:-97,z2:-32};       // caixa que envolve a cruz (para o cliente e a planta)
const CLAUSTRO={x1:-39,x2:-21,z1:-59,z2:-49};      // o jardim a céu aberto no meio do Claustro
// Zonas: a primeira que contém o ponto vale; fora de todas é Floresta. luar: quanto o céu ilumina ali (a mata calcula o
// seu pelas clareiras e trilhas); coberto: tem teto (não chove, passos ecoam).
const ZONAS=[
  {name:'Claustro',...CLAUSTRO,luar:.55},
  {name:'Catedral',x1:16,x2:44,z1:-78,z2:-64,luar:.45},                // braço leste do transepto: o teto desabou
  {name:'Catedral',x1:-16.5,x2:16.5,z1:-64,z2:-31.5,luar:0,coberto:true},  // nártex, nave e naves laterais
  {name:'Catedral',x1:-44.5,x2:16,z1:-78.5,z2:-64,luar:0,coberto:true},    // braço oeste e cruzeiro
  {name:'Catedral',x1:-12.5,x2:12.5,z1:-97,z2:-78,luar:0,coberto:true},    // coro e abside
  {name:'Catedral',x1:-44.5,x2:-16,z1:-64,z2:-43.5,luar:0,coberto:true},   // galerias do Claustro
  {name:'Catedral',x1:-70.5,x2:-51.5,z1:-64.5,z2:-45.5,luar:0,coberto:true}, // Sala do Capítulo
  {name:'Catedral',x1:-30.5,x2:-12,z1:-90.5,z2:-78,luar:0,coberto:true},   // Sacristia
  // a lua vem do sudeste: a catedral (24 m) deixa na sombra o mosteiro a noroeste e o ossuário ao norte do transepto, e a
  // passagem dos monges fica entre paredes altas. São as aproximações escuras dos altares do promontório.
  {name:'Planalto',x1:-82,x2:-44.5,z1:-100,z2:-64.5,luar:.12},          // ruínas do mosteiro
  {name:'Planalto',x1:-51.5,x2:-44.5,z1:-64.5,z2:-45.5,luar:.1},        // passagem dos monges
  {name:'Planalto',x1:44.5,x2:82,z1:-100,z2:-78,luar:.15},              // ossuário
  {name:'Planalto',x1:12.5,x2:44.5,z1:-90.5,z2:-78.5,luar:.12},         // Tesouro desabado, na sombra do transepto
  {name:'Planalto',x1:44.5,x2:51.5,z1:-78,z2:-64,luar:.1},              // beco entre o transepto e o ossuário
  {name:'Planalto',x1:-44.5,x2:44.5,z1:-100,z2:-90.5,luar:.08},         // atrás da abside, na beira do penhasco
  {name:'Planalto',...PROMONTORIO,luar:.42},
  {name:'Desfiladeiro',...DESFILADEIRO,luar:.5}];
const ZONES=[{name:'Claustro',...CLAUSTRO},{name:'Catedral',...CATEDRAL},{name:'Planalto',...PROMONTORIO},{name:'Floresta'}]; // compatibilidade
const zonaDe=(x,z)=>{ for(const Z of ZONAS) if(x>Z.x1&&x<Z.x2&&z>Z.z1&&z<Z.z2) return Z; return null; };
const zoneAt=(x,z)=>{ const Z=zonaDe(x,z); return Z?Z.name:'Floresta'; };
const naCatedral=(x,z)=>{ const n=zoneAt(x,z); return n==='Catedral'||n==='Claustro'; };
const coberto=(x,z)=>{ const Z=zonaDe(x,z); return !!(Z&&Z.coberto); };
const foraDaMata=(x,z,m)=>{ m=m||0; return x>DESFILADEIRO.x1-m&&x<DESFILADEIRO.x2+m&&z<DESFILADEIRO.z2+m; };

// Seis altares, dois por setor do eco (oeste, centro, leste): em cada setor, um no promontório e um na mata.
const ALTARS=[
  {name:'Sala do Capítulo',x:-61,z:-55},  // sala de pedra apertada, três portas, bancos de pedra: briga de perto
  {name:'Abside',x:0,z:-87},             // o altar-mor, no fundo do coro: atrás do jubé, colunata em meia-lua e deambulatório
  {name:'Capela Desabada',x:31,z:-71},   // braço leste do transepto sem teto: luar, escombros e uma brecha para o cemitério
  {name:'Menires',x:-82,z:64},           // anel de pedras numa clareira a sudoeste, três vãos, mata fechada em volta
  {name:'Portaria',x:14,z:9},             // pátio da portaria na cabeça da ponte, fora do eixo: as torres o escondem da fachada
  {name:'Encapuzados',x:82,z:66}];       // capela em ruínas a sudeste, vigiada por fiéis encapuzados de pedra
// nascem olhando para a torre do cruzeiro, do outro lado do desfiladeiro
const SPAWN={H:{x:-120,z:-24,yaw:-1.2},C:{x:120,z:-24,yaw:1.2}};
const LUGARES=[ // nomes que aparecem na planta
  {name:'Nave',x:0,z:-50},{name:'Cruzeiro',x:0,z:-71},{name:'Claustro',x:-30,z:-54},{name:'Sacristia',x:-21,z:-84},{name:'Tesouro',x:21,z:-84},
  {name:'Cemitério dos Monges',x:54,z:-48},{name:'Ossuário',x:63,z:-84},{name:'Mosteiro',x:-63,z:-84},{name:'Adro',x:0,z:-26},
  {name:'Ponte Grande',x:0,z:-14},{name:'Ponte Caída',x:-90,z:-82},{name:'Aqueduto',x:90,z:-82},
  {name:'Acampamento',x:-120,z:-10},{name:'Círculo de Pedras',x:120,z:-10},{name:'Cabana do Ermitão',x:-123,z:-70},{name:'Oratório',x:123,z:-70},
  {name:'Ruínas Sombrias',x:-120,z:-96},{name:'Carvoaria',x:120,z:-96},{name:'Pedreira',x:-46,z:20},{name:'Charco',x:46,z:24},
  {name:'Encruzilhada',x:-10,z:40},{name:'Carvalho Oco',x:14,z:70},{name:'Calvário',x:0,z:95},{name:'Mirante Oeste',x:-58,z:-1},{name:'Mirante Leste',x:58,z:-1}];

const WALLS=[];
function W(x1,x2,z1,z2,h,kind,extra){ const w={x1:Math.min(x1,x2),x2:Math.max(x1,x2),z1:Math.min(z1,z2),z2:Math.max(z1,z2),h:h||9,tall:(h||9)>2.2,kind:kind||'pedra'}; if(extra) Object.assign(w,extra); WALLS.push(w); return w; }
const PORTAS=[];
function muro(eixo,fixo,de,ate,vaos,h,kind){
  const t=1, pts=[de]; (vaos||[]).flat().forEach(v=>pts.push(v)); pts.push(ate);
  (vaos||[]).forEach(([a,b])=>{ const c=(a+b)/2; if(eixo==='x') PORTAS.push({x:c,z:fixo-2},{x:c,z:fixo+2}); else PORTAS.push({x:fixo-2,z:c},{x:fixo+2,z:c}); });
  for(let i=0;i<pts.length;i+=2){ const a=pts[i],b=pts[i+1]; if(b-a<=0.2) continue;
    if(eixo==='x') W(a,b,fixo-t/2,fixo+t/2,h,kind); else W(fixo-t/2,fixo+t/2,a,b,h,kind); }
}
// sorteio determinístico: o mapa é igual para todos os jogadores e em todas as partidas
let _sem=917; const sorte=()=>((_sem=(_sem*16807)%2147483647)/2147483647);
function escombros(cx,cz,raio,n,alt){ for(let k=0;k<n;k++){ const a=sorte()*Math.PI*2, r=Math.sqrt(sorte())*raio, w=.9+sorte()*1.6, d=.9+sorte()*1.6;
  const x=cx+Math.cos(a)*r, z=cz+Math.sin(a)*r; W(x-w/2,x+w/2,z-d/2,z+d/2,(alt||1.3)*(.6+sorte()*.7),'escombro'); } }
function colunaCaida(x1,z1,x2,z2){ W(x1,x2,z1,z2,1.35,'coluna_caida'); }
function coluna(x,z,h){ W(x-.8,x+.8,z-.8,z+.8,h,'coluna'); }
const ESTATUA=(x,z,pose,rot)=>W(x-.45,x+.45,z-.45,z+.45,2.4,'estatua',{pose,rot:r2(rot)});
// portas com batente e lintel para o cliente desenhar; o muro abre o vão com os mesmos números
const PORTAIS=[];
function porta(nome,eixo,fixo,a,b,brecha){ PORTAIS.push(brecha?{nome,eixo,fixo,a,b,brecha:true}:{nome,eixo,fixo,a,b}); return [a,b]; }

// limite do mapa: muro velho de pedra na mata; no promontório, a beira do penhasco
W(-HALF.x-1,HALF.x+1,-HALF.z-1,-HALF.z,5,'limite'); W(-HALF.x-1,HALF.x+1,HALF.z,HALF.z+1,5,'limite');
W(-HALF.x-1,-HALF.x,-HALF.z,HALF.z,5,'limite'); W(HALF.x,HALF.x+1,-HALF.z,HALF.z,5,'limite');

// ---------- o desfiladeiro ----------
// Fatias de 4 m com as bordas irregulares; os vãos são os tabuleiros das travessias. Baixo (não bloqueia visão nem tiro).
const TRAVESSIAS=[ // ao longo (de..ate) e largura (a..b) de cada tabuleiro
  {nome:'Ponte Grande',eixo:'z',a:-4,b:4,de:-22,ate:-6},
  {nome:'Ponte Caída',eixo:'x',a:-79,b:-75,de:-98,ate:-82},
  {nome:'Aqueduto',eixo:'x',a:-79,b:-75,de:82,ate:98}];
const ABISMO=[];
// Bordas retas (a irregularidade fica no visual: rocha do paredão e pedras soltas na beira), para que o chão que se vê e o
// chão onde se anda coincidam ao centímetro. Os vãos são os tabuleiros das travessias.
{ const fenda=(x1,x2,z1,z2)=>ABISMO.push(W(x1,x2,z1,z2,.6,'abismo'));
  for(const [x1,x2] of [[-98,-82],[82,98]]){ fenda(x1,x2,-100,-79); fenda(x1,x2,-75,-22); }
  fenda(-98,-5,-22,-6); fenda(5,98,-22,-6); }
// Ponte Grande: muretas baixas (cobertura de corpo inteiro só agachado atrás das estátuas), quatro estátuas em pedestais e
// uma estátua tombada no meio do tabuleiro, que obriga a desviar e serve de cobertura baixa
W(-5,-4,-22,-6,1.1,'parapeito'); W(4,5,-22,-6,1.1,'parapeito');
for(const z of [-18,-10]) for(const sx of [-1,1]) W(sx*4.5-.8,sx*4.5+.8,z-.8,z+.8,3.4,'estatua_ponte',{rot:sx>0?Math.PI/2:-Math.PI/2});
W(-1.2,1.6,-14.7,-13.3,1.2,'coluna_caida');
// Aqueduto: o canal velho virou passarela entre muretas; arcos nas duas cabeceiras
W(82,98,-80,-79,1.2,'parapeito'); W(82,98,-75,-74,1.2,'parapeito');
for(const x of [81,99]) for(const z of [-80.9,-73.1]) W(x-.8,x+.8,z-.8,z+.8,8,'pilar_ruina');

// ---------- a catedral ----------
// Fachada ao sul, voltada para a ponte. Torre oeste inteira (com a agulha), torre leste quebrada.
muro('x',-32,-16.5,16.5,[porta('Grande Porta','x',-32,-4,4)],26,'fachada');
W(-28,-16.5,-44,-31.5,52,'torre_oeste'); W(16.5,28,-44,-31.5,26,'torre_leste');
escombros(31,-37,3.2,7,1.6); escombros(31,-27,2.4,4,1.2); // a torre leste perdeu o topo: pedras no adro e no cemitério
// nártex: três arcos para a nave; o do meio fica no eixo da Grande Porta
muro('x',-40,-15.5,15.5,[porta('Arco Oeste','x',-40,-12,-8),porta('Arco Central','x',-40,-4,4),porta('Arco Leste','x',-40,8,12)],24,'catedral');
// nave: paredes altas com porta para o Claustro e a brecha da torre que caiu para o cemitério
muro('z',-16,-64,-32,[porta('Porta do Claustro','z',-16,-56,-52)],24,'catedral');
muro('z',16,-64,-32,[porta('Brecha da Torre','z',16,-53,-49,true)],24,'catedral');
// transepto: braço oeste inteiro, braço leste desabado (o teto caiu e levou parte da parede sul)
muro('x',-64,-44.5,-15.5,[porta('Porta da Galeria','x',-64,-34,-30)],22,'catedral');
muro('x',-64,15.5,44.5,[porta('Brecha da Capela','x',-64,26,34,true)],22,'catedral');
muro('x',-78,-44.5,-11.5,[porta('Porta da Sacristia','x',-78,-24,-20)],22,'catedral');
muro('x',-78,11.5,44.5,[porta('Porta do Tesouro','x',-78,20,24)],22,'catedral');
muro('z',-44,-78,-64,[porta('Porta do Mosteiro','z',-44,-73,-69)],22,'catedral');
muro('z',44,-78,-64,[porta('Porta do Ossuário','z',44,-73,-69)],22,'catedral');
// cruzeiro: quatro pilares que sustentam a torre do cruzeiro
for(const [x,z] of [[-8,-64],[8,-64],[-8,-78],[8,-78]]) W(x-1.3,x+1.3,z-1.3,z+1.3,24,'pilar_cruzeiro');
// coro: o jubé (anteparo de pedra com o crucifixo) fecha o eixo; passa-se pelas portas dos lados
muro('x',-78,-11.5,11.5,[porta('Jubé Oeste','x',-78,-6,-1.5),porta('Jubé Leste','x',-78,1.5,6)],9,'jube'); // entre os pilares do cruzeiro; o meio (com o crucifixo) fecha o eixo
muro('z',-12,-85,-78,[porta('Coro Oeste','z',-12,-83,-80)],22,'catedral');
muro('z',12,-85,-78,[porta('Coro Leste','z',12,-83,-80)],22,'catedral');
// abside: meia-volta de muro na beira do penhasco e, dentro, a colunata em meia-lua atrás do altar (deambulatório)
for(let k=0;k<=15;k++){ const a=Math.PI*(1+k/15), x=Math.cos(a)*11, z=-85+Math.sin(a)*11; W(x-1.2,x+1.2,z-1.2,z+1.2,22,'abside'); }
for(let k=0;k<7;k++){ const a=Math.PI*(1.12+k*.127), x=Math.cos(a)*6.6, z=-86+Math.sin(a)*6.2; W(x-.75,x+.75,z-.75,z+.75,12,'coluna_abside'); }
// Sacristia (oeste do coro): três portas; Tesouro (leste): desabado, só escombros e uma parede de pé
muro('z',-30,-90,-78,[porta('Sacristia Oeste','z',-30,-87,-83)],14,'pedra'); muro('x',-90,-30.5,-8.5,null,14,'pedra');
W(-29,-26,-89,-87.5,1.1,'tumba'); W(-16,-14.5,-88,-85,1.2,'arca');
muro('z',30,-90,-78,[porta('Tesouro Leste','z',30,-88,-84,true)],7,'ruina'); muro('x',-90,8.5,30.5,[[18,23]],5,'ruina');
escombros(24,-85,2.4,5,1.4);
// nave por dentro: bancos em dois blocos de cada lado do corredor central, alguns tombados
const PILLARS=[[-8,-46],[8,-46],[-8,-52],[8,-52],[-8,-58],[8,-58]];
const PEWS=[[-8,-1.8,-50,-47],[1.8,8,-50,-47],[-8,-1.8,-56,-53],[1.8,8,-56,-53],[-8,-1.8,-62,-59.2],[-42,-36,-75,-72],[-66.5,-63.5,-62,-59]];
escombros(4,-60.5,1.6,3,1); escombros(-12,-47,1.2,2,1); colunaCaida(10,-61,14.6,-59.8);
// braço oeste: túmulos de bispos como cobertura baixa
[[-38,-34,-68.5,-67],[-28,-24,-75.5,-74],[-24,-20,-68.5,-67]].forEach(([a,b,c,d])=>W(a,b,c,d,1.1,'tumba'));
// braço leste (Capela Desabada): o teto caído no chão e uma coluna tombada
escombros(24,-68,2.6,6,1.5); escombros(38,-75,2.2,5,1.4); escombros(40,-66,1.6,3,1.2); colunaCaida(20,-76,27,-74.8);

// ---------- Claustro: galeria coberta em volta de um jardim a céu aberto ----------
muro('x',-44,-44.5,-28,[porta('Porta do Adro','x',-44,-38,-34)],12,'claustro');
muro('z',-44,-64,-44,[porta('Porta dos Monges','z',-44,-56,-52)],12,'claustro');
for(const x of [-39,-34.5,-30,-25.5,-21]) for(const z of [-59,-49]){ if(x===-30&&z===-49) continue; W(x-.45,x+.45,z-.45,z+.45,6,'coluna_claustro'); }
for(const z of [-54]) for(const x of [-39,-21]) W(x-.45,x+.45,z-.45,z+.45,6,'coluna_claustro');
W(-31.5,-28.5,-55.5,-52.5,1.1,'poco');
[[-37,-34,-57.6,-57],[-26,-23,-51,-50.4]].forEach(([a,b,c,d])=>W(a,b,c,d,1,'sebe'));

// ---------- Sala do Capítulo (altar), passagem dos monges e ruínas do mosteiro ----------
muro('z',-52,-64.5,-45.5,[porta('Capítulo Leste','z',-52,-57,-53)],14,'pedra');
muro('z',-70,-64.5,-45.5,[porta('Capítulo Oeste','z',-70,-53,-49)],14,'pedra');
muro('x',-46,-70.5,-51.5,null,14,'pedra'); muro('x',-64,-70.5,-51.5,[porta('Capítulo Norte','x',-64,-63,-59)],14,'pedra');
W(-69,-68,-62,-56,.6,'banco'); W(-55,-54,-50,-47,.6,'banco');
// mosteiro: dormitório sem teto, paredes partidas e colunas que sobraram
muro('x',-70,-80,-74,[[-78,-76]],5,'ruina'); muro('z',-74,-96,-84,[[-92,-89]],4.5,'ruina'); muro('x',-92,-60,-44,[[-55,-51]],6,'ruina');
muro('z',-56,-92,-80,[[-87,-84]],3.5,'ruina'); coluna(-62,-80,5.5); coluna(-50,-84,3.2); colunaCaida(-68,-97,-60,-95.8); escombros(-76,-60,2,4,1.2);
// adro: o largo diante da fachada, com o cruzeiro de pedra fora do eixo e dois tanques secos
W(-12.5,-11.5,-27.5,-26.5,5,'cruz'); W(-27,-22,-28,-25,1,'tanque'); W(22,27,-28,-25,1,'tanque');
ESTATUA(-38,-30,1,.8); ESTATUA(38,-30,3,-.8);

// ---------- Cemitério dos Monges e Ossuário (leste) ----------
const LAPIDES=[];
for(const z of [-58,-52,-46,-40]) for(let x=36;x<=74;x+=5){ if(sorte()<.22) continue; const ox=(sorte()-.5)*1.4, oz=(sorte()-.5)*.8; LAPIDES.push(W(x+ox-.6,x+ox+.6,z+oz-.2,z+oz+.2,1.1,'lapide')); }
W(66,72,-64,-59,5,'mausoleu'); W(46,47,-62,-61,7,'arvore'); muro('z',33,-58,-36,[[-52,-48],[-44,-40]],1.1,'mureta');
muro('x',-66,52,80,[[60,64]],4,'ruina'); muro('z',52,-96,-66,[[-82,-78]],4,'ruina');
W(56,61,-90,-85,4.5,'ossos'); W(68,72,-76,-72,3,'ossos'); escombros(74,-92,2.2,4,1.1);

// ---------- a Floresta: estruturas fixas ----------
// Faixa oeste (entre o limite e o desfiladeiro): Acampamento dos Caçadores, Cabana do Ermitão, Ruínas Sombrias e a
// cabeceira da Ponte Caída. Faixa leste, o espelho do culto: Círculo de Pedras, Oratório, Carvoaria e o Aqueduto.
// Ao sul, em três fileiras: o Caminho da Borda com os mirantes e a Portaria; a Pedreira, a Encruzilhada e o Charco; os
// Menires, o Carvalho Oco e os Encapuzados. No fundo, o Calvário e a mata velha.
const FIXAS_ANTES=WALLS.length;
const MEN=[-82,64], ENC=[82,66], PED=[-46,38], CHA=[46,40], ENCR=[0,46], CARV=[14,61], CALV=[0,88], RUI=[-120,-86], CARVO=[120,-86], CAB=[-123,-60], ORA=[123,-60];
// Acampamento dos Caçadores: barracas, caixotes e fogueira
{ const [cx,cz]=[SPAWN.H.x,SPAWN.H.z]; [[-8,-10,3.4,3],[-10,7,3.4,3],[6,-12,3,3.2],[-6,12,3,2.6]].forEach(([x,z,w,d])=>W(cx+x-w/2,cx+x+w/2,cz+z-d/2,cz+z+d/2,2.6,'tenda'));
  [[8,9,1.2,1.2],[9.4,10.4,1,1],[4,-15,1.4,1]].forEach(([x,z,w,d])=>W(cx+x-w/2,cx+x+w/2,cz+z-d/2,cz+z+d/2,1,'caixote')); }
// Círculo de Pedras dos Cultistas: menires em anel, aberto para o noroeste (para a catedral)
for(let k=0;k<10;k++){ const a=k/10*Math.PI*2; if(Math.cos(a-Math.PI*1.15)>.55||k===3||k===7) continue; const x=SPAWN.C.x+Math.cos(a)*10, z=SPAWN.C.z+Math.sin(a)*10; W(x-.7,x+.7,z-.7,z+.7,3.6+((k*7)%3)*.5,'menir'); }
// Menires (altar): anel de pedras altas com três vãos (norte, oeste e sudeste)
for(let k=0;k<12;k++){ if(k===1||k===5||k===8) continue; const a=k/12*Math.PI*2+.2, x=MEN[0]+Math.cos(a)*8.5, z=MEN[1]+Math.sin(a)*8.5; W(x-.8,x+.8,z-.8,z+.8,4.2+((k*5)%4)*.4,'menir'); }
// Encapuzados (altar): capela sem teto e seis fiéis encapuzados de pedra em duas fileiras voltadas para o altar
{ const [cx,cz]=ENC; muro('x',cz-11,cx-10,cx+10,[[cx-4,cx]],3.2,'ruina'); muro('x',cz+11,cx-10,cx+10,[[cx+2,cx+6]],4.6,'ruina');
  muro('z',cx-10,cz-11,cz+11,[[cz-4,cz+2]],5.2,'ruina'); muro('z',cx+10,cz-11,cz+11,[[cz+2,cz+6]],3.8,'ruina');
  for(const [dx,dz,r] of [[-5,-6,.6],[-5,0,.9],[-5,6,1.2],[5,-6,-.6],[5,0,-.9],[5,6,-1.2]]) ESTATUA(cx+dx,cz+dz,4,Math.PI/2*(dx<0?-1:1)+r*.3);
  escombros(cx-8,cz+9,1.6,3,1.1); }
// Portaria (altar): duas torres na cabeça da ponte e um pátio murado; o altar fica fora do eixo, atrás da torre leste
W(-11,-5,-6,1,12,'torre_portaria'); W(5,11,-6,1,12,'torre_portaria');
// a passagem da ponte segue murada até a estrada; o pátio do altar fica a leste, com porta para a passagem
muro('z',-5,1,15,null,5,'ruina'); muro('z',5,1,15,[[8,12]],5,'ruina');
muro('x',1,11,23,null,5,'ruina'); muro('z',23,1,17,[[6,10]],4,'ruina'); muro('x',17,5,23,[[13,17]],5,'ruina');
escombros(-12,8,2,3,1.1); W(18,20.6,13,14.2,1.1,'tumba'); W(-16,-12,2,3,4,'ruina');
// Pedreira: paredões de pedra em ferradura abertos para o norte e o leste, blocos cortados no chão e o guindaste
{ const [cx,cz]=PED; muro('z',cx-14,cz-12,cz+14,[[cz-1,cz+3]],7,'penhasco'); muro('x',cz+14,cx-14,cx+12,[[cx-2,cx+2]],6,'penhasco'); muro('x',cz-12,cx-14,cx-4,null,6,'penhasco');
  [[-8,-6,3,2],[4,10,3,2.5],[-4,6,2.5,2],[8,-4,2.5,2]].forEach(([dx,dz,w,d])=>W(cx+dx-w/2,cx+dx+w/2,cz+dz-d/2,cz+dz+d/2,1.2,'bloco'));
  W(cx-.4,cx+.4,cz-.4,cz+.4,9,'guindaste'); }
// Charco: árvores mortas em pé dentro d'água e uma passarela velha
{ const [cx,cz]=CHA; [[-8,-6],[-2,-10],[8,-4],[4,8],[-10,6],[12,4],[-4,0]].forEach(([dx,dz])=>W(cx+dx-.4,cx+dx+.4,cz+dz-.4,cz+dz+.4,7,'arvore'));
  W(cx,cx+10,cz+1,cz+1.8,.5,'passarela'); }
// Encruzilhada e Carvalho Oco: o marco no cruzamento e o carvalho gigante, com raízes como cobertura
W(ENCR[0]-4.5,ENCR[0]-3.5,ENCR[1]-4.5,ENCR[1]-3.5,2.4,'marco');
{ const [cx,cz]=CARV; W(cx-3,cx+3,cz-2.5,cz+2,14,'carvalho'); [[-7,-3,-5,-3.8],[7,11.5,-5.5,-4.3],[-4.5,-3,-1,3],[7,8.3,-1.5,3]].forEach(([a,b,c,d])=>W(cx+a,cx+b,cz+c,cz+d,1.1,'raiz')); }
// Calvário: três cruzes num morro ao sul, visíveis de longe
for(const x of [-4,0,4]) W(CALV[0]+x-.35,CALV[0]+x+.35,CALV[1]-2.4,CALV[1]-1.6,x?5:7,'cruz');
// Cabana do Ermitão (mercador dos Caçadores) e Oratório da Carpideira (mercadora do culto)
{ const [cx,cz]=CAB; muro('x',cz-5,cx-5,cx+5,null,3.6,'madeira'); muro('x',cz+5,cx-5,cx+5,null,3.6,'madeira'); muro('z',cx-5,cz-5,cz+5,null,3.6,'madeira'); muro('z',cx+5,cz-5,cz+5,[[cz-2,cz+2]],3.6,'madeira'); }
{ const [cx,cz]=ORA; muro('x',cz-5,cx-4,cx+5,null,3.4,'ruina'); muro('x',cz+5,cx-4,cx+5,null,3.4,'ruina'); muro('z',cx+5,cz-5,cz+5,null,3.4,'ruina'); }
// Ruínas Sombrias (noroeste): capela sem teto engolida pela mata, paredes desabadas e fiéis petrificados
{ const [cx,cz]=RUI; muro('x',cz-7,cx-12,cx+12,[[cx-9,cx-5],[cx+2,cx+5]],6.5,'ruina'); muro('x',cz+7,cx-12,cx+12,[[cx-4,cx+1]],5,'ruina');
  muro('z',cx-12,cz-7,cz+7,[[cz-2,cz+1]],4.5,'ruina'); muro('z',cx+12,cz-7,cz+7,[[cz-3,cz+3]],3,'ruina'); escombros(cx+9,cz,2,4,1.2);
  coluna(cx-6,cz,3.2); coluna(cx+4,cz+1,5.5); colunaCaida(cx-8,cz+1,cx-6.8,cz+5);
  [[-7,-3,1,.3],[0,4,0,2.5],[3,-4,2,4]].forEach(([dx,dz,p,r])=>ESTATUA(cx+dx,cz+dz,p,r)); }
// Carvoaria (nordeste): fornos de carvão cobertos de terra, lenha empilhada e um telheiro
{ const [cx,cz]=CARVO; [[-6,-4],[2,-6],[7,2]].forEach(([dx,dz])=>W(cx+dx-2,cx+dx+2,cz+dz-2,cz+dz+2,2.4,'forno'));
  W(cx-5,cx+1,cz+5,cz+6.2,1.2,'tronco'); W(cx-4,cx+1,cz+7.2,cz+8.4,1.2,'tronco'); muro('x',cz-11,cx-2,cx+8,null,3.2,'madeira'); }

// reagentes: todos na mata, em lugares escolhidos. Dois na Pedreira e dois no Charco (no meio do caminho entre os lados),
// dois no Carvalho Oco e na Encruzilhada (o centro) e quatro na última mata antes de cada travessia: ao lado da Portaria,
// nas duas cabeças da Ponte Grande, e nas cabeceiras da Ponte Caída e do Aqueduto. Quem vai celebrar no promontório
// pega o reagente ali e atravessa carregando; a caça sabe disso e queima. (Medido em 400 noites de bots: com os reagentes
// longe das travessias, a Sala do Capítulo quase nunca era usada pelo culto.)
const REAG=[[PED[0]-6,PED[1]-2],[PED[0]+4,PED[1]+8],[CHA[0]+6,CHA[1]-4],[CHA[0]-4,CHA[1]+8],[CARV[0]+9,CARV[1]+4],[ENCR[0]-12,ENCR[1]+4],[-24,-1],[24,-1],[-106,-70],[106,-70]];

// Trilhas principais: faixas de chão batido onde o luar atravessa as copas. Fora delas a mata é fechada.
const TRILHAS=[
  {w:5,pts:[[SPAWN.H.x,SPAWN.H.z],[-116,-48],[-110,-66],[-99,-77]]},                         // 0 acampamento → Ponte Caída
  {w:5,pts:[[SPAWN.C.x,SPAWN.C.z],[116,-48],[110,-66],[99,-77]]},                            // 1 círculo → Aqueduto
  {w:4.5,pts:[[SPAWN.H.x,SPAWN.H.z],[-108,-6],[-90,5],[-56,9],[-28,15],[-14,23],[14,23],[28,15],[56,9],[90,5],[108,-6],[SPAWN.C.x,SPAWN.C.z]]}, // 2 Caminho da Borda
  {w:5,pts:[[0,17],[ENCR[0],ENCR[1]],[0,CALV[1]-8]]},                                       // 3 Estrada dos Peregrinos
  {w:3.5,pts:[[MEN[0]+7,MEN[1]+6],[-66,60],[-40,56],[-18,50],[ENCR[0],ENCR[1]],[18,50],[40,56],[66,62],[ENC[0]-12,ENC[1]-1]]}, // 4 volta do sul
  {w:4,pts:[[-108,-6],[-108,24],[MEN[0]-3,MEN[1]-10]]},                                      // 5 acampamento → Menires
  {w:4,pts:[[108,-6],[108,24],[ENC[0]-2,ENC[1]-14]]},                                        // 6 círculo → Encapuzados
  {w:3,pts:[[-110,-66],[RUI[0]-1.5,RUI[1]+9]]},{w:3,pts:[[110,-66],[CARVO[0],CARVO[1]+10]]}, // 7, 8 ruínas e carvoaria
  {w:3,pts:[[-56,9],[PED[0]+2,PED[1]-18]]},{w:3,pts:[[56,9],[CHA[0],CHA[1]-14]]},             // 9, 10 Borda → Pedreira, Charco
  {w:3.5,pts:[[-116,-48],[CAB[0]+7,CAB[1]]]},{w:3.5,pts:[[116,-48],[ORA[0]-6,ORA[1]]]},       // 11, 12 cabana e oratório
  {w:3,pts:[[-17,9],[-28,15]]},{w:3,pts:[[17,7],[28,15]]},                                   // 13, 14 portões laterais da Portaria
  {w:3,pts:[[PED[0],PED[1]+16],[-40,56]]},{w:3,pts:[[CHA[0],CHA[1]+14],[40,56]]}];           // 15, 16 Pedreira e Charco → volta do sul
// Clareiras: abertas ao céu. O luar ilumina (bom para os Caçadores) e a mata em volta esconde quem chega.
const CLAREIRAS=[{x:SPAWN.H.x,z:SPAWN.H.z,r:15},{x:SPAWN.C.x,z:SPAWN.C.z,r:15},{x:MEN[0],z:MEN[1],r:14},{x:ENC[0],z:ENC[1],r:15},{x:PED[0],z:PED[1],r:15},{x:CHA[0],z:CHA[1],r:15},
  {x:ENCR[0]+2,z:ENCR[1],r:11},{x:CALV[0],z:CALV[1]-3,r:10},{x:-56,z:0,r:8},{x:56,z:0,r:8},{x:RUI[0],z:RUI[1],r:11},{x:CARVO[0],z:CARVO[1],r:10},{x:CAB[0],z:CAB[1],r:8},{x:ORA[0],z:ORA[1],r:8},
  {x:0,z:9,r:15},{x:CARV[0]+2,z:CARV[1],r:9}];
// pontos de tarefa da floresta (fixos; a mata é gerada em volta deles)
const PISTA_PTS=[[-136,-96],[-108,-38],[-80,24],[-36,82],[36,82],[80,24],[108,-38],[136,-96]];
const ERVA_PTS=[[-136,8],[-122,86],[-74,86],[-24,66],[30,72],[74,86],[122,86],[136,8]];
const SENTINELA_PTS=[[-48,-58],[0,-71],[31,-56],[MEN[0]-4,MEN[1]-22],[0,26],[ENC[0]+4,ENC[1]-22]];
// trilhas estreitas (2,4 m, sem luar): de cada ponto de tarefa até a trilha principal mais próxima, com curvas,
// e mais algumas vielas que entram na mata e voltam. Todas geradas com a mesma semente.
{ const segs=[]; TRILHAS.forEach(T=>{ for(let i=1;i<T.pts.length;i++) segs.push([T.pts[i-1],T.pts[i]]); });
  const maisPerto=(x,z)=>{ let best=null,bd=1e9; for(const [A,B] of segs){ const dx=B[0]-A[0],dz=B[1]-A[1],L=dx*dx+dz*dz, t=clamp(L?((x-A[0])*dx+(z-A[1])*dz)/L:0,0,1), px=A[0]+dx*t, pz=A[1]+dz*t, d=Math.hypot(px-x,pz-z); if(d<bd){ bd=d; best=[px,pz]; } } return best; };
  const dentro=(x,z)=>Math.abs(x)<HALF.x-4&&Math.abs(z)<HALF.z-4&&!foraDaMata(x,z,5);
  const curva=(A,B,n)=>{ const pts=[A]; const dx=B[0]-A[0],dz=B[1]-A[1],L=Math.hypot(dx,dz)||1, nx=-dz/L, nz=dx/L;
    for(let k=1;k<n;k++){ const t=k/n, j=(sorte()-.5)*Math.min(10,L*.35); let p=[A[0]+dx*t+nx*j,A[1]+dz*t+nz*j]; if(!dentro(p[0],p[1])) p=[A[0]+dx*t,A[1]+dz*t]; pts.push(p); } pts.push(B); return pts.map(([x,z])=>[r2(x),r2(z)]); };
  for(const [x,z] of PISTA_PTS.concat(ERVA_PTS)){ const q=maisPerto(x,z); TRILHAS.push({w:2.4,estreita:true,pts:curva([x,z],q,Math.max(2,Math.round(Math.hypot(q[0]-x,q[1]-z)/9)))}); }
  for(let n=0,t=0;n<11&&t<400;t++){ const x=(sorte()*2-1)*(HALF.x-8), z=(sorte()*2-1)*(HALF.z-8); if(!dentro(x,z)) continue; const q=maisPerto(x,z), d=Math.hypot(q[0]-x,q[1]-z); if(d<18||d>55) continue;
    TRILHAS.push({w:2.4,estreita:true,pts:curva(q,[x,z],Math.round(d/9))}); n++; } }
const SEG_TRILHA=[]; TRILHAS.forEach(T=>{ for(let i=1;i<T.pts.length;i++) SEG_TRILHA.push({ax:T.pts[i-1][0],az:T.pts[i-1][1],bx:T.pts[i][0],bz:T.pts[i][1],w:T.w,estreita:!!T.estreita}); });
function distSeg(x,z,s){ const dx=s.bx-s.ax,dz=s.bz-s.az,L=dx*dx+dz*dz; let t=L?((x-s.ax)*dx+(z-s.az)*dz)/L:0; t=clamp(t,0,1); return Math.hypot(x-(s.ax+dx*t),z-(s.az+dz*t)); }
const naTrilha=(x,z,folga)=>SEG_TRILHA.some(s=>distSeg(x,z,s)<s.w/2+(folga||0));
PISTA_PTS.forEach(([x,z])=>W(x-.8,x+.8,z+1.2,z+2.2,1.3,'santuario')); // santuário em ruínas; o sinal fica na frente dele
// Arcos de pedra sobre as trilhas: portais do que já foi a estrada dos peregrinos (pilares fora da faixa da trilha)
const ARCOS=[];
[[3,0,.45],[3,1,.55],[2,3,.5],[2,7,.5],[0,1,.5],[1,1,.5],[4,1,.5],[4,6,.5]].forEach(([ti,si,t])=>{ const P=TRILHAS[ti].pts, A=P[si], B=P[si+1], dx=B[0]-A[0], dz=B[1]-A[1], L=Math.hypot(dx,dz), nx=-dz/L, nz=dx/L, cx=A[0]+dx*t, cz=A[1]+dz*t, o=TRILHAS[ti].w/2+1.3;
  ARCOS.push([r2(cx+nx*o),r2(cz+nz*o),r2(cx-nx*o),r2(cz-nz*o)]); });
ARCOS.forEach(([x1,z1,x2,z2])=>{ W(x1-.65,x1+.65,z1-.65,z1+.65,7.5,'pilar_ruina'); W(x2-.65,x2+.65,z2-.65,z2+.65,7.5,'pilar_ruina'); });
const FIXAS=WALLS.slice(FIXAS_ANTES).concat(WALLS.filter(w=>w.kind==='limite'));

// Mata: árvores enormes, espinheiros no meio delas e pouco espaço entre uma coisa e outra.
// Gerada com a mesma semente, então todos veem a mesma floresta. Nada nasce no promontório nem no desfiladeiro.
const ARVORES=[], ARBUSTOS=[];
{
  const livreDeTudo=(x,z,folga)=>{
    if(foraDaMata(x,z,3+folga)) return false;
    if(Math.abs(x)>HALF.x-1.5||Math.abs(z)>HALF.z-1.5) return false;
    if(naTrilha(x,z,.9+folga)) return false;
    for(const c of CLAREIRAS) if(Math.hypot(x-c.x,z-c.z)<c.r+folga) return false;
    for(const [px,pz] of PISTA_PTS.concat(ERVA_PTS,SENTINELA_PTS,REAG)) if(Math.hypot(x-px,z-pz)<3.5+folga) return false;
    for(const w of FIXAS) if(x>w.x1-1.5-folga&&x<w.x2+1.5+folga&&z>w.z1-1.5-folga&&z<w.z2+1.5+folga) return false;
    return true; };
  // grade de ocupação para manter espaçamento entre peças sem O(n²)
  const cel=6, ocup=new Map(), chave=(x,z)=>Math.floor(x/cel)*10000+Math.floor(z/cel);
  const perto=(x,z,r,folga)=>{ const cx=Math.floor(x/cel),cz=Math.floor(z/cel); for(let i=-2;i<=2;i++) for(let j=-2;j<=2;j++){ const L=ocup.get((cx+i)*10000+cz+j); if(L) for(const q of L) if(Math.hypot(q.x-x,q.z-z)<q.r+r+folga) return true; } return false; };
  const guarda=q=>{ const k=chave(q.x,q.z); let L=ocup.get(k); if(!L) ocup.set(k,L=[]); L.push(q); };
  const aleat=m=>[(sorte()*2-1)*(HALF.x-m),(sorte()*2-1)*(HALF.z-m)];
  // muros quebrados soltos: uns baixos (cobertura), outros altos (cortam a visão)
  for(let t=0,n=0;t<3000&&n<16;t++){ const [x,z]=aleat(6), len=3+sorte()*5, aoX=sorte()<.5, alto=sorte()<.6;
    const x1=aoX?x-len/2:x-.5, x2=aoX?x+len/2:x+.5, z1=aoX?z-.5:z-len/2, z2=aoX?z+.5:z+len/2;
    let ok=true; for(let k=0;k<=4&&ok;k++){ const px=lerp(x1,x2,k/4), pz=lerp(z1,z2,k/4); if(!livreDeTudo(px,pz,1)||perto(px,pz,.6,3)) ok=false; }
    if(!ok) continue; for(let k=0;k<=4;k++) guarda({x:lerp(x1,x2,k/4),z:lerp(z1,z2,k/4),r:.6}); W(x1,x2,z1,z2,alto?3+sorte()*3:1.2+sorte()*.6,'ruina'); n++; }
  // estátuas à beira das trilhas estreitas e no meio da mata
  for(let t=0,n=0;t<5000&&n<20;t++){ const [x,z]=aleat(4);
    if(!livreDeTudo(x,z,.3)||perto(x,z,.5,2.5)) continue; guarda({x,z,r:.5}); ESTATUA(x,z,Math.floor(sorte()*5),sorte()*6.283); n++; }
  // árvores caídas primeiro: precisam de um corredor livre comprido
  for(let t=0,n=0;t<4000&&n<22;t++){ const [x,z]=aleat(5), len=6+sorte()*5, aoX=sorte()<.5;
    const x1=aoX?x-len/2:x-.6, x2=aoX?x+len/2:x+.6, z1=aoX?z-.6:z-len/2, z2=aoX?z+.6:z+len/2;
    let ok=true; for(let k=0;k<=5&&ok;k++){ const px=lerp(x1,x2,k/5), pz=lerp(z1,z2,k/5); if(!livreDeTudo(px,pz,.6)||perto(px,pz,.6,1.6)) ok=false; }
    if(!ok) continue; for(let k=0;k<=5;k++) guarda({x:lerp(x1,x2,k/5),z:lerp(z1,z2,k/5),r:.6}); W(x1,x2,z1,z2,1.2,'tronco'); n++; }
  // árvores: vão de 1,4 a 3 m entre troncos, então a mata vira um labirinto de passagens estreitas
  for(let t=0;t<30000&&ARVORES.length<860;t++){
    const [x,z]=aleat(2);
    const u=sorte(), tipo=u<.3?0:u<.76?1:2; // 0 anciã (tronco enorme), 1 alta de copa fechada, 2 morta retorcida
    const r=tipo===0?.95+sorte()*.5:tipo===1?.55+sorte()*.3:.45+sorte()*.25;
    if(!livreDeTudo(x,z,r)||perto(x,z,r,1.4+sorte()*1.6)) continue;
    const q={x:r2(x),z:r2(z),r:r2(r),tipo,s:r2(.85+sorte()*.32),rot:r2(sorte()*6.283),inc:r2((sorte()-.5)*.08)};
    guarda(q); ARVORES.push(q);
  }
  ARVORES.forEach(q=>{ const h=q.r*.85; W(q.x-h,q.x+h,q.z-h,q.z+h,12,'arvore_f'); });
  // pedras cobertas de musgo
  for(let t=0,n=0;t<3000&&n<44;t++){ const [x,z]=aleat(3), w=1.2+sorte()*1.8, d=1+sorte()*1.6;
    if(!livreDeTudo(x,z,Math.max(w,d)*.6)||perto(x,z,Math.max(w,d)/2,1.2)) continue; guarda({x,z,r:Math.max(w,d)/2}); W(x-w/2,x+w/2,z-d/2,z+d/2,.9+sorte()*.9,'rocha'); n++; }
  // espinheiros: fecham boa parte dos vãos entre as árvores. Não dá para passar por eles nem ver através.
  for(let t=0,n=0;t<30000&&n<940;t++){ const [x,z]=aleat(2), r=.8+sorte()*.8;
    if(!livreDeTudo(x,z,r*.9)||perto(x,z,r,.45+sorte()*.5)) continue; guarda({x,z,r}); const h=r*.9; W(x-h,x+h,z-h,z+h,2.6,'espinheiro',{rot:r2(sorte()*6.283)}); n++; }
  // vegetação rasteira: samambaias e montes de musgo, só visual (não bloqueia)
  for(let t=0;t<9000&&ARBUSTOS.length<1300;t++){ const [x,z]=aleat(1);
    if(foraDaMata(x,z,1.5)||naTrilha(x,z,-.3)) continue; let dentro=false; for(const c of CLAREIRAS) if(Math.hypot(x-c.x,z-c.z)<c.r*.6) dentro=true; if(dentro) continue;
    ARBUSTOS.push({x:r2(x),z:r2(z),s:r2(.6+sorte()*.8),rot:r2(sorte()*6.283),tipo:sorte()<.62?0:1}); }
}

// Luzes: velas dentro da catedral; lanternas, fogueiras e braseiros lá fora. [x, z, tipo]
const CANDLES=[[-10,-36],[10,-36],[-12,-48],[12,-56],[-12,-60],[-13,-70],[13,-70],[-3,-81],[3,-81],[-5,-92],[5,-92],[-22,-84],
  [-66,-50],[-56,-61],[-42,-46],[-18,-62],[-37,-70],[-30,-76],[40,-68],
  [-6,-24,'lanterna'],[6,-24,'lanterna'],[-12.5,-3,'lanterna'],[12.5,-3,'lanterna'],[-80,-82,'lanterna'],[79,-83,'lanterna'],[-101,-82,'lanterna'],[101,-82,'lanterna'],
  [SPAWN.H.x,SPAWN.H.z-4,'fogueira'],[SPAWN.C.x-4,SPAWN.C.z-6,'braseiro'],[SPAWN.C.x-4,SPAWN.C.z+6,'braseiro'],[CAB[0]+6.5,CAB[1]-3,'lanterna'],[CAB[0]+6.5,CAB[1]+3,'lanterna'],
  [ORA[0]-5,ORA[1]-2,'lanterna'],[ENC[0]+2,ENC[1]-4,'lanterna'],[ENC[0]-2,ENC[1]+6,'lanterna'],[ENCR[0]-4,ENCR[1]-2,'lanterna'],[-18,27,'lanterna'],[18,27,'lanterna'],
  [CALV[0]-2,CALV[1]-6,'lanterna'],[-60,-28,'lanterna'],[60,-28,'lanterna']];
const BOX=[];
function addCol(x1,x2,z1,z2,h,tall){ BOX.push({x1:Math.min(x1,x2),x2:Math.max(x1,x2),z1:Math.min(z1,z2),z2:Math.max(z1,z2),h,tall}); }
WALLS.forEach(w=>addCol(w.x1,w.x2,w.z1,w.z2,w.h,w.tall));
PILLARS.forEach(([x,z])=>addCol(x-0.85,x+0.85,z-0.85,z+0.85,14,true));
PEWS.forEach(p=>addCol(p[0],p[1],p[2],p[3],0.95,false));
ALTARS.forEach(a=>addCol(a.x-1.3,a.x+1.3,a.z-0.65,a.z+0.65,1.1,false));
const TALL=BOX.filter(b=>b.tall);
const WALLDEF=WALLS; // compatibilidade com o cliente

function segBox(ax,az,bx,bz,b,inf){
  const x1=b.x1-inf,x2=b.x2+inf,z1=b.z1-inf,z2=b.z2+inf, dx=bx-ax,dz=bz-az; let t0=0,t1=1;
  const ps=[[-dx,ax-x1],[dx,x2-ax],[-dz,az-z1],[dz,z2-az]];
  for(const [p,q] of ps){ if(Math.abs(p)<1e-9){ if(q<0) return -1; } else { const r=q/p; if(p<0){ if(r>t1) return -1; if(r>t0) t0=r; } else { if(r<t0) return -1; if(r<t1) t1=r; } } }
  return t0;
}
// Grade espacial: cada checagem de colisão ou visão olha só as peças das células por onde passa.
const GRADE={};
function grade(list){ const chave=list===BOX?'b':list===TALL?'t':null; if(!chave) return null; if(GRADE[chave]) return GRADE[chave];
  const G={c:6,m:new Map(),marca:new Uint32Array(list.length),vez:0,list};
  list.forEach((b,i)=>{ for(let cx=Math.floor(b.x1/G.c);cx<=Math.floor(b.x2/G.c);cx++) for(let cz=Math.floor(b.z1/G.c);cz<=Math.floor(b.z2/G.c);cz++){ const k=cx*1000+cz; let a=G.m.get(k); if(!a) G.m.set(k,a=[]); a.push(i); } });
  return GRADE[chave]=G; }
function algum(G,x1,z1,x2,z2,fn){ G.vez++; const c=G.c;
  for(let cx=Math.floor(x1/c);cx<=Math.floor(x2/c);cx++) for(let cz=Math.floor(z1/c);cz<=Math.floor(z2/c);cz++){ const a=G.m.get(cx*1000+cz); if(!a) continue;
    for(const i of a){ if(G.marca[i]===G.vez) continue; G.marca[i]=G.vez; if(fn(G.list[i])) return true; } }
  return false; }
// Segmentos longos atravessam muitas células: percorre a grade só pelas células que a linha toca (DDA),
// em vez da caixa inteira que envolve o segmento.
function segClear(ax,az,bx,bz,inf,list){ const G=grade(list);
  if(!G){ for(const b of list){ if(segBox(ax,az,bx,bz,b,inf)>=0) return false; } return true; }
  const L=Math.hypot(bx-ax,bz-az);
  if(L<G.c*2) return !algum(G,Math.min(ax,bx)-inf,Math.min(az,bz)-inf,Math.max(ax,bx)+inf,Math.max(az,bz)+inf,b=>segBox(ax,az,bx,bz,b,inf)>=0);
  const n=Math.ceil(L/(G.c*.5)); G.vez++;
  for(let k=0;k<=n;k++){ const px=ax+(bx-ax)*k/n, pz=az+(bz-az)*k/n, r=G.c*.5+inf;
    const cx1=Math.floor((px-r)/G.c),cx2=Math.floor((px+r)/G.c),cz1=Math.floor((pz-r)/G.c),cz2=Math.floor((pz+r)/G.c);
    for(let cx=cx1;cx<=cx2;cx++) for(let cz=cz1;cz<=cz2;cz++){ const a=G.m.get(cx*1000+cz); if(!a) continue;
      for(const i of a){ if(G.marca[i]===G.vez) continue; G.marca[i]=G.vez; if(segBox(ax,az,bx,bz,G.list[i],inf)>=0) return false; } } }
  return true; }
const losClear=(a,b)=>segClear(a.x,a.z,b.x,b.z,0,TALL);
function occlusion(ax,az,bx,bz){ let n=0; algum(grade(TALL),Math.min(ax,bx),Math.min(az,bz),Math.max(ax,bx),Math.max(az,bz),b=>{ if(segBox(ax,az,bx,bz,b,0)>=0){ n++; if(n>=3) return true; } return false; }); return Math.pow(0.55,n); }
// primeira peça alta atingida por um raio (tiros, sinalizadores). Devolve a distância ao longo do raio.
// lista: TALL (padrão) ou BOX, para tiros que também param na cobertura baixa (com alturaEm, só quando passam abaixo dela)
function raioParede(ox,oz,dx,dz,alcance,inf,alturaEm,lista){ let best=alcance; const G=grade(lista||TALL), n=Math.ceil(alcance/(G.c*.5)); G.vez++;
  for(let k=0;k<=n;k++){ const px=ox+dx*alcance*k/n, pz=oz+dz*alcance*k/n; if(k/n*alcance>best+G.c) break; const r=G.c*.5+inf;
    for(let cx=Math.floor((px-r)/G.c);cx<=Math.floor((px+r)/G.c);cx++) for(let cz=Math.floor((pz-r)/G.c);cz<=Math.floor((pz+r)/G.c);cz++){ const a=G.m.get(cx*1000+cz); if(!a) continue;
      for(const i of a){ if(G.marca[i]===G.vez) continue; G.marca[i]=G.vez; const b=G.list[i], t=segBox(ox,oz,ox+dx*alcance,oz+dz*alcance,b,inf); if(t<0) continue; const d=t*alcance;
        if(d<best&&(!alturaEm||alturaEm(d)<b.h)) best=d; } } }
  return best; }
function resolve(p,r){
  for(let it=0;it<4;it++){
    algum(grade(BOX),p.x-r,p.z-r,p.x+r,p.z+r,b=>{ const cx=clamp(p.x,b.x1,b.x2), cz=clamp(p.z,b.z1,b.z2), dx=p.x-cx, dz=p.z-cz, d=Math.hypot(dx,dz);
      if(d<r){ if(d>1e-6){ p.x=cx+dx/d*r; p.z=cz+dz/d*r; } else { const l=p.x-b.x1,rr=b.x2-p.x,t=p.z-b.z1,bb=b.z2-p.z,m=Math.min(l,rr,t,bb); if(m===l)p.x=b.x1-r; else if(m===rr)p.x=b.x2+r; else if(m===t)p.z=b.z1-r; else p.z=b.z2+r; } }
      return false; });
    p.x=clamp(p.x,-HALF.x+r,HALF.x-r); p.z=clamp(p.z,-HALF.z+r,HALF.z-r);
  }
}
// Verdadeiro se um círculo de raio r em p não encosta em nenhuma peça.
function livre(p,r){ return !algum(grade(BOX),p.x-r,p.z-r,p.x+r,p.z+r,b=>{ const cx=clamp(p.x,b.x1,b.x2), cz=clamp(p.z,b.z1,b.z2); return Math.hypot(p.x-cx,p.z-cz)<r-.02; }); }

// ============ NAVEGAÇÃO ============
// Grade de 2 m sobre o mapa inteiro, ligada em 8 direções, mais âncoras (portas, altares, reagentes, spawns,
// mercadores, pontos de tarefa). A* com fila binária em vetores tipados: o mapa tem ~15 mil nós e cada
// busca visita só uma fração deles. Nós fora da maior componente conexa nunca são usados como início ou fim.
const NAV={passo:2}; const WP=[];
{
  const P=NAV.passo; NAV.nx=Math.floor((HALF.x*2-4)/P)+1; NAV.nz=Math.floor((HALF.z*2-4)/P)+1; NAV.x0=-HALF.x+2; NAV.z0=-HALF.z+2; // coordenadas pares: cabem nos vãos de 3 m entre paredes em coordenadas inteiras
  NAV.cel=new Int32Array(NAV.nx*NAV.nz).fill(-1);
  const livreNo=(x,z)=>livre({x,z},.8);
  for(let i=0;i<NAV.nx;i++) for(let j=0;j<NAV.nz;j++){ const x=NAV.x0+i*P, z=NAV.z0+j*P; if(livreNo(x,z)){ NAV.cel[i*NAV.nz+j]=WP.length; WP.push({x,z,n:[]}); } }
  const liga=(a,b)=>{ const A=WP[a],B=WP[b]; if(A.n.some(q=>q[0]===b)) return; const d=Math.hypot(A.x-B.x,A.z-B.z); if(!segClear(A.x,A.z,B.x,B.z,.35,BOX)) return; A.n.push([b,d]); B.n.push([a,d]); };
  for(let i=0;i<NAV.nx;i++) for(let j=0;j<NAV.nz;j++){ const a=NAV.cel[i*NAV.nz+j]; if(a<0) continue;
    for(const [di,dj] of [[1,0],[0,1],[1,1],[1,-1]]){ const ii=i+di,jj=j+dj; if(ii<0||jj<0||ii>=NAV.nx||jj>=NAV.nz) continue; const b=NAV.cel[ii*NAV.nz+jj]; if(b>=0) liga(a,b); } }
  // índice espacial de todos os nós (inclusive âncoras) em células de 4 m
  NAV.idx=new Map(); NAV.chave=(x,z)=>Math.floor(x/4)*10000+Math.floor(z/4);
  const indexa=k=>{ const q=WP[k], c=NAV.chave(q.x,q.z); let L=NAV.idx.get(c); if(!L) NAV.idx.set(c,L=[]); L.push(k); };
  for(let k=0;k<WP.length;k++) indexa(k);
  NAV.vizinhos=(x,z,r)=>{ const out=[], c1=Math.floor((x-r)/4),c2=Math.floor((x+r)/4),d1=Math.floor((z-r)/4),d2=Math.floor((z+r)/4);
    for(let cx=c1;cx<=c2;cx++) for(let cz=d1;cz<=d2;cz++){ const L=NAV.idx.get(cx*10000+cz); if(L) for(const k of L) out.push(k); } return out; };
  const ancora=(x,z)=>{ if(Math.abs(x)>HALF.x-1||Math.abs(z)>HALF.z-1||!livre({x,z},.55)) return; const k=WP.length; WP.push({x,z,n:[]});
    for(const j of NAV.vizinhos(x,z,3.2)) if(j!==k&&Math.hypot(WP[j].x-x,WP[j].z-z)<3.2) liga(k,j); indexa(k); };
  PORTAS.forEach(p=>ancora(p.x,p.z));
  PORTAIS.forEach(p=>{ const c=(p.a+p.b)/2; if(p.eixo==='x') ancora(c,p.fixo); else ancora(p.fixo,c); });
  ALTARS.forEach(a=>{ ancora(a.x,a.z+3); ancora(a.x,a.z-3); ancora(a.x+3,a.z); ancora(a.x-3,a.z); });
  REAG.forEach(([x,z])=>ancora(x+1.5,z));
  ancora(SPAWN.H.x,SPAWN.H.z); ancora(SPAWN.C.x,SPAWN.C.z);
  PISTA_PTS.concat(ERVA_PTS,SENTINELA_PTS).forEach(([x,z])=>ancora(x,z));
  // componentes conexas: só a maior vale como destino
  NAV.comp=new Int32Array(WP.length).fill(-1); let nc=0; const tam=[];
  for(let i=0;i<WP.length;i++){ if(NAV.comp[i]>=0) continue; const st=[i]; NAV.comp[i]=nc; tam[nc]=0; while(st.length){ const u=st.pop(); tam[nc]++; for(const [v] of WP[u].n) if(NAV.comp[v]<0){ NAV.comp[v]=nc; st.push(v); } } nc++; }
  NAV.principal=tam.indexOf(Math.max(...tam)); NAV.ilhas=tam.map((n,i)=>({i,n})).filter(q=>q.i!==NAV.principal);
  NAV.g=new Float64Array(WP.length); NAV.de=new Int32Array(WP.length); NAV.marca=new Uint32Array(WP.length); NAV.fechado=new Uint32Array(WP.length); NAV.vez=0;
}
function nearestWP(p){ let best=-1,bd=1e9;
  for(const r of [3,6,12]){ for(const i of NAV.vizinhos(p.x,p.z,r)){ if(NAV.comp[i]!==NAV.principal) continue; const d=Math.hypot(p.x-WP[i].x,p.z-WP[i].z); if(d<bd&&d<=r*1.5&&segClear(p.x,p.z,WP[i].x,WP[i].z,0.4,BOX)){ bd=d; best=i; } } if(best>=0) return best; }
  for(const r of [12,30]){ for(const i of NAV.vizinhos(p.x,p.z,r)){ if(NAV.comp[i]!==NAV.principal) continue; const d=Math.hypot(p.x-WP[i].x,p.z-WP[i].z); if(d<bd){ bd=d; best=i; } } if(best>=0) return best; }
  for(let i=0;i<WP.length;i++){ if(NAV.comp[i]!==NAV.principal) continue; const d=Math.hypot(p.x-WP[i].x,p.z-WP[i].z); if(d<bd){ bd=d; best=i; } } return best; }
function aEstrela(s,gl){
  const N=NAV, vez=++N.vez, alvo=WP[gl]; const heap=[]; // pares [f,i]
  const push=(f,i)=>{ heap.push([f,i]); let c=heap.length-1; while(c>0){ const p=(c-1)>>1; if(heap[p][0]<=heap[c][0]) break; [heap[p],heap[c]]=[heap[c],heap[p]]; c=p; } };
  const pop=()=>{ const top=heap[0],last=heap.pop(); if(heap.length){ heap[0]=last; let c=0; for(;;){ const l=c*2+1,r=l+1; let m=c; if(l<heap.length&&heap[l][0]<heap[m][0]) m=l; if(r<heap.length&&heap[r][0]<heap[m][0]) m=r; if(m===c) break; [heap[m],heap[c]]=[heap[c],heap[m]]; c=m; } } return top; };
  N.marca[s]=vez; N.g[s]=0; N.de[s]=-1; push(Math.hypot(WP[s].x-alvo.x,WP[s].z-alvo.z),s);
  while(heap.length){ const [,u]=pop(); if(N.fechado[u]===vez) continue; N.fechado[u]=vez; if(u===gl) break;
    const gu=N.g[u];
    for(const [v,w] of WP[u].n){ if(N.fechado[v]===vez) continue; const gv=gu+w; if(N.marca[v]!==vez||gv<N.g[v]){ N.marca[v]=vez; N.g[v]=gv; N.de[v]=u; push(gv+Math.hypot(WP[v].x-alvo.x,WP[v].z-alvo.z),v); } } }
  if(N.fechado[gl]!==vez) return null;
  const path=[]; for(let c=gl,guard=0;c>=0&&guard<20000;c=N.de[c],guard++) path.push(c); path.reverse(); return path; }
function findPath(from,to){
  if(Math.hypot(to.x-from.x,to.z-from.z)<40&&segClear(from.x,from.z,to.x,to.z,0.45,BOX)) return [{x:to.x,z:to.z}];
  const s=nearestWP(from), gl=nearestWP(to); if(s<0||gl<0) return [{x:to.x,z:to.z}];
  const ids=aEstrela(s,gl); if(!ids) return [{x:to.x,z:to.z}];
  const path=ids.map(i=>({x:WP[i].x,z:WP[i].z})); path.push({x:to.x,z:to.z});
  // suavização: de cada ponto, pula para o mais distante que ainda se enxerga (até 24 nós à frente)
  const out=[]; let cur=from,i=0;
  while(i<path.length){ let j=i; while(j+1<path.length&&j+1-i<24&&segClear(cur.x,cur.z,path[j+1].x,path[j+1].z,0.45,BOX)) j++; out.push(path[j]); cur=path[j]; i=j+1; }
  return out;
}

// ============ JOGO ============
const BOT_NAMES={A:['Irmã Beatriz','Irmão Tomé'],B:['Irmão Cinza','Madre Vesper']};
function defaultSlots(){ return [{team:'A',cid:'local',name:'Você'},{team:'A',cid:null,name:'Irmã Beatriz'},{team:'B',cid:null,name:'Irmão Cinza'},{team:'B',cid:null,name:'Madre Vesper'}]; }
const roleOf=(g,team)=>((g.round===1)===(team==='A'))?'H':'C';
function createGame(opts){
  opts=opts||{};
  const g={opts:opts,t:0,rt:0,phase:'pick',phaseT:0,round:1,timers:!!opts.timers,slots:(opts.slots||defaultSlots()).map(s=>({...s})),
    altars:[],reagents:REAG.map(([x,z],i)=>({i,x,z,has:true,t:0})),actors:[],proj:[],pickups:[],flares:[0,1,2,3,4,5].map(()=>({x:0,z:0,t:0})),
    know:[],visit:[],decoys:2,transferUsed:false,lastResolveT:0,endAt:0,events:[],log:[],scores:{A:null,B:null},stats:{},picks:[],ready:{},cls:{},
    nid:1,feit:{},buff:{ess:0,selo:0},firstContact:null,matchId:opts.matchId||String(Date.now()),lab:labDe(opts.lab)};
  enterPick(g,1);
  return g;
}
function ev(g,e){ g.events.push(e); }
function feed(g,msg,cls,team){ ev(g,{type:'feed',msg,cls:cls||'',team:team||null}); }
function logE(g,e,data){ g.log.push(Object.assign({t:r2(g.rt),r:g.round,ev:e},data||{})); }
const humansInRole=(g,role)=>g.slots.filter(s=>s.cid&&roleOf(g,s.team)===role);
const humans=g=>g.slots.filter(s=>s.cid);

function resetRoundState(g){
  g.altars=ALTARS.map((d,i)=>({i,name:d.name,x:d.x,z:d.z,state:'dormant',chosen:false,decoy:false,prog:0,cp:0,seal:0,sealIdle:0,localized:false,lastN:1,startT:0,maxProg:0,doneT:null,selo:1,sealers:[],arrived:false,eco:false}));
  g.reagents.forEach(R=>{ R.has=true; R.t=0; });
  g.know=g.altars.map(()=>'?'); g.visit=g.altars.map(()=>-99); g.decoys=2; g.transferUsed=false; g.transferPara=-1; g.transferPor=''; g.rastros=[]; g.pegadas=[];
  g.proj=[]; g.pickups=[]; g.flares.forEach(f=>f.t=0); g.actors=[]; g.picks=[]; g.ready={}; g.buff={ess:0,selo:0};
  g.endAt=0; g.lastResolveT=0; g.rt=0; g.firstContact=null; g.momento=0; g.eco=null; g.suspeitaC={}; prepararNoite(g);
}
function enterPick(g,round){
  g.round=round; resetRoundState(g); g.phase='pick'; g.phaseT=CFG.timers.pick;
  if(!humansInRole(g,'C').length){ g.picks=[0,1,2,3,4,5].sort(()=>Math.random()-.5).slice(0,3); confirmPicks(g); }
}
function confirmPicks(g){
  g.altars.forEach(A=>A.chosen=false); g.picks.forEach(i=>g.altars[i].chosen=true);
  logE(g,'choose',{altars:g.picks.map(i=>g.altars[i].name)});
  g.phase='intro'; g.phaseT=CFG.timers.intro; g.ready={};
  humans(g).forEach(s=>{ const role=roleOf(g,s.team); if(!g.cls[s.cid]||CLASSES[g.cls[s.cid]].team!==role) g.cls[s.cid]=CLASS_BY_TEAM[role][0]; });
}
function allReady(g){ const hs=humans(g); return hs.length>0&&hs.every(s=>g.ready[s.cid]); }
function startPlay(g){
  g.actors=[];
  const taken={H:[],C:[]};
  g.slots.forEach((s,si)=>{ if(!s.cid) return; const role=roleOf(g,s.team); const c=g.cls[s.cid]; taken[role].push(c);
    const f=FEITICOS[g.feit[s.cid]]&&FEITICOS[g.feit[s.cid]].team===role?g.feit[s.cid]:FEIT_BY_TEAM[role][0]; g.actors.push(makeActor(g,s,si,role,c,f)); });
  g.slots.forEach((s,si)=>{ if(s.cid) return; const role=roleOf(g,s.team); const opts=CLASS_BY_TEAM[role], nt=opts.filter(o=>!taken[role].includes(o)); const c=nt.length?nt[Math.floor(Math.random()*nt.length)]:opts[si%opts.length]; taken[role].push(c);
    const usados=g.actors.filter(x=>x.team===role).map(x=>x.feitico), livres=FEIT_BY_TEAM[role].filter(f=>!usados.includes(f));
    g.actors.push(makeActor(g,s,si,role,c,livres[Math.floor(Math.random()*livres.length)]||FEIT_BY_TEAM[role][0])); });
  g.momento=-1; prepararNoite(g);
  g.actors.forEach((a,k)=>spawn(g,a,true,k));
  g.phase='play'; g.rt=0; g.lastResolveT=0;
  logE(g,'round_start',{teams:g.actors.map(a=>({name:a.name,team:a.team,cls:a.cls,human:a.human}))});
  feed(g,'O Crepúsculo começou. Prepare-se: os altares despertam na Vigília.','big');
}
function makeActor(g,slot,si,role,cls,feit){
  const C=CLASSES[cls];
  return {id:g.nid++,slot:si,cid:slot.cid||null,human:!!slot.cid,name:slot.name,team:role,cls,maxHp:C.hp,hp:C.hp,speed:C.speed,
    x:0,z:0,yaw:0,pitch:0,st:'alive',downT:0,respawnT:0,deaths:0,dc:0,revUsed:false,reag:0,hitT:-9,att:{},fireCd:0,ammo:8,reserve:48,reloadT:0,
    fervor:100,sanity:100,invuln:0,sealing:-1,hold:{key:'',t:0,max:0},lantern:false,carga:100,flares:2,sensorCd:0,abilCd:0,lastShotT:-9,ataqueT:-9,moving:false,
    panicT:-9,blindT:0,stunT:0,runeT:0,shield:0,shieldT:0,pushedT:-9,pingT:-9,hist:[],feitico:feit,obolos:g.lab.economia?CFG.obolosInicio:0,veuT:0,slowT:0,revelT:0,curaT:0,amuleto:false,
    inp:{mx:0,mz:0,sp:false,ag:false,yaw:0,pitch:0,fire:false,use:false,use2:false,viewT:0,c:{q:0,f:0,g:0,r:0,ab:0}},lc:{q:0,f:0,g:0,r:0,ab:0},ag:false,
    ai:{path:[],goal:null,goalKey:'',repath:0,stuckT:0,lx:0,lz:0,alert:null,target:null,wait:0,decoyUsed:false,strafe:1,strafeT:0,patrol:null,patrolT:0}};
}
function spawn(g,a,first,k){
  const s=SPAWN[a.team], sameTeam=g.actors.filter(b=>b.team===a.team); const idx=first?sameTeam.indexOf(a):Math.floor(Math.random()*2);
  a.x=s.x+rand(-.6,.6); a.z=s.z+(idx===0?-2.5:2.5); a.yaw=s.yaw; a.pitch=0; a.inp.yaw=a.yaw; a.inp.pitch=0;
  a.hp=a.maxHp; a.st='alive'; a.invuln=2; a.sealing=-1; a.hold={key:'',t:0,max:0}; a.ammo=8; a.reserve=a.cls==='soldado'?64:48; a.fervor=100; a.reloadT=0;
  a.blindT=0; a.stunT=0; a.runeT=0; a.shield=0; a.hist=[]; a._pegada=null; a.ai.path=[]; a.ai.goal=null; a.ai.goalKey=''; a.ai.alert=null;
  if(first){ a.sanity=100; a.flares=2; a.carga=100; a.reag=0; a.deaths=0; a.dc=0; a.revUsed=false; a.abilCd=0; }
}
function timeoutPhase(g){
  if(g.phase==='pick'){ while(g.picks.length<3){ const i=Math.floor(Math.random()*6); if(!g.picks.includes(i)) g.picks.push(i); } confirmPicks(g); }
  else if(g.phase==='intro') startPlay(g);
  else if(g.phase==='summary') afterSummary(g);
}
function afterSummary(g){ if(g.round===1&&(g.opts&&g.opts.noites||CFG.noites)>1) enterPick(g,2); else { g.phase='final'; g.phaseT=0; } }
function act(g,cid,a){
  const slot=g.slots.find(s=>s.cid===cid); if(!slot||!a) return;
  const role=roleOf(g,slot.team);
  if(a.type==='pick'&&g.phase==='pick'&&role==='C'){ const i=a.i|0; if(i<0||i>5) return; if(g.picks.includes(i)) g.picks=g.picks.filter(k=>k!==i); else if(g.picks.length<3) g.picks.push(i); }
  if(a.type==='pickRandom'&&g.phase==='pick'&&role==='C'){ g.picks=[0,1,2,3,4,5].sort(()=>Math.random()-.5).slice(0,3); }
  if(a.type==='pickConfirm'&&g.phase==='pick'&&role==='C'&&g.picks.length===3) confirmPicks(g);
  if(a.type==='cls'&&g.phase==='intro'&&CLASSES[a.cls]&&CLASSES[a.cls].team===role){ g.cls[cid]=a.cls; }
  if(a.type==='feit'&&g.phase==='intro'&&FEITICOS[a.f]&&FEITICOS[a.f].team===role){ g.feit[cid]=a.f; }
  if(a.type==='comprar'&&g.phase==='play') comprar(g,cid,a.npc,a.item);
  if(a.type==='ready'&&(g.phase==='intro'||g.phase==='summary')){ g.ready[cid]=true; if(allReady(g)){ if(g.phase==='intro') startPlay(g); else afterSummary(g); } }
  if(a.type==='restart'&&g.phase==='final'){ g.scores={A:null,B:null}; g.stats={}; enterPick(g,1); }
  // ping: marca um ponto, um altar ou um inimigo para a própria equipe (no máximo um por segundo)
  if(a.type==='ping'&&g.phase==='play'){ const me=g.actors.find(b=>b.cid===cid); if(!me||me.st==='dead'||g.t-me.pingT<1) return;
    const x=+a.x, z=+a.z; if(!isFinite(x)||!isFinite(z)) return; me.pingT=g.t;
    const k=a.k==='altar'||a.k==='inimigo'||a.k==='perigo'?a.k:'ponto';
    const px=r2(clamp(x,-HALF.x,HALF.x)), pz=r2(clamp(z,-HALF.z,HALF.z));
    ev(g,{type:'ping',a:me.id,team:me.team,x:px,z:pz,k,i:k==='altar'?clamp(a.i|0,0,5):-1}); logE(g,'ping',{who:me.name,team:me.team,k});
    g.pedidos=(g.pedidos||0)+1; for(const o of g.actors) if(o!==me&&o.team===me.team&&!o.human&&o.st==='alive') o.ai.pedido={x:px,z:pz,k,ate:g.t+(k==='inimigo'?12:30),chegou:-1,n:g.pedidos}; }
  // frases rápidas para a equipe (0 ajuda, 1 indo, 2 recuar, 3 inimigo aqui, 4 obrigado, 5 purgar/iniciar); o bot aliado atende a ajuda e o recuo
  if(a.type==='fala'&&g.phase==='play'){ const me=g.actors.find(b=>b.cid===cid), f=a.i|0; if(!me||me.st==='dead'||f<0||f>5||g.t-me.pingT<1) return; me.pingT=g.t;
    ev(g,{type:'fala',a:me.id,team:me.team,f,x:r2(me.x),z:r2(me.z)}); logE(g,'fala',{who:me.name,team:me.team,f});
    if(f===0||f===3){ g.pedidos=(g.pedidos||0)+1; for(const o of g.actors) if(o!==me&&o.team===me.team&&!o.human&&o.st==='alive') o.ai.pedido={x:r2(me.x),z:r2(me.z),k:'ponto',ate:g.t+20,chegou:-1,n:g.pedidos}; }
    if(f===2) for(const o of g.actors) if(o!==me&&o.team===me.team&&!o.human) o.ai.pedido=null; }
}
function dropHuman(g,cid){
  const s=g.slots.find(s=>s.cid===cid); if(!s) return; s.cid=null; if(g.phase!=='final') logE(g,'saiu',{who:s.name,fase:g.phase});
  const a=g.actors.find(a=>a.cid===cid); if(a){ a.cid=null; a.human=false; a.inp.fire=false; a.inp.use=false; a.inp.ag=false; a.ag=false; }
  delete g.ready[cid];
  if((g.phase==='intro'||g.phase==='summary')&&allReady(g)){ if(g.phase==='intro') startPlay(g); else afterSummary(g); }
}
// Devolve a vaga (e o personagem, com vida, reagentes e posição) a quem reconectou.
function reclaimHuman(g,slotIdx,cid,name){
  const s=g.slots[slotIdx]; if(!s||s.cid) return false;
  s.cid=cid; if(name) s.name=name;
  const a=g.actors.find(a=>a.slot===slotIdx);
  if(a){ a.cid=cid; a.human=true; if(name) a.name=name; a.inp={mx:0,mz:0,sp:false,ag:false,yaw:a.yaw,pitch:0,fire:false,use:false,use2:false,viewT:0,c:{q:0,f:0,g:0,r:0,ab:0}}; a.lc={q:0,f:0,g:0,r:0,ab:0}; a.hold={key:'',t:0,max:0};
    g.cls[cid]=a.cls; }
  else { const role=roleOf(g,s.team); g.cls[cid]=CLASS_BY_TEAM[role][0]; }
  logE(g,'reconnect',{who:s.name});
  return true;
}


// versão do jogo e do formato do registro (sobe quando muda o que vai no registro; os relatórios antigos continuam legíveis)
const VERSAO='6.1', REGISTRO=3;

// ============ A NOITE: momentos, tarefas, pontos de interação, mercadores ============
// Os pontos ficam sobre nós de navegação: garante que todo ponto é alcançável e livre de parede.
const pontoNav=(x,z)=>{ let m=null,md=1e9; for(let i=0;i<WP.length;i++){ if(NAV.comp[i]!==NAV.principal) continue; const w=WP[i], d=Math.hypot(w.x-x,w.z-z); if(d<md){ md=d; m=w; } } return {x:m.x,z:m.z}; };
// túmulos a profanar: seis lápides do cemitério, espalhadas pelas fileiras
const TUMBAS=LAPIDES.filter((w,i)=>i%Math.max(1,Math.floor(LAPIDES.length/6))===0).slice(0,6).map(w=>pontoNav((w.x1+w.x2)/2,w.z1-1.2));
const PONTOS_DEF={
  pista:PISTA_PTS.map(([x,z])=>pontoNav(x,z)),
  sentinela:SENTINELA_PTS.map(([x,z])=>pontoNav(x,z)),
  erva:ERVA_PTS.map(([x,z])=>pontoNav(x,z)),
  tumulo:TUMBAS,
};
const NPCS=[
  {id:'ermitao',nome:'o Ermitão',...pontoNav(CAB[0]-1,CAB[1]),time:'H',vende:['municao','flare','oleo','pocao','amuleto']},
  {id:'carpideira',nome:'a Carpideira',...pontoNav(ORA[0]+1,ORA[1]),time:'C',vende:['reagente','cinza','pocao','amuleto']},
  {id:'mercador',nome:'o Mercador sem Rosto',...pontoNav(ENCR[0]+6,ENCR[1]-4),time:null,vende:['reagente','municao','pocao']}];
const ITENS={
  reagente:{nome:'Reagente',preco:3,time:'C',desc:'Um frasco para consagrar ou iniciar um ritual.'},
  cinza:{nome:'Cinza de Chamariz',preco:3,time:'C',desc:'Mais um Chamariz para a equipe.'},
  municao:{nome:'Munição benta',preco:1,time:'H',desc:'16 balas na reserva.'},
  flare:{nome:'Sinalizador',preco:2,time:'H',desc:'Mais um sinalizador de luz.'},
  oleo:{nome:'Óleo de lamparina',preco:1,time:'H',desc:'Enche a carga da Lanterna e do EVP.'},
  pocao:{nome:'Tônico amargo',preco:2,time:null,desc:'Recupera 60 de vida em 4 s.'},
  amuleto:{nome:'Amuleto',preco:6,time:null,momento:1,desc:'+25 de vida máxima até o amanhecer. Um por noite.'}};
const TAREFAS=[
  {H:[{id:'pistas',nome:'Recolha pistas nos santuários da floresta',meta:3,rec:3},{id:'sentinelas',nome:'Acenda sentinelas nos caminhos dos altares: elas denunciam consagrações por perto',meta:2,rec:3}],
   C:[{id:'ervas',nome:'Colha ervas-noturnas na floresta',meta:4,rec:3},{id:'tumulos',nome:'Profane túmulos no cemitério: cada um rende um reagente',meta:2,rec:3}]},
  {H:[{id:'purgar',nome:'Purgue um altar consagrado',meta:1,rec:4},{id:'selar',nome:'Entre num círculo e comece a selar',meta:1,rec:5}],
   C:[{id:'consagrar',nome:'Consagre altares',meta:2,rec:4},{id:'ritual',nome:'Complete um ritual',meta:1,rec:5}]},
  {H:[{id:'final',nome:'Sele dois dos três rituais ou resista até o amanhecer',meta:0,rec:0}],
   C:[{id:'final',nome:'Vença dois dos três rituais antes do amanhecer',meta:0,rec:0}]}];
// o fim do Crepúsculo vem do laboratório (180 s no padrão)
const momentoDe=(rt,crep)=>{ const M=CFG.momentos; for(let i=0;i<M.length;i++) if(rt<(i===0&&crep?crep:M[i].ate)) return i; return M.length-1; };
const fendas=g=>g.altars.filter(A=>A.state==='fenda').length;
const farois=g=>g.altars.filter(A=>A.state==='farol').length;
// Três rituais por noite, no máximo, e um de cada vez. Devolve o motivo do bloqueio, ou null se pode começar.
function bloqueioRitual(g){
  if(g.altars.some(A=>A.state==='active')) return 'Já há um ritual em andamento';
  const r=fendas(g)+farois(g);
  if(r>=3) return 'Os três rituais desta noite já aconteceram';
  if(g.momento<1) return 'Este altar só desperta na Vigília';
  if(r>=2&&g.momento<2) return 'O último ritual só pode começar na Hora Morta';
  return null; }
function prepararNoite(g){
  const pistas=PONTOS_DEF.pista.slice().sort(()=>Math.random()-.5).slice(0,3);
  g.pontos=[...pistas.map(p=>({tipo:'pista',x:p.x,z:p.z,ativo:true})),
    ...PONTOS_DEF.sentinela.map(p=>({tipo:'sentinela',x:p.x,z:p.z,ativo:true,acesa:false})),
    ...PONTOS_DEF.erva.map(p=>({tipo:'erva',x:p.x,z:p.z,ativo:true,t:0})),
    ...PONTOS_DEF.tumulo.map(p=>({tipo:'tumulo',x:p.x,z:p.z,ativo:true}))].map((p,i)=>Object.assign(p,{i}));
  g.sal=[]; g.realocar=[]; g.tarefas={H:[],C:[]}; g.rastros=[]; g.pegadas=[];
}
function novoMomento(g,mi){
  g.momento=mi; const T=TAREFAS[mi]; g.tarefas={H:T.H.map(t=>({...t,prog:0,feita:false})),C:T.C.map(t=>({...t,prog:0,feita:false}))};
  if(mi>0){ ev(g,{type:'momento',i:mi,nome:CFG.momentos[mi].nome}); feed(g,mi===1?'A Vigília começou: os altares despertam.':'A Hora Morta chegou: rituais mais rápidos, a névoa se fecha.','big'); }
  logE(g,'momento',{i:mi,nome:CFG.momentos[mi].nome});
}
function tarefaProg(g,role,id,n){ const t=(g.tarefas&&g.tarefas[role]||[]).find(t=>t.id===id); if(!t||t.feita||!t.meta) return;
  t.prog=Math.min(t.meta,t.prog+(n||1)); if(t.prog>=t.meta){ t.feita=true; const paga=g.lab.economia; if(paga) g.actors.filter(a=>a.team===role).forEach(a=>a.obolos+=t.rec);
    feed(g,paga?`Tarefa cumprida: ${t.nome}. +${t.rec} óbolos para cada um.`:`Tarefa cumprida: ${t.nome}.`,role==='H'?'h':'c',role); ev(g,{type:'tarefa',team:role,id}); logE(g,'tarefa',{team:role,id}); } }
const ganhaObolos=(g,a,n)=>{ if(g.lab.economia) a.obolos+=n; };
function comprar(g,cid,npcId,itemId){
  const a=g.actors.find(x=>x.cid===cid), n=NPCS.find(x=>x.id===npcId), it=ITENS[itemId];
  const nega=msg=>{ if(a) ev(g,{type:'feed',msg,cls:'',to:a.id}); };
  if(!a||!n||!it||a.st!=='alive'||!g.lab.economia) return; if(dist(a,n)>3.2) return nega('Chegue mais perto para negociar.');
  if(!n.vende.includes(itemId)||(n.time&&n.time!==a.team)||(it.time&&it.time!==a.team)) return nega('Este item não é para você.');
  if(it.momento&&g.momento<it.momento) return nega(`${it.nome} só aparece a partir da Vigília.`);
  if(a.obolos<it.preco) return nega(`Faltam ${it.preco-a.obolos} óbolos.`);
  if(itemId==='reagente'&&a.reag>=CFG.carry) return nega('Você já carrega 2 reagentes.');
  if(itemId==='municao'&&a.reserve>=80) return nega('Sua reserva de munição já está cheia.');   // antes cobrava e não dava nada
  if(itemId==='flare'&&a.flares>=4) return nega('Você já carrega 4 sinalizadores.');
  if(itemId==='amuleto'&&a.amuleto) return nega('Só um amuleto por noite.');
  if(itemId==='cinza'&&g.decoys>=4) return nega('A equipe já tem Chamarizes demais.');
  a.obolos-=it.preco;
  if(itemId==='reagente') a.reag++; else if(itemId==='cinza') g.decoys++; else if(itemId==='municao') a.reserve=Math.min(80,a.reserve+16);
  else if(itemId==='flare') a.flares=Math.min(4,a.flares+1); else if(itemId==='oleo') a.carga=100; else if(itemId==='pocao') a.curaT=4;
  else if(itemId==='amuleto'){ a.amuleto=true; a.maxHp+=25; a.hp+=25; }
  ev(g,{type:'feed',msg:`Comprou ${it.nome.toLowerCase()} com ${n.nome}.`,cls:a.team==='H'?'h':'c',to:a.id}); ev(g,{type:'sfx',k:'collect',x:r2(a.x),z:r2(a.z),a:a.id});
  logE(g,'compra',{who:a.name,item:itemId,npc:npcId});
}
function passoNoite(g,dt){
  const mi=momentoDe(g.rt,g.lab.crepusculo); if(mi!==g.momento) novoMomento(g,mi);
  for(const p of g.pontos) if(p.tipo==='erva'&&!p.ativo){ p.t-=dt; if(p.t<=0) p.ativo=true; }
  for(let i=g.realocar.length-1;i>=0;i--) if(g.rt>=g.realocar[i]){ g.realocar.splice(i,1);
    const livres=g.altars.filter(A=>!A.chosen&&A.state==='dormant'); if(livres.length){ const N=livres[Math.floor(Math.random()*livres.length)]; N.chosen=true; feed(g,`Um novo altar foi escolhido: ${N.name}.`,'c','C'); logE(g,'realocado',{altar:N.name}); } }
  for(let i=g.sal.length-1;i>=0;i--){ const q=g.sal[i]; q.t-=dt; let gasto=q.t<=0;
    for(const e of g.actors){ if(gasto||e.team!=='C'||e.st!=='alive'||Math.hypot(e.x-q.x,e.z-q.z)>2.2) continue;
      e.slowT=3; e.revelT=6; e.veuT=0; gasto=true; feed(g,`O sal denunciou ${e.name}.`,'h','H'); ev(g,{type:'feed',msg:'Você pisou no sal: está lento e visível.',cls:'c',to:e.id}); logE(g,'sal',{who:e.name}); }
    if(gasto) g.sal.splice(i,1); }
  for(const a of g.actors){ for(const k of ['veuT','slowT','revelT']) if(a[k]>0) a[k]=Math.max(0,a[k]-dt);
    if(a.curaT>0&&a.st==='alive'){ a.curaT=Math.max(0,a.curaT-dt); a.hp=Math.min(a.maxHp,a.hp+15*dt); } }
}
// Pegadas dos Cultistas (só o Rastreador as vê) e marcas de arrasto da Transferência (qualquer Caçador lê de perto).
function rastrear(g){
  const temRastreador=g.actors.some(h=>h.team==='H'&&h.cls==='rastreador');
  if(temRastreador) for(const c of g.actors){ if(c.team!=='C'||c.st!=='alive') continue;
    const u=c._pegada, d=u?Math.hypot(c.x-u.x,c.z-u.z):0; if(!u||d>1.3){ const lado=u?-u.l:1; c._pegada={x:c.x,z:c.z,l:lado};
      if(u&&d<4){ const ux=(c.x-u.x)/d, uz=(c.z-u.z)/d; g.pegadas.push({x:r2(c.x+uz*.18*lado),z:r2(c.z-ux*.18*lado),yaw:r2(Math.atan2(ux,uz)),t:r2(g.t)}); } } }
  while(g.pegadas.length&&(g.t-g.pegadas[0].t>CFG.pegadaVida||g.pegadas.length>160)) g.pegadas.shift();
  for(const R of g.rastros){ if(g.rt-R.t>180) continue;
    for(const h of g.actors){ if(h.team!=='H'||h.st!=='alive'||Math.hypot(h.x-R.x,h.z-R.z)>9) continue; const P=g.altars[R.para], O=g.altars[R.de];
      if(!R.visto){ R.visto=true; feed(g,`Marcas de arrasto em ${O.name}: o culto levou o ritual daqui para outro altar.`,'big h','H'); logE(g,'rastro_visto',{altar:O.name,by:h.name}); }
      if(!R.lido&&h.cls==='rastreador'){ R.lido=true; if(semAura(g.know[P.i])) g.know[P.i]='desperto';
        feed(g,`${h.name} segue o arrasto: ele termina em ${P.name}.`,'big h','H'); logE(g,'rastro_lido',{para:P.name,by:h.name}); } } }
}
// sentinelas acesas denunciam consagrações e rituais por perto
function sentinelaAvisa(g,A,oque){ const s=g.pontos.find(p=>p.tipo==='sentinela'&&p.acesa&&Math.hypot(p.x-A.x,p.z-A.z)<24); if(!s) return;
  if(semAura(g.know[A.i])) g.know[A.i]='desperto'; feed(g,`Uma sentinela tocou: ${oque} em ${A.name}.`,'big h','H'); logE(g,'sentinela',{altar:A.name}); }

// O que a caça sabe de cada altar (g.know): '?' nada; 'visto' passou perto e não havia nada desperto (não descarta);
// 'descartado' um sinal disse que o culto não o escolheu (ou um Chamariz foi queimado ali); 'purgado' era do culto e foi
// purgado; 'desperto' há vozes (consagrado ou Chamariz); 'confirmado' resíduo de reagente; 'chamariz' sem resíduo; 'ativo'.
const semAura=k=>k==='?'||k==='visto'||k==='descartado'||k==='purgado'; // nada desperto conhecido ali
// ============ LUZ E SANIDADE ============
function lightAt(g,x,z){
  let L=0;
  for(const [cx,cz] of CANDLES){ const d=Math.hypot(x-cx,z-cz); if(d<8.5) L+=1-d/8.5; }
  const Z=zonaDe(x,z);
  if(Z) L+=Z.luar; // luar das zonas: o Claustro e a capela desabada não têm teto; o promontório e as pontes, céu aberto
  else { // na floresta, o luar só chega nas clareiras e ao longo das trilhas; a mata fechada é breu
    let lua=0; for(const c of CLAREIRAS){ const d=Math.hypot(x-c.x,z-c.z); if(d<c.r+2) lua=Math.max(lua,.55*clamp((c.r+2-d)/4,0,1)); }
    if(lua<.42) for(const sg of SEG_TRILHA){ if(sg.estreita) continue; const d=distSeg(x,z,sg); if(d<sg.w/2+1){ lua=Math.max(lua,.42*clamp((sg.w/2+1-d)/1.5,0,1)); if(lua>=.42) break; } }
    L+=lua; }
  for(const f of g.flares) if(f.t>0){ const d=Math.hypot(x-f.x,z-f.z); if(d<12) L+=1.4*(1-d/12); }
  for(const A of g.altars){ if(A.state==='farol'){ const d=Math.hypot(x-A.x,z-A.z); if(d<12) L+=1.3*(1-d/12); } if(A.state==='fenda'){ const d=Math.hypot(x-A.x,z-A.z); if(d<9) L-=0.8*(1-d/9); } }
  return L;
}
function updateSanity(g,a,dt){
  const L=lightAt(g,a.x,a.z)+(a.lantern?.3:0); let d=0;
  const nearRit=g.altars.some(A=>(A.state==='active'||(A.state==='awake'&&!A.decoy))&&dist(a,A)<15);
  const ally=g.actors.some(b=>b!==a&&b.team===a.team&&b.st==='alive'&&dist(a,b)<9);
  const alone=!g.actors.some(b=>b!==a&&b.team===a.team&&b.st==='alive'&&dist(a,b)<16);
  if(a.team==='H'){ d+=L>=.35?1.4:-.7; if(nearRit) d-=1.5; if(ally) d+=1; if(alone) d-=.25; }
  else { d+=L<.35?1:-.35; if(nearRit) d+=2; if(alone) d-=.3; for(const f of g.flares) if(f.t>0&&Math.hypot(a.x-f.x,a.z-f.z)<10) d-=3; }
  for(const A of g.altars){ const dd=dist(a,A); if(A.state==='fenda'&&dd<9) d+=a.team==='H'?-2:3; if(A.state==='farol'&&dd<12) d+=a.team==='H'?3:-2; }
  a.sanity=clamp(a.sanity+d*dt,0,100);
  if(a.sanity<15) a.panicT=g.t;
}

// ============ MOVIMENTO (também usado na predição do cliente) ============
function moveHuman(g,a,inp,dt){
  if(inp){ a.inp.mx=clamp(+inp.mx||0,-1,1); a.inp.mz=clamp(+inp.mz||0,-1,1); a.inp.sp=!!inp.sp; if(isFinite(inp.yaw)) a.inp.yaw=+inp.yaw; if(isFinite(inp.pitch)) a.inp.pitch=clamp(+inp.pitch,-1.5,1.5);
    a.inp.fire=!!inp.fire; a.inp.use=!!inp.use; a.inp.use2=!!inp.use2; a.inp.ag=!!inp.ag; if(isFinite(inp.viewT)) a.inp.viewT=+inp.viewT; if(inp.c) a.inp.c=inp.c; }
  a.yaw=a.inp.yaw; a.pitch=a.inp.pitch; a.ag=!!a.inp.ag&&a.st==='alive';
  let mx=a.inp.mx, mz=a.inp.mz; const len=Math.hypot(mx,mz);
  a.moving=false;
  if(a.st==='dead'||a.stunT>0||len<0.01) return;
  mx/=len; mz/=len;
  let sp=a.speed*(a.slowT>0?.6:1); if(a.st==='down') sp=.8; else { if(a.inp.sp&&mz<0&&a.sealing<0&&!a.ag) sp=CFG.sprint*(a.speed/CFG.speed); if(a.ag) sp*=CFG.agachadoVel; if(a.sealing>=0) sp*=.4; if(a.hp<a.maxHp*.5) sp*=.9; }
  const s=Math.sin(a.yaw),c=Math.cos(a.yaw);
  moverSeguro(a,a.x+(mx*c+mz*s)*sp*dt,a.z+(-mx*s+mz*c)*sp*dt,.4); a.moving=true;
}

// ============ COMBATE ============
const enemiesOf=(g,a)=>g.actors.filter(b=>b.team!==a.team);
function recentAttackers(g,a){ let n=0; for(const k in a.att) if(g.t-a.att[k]<1) n++; return n; }
function posAt(a,T){ const h=a.hist; if(!h.length||T>=h[h.length-1][0]) return {x:a.x,z:a.z}; for(let i=h.length-1;i>0;i--){ if(h[i-1][0]<=T){ const k=(T-h[i-1][0])/Math.max(1e-6,h[i][0]-h[i-1][0]); return {x:lerp(h[i-1][1],h[i][1],k),z:lerp(h[i-1][2],h[i][2],k)}; } } return {x:h[0][1],z:h[0][2]}; }
function traceShot(g,a,ox,oy,oz,dx,dy,dz,T,forceMiss){ a.veuT=0;
  a.lastShotT=g.t; a.ammo--; if(a.ammo<=0) reload(g,a);
  ev(g,{type:'sfx',k:'shot',x:r2(a.x),z:r2(a.z),a:a.id});
  let tWall=raioParede(ox,oz,dx,dz,90,0,d=>oy+dy*d,BOX); // v6: túmulos, muretas, bancos e pedras param o tiro que passa abaixo deles
  if(dy<0){ const tf=-oy/dy; if(tf<tWall) tWall=tf; }
  let best=null,bt=tWall,head=false;
  if(!forceMiss) for(const e of enemiesOf(g,a)){ if(e.st==='dead') continue; const p=posAt(e,T);
    const fx=ox-p.x,fz=oz-p.z,A2=dx*dx+dz*dz,B=2*(fx*dx+fz*dz),C=fx*fx+fz*fz-.45*.45,disc=B*B-4*A2*C; if(disc<0||A2<1e-9) continue;
    const t=(-B-Math.sqrt(disc))/(2*A2); if(t<0||t>bt) continue; const y=oy+dy*t, top=alturaAlvo(e); if(y<0||y>top) continue; best=e; bt=t; head=e.st==='alive'&&y>top-.45; }
  ev(g,{type:'tracer',a:a.id,x0:r2(ox),y0:r2(oy-.2),z0:r2(oz),x1:r2(ox+dx*bt),y1:r2(oy+dy*bt),z1:r2(oz+dz*bt),alvo:best?best.id:null,cab:!!head});
  if(best){ const d=bt, k=clamp(1-(d-CFG.quedaPerto)/(CFG.quedaLonge-CFG.quedaPerto)*(1-CFG.quedaMin),CFG.quedaMin,1);
    damage(g,best,Math.round((head?26:CFG.quedaDano)*k),a); }
}
// altura de quem leva o tiro: em pé, agachado (v6) ou caído. A cabeça são os 45 cm de cima.
function alturaAlvo(e){ return e.st==='down'?.6:e.ag?CFG.agachadoAltura:1.95; }
const alturaOlho=a=>a.ag?CFG.agachadoOlho:1.62;
// intervalo entre tiros do jogador (Caçador e Cultista); o cliente usa o mesmo número para prever o tiro
const CADENCIA_HUMANA=.42;
function fireHuman(g,a){
  a.fireCd=CADENCIA_HUMANA; const y=a.inp.yaw,p=a.inp.pitch; const dx=-Math.sin(y)*Math.cos(p),dy=Math.sin(p),dz=-Math.cos(y)*Math.cos(p);
  const T=clamp(a.inp.viewT||g.t,g.t-CFG.compensacaoMax,g.t);
  traceShot(g,a,a.x,alturaOlho(a),a.z,dx,dy,dz,T,false);
}
// dificuldade dos bots (só no jogo local): mira, dispersão dos sigilos, ritmo de tiro e alcance da vista. Normal = 1 em tudo.
const DIF={treino:{mira:.12,disp:3.2,ritmo:2.4,vista:.45},facil:{mira:.6,disp:1.7,ritmo:1.4,vista:.75},normal:{mira:1,disp:1,ritmo:1,vista:1},dificil:{mira:1.22,disp:.6,ritmo:.85,vista:1.15}};
// vale só para os bots do lado contrário ao dos jogadores humanos: o aliado bot continua no normal
function difDe(g,b){ const d=DIF[g&&g.opts&&g.opts.dificuldade]; if(!d||d===DIF.normal) return DIF.normal; return b&&g.actors.some(o=>o.human&&o.team===b.team)?DIF.normal:d; }
function fireBotAt(g,a,tx,ty,tz,miss){
  a.fireCd=(.55+rand(0,.2))*difDe(g,a).ritmo; const oy=alturaOlho(a); let dx=tx-a.x,dy=ty-oy,dz=tz-a.z; const l=Math.hypot(dx,dy,dz)||1; dx/=l; dy/=l; dz/=l;
  traceShot(g,a,a.x,oy,a.z,dx,dy,dz,g.t,miss);
}
let PID=1;
function castSigil(g,a,dx,dy,dz){ a.veuT=0;
  a.lastShotT=g.t; a.ataqueT=g.t; a.fireCd=a.human?CADENCIA_HUMANA:(1+rand(0,.3))*difDe(g,a).ritmo; a.fervor-=CFG.sigilCusto;
  ev(g,{type:'sfx',k:'sigil',x:r2(a.x),z:r2(a.z),a:a.id});
  const rx=Math.cos(a.yaw), rz=-Math.sin(a.yaw); const ox=a.x+rx*.3, oy=alturaOlho(a)-.17, oz=a.z+rz*.3;
  const tx=a.x+dx*40, ty=alturaOlho(a)+dy*40, tz=a.z+dz*40; let vx=tx-ox,vy=ty-oy,vz=tz-oz; const l=Math.hypot(vx,vy,vz)||1; const sp=CFG.sigilVel;
  g.proj.push({id:PID++,x:ox,y:oy,z:oz,vx:vx/l*sp,vy:vy/l*sp,vz:vz/l*sp,owner:a.id,team:a.team,life:2.5,dmg:CFG.sigilDano});
}
function reload(g,a){ if(a.team!=='H'||a.reloadT>0||a.ammo>=8||a.reserve<=0) return; a.reloadT=1.6; ev(g,{type:'reload',a:a.id,x:r2(a.x),z:r2(a.z)}); }
function damage(g,t,amt,src){
  if(t.st==='dead'||t.invuln>0) return;
  if(!g.firstContact){ g.firstContact=r2(g.rt); logE(g,'first_contact',{}); }
  if(t.st==='down'){ t.downT-=amt/8; t.att[src.id]=g.t; return; }
  // executar exige calma: quem leva dano recomeça a execução
  if(CFG.danoInterrompeExecucao&&t.hold.key.startsWith('exe')){ if(t.hold.t>0){ logE(g,'execucao_interrompida',{who:t.name,by:src.name}); if(t.human) ev(g,{type:'feed',msg:'Você levou dano: a execução recomeçou.',cls:'',to:t.id});
      const B=g.actors.find(x=>'exe'+x.id===t.hold.key); if(B&&B.st==='down') B.salvoPor=src.name; } t.hold={key:'',t:0,max:0}; }
  let absorvido=0; if(t.shield>0){ absorvido=Math.min(t.shield,amt); t.shield-=absorvido; amt-=absorvido; }
  t.hp-=amt; t.hitT=g.t; t.att[src.id]=g.t; t.veuT=0; disputaDano(g,t,src,amt);
  // escudo: o golpe foi (todo ou em parte) absorvido pela armadura de osso; o cliente mostra como bloqueio
  ev(g,{type:'hurt',a:t.id,s:src.id,sx:r2(src.x),sz:r2(src.z)}); ev(g,{type:'hit',a:src.id,alvo:t.id,derrubou:t.hp<=0,escudo:absorvido>0&&amt<=0});
  if(!t.human) t.ai.alert={x:src.x,z:src.z,t:g.t};
  if(t.hp<=0) goDown(g,t,src);
}
function goDown(g,t,src){
  if(t.revUsed){ kill(g,t,src); return; }
  if(src&&src.team!==t.team) ganhaObolos(g,src,1);
  const selando=t.sealing, canal=t.team==='C'?g.altars.find(A=>A.state==='active'&&inCircle(t,A)):null;
  t.st='down'; t.hp=0; t.downT=CFG.downTime; t.sealing=-1; t.hold={key:'',t:0,max:0}; t.lantern=false; t.salvoPor=null;
  feed(g,`${src.name} derrubou ${t.name}`,t.team==='C'?'h':'c'); logE(g,'down',{who:t.name,team:t.team,by:src.name,x:r2(t.x),z:r2(t.z)});
  for(const A of g.altars){ if(A.state!=='active'||!A.arrived||!A.obs) continue; if(dist(t,A)<12) dsp(A.obs,t.id).quedas++; if(dist(src,A)<12) dsp(A.obs,src.id).derrubou++; }
  if(selando>=0) interrupcao(g,'selo','queda',src,t,g.altars[selando]);
  else if(canal) interrupcao(g,'canal','queda',src,t,canal);
}
// Interrupção que se sente: quem causou e quem sofreu recebem o aviso, e o mostrador do círculo pisca. Um ritual que a
// caça ainda não localizou só é anunciado ao culto (derrubar alguém no círculo não revela o altar).
function interrupcao(g,tipo,causa,src,t,A){ if(!A) return;
  if(src.team==='C'&&A.obs) A.obs.defesa[src.id]=1;
  ev(g,{type:'interrupcao',tipo,causa,a:src.id,alvo:t.id,altar:A.i,team:tipo==='canal'&&!A.localized?'C':null});
  logE(g,'interrupcao',{tipo,causa,by:src.name,who:t.name,altar:A.name,prog:r2(A.prog),seal:r2(A.seal)}); }
// Lance da noite: uma jogada que decidiu ou quase decidiu um ritual. Vai para o registro (tela do fim da noite) e, ao
// vivo, para a equipe que fez a jogada (team) ou para todos.
function lance(g,tipo,quem,A,team,v){ quem=[...new Set(quem.filter(Boolean))];
  ev(g,{type:'lance',tipo,quem,altar:A?A.i:-1,team:team||null,v:v==null?null:v});
  logE(g,'lance',{tipo,quem,altar:A?A.name:'',team:team||null,v:v==null?null:v}); }
// registro da disputa no círculo: o que cada um fez a menos de 12 m de um ritual depois que a caça chegou
function dsp(O,id){ return O.d[id]||(O.d[id]={perto:0,circulo:0,selando:0,sobFogo:0,canal:0,atacando:0,dano:0,sofrido:0,quedas:0,derrubou:0}); }
function disputaDano(g,t,src,amt){ for(const A of g.altars){ if(A.state!=='active'||!A.arrived||!A.obs) continue;
    if(dist(t,A)<12||dist(src,A)<12){ dsp(A.obs,src.id).dano+=amt; dsp(A.obs,t.id).sofrido+=amt; } } }
function medirDisputa(g,A,ch,sealers,dt){ const O=A.obs; for(const c of ch) O.canal[c.id]=1; if(!A.arrived) return;
  for(const a of g.actors){ if(a.st!=='alive'||dist(a,A)>=12) continue; const D=dsp(O,a.id); D.perto+=dt;
    if(inCircle(a,A)) D.circulo+=dt;
    if(sealers.includes(a)){ D.selando+=dt; if(recentAttackers(g,a)>0) D.sobFogo+=dt; }
    if(ch.includes(a)) D.canal+=dt;
    if(g.t-a.ataqueT<.6||g.t-a.lastShotT<.6) D.atacando+=dt; } }
function kill(g,t,src,how){
  t.st='dead'; t.hp=0; t.deaths++; t.dc++; t.sealing=-1; t.hold={key:'',t:0,max:0}; t.lantern=false;
  t.respawnT=CFG.respawnMomento[Math.max(0,g.momento)]+Math.min(6,2*(t.dc-1));
  for(let k=0;k<t.reag;k++) dropPickup(g,'reag',t.x+rand(-.6,.6),t.z+rand(-.6,.6)); t.reag=0;
  dropPickup(g,t.team==='C'?'ess':'selo',t.x+rand(-.4,.4),t.z+rand(-.4,.4));
  const msg=how==='exec'?`${src.name} executou ${t.name}`:src?`${t.name} morreu`:`${t.name} sangrou até morrer`;
  feed(g,msg,t.team==='C'?'h':'c'); logE(g,'death',{who:t.name,team:t.team,how:how||'bleed',x:r2(t.x),z:r2(t.z)});
}
function dropPickup(g,kind,x,z){ g.pickups.push({id:PID++,kind,x:r2(x),z:r2(z),life:kind==='reag'?30:40}); }

// ============ HABILIDADES ============
function useAbility(g,a){
  if(a.abilCd>0||a.st!=='alive'||a.stunT>0) return false;
  const fx=-Math.sin(a.yaw), fz=-Math.cos(a.yaw), alvos=[];
  const F=a.feitico;
  if(F==='runa'){ a.runeT=8; ev(g,{type:'sfx',k:'rune',x:r2(a.x),z:r2(a.z),a:a.id}); }
  else if(F==='veu'){ a.veuT=5; ev(g,{type:'sfx',k:'rune',x:r2(a.x),z:r2(a.z),a:a.id}); }
  else if(F==='sal'){ const meus=g.sal.filter(q=>q.dono===a.id); if(meus.length>=2) g.sal.splice(g.sal.indexOf(meus[0]),1);
    g.sal.push({id:PID++,x:r2(a.x+fx*1.2),z:r2(a.z+fz*1.2),dono:a.id,t:60}); ev(g,{type:'sfx',k:'collect',x:r2(a.x),z:r2(a.z),a:a.id}); }
  else if(F==='empurrao'){ a.ataqueT=g.t; // empurrar é atacar: pausa a canalização de quem empurra
    let n=0; for(const e of enemiesOf(g,a)){ if(e.st!=='alive') continue; const dx=e.x-a.x,dz=e.z-a.z,d=Math.hypot(dx,dz); if(d>3.8||d<.01) continue; if((dx*fx+dz*fz)/d<.34) continue;
      const ux=dx/d,uz=dz/d; for(let s=0;s<8;s++) moverSeguro(e,e.x+ux*.5,e.z+uz*.5,.4);
      const sl=e.sealing, was=sl>=0; e.sealing=-1; e.hold={key:'',t:0,max:0}; e.pushedT=g.t; e.stunT=Math.max(e.stunT,.45); damage(g,e,10,a); n++; alvos.push(e.id);
      if(was){ logE(g,'push_seal',{who:e.name,by:a.name}); interrupcao(g,'selo',e.st==='alive'?'empurrao':'queda',a,e,g.altars[sl]); } }
    ev(g,{type:'sfx',k:'push',x:r2(a.x),z:r2(a.z),a:a.id});
  }
  else if(F==='flash'){
    for(const e of enemiesOf(g,a)){ if(e.st!=='alive') continue; const dx=e.x-a.x,dz=e.z-a.z,d=Math.hypot(dx,dz); if(d>14||d<.01) continue; if((dx*fx+dz*fz)/d<.8||!losClear(a,e)) continue; e.blindT=1.5; alvos.push(e.id); ev(g,{type:'blind',a:e.id}); }
    ev(g,{type:'sfx',k:'flash',x:r2(a.x+fx*3),z:r2(a.z+fz*3),a:a.id});
  }
  else if(F==='purificacao'){
    for(const e of enemiesOf(g,a)){ if(e.st!=='alive') continue; if(dist(a,e)>6||!losClear(a,e)) continue; e.stunT=1; e.runeT=0; e.veuT=0; e.shield=0; alvos.push(e.id); ev(g,{type:'stun',a:e.id}); }
    ev(g,{type:'sfx',k:'purify',x:r2(a.x),z:r2(a.z),a:a.id});
  }
  ev(g,{type:'hab',a:a.id,f:F,x:r2(a.x),z:r2(a.z),fx:r2(fx),fz:r2(fz),alvos});
  a.abilCd=FEITICOS[F].cd; logE(g,'ability',{who:a.name,feitico:F,alvos:alvos.length}); return true;
}

// ============ CONTEXTOS (E / T) ============
const inCircle=(a,A)=>Math.hypot(a.x-A.x,a.z-A.z)<=CFG.circleR;
function contexts(g,a){
  const out={e:null,t:null,info:null};
  if(!a||a.st!=='alive') return out;
  for(const b of g.actors){ if(b===a||b.hidden||b.st!=='down'||dist(a,b)>1.8) continue;
    out.e=b.team===a.team?{key:'rev'+b.id,type:'revive',b:b.id,label:`Segure E para reanimar ${b.name}`,time:CFG.revive}:{key:'exe'+b.id,type:'execute',b:b.id,label:`Segure E para executar ${b.name}`,time:g.lab.execucao};
    return out; }
  if(g.lab.economia) for(const n of NPCS){ if(dist(a,n)>2.6) continue; if(n.time&&n.time!==a.team){ out.info=`${n.nome[0].toUpperCase()+n.nome.slice(1)} não negocia com você`; return out; }
    out.e={key:'loja'+n.id,type:'loja',npc:n.id,label:`Aperte E para negociar com ${n.nome}`,time:0}; return out; }
  for(const p of g.pontos||[]){ if(!p.ativo||Math.hypot(a.x-p.x,a.z-p.z)>1.9) continue;
    if(a.team==='H'&&p.tipo==='pista'){ out.e={key:'pt'+p.i,type:'ponto',P:p.i,label:'Segure E para examinar o sinal antigo',time:2}; return out; }
    if(a.team==='H'&&p.tipo==='sentinela'&&!p.acesa){ out.e={key:'pt'+p.i,type:'ponto',P:p.i,label:'Segure E para acender a sentinela',time:4}; return out; }
    if(a.team==='C'&&p.tipo==='erva'){ out.e={key:'pt'+p.i,type:'ponto',P:p.i,label:'Segure E para colher a erva-noturna',time:2.5}; return out; }
    if(a.team==='C'&&p.tipo==='tumulo'){ out.e={key:'pt'+p.i,type:'ponto',P:p.i,label:'Segure E para profanar o túmulo',time:4}; return out; } }
  if(a.team==='H'){
    for(const A of g.altars) if(A.state==='active'&&inCircle(a,A)){ out.e={key:'seal'+A.i,type:'seal',A:A.i,label:'Segure E para selar',time:0}; return out; }
    for(const A of g.altars) if(A.state==='awake'&&dist(a,A)<3.4){ out.e={key:'purge'+A.i,type:'purge',A:A.i,label:'Segure E para purgar o altar',time:CFG.purge}; return out; }
    for(const R of g.reagents) if(R.has&&dist(a,R)<2){ out.e={key:'burn'+R.i,type:'burn',R:R.i,label:'Segure E para queimar o reagente',time:CFG.burn}; return out; }
  } else {
    for(const R of g.reagents) if(R.has&&dist(a,R)<2){ if(a.reag<CFG.carry) out.e={key:'col'+R.i,type:'collect',R:R.i,label:'Segure E para coletar',time:CFG.collect}; else out.info=`Você já carrega ${CFG.carry} reagentes`; return out; }
    for(const A of g.altars){ const d=dist(a,A);
      if(A.state==='active'&&inCircle(a,A)){ out.info=A.chosen?'Canalizando o ritual':''; return out; }
      if(d>=3.4) continue;
      if(A.chosen&&A.state==='dormant'){ if(a.reag>0) out.e={key:'cons'+A.i,type:'consecrate',A:A.i,label:'Segure E para consagrar (gasta 1 reagente)',time:CFG.consecrate}; else out.info='Você precisa de 1 reagente'; return out; }
      if(A.chosen&&A.state==='awake'&&!A.decoy){ const bl=bloqueioRitual(g); if(bl){ out.info=bl; return out; } }
      if(A.chosen&&A.state==='awake'&&!A.decoy){ if(a.reag>0||CFG.iniciarDeGraca) out.e={key:'start'+A.i,type:'start',A:A.i,label:'Segure E para iniciar o ritual (gasta 1 reagente)',time:CFG.start}; else out.info='Você precisa de 1 reagente'; return out; }
      if(!A.chosen&&A.state==='dormant'){
        if(g.decoys>0) out.e={key:'dec'+A.i,type:'decoy',A:A.i,label:`Segure E para plantar um Chamariz (restam ${g.decoys})`,time:CFG.decoy};
        const from=transferSource(g,a);
        if(!g.transferUsed&&a.reag>0&&from) out.t={key:'tr'+A.i,type:'transfer',A:A.i,from:from.i,label:`Segure T para transferir ${from.name} para cá (1 reagente, uma vez por noite)`,time:CFG.transfer};
        if(!out.e&&!out.t) out.info=g.transferUsed?'Sem Chamarizes restantes; a Transferência desta noite já foi usada':!from?'Sem Chamarizes restantes':'Sem Chamarizes. Para transferir para cá, você precisa de 1 reagente';
        return out; }
    }
  }
  return out;
}
function transferSource(g,a){ const c=g.altars.filter(A=>A.chosen&&A.state==='dormant'); if(!c.length) return null; c.sort((x,y)=>dist(a,y)-dist(a,x)); return c[0]; }
// A ação é conferida de novo na hora de acontecer: o jogador passa por contexts(), mas o bot decide antes e segura o E;
// nesse meio-tempo o parceiro pode ter pego o reagente, gasto o último Chamariz ou o ritual pode ter começado ali.
function acaoNegada(g,a,ctx,A,R){
  switch(ctx.type){
    case 'collect': return !R||!R.has?'reagente já foi pego':a.reag>=CFG.carry?'mãos cheias':null;
    case 'burn': return !R||!R.has?'reagente já foi pego':null;
    case 'consecrate': return !A||!A.chosen||A.state!=='dormant'?'altar não está pronto para consagrar':a.reag<1?'sem reagente':null;
    case 'start': return !A||!A.chosen||A.decoy||A.state!=='awake'?'altar não está desperto':bloqueioRitual(g)||(!CFG.iniciarDeGraca&&a.reag<1?'sem reagente':null);
    case 'decoy': return !A||A.chosen||A.state!=='dormant'?'altar não está livre':g.decoys<1?'sem Chamarizes':null;
    case 'purge': return !A||A.state!=='awake'?'altar não está desperto':null;
    case 'transfer': return !A||A.chosen||A.state!=='dormant'?'destino não está livre':null;
  }
  return null; }
function doAction(g,a,ctx){
  const A=ctx.A!=null?g.altars[ctx.A]:null, R=ctx.R!=null?g.reagents[ctx.R]:null, B=ctx.b!=null?g.actors.find(x=>x.id===ctx.b):null;
  const neg=acaoNegada(g,a,ctx,A,R); if(neg){ logE(g,'acao_negada',{who:a.name,tipo:ctx.type,motivo:neg}); return; }
  switch(ctx.type){
    case 'ponto': { const p=g.pontos[ctx.P]; if(!p||!p.ativo) break;
      if(p.tipo==='pista'){ p.ativo=false; ganhaObolos(g,a,1); tarefaProg(g,'H','pistas');
        // o sinal responde "onde NÃO está": descarta um altar que o culto não escolheu e que a equipe ainda não conhece
        const cand=g.altars.filter(A=>!A.chosen&&A.state==='dormant'&&(g.know[A.i]==='?'||g.know[A.i]==='visto')); const D=cand[Math.floor(Math.random()*cand.length)];
        if(D){ g.know[D.i]='descartado'; feed(g,`${a.name} leu um sinal antigo: ${D.name} não foi escolhido pelo culto.`,'h','H'); logE(g,'pista_info',{altar:D.name}); }
        else feed(g,`${a.name} examinou um sinal antigo; ele não diz nada novo.`,'h','H'); }
      else if(p.tipo==='sentinela'){ p.acesa=true; tarefaProg(g,'H','sentinelas'); feed(g,`${a.name} acendeu uma sentinela.`,'h','H'); ev(g,{type:'sfx',k:'flare',x:p.x,z:p.z,a:a.id}); }
      else if(p.tipo==='erva'){ p.ativo=false; p.t=50; ganhaObolos(g,a,1); tarefaProg(g,'C','ervas'); }
      else if(p.tipo==='tumulo'){ p.ativo=false; if(a.reag<CFG.carry) a.reag++; else ganhaObolos(g,a,2); tarefaProg(g,'C','tumulos'); feed(g,`${a.name} profanou um túmulo.`,'c','C'); }
      logE(g,'ponto',{tipo:p.tipo,by:a.name}); break; }
    case 'purge': { const was=A.decoy, isca=r2(A.iscaT||0); tarefaProg(g,'H','purgar'); A.state='dormant'; A.decoy=false; g.know[A.i]=was?'descartado':'purgado'; if(A.chosen) g.suspeitaC[A.i]=true; /* purgar um verdadeiro revela que era do culto; um Chamariz queimado, que não era */ // o culto recebe o aviso da purga
      feed(g,was?`Chamariz queimado em ${A.name}.`:`${A.name} foi purgado.`,'h','H'); feed(g,`Um altar foi purgado: ${A.name}.`,'c','C');
      logE(g,'purge',{altar:A.name,decoy:was,by:a.name,isca:was?isca:null});
      // o engano recompensado: o culto só fica sabendo quanto o Chamariz prendeu a caça quando ele é queimado
      if(was){ feed(g,`Seu Chamariz em ${A.name} prendeu a caça por ${Math.round(isca)} s.`,'c','C'); if(isca>=CFG.iscaLance) lance(g,'chamariz',[A.iscaDono],A,'C',Math.round(isca)); }
      A.iscaT=0; A.iscaDono=''; break; }
    case 'burn': R.has=false; R.t=CFG.regrow*1.2; logE(g,'burn',{by:a.name}); if(a.human) ev(g,{type:'feed',msg:'Reagente queimado.',cls:'h',to:a.id}); break;
    case 'collect': R.has=false; R.t=CFG.regrow; a.reag++; ev(g,{type:'sfx',k:'collect',x:R.x,z:R.z,a:a.id}); logE(g,'collect',{by:a.name}); if(a.human) ev(g,{type:'feed',msg:`Reagente coletado (${a.reag}/${CFG.carry}).`,cls:'c',to:a.id}); break;
    case 'consecrate': a.reag--; A.state='awake'; A.decoy=false; tarefaProg(g,'C','consagrar'); sentinelaAvisa(g,A,'consagração'); feed(g,`${A.name} consagrado.`,'c','C'); logE(g,'consecrate',{altar:A.name,by:a.name}); break;
    case 'start': if(!CFG.iniciarDeGraca) a.reag--; startRitual(g,A,a); break;
    case 'decoy': g.decoys--; A.state='awake'; A.decoy=true; A.iscaT=0; A.iscaDono=a.name; feed(g,`Chamariz plantado em ${A.name}.`,'c','C'); logE(g,'decoy',{altar:A.name,by:a.name}); break;
    case 'transfer': { const F=g.altars[ctx.from]; if(!F||!F.chosen||F.state!=='dormant'||g.transferUsed||a.reag<1) break; F.chosen=false; A.chosen=true; a.reag--; g.transferUsed=true; g.transferPara=A.i; g.transferPor=a.name;
      feed(g,`Transferência: ${A.name} substitui ${F.name}.`,'c','C'); logE(g,'transfer',{from:F.name,to:A.name,by:a.name});
      // a Transferência deixa rastro: marcas de arrasto no altar de origem por 3 min e um lamento audível por perto dos dois altares
      g.rastros.push({de:F.i,para:A.i,x:F.x,z:F.z,t:g.rt,visto:false,lido:false});
      ev(g,{type:'sfx',k:'arrasto',x:r2(F.x),z:r2(F.z),a:a.id}); ev(g,{type:'sfx',k:'arrasto',x:r2(A.x),z:r2(A.z),a:a.id}); break; }
    case 'revive': if(B&&B.st==='down'){ B.st='alive'; B.hp=Math.round(B.maxHp*.45); B.revUsed=true; B.invuln=1; B.downT=0; feed(g,`${a.name} reanimou ${B.name}`,B.team==='H'?'h':'c',B.team); logE(g,'revive',{who:B.name,by:a.name});
      if(B.salvoPor) lance(g,'resgate',[B.salvoPor,a.name],null,B.team,null); B.salvoPor=null; } break;
    case 'execute': if(B&&B.st==='down'){ kill(g,B,a,'exec'); } break;
  }
}
function holdStep(g,a,ctx,dt){
  if(a.hold.key!==ctx.key){ a.hold.key=ctx.key; a.hold.t=0; a.hold.max=ctx.time; }
  a.hold.t+=dt; if(a.hold.t>=ctx.time){ doAction(g,a,ctx); a.hold={key:'',t:0,max:0}; }
}

// ============ FERRAMENTAS DO CAÇADOR ============
function useSensor(g,a){
  if(a.sensorCd>0) return; a.sensorCd=15; let best=0;
  const RS=CFG.sensorRaio;
  for(const A of g.altars) if(A.state==='awake'||A.state==='active'){ const d=dist(a,A); if(d<RS) best=Math.max(best,(1-d/RS)*(A.state==='active'?1.3:1)); }
  for(const R of g.reagents) if(R.has){ const d=dist(a,R); if(d<20) best=Math.max(best,.28*(1-d/20)); }
  best=Math.min(1,best);
  ev(g,{type:'sensor',a:a.id,v:r2(best)}); logE(g,'sensor',{by:a.name,v:r2(best)});
}
function throwFlare(g,a){
  if(a.flares<=0) return; const f=g.flares.find(f=>f.t<=0)||g.flares.reduce((m,q)=>q.t<m.t?q:m); // todos acesos: substitui o que está mais perto de apagar
  const dx=-Math.sin(a.yaw),dz=-Math.cos(a.yaw); const d=raioParede(a.x,a.z,dx,dz,10,.3);
  f.x=r2(a.x+dx*d); f.z=r2(a.z+dz*d); f.t=14; a.flares--; ev(g,{type:'sfx',k:'flare',x:f.x,z:f.z,a:a.id});
}

// ============ AÇÕES DO JOGADOR HUMANO ============
function humanActions(g,a,dt){
  const c=a.inp.c||{}, L=a.lc; if(!L._i){ for(const k of ['q','f','g','r','ab']) L[k]=c[k]|0; L._i=1; }
  const edge=k=>{ const v=c[k]|0; if(v!==L[k]){ L[k]=v; return true; } return false; };
  const eq=edge('q'),ef=edge('f'),eg=edge('g'),er=edge('r'),eab=edge('ab');
  if(a.st!=='alive'||a.stunT>0){ a.sealing=-1; a.hold.t=0; return; }
  if(er) reload(g,a);
  if(a.team==='H'){ if(eq) useSensor(g,a); if(ef){ a.lantern=!a.lantern&&a.carga>0; } if(eg) throwFlare(g,a); }
  if(eab) useAbility(g,a);
  const ctx=contexts(g,a); a.sealing=-1;
  if(ctx.e&&ctx.e.type==='loja'){ a.hold={key:'',t:0,max:0}; }
  else if(ctx.e&&ctx.e.type==='seal'&&a.inp.use){ a.sealing=ctx.e.A; a.hold={key:ctx.e.key,t:0,max:0}; }
  else if(ctx.e&&a.inp.use) holdStep(g,a,ctx.e,dt);
  else if(ctx.t&&a.inp.use2) holdStep(g,a,ctx.t,dt);
  else a.hold={key:'',t:0,max:0};
  if(a.team==='H'){
    if(a.inp.fire&&a.fireCd<=0&&a.ammo>0&&a.reloadT<=0&&a.sealing<0&&a.blindT<=0) fireHuman(g,a);
    if(a.lantern){ a.carga-=dt*3.2; if(a.carga<=0){ a.carga=0; a.lantern=false; ev(g,{type:'feed',msg:'Carga esgotada. Procure luz para recarregar.',cls:'h',to:a.id}); } }
    else if(lightAt(g,a.x,a.z)>.5) a.carga=Math.min(100,a.carga+dt*6);
  } else {
    if(a.inp.fire&&a.fireCd<=0&&a.fervor>=CFG.sigilCusto&&a.blindT<=0){ const y=a.inp.yaw,p=a.inp.pitch; castSigil(g,a,-Math.sin(y)*Math.cos(p),Math.sin(p),-Math.cos(y)*Math.cos(p)); }
  }
}

// ============ ALTARES ============
function mareFactor(g){ return 1+Math.min(.2,Math.floor((g.rt-g.lastResolveT)/60)*.05); }
function startRitual(g,A,by){
  A.state='active'; A.startT=g.rt; A.localized=false; A.selo=1; A.arrived=false; A.maxN=0; A.tardio=g.momento>=2; A.grande=fendas(g)+farois(g)>=2; A.eco=false; A.obs=novaObs();
  if(g.buff.selo>g.t){ A.selo=1.1; g.buff.selo=0; feed(g,'Selo de Luz corrompido: este ritual corre 10% mais rápido.','c','C'); }
  const n=g.actors.filter(c=>c.team==='C'&&c.st==='alive'&&inCircle(c,A)).length;
  sentinelaAvisa(g,A,'um ritual começou');
  if(A.grande){ localize(g,A,'grande'); feed(g,`O Grande Ritual começou em ${A.name}. A catedral inteira ouve.`,'big'); ev(g,{type:'grande',altar:A.i}); }
  ev(g,{type:'presage'}); feed(g,'Presságio: um ritual despertou.','big','H'); feed(g,`Ritual iniciado: ${A.name}.`,'c','C');
  logE(g,'ritual_start',{altar:A.name,channelers:Math.max(1,n),by:by.name});
}
// O eco do coro: a caça ouve de que setor vem o ritual (dois altares possíveis) antes de o coro revelar o altar.
function ecoDoCoro(g,A){ A.eco=true; const S=setorDoAltar(A.i); g.eco={setor:S.id,t:r2(g.rt)};
  ev(g,{type:'eco',setor:S.id,altares:S.altares.slice()});
  feed(g,`O coro ecoa ${S.nome}: o ritual está em ${S.altares.map(i=>g.altars[i].name).join(' ou em ')}.`,'big h','H'); feed(g,`Os Caçadores ouviram o eco do coro ${S.nome}.`,'c','C');
  logE(g,'eco',{altar:A.name,setor:S.id}); }
function localize(g,A,how){ if(A.localized) return; A.localized=true; g.know[A.i]='ativo'; g.eco=null;
  feed(g,`O coro revela: ${A.name}.`,'big','H'); feed(g,`Os Caçadores descobriram ${A.name}.`,'c','C');
  logE(g,'localized',{altar:A.name,prog:r2(A.prog),how});
  if((how==='ouvido'||how==='circulo')&&!A.eco&&A.prog>=CFG.achouLance){ const h=g.actors.filter(x=>x.team==='H'&&x.st==='alive').sort((x,y)=>dist(x,A)-dist(y,A))[0]; if(h) lance(g,'achou_cedo',[h.name],A,'H',r2(A.prog)); } }
// Move e, se o resultado ainda ficar preso entre peças (cantos côncavos de escombros), desfaz o passo.
function moverSeguro(a,nx,nz,r){ const px=a.x,pz=a.z; a.x=nx; a.z=nz; resolve(a,r); if(!livre(a,r)){ a.x=px; a.z=pz; } }
function resolveAltar(g,A,kind){
  const O=A.obs||(A.obs=novaObs());
  const nomes=f=>g.actors.filter(f).map(a=>a.name), seladores=nomes(a=>A.sealers.includes(a.id)), canalizaram=nomes(a=>O.canal[a.id]);
  // a disputa no círculo de cada um, para o relatório (só se a caça chegou ao círculo)
  if(A.arrived) for(const a of g.actors){ const D=O.d[a.id]; if(!D) continue; const o={altar:A.name,who:a.name,team:a.team}; for(const k in D) o[k]=r2(D[k]); logE(g,'disputa',o); }
  A.state=kind; A.maxProg=Math.max(A.maxProg,A.prog); g.lastResolveT=g.rt; A.sealers=[]; g.eco=null;
  g.actors.forEach(a=>{ a.dc=0; a.revUsed=false; if(a.sealing===A.i) a.sealing=-1; });
  if(kind==='fenda'){ A.doneT=r2(g.rt); ev(g,{type:'doom',altar:A.i,por:canalizaram}); feed(g,`Ritual completo em ${A.name}. Uma Fenda se abriu.`,'big c'); logE(g,'ritual_complete',{altar:A.name,canalizadores:A.maxN||1,por:canalizaram});
    if(O.maxSeal>=CFG.defesaLance) lance(g,'defesa',Object.keys(O.defesa).length?nomes(a=>O.defesa[a.id]):canalizaram,A,'C',r2(O.maxSeal));
    if(g.transferPara===A.i) lance(g,'transferencia',[g.transferPor],A,'C',null); }
  else { ev(g,{type:'sealed',altar:A.i,por:seladores}); g.buff.ess=0; feed(g,`${A.name} foi selado. Um Farol se acendeu.`,'big h'); logE(g,'sealed',{altar:A.name,prog:r2(A.prog),canalizadores:A.maxN||1,por:seladores});
    // "no último instante" conta o ponto mais alto a que o ritual chegou: sem ninguém canalizando, o progresso recua até a
    // última etapa enquanto a caça sela, e um ritual salvo a 98% terminava em 75% e não ganhava o lance
    if(A.maxProg>=CFG.limiteLance) lance(g,'selo_limite',seladores,A,null,r2(A.maxProg)); }
  if(kind==='fenda') tarefaProg(g,'C','ritual');
  const F=fendas(g), P=farois(g);
  if(F+P>=3||(g.lab.formato!=='placar'&&(F>=2||P>=2))) g.endAt=g.rt+3;
}
// Observação para o relatório (não muda regra nenhuma): onde a caça esteve durante o ritual e por que o culto saiu do
// círculo. Separa "nunca chegou", "foi ao altar errado", "chegou perto e não entrou", "chegou tarde" e "perdeu a disputa".
const MOTIVO_BOT=[['npc','comprar'],['tr','transferir'],['dg','chamariz'],['rv','reanimar'],['ex','executar'],['r','reagente'],['pedido','ping'],['desvio','desvio'],['ch','voltar ao círculo']];
const novaObs=()=>({perto:{},errado:{},dentro:{},canal:{},defesa:{},d:{},maxSeal:0});
function observarRitual(g,A){ const O=A.obs||(A.obs=novaObs());
  for(const h of g.actors){ if(h.team!=='H'||h.st!=='alive') continue;
    if(!O.perto[h.id]&&dist(h,A)<25){ O.perto[h.id]=1; logE(g,'caca_perto',{altar:A.name,who:h.name,prog:r2(A.prog)}); }
    for(const B of g.altars){ if(B===A||B.state==='fenda'||B.state==='farol'||dist(h,B)>=8) continue; const k=h.id+':'+B.i; if(O.errado[k]) continue; O.errado[k]=1;
      const S=g.eco&&SETORES.find(x=>x.id===g.eco.setor); logE(g,'altar_errado',{ritual:A.name,altar:B.name,who:h.name,prog:r2(A.prog),comEco:!!g.eco,doSetor:!!(S&&S.altares.includes(B.i))}); } }
  for(const c of g.actors){ if(c.team!=='C') continue; const dentro=c.st==='alive'&&inCircle(c,A);
    if(O.dentro[c.id]&&!dentro){ let motivo='jogador';
      if(c.st!=='alive') motivo='caiu'; else if(!c.human){ const k=c.ai.goalKey||''; const M=MOTIVO_BOT.find(([p])=>k.startsWith(p)); motivo=c.ai.target?'lutar':M?M[1]:k||'nenhum'; }
      logE(g,'canal_saiu',{altar:A.name,who:c.name,prog:r2(A.prog),motivo}); }
    O.dentro[c.id]=dentro?1:0; } }
function updateAltars(g,dt){
  for(const A of g.altars){
    if(A.state!=='active') continue;
    observarRitual(g,A);
    // quem acabou de atacar (sigilo ou Empurrão) não canaliza (CFG.ataqueCanal): defender e celebrar são escolhas separadas
    const ch=g.actors.filter(c=>c.team==='C'&&c.st==='alive'&&inCircle(c,A)&&!(CFG.ataqueCanal>0&&g.t-c.ataqueT<CFG.ataqueCanal)).slice(0,2), n=ch.length;
    let rune=false;
    if(n>0){ let f=0; ch.forEach(c=>{ let k=(g.t-c.hitT<1||c.blindT>0||c.stunT>0)?.5:1; if(c.runeT>0){ k*=1.2; rune=true; } f+=k; }); f/=n;
      A.prog+=dt*f*mareFactor(g)*A.selo*(A.tardio?1.25:1)*(A.grande?.6:1)/CFG.rit[n].dur; A.lastN=n; A.maxN=Math.max(A.maxN||0,n); }
    else if(A.prog>A.cp) A.prog=Math.max(A.cp,A.prog-.02*dt);
    for(const c of CFG.cps) if(A.prog>=c&&A.cp<c){ A.cp=c; logE(g,'checkpoint',{altar:A.name,cp:c});
      if(ch.some(x=>x.cls==='ritualista')) ch.forEach(x=>{ x.shield=15; x.shieldT=8; }); }
    A.maxProg=Math.max(A.maxProg,A.prog);
    const nn=Math.max(1,A.lastN), rad=CFG.rit[nn].rad*(rune?1.5:1)*(A.tardio?1.4:1);
    if(!A.localized&&!A.eco&&CFG.ecoProg>0&&A.prog>=CFG.ecoProg) ecoDoCoro(g,A);
    if(!A.localized){ if(A.prog>=CFG.rit[nn].loc) localize(g,A,'coro'); else if(n>0&&g.actors.some(h=>h.team==='H'&&h.st==='alive'&&dist(h,A)<rad)) localize(g,A,'ouvido');
      else if(g.actors.some(h=>h.team==='H'&&h.st==='alive'&&inCircle(h,A))) localize(g,A,'circulo'); } // quem pisa no círculo vê o ritual, mesmo sem ninguém canalizando
    if(!A.arrived&&g.actors.some(h=>h.team==='H'&&h.st==='alive'&&inCircle(h,A))){ A.arrived=true; logE(g,'hunter_arrive',{altar:A.name,prog:r2(A.prog),channelers:nn,remaining:r2((1-A.prog)*CFG.rit[nn].dur)}); }
    const sealers=g.actors.filter(h=>h.team==='H'&&h.st==='alive'&&h.sealing===A.i&&inCircle(h,A));
    if(sealers.length){
      if(!A.sealers.length){ tarefaProg(g,'H','selar'); }
      if(!A.sealers.length) logE(g,'seal_start',{altar:A.name,prog:r2(A.prog),sealers:sealers.length});
      // o ritual sente o selo: o culto é avisado onde estiver (no máximo a cada 8 s por altar) e decide se volta para defender
      if(!A.sealers.length&&g.rt-(A.avisoSeloT==null?-99:A.avisoSeloT)>8){ A.avisoSeloT=g.rt; ev(g,{type:'selando',altar:A.i,x:A.x,z:A.z}); feed(g,`Os Caçadores estão selando o ritual em ${A.name}!`,'big c','C'); }
      let rate=0; sealers.forEach(s=>rate=Math.max(rate,1/(s.cls==='exorcista'?CFG.sealExo:CFG.seal)));
      if(sealers.length>=2) rate*=CFG.coSeal; if(g.buff.ess>g.t) rate*=1.25; rate=Math.min(rate,1/CFG.sealMin);
      let at=0; sealers.forEach(s=>at=Math.max(at,recentAttackers(g,s)));
      if(at>=2) rate*=g.lab.fire2; else if(at===1) rate*=g.lab.fire1;
      A.seal+=rate*dt; A.sealIdle=0; A.fireT=(A.fireT||0)+(at?dt:0); A.sealT=(A.sealT||0)+dt;
    } else {
      if(A.sealers.length){ const who=A.sealers.map(id=>g.actors.find(x=>x.id===id)).filter(Boolean);
        // 'soltou': continuou no círculo e parou de selar (bot que trocou o selo pela luta: 'lutar'); 'saiu': deixou o círculo
        const cause=who.some(x=>x.st!=='alive')?'caído':who.some(x=>g.t-x.pushedT<.6)?'empurrão':who.some(x=>x.stunT>0)?'atordoado':
          who.some(x=>inCircle(x,A))?(who.some(x=>!x.human&&x.ai.target)?'lutar':'soltou'):'saiu';
        logE(g,'seal_stop',{altar:A.name,seal:r2(A.seal),cause}); }
      A.sealIdle+=dt; if(A.sealIdle>3) A.seal=Math.max(0,A.seal-dt/CFG.seal);
    }
    medirDisputa(g,A,ch,sealers,dt); A.obs.maxSeal=Math.max(A.obs.maxSeal,A.seal);
    A.sealers=sealers.map(s=>s.id);
    if(A.seal>=1) resolveAltar(g,A,'farol'); else if(A.prog>=1){ A.prog=1; resolveAltar(g,A,'fenda'); }
  }
}
function updateKnowledge(g,dt){
  // quanto um Chamariz prende a caça: segundos de Caçador a menos de 12 m dele (cada Caçador conta)
  for(const A of g.altars) if(A.decoy&&A.state==='awake') for(const h of g.actors) if(h.team==='H'&&h.st==='alive'&&dist(h,A)<12) A.iscaT=(A.iscaT||0)+dt;
  for(const h of g.actors){ if(h.team!=='H'||h.st!=='alive') continue;
    g.altars.forEach((A,i)=>{ const d=dist(h,A); if(d>=10) return; g.visit[i]=g.t; const k=g.know[i];
      if(A.state==='awake'){ if(semAura(k)){ g.know[i]='desperto'; ev(g,{type:'evp',a:h.id,x:A.x,z:A.z}); feed(g,`EVP: vozes em ${A.name}.`,'h','H'); logE(g,'evp',{altar:A.name,real:A.chosen&&!A.decoy,by:h.name}); }
      }
      // passar perto de um altar dormente só diz que nada está desperto agora: ele ainda pode ser do culto e não ter sido
      // consagrado. Só o sinal do santuário descarta, e só a purga revela que era do culto.
      else if(A.state==='dormant'&&(k==='?'||k==='desperto'||k==='confirmado'||k==='chamariz')) g.know[i]='visto';
    });
    if(h.lantern){ const fx=-Math.sin(h.yaw),fz=-Math.cos(h.yaw);
      g.altars.forEach((A,i)=>{ if(A.state!=='awake'&&A.state!=='active') return; const dx=A.x-h.x,dz=A.z-h.z,d=Math.hypot(dx,dz); if(d>16||d<.5) return;
        if((dx*fx+dz*fz)/d<.9||!losClear(h,A)) return;
        if(A.decoy){ if(g.know[i]!=='chamariz'){ g.know[i]='chamariz'; feed(g,`Lanterna: nenhum resíduo em ${A.name}. É um Chamariz.`,'h','H'); logE(g,'decoy_spotted',{altar:A.name,by:h.name}); } }
        else if(g.know[i]!=='confirmado'&&g.know[i]!=='ativo'){ g.know[i]='confirmado'; feed(g,`Lanterna: resíduo de reagente em ${A.name}.`,'h','H'); logE(g,'lanterna',{altar:A.name,by:h.name}); } }); }
  }
}

// ============ PROJÉTEIS, COLETÁVEIS ============
function updateProjectiles(g,dt){
  for(let i=g.proj.length-1;i>=0;i--){ const p=g.proj[i]; let dead=false;
    for(let s=0;s<3&&!dead;s++){ p.x+=p.vx*dt/3; p.y+=p.vy*dt/3; p.z+=p.vz*dt/3;
      if(p.y<.05||p.y>14||Math.abs(p.x)>HALF.x||Math.abs(p.z)>HALF.z) dead=true;
      if(!dead&&algum(grade(BOX),p.x,p.z,p.x,p.z,b=>p.x>b.x1&&p.x<b.x2&&p.z>b.z1&&p.z<b.z2&&p.y<b.h)) dead=true; // v6: a cobertura baixa também segura sigilos
      if(!dead) for(const e of g.actors){ if(e.team===p.team||e.st==='dead') continue; const top=alturaAlvo(e);
        if(Math.hypot(p.x-e.x,p.z-e.z)<.5&&p.y>0&&p.y<top){ const src=g.actors.find(x=>x.id===p.owner); if(src) damage(g,e,p.dmg,src); dead=true; break; } } }
    p.life-=dt; if(p.life<=0) dead=true;
    if(dead){ ev(g,{type:'burst',x:r2(p.x),y:r2(Math.max(.1,p.y)),z:r2(p.z),team:p.team}); g.proj.splice(i,1); }
  }
}
function updatePickups(g,dt){
  for(let i=g.pickups.length-1;i>=0;i--){ const p=g.pickups[i]; p.life-=dt; let gone=p.life<=0;
    for(const a of g.actors){ if(gone||a.st!=='alive'||Math.hypot(a.x-p.x,a.z-p.z)>1.1) continue;
      if(p.kind==='reag'){ if(a.team==='C'&&a.reag<CFG.carry){ a.reag++; gone=true; } else if(a.team==='H') gone=true; }
      else if(p.kind==='ess'){ if(a.team==='H'){ g.buff.ess=g.t+60; feed(g,'Essência Profana capturada: próximo selamento a 125%.','h','H'); logE(g,'pickup',{kind:'ess',by:a.name}); } gone=true; }
      else if(p.kind==='selo'){ if(a.team==='C'){ g.buff.selo=g.t+120; feed(g,'Selo de Luz capturado: o próximo ritual corre 10% mais rápido.','c','C'); logE(g,'pickup',{kind:'selo',by:a.name}); } gone=true; } }
    if(gone) g.pickups.splice(i,1);
  }
}

// ============ BOTS ============
const noCone=(a,t,alc,cos)=>{ const dx=t.x-a.x,dz=t.z-a.z,d=Math.hypot(dx,dz); if(d>alc||d<.01) return d<=.01; return (dx*-Math.sin(a.yaw)+dz*-Math.cos(a.yaw))/d>=cos; };
function canSee(g,b,t,range){
  if(t.st==='dead'||b.blindT>0) return false; const d=dist(b,t); if(d>range) return false; if(t.veuT>0&&d>8&&!(b.lantern&&noCone(b,t,16,.9))) return false; // Véu: silhueta tênue, só some de longe
  if(d>CFG.nevoaAlcance&&(zoneAt(b.x,b.z)==='Floresta'||zoneAt(t.x,t.z)==='Floresta')) return false; // névoa da mata
  if(d>7){ const fx=-Math.sin(b.yaw),fz=-Math.cos(b.yaw); if(((t.x-b.x)*fx+(t.z-b.z)*fz)/d<.35&&g.t-b.hitT>2) return false; }
  if(!losClear(b,t)) return false;
  const lit=lightAt(g,t.x,t.z)+(t.lantern?.8:0)+(g.t-t.lastShotT<1.5?.8:0);
  return !(d>13&&lit<.35);
}
function botPickTarget(g,b){
  const ens=enemiesOf(g,b).filter(e=>e.st==='alive'&&canSee(g,b,e,34*difDe(g,b).vista)); if(!ens.length) return null;
  const pri=e=>dist(b,e)-(e.sealing>=0?30:0)-(b.team==='H'&&g.altars.some(A=>A.state==='active'&&inCircle(e,A))?15:0)-(e.hold.key.startsWith('exe')?25:0); // quem executa um aliado vem primeiro
  ens.sort((x,y)=>pri(x)-pri(y)); return ens[0];
}
const lerpAngle=(a,b,t)=>{ const d=((b-a+Math.PI)%(Math.PI*2)+Math.PI*2)%(Math.PI*2)-Math.PI; return a+d*t; };
function pontoDesvio(b){ const a0=Math.random()*6.283;
  for(let k=0;k<8;k++){ const an=a0+k*Math.PI/4, q={x:b.x+Math.cos(an)*7,z:b.z+Math.sin(an)*7}; if(livre(q,.5)&&segClear(b.x,b.z,q.x,q.z,.45,BOX)) return q; }
  return {x:b.x,z:b.z}; }
function setGoal(g,b,x,z,key){ const ai=b.ai;
  if(ai.ignora&&ai.ignora[key]>g.t){ if(ai.goalKey!=='desvio'){ const p=pontoDesvio(b); ai.goal=p; ai.goalKey='desvio'; ai.path=[p]; ai.repath=g.t+3; } return; }
  if(b.ai.goalKey!==key||!b.ai.goal){ b.ai.goal={x,z}; b.ai.goalKey=key; b.ai.path=findPath(b,{x,z}); b.ai.repath=g.t+3; } }
function botMove(g,b,dt,mul){
  const ai=b.ai; b.moving=false; if(!ai.goal||b.stunT>0) return;
  { const d=dist(b,ai.goal); if(ai.progKey!==ai.goalKey){ ai.progKey=ai.goalKey; ai.melhor=d; ai.progT=g.t; ai.progX=b.x; ai.progZ=b.z; }
    if(d<ai.melhor-1||Math.hypot(b.x-ai.progX,b.z-ai.progZ)>2){ ai.melhor=Math.min(ai.melhor,d); ai.progT=g.t; ai.progX=b.x; ai.progZ=b.z; }
    if(d>1.2&&g.t-ai.progT>10&&ai.goalKey!=='desvio'){ (ai.ignora||(ai.ignora={}))[ai.goalKey]=g.t+25; ai.goal=null; ai.goalKey=''; ai.progKey=''; return; } }
  if(g.t>ai.repath){ const nova=findPath(b,ai.goal), comp=pp=>{ let d=0,c=b; for(const q of pp){ d+=Math.hypot(q.x-c.x,q.z-c.z); c=q; } return d; };
    if(!ai.path.length||comp(nova)<comp(ai.path)*.85) ai.path=nova; ai.repath=g.t+3; }
  let tgt=ai.path[0]; if(!tgt) return; let dx=tgt.x-b.x,dz=tgt.z-b.z,d=Math.hypot(dx,dz);
  if(d<.7){ ai.path.shift(); if(!ai.path.length) return; tgt=ai.path[0]; dx=tgt.x-b.x; dz=tgt.z-b.z; d=Math.hypot(dx,dz); }
  const sp=(b.st==='down'?.8:CFG.botSpeed*(b.speed/CFG.speed))*mul*(b.sealing>=0?.4:1)*(b.slowT>0?.6:1);
  const bx0=b.x,bz0=b.z; b.x+=dx/d*sp*dt; b.z+=dz/d*sp*dt; b.moving=true;
  if(!ai.target) b.yaw=lerpAngle(b.yaw,Math.atan2(-dx,-dz),.15);
  for(const o of g.actors){ if(o===b||o.st==='dead') continue; const ox=b.x-o.x,oz=b.z-o.z,od=Math.hypot(ox,oz); if(od<.8&&od>.01){ b.x+=ox/od*(.8-od)*.5; b.z+=oz/od*(.8-od)*.5; } }
  resolve(b,.4); if(!livre(b,.4)){ b.x=bx0; b.z=bz0; }
  ai.stuckT+=dt; if(ai.stuckT>1.5){ if(Math.hypot(b.x-ai.lx,b.z-ai.lz)<.6){ ai.path=findPath(b,ai.goal); b.x+=rand(-.5,.5); b.z+=rand(-.5,.5); resolve(b,.4); } ai.stuckT=0; ai.lx=b.x; ai.lz=b.z; }
}
let G_ATUAL=null;
// com pressa (ritual em andamento), o bot corre como um jogador: a corrida do jogador é CFG.sprint/CFG.speed
const CORRIDA_BOT=1.3;
const circleSpot=(A,b)=>{ const a0=(b.id*2.1)%(Math.PI*2);
  for(let k=0;k<8;k++){ const a=a0+k*Math.PI/4, p={x:A.x+Math.cos(a)*2.1,z:A.z+Math.sin(a)*2.1};
    const ocupado=G_ATUAL&&G_ATUAL.actors.some(o=>o!==b&&o.st!=='dead'&&Math.hypot(o.x-p.x,o.z-p.z)<.9);
    if(!ocupado&&livre(p,.42)) return p; }
  return {x:A.x+Math.cos(a0)*2.1,z:A.z+Math.sin(a0)*2.1}; };
function botCombat(g,b,dt){
  const ai=b.ai; ai.target=botPickTarget(g,b);
  if(!ai.target&&ai.alert&&g.t-ai.alert.t<4) b.yaw=lerpAngle(b.yaw,Math.atan2(-(ai.alert.x-b.x),-(ai.alert.z-b.z)),.1);
  if(!ai.target) return false;
  const t=ai.target; b.yaw=lerpAngle(b.yaw,Math.atan2(-(t.x-b.x),-(t.z-b.z)),.25);
  if(b.fireCd<=0&&b.sealing<0&&b.hold.t<=0){ const d=dist(b,t);
    if(b.team==='H'){ if(b.reloadT>0) return true; if(b.ammo<=0){ reload(g,b); return true; }
      const lit=lightAt(g,t.x,t.z)>.35, p=Math.min(.92,clamp(.78-d/55,.22,.78)*(t.moving?.8:1)*(lit?1:.72)*difDe(g,b).mira), hit=Math.random()<p, ex=hit?0:rand(-1.2,1.2);
      fireBotAt(g,b,t.x+ex,t.ag?rand(.5,1.1):rand(.9,1.6),t.z+ex,!hit);
    } else { if(b.fervor<CFG.sigilCusto) return true;
      // canalizando, só ataca quem sela, quem chega perto, quem acabou de acertá-lo ou quando a vida aperta (atacar pausa a canalização)
      if(CFG.ataqueCanal>0){ const A=g.altars.find(A=>A.state==='active'&&A.chosen&&inCircle(b,A)); if(A&&!(t.sealing===A.i||d<CFG.botRevidaPerto||g.t-b.hitT<CFG.botRevida||b.hp<b.maxHp*.5)) return true; }
      const lead=d/CFG.sigilVel; const sd=.7*difDe(g,b).disp; castSigilAt(g,b,t.x+rand(-sd,sd)+(t._vx||0)*lead*.6,t.ag?.8:1.2,t.z+rand(-sd,sd)+(t._vz||0)*lead*.6); }
  }
  return true;
}
function castSigilAt(g,b,tx,ty,tz){ let dx=tx-b.x,dy=ty-alturaOlho(b),dz=tz-b.z; const l=Math.hypot(dx,dy,dz)||1; castSigil(g,b,dx/l,dy/l,dz/l); }
function botHold(g,b,key,time,dt,ctx){ b._segurou=true; if(b.hold.key!==key){ b.hold={key,t:0,max:time}; } b.hold.t+=dt; if(b.hold.t>=time){ doAction(g,b,ctx); b.hold={key:'',t:0,max:0}; } }
const inEnemySpawn=(b,p)=>{ const s=SPAWN[b.team==='H'?'C':'H']; return Math.hypot(p.x-s.x,p.z-s.z)<CFG.spawnSafe+2; };
function botHelpDowned(g,b,dt,fighting){
  const ally=g.actors.find(o=>o!==b&&o.team===b.team&&o.st==='down'&&dist(o,b)<20);
  // reanima também lutando, se o aliado está ao lado e o inimigo à vista está longe
  if(ally&&(!fighting||(dist(b,ally)<6&&(!b.ai.target||dist(b,b.ai.target)>18)))){ if(dist(b,ally)<1.5){ b.moving=false; botHold(g,b,'rev'+ally.id,CFG.revive,dt,{type:'revive',b:ally.id}); return true; } setGoal(g,b,ally.x,ally.z,'rv'+ally.id); botMove(g,b,dt,1.05); return true; }
  const foe=g.actors.find(o=>o.team!==b.team&&o.st==='down'&&dist(o,b)<10&&losClear(b,o)); // só executa quem enxerga
  if(foe&&!fighting){ if(dist(b,foe)<1.5){ b.moving=false; botHold(g,b,'exe'+foe.id,g.lab.execucao,dt,{type:'execute',b:foe.id}); return true; } setGoal(g,b,foe.x,foe.z,'ex'+foe.id); botMove(g,b,dt,1.05); return true; }
  return false;
}
// Ping de um humano da equipe: o bot aliado vai até lá (30 s; 12 s para um inimigo marcado). Num ponto ou inimigo,
// fica vigiando 6 s ao chegar; num altar, volta ao fluxo normal (purgar, consagrar, canalizar) assim que chega.
function botAtendePing(g,b,dt,fighting){ const P=b.ai.pedido; if(!P) return false;
  if(g.t>P.ate||b.st!=='alive'){ b.ai.pedido=null; return false; } if(fighting) return false;
  if(Math.hypot(P.x-b.x,P.z-b.z)<3.2){ if(P.k==='altar'){ b.ai.pedido=null; return false; }
    if(P.chegou<0) P.chegou=g.t; if(g.t-P.chegou>6){ b.ai.pedido=null; return false; } b.moving=false; return true; }
  setGoal(g,b,P.x,P.z,'pedido'+P.n); botMove(g,b,dt,1.1); return true; }
// Caído, o bot rasteja até o aliado de pé mais perto (até 25 m), como um jogador faria para ser reanimado.
function botRasteja(g,b,dt){ b.moving=false; if(b.st!=='down') return;
  const al=g.actors.filter(o=>o!==b&&o.team===b.team&&o.st==='alive'&&dist(o,b)<25).sort((x,y)=>dist(b,x)-dist(b,y))[0];
  if(!al||dist(b,al)<1.2) return; setGoal(g,b,al.x,al.z,'rasteja'+al.id); botMove(g,b,dt,1); }
function botNoite(g,b,dt){ const ai=b.ai, role=b.team;
  // compras: Cultista sem reagente compra; Caçador com pouca munição compra
  // com ritual em andamento (o seu, ou um que a caça já ouviu), ninguém sai para fazer compras
  const urgente=g.altars.some(A=>A.state==='active'&&(role==='C'?A.chosen:A.localized))||(role==='H'&&!!g.eco);
  const quer=!urgente&&g.lab.economia&&(role==='C'?(b.reag===0&&b.obolos>=ITENS.reagente.preco&&'reagente'):(b.reserve<16&&b.obolos>=ITENS.municao.preco&&'municao'));
  if(quer){ const n=NPCS.filter(n=>(!n.time||n.time===role)&&n.vende.includes(quer)).sort((x,y)=>dist(b,x)-dist(b,y))[0];
    const comprometido=n&&ai.goalKey==='npc'+n.id; // já decidiu ir: não desiste ao cruzar o limite em linha reta (o caminho dá voltas)
    if(n&&(dist(b,n)<55||(comprometido&&dist(b,n)<90))){ if(dist(b,n)<2.4){ b.moving=false; if(b.cid==null) comprarBot(g,b,n,quer); return true; } setGoal(g,b,n.x,n.z,'npc'+n.id); botMove(g,b,dt,1); return true; } }
  // tarefas do Crepúsculo
  if(g.momento!==0) return false;
  const pend=(g.tarefas[role]||[]).filter(t=>!t.feita).map(t=>t.id);
  const tipos=role==='H'?[pend.includes('pistas')&&'pista',pend.includes('sentinelas')&&'sentinela']:[pend.includes('ervas')&&'erva',pend.includes('tumulos')&&'tumulo'];
  const alvo=g.pontos.filter(p=>p.ativo&&tipos.includes(p.tipo)&&!(p.tipo==='sentinela'&&p.acesa)&&!(ai.ignora&&ai.ignora['pn'+p.i]>g.t)).sort((x,y)=>dist(b,x)-dist(b,y))[0];
  if(!alvo) return false;
  if(dist(b,alvo)<1.7){ b.moving=false; const ctx=contexts(g,b).e; if(ctx&&ctx.type==='ponto') botHold(g,b,ctx.key,ctx.time,dt,ctx);
    else (ai.ignora||(ai.ignora={}))['pn'+alvo.i]=g.t+40; // a ação não aparece aqui (outra coisa tem prioridade): tenta outro ponto
    return true; }
  setGoal(g,b,alvo.x,alvo.z,'pn'+alvo.i); botMove(g,b,dt,1); return true;
}
function comprarBot(g,b,n,item){ const it=ITENS[item]; if(b.obolos<it.preco) return; b.obolos-=it.preco;
  if(item==='reagente'&&b.reag<CFG.carry) b.reag++; else if(item==='municao') b.reserve=Math.min(80,b.reserve+16); logE(g,'compra',{who:b.name,item,npc:n.id}); }
function aiHunter(g,b,dt){
  const fighting=botCombat(g,b,dt), ai=b.ai; b.sealing=-1;
  // Lanterna do bot: só acesa enquanto confere um altar (gasta carga e o denuncia, como a do jogador)
  if(b.lantern){ b.carga-=dt*3.2; if(b.carga<=0){ b.carga=0; } } else if(b.carga<100&&lightAt(g,b.x,b.z)>.5) b.carga=Math.min(100,b.carga+dt*6);
  b.lantern=false;
  if(b.reloadT>0){ b.reloadT-=dt; if(b.reloadT<=0){ const n=Math.min(8-b.ammo,b.reserve); b.ammo+=n; b.reserve-=n; if(b.reserve<=0) b.reserve=24; } }
  if(ai.target&&b.abilCd<=0){ const d=dist(b,ai.target);
    if(b.feitico==='flash'&&d<12) useAbility(g,b);
    if(b.feitico==='purificacao'&&enemiesOf(g,b).some(e=>e.st==='alive'&&dist(b,e)<5.5&&losClear(b,e))) useAbility(g,b); }
  if(b.feitico==='sal'&&b.abilCd<=0){ const A=g.altars.find(A=>((A.state==='active'&&A.localized)||g.know[A.i]==='confirmado')&&dist(b,A)<7); if(A) useAbility(g,b); }
  if(botHelpDowned(g,b,dt,fighting)) return;
  if(botAtendePing(g,b,dt,fighting)) return;
  if(!fighting&&botNoite(g,b,dt)) return;
  const act=g.altars.filter(A=>A.state==='active'&&A.localized).sort((x,y)=>dist(b,x)-dist(b,y))[0];
  if(act){
    if(inCircle(b,act)){ const close=ai.target&&dist(b,ai.target)<11&&b.hp>45;
      if(!close){ b.sealing=act.i; b.moving=false; return; }
      ai.strafeT-=dt; if(ai.strafeT<=0){ ai.strafe*=-1; ai.strafeT=rand(.6,1.4); }
      const s=circleSpot(act,b); setGoal(g,b,s.x+ai.strafe*.8,s.z,'fight'+act.i+ai.strafe); botMove(g,b,dt,.7); return; }
    const s=circleSpot(act,b); setGoal(g,b,s.x,s.z,'go'+act.i); botMove(g,b,dt,fighting?.8:CORRIDA_BOT); return;
  }
  // eco do coro: vai aos altares do setor que a equipe ainda não descartou (primeiro os despertos); a dupla se divide
  if(g.eco&&!fighting){ const S=SETORES.find(s=>s.id===g.eco.setor);
    const cands=S.altares.map(i=>g.altars[i]).filter(A=>A.state!=='fenda'&&A.state!=='farol'&&g.know[A.i]!=='descartado'&&g.know[A.i]!=='chamariz');
    if(cands.length){ const pri=A=>g.know[A.i]==='confirmado'||g.know[A.i]==='desperto'?0:1; cands.sort((x,y)=>pri(x)-pri(y)||dist(b,x)-dist(b,y));
      const mate=g.actors.find(h=>h!==b&&h.team==='H'&&h.st==='alive'), mateAlvo=mate&&mate.ai.ecoT===g.eco.t?mate.ai.ecoAlvo:-1;
      let T=cands[0]; if(cands.length>1&&mateAlvo===T.i&&mate.id<b.id) T=cands[1];
      ai.ecoAlvo=T.i; ai.ecoT=g.eco.t; const s=circleSpot(T,b); setGoal(g,b,s.x,s.z,'eco'+T.i); botMove(g,b,dt,CORRIDA_BOT); return; } }
  // o bot só vê o que a caça sabe: um ritual ainda não localizado continua sendo, para ele, o altar desperto que era
  const known=g.altars.filter(A=>(A.state==='awake'||(A.state==='active'&&!A.localized))&&['confirmado','desperto','chamariz'].includes(g.know[A.i])).sort((x,y)=>dist(b,x)-dist(b,y))[0];
  if(known&&!(g.know[known.i]==='chamariz'&&dist(b,known)>12)){
    // antes de purgar, confere com a Lanterna se o altar desperto é de verdade ou um Chamariz
    if(g.know[known.i]==='desperto'&&!fighting&&b.carga>5){ const d=dist(b,known);
      if(d<14&&d>1.5&&losClear(b,known)){ b.moving=false; b.lantern=true; b.yaw=lerpAngle(b.yaw,Math.atan2(-(known.x-b.x),-(known.z-b.z)),.3); return; } }
    if(dist(b,known)<3){ b.moving=false; if(known.state!=='awake') return; /* já está no círculo: o ritual se revela */ if(!fighting) botHold(g,b,'purge'+known.i,CFG.purge,dt,{type:'purge',A:known.i}); return; }
    const s=circleSpot(known,b); setGoal(g,b,s.x,s.z,'pg'+known.i); botMove(g,b,dt,1); return; }
  if(ai.alert&&g.t-ai.alert.t<6&&!fighting&&!inEnemySpawn(b,ai.alert)){ setGoal(g,b,ai.alert.x,ai.alert.z,'al'+Math.round(ai.alert.t)); botMove(g,b,dt,1); if(dist(b,ai.alert)<2) ai.alert=null; return; }
  if(b.cls==='rastreador'&&!fighting){ // segue a pegada mais fresca por perto
    if(!ai.rastro||g.t>ai.rastro.ate||dist(b,ai.rastro)<2){ const p=(g.pegadas||[]).filter(p=>g.t-p.t<10&&Math.hypot(p.x-b.x,p.z-b.z)<30).pop();
      ai.rastro=p?{x:p.x,z:p.z,ate:g.t+2.5,k:'rs'+p.t}:null; }
    if(ai.rastro&&!inEnemySpawn(b,ai.rastro)){ setGoal(g,b,ai.rastro.x,ai.rastro.z,ai.rastro.k); botMove(g,b,dt,1.05); return; } }
  const R=g.reagents.find(R=>R.has&&dist(b,R)<6);
  if(R&&!fighting&&b.id%2===0){ if(dist(b,R)<1.8){ b.moving=false; botHold(g,b,'burn'+R.i,CFG.burn,dt,{type:'burn',R:R.i}); return; } setGoal(g,b,R.x,R.z,'b'+R.i); botMove(g,b,dt,1); return; }
  const chegouVigia=ai.goal&&ai.goalKey==='pt'+ai.patrol&&dist(b,ai.goal)<1.5, desviado=ai.goalKey==='desvio'&&(!ai.goal||dist(b,ai.goal)<1.5);
  if(desviado) ai.patrol=null;
  if(ai.patrol==null||g.t>ai.patrolT||dist(b,g.altars[ai.patrol])<6||chegouVigia){
    const cands=g.altars.filter(A=>A.state!=='fenda'&&A.state!=='farol'&&g.know[A.i]!=='chamariz');
    const mate=g.actors.find(h=>h!==b&&h.team==='H'); const avoid=mate?mate.ai.patrol:-1;
    const early=g.rt<60;
    cands.sort((x,y)=>(g.visit[x.i]-g.visit[y.i])+(x.i===avoid?40:0)-(y.i===avoid?40:0)+(dist(b,x)-dist(b,y))*(early?1.2:.4));
    if(cands.length){ ai.patrol=cands[0].i; ai.patrolT=g.t+25; } }
  if(ai.patrol!=null){ const A=g.altars[ai.patrol]; if(!ai.vigiaPt||ai.vigiaPt.i!==A.i){ let pt=null; const a0=b.id*1.3;
      for(let k=0;k<12&&!pt;k++){ const an=a0+k*Math.PI/6, q={x:A.x+Math.cos(an)*4.5,z:A.z+Math.sin(an)*4.5}; if(livre(q,.5)&&segClear(q.x,q.z,A.x,A.z,.2,TALL)) pt=q; }
      ai.vigiaPt={i:A.i,x:(pt||A).x,z:(pt||A).z}; }
    setGoal(g,b,ai.vigiaPt.x,ai.vigiaPt.z,'pt'+A.i); botMove(g,b,dt,fighting?.8:1); }
}
function aiCult(g,b,dt){
  const fighting=botCombat(g,b,dt), ai=b.ai;
  // o que o culto vê: um Caçador rondando um altar escolhido torna o altar suspeito para a equipe
  if(g.t>=(ai.olharT||0)){ ai.olharT=g.t+.5;
    for(const A of g.altars){ if(!A.chosen||g.suspeitaC[A.i]||dist(b,A)>40) continue;
      if(g.actors.some(h=>h.team==='H'&&h.st==='alive'&&dist(h,A)<12&&canSee(g,b,h,40))) g.suspeitaC[A.i]=true; } }
  if(b.feitico==='veu'&&b.abilCd<=0&&ai.target&&dist(b,ai.target)<22&&b.hp<b.maxHp*.7) useAbility(g,b);
  if(b.feitico==='empurrao'&&b.abilCd<=0){ const t=enemiesOf(g,b).find(e=>e.st==='alive'&&dist(b,e)<3.4&&(e.sealing>=0||ai.target===e));
    if(t){ b.yaw=Math.atan2(-(t.x-b.x),-(t.z-b.z)); useAbility(g,b); } }
  if(botHelpDowned(g,b,dt,fighting)) return;
  if(botAtendePing(g,b,dt,fighting)) return;
  if(!fighting&&botNoite(g,b,dt)) return;
  const act=g.altars.filter(A=>A.state==='active'&&A.chosen).sort((x,y)=>dist(b,x)-dist(b,y))[0];
  if(act){ if(b.feitico==='runa'&&b.abilCd<=0&&inCircle(b,act)) useAbility(g,b);
    const s=circleSpot(act,b); if(inCircle(b,act)&&dist(b,s)<1.2){ b.moving=false; return; } setGoal(g,b,s.x,s.z,'ch'+act.i); botMove(g,b,dt,fighting?1.05:CORRIDA_BOT); return; }
  const rem=g.altars.filter(A=>A.chosen&&(A.state==='dormant'||A.state==='awake'));
  if(!rem.length){ // sem altar para trabalhar (esperando substituto): vai se abastecer no Claustro
    const R=g.reagents.filter(R=>R.has).sort((x,y)=>dist(b,x)-dist(b,y))[0];
    if(R&&b.reag<CFG.carry){ if(dist(b,R)<1.8){ b.moving=false; botHold(g,b,'col'+R.i,CFG.collect,dt,{type:'collect',R:R.i}); return; } setGoal(g,b,R.x+.9,R.z,'r'+R.x+','+R.z); botMove(g,b,dt,1); return; }
    b.moving=false; ai.esperando=g.t; return; }
  rem.sort((x,y)=>((y.state==='awake'&&!y.decoy)?1:0)-((x.state==='awake'&&!x.decoy)?1:0)||dist(b,x)-dist(b,y));
  const T=rem[0]; if(ai.esperaAltar!==T.i){ ai.esperaAltar=T.i; ai.wait=0; }
  // Chamariz: só um bot da equipe cuida disso (o outro segue o ritual). O primeiro sai no Crepúsculo;
  // o segundo, se sobrar cinza, depois que um ritual se resolver. O prazo para chegar cresce com a distância.
  const dono=g.actors.filter(c=>c.team==='C'&&!c.human&&c.st!=='dead').sort((x,y)=>x.id-y.id)[0];
  const querChamariz=dono===b&&!ai.decoyUsed&&g.decoys>0&&g.rt>25&&(!ai.chamarizes||(g.lastResolveT>(ai.chamarizT||0)&&g.rt-(ai.chamarizT||0)>90));
  if(querChamariz){
    if(ai.decoyAlvo==null||g.altars[ai.decoyAlvo].chosen||g.altars[ai.decoyAlvo].state!=='dormant'){
      const D=g.altars.filter(A=>!A.chosen&&A.state==='dormant').sort((x,y)=>dist(b,x)-dist(b,y))[0];
      ai.decoyAlvo=D?D.i:null; ai.decoyT=0; ai.decoyPrazo=D?20+dist(b,D)/2.2:0; }
    const D=ai.decoyAlvo!=null?g.altars[ai.decoyAlvo]:null;
    if(D){ if(dist(b,D)<3.3){ b.moving=false; botHold(g,b,'dec'+D.i,CFG.decoy,dt,{type:'decoy',A:D.i});
        if(D.state==='awake'){ ai.chamarizes=(ai.chamarizes||0)+1; ai.chamarizT=g.rt; ai.decoyAlvo=null; if(ai.chamarizes>=2) ai.decoyUsed=true; } return; }
      const sd=circleSpot(D,b); setGoal(g,b,sd.x,sd.z,'dg'+D.i); botMove(g,b,dt,1);
      ai.decoyT=(ai.decoyT||0)+dt; if(ai.decoyT>ai.decoyPrazo){ ai.decoyUsed=true; ai.decoyAlvo=null; logE(g,'chamariz_desistiu',{who:b.name,altar:D.name}); } // rota ruim: desiste sem ficar preso
      return; } }
  // A Transferência não é gratuita nem automática: o bot só a usa quando o altar escolhido já foi
  // descoberto pelos Caçadores (ou quando a noite já está avançada). Isso testa a mecânica de dedução
  // sem dar ao bot uma decisão onisciente em toda partida.
  if(!g.transferUsed&&b.reag>0&&g.rt>120){
    const F=g.altars.find(A=>A.chosen&&A.state==='dormant');
    const D=g.altars.filter(A=>!A.chosen&&A.state==='dormant').sort((x,y)=>dist(b,x)-dist(b,y))[0];
    const descoberta=F&&g.suspeitaC[F.i]; // o culto só sabe o que viu: um Caçador perto do altar ou o aviso de purga
    const emergencia=F&&g.rt>420;
    if(F&&D&&(descoberta||emergencia)){
      if(dist(b,D)<3.4){ b.moving=false; botHold(g,b,'tr'+D.i,CFG.transfer,dt,{type:'transfer',A:D.i,from:F.i}); return; }
      const sd=circleSpot(D,b); setGoal(g,b,sd.x,sd.z,'trg'+D.i); botMove(g,b,dt,1.02); return;
    }
  }
  const partner=g.actors.some(c=>c!==b&&c.team==='C'&&c.st==='alive'&&c.reag>0&&!c.human);
  const want=b.reag===0||(b.reag<CFG.carry&&(!partner||T.state==='dormant'));
  if(want){
    const ign=k=>ai.ignora&&ai.ignora[k]>g.t;
    const R=g.reagents.filter(R=>R.has&&!ign('r'+R.x+','+R.z)).sort((x,y)=>dist(b,x)-dist(b,y))[0];
    const P=g.pickups.filter(p=>p.kind!=='ess'&&!ign('r'+p.x+','+p.z)).sort((x,y)=>dist(b,x)-dist(b,y))[0];
    const tgt=(P&&(!R||dist(b,P)<dist(b,R)))?P:R;
    const comprometido=tgt&&ai.goalKey==='r'+tgt.x+','+tgt.z; // já decidiu buscar este frasco: não desiste por causa de um limiar em linha reta
    if(tgt&&(b.reag===0||dist(b,tgt)<14||comprometido)){
      if(tgt===R&&dist(b,R)<1.8){ b.moving=false; if(!fighting||b.hold.t>0) botHold(g,b,'col'+R.i,CFG.collect,dt,{type:'collect',R:R.i}); return; }
      const ox=tgt===P?0:.9; setGoal(g,b,tgt.x+ox,tgt.z,'r'+tgt.x+','+tgt.z); botMove(g,b,dt,1); return; }
  }
  if(b.reag>0){ const s=circleSpot(T,b);
    if(dist(b,T)<3.3){ b.moving=false; ai.emperrado=0;
      if(T.state==='dormant') botHold(g,b,'cons'+T.i,CFG.consecrate,dt,{type:'consecrate',A:T.i});
      else if(T.state==='awake'&&!T.decoy&&bloqueioRitual(g)){ b.moving=false; ai.esperando=g.t; }
      else if(T.state==='awake'&&!T.decoy){ const mate=g.actors.find(c=>c!==b&&c.team==='C'&&c.st==='alive'); const near=mate&&dist(mate,T)<8;
        ai.wait+=dt; if(near||ai.wait>6||!mate||b.hold.key==='start'+T.i) botHold(g,b,'start'+T.i,CFG.start,dt,{type:'start',A:T.i}); } // uma vez decidido, segura até o fim
      return; }
    setGoal(g,b,s.x,s.z,'to'+T.i); botMove(g,b,dt,1);
    if(dist(b,T)<6){ ai.emperrado=(ai.emperrado||0)+dt; if(ai.emperrado>6){ ai.emperrado=0; ai.path=findPath(b,{x:T.x,z:T.z}); } } else ai.emperrado=0;
    return; }
  const s=circleSpot(T,b); setGoal(g,b,s.x+2,s.z,'gd'+T.i); botMove(g,b,dt,.8);
}

// ============ PASSO PRINCIPAL ============
function step(g,dt){
  g.t+=dt;
  if(g.phase==='play'){ g.rt+=dt; playStep(g,dt); }
  else if(g.timers&&g.phase!=='final'){ g.phaseT-=dt; if(g.phaseT<=0) timeoutPhase(g); }
}
function playStep(g,dt){
  G_ATUAL=g;
  for(const a of g.actors){
    a._vx=(a.x-(a._px!=null?a._px:a.x))/dt; a._vz=(a.z-(a._pz!=null?a._pz:a.z))/dt; a._px=a.x; a._pz=a.z;
    a.hist.push([g.t,a.x,a.z]); while(a.hist.length&&a.hist[0][0]<g.t-1.2) a.hist.shift();
    for(const k of ['invuln','blindT','stunT','runeT','abilCd','sensorCd','fireCd']) if(a[k]>0) a[k]=Math.max(0,a[k]-dt);
    if(a.shieldT>0){ a.shieldT-=dt; if(a.shieldT<=0) a.shield=0; }
    if(a.human&&a.reloadT>0){ a.reloadT-=dt; if(a.reloadT<=0){ const n=Math.min(8-a.ammo,a.reserve); a.ammo+=n; a.reserve-=n; } }
    if(a.st==='alive'&&Math.hypot(a.x-SPAWN[a.team].x,a.z-SPAWN[a.team].z)<CFG.spawnSafe){ a.invuln=Math.max(a.invuln,.25); if(!a.human||a.reserve<48) a.reserve=Math.max(a.reserve,48); }
  }
  for(const a of g.actors){
    if(a.st==='dead'){ a.respawnT-=dt; if(a.respawnT<=0){ spawn(g,a,false); ev(g,{type:'respawn',a:a.id}); } continue; }
    if(a.st==='down'){ a.downT-=dt; a.sealing=-1; if(a.downT<=0) kill(g,a,null); if(!a.human) botRasteja(g,a,dt); else humanActions(g,a,dt); continue; }
    if(a.human) humanActions(g,a,dt);
    else if(a.stunT<=0){ a._segurou=false; if(a.team==='H') aiHunter(g,a,dt); else aiCult(g,a,dt);
      // como o jogador que solta o E: meio segundo sem segurar zera a contagem (antes ela ficava pendurada e
      // impedia o bot de atirar, o que travava a dupla parada ao lado de um inimigo)
      if(a._segurou) a._semSegurar=0; else if(a.hold.key&&(a._semSegurar=(a._semSegurar||0)+dt)>.5){ a.hold={key:'',t:0,max:0}; a._semSegurar=0; }
      // rede de segurança: bot parado 15 s sem estar ocupado tem a decisão reiniciada (não corrige a causa; limita o estrago)
      const ai=a.ai, vigiando=ai.goal&&ai.goalKey.startsWith('pt')&&dist(a,ai.goal)<2, lutando=ai.target&&g.t-a.lastShotT<2.5;
      const ocupado=a.sealing>=0||a.hold.t>0||vigiando||lutando||g.altars.some(A=>A.state==='active'&&inCircle(a,A))||(ai.esperando&&g.t-ai.esperando<.5);
      if(!ai.vigia||ocupado||Math.hypot(a.x-ai.vigia.x,a.z-ai.vigia.z)>3) ai.vigia={x:a.x,z:a.z,t:g.rt};
      else if(g.rt-ai.vigia.t>15){ const objetivoAntes=ai.goalKey, alvoAntes=ai.target?ai.target.name:null, segurando=a.hold.key; ai.goal=null; ai.goalKey=''; ai.path=[]; ai.ignora={}; ai.wait=0; ai.decoyUsed=true; ai.progKey=''; ai.vigia={x:a.x,z:a.z,t:g.rt};
        const q=pontoDesvio(a); ai.goal=q; ai.goalKey='desvio'; ai.path=[q]; ai.repath=g.t+3;
        logE(g,'bot_reiniciado',{who:a.name,team:a.team,cls:a.cls,objetivo:objetivoAntes,combate:alvoAntes,reag:a.reag,x:r2(a.x),z:r2(a.z)}); } }
    if(a.moving&&a.st==='alive'){ a.stepT=(a.stepT||0)-dt; if(a.stepT<=0){ const leve=a.ag, corre=!leve&&a.human&&a.inp.sp&&a.inp.mz<0; a.stepT=leve?.6:corre?.3:.42; ev(g,{type:'sfx',k:leve?'passoLeve':corre?'passoForte':'step',x:r2(a.x),z:r2(a.z),a:a.id}); } }
    if(g.t-a.hitT>6&&a.hp<a.maxHp&&a.st==='alive') a.hp=Math.min(a.maxHp,a.hp+dt*8);
    if(a.team==='C'&&a.st==='alive'&&g.t-a.lastShotT>CFG.fervorEspera) a.fervor=Math.min(100,a.fervor+dt*CFG.fervorRegen);
    updateSanity(g,a,dt);
    if(!a.human){ for(const o of g.actors) if(o.team!==a.team&&o.st==='alive'&&g.t-o.panicT<.5&&dist(a,o)<18) a.ai.alert={x:o.x,z:o.z,t:g.t}; }
  }
  rastrear(g);
  passoNoite(g,dt); updateAltars(g,dt); updateKnowledge(g,dt); updateProjectiles(g,dt); updatePickups(g,dt);
  g.reagents.forEach(R=>{ if(!R.has){ R.t-=dt; if(R.t<=0) R.has=true; } });
  g.flares.forEach(f=>{ if(f.t>0) f.t=Math.max(0,f.t-dt); });
  if(g.rt>=CFG.roundTime||(g.endAt&&g.rt>=g.endAt)) endRound(g);
}
function endRound(g){
  const done=g.altars.filter(A=>A.chosen&&A.state==='fenda');
  const lastT=done.length?Math.max(...done.map(A=>A.doneT)):null;
  const maxP=Math.max(0,...g.altars.filter(A=>A.chosen).map(A=>A.maxProg||0));
  const sealed=g.altars.filter(A=>A.state==='farol').length;
  const cultTeam=g.round===1?'B':'A';
  g.scores[cultTeam]={done:done.length,lastT,maxP:r2(Math.min(1,maxP)),sealed};
  logE(g,'round_end',{cultTeam,done:done.length,sealed,lastT});
  g.stats[g.round]=roundStats(g,g.round);
  g.phase='summary'; g.phaseT=CFG.timers.summary; g.ready={}; g.proj=[];
  g.actors.forEach(a=>{ a.sealing=-1; a.inp.fire=false; });
}
function roundStats(g,round){
  const L=g.log.filter(e=>e.r===round), c=k=>L.filter(e=>e.ev===k);
  const starts=c('ritual_start'), arrive=c('hunter_arrive'), sStart=c('seal_start'), sStop=c('seal_stop'), sealed=c('sealed');
  const fireT=g.altars.reduce((s,A)=>s+(A.fireT||0),0), sealT=g.altars.reduce((s,A)=>s+(A.sealT||0),0);
  return {rituais:starts.length,completos:c('ritual_complete').length,selados:sealed.length,chegadas:arrive.length,
    tentativasSelo:sStart.length,cancelados:sStop.filter(e=>e.cause==='caído'||e.cause==='empurrão'||e.cause==='atordoado').length, // interrompidos pelo culto
    fracaoSobFogo:sealT>0?r2(fireT/sealT):null,primeiroContato:L.find(e=>e.ev==='first_contact')?.t??null,
    quedas:c('down').length,mortes:c('death').length,reanimacoes:c('revive').length,purgas:c('purge').length,chamarizes:c('decoy').length,transferencias:c('transfer').length};
}
// Relatório de playtest de uma rodada, a partir do registro (serve para o cliente, o servidor e tools/analisar.js).
// Mede o que importa para saber se a investigação funciona: quando a caça descobre a verdade, quanto tempo passa sem
// pista nova, quanto o engano custa, como as descobertas viram (ou não) Faróis, e o que o combate faz com isso.
const INFO_CACA=['eco','pista_info','evp','sentinela','lanterna','decoy_spotted','rastro_visto','rastro_lido','localized','sal'];
// a disputa de um ritual somada por equipe (registros 'disputa' do fim do ritual; vazio se a caça não chegou ao círculo)
function resumoDisputa(L){ if(!L.length) return null; const soma=team=>{ const o={jogadores:0}; for(const e of L) if(e.team===team){ o.jogadores++;
      for(const k of ['perto','circulo','selando','sobFogo','canal','atacando','dano','sofrido','quedas','derrubou']) o[k]=r2((o[k]||0)+(e[k]||0)); } return o; };
  return {caca:soma('H'),culto:soma('C')}; }
function relatorio(log,r){ r=r||1;
  const L=(log||[]).filter(e=>(e.r||1)===r), c=k=>L.filter(e=>e.ev===k), fim=c('round_end')[0], T=fim?fim.t:(L.length?L[L.length-1].t:0);
  const verdade=L.filter(e=>(e.ev==='evp'&&e.real)||e.ev==='sentinela'||e.ev==='lanterna'||e.ev==='rastro_lido'||e.ev==='localized');
  const info=L.filter(e=>INFO_CACA.includes(e.ev)||(e.ev==='sensor'&&e.v>.02)).map(e=>e.t).sort((a,b)=>a-b);
  let maior=0, soma=0, n=0, ant=0; for(const t of info.concat([T])){ const d=t-ant; if(d>maior) maior=d; soma+=d; n++; ant=t; }
  const rituais=c('ritual_start').map(s=>{ const fimR=L.find(e=>(e.ev==='sealed'||e.ev==='ritual_complete')&&e.altar===s.altar&&e.t>=s.t);
    const loc=L.find(e=>e.ev==='localized'&&e.altar===s.altar&&e.t>=s.t), cheg=L.find(e=>e.ev==='hunter_arrive'&&e.altar===s.altar&&e.t>=s.t);
    const sel=L.filter(e=>e.ev==='seal_start'&&e.altar===s.altar&&e.t>=s.t&&(!fimR||e.t<=fimR.t)).length;
    const no=L.filter(e=>e.t>=s.t&&(!fimR||e.t<=fimR.t)), resultado=fimR?(fimR.ev==='sealed'?'farol':'fenda'):'aberto';
    const perto=no.some(e=>e.ev==='caca_perto'&&e.altar===s.altar), errados=no.filter(e=>e.ev==='altar_errado'&&e.ritual===s.altar);
    // o que aconteceu com a caça neste ritual (na ordem: o resultado, depois o quanto ela chegou perto)
    const caca=resultado==='farol'?'selou':resultado==='aberto'?'aberto':cheg?(cheg.remaining<CFG.sealMin?'chegou_tarde':'perdeu_disputa'):perto?'perto_sem_entrar':errados.length?'altar_errado':'nunca_chegou';
    return {altar:s.altar,inicio:s.t,localizado:loc?r2(loc.t-s.t):null,comoLocalizou:loc?loc.how:null,chegada:cheg?r2(cheg.t-s.t):null,restanteNaChegada:cheg?cheg.remaining:null,tentativasSelo:sel,
      resultado,caca,altaresErrados:errados.map(e=>e.altar),interrupcoesSelo:no.filter(e=>e.ev==='seal_stop'&&e.altar===s.altar).map(e=>e.cause),
      cultoSaiu:no.filter(e=>e.ev==='canal_saiu'&&e.altar===s.altar).map(e=>e.motivo),duracao:fimR?r2(fimR.t-s.t):null,localizarAoFim:loc&&fimR?r2(fimR.t-loc.t):null,
      disputa:resumoDisputa(no.filter(e=>e.ev==='disputa'&&e.altar===s.altar&&fimR&&e.t===fimR.t)),
      interrupcoes:{selo:no.filter(e=>e.ev==='interrupcao'&&e.altar===s.altar&&e.tipo==='selo').length,canal:no.filter(e=>e.ev==='interrupcao'&&e.altar===s.altar&&e.tipo==='canal').length}}; });
  const conversao={}; rituais.forEach(R=>conversao[R.caca]=(conversao[R.caca]||0)+1);
  const causas={}; c('seal_stop').forEach(e=>causas[e.cause]=(causas[e.cause]||0)+1);
  const feiticos={}; c('ability').forEach(e=>feiticos[e.feitico]=(feiticos[e.feitico]||0)+1);
  const mortes=c('death'), purgas=c('purge');
  return {rodada:r,duracao:T,fendas:c('ritual_complete').length,farois:c('sealed').length,
    investigacao:{primeiraVerdade:verdade.length?verdade[0].t:null,comoDescobriu:verdade.length?verdade[0].ev:null,pistasNovas:info.length,
      maiorIntervaloSemPista:r2(maior),intervaloMedioSemPista:n?r2(soma/n):null},
    ferramentas:{sinaisLidos:c('pista_info').length,sentinelasAcesas:L.filter(e=>e.ev==='ponto'&&e.tipo==='sentinela').length,avisosDeSentinela:c('sentinela').length,
      sensor:c('sensor').length,sensorComSinal:c('sensor').filter(e=>e.v>.02).length,evp:c('evp').length,lanterna:c('lanterna').length+c('decoy_spotted').length,
      sal:c('sal').length,rastrosVistos:c('rastro_visto').length,rastrosLidos:c('rastro_lido').length,pings:c('ping').length,frases:c('fala').length,feiticos},
    engano:{chamarizes:c('decoy').length,chamarizesDesmascarados:c('decoy_spotted').length,chamarizesQueimados:purgas.filter(e=>e.decoy).length,
      purgasDeAltarVerdadeiro:purgas.filter(e=>!e.decoy).length,evpEmChamariz:c('evp').filter(e=>!e.real).length,transferencias:c('transfer').length,
      segundosPresosEmChamariz:r2(purgas.reduce((t,e)=>t+(e.decoy&&e.isca?e.isca:0),0))},
    rituais, conversao, lances:c('lance').map(e=>({t:e.t,tipo:e.tipo,quem:e.quem||[],altar:e.altar,team:e.team,v:e.v})),
    selamento:{tentativas:c('seal_start').length,interrupcoes:causas,selados:c('sealed').length},
    combate:{primeiraQueda:c('down').length?c('down')[0].t:null,quedas:c('down').length,execucoes:mortes.filter(e=>e.how==='exec').length,
      sangraram:mortes.filter(e=>e.how==='bleed').length,reanimacoes:c('revive').length},
    sessao:{saidas:c('saiu').length,reconexoes:c('reconnect').length,botsReiniciados:c('bot_reiniciado').length}};
}
function winner(g){
  if(!((g.opts&&g.opts.noites||CFG.noites)>1)){ const S=g.scores.B; if(!S) return null; return S.done>=2?{win:'B',why:'os Cultistas venceram dois dos três rituais'}:{win:'A',why:(S.sealed||0)>=2?'os Caçadores selaram dois dos três rituais':'os Caçadores resistiram até o amanhecer'}; }
  const A=g.scores.A,B=g.scores.B; if(!A||!B) return null;
  if(A.done!==B.done) return {win:A.done>B.done?'A':'B',why:'mais rituais completados'};
  if(A.done>0&&A.lastT!==B.lastT) return {win:A.lastT<B.lastT?'A':'B',why:'desempate: completou o último ritual mais cedo'};
  if(Math.abs(A.maxP-B.maxP)>0.005) return {win:A.maxP>B.maxP?'A':'B',why:'desempate: maior progresso em um único ritual'};
  return {win:null,why:'empate exato; na versão completa, isso levaria ao Ritual Final'};
}

// ============ QUEM RECEBE CADA EVENTO (servidor) ============
// O servidor passa cada evento por aqui antes de mandar a um cliente. Esconder no HUD não basta: o que chega no
// cliente pode ser lido por quem modificar o jogo. Sons e efeitos com posição só chegam a quem estaria perto o
// bastante para ouvir ou ver (ou se o emissor está à vista da equipe); informação de equipe só chega à equipe.
const ALCANCE_SOM={shot:55,sigil:35,step:16,passoForte:24,passoLeve:5,collect:26,flare:20,rune:30,arrasto:45,push:28,flash:30,purify:30};
const visivelPara=(g,a,role)=>!!(a._visto&&a._visto[role]>g.t);
function eventoPara(g,e,me,role){
  const t=e.type;
  if(e.to!=null&&(!me||e.to!==me.id)) return null;
  if(t==='feed') return e.team&&e.team!==role?null:e;
  if(t==='sensor'||t==='evp'||t==='blind'||t==='stun'||t==='hit') return me&&e.a===me.id?e:null;
  if(t==='presage'||t==='eco') return role==='H'?e:null;
  if(t==='selando') return role==='C'?e:null;
  if(t==='interrupcao'||t==='lance') return e.team&&e.team!==role?null:e;
  if(t==='ping'||t==='fala'||t==='tarefa') return e.team===role?e:null;
  if(t==='hurt'){ if(me&&e.a===me.id) return e; const v=g.actors.find(a=>a.id===e.a); // os outros veem o golpe, sem saber de onde veio
    return v&&(v.team===role||visivelPara(g,v,role))?Object.assign({},e,{sx:null,sz:null}):null; }
  let x=e.x, z=e.z, raio=null;
  if(t==='sfx') raio=ALCANCE_SOM[e.k]||40; else if(t==='reload') raio=18; else if(t==='hab') raio=34; else if(t==='burst') raio=CFG.visaoRede;
  else if(t==='tracer'){ x=e.x0; z=e.z0; raio=ALCANCE_SOM.shot; }
  if(raio==null) return e; // eventos da noite toda: sinos, Fendas, Faróis, Grande Ritual, renascimentos
  const em=e.a!=null?g.actors.find(a=>a.id===e.a):null;
  if(em&&(em.team===role||visivelPara(g,em,role))) return e;
  const ouvintes=me&&me.st!=='dead'?[me]:g.actors.filter(a=>a.team===role&&a.st!=='dead'); // morto: ouve pelo aliado que acompanha
  return ouvintes.some(o=>Math.hypot(o.x-x,o.z-z)<=raio*1.2+2)?e:null;
}

// ============ SNAPSHOT (servidor → cliente) ============
const ACT_FIELDS=['feitico','amuleto','id','cid','human','name','team','cls','maxHp','st','reag','sealing','ammo','reserve','fervor','lantern','flares','moving','deaths','revUsed','ag'];
const ACT_NUM=['ataqueT','obolos','veuT','slowT','revelT','curaT','x','z','yaw','pitch','hp','downT','respawnT','reloadT','sanity','carga','sensorCd','abilCd','lastShotT','blindT','stunT','runeT','shield','invuln','speed'];
function snapshot(g,role){
  const hide=role==='H';
  return {t:r2(g.t),rt:r2(g.rt),phase:g.phase,phaseT:r2(g.phaseT),round:g.round,timers:g.timers,matchId:g.matchId,
    slots:g.slots.map(s=>({team:s.team,name:s.name,cid:s.cid})),
    altars:g.altars.map(A=>{
      // a caça só vê o que sabe: um ritual ainda não localizado aparece como o altar que ela conhecia (desperto ou não)
      const oculto=hide&&A.state==='active'&&!A.localized, sabe=!semAura(g.know[A.i]);
      const masked=hide&&A.state==='awake'&&!sabe, st=oculto?(sabe?'awake':'dormant'):masked?'dormant':A.state;
      if(oculto) return {i:A.i,name:A.name,x:A.x,z:A.z,state:st,chosen:false,decoy:false,prog:0,cp:0,seal:0,localized:false,grande:false,lastN:1,maxProg:0,doneT:A.doneT};
      return {i:A.i,name:A.name,x:A.x,z:A.z,state:st,chosen:hide?false:A.chosen,decoy:hide?false:A.decoy,prog:r2(A.prog),cp:A.cp,seal:r2(A.seal),localized:A.localized,grande:!!A.grande,lastN:A.lastN,maxProg:r2(A.maxProg),doneT:A.doneT}; }),
    reagents:g.reagents.map(R=>({i:R.i,x:R.x,z:R.z,has:R.has})),
    actors:g.actors.map(a=>{
      const o={}; ACT_FIELDS.forEach(k=>o[k]=a[k]); ACT_NUM.forEach(k=>o[k]=r2(a[k])); o.hold={key:a.hold.key,t:r2(a.hold.t),max:a.hold.max};
      // Não vazar posições de inimigos pelo snapshot. O cliente só recebe a posição quando alguém da equipe
      // observadora tem linha de visão até ele (paredes, árvores e espinheiros cortam) dentro do alcance em que a
      // névoa ainda deixa ver, ou quando o Sal o revelou. Depois de visto, continua sendo enviado por 0,6 s:
      // evita que ele pisque ao cruzar a quina de uma coluna.
      if(a.team!==role&&a.st!=='dead'){
        const obs=g.actors.filter(h=>h.team===role&&h.human&&h.st!=='dead');
        const agora=a.revelT>0||obs.some(h=>dist(h,a)<=CFG.visaoRede&&losClear(h,a));
        const V=a._visto||(a._visto={}); if(agora) V[role]=g.t+.6;
        if(!(V[role]>g.t)){ o.hidden=true; o.x=0; o.z=0; o.yaw=0; o.pitch=0; // nem o que ele está fazendo: a contagem do E diria em qual altar
          o.hold={key:'',t:0,max:0}; o.sealing=-1; o.reag=0; o.moving=false; o.lantern=false; o.lastShotT=-9; o.ataqueT=-9; }
      }
      return o;
    }),
    proj:g.proj.map(p=>({id:p.id,x:r2(p.x),y:r2(p.y),z:r2(p.z),vx:r2(p.vx),vy:r2(p.vy),vz:r2(p.vz)})),
    pickups:g.pickups.map(p=>({id:p.id,kind:p.kind,x:p.x,z:p.z})),
    flares:g.flares.map(f=>({x:f.x,z:f.z,t:r2(f.t)})),
    know:hide?g.know.slice():g.know.map(()=>'?'),
    momento:g.momento,tarefas:{[role]:(g.tarefas&&g.tarefas[role])||[]},pontos:(g.pontos||[]).map(p=>({i:p.i,tipo:p.tipo,x:p.x,z:p.z,ativo:p.ativo,acesa:p.acesa})),sal:(g.sal||[]).map(q=>({id:q.id,x:q.x,z:q.z})),feit:Object.assign({},g.feit),
    decoys:hide?0:g.decoys,transferUsed:hide?false:g.transferUsed,pegadas:hide?(g.pegadas||[]):[],rastros:hide?(g.rastros||[]).filter(R=>g.rt-R.t<180).map(R=>({x:R.x,z:R.z,t:R.t})):[],picks:hide?[]:g.picks.slice(),ready:Object.assign({},g.ready),cls:Object.assign({},g.cls),
    scores:g.scores,stats:g.stats,buff:{ess:g.buff.ess>g.t?r2(g.buff.ess-g.t):0,selo:g.buff.selo>g.t?r2(g.buff.selo-g.t):0},lastResolveT:g.lastResolveT,lab:g.lab,eco:g.eco};
}

return {PORTAS,PISTA_PTS,ERVA_PTS,SENTINELA_PTS,SEG_TRILHA,ZONAS,zonaDe,coberto,foraDaMata,PROMONTORIO,DESFILADEIRO,TRAVESSIAS,ABISMO,ARCOS,PONTOS_DEF,CATEDRAL,naCatedral,ARVORES,ARBUSTOS,TRILHAS,CLAREIRAS,PORTAIS,LAPIDES,NAV,livre,raioParede,FEITICOS,FEIT_BY_TEAM,NPCS,ITENS,TAREFAS,momentoDe,WP,CFG,CLASSES,CLASS_BY_TEAM,HALF,CLAUSTRO,ZONES,zoneAt,LUGARES,ALTARS,SPAWN,REAG,CANDLES,WALLS,WALLDEF,PILLARS,PEWS,BOX,TALL,segBox,segClear,losClear,occlusion,resolve,findPath,
  DIF,LAB_OPCOES,labDe,relatorio,CADENCIA_HUMANA,SETORES,damage,_botHold:botHold,_doAction:doAction,_traceShot:traceShot,eventoPara,ALCANCE_SOM,VERSAO,REGISTRO,createGame,step,act,dropHuman,reclaimHuman,moveHuman,contexts,lightAt,snapshot,roleOf,winner,mareFactor,inCircle,dist,clamp,lerp,BOT_NAMES};
});
