#!/usr/bin/env node
// Testes de aceite do mapa e das regras. Uso: node tools/testar.js [partidas]   (padrão: 6)
// 1. Grafo de navegação: sem ilhas; todo ponto importante cai na parte principal.
// 2. Caminhada real: um andador com o raio de um jogador segue findPath (com colisão) de cada spawn até
//    cada altar, reagente, mercador, ponto de tarefa e porta. Chegar = ficar a menos de 1,6 m do alvo.
//    (Conferir só se a rota "termina no destino" não prova nada: o planejador sempre acrescenta o destino.)
// 3. Partidas só de bots, conferindo a cada passo: ninguém dentro de parede, fora do mapa, com NaN ou vida inválida.
'use strict';
const Sim=require('../shared/sim.js');
const N=+process.argv[2]||6;
let falhas=0; const falha=m=>{ falhas++; console.log('  FALHA:',m); };
const t0=Date.now();

console.log('1. Grafo de navegação');
console.log(`   ${Sim.WP.length} nós, ${Sim.NAV.ilhas.length} ilhas, ${Sim.ARVORES.length} árvores, ${Sim.WALLS.length} peças de colisão`);
// Na mata fechada, espinheiros e troncos isolam bolsões (ninguém chega lá sem atravessar o mato).
// O que importa é a caminhada real até cada ponto de interesse, conferida na etapa 2.
{ const N=Sim.NAV, mata=Sim.WP.filter((w,i)=>N.comp[i]===N.principal&&!Sim.naCatedral(w.x,w.z,0)).length, bols=N.ilhas.reduce((s,q)=>s+q.n,0);
  console.log(`   mata percorrível: ${mata} nós (${(mata*4/1000).toFixed(1)} mil m²); ${N.ilhas.length} bolsões fechados somando ${bols} nós`); }

const alvos=[];
Sim.ALTARS.forEach(A=>alvos.push({nome:'altar '+A.name,x:A.x,z:A.z+(A.z>0?-2.2:2.2),perto:3}));
Sim.REAG.forEach(([x,z],i)=>alvos.push({nome:'reagente '+i,x:x+.9,z,perto:1.6}));
Sim.NPCS.forEach(n=>alvos.push({nome:'mercador '+n.id,x:n.x,z:n.z,perto:2.2}));
const g0=Sim.createGame({timers:true,slots:[0,1,2,3].map(j=>({team:j<2?'A':'B',cid:null,name:'b'+j}))});
g0.pontos.forEach(p=>alvos.push({nome:'ponto '+p.tipo+' '+p.i,x:p.x,z:p.z,perto:1.6}));
for(const k of ['pista','erva','sentinela','tumulo']) (Sim.PONTOS_DEF?Sim.PONTOS_DEF[k]:[]).forEach((p,i)=>alvos.push({nome:k+' '+i,x:p.x,z:p.z,perto:1.6}));
Sim.PORTAIS.forEach(p=>{ const c=(p.a+p.b)/2; alvos.push({nome:p.nome,x:p.eixo==='x'?c:p.fixo,z:p.eixo==='x'?p.fixo:c,perto:1.6}); });

// mercadores e pontos de tarefa não podem se sobrepor: o contexto do mercador tem prioridade e esconderia a tarefa
{ const pts=[]; for(const k of ['pista','erva','sentinela','tumulo']) Sim.PONTOS_DEF[k].forEach((p,i)=>pts.push({n:k+' '+i,...p}));
  for(const n of Sim.NPCS) for(const p of pts) if(Math.hypot(n.x-p.x,n.z-p.z)<3.2) falha(`${p.n} a menos de 3,2 m de ${n.id}`);
  for(let i=0;i<pts.length;i++) for(let j=i+1;j<pts.length;j++) if(Math.hypot(pts[i].x-pts[j].x,pts[i].z-pts[j].z)<2.5) falha(`${pts[i].n} e ${pts[j].n} sobrepostos`); }
