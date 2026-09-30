#!/usr/bin/env node
// Compara o porte em C# (unity/Assets/RitualReversal/Scripts/Simulacao) com shared/sim.js:
// os dois jogam as mesmas partidas só de bots, com o mesmo sorteio (Mulberry32), e o estado é comparado passo a passo,
// bit a bit (posições, vida, progresso dos rituais, decisões dos bots, eventos e registro).
// Com [humano] = H ou C, um dos lugares é um jogador roteirizado (anda por rotas, atira, segura E e T, usa Q/F/G/R,
// habilidade, compra, escolhe altares e classe), dirigido pelo mesmo sorteio nos dois lados: cobre o código do jogador.
// Precisa do Mono (mcs e mono). Uso: node tools/comparar-cs.js [partidas] [semente] [passos por partida] [bots|H|C]
'use strict';
const fs=require('fs'), path=require('path'), os=require('os'), {execFileSync}=require('child_process');
const RAIZ=path.join(__dirname,'..'), SIMCS=path.join(RAIZ,'unity','Assets','RitualReversal','Scripts','Simulacao');
const DADOS=path.join(RAIZ,'unity','Assets','RitualReversal','Resources','simulacao.json');
const partidas=+process.argv[2]||1, semente=+process.argv[3]||12345, passos=+process.argv[4]||40000, modo=process.argv[5]||'bots';
const tmp=fs.mkdtempSync(path.join(os.tmpdir(),'rr-cs-'));

function mulberry32(a){ return function(){ a|=0; a=a+0x6D2B79F5|0; let t=Math.imul(a^a>>>15,1|a); t=t+Math.imul(t^t>>>7,61|t)^t; return ((t^t>>>14)>>>0)/4294967296; }; }
const dv=new DataView(new ArrayBuffer(8));
const H=v=>{ if(v===0) v=0; dv.setFloat64(0,v); return dv.getBigUint64(0).toString(16); };
const deH=h=>{ dv.setBigUint64(0,BigInt('0x'+h)); return dv.getFloat64(0); };
const V=v=>{ if(v===null||v===undefined) return 'null'; if(typeof v==='boolean') return v?'true':'false'; if(typeof v==='string') return '"'+v+'"';
  if(typeof v==='number') return H(v); if(Array.isArray(v)) return '['+v.map(V).join(',')+']'; return O(v); };
const O=o=>'{'+Object.keys(o).filter(k=>o[k]!==undefined).sort().map(k=>k+':'+V(o[k])).join(',')+'}';
function linha(Sim,g,nLog){
  const p=['t:'+H(g.t),'ph:'+g.phase,'rt:'+H(g.rt),'m:'+g.momento];
  for(const a of g.actors) p.push([a.id,a.st,H(a.x),H(a.z),H(a.yaw),H(a.hp),a.reag,a.sealing,a.hold.key,H(a.hold.t),a.ai.goalKey,H(a.fervor),a.ammo,a.reserve,a.obolos,H(a.sanity),a.ai.path.length,a.cls,a.feitico].join(','));
  for(const A of g.altars) p.push([A.state,A.chosen?1:0,A.decoy?1:0,H(A.prog),H(A.seal),H(A.cp),A.localized?1:0].join(','));
  p.push(g.know.join(',')); p.push(g.reagents.map(R=>R.has?'1':'0').join(''));
  p.push(g.proj.map(q=>q.id+'@'+H(q.x)+','+H(q.y)+','+H(q.z)).join(';'));
  p.push(g.pickups.map(q=>q.id+q.kind+'@'+H(q.x)+','+H(q.z)).join(';'));
  p.push('d'+g.decoys+(g.transferUsed?'T':'F'));
  for(const e of g.events){ const d=Object.assign({},e); delete d.type; p.push('E'+e.type+O(d)); }
  for(let i=nLog;i<g.log.length;i++) p.push('L'+O(g.log[i]));
  return p.join('|');
}

