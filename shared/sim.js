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
  momentos:[{id:'crepusculo',nome:'Crepúsculo',ate:300},{id:'vigilia',nome:'Vigília',ate:720},{id:'horamorta',nome:'Hora Morta',ate:1080}],
  rit:{1:{dur:45,rad:14,loc:0.5},2:{dur:34,rad:24,loc:0.3}},
  circleR:3, seal:8, sealExo:6.5, coSeal:1.35, fire1:0.5, fire2:0.25, sealMin:5, cps:[0.25,0.75],
  carry:2, collect:3.5, consecrate:3, start:1.2, purge:6, burn:6, decoy:2, transfer:12, revive:6, execute:3, downTime:20,
  sigilDano:20, sigilVel:48, sigilCusto:14, fervorRegen:8, fervorEspera:1.8, quedaDano:16, quedaPerto:26, quedaLonge:55, quedaMin:.75,
  compensacaoMax:.5, // quanto o servidor volta no tempo para julgar um tiro: ida e volta + 100 ms de interpolação, com folga
  regrow:40, sensorRaio:58, respawn:[10,3,18], speed:4.6, sprint:6, botSpeed:3.9, spawnSafe:7,
  timers:{pick:35,intro:25,summary:15},
};
const CLASSES={
  ritualista:{team:'C',name:'Ritualista',hp:150,speed:4.6,arma:'Cetro de vértebras',
    text:'Cetro de vértebras. Passiva: quem canaliza com você ganha escudo ao atingir 25% e 75% do ritual.'},
  guardiao:{team:'C',name:'Guardião Profano',hp:200,speed:4.1,arma:'Cetro de vértebras',
    text:'Cetro de vértebras. Passiva: 200 de vida e armadura de osso, mas mais lento.'},
  soldado:{team:'H',name:'Soldado',hp:170,speed:4.6,arma:'Carabina de alavanca',
    text:'Carabina de alavanca. Passiva: carrega mais munição de reserva.'},
  exorcista:{team:'H',name:'Exorcista',hp:160,speed:4.6,arma:'Revólver de prata',
    text:'Revólver de prata. Passiva: sela rituais mais depressa.'},
};
const FEITICOS={
  runa:{team:'C',nome:'Runa',cd:20,text:'Canalização 20% mais rápida por 8 s, mas o ritual fica audível de mais longe.'},
  empurrao:{team:'C',nome:'Empurrão',cd:12,text:'Arremessa quem está à sua frente e interrompe o selamento.'},
  veu:{team:'C',nome:'Véu das Sombras',cd:24,text:'Some da vista dos Caçadores por 5 s. Atacar ou levar dano desfaz o véu.'},
  flash:{team:'H',nome:'Flash',cd:18,text:'Cega os inimigos no cone por 1,5 s. Cultista cego canaliza pela metade.'},
  purificacao:{team:'H',nome:'Purificação',cd:25,text:'Atordoa inimigos a 6 m por 1 s e desfaz Runa e Véu.'},
  sal:{team:'H',nome:'Círculo de Sal',cd:16,text:'Deixa sal no chão por 60 s. O primeiro Cultista que pisar fica lento e aparece para a sua equipe.'},
};
const FEIT_BY_TEAM={H:['flash','purificacao','sal'],C:['runa','empurrao','veu']};
const CLASS_BY_TEAM={H:['soldado','exorcista'],C:['ritualista','guardiao']};

// ============ MAPA ============
// A catedral é o coração do mapa e a floresta a envolve por todos os lados. Os dois lados nascem na mata
// (acampamento dos Caçadores a oeste, círculo de pedras dos Cultistas a leste) e entram pelas portas.
// Dentro: o anel de capelas e o Claustro a céu aberto, onde ficam TODOS os reagentes.
// Fora: trilhas iluminadas pelo luar, mata fechada e escura, dois altares em clareiras, tarefas e mercadores.
const HALF={x:150,z:110};
const CATEDRAL={x1:-96,x2:96,z1:-58,z2:58};
const CLAUSTRO={x1:-56,x2:56,z1:-28,z2:28};
const ZONES=[{name:'Claustro',...CLAUSTRO},{name:'Catedral',...CATEDRAL},{name:'Floresta'}];
const naCatedral=(x,z,m)=>{ m=m||0; return x>CATEDRAL.x1-m&&x<CATEDRAL.x2+m&&z>CATEDRAL.z1-m&&z<CATEDRAL.z2+m; };
const zoneAt=(x,z)=>(x>CLAUSTRO.x1&&x<CLAUSTRO.x2&&z>CLAUSTRO.z1&&z<CLAUSTRO.z2)?'Claustro':naCatedral(x,z)?'Catedral':'Floresta';

// Sem espelhamento: os papéis trocam a cada rodada e cada papel sempre nasce do mesmo lado,
// então as duas equipes vivem o mesmo mapa. Assimetria não é injusta aqui; ela dá a cada altar uma personalidade.
const ALTARS=[
  {name:'Capela Oeste',x:-66,z:-45},   // apertada, duas portas estreitas, bancos caídos por dentro
  {name:'Abside',x:0,z:-47},           // colunata em meia-lua, frente aberta: arena exposta
  {name:'Capela Leste',x:64,z:-46},    // desabou: parede sul virou brecha com escombros
  {name:'Menires',x:-40,z:-88},        // clareira ao norte, anel de pedras com três vãos: aberta ao luar, cercada de mata
  {name:'Nártex',x:0,z:48},            // salão de entrada, logo atrás da grande porta sul
  {name:'Carvalho Oco',x:36,z:90}];    // ao sul, sob um carvalho gigante: raízes como cobertura, mata escura em volta
const SPAWN={H:{x:-132,z:0,yaw:-Math.PI/2},C:{x:132,z:0,yaw:Math.PI/2}};
const LUGARES=[ // nomes que aparecem na planta
  {name:'Sacristia',x:-66,z:-8},{name:'Nave em ruínas',x:72,z:-16},{name:'Coro',x:-34,z:40},{name:'Poço',x:6,z:-4},{name:'Jardim',x:30,z:16},
  {name:'Cripta',x:-66,z:52},{name:'Ossuário',x:66,z:52},
  {name:'Acampamento',x:-132,z:18},{name:'Círculo de Pedras',x:132,z:18},{name:'Cabana do Ermitão',x:-118,z:-62},
  {name:'Cemitério',x:108,z:94},{name:'Encruzilhada',x:52,z:-92}];

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

// limite do mapa: muro velho de pedra, com a mata continuando do lado de fora
W(-HALF.x-1,HALF.x+1,-HALF.z-1,-HALF.z,5,'limite'); W(-HALF.x-1,HALF.x+1,HALF.z,HALF.z+1,5,'limite');
W(-HALF.x-1,-HALF.x,-HALF.z,HALF.z,5,'limite'); W(HALF.x,HALF.x+1,-HALF.z,HALF.z,5,'limite');

// ---- paredes da catedral: portas principais nos quatro lados e duas brechas ----
// Oeste: porta dos Caçadores e uma porta lateral para o Coro. Leste: porta dos Cultistas e uma brecha para a Nave.
// Sul: a grande porta do Nártex e uma brecha. Norte: uma porta estreita para a Encruzilhada.
const PORTAIS=[ // para o cliente desenhar batentes, lintéis e lanternas
  {nome:'Porta Oeste',eixo:'z',fixo:-96.5,a:-4,b:4},{nome:'Porta do Coro',eixo:'z',fixo:-96.5,a:27,b:31},
  {nome:'Porta Leste',eixo:'z',fixo:96.5,a:-4,b:4},{nome:'Brecha Leste',eixo:'z',fixo:96.5,a:-24,b:-20,brecha:true},
  {nome:'Grande Porta',eixo:'x',fixo:58.5,a:-4,b:4},{nome:'Brecha Sul',eixo:'x',fixo:58.5,a:-24,b:-20,brecha:true},
  {nome:'Porta Norte',eixo:'x',fixo:-58.5,a:33,b:38}];
const vaosDe=(eixo,fixo)=>PORTAIS.filter(p=>p.eixo===eixo&&p.fixo===fixo).map(p=>[p.a,p.b]).sort((u,v)=>u[0]-v[0]);
muro('x',-58.5,-97,97,vaosDe('x',-58.5),14); muro('x',58.5,-97,97,vaosDe('x',58.5),14);
muro('z',-96.5,-58,58,vaosDe('z',-96.5),14); muro('z',96.5,-58,58,vaosDe('z',96.5),14);
escombros(97.5,-25,1.6,4,1.2); escombros(-25,59.8,1.6,4,1.2); // pedras que caíram para fora das brechas

// ---- Claustro: entradas de larguras e posições diferentes, um canto desabado que deixa ver mas não passar ----
muro('x',CLAUSTRO.z1,CLAUSTRO.x1,44,[[-40,-33],[22,25]]);          // norte: uma larga, uma estreita
W(44,56,-28.6,-27.4,1.5,'escombro'); W(55.4,56.6,-28,-22,1.5,'escombro'); // canto nordeste caído: janela de escombros, sem passagem
muro('x',CLAUSTRO.z2,CLAUSTRO.x1,CLAUSTRO.x2,[[-24,-19],[30,38]]);  // sul
muro('z',CLAUSTRO.x1,CLAUSTRO.z1,CLAUSTRO.z2,[[-8,-5],[8,14]]);     // oeste: passagem apertada da Sacristia e uma porta larga
muro('z',CLAUSTRO.x2,-22,CLAUSTRO.z2,[[4,10]]);                     // leste
// dentro do Claustro: poço no centro, jardim de sebes, criptas soltas, colunata quebrada, árvore morta
W(3,9,-7,-1,1.1,'poco');
[[20,34,10,11.4],[33,34.4,11.4,22],[24,30,18,19.4],[14,20,-18,-16.6]].forEach(([a,b,c,d])=>W(a,b,c,d,2.6,'sebe'));
[[-50,-44,20,21.2],[40,46,18,19.2],[-8,-2,22,23.2]].forEach(([a,b,c,d])=>W(a,b,c,d,1.2,'sebe'));
[[-46,-38,-16,-8],[-30,-22,6,14],[-12,-2,12,19],[40,48,-15,-7],[-20,-12,-24,-19]].forEach(([a,b,c,d])=>W(a,b,c,d,6,'cripta'));
[[-40,5],[-32,2.8],[-24,4.2],[-8,6],[0,3.1]].forEach(([x,h])=>coluna(x,-20,h));   // colunata do lado norte, uma faltando
colunaCaida(-18,-14,-17,-8);
W(-19,-18,-4,-3,8,'arvore');
[[-40,-36,17,19],[12,16,-23,-21],[46,50,23,25]].forEach(([a,b,c,d])=>W(a,b,c,d,1.1,'tumba'));