console.log('2. Caminhada real de cada spawn até '+alvos.length+' alvos');
function caminhar(de,alvo){
  const p={x:de.x,z:de.z}; let path=Sim.findPath(p,alvo), t=0, parado=0, lx=p.x, lz=p.z; const dt=1/30, v=4.6;
  while(t<240){ t+=dt;
    if(Math.hypot(p.x-alvo.x,p.z-alvo.z)<alvo.perto) return t;
    let q=path[0]; if(!q){ path=Sim.findPath(p,alvo); q=path[0]; if(!q) return -1; }
    const dx=q.x-p.x,dz=q.z-p.z,d=Math.hypot(dx,dz);
    if(d<.5){ path.shift(); continue; }
    p.x+=dx/d*v*dt; p.z+=dz/d*v*dt; Sim.resolve(p,.4);
    if(t%1<dt){ if(Math.hypot(p.x-lx,p.z-lz)<.5){ if(++parado>3) return -1; path=Sim.findPath(p,alvo); } else parado=0; lx=p.x; lz=p.z; } }
  return -1; }
let maxT=0, somaT=0, nCam=0; const tc=Date.now();
for(const lado of ['H','C']) for(const a of alvos){ const t=caminhar(Sim.SPAWN[lado],a); if(t<0) falha(`${lado} não chega em ${a.nome} (${a.x},${a.z})`); else { maxT=Math.max(maxT,t); somaT+=t; nCam++; } }
console.log(`   ${nCam} caminhadas ok; média ${(somaT/Math.max(1,nCam)).toFixed(1)} s, mais longa ${maxT.toFixed(1)} s; planejamento ${(Date.now()-tc)} ms no total`);

console.log(`3. ${N} partidas só de bots com invariantes`);
const dentroDeParede=a=>!Sim.livre(a,.3);
let passos=0, tempoStep=0, piorStep=0, reinicios=0; const res={B:0,A:0}; const fendas=[], quedas={H:0,C:0};
for(let m=0;m<N;m++){
  const g=Sim.createGame({timers:true,slots:[0,1,2,3].map(j=>({team:j<2?'A':'B',cid:null,name:'b'+j})),matchId:'teste'+m});
  let n=0, problemas=0;
  while(g.phase!=='final'&&n<30*60*25){ const s=process.hrtime.bigint(); Sim.step(g,1/30); const dtms=Number(process.hrtime.bigint()-s)/1e6; tempoStep+=dtms; piorStep=Math.max(piorStep,dtms); g.events.length=0; n++; passos++;
    if(g.phase==='play') for(const a of g.actors){
      if(!isFinite(a.x)||!isFinite(a.z)||!isFinite(a.hp)){ if(problemas++<3) falha(`partida ${m}: NaN em ${a.name}`); }
      else if(Math.abs(a.x)>Sim.HALF.x||Math.abs(a.z)>Sim.HALF.z){ if(problemas++<3) falha(`partida ${m}: ${a.name} fora do mapa (${a.x.toFixed(1)},${a.z.toFixed(1)})`); }
      else if(a.st!=='dead'&&dentroDeParede(a)){ if(problemas++<3) falha(`partida ${m}: ${a.name} dentro de parede (${a.x.toFixed(1)},${a.z.toFixed(1)})`); }
      if(a.hp<0||a.hp>a.maxHp+.01){ if(problemas++<3) falha(`partida ${m}: vida inválida ${a.name} ${a.hp}`); } } }
  if(g.phase!=='final') falha(`partida ${m} não terminou`);
  const w=Sim.winner(g); if(w&&w.win) res[w.win]++;
  const L=g.log; reinicios+=L.filter(e=>e.ev==='bot_reiniciado').length; fendas.push(L.filter(e=>e.ev==='ritual_complete').length);
  L.filter(e=>e.ev==='down').forEach(e=>quedas[e.team]++);
  process.stdout.write(`\r   ${m+1}/${N}`);
}
console.log(`\n   ${passos} passos; ${(tempoStep/passos).toFixed(3)} ms por passo em média, pior ${piorStep.toFixed(1)} ms (orçamento do servidor: 33 ms)`);
console.log(`   vitórias: Cultistas ${res.B}, Caçadores ${res.A}; Fendas por partida: ${fendas.join(' ')}; quedas: Caçadores ${quedas.H}, Cultistas ${quedas.C}; bots reiniciados: ${reinicios}`);
console.log(falhas?`\n${falhas} falha(s).`:`\nTudo passou em ${((Date.now()-t0)/1000).toFixed(0)} s.`);
process.exit(falhas?1:0);
