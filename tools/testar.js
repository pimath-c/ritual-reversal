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

// 3. O snapshot não entrega a posição de inimigos que ninguém da equipe está vendo
console.log('3. Visibilidade no snapshot');
{ const g=Sim.createGame({timers:true,slots:[0,1,2,3].map(j=>({team:j<2?'A':'B',cid:null,name:'b'+j})),matchId:'vis'});
  for(let i=0;i<3000&&g.phase!=='play';i++) Sim.step(g,1/30);
  const h=g.actors.find(a=>a.team==='H'), outroH=g.actors.find(a=>a.team==='H'&&a!==h), c=g.actors.find(a=>a.team==='C'), c2=g.actors.find(a=>a.team==='C'&&a!==c);
  h.human=true; outroH.st='dead'; c2.x=140; c2.z=90; // o segundo Cultista fica longe, fora de qualquer vista
  const ver=(x,z,cx,cz)=>{ h.x=x; h.z=z; c.x=cx; c.z=cz; c.revelT=0; g.t+=1; const s=Sim.snapshot(g,'H').actors.find(a=>a.id===c.id); return !s.hidden&&Math.abs(s.x-cx)<.01; };
  if(!ver(-20,0,-10,0)) falha('inimigo a 10 m em campo aberto no Claustro deveria aparecer');
  if(!ver(-40,0,20,0)) falha('inimigo a 60 m em campo aberto no Claustro deveria aparecer');
  if(ver(-40,0,-40,-45)) falha('inimigo atrás da parede da Capela Oeste não deveria aparecer');
  if(ver(0,0,0,100)) falha('inimigo a 100 m não deveria aparecer');
  const s2=Sim.snapshot(g,'H').actors.find(a=>a.id===c2.id); if(!s2.hidden||s2.x!==0) falha('inimigo longe vazou a posição');
  if(Sim.snapshot(g,'C').actors.find(a=>a.id===c.id).hidden) falha('aliado nunca deveria ser escondido');
  c.revelT=3; h.x=0; h.z=0; c.x=0; c.z=100; if(Sim.snapshot(g,'H').actors.find(a=>a.id===c.id).hidden) falha('Sal deveria revelar a posição');
  console.log('   inimigos só aparecem com linha de visão até 75 m (ou revelados pelo Sal)'); }