// ---- Capela Oeste: sala apertada, porta estreita ao sul e porta lateral a leste ----
muro('x',-34,-80,-50,[[-61,-58]]); muro('z',-80,-58,-34); muro('z',-50,-58,-34,[[-51,-47]]);
// ---- Abside: meia-lua de colunas atrás do altar, frente aberta com túmulos como cobertura ----
for(let k=0;k<9;k++){ const a=Math.PI*(1.08+k*0.105), x=Math.cos(a)*12.5, z=-47+Math.sin(a)*10.5; W(x-1.2,x+1.2,z-1.2,z+1.2,12,'pedra'); }
[[-8,-5,-37,-35.6],[4,7.5,-38,-36.6],[-2,1,-31,-29.8]].forEach(([a,b,c,d])=>W(a,b,c,d,1.1,'tumba'));
// ---- Capela Leste: o lado sul desabou; sobrou a porta oeste e uma brecha cheia de escombros ----
muro('z',50,-58,-34,[[-50,-46]]); muro('z',80,-58,-34); muro('x',-34,50,54); muro('x',-34,64,80); // brecha de 10 m; o resto da parede ficou em pé
escombros(59,-36,3,6,1.4); escombros(69,-39,2.4,5,1.3); escombros(58,-53,2.2,4,1.1); colunaCaida(55,-43,66,-42);
// ---- Cripta (sudoeste): sala sem janelas, entrada por um corredor em L. Perdeu o altar para a floresta; virou esconderijo ----
muro('x',40,-80,-52); muro('z',-80,40,58); muro('z',-52,40,58,[[44,48]]);
muro('z',-48,34,52); muro('z',-52,34,40); muro('x',52,-52,-48); // boca do corredor abre para o anel, com folga
// ---- Nártex: salão de entrada; a grande porta sul se abre direto nele ----
muro('x',36,-16,16); muro('z',-16,36,58,[[41,45]]); muro('z',16,36,58,[[50,54]]);
[[-9,-6,49,51],[5,8,41,43],[-2,1,53,55]].forEach(([a,b,c,d])=>W(a,b,c,d,1.1,'tumba'));
// ---- Ossuário (sudeste): duas portas e uma pilha de ossos no meio para dar a volta ----
muro('x',34,50,80,[[54,58]]); muro('z',50,34,58,[[48,52]]); muro('z',80,34,58);
W(58,63,41,47,4.5,'ossos');

// ---- o anel entre o Claustro e as capelas ----
// Sacristia (oeste, lado dos Caçadores): atalho estreito da porta oeste para dentro do Claustro
muro('x',-12,-70,-60); muro('x',-4,-70,-60); muro('z',-70,-12,-4,[[-9,-6]]); muro('z',-60,-12,-4,[[-8,-5]]);
// Nave em ruínas (leste, lado dos Cultistas): desabamento que força desvio
escombros(72,-17,4.2,10,1.7); colunaCaida(66,13,80,14.4); coluna(76,-6,3.5);
// biombos soltos quebrando as linhas longas de visão
[[-30,-29,-58,-48],[28,29,-58,-50],[-89,-80,-24,-23],[82,90,24,25],[34,35,40,50],[-89,-80,22,23]].forEach(([a,b,c,d])=>W(a,b,c,d,9,'biombo'));

const PILLARS=[[-40,-50],[-22,-40],[20,-40],[38,-52],[-44,38],[24,40],[42,52],[-78,-26],[-76,26],[88,-30],[88,34]];
const PEWS=[[-44,-26,42,45],[-44,-28,47,50],[-40,-26,52,55],[-77,-69,-55,-52],[-63,-55,-40,-37]];
// reagentes: todos no Claustro, espalhados de forma irregular (dois no poço, os mais disputados)
const REAG=[[1,-4],[11,-5],[18,-12],[29,21],[48,0],[-40,-4],[-48,24],[-36,-24],[-20,24],[-6,9]];

// ============ A FLORESTA ============
// Trilhas: faixas de chão batido onde o luar atravessa as copas. Fora delas, a mata é fechada e escura.
const TRILHAS=[
  {w:5,pts:[[-132,0],[-97,0]]},                                   // acampamento → porta oeste
  {w:5,pts:[[132,0],[97,0]]},                                     // círculo de pedras → porta leste
  {w:4,pts:[[-114,-76],[114,-76],[114,76],[-114,76],[-114,-76]]}, // trilha do perímetro, em volta da catedral
  {w:4,pts:[[-118,-45],[-114,-30],[-114,-8],[-104,-2]]},          // cabana do Ermitão
  {w:4,pts:[[35.5,-59],[36,-70],[44,-82]]},                        // porta norte → encruzilhada
  {w:4,pts:[[44,-82],[4,-92],[-40,-88]]},                         // encruzilhada → Menires
  {w:4,pts:[[44,-82],[84,-94],[114,-76]]},
  {w:5,pts:[[0,59],[12,74],[34,84]]},                             // grande porta → Carvalho Oco
  {w:3.5,pts:[[-22,59],[-38,76]]},{w:3.5,pts:[[-96,29],[-114,34]]},{w:3.5,pts:[[97,-22],[114,-30]]},
  {w:4,pts:[[114,44],[104,62]]}];                                 // perímetro → cemitério
const SEG_TRILHA=[]; TRILHAS.forEach(T=>{ for(let i=1;i<T.pts.length;i++) SEG_TRILHA.push({ax:T.pts[i-1][0],az:T.pts[i-1][1],bx:T.pts[i][0],bz:T.pts[i][1],w:T.w}); });
function distSeg(x,z,s){ const dx=s.bx-s.ax,dz=s.bz-s.az,L=dx*dx+dz*dz; let t=L?((x-s.ax)*dx+(z-s.az)*dz)/L:0; t=clamp(t,0,1); return Math.hypot(x-(s.ax+dx*t),z-(s.az+dz*t)); }
const naTrilha=(x,z,folga)=>SEG_TRILHA.some(s=>distSeg(x,z,s)<s.w/2+(folga||0));
// Clareiras: abertas ao céu. O luar ilumina (bom para os Caçadores) e a mata em volta esconde quem chega.
const CLAREIRAS=[{x:-132,z:0,r:15},{x:132,z:0,r:15},{x:-40,z:-88,r:16},{x:36,z:89,r:13},{x:108,z:78,r:19},{x:44,z:-83,r:9},{x:-118,z:-50,r:10}];

// Acampamento dos Caçadores: barracas, caixotes e fogueira
[[-140,-10,3.4,3],[-142,7,3.4,3],[-126,-12,3,3.2],[-138,12,3,2.6]].forEach(([x,z,w,d])=>W(x-w/2,x+w/2,z-d/2,z+d/2,2.6,'tenda'));
[[-124,9,1.2,1.2],[-122.6,10.4,1,1],[-128,-15,1.4,1]].forEach(([x,z,w,d])=>W(x-w/2,x+w/2,z-d/2,z+d/2,1,'caixote'));
// Círculo de Pedras dos Cultistas: menires em anel, aberto para o oeste (a porta leste da catedral)
for(let k=0;k<10;k++){ const a=k/10*Math.PI*2; if(Math.cos(a)<-.55||k===3||k===7) continue; const x=132+Math.cos(a)*10, z=Math.sin(a)*10; W(x-.7,x+.7,z-.7,z+.7,3.6+((k*7)%3)*.5,'menir'); }
// Menires (altar): anel de nove pedras altas com três vãos
for(let k=0;k<12;k++){ if(k===1||k===5||k===8) continue; const a=k/12*Math.PI*2+.2, x=-40+Math.cos(a)*8.5, z=-88+Math.sin(a)*8.5; W(x-.8,x+.8,z-.8,z+.8,4.2+((k*5)%4)*.4,'menir'); }
// Carvalho Oco (altar): tronco gigante atrás do altar e raízes que servem de cobertura baixa
W(33,39,93.5,98,14,'carvalho');
[[27,31,91,92.2],[41,45.5,90.5,91.7],[29.5,31,95,99],[41,42.3,94.5,99]].forEach(([a,b,c,d])=>W(a,b,c,d,1.1,'raiz'));
// Cabana do Ermitão
muro('x',-55,-123,-113,null,3.6,'madeira'); muro('x',-45,-123,-113,null,3.6,'madeira'); muro('z',-123,-55,-45,null,3.6,'madeira'); muro('z',-113,-55,-45,[[-52,-48]],3.6,'madeira');
// Cemitério: lápides em fileiras tortas, um mausoléu e uma árvore morta
const LAPIDES=[]; for(const z of [66,72,78,84]) for(let x=96;x<=120;x+=5){ if(sorte()<.25) continue; const ox=(sorte()-.5)*1.4, oz=(sorte()-.5)*.8; LAPIDES.push(W(x+ox-.6,x+ox+.6,z+oz-.2,z+oz+.2,1.1,'lapide')); }
W(114,120,86,92,5,'mausoleu'); W(97,98,90,91,7,'arvore');
// Encruzilhada: marco de pedra no cruzamento das trilhas
W(41.5,42.5,-79.5,-78.5,2.4,'marco');