// Jogador roteirizado: a mesma lógica existe em tools/cs/CompararSim.cs (Roteiro). Usa um sorteio próprio,
// separado do Math.random da simulação, e chama as funções na mesma ordem.
function slotsDe(){ return [0,1,2,3].map(j=>({team:j<2?'A':'B',cid:(modo==='H'&&j===0)||(modo==='C'&&j===2)?'eu':null,name:'b'+j})); }
function roteiro(Sim,g,D,r){
  const dt=1/30;
  if(g.phase==='pick'){ if(r()<.02) Sim.act(g,'eu',{type:'pick',i:Math.floor(r()*6)}); if(r()<.01) Sim.act(g,'eu',{type:'pickRandom'}); if(r()<.005) Sim.act(g,'eu',{type:'pickConfirm'}); Sim.step(g,dt); return; }
  if(g.phase==='intro'){ const role=Sim.roleOf(g,g.slots.find(s=>s.cid==='eu').team);
    if(r()<.02){ const L=Sim.CLASS_BY_TEAM[role]; Sim.act(g,'eu',{type:'cls',cls:L[Math.floor(r()*L.length)]}); }
    if(r()<.02){ const L=Sim.FEIT_BY_TEAM[role]; Sim.act(g,'eu',{type:'feit',f:L[Math.floor(r()*L.length)]}); }
    if(r()<.004) Sim.act(g,'eu',{type:'ready'}); Sim.step(g,dt); return; }
  if(g.phase!=='play'){ Sim.step(g,dt); return; }
  const m=g.actors.find(a=>a.cid==='eu');
  if(!D.alvo||g.t>D.trocaT){ const L=[...g.altars,...g.reagents,...Sim.NPCS,...g.pontos]; const k=Math.floor(r()*L.length); D.alvo={x:L[k].x,z:L[k].z}; D.trocaT=g.t+15+r()*15; D.path=Sim.findPath(m,D.alvo); }
  if(D.path.length&&Math.hypot(D.path[0].x-m.x,D.path[0].z-m.z)<.8) D.path.shift();
  if(!D.path.length&&Math.hypot(D.alvo.x-m.x,D.alvo.z-m.z)>2&&r()<.1) D.path=Sim.findPath(m,D.alvo);
  const q=D.path[0]; let yaw=q?Math.atan2(-(q.x-m.x),-(q.z-m.z)):m.yaw, fire=false;
  let inimigo=null, bd=18; for(const e of g.actors){ if(e.team===m.team||e.st!=='alive') continue; const d=Math.hypot(e.x-m.x,e.z-m.z); if(d<bd&&Sim.losClear(m,e)){ bd=d; inimigo=e; } }
  if(inimigo){ yaw=Math.atan2(-(inimigo.x-m.x),-(inimigo.z-m.z)); fire=r()<.7; }
  const mz=q?-1:0, mx=r()<.1?(r()<.5?-1:1):0, sp=r()<.3, use=r()>=.1, use2=r()<.5, pitch=(r()-.5)*.2;
  for(const k of ['q','f','g','r','ab']) if(r()<1/90) D.c[k]++;
  if(g.t>D.compraT){ D.compraT=g.t+10; let n=null, nd=1e9; for(const x of Sim.NPCS){ const d=Math.hypot(x.x-m.x,x.z-m.z); if(d<nd){ nd=d; n=x; } }
    Sim.act(g,'eu',{type:'comprar',npc:n.id,item:n.vende[Math.floor(r()*n.vende.length)]}); }
  Sim.moveHuman(g,m,{mx,mz,sp,yaw,pitch,fire,use,use2,c:Object.assign({},D.c),viewT:g.t-.05},dt); Sim.step(g,dt);
}

// 1. JS
Math.random=mulberry32(semente);
const Sim=require('../shared/sim.js');
const saidaJS=path.join(tmp,'js.txt'), fd=fs.openSync(saidaJS,'w'), cobertura={};
let t0=Date.now();
for(let m=0;m<partidas;m++){
  const g=Sim.createGame({timers:true,slots:slotsDe(),matchId:'cmp'+m});
  let nLog=0; const D={alvo:null,trocaT:0,path:[],c:{q:0,f:0,g:0,r:0,ab:0},compraT:0}, r=mulberry32((semente^0x5bd1e995)+m);
  for(let n=0;n<passos&&g.phase!=='final';n++){ if(modo==='bots') Sim.step(g,1/30); else roteiro(Sim,g,D,r); fs.writeSync(fd,linha(Sim,g,nLog)+'\n'); nLog=g.log.length; g.events.length=0; }
  g.log.forEach(e=>cobertura[e.ev]=(cobertura[e.ev]||0)+1);
  const w=Sim.winner(g); fs.writeSync(fd,'FIM '+(w?w.win+' '+w.why:'-')+'\n');
}
fs.closeSync(fd); const tJS=Date.now()-t0;

// 2. C#
const exe=path.join(tmp,'CompararSim.exe');
const fontes=fs.readdirSync(SIMCS).filter(f=>f.endsWith('.cs')).map(f=>path.join(SIMCS,f));
execFileSync('mcs',['-langversion:experimental','-optimize+','-nowarn:414','-out:'+exe,...fontes,path.join(__dirname,'cs','CompararSim.cs')],{stdio:'inherit'});
const saidaCS=path.join(tmp,'cs.txt'); t0=Date.now();
execFileSync('mono',[exe,DADOS,String(semente>>>0),String(passos),saidaCS,String(partidas),modo],{stdio:'inherit'});
const tCS=Date.now()-t0;

// 3. comparação
const A=fs.readFileSync(saidaJS,'utf8').split('\n'), B=fs.readFileSync(saidaCS,'utf8').split('\n');
const nomes=['id','st','x','z','yaw','hp','reag','sealing','hold','holdT','goalKey','fervor','ammo','reserve','obolos','sanity','path','cls','feitico'];
const legivel=s=>s.replace(/(^|[,:@;{\[])([0-9a-f]{8,16})(?=$|[,;|}\]])/g,(m,a,h)=>a+(+deH(h).toPrecision(10)));
let igual=0, falhou=false;
for(let i=0;i<Math.max(A.length,B.length);i++){
  if(A[i]===B[i]){ igual++; continue; }
  falhou=true; const a=(A[i]||'').split('|'), b=(B[i]||'').split('|');
  console.log(`\nDIFERENÇA na linha ${i+1} (${(i/30).toFixed(2)} s de jogo contando todas as fases)`);
  for(let k=0;k<Math.max(a.length,b.length);k++) if(a[k]!==b[k]){
    console.log('  JS: '+legivel(a[k]||'(nada)')); console.log('  C#: '+legivel(b[k]||'(nada)'));
    if(a[k]&&b[k]&&a[k].split(',').length===nomes.length){ const x=a[k].split(','),y=b[k].split(','); console.log('  campos: '+nomes.filter((n,j)=>x[j]!==y[j]).join(', ')); }
  }
  break;
}
const fins=A.filter(l=>l.startsWith('FIM'));
console.log('Regras exercitadas (registro do JS): '+Object.entries(cobertura).sort().map(([k,v])=>k+' '+v).join(', '));
console.log(`\n${igual} linhas iguais de ${A.length}. JS ${tJS} ms, C# ${tCS} ms. Resultados: ${fins.join(' / ')}`);
fs.rmSync(tmp,{recursive:true,force:true});
process.exit(falhou?1:0);