// 4. Regras do círculo (v5.5) e bots que não podem empacar
console.log('4. Regras do círculo e bots');
{ const nova=()=>{ const g=Sim.createGame({timers:true,slots:[0,1,2,3].map(j=>({team:j<2?'A':'B',cid:null,name:'b'+j})),matchId:'reg'}); for(let i=0;i<3000&&g.phase!=='play';i++) Sim.step(g,1/30);
    g.rt=300; for(const a of g.actors){ a.st='dead'; a.respawnT=999; } return g; };
  // eco: aos 25% a caça ouve o setor do ritual (Capela Leste fica a leste, com o Carvalho Oco)
  { const g=nova(), A=g.altars[2], c=g.actors.find(a=>a.team==='C'); Object.assign(A,{state:'active',chosen:true,prog:.2,cp:0,localized:false,eco:false});
    Object.assign(c,{st:'alive',x:A.x,z:A.z,lastShotT:-99,hp:c.maxHp,human:true}); for(let i=0;i<30*15&&!g.eco;i++) Sim.step(g,1/30); // jogador parado no círculo
    if(!g.eco||g.eco.setor!=='leste') falha('o eco não anunciou o setor leste: '+JSON.stringify(g.eco)); else if(A.localized) falha('o eco não deveria revelar o altar');
  // quem ataca não canaliza: com o único Cultista atacando, o ritual não avança
    const p0=A.prog; for(let i=0;i<30;i++){ c.ataqueT=g.t; c.x=A.x; c.z=A.z; Sim.step(g,1/30); }
    if(A.prog>p0+1e-9) falha(`ritual avançou com o Cultista atacando (${p0} → ${A.prog})`);
    for(let i=0;i<60;i++){ c.x=A.x; c.z=A.z; Sim.step(g,1/30); } if(!(A.prog>p0)) falha('ritual não voltou a avançar depois de parar de atacar'); }
  // levar dano recomeça a execução
  { const g=nova(), c=g.actors.find(a=>a.team==='C'), h=g.actors.find(a=>a.team==='H'); Object.assign(c,{st:'alive',hp:c.maxHp,invuln:0}); c.hold={key:'exe'+h.id,t:3,max:4.5};
    Sim.damage(g,c,5,h); if(c.hold.t!==0||c.hold.key!=='') falha('dano não interrompeu a execução'); if(!g.log.some(e=>e.ev==='execucao_interrompida')) falha('execução interrompida fora do registro'); }
  // o Caçador que decidiu ir ao Ermitão chega lá (antes empacava a 55 m em linha reta, onde o caminho se afasta)
  { const g=nova(), h=g.actors.find(a=>a.team==='H'); Object.assign(h,{st:'alive',hp:h.maxHp,x:-88.1,z:-30.6,reserve:8,obolos:5}); h.ai.goal=null; h.ai.goalKey=''; // a 52 m em linha reta; o caminho passa a 67 m antes de chegar
    for(let i=0;i<30*60&&!g.log.some(e=>e.ev==='compra');i++){ h.reserve=Math.min(h.reserve,8); Sim.step(g,1/30); }
    if(!g.log.some(e=>e.ev==='compra')) falha(`Caçador não chegou ao Ermitão (parou em ${h.x.toFixed(1)}, ${h.z.toFixed(1)})`); }
  // um bot que largou o E não fica travado sem atirar (a contagem pendurada impedia o tiro com o inimigo a 4 m)
  { const g=nova(), h=g.actors.find(a=>a.team==='H'), c=g.actors.find(a=>a.team==='C'); Object.assign(h,{st:'alive',hp:h.maxHp,x:-66.6,z:-42.1,ammo:8,reserve:48}); h.ai.goal=null; h.ai.goalKey='';
    Object.assign(c,{st:'alive',hp:c.maxHp,x:-67,z:-38,human:true,invuln:0}); h.hold={key:'purge0',t:2,max:6}; const A=g.altars[0]; Object.assign(A,{state:'awake',chosen:true}); g.know[0]='confirmado';
    let atirou=false; for(let i=0;i<30*3&&!atirou;i++){ Sim.step(g,1/30); atirou=g.events.some(e=>e.type==='tracer'&&e.a===h.id); g.events.length=0; }
    if(!atirou) falha('Caçador com a contagem de purga pendurada não atirou no Cultista a 4 m'); }
  console.log('   eco do setor, ataque pausa a canalização, dano recomeça a execução, bot chega ao mercador, bot que larga o E atira'); }
// 5. Informação por equipe: o que o servidor manda a cada cliente (esconder no HUD não basta)
console.log('5. Informação por equipe no snapshot e nos eventos');
{ const g=Sim.createGame({timers:true,slots:[0,1,2,3].map(j=>({team:j<2?'A':'B',cid:null,name:'b'+j})),matchId:'info'}); for(let i=0;i<3000&&g.phase!=='play';i++) Sim.step(g,1/30);
  const h=g.actors.find(a=>a.team==='H'), h2=g.actors.find(a=>a.team==='H'&&a!==h), c=g.actors.find(a=>a.team==='C'), c2=g.actors.find(a=>a.team==='C'&&a!==c);
  h.human=true; h2.st='dead'; c2.x=140; c2.z=90; h.x=-140; h.z=0; c.x=60; c.z=-46;
  const A=g.altars[2]; Object.assign(A,{state:'active',chosen:true,prog:.4,localized:false}); g.know[2]='?';
  const sH=Sim.snapshot(g,'H'), aH=sH.altars[2];
  if(aH.state==='active'||aH.prog>0||aH.chosen) falha('a caça vê o ritual não localizado: '+JSON.stringify(aH));
  g.know[2]='desperto'; if(Sim.snapshot(g,'H').altars[2].state!=='awake') falha('altar desperto conhecido deveria aparecer desperto enquanto o ritual não é localizado');
  if(Sim.snapshot(g,'C').altars[2].state!=='active') falha('o culto deveria ver o próprio ritual');
  A.localized=true; if(Sim.snapshot(g,'H').altars[2].state!=='active') falha('ritual localizado deveria aparecer para a caça');
  c.hold={key:'cons3',t:1,max:3}; c.sealing=-1; c.reag=2; const cs=Sim.snapshot(g,'H').actors.find(a=>a.id===c.id);
  if(!cs.hidden||cs.hold.key||cs.reag) falha('inimigo escondido revela o que está fazendo: '+JSON.stringify({h:cs.hold,r:cs.reag}));
  if(Object.keys(Sim.snapshot(g,'H').tarefas).join()!=='H'||Object.keys(Sim.snapshot(g,'C').tarefas).join()!=='C') falha('tarefas da outra equipe no snapshot');
  if(Sim.snapshot(g,'H').decoys!==0) falha('a caça não deveria saber quantos Chamarizes o culto tem');
  const para=(e,me,role)=>Sim.eventoPara(g,e,me,role);
  if(para({type:'sfx',k:'step',x:c.x,z:c.z,a:c.id},h,'H')) falha('passos de um Cultista a 200 m chegaram à caça');
  const perto={type:'sfx',k:'step',x:h.x+8,z:h.z,a:c.id}; if(!para(perto,h,'H')) falha('passos a 8 m deveriam chegar');
  if(!para({type:'sfx',k:'step',x:c.x,z:c.z,a:c.id},c,'C')) falha('os próprios sons deveriam chegar à equipe');
  if(para({type:'selando',altar:2,x:A.x,z:A.z},h,'H')) falha('aviso de selo chegou à caça');
  if(!para({type:'selando',altar:2,x:A.x,z:A.z},c,'C')) falha('aviso de selo não chegou ao culto');
  if(para({type:'eco',setor:'leste',altares:[2,5]},c,'C')||!para({type:'eco',setor:'leste',altares:[2,5]},h,'H')) falha('eco do coro com destinatário errado');
  if(para({type:'hit',a:c.id,alvo:h.id},h,'H')) falha('confirmação de acerto chegou a quem levou o tiro');
  const hu=para({type:'hurt',a:c2.id,s:h.id,sx:h.x,sz:h.z},c,'C'); if(!hu||hu.sx!=null) falha('o aliado de quem levou dano não deveria saber de onde veio o tiro');
  if(para({type:'tracer',a:c.id,x0:c.x,z0:c.z,x1:c.x+5,z1:c.z},h,'H')) falha('tiro de inimigo escondido a 200 m chegou');
  if(para({type:'ping',team:'C',a:c.id,x:0,z:0},h,'H')||para({type:'feed',msg:'x',team:'C'},h,'H')) falha('ping ou aviso do culto chegou à caça');
  if(!para({type:'doom'},h,'H')) falha('eventos da noite toda deveriam chegar a todos');
  console.log('   ritual não localizado, inimigo escondido, tarefas, Chamarizes, sons, selo, eco, acertos e pings por equipe'); }