// pontos de tarefa da floresta (fixos; a mata é gerada em volta deles)
const PISTA_PTS=[[-128,-88],[-70,-68],[0,-100],[86,-100],[128,-44],[-134,62],[-66,98],[74,100]];
const ERVA_PTS=[[-88,-96],[-20,-70],[74,-66],[122,-96],[126,32],[64,70],[-8,98],[-104,88]];
const SENTINELA_PTS=[[-58,-30],[16,-34],[50,-30],[0,31],[-40,-74],[24,76]];
PISTA_PTS.forEach(([x,z])=>W(x-.8,x+.8,z+1.2,z+2.2,1.3,'santuario')); // santuário em ruínas; o sinal fica na frente dele

// Árvores, pedras e troncos caídos: gerados com a mesma semente, então todos veem a mesma floresta.
const ARVORES=[], ARBUSTOS=[];
{
  const livreDeTudo=(x,z,folga)=>{
    if(naCatedral(x,z,4+folga)) return false;
    if(Math.abs(x)>HALF.x-1.5||Math.abs(z)>HALF.z-1.5) return false;
    if(naTrilha(x,z,1+folga)) return false;
    for(const c of CLAREIRAS) if(Math.hypot(x-c.x,z-c.z)<c.r+folga) return false;
    for(const [px,pz] of PISTA_PTS.concat(ERVA_PTS)) if(Math.hypot(x-px,z-pz)<3.5+folga) return false;
    for(const w of WALLS) if(x>w.x1-1.5-folga&&x<w.x2+1.5+folga&&z>w.z1-1.5-folga&&z<w.z2+1.5+folga) return false;
    return true; };
  // grade de ocupação para manter espaçamento mínimo entre troncos sem O(n²)
  const cel=4, ocup=new Map(), chave=(x,z)=>Math.floor(x/cel)*10000+Math.floor(z/cel);
  const perto=(x,z,d)=>{ const cx=Math.floor(x/cel),cz=Math.floor(z/cel); for(let i=-1;i<=1;i++) for(let j=-1;j<=1;j++){ const L=ocup.get((cx+i)*10000+cz+j); if(L) for(const q of L) if(Math.hypot(q.x-x,q.z-z)<d) return true; } return false; };
  const guarda=q=>{ const k=chave(q.x,q.z); let L=ocup.get(k); if(!L) ocup.set(k,L=[]); L.push(q); };
  for(let t=0;t<9000&&ARVORES.length<1150;t++){
    const x=(sorte()*2-1)*(HALF.x-2), z=(sorte()*2-1)*(HALF.z-2);
    // densidade: mais fechada perto das bordas do mapa, mais rala junto da catedral
    const borda=Math.min(HALF.x-Math.abs(x),HALF.z-Math.abs(z)), dens=borda<14?1:borda<30?.85:.6;
    if(sorte()>dens) continue;
    const tipo=sorte()<.62?0:sorte()<.6?1:2; // 0 pinheiro escuro, 1 árvore larga, 2 árvore morta
    const r=tipo===1?.5+sorte()*.25:tipo===0?.32+sorte()*.18:.26+sorte()*.14;
    if(!livreDeTudo(x,z,r)||perto(x,z,4.2)) continue;
    const q={x:r2(x),z:r2(z),r:r2(r),tipo,s:r2(.8+sorte()*.55),rot:r2(sorte()*6.283),inc:r2((sorte()-.5)*.12)};
    guarda(q); ARVORES.push(q);
  }
  ARVORES.forEach(q=>W(q.x-q.r,q.x+q.r,q.z-q.r,q.z+q.r,10,'arvore_f'));
  // pedras cobertas de musgo e troncos caídos: cobertura baixa no meio da mata
  for(let t=0,n=0;t<3000&&n<70;t++){ const x=(sorte()*2-1)*(HALF.x-3), z=(sorte()*2-1)*(HALF.z-3), w=1+sorte()*1.4, d=.9+sorte()*1.2;
    if(!livreDeTudo(x,z,Math.max(w,d)*.6)||perto(x,z,3.4)) continue; const q={x,z,r:Math.max(w,d)/2}; guarda(q); W(x-w/2,x+w/2,z-d/2,z+d/2,.8+sorte()*.7,'rocha'); n++; }
  for(let t=0,n=0;t<3000&&n<34;t++){ const x=(sorte()*2-1)*(HALF.x-4), z=(sorte()*2-1)*(HALF.z-4), len=4+sorte()*4, aoX=sorte()<.5;
    const x1=aoX?x-len/2:x-.45, x2=aoX?x+len/2:x+.45, z1=aoX?z-.45:z-len/2, z2=aoX?z+.45:z+len/2;
    let ok=true; for(let k=0;k<=4&&ok;k++){ const px=lerp(x1,x2,k/4), pz=lerp(z1,z2,k/4); if(!livreDeTudo(px,pz,.6)||perto(px,pz,2.4)) ok=false; }
    if(!ok) continue; for(let k=0;k<=4;k++) guarda({x:lerp(x1,x2,k/4),z:lerp(z1,z2,k/4)}); W(x1,x2,z1,z2,.95,'tronco'); n++; }
  // arbustos: só visual (não bloqueiam), mais densos na mata e rareando nas trilhas
  for(let t=0;t<6000&&ARBUSTOS.length<1500;t++){ const x=(sorte()*2-1)*(HALF.x-1), z=(sorte()*2-1)*(HALF.z-1);
    if(naCatedral(x,z,1.5)||naTrilha(x,z,-.5)) continue; let dentro=false; for(const c of CLAREIRAS) if(Math.hypot(x-c.x,z-c.z)<c.r*.7) dentro=true; if(dentro) continue;
    ARBUSTOS.push({x:r2(x),z:r2(z),s:r2(.5+sorte()*.9),rot:r2(sorte()*6.283),tipo:sorte()<.55?0:1}); }
}

// Luzes: velas dentro da catedral; lanternas, fogueiras e braseiros lá fora. [x, z, tipo]
const CANDLES=[[-58,-40],[-73,-50],[5,-40],[-6,-54],[58,-40],[73,-51],[-60,52],[-73,44],[-10,40],[10,55],[62,38],[75,53],
  [-84,-4],[-84,4],[84,-4],[84,4],[-65,-6],[-30,-44],[34,46],[76,-30],
  [-136,0,'fogueira'],[128,-6,'braseiro'],[128,6,'braseiro'],[-116,-47,'lanterna'],[-110,-50,'lanterna'],[103,97,'lanterna'],[113,78,'lanterna'],
  [42,-78,'lanterna'],[-100,-6,'lanterna'],[-100,6,'lanterna'],[100,-6,'lanterna'],[100,6,'lanterna'],[-6,62,'lanterna'],[6,62,'lanterna'],[31,-62,'lanterna']];
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
function raioParede(ox,oz,dx,dz,alcance,inf,alturaEm){ let best=alcance; const G=grade(TALL), n=Math.ceil(alcance/(G.c*.5)); G.vez++;
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
    nid:1,feit:{},buff:{ess:0,selo:0},firstContact:null,matchId:opts.matchId||String(Date.now())};
  enterPick(g,1);
  return g;
}
function ev(g,e){ g.events.push(e); }
function feed(g,msg,cls,team){ ev(g,{type:'feed',msg,cls:cls||'',team:team||null}); }
function logE(g,e,data){ g.log.push(Object.assign({t:r2(g.rt),r:g.round,ev:e},data||{})); }
const humansInRole=(g,role)=>g.slots.filter(s=>s.cid&&roleOf(g,s.team)===role);
const humans=g=>g.slots.filter(s=>s.cid);