// 6. Regras da v5.6: Empurrão também pausa a canalização; quem pisa no círculo localiza o ritual
console.log('6. Empurrão pausa a canalização, pisar no círculo localiza');
{ const g=Sim.createGame({timers:true,slots:[0,1,2,3].map(j=>({team:j<2?'A':'B',cid:null,name:'b'+j})),matchId:'v56'}); for(let i=0;i<3000&&g.phase!=='play';i++) Sim.step(g,1/30);
  g.rt=300; for(const a of g.actors){ a.st='dead'; a.respawnT=999; }
  const A=g.altars[1], c=g.actors.find(a=>a.team==='C'), h=g.actors.find(a=>a.team==='H'); Object.assign(A,{state:'active',chosen:true,prog:.1,cp:0,localized:false,eco:false});
  Object.assign(c,{st:'alive',hp:c.maxHp,x:A.x,z:A.z,human:true,feitico:'empurrao',abilCd:0}); const p0=A.prog;
  Object.assign(h,{st:'alive',hp:h.maxHp,x:A.x+2.5,z:A.z,human:true,invuln:0}); c.yaw=Math.atan2(-(h.x-c.x),-(h.z-c.z));
  if(!A.localized){ Sim.step(g,1/30); if(A.localized&&g.log.some(e=>e.ev==='localized'&&e.how==='circulo')) {} }
  const L=g.log.find(e=>e.ev==='localized'); if(!L) falha('Caçador dentro do círculo não localizou o ritual');
  const p1=A.prog; c.inp.c={q:0,f:0,g:0,r:0,ab:1}; for(let i=0;i<12;i++){ c.x=A.x; c.z=A.z; Sim.step(g,1/30); }
  if(!g.log.some(e=>e.ev==='ability'&&e.feitico==='empurrao')) falha('o Empurrão não saiu no teste'); else if(A.prog>p1+1e-9) falha(`ritual avançou logo depois do Empurrão (${p1} → ${A.prog})`);
  // ação conferida na hora: o bot que segura "coletar" num frasco que o parceiro já pegou não ganha reagente de graça
  { const g2=Sim.createGame({timers:true,slots:[0,1,2,3].map(j=>({team:j<2?'A':'B',cid:null,name:'b'+j})),matchId:'neg'}); for(let i=0;i<3000&&g2.phase!=='play';i++) Sim.step(g2,1/30);
    const c2=g2.actors.find(a=>a.team==='C'), R=g2.reagents[0]; R.has=false; c2.reag=0; c2.hold={key:'col0',t:3.4,max:3.5};
    Sim._botHold(g2,c2,'col0',3.5,.2,{type:'collect',R:0}); if(c2.reag!==0) falha('bot coletou reagente que já tinha sido pego');
    if(!g2.log.some(e=>e.ev==='acao_negada')) falha('ação negada fora do registro'); }
  console.log('   ok'); }
// 7. O que a caça sabe de cada altar: passar perto não descarta (antes todo altar visitado aparecia "descartado")
console.log('7. Conhecimento dos altares: visto, descartado, purgado');
{ const g=Sim.createGame({timers:true,slots:[0,1,2,3].map(j=>({team:j<2?'A':'B',cid:null,name:'b'+j})),matchId:'know'}); for(let i=0;i<3000&&g.phase!=='play';i++) Sim.step(g,1/30);
  for(const a of g.actors){ a.st='dead'; a.respawnT=999; } const h=g.actors.find(a=>a.team==='H'); Object.assign(h,{st:'alive',hp:h.maxHp,human:true});
  const esc=g.altars.find(A=>A.chosen), livre=g.altars.find(A=>!A.chosen); esc.state='dormant'; g.know=g.know.map(()=>'?');
  h.x=esc.x+3; h.z=esc.z; Sim.step(g,1/30); if(g.know[esc.i]!=='visto') falha(`altar escolhido visitado virou "${g.know[esc.i]}" (deveria ser "visto": nada prova que não é do culto)`);
  esc.state='awake'; Sim.step(g,1/30); if(g.know[esc.i]!=='desperto') falha('altar visto que desperta deveria virar "desperto"');
  h.x=-140; h.z=0; Sim.step(g,1/30); esc.decoy=false; Sim._doAction(g,h,{type:'purge',A:esc.i}); if(g.know[esc.i]!=='purgado') falha('purgar um altar verdadeiro deveria marcar "purgado"');
  h.x=esc.x+3; h.z=esc.z; Sim.step(g,1/30); if(g.know[esc.i]!=='purgado') falha('passar perto não deveria apagar "purgado"');
  Object.assign(livre,{state:'awake',decoy:true}); Sim._doAction(g,h,{type:'purge',A:livre.i}); if(g.know[livre.i]!=='descartado') falha('Chamariz queimado deveria marcar "descartado"'); }