function resetRoundState(g){
  g.altars=ALTARS.map((d,i)=>({i,name:d.name,x:d.x,z:d.z,state:'dormant',chosen:false,decoy:false,prog:0,cp:0,seal:0,sealIdle:0,localized:false,lastN:1,startT:0,maxProg:0,doneT:null,selo:1,sealers:[],arrived:false}));
  g.reagents.forEach(R=>{ R.has=true; R.t=0; });
  g.know=g.altars.map(()=>'?'); g.visit=g.altars.map(()=>-99); g.decoys=2; g.transferUsed=false;
  g.proj=[]; g.pickups=[]; g.flares.forEach(f=>f.t=0); g.actors=[]; g.picks=[]; g.ready={}; g.buff={ess:0,selo:0};
  g.endAt=0; g.lastResolveT=0; g.rt=0; g.firstContact=null; g.momento=0; prepararNoite(g);
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
  g.slots.forEach((s,si)=>{ if(s.cid) return; const role=roleOf(g,s.team); const opts=CLASS_BY_TEAM[role]; const c=opts.find(o=>!taken[role].includes(o))||opts[si%2]; taken[role].push(c);
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
    fervor:100,sanity:100,invuln:0,sealing:-1,hold:{key:'',t:0,max:0},lantern:false,carga:100,flares:2,sensorCd:0,abilCd:0,lastShotT:-9,moving:false,
    panicT:-9,blindT:0,stunT:0,runeT:0,shield:0,shieldT:0,pushedT:-9,hist:[],feitico:feit,obolos:CFG.obolosInicio,veuT:0,slowT:0,revelT:0,curaT:0,amuleto:false,
    inp:{mx:0,mz:0,sp:false,yaw:0,pitch:0,fire:false,use:false,use2:false,viewT:0,c:{q:0,f:0,g:0,r:0,ab:0}},lc:{q:0,f:0,g:0,r:0,ab:0},
    ai:{path:[],goal:null,goalKey:'',repath:0,stuckT:0,lx:0,lz:0,alert:null,target:null,wait:0,decoyUsed:false,strafe:1,strafeT:0,patrol:null,patrolT:0}};
}
function spawn(g,a,first,k){
  const s=SPAWN[a.team], sameTeam=g.actors.filter(b=>b.team===a.team); const idx=first?sameTeam.indexOf(a):Math.floor(Math.random()*2);
  a.x=s.x+rand(-.6,.6); a.z=s.z+(idx===0?-2.5:2.5); a.yaw=s.yaw; a.pitch=0; a.inp.yaw=a.yaw; a.inp.pitch=0;
  a.hp=a.maxHp; a.st='alive'; a.invuln=2; a.sealing=-1; a.hold={key:'',t:0,max:0}; a.ammo=8; a.reserve=a.cls==='soldado'?64:48; a.fervor=100; a.reloadT=0;
  a.blindT=0; a.stunT=0; a.runeT=0; a.shield=0; a.hist=[]; a.ai.path=[]; a.ai.goal=null; a.ai.goalKey=''; a.ai.alert=null;
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
}
function dropHuman(g,cid){
  const s=g.slots.find(s=>s.cid===cid); if(!s) return; s.cid=null;
  const a=g.actors.find(a=>a.cid===cid); if(a){ a.cid=null; a.human=false; a.inp.fire=false; a.inp.use=false; }
  delete g.ready[cid];
  if((g.phase==='intro'||g.phase==='summary')&&allReady(g)){ if(g.phase==='intro') startPlay(g); else afterSummary(g); }
}
// Devolve a vaga (e o personagem, com vida, reagentes e posição) a quem reconectou.
function reclaimHuman(g,slotIdx,cid,name){
  const s=g.slots[slotIdx]; if(!s||s.cid) return false;
  s.cid=cid; if(name) s.name=name;
  const a=g.actors.find(a=>a.slot===slotIdx);
  if(a){ a.cid=cid; a.human=true; if(name) a.name=name; a.inp={mx:0,mz:0,sp:false,yaw:a.yaw,pitch:0,fire:false,use:false,use2:false,viewT:0,c:{q:0,f:0,g:0,r:0,ab:0}}; a.lc={q:0,f:0,g:0,r:0,ab:0}; a.hold={key:'',t:0,max:0};
    g.cls[cid]=a.cls; }
  else { const role=roleOf(g,s.team); g.cls[cid]=CLASS_BY_TEAM[role][0]; }
  logE(g,'reconnect',{who:s.name});
  return true;
}


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
  {id:'ermitao',nome:'o Ermitão',...pontoNav(-119,-50),time:'H',vende:['municao','flare','oleo','pocao','amuleto']},
  {id:'carpideira',nome:'a Carpideira',...pontoNav(105,95),time:'C',vende:['reagente','cinza','pocao','amuleto']},
  {id:'mercador',nome:'o Mercador sem Rosto',...pontoNav(46,-84),time:null,vende:['reagente','municao','pocao']}];
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
  {H:[{id:'final',nome:'Impeça o terceiro ritual até o amanhecer',meta:0,rec:0}],
   C:[{id:'final',nome:'Complete três rituais antes do amanhecer',meta:0,rec:0}]}];
const momentoDe=rt=>{ const M=CFG.momentos; for(let i=0;i<M.length;i++) if(rt<M[i].ate) return i; return M.length-1; };
const fendas=g=>g.altars.filter(A=>A.state==='fenda').length;
function prepararNoite(g){
  const pistas=PONTOS_DEF.pista.slice().sort(()=>Math.random()-.5).slice(0,3);
  g.pontos=[...pistas.map(p=>({tipo:'pista',x:p.x,z:p.z,ativo:true})),
    ...PONTOS_DEF.sentinela.map(p=>({tipo:'sentinela',x:p.x,z:p.z,ativo:true,acesa:false})),
    ...PONTOS_DEF.erva.map(p=>({tipo:'erva',x:p.x,z:p.z,ativo:true,t:0})),
    ...PONTOS_DEF.tumulo.map(p=>({tipo:'tumulo',x:p.x,z:p.z,ativo:true}))].map((p,i)=>Object.assign(p,{i}));
  g.sal=[]; g.realocar=[]; g.tarefas={H:[],C:[]};
}
function novoMomento(g,mi){
  g.momento=mi; const T=TAREFAS[mi]; g.tarefas={H:T.H.map(t=>({...t,prog:0,feita:false})),C:T.C.map(t=>({...t,prog:0,feita:false}))};
  if(mi>0){ ev(g,{type:'momento',i:mi,nome:CFG.momentos[mi].nome}); feed(g,mi===1?'A Vigília começou: os altares despertam.':'A Hora Morta chegou: rituais mais rápidos, a névoa se fecha.','big'); }
  logE(g,'momento',{i:mi,nome:CFG.momentos[mi].nome});
}
function tarefaProg(g,role,id,n){ const t=(g.tarefas&&g.tarefas[role]||[]).find(t=>t.id===id); if(!t||t.feita||!t.meta) return;
  t.prog=Math.min(t.meta,t.prog+(n||1)); if(t.prog>=t.meta){ t.feita=true; g.actors.filter(a=>a.team===role).forEach(a=>a.obolos+=t.rec);
    feed(g,`Tarefa cumprida: ${t.nome}. +${t.rec} óbolos para cada um.`,role==='H'?'h':'c',role); ev(g,{type:'tarefa',team:role,id}); logE(g,'tarefa',{team:role,id}); } }
function comprar(g,cid,npcId,itemId){
  const a=g.actors.find(x=>x.cid===cid), n=NPCS.find(x=>x.id===npcId), it=ITENS[itemId];
  const nega=msg=>{ if(a) ev(g,{type:'feed',msg,cls:'',to:a.id}); };
  if(!a||!n||!it||a.st!=='alive') return; if(dist(a,n)>3.2) return nega('Chegue mais perto para negociar.');
  if(!n.vende.includes(itemId)||(n.time&&n.time!==a.team)||(it.time&&it.time!==a.team)) return nega('Este item não é para você.');
  if(it.momento&&g.momento<it.momento) return nega(`${it.nome} só aparece a partir da Vigília.`);
  if(a.obolos<it.preco) return nega(`Faltam ${it.preco-a.obolos} óbolos.`);
  if(itemId==='reagente'&&a.reag>=CFG.carry) return nega('Você já carrega 2 reagentes.');
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
  const mi=momentoDe(g.rt); if(mi!==g.momento) novoMomento(g,mi);
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
// sentinelas acesas denunciam consagrações e rituais por perto
function sentinelaAvisa(g,A,oque){ const s=g.pontos.find(p=>p.tipo==='sentinela'&&p.acesa&&Math.hypot(p.x-A.x,p.z-A.z)<24); if(!s) return;
  if(g.know[A.i]==='?'||g.know[A.i]==='limpo') g.know[A.i]='desperto'; feed(g,`Uma sentinela tocou: ${oque} em ${A.name}.`,'big h','H'); logE(g,'sentinela',{altar:A.name}); }

// ============ LUZ E SANIDADE ============
function lightAt(g,x,z){
  let L=0;
  for(const [cx,cz] of CANDLES){ const d=Math.hypot(x-cx,z-cz); if(d<8.5) L+=1-d/8.5; }
  if(x>CLAUSTRO.x1&&x<CLAUSTRO.x2&&z>CLAUSTRO.z1&&z<CLAUSTRO.z2) L+=0.55; // luar: o Claustro não tem teto
  else if(!naCatedral(x,z)){ // na floresta, o luar só chega nas clareiras e ao longo das trilhas; a mata fechada é breu
    let lua=0; for(const c of CLAREIRAS){ const d=Math.hypot(x-c.x,z-c.z); if(d<c.r+2) lua=Math.max(lua,.55*clamp((c.r+2-d)/4,0,1)); }
    if(lua<.42) for(const sg of SEG_TRILHA){ const d=distSeg(x,z,sg); if(d<sg.w/2+1){ lua=Math.max(lua,.42*clamp((sg.w/2+1-d)/1.5,0,1)); if(lua>=.42) break; } }
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
    a.inp.fire=!!inp.fire; a.inp.use=!!inp.use; a.inp.use2=!!inp.use2; if(isFinite(inp.viewT)) a.inp.viewT=+inp.viewT; if(inp.c) a.inp.c=inp.c; }
  a.yaw=a.inp.yaw; a.pitch=a.inp.pitch;
  let mx=a.inp.mx, mz=a.inp.mz; const len=Math.hypot(mx,mz);
  a.moving=false;
  if(a.st==='dead'||a.stunT>0||len<0.01) return;
  mx/=len; mz/=len;
  let sp=a.speed*(a.slowT>0?.6:1); if(a.st==='down') sp=.8; else { if(a.inp.sp&&mz<0&&a.sealing<0) sp=CFG.sprint*(a.speed/CFG.speed); if(a.sealing>=0) sp*=.4; if(a.hp<a.maxHp*.5) sp*=.9; }
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
  let tWall=raioParede(ox,oz,dx,dz,90,0,d=>oy+dy*d);
  if(dy<0){ const tf=-oy/dy; if(tf<tWall) tWall=tf; }
  let best=null,bt=tWall,head=false;
  if(!forceMiss) for(const e of enemiesOf(g,a)){ if(e.st==='dead') continue; const p=posAt(e,T);
    const fx=ox-p.x,fz=oz-p.z,A2=dx*dx+dz*dz,B=2*(fx*dx+fz*dz),C=fx*fx+fz*fz-.45*.45,disc=B*B-4*A2*C; if(disc<0||A2<1e-9) continue;
    const t=(-B-Math.sqrt(disc))/(2*A2); if(t<0||t>bt) continue; const y=oy+dy*t, top=e.st==='down'?.6:1.95; if(y<0||y>top) continue; best=e; bt=t; head=e.st==='alive'&&y>1.5; }
  ev(g,{type:'tracer',a:a.id,x0:r2(ox),y0:r2(oy-.2),z0:r2(oz),x1:r2(ox+dx*bt),y1:r2(oy+dy*bt),z1:r2(oz+dz*bt),alvo:best?best.id:null,cab:!!head});
  if(best){ const d=bt, k=clamp(1-(d-CFG.quedaPerto)/(CFG.quedaLonge-CFG.quedaPerto)*(1-CFG.quedaMin),CFG.quedaMin,1);
    damage(g,best,Math.round((head?26:CFG.quedaDano)*k),a); }
}
function fireHuman(g,a){
  a.fireCd=.42; const y=a.inp.yaw,p=a.inp.pitch; const dx=-Math.sin(y)*Math.cos(p),dy=Math.sin(p),dz=-Math.cos(y)*Math.cos(p);
  const T=clamp(a.inp.viewT||g.t,g.t-CFG.compensacaoMax,g.t);
  traceShot(g,a,a.x,1.62,a.z,dx,dy,dz,T,false);
}
function fireBotAt(g,a,tx,ty,tz,miss){
  a.fireCd=.55+rand(0,.2); let dx=tx-a.x,dy=ty-1.62,dz=tz-a.z; const l=Math.hypot(dx,dy,dz)||1; dx/=l; dy/=l; dz/=l;
  traceShot(g,a,a.x,1.62,a.z,dx,dy,dz,g.t,miss);
}
let PID=1;
function castSigil(g,a,dx,dy,dz){ a.veuT=0;
  a.lastShotT=g.t; a.fireCd=a.human?.42:1+rand(0,.3); a.fervor-=CFG.sigilCusto;
  ev(g,{type:'sfx',k:'sigil',x:r2(a.x),z:r2(a.z),a:a.id});
  const rx=Math.cos(a.yaw), rz=-Math.sin(a.yaw); const ox=a.x+rx*.3, oy=1.45, oz=a.z+rz*.3;
  const tx=a.x+dx*40, ty=1.62+dy*40, tz=a.z+dz*40; let vx=tx-ox,vy=ty-oy,vz=tz-oz; const l=Math.hypot(vx,vy,vz)||1; const sp=CFG.sigilVel;
  g.proj.push({id:PID++,x:ox,y:oy,z:oz,vx:vx/l*sp,vy:vy/l*sp,vz:vz/l*sp,owner:a.id,team:a.team,life:2.5,dmg:CFG.sigilDano});
}
function reload(g,a){ if(a.team!=='H'||a.reloadT>0||a.ammo>=8||a.reserve<=0) return; a.reloadT=1.6; ev(g,{type:'reload',a:a.id,x:r2(a.x),z:r2(a.z)}); }
function damage(g,t,amt,src){
  if(t.st==='dead'||t.invuln>0) return;
  if(!g.firstContact){ g.firstContact=r2(g.rt); logE(g,'first_contact',{}); }
  if(t.st==='down'){ t.downT-=amt/8; t.att[src.id]=g.t; return; }
  if(t.shield>0){ const s=Math.min(t.shield,amt); t.shield-=s; amt-=s; }
  t.hp-=amt; t.hitT=g.t; t.att[src.id]=g.t; t.veuT=0;
  ev(g,{type:'hurt',a:t.id,s:src.id,sx:r2(src.x),sz:r2(src.z)}); ev(g,{type:'hit',a:src.id,alvo:t.id,derrubou:t.hp<=0});
  if(!t.human) t.ai.alert={x:src.x,z:src.z,t:g.t};
  if(t.hp<=0) goDown(g,t,src);
}
function goDown(g,t,src){
  if(t.revUsed){ kill(g,t,src); return; }
  if(src&&src.team!==t.team) src.obolos+=1;
  t.st='down'; t.hp=0; t.downT=CFG.downTime; t.sealing=-1; t.hold={key:'',t:0,max:0}; t.lantern=false;
  feed(g,`${src.name} derrubou ${t.name}`,t.team==='C'?'h':'c'); logE(g,'down',{who:t.name,team:t.team,by:src.name,x:r2(t.x),z:r2(t.z)});
}
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
  else if(F==='empurrao'){
    let n=0; for(const e of enemiesOf(g,a)){ if(e.st!=='alive') continue; const dx=e.x-a.x,dz=e.z-a.z,d=Math.hypot(dx,dz); if(d>3.8||d<.01) continue; if((dx*fx+dz*fz)/d<.34) continue;
      const ux=dx/d,uz=dz/d; for(let s=0;s<8;s++) moverSeguro(e,e.x+ux*.5,e.z+uz*.5,.4);
      const was=e.sealing>=0; e.sealing=-1; e.hold={key:'',t:0,max:0}; e.pushedT=g.t; e.stunT=Math.max(e.stunT,.45); damage(g,e,10,a); n++; alvos.push(e.id);
      if(was) logE(g,'push_seal',{who:e.name,by:a.name}); }
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
  for(const b of g.actors){ if(b===a||b.st!=='down'||dist(a,b)>1.8) continue;
    out.e=b.team===a.team?{key:'rev'+b.id,type:'revive',b:b.id,label:`Segure E para reanimar ${b.name}`,time:CFG.revive}:{key:'exe'+b.id,type:'execute',b:b.id,label:`Segure E para executar ${b.name}`,time:CFG.execute};
    return out; }
  for(const n of NPCS){ if(dist(a,n)>2.6) continue; if(n.time&&n.time!==a.team){ out.info=`${n.nome[0].toUpperCase()+n.nome.slice(1)} não negocia com você`; return out; }
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
      if(A.chosen&&A.state==='dormant'){ if(a.reag>0) out.e={key:'cons'+A.i,type:'consecrate',A:A.i,label:'Segure E para consagrar',time:CFG.consecrate}; else out.info='Você precisa de 1 reagente'; return out; }
      if(A.chosen&&A.state==='awake'&&!A.decoy&&g.momento<1){ out.info='Este altar só desperta na Vigília'; return out; }
      if(A.chosen&&A.state==='awake'&&!A.decoy&&g.momento<2&&fendas(g)>=2){ out.info='O último ritual só pode começar na Hora Morta'; return out; }
      if(A.chosen&&A.state==='awake'&&!A.decoy){ if(a.reag>0||CFG.iniciarDeGraca) out.e={key:'start'+A.i,type:'start',A:A.i,label:'Segure E para iniciar o ritual',time:CFG.start}; else out.info='Você precisa de 1 reagente'; return out; }
      if(!A.chosen&&A.state==='dormant'){
        if(g.decoys>0) out.e={key:'dec'+A.i,type:'decoy',A:A.i,label:`Segure E para plantar um Chamariz (${g.decoys})`,time:CFG.decoy};
        const from=transferSource(g,a);
        if(!g.transferUsed&&a.reag>0&&from) out.t={key:'tr'+A.i,type:'transfer',A:A.i,from:from.i,label:`Segure T para transferir ${from.name} para cá`,time:CFG.transfer};
        if(!out.e&&!out.t) out.info='Sem Chamarizes restantes';
        return out; }
    }
  }
  return out;
}
function transferSource(g,a){ const c=g.altars.filter(A=>A.chosen&&A.state==='dormant'); if(!c.length) return null; c.sort((x,y)=>dist(a,y)-dist(a,x)); return c[0]; }
function doAction(g,a,ctx){
  const A=ctx.A!=null?g.altars[ctx.A]:null, R=ctx.R!=null?g.reagents[ctx.R]:null, B=ctx.b!=null?g.actors.find(x=>x.id===ctx.b):null;
  switch(ctx.type){
    case 'ponto': { const p=g.pontos[ctx.P]; if(!p||!p.ativo) break;
      if(p.tipo==='pista'){ p.ativo=false; a.obolos+=1; tarefaProg(g,'H','pistas'); feed(g,`${a.name} examinou um sinal antigo.`,'h','H'); }
      else if(p.tipo==='sentinela'){ p.acesa=true; tarefaProg(g,'H','sentinelas'); feed(g,`${a.name} acendeu uma sentinela.`,'h','H'); ev(g,{type:'sfx',k:'flare',x:p.x,z:p.z,a:a.id}); }
      else if(p.tipo==='erva'){ p.ativo=false; p.t=50; a.obolos+=1; tarefaProg(g,'C','ervas'); }
      else if(p.tipo==='tumulo'){ p.ativo=false; if(a.reag<CFG.carry) a.reag++; else a.obolos+=2; tarefaProg(g,'C','tumulos'); feed(g,`${a.name} profanou um túmulo.`,'c','C'); }
      logE(g,'ponto',{tipo:p.tipo,by:a.name}); break; }
    case 'purge': { const was=A.decoy; tarefaProg(g,'H','purgar'); A.state='dormant'; A.decoy=false; g.know[A.i]='limpo';
      feed(g,was?`Chamariz queimado em ${A.name}.`:`${A.name} foi purgado.`,'h','H'); feed(g,`Um altar foi purgado: ${A.name}.`,'c','C');
      logE(g,'purge',{altar:A.name,decoy:was,by:a.name}); break; }
    case 'burn': R.has=false; R.t=CFG.regrow*1.2; logE(g,'burn',{by:a.name}); if(a.human) ev(g,{type:'feed',msg:'Reagente queimado.',cls:'h',to:a.id}); break;
    case 'collect': R.has=false; R.t=CFG.regrow; a.reag++; ev(g,{type:'sfx',k:'collect',x:R.x,z:R.z,a:a.id}); logE(g,'collect',{by:a.name}); if(a.human) ev(g,{type:'feed',msg:`Reagente coletado (${a.reag}/${CFG.carry}).`,cls:'c',to:a.id}); break;
    case 'consecrate': a.reag--; A.state='awake'; A.decoy=false; tarefaProg(g,'C','consagrar'); sentinelaAvisa(g,A,'consagração'); feed(g,`${A.name} consagrado.`,'c','C'); logE(g,'consecrate',{altar:A.name,by:a.name}); break;
    case 'start': if(!CFG.iniciarDeGraca) a.reag--; startRitual(g,A,a); break;
    case 'decoy': g.decoys--; A.state='awake'; A.decoy=true; feed(g,`Chamariz plantado em ${A.name}.`,'c','C'); logE(g,'decoy',{altar:A.name,by:a.name}); break;
    case 'transfer': { const F=g.altars[ctx.from]; if(!F||!F.chosen||F.state!=='dormant'||g.transferUsed||a.reag<1) break; F.chosen=false; A.chosen=true; a.reag--; g.transferUsed=true;
      feed(g,`Transferência: ${A.name} substitui ${F.name}.`,'c','C'); logE(g,'transfer',{from:F.name,to:A.name,by:a.name}); break; }
    case 'revive': if(B&&B.st==='down'){ B.st='alive'; B.hp=Math.round(B.maxHp*.45); B.revUsed=true; B.invuln=1; B.downT=0; feed(g,`${a.name} reanimou ${B.name}`,B.team==='H'?'h':'c',B.team); logE(g,'revive',{who:B.name,by:a.name}); } break;
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
  A.state='active'; A.startT=g.rt; A.localized=false; A.selo=1; A.arrived=false; A.maxN=0; A.tardio=g.momento>=2; A.grande=fendas(g)>=2;
  if(g.buff.selo>g.t){ A.selo=1.1; g.buff.selo=0; feed(g,'Selo de Luz corrompido: este ritual corre 10% mais rápido.','c','C'); }
  const n=g.actors.filter(c=>c.team==='C'&&c.st==='alive'&&inCircle(c,A)).length;
  sentinelaAvisa(g,A,'um ritual começou');
  if(A.grande){ localize(g,A,'grande'); feed(g,`O Grande Ritual começou em ${A.name}. A catedral inteira ouve.`,'big'); ev(g,{type:'grande',altar:A.i}); }
  ev(g,{type:'presage'}); feed(g,'Presságio: um ritual despertou.','big','H'); feed(g,`Ritual iniciado: ${A.name}.`,'c','C');
  logE(g,'ritual_start',{altar:A.name,channelers:Math.max(1,n),by:by.name});
}
function localize(g,A,how){ if(A.localized) return; A.localized=true; g.know[A.i]='ativo';
  feed(g,`O coro revela: ${A.name}.`,'big','H'); feed(g,`Os Caçadores descobriram ${A.name}.`,'c','C');
  logE(g,'localized',{altar:A.name,prog:r2(A.prog),how}); }
// Move e, se o resultado ainda ficar preso entre peças (cantos côncavos de escombros), desfaz o passo.
function moverSeguro(a,nx,nz,r){ const px=a.x,pz=a.z; a.x=nx; a.z=nz; resolve(a,r); if(!livre(a,r)){ a.x=px; a.z=pz; } }
function resolveAltar(g,A,kind){
  A.state=kind; A.maxProg=Math.max(A.maxProg,A.prog); g.lastResolveT=g.rt; A.sealers=[];
  g.actors.forEach(a=>{ a.dc=0; a.revUsed=false; if(a.sealing===A.i) a.sealing=-1; });
  if(kind==='fenda'){ A.doneT=r2(g.rt); ev(g,{type:'doom'}); feed(g,`Ritual completo em ${A.name}. Uma Fenda se abriu.`,'big c'); logE(g,'ritual_complete',{altar:A.name,canalizadores:A.maxN||1}); }
  else { ev(g,{type:'sealed'}); g.buff.ess=0; feed(g,`${A.name} foi selado. Um Farol se acendeu.`,'big h'); logE(g,'sealed',{altar:A.name,prog:r2(A.prog),canalizadores:A.maxN||1}); }
  if(kind==='fenda') tarefaProg(g,'C','ritual');
  if(g.altars.filter(B=>B.state==='fenda').length>=3) g.endAt=g.rt+3;
  else if(kind==='farol'){ A.chosen=false; g.realocar.push(g.rt+40); }
}
function updateAltars(g,dt){
  for(const A of g.altars){
    if(A.state!=='active') continue;
    const ch=g.actors.filter(c=>c.team==='C'&&c.st==='alive'&&inCircle(c,A)).slice(0,2), n=ch.length;
    let rune=false;
    if(n>0){ let f=0; ch.forEach(c=>{ let k=(g.t-c.hitT<1||c.blindT>0||c.stunT>0)?.5:1; if(c.runeT>0){ k*=1.2; rune=true; } f+=k; }); f/=n;
      A.prog+=dt*f*mareFactor(g)*A.selo*(A.tardio?1.25:1)*(A.grande?.6:1)/CFG.rit[n].dur; A.lastN=n; A.maxN=Math.max(A.maxN||0,n); }
    else if(A.prog>A.cp) A.prog=Math.max(A.cp,A.prog-.02*dt);
    for(const c of CFG.cps) if(A.prog>=c&&A.cp<c){ A.cp=c; logE(g,'checkpoint',{altar:A.name,cp:c});
      if(ch.some(x=>x.cls==='ritualista')) ch.forEach(x=>{ x.shield=15; x.shieldT=8; }); }
    A.maxProg=Math.max(A.maxProg,A.prog);
    const nn=Math.max(1,A.lastN), rad=CFG.rit[nn].rad*(rune?1.5:1)*(A.tardio?1.4:1);
    if(!A.localized){ if(A.prog>=CFG.rit[nn].loc) localize(g,A,'coro'); else if(n>0&&g.actors.some(h=>h.team==='H'&&h.st==='alive'&&dist(h,A)<rad)) localize(g,A,'ouvido'); }
    if(!A.arrived&&g.actors.some(h=>h.team==='H'&&h.st==='alive'&&inCircle(h,A))){ A.arrived=true; logE(g,'hunter_arrive',{altar:A.name,prog:r2(A.prog),channelers:nn,remaining:r2((1-A.prog)*CFG.rit[nn].dur)}); }
    const sealers=g.actors.filter(h=>h.team==='H'&&h.st==='alive'&&h.sealing===A.i&&inCircle(h,A));
    if(sealers.length){
      if(!A.sealers.length){ tarefaProg(g,'H','selar'); }
      if(!A.sealers.length) logE(g,'seal_start',{altar:A.name,prog:r2(A.prog),sealers:sealers.length});
      let rate=0; sealers.forEach(s=>rate=Math.max(rate,1/(s.cls==='exorcista'?CFG.sealExo:CFG.seal)));
      if(sealers.length>=2) rate*=CFG.coSeal; if(g.buff.ess>g.t) rate*=1.25; rate=Math.min(rate,1/CFG.sealMin);
      let at=0; sealers.forEach(s=>at=Math.max(at,recentAttackers(g,s)));
      if(at>=2) rate*=CFG.fire2; else if(at===1) rate*=CFG.fire1;
      A.seal+=rate*dt; A.sealIdle=0; A.fireT=(A.fireT||0)+(at?dt:0); A.sealT=(A.sealT||0)+dt;
    } else {
      if(A.sealers.length){ const who=A.sealers.map(id=>g.actors.find(x=>x.id===id)).filter(Boolean);
        const cause=who.some(x=>x.st!=='alive')?'caído':who.some(x=>g.t-x.pushedT<.6)?'empurrão':who.some(x=>x.stunT>0)?'atordoado':'saiu';
        logE(g,'seal_stop',{altar:A.name,seal:r2(A.seal),cause}); }
      A.sealIdle+=dt; if(A.sealIdle>3) A.seal=Math.max(0,A.seal-dt/CFG.seal);
    }
    A.sealers=sealers.map(s=>s.id);
    if(A.seal>=1) resolveAltar(g,A,'farol'); else if(A.prog>=1){ A.prog=1; resolveAltar(g,A,'fenda'); }
  }
}
function updateKnowledge(g,dt){
  for(const h of g.actors){ if(h.team!=='H'||h.st!=='alive') continue;
    g.altars.forEach((A,i)=>{ const d=dist(h,A); if(d>=10) return; g.visit[i]=g.t; const k=g.know[i];
      if(A.state==='awake'){ if(k==='?'||k==='limpo'){ g.know[i]='desperto'; ev(g,{type:'evp',a:h.id,x:A.x,z:A.z}); feed(g,`EVP: vozes em ${A.name}.`,'h','H'); }
        if(!h.human&&d<6&&g.know[i]==='desperto'&&Math.random()<dt*.35) g.know[i]=A.decoy?'chamariz':'confirmado'; }
      else if(A.state==='dormant'&&k!=='limpo') g.know[i]='limpo';
    });
    if(h.human&&h.lantern){ const fx=-Math.sin(h.yaw),fz=-Math.cos(h.yaw);
      g.altars.forEach((A,i)=>{ if(A.state!=='awake'&&A.state!=='active') return; const dx=A.x-h.x,dz=A.z-h.z,d=Math.hypot(dx,dz); if(d>16||d<.5) return;
        if((dx*fx+dz*fz)/d<.9||!losClear(h,A)) return;
        if(A.decoy){ if(g.know[i]!=='chamariz'){ g.know[i]='chamariz'; feed(g,`Lanterna: nenhum resíduo em ${A.name}. É um Chamariz.`,'h','H'); logE(g,'decoy_spotted',{altar:A.name,by:h.name}); } }
        else if(g.know[i]!=='confirmado'&&g.know[i]!=='ativo'){ g.know[i]='confirmado'; feed(g,`Lanterna: resíduo de reagente em ${A.name}.`,'h','H'); } }); }
  }
}

// ============ PROJÉTEIS, COLETÁVEIS ============
function updateProjectiles(g,dt){
  for(let i=g.proj.length-1;i>=0;i--){ const p=g.proj[i]; let dead=false;
    for(let s=0;s<3&&!dead;s++){ p.x+=p.vx*dt/3; p.y+=p.vy*dt/3; p.z+=p.vz*dt/3;
      if(p.y<.05||p.y>14||Math.abs(p.x)>HALF.x||Math.abs(p.z)>HALF.z) dead=true;
      if(!dead&&algum(grade(TALL),p.x,p.z,p.x,p.z,b=>p.x>b.x1&&p.x<b.x2&&p.z>b.z1&&p.z<b.z2&&p.y<b.h)) dead=true;
      if(!dead) for(const e of g.actors){ if(e.team===p.team||e.st==='dead') continue; const top=e.st==='down'?.6:1.95;
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
function canSee(g,b,t,range){
  if(t.st==='dead'||b.blindT>0) return false; const d=dist(b,t); if(d>range) return false; if(t.veuT>0&&d>3) return false;
  if(d>7){ const fx=-Math.sin(b.yaw),fz=-Math.cos(b.yaw); if(((t.x-b.x)*fx+(t.z-b.z)*fz)/d<.35&&g.t-b.hitT>2) return false; }
  if(!losClear(b,t)) return false;
  const lit=lightAt(g,t.x,t.z)+(t.lantern?.8:0)+(g.t-t.lastShotT<1.5?.8:0);
  return !(d>13&&lit<.35);
}
function botPickTarget(g,b){
  const ens=enemiesOf(g,b).filter(e=>e.st==='alive'&&canSee(g,b,e,34)); if(!ens.length) return null;
  const pri=e=>dist(b,e)-(e.sealing>=0?30:0)-(b.team==='H'&&g.altars.some(A=>A.state==='active'&&inCircle(e,A))?15:0);
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
      const lit=lightAt(g,t.x,t.z)>.35, p=clamp(.78-d/55,.22,.78)*(t.moving?.8:1)*(lit?1:.72), hit=Math.random()<p, ex=hit?0:rand(-1.2,1.2);
      fireBotAt(g,b,t.x+ex,rand(.9,1.6),t.z+ex,!hit);
    } else { if(b.fervor<CFG.sigilCusto) return true; const lead=d/CFG.sigilVel; castSigilAt(g,b,t.x+rand(-.7,.7)+(t._vx||0)*lead*.6,1.2,t.z+rand(-.7,.7)+(t._vz||0)*lead*.6); }
  }
  return true;
}
function castSigilAt(g,b,tx,ty,tz){ let dx=tx-b.x,dy=ty-1.62,dz=tz-b.z; const l=Math.hypot(dx,dy,dz)||1; castSigil(g,b,dx/l,dy/l,dz/l); }
function botHold(g,b,key,time,dt,ctx){ if(b.hold.key!==key){ b.hold={key,t:0,max:time}; } b.hold.t+=dt; if(b.hold.t>=time){ doAction(g,b,ctx); b.hold={key:'',t:0,max:0}; } }
const inEnemySpawn=(b,p)=>{ const s=SPAWN[b.team==='H'?'C':'H']; return Math.hypot(p.x-s.x,p.z-s.z)<CFG.spawnSafe+2; };
function botHelpDowned(g,b,dt,fighting){
  const ally=g.actors.find(o=>o!==b&&o.team===b.team&&o.st==='down'&&dist(o,b)<20);
  if(ally&&!fighting){ if(dist(b,ally)<1.5){ b.moving=false; botHold(g,b,'rev'+ally.id,CFG.revive,dt,{type:'revive',b:ally.id}); return true; } setGoal(g,b,ally.x,ally.z,'rv'+ally.id); botMove(g,b,dt,1.05); return true; }
  const foe=g.actors.find(o=>o.team!==b.team&&o.st==='down'&&dist(o,b)<10);
  if(foe&&!fighting){ if(dist(b,foe)<1.5){ b.moving=false; botHold(g,b,'exe'+foe.id,CFG.execute,dt,{type:'execute',b:foe.id}); return true; } setGoal(g,b,foe.x,foe.z,'ex'+foe.id); botMove(g,b,dt,1.05); return true; }
  return false;
}
function botNoite(g,b,dt){ const ai=b.ai, role=b.team;
  // compras: Cultista sem reagente compra; Caçador com pouca munição compra
  const quer=role==='C'?(b.reag===0&&b.obolos>=ITENS.reagente.preco&&'reagente'):(b.reserve<16&&b.obolos>=ITENS.municao.preco&&'municao');
  if(quer){ const n=NPCS.filter(n=>(!n.time||n.time===role)&&n.vende.includes(quer)).sort((x,y)=>dist(b,x)-dist(b,y))[0];
    if(n&&dist(b,n)<55){ if(dist(b,n)<2.4){ b.moving=false; if(b.cid==null) comprarBot(g,b,n,quer); return true; } setGoal(g,b,n.x,n.z,'npc'+n.id); botMove(g,b,dt,1); return true; } }
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
  if(b.reloadT>0){ b.reloadT-=dt; if(b.reloadT<=0){ const n=Math.min(8-b.ammo,b.reserve); b.ammo+=n; b.reserve-=n; if(b.reserve<=0) b.reserve=24; } }
  if(ai.target&&b.abilCd<=0){ const d=dist(b,ai.target);
    if(b.feitico==='flash'&&d<12) useAbility(g,b);
    if(b.feitico==='purificacao'&&enemiesOf(g,b).some(e=>e.st==='alive'&&dist(b,e)<5.5&&losClear(b,e))) useAbility(g,b); }
  if(b.feitico==='sal'&&b.abilCd<=0){ const A=g.altars.find(A=>(A.state==='active'||g.know[A.i]==='confirmado')&&dist(b,A)<7); if(A) useAbility(g,b); }
  if(botHelpDowned(g,b,dt,fighting)) return;
  if(!fighting&&botNoite(g,b,dt)) return;
  const act=g.altars.filter(A=>A.state==='active'&&A.localized).sort((x,y)=>dist(b,x)-dist(b,y))[0];
  if(act){
    if(inCircle(b,act)){ const close=ai.target&&dist(b,ai.target)<11&&b.hp>45;
      if(!close){ b.sealing=act.i; b.moving=false; return; }
      ai.strafeT-=dt; if(ai.strafeT<=0){ ai.strafe*=-1; ai.strafeT=rand(.6,1.4); }
      const s=circleSpot(act,b); setGoal(g,b,s.x+ai.strafe*.8,s.z,'fight'+act.i+ai.strafe); botMove(g,b,dt,.7); return; }
    const s=circleSpot(act,b); setGoal(g,b,s.x,s.z,'go'+act.i); botMove(g,b,dt,fighting?.8:1.05); return;
  }
  const known=g.altars.filter(A=>A.state==='awake'&&['confirmado','desperto','chamariz'].includes(g.know[A.i])).sort((x,y)=>dist(b,x)-dist(b,y))[0];
  if(known&&!(g.know[known.i]==='chamariz'&&dist(b,known)>12)){
    if(dist(b,known)<3){ b.moving=false; if(!fighting) botHold(g,b,'purge'+known.i,CFG.purge,dt,{type:'purge',A:known.i}); return; }
    const s=circleSpot(known,b); setGoal(g,b,s.x,s.z,'pg'+known.i); botMove(g,b,dt,1); return; }
  if(ai.alert&&g.t-ai.alert.t<6&&!fighting&&!inEnemySpawn(b,ai.alert)){ setGoal(g,b,ai.alert.x,ai.alert.z,'al'+Math.round(ai.alert.t)); botMove(g,b,dt,1); if(dist(b,ai.alert)<2) ai.alert=null; return; }
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
  if(b.feitico==='veu'&&b.abilCd<=0&&ai.target&&dist(b,ai.target)<22&&b.hp<b.maxHp*.7) useAbility(g,b);
  if(b.feitico==='empurrao'&&b.abilCd<=0){ const t=enemiesOf(g,b).find(e=>e.st==='alive'&&dist(b,e)<3.4&&(e.sealing>=0||ai.target===e));
    if(t){ b.yaw=Math.atan2(-(t.x-b.x),-(t.z-b.z)); useAbility(g,b); } }
  if(botHelpDowned(g,b,dt,fighting)) return;
  if(!fighting&&botNoite(g,b,dt)) return;
  const act=g.altars.filter(A=>A.state==='active'&&A.chosen).sort((x,y)=>dist(b,x)-dist(b,y))[0];
  if(act){ if(b.feitico==='runa'&&b.abilCd<=0&&inCircle(b,act)) useAbility(g,b);
    const s=circleSpot(act,b); if(inCircle(b,act)&&dist(b,s)<1.2){ b.moving=false; return; } setGoal(g,b,s.x,s.z,'ch'+act.i); botMove(g,b,dt,1.05); return; }
  const rem=g.altars.filter(A=>A.chosen&&(A.state==='dormant'||A.state==='awake'));
  if(!rem.length){ // sem altar para trabalhar (esperando substituto): vai se abastecer no Claustro
    const R=g.reagents.filter(R=>R.has).sort((x,y)=>dist(b,x)-dist(b,y))[0];
    if(R&&b.reag<CFG.carry){ if(dist(b,R)<1.8){ b.moving=false; botHold(g,b,'col'+R.i,CFG.collect,dt,{type:'collect',R:R.i}); return; } setGoal(g,b,R.x+.9,R.z,'r'+R.x+','+R.z); botMove(g,b,dt,1); return; }
    b.moving=false; ai.esperando=g.t; return; }
  rem.sort((x,y)=>((y.state==='awake'&&!y.decoy)?1:0)-((x.state==='awake'&&!x.decoy)?1:0)||dist(b,x)-dist(b,y));
  const T=rem[0]; if(ai.esperaAltar!==T.i){ ai.esperaAltar=T.i; ai.wait=0; }
  if(!ai.decoyUsed&&g.decoys>0&&g.rt>25){ const D=g.altars.filter(A=>!A.chosen&&A.state==='dormant').sort((x,y)=>dist(b,x)-dist(b,y))[0];
    if(D){ if(dist(b,D)<3.3){ b.moving=false; botHold(g,b,'dec'+D.i,CFG.decoy,dt,{type:'decoy',A:D.i}); if(D.state==='awake') ai.decoyUsed=true; return; }
      if(dist(b,D)<70){ const sd=circleSpot(D,b); setGoal(g,b,sd.x,sd.z,'dg'+D.i); botMove(g,b,dt,1);
        ai.decoyT=(ai.decoyT||0)+dt; if(ai.decoyT>15) ai.decoyUsed=true; // não conseguiu encostar: desiste e volta ao ritual
        return; } } }
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
      else if(T.state==='awake'&&!T.decoy&&(g.momento<1||(g.momento<2&&fendas(g)>=2))){ b.moving=false; ai.esperando=g.t; }
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
    if(a.st==='down'){ a.downT-=dt; a.sealing=-1; if(a.downT<=0) kill(g,a,null); if(!a.human){ a.moving=false; } else humanActions(g,a,dt); continue; }
    if(a.human) humanActions(g,a,dt);
    else if(a.stunT<=0){ if(a.team==='H') aiHunter(g,a,dt); else aiCult(g,a,dt);
      // rede de segurança: bot parado 15 s sem estar ocupado tem a decisão reiniciada (não corrige a causa; limita o estrago)
      const ai=a.ai, vigiando=ai.goal&&ai.goalKey.startsWith('pt')&&dist(a,ai.goal)<2, lutando=ai.target&&g.t-a.lastShotT<2.5;
      const ocupado=a.sealing>=0||a.hold.t>0||vigiando||lutando||g.altars.some(A=>A.state==='active'&&inCircle(a,A))||(ai.esperando&&g.t-ai.esperando<.5);
      if(!ai.vigia||ocupado||Math.hypot(a.x-ai.vigia.x,a.z-ai.vigia.z)>3) ai.vigia={x:a.x,z:a.z,t:g.rt};
      else if(g.rt-ai.vigia.t>15){ const objetivoAntes=ai.goalKey, alvoAntes=ai.target?ai.target.name:null, segurando=a.hold.key; ai.goal=null; ai.goalKey=''; ai.path=[]; ai.ignora={}; ai.wait=0; ai.decoyUsed=true; ai.progKey=''; ai.vigia={x:a.x,z:a.z,t:g.rt};
        const q=pontoDesvio(a); ai.goal=q; ai.goalKey='desvio'; ai.path=[q]; ai.repath=g.t+3;
        logE(g,'bot_reiniciado',{who:a.name,team:a.team,cls:a.cls,objetivo:objetivoAntes,combate:alvoAntes,reag:a.reag,x:r2(a.x),z:r2(a.z)}); } }
    if(a.moving&&a.st==='alive'){ a.stepT=(a.stepT||0)-dt; if(a.stepT<=0){ const corre=a.human&&a.inp.sp&&a.inp.mz<0; a.stepT=corre?.3:.42; ev(g,{type:'sfx',k:corre?'passoForte':'step',x:r2(a.x),z:r2(a.z),a:a.id}); } }
    if(g.t-a.hitT>6&&a.hp<a.maxHp&&a.st==='alive') a.hp=Math.min(a.maxHp,a.hp+dt*8);
    if(a.team==='C'&&a.st==='alive'&&g.t-a.lastShotT>CFG.fervorEspera) a.fervor=Math.min(100,a.fervor+dt*CFG.fervorRegen);
    updateSanity(g,a,dt);
    if(!a.human){ for(const o of g.actors) if(o.team!==a.team&&o.st==='alive'&&g.t-o.panicT<.5&&dist(a,o)<18) a.ai.alert={x:o.x,z:o.z,t:g.t}; }
  }
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
    tentativasSelo:sStart.length,cancelados:sStop.filter(e=>e.cause!=='saiu').length,
    fracaoSobFogo:sealT>0?r2(fireT/sealT):null,primeiroContato:L.find(e=>e.ev==='first_contact')?.t??null,
    quedas:c('down').length,mortes:c('death').length,reanimacoes:c('revive').length,purgas:c('purge').length,chamarizes:c('decoy').length,transferencias:c('transfer').length};
}
function winner(g){
  if(!((g.opts&&g.opts.noites||CFG.noites)>1)){ const S=g.scores.B; if(!S) return null; return S.done>=3?{win:'B',why:'os Cultistas completaram três rituais antes do amanhecer'}:{win:'A',why:'os Caçadores resistiram até o amanhecer'}; }
  const A=g.scores.A,B=g.scores.B; if(!A||!B) return null;
  if(A.done!==B.done) return {win:A.done>B.done?'A':'B',why:'mais rituais completados'};
  if(A.done>0&&A.lastT!==B.lastT) return {win:A.lastT<B.lastT?'A':'B',why:'desempate: completou o último ritual mais cedo'};
  if(Math.abs(A.maxP-B.maxP)>0.005) return {win:A.maxP>B.maxP?'A':'B',why:'desempate: maior progresso em um único ritual'};
  return {win:null,why:'empate exato; na versão completa, isso levaria ao Ritual Final'};
}

// ============ SNAPSHOT (servidor → cliente) ============
const ACT_FIELDS=['feitico','amuleto','id','cid','human','name','team','cls','maxHp','st','reag','sealing','ammo','reserve','fervor','lantern','flares','moving','deaths','revUsed'];
const ACT_NUM=['obolos','veuT','slowT','revelT','curaT','x','z','yaw','pitch','hp','downT','respawnT','reloadT','sanity','carga','sensorCd','abilCd','lastShotT','blindT','stunT','runeT','shield','invuln','speed'];
function snapshot(g,role){
  const hide=role==='H';
  return {t:r2(g.t),rt:r2(g.rt),phase:g.phase,phaseT:r2(g.phaseT),round:g.round,timers:g.timers,matchId:g.matchId,
    slots:g.slots.map(s=>({team:s.team,name:s.name,cid:s.cid})),
    altars:g.altars.map(A=>{ const masked=hide&&A.state==='awake'&&(g.know[A.i]==='?'||g.know[A.i]==='limpo');
      return {i:A.i,name:A.name,x:A.x,z:A.z,state:masked?'dormant':A.state,chosen:hide?false:A.chosen,decoy:hide?false:A.decoy,prog:r2(A.prog),cp:A.cp,seal:r2(A.seal),localized:A.localized,lastN:A.lastN,maxProg:r2(A.maxProg),doneT:A.doneT}; }),
    reagents:g.reagents.map(R=>({i:R.i,x:R.x,z:R.z,has:R.has})),
    actors:g.actors.map(a=>{ const o={}; ACT_FIELDS.forEach(k=>o[k]=a[k]); ACT_NUM.forEach(k=>o[k]=r2(a[k])); o.hold={key:a.hold.key,t:r2(a.hold.t),max:a.hold.max}; return o; }),
    proj:g.proj.map(p=>({id:p.id,x:r2(p.x),y:r2(p.y),z:r2(p.z),vx:r2(p.vx),vy:r2(p.vy),vz:r2(p.vz)})),
    pickups:g.pickups.map(p=>({id:p.id,kind:p.kind,x:p.x,z:p.z})),
    flares:g.flares.map(f=>({x:f.x,z:f.z,t:r2(f.t)})),
    know:hide?g.know.slice():g.know.map(()=>'?'),
    momento:g.momento,tarefas:g.tarefas,pontos:(g.pontos||[]).map(p=>({i:p.i,tipo:p.tipo,x:p.x,z:p.z,ativo:p.ativo,acesa:p.acesa})),sal:(g.sal||[]).map(q=>({id:q.id,x:q.x,z:q.z})),feit:Object.assign({},g.feit),
    decoys:g.decoys,transferUsed:g.transferUsed,picks:hide?[]:g.picks.slice(),ready:Object.assign({},g.ready),cls:Object.assign({},g.cls),
    scores:g.scores,stats:g.stats,buff:{ess:g.buff.ess>g.t?r2(g.buff.ess-g.t):0,selo:g.buff.selo>g.t?r2(g.buff.selo-g.t):0},lastResolveT:g.lastResolveT};
}

return {PONTOS_DEF,CATEDRAL,naCatedral,ARVORES,ARBUSTOS,TRILHAS,CLAREIRAS,PORTAIS,LAPIDES,NAV,livre,raioParede,FEITICOS,FEIT_BY_TEAM,NPCS,ITENS,TAREFAS,momentoDe,WP,CFG,CLASSES,CLASS_BY_TEAM,HALF,CLAUSTRO,ZONES,zoneAt,LUGARES,ALTARS,SPAWN,REAG,CANDLES,WALLS,WALLDEF,PILLARS,PEWS,BOX,TALL,segBox,segClear,losClear,occlusion,resolve,findPath,
  createGame,step,act,dropHuman,reclaimHuman,moveHuman,contexts,lightAt,snapshot,roleOf,winner,mareFactor,inCircle,dist,clamp,lerp,BOT_NAMES};
});