console.log('   ok');
// 8. v5.7: interrupções, desfecho com nomes, lances da noite e registro da disputa (só reconhecimento, nenhuma regra muda)
console.log('8. Interrupções, lances e disputa no círculo');
{ const g=Sim.createGame({timers:true,slots:[0,1,2,3].map(j=>({team:j<2?'A':'B',cid:null,name:'b'+j})),matchId:'v57'}); for(let i=0;i<3000&&g.phase!=='play';i++) Sim.step(g,1/30);
  for(const a of g.actors){ a.st='dead'; a.respawnT=999; } const [h,h2]=g.actors.filter(a=>a.team==='H'), [c,c2]=g.actors.filter(a=>a.team==='C');
  const viva=a=>Object.assign(a,{st:'alive',hp:a.maxHp,human:true,revUsed:false,invuln:0,inp:Object.assign({},a.inp,{use:false,fire:false})});
  const A=g.altars.find(x=>x.chosen); Object.assign(A,{state:'active',localized:true,prog:.5,cp:.25,seal:0,arrived:true}); viva(h); viva(c); h.x=A.x+1; h.z=A.z; c.x=A.x-1.5; c.z=A.z;
  Sim.step(g,1/30); g.events.length=0; h.sealing=A.i; Sim.damage(g,h,999,c);
  const ei=g.events.find(e=>e.type==='interrupcao'); if(!ei||ei.tipo!=='selo'||ei.a!==c.id||ei.alvo!==h.id||ei.altar!==A.i||ei.team) falha('derrubar quem sela deveria gerar a interrupção do selo para todos');
  if(!g.log.some(e=>e.ev==='interrupcao'&&e.tipo==='selo'&&e.by===c.name)) falha('a interrupção do selo deveria ir para o registro');
  // canalização interrompida num ritual que a caça não localizou: só o culto fica sabendo
  viva(h); h.x=A.x+40; h.z=A.z; A.localized=false; c.x=A.x+.5; c.z=A.z; g.events.length=0; Sim.damage(g,c,999,h);
  const ec=g.events.find(e=>e.type==='interrupcao'); if(!ec||ec.tipo!=='canal'||ec.team!=='C') falha('derrubar quem canaliza um ritual não localizado deveria avisar só o culto');
  else if(Sim.eventoPara(g,ec,h,'H')||!Sim.eventoPara(g,ec,c2,'C')) falha('o filtro de eventos deveria mandar a interrupção da canalização só ao culto');
  // selo no último instante: o Farol anuncia quem selou e vira lance
  c.st='dead'; A.localized=true; Object.assign(A,{prog:.9,seal:.999}); viva(h); h.x=A.x+1; h.z=A.z; h.inp.use=true; g.events.length=0;
  for(let i=0;i<5&&A.state==='active';i++) Sim.step(g,1/30); h.inp.use=false;
  const es=g.events.find(e=>e.type==='sealed'); if(A.state!=='farol'||!es||!(es.por||[]).includes(h.name)) falha('o Farol deveria anunciar quem selou');
  const el=g.events.find(e=>e.type==='lance'&&e.tipo==='selo_limite'); if(!el||!el.quem.includes(h.name)||el.v<.85) falha('selar com o ritual acima de 85% deveria virar o lance "selo no último instante"');
  const dp=g.log.filter(e=>e.ev==='disputa'&&e.altar===A.name); if(!dp.length||!dp.some(e=>e.who===h.name&&e.team==='H'&&e.selando>0)) falha('o fim do ritual deveria registrar a disputa de cada um (quem selou, por quanto tempo)');
  if(!dp.some(e=>e.who===c.name&&e.derrubou>=1)) falha('a disputa deveria contar quem derrubou alguém perto do círculo');
  // Chamariz: o culto só fica sabendo quanto ele prendeu a caça quando ele é queimado
  const D=g.altars.find(x=>!x.chosen&&x.state==='dormant'); Sim._doAction(g,c,{type:'decoy',A:D.i}); viva(c); Sim._doAction(g,c,{type:'decoy',A:D.i});
  if(!D.decoy||D.state!=='awake') falha('plantar Chamariz no teste'); h.x=D.x+5; h.z=D.z; c.x=-140; c.z=0;
  for(let i=0;i<30*25;i++){ Sim.step(g,1/30); g.events.length=0; h.x=D.x+5; h.z=D.z; }
  g.events.length=0; Sim._doAction(g,h,{type:'purge',A:D.i});
  const pg=g.log.filter(e=>e.ev==='purge').pop(); if(!pg||!pg.decoy||!(pg.isca>=24&&pg.isca<=26)) falha(`a purga do Chamariz deveria registrar ~25 s de caça presa (${pg&&pg.isca})`);
  const fc=g.events.find(e=>e.type==='feed'&&e.team==='C'&&/prendeu a caça por/.test(e.msg)); if(!fc) falha('o culto deveria saber quanto o Chamariz prendeu a caça');
  else if(Sim.eventoPara(g,fc,h,'H')) falha('o tempo do Chamariz não pode chegar à caça');
  const lc=g.events.find(e=>e.type==='lance'&&e.tipo==='chamariz'); if(!lc||lc.team!=='C'||lc.quem[0]!==c.name) falha('Chamariz que prende a caça por 20 s deveria virar lance do culto');
  // resgate: quem impede a execução e reanima o aliado
  viva(c); viva(c2); viva(h); c2.x=h.x=c.x=0; c2.z=0; h.z=1; c.z=-1; Sim.damage(g,c2,999,h); if(c2.st!=='down') falha('derrubar no teste do resgate');
  h.hold={key:'exe'+c2.id,t:1,max:4.5}; Sim.damage(g,h,5,c); if(c2.salvoPor!==c.name) falha('interromper a execução deveria marcar quem salvou');
  g.events.length=0; Sim._doAction(g,c,{type:'revive',b:c2.id});
  const lr=g.events.find(e=>e.type==='lance'&&e.tipo==='resgate'); if(!lr||lr.team!=='C'||lr.quem.join()!==c.name) falha('reanimar depois de impedir a execução deveria virar o lance "resgate"');
  const M=Sim.relatorio(g.log,g.round); if(!M.lances||M.lances.length<3) falha('o relatório deveria listar os lances da noite');
  if(!(M.engano.segundosPresosEmChamariz>=24)) falha('o relatório deveria somar o tempo preso em Chamarizes'); }
console.log('   ok');
console.log(`9. ${N} partidas só de bots com invariantes`);
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
