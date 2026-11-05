#!/usr/bin/env node
// Análise de fluxo do mapa, direto dos dados da simulação: distâncias de caminhada (A* do jogo) entre os nascimentos,
// os altares, os reagentes e os mercadores; exposição de cada altar (linhas de visão abertas); e uma planta vista de cima
// em SVG com as rotas mais curtas de cada lado.
// Uso: node tools/fluxo.js [pasta]   (padrão: dist/fluxo/; grava planta.svg e fluxo.json e imprime as tabelas)
'use strict';
const fs=require('fs'), path=require('path');
const Sim=require('../shared/sim.js');
const out=process.argv[2]||path.join(__dirname,'..','dist','fluxo'); fs.mkdirSync(out,{recursive:true});
const {ALTARS,SPAWN,REAG,NPCS,HALF,CFG,findPath,raioParede,losClear,WP,NAV}=Sim;

// comprimento do caminho que o bot faria (o mesmo findPath do jogo)
function caminho(a,b){ const p=findPath(a,b); let L=0,c=a; const pts=[[a.x,a.z]]; for(const q of p){ L+=Math.hypot(q.x-c.x,q.z-c.z); c=q; pts.push([q.x,q.z]); } return {L,pts}; }
const seg=s=>(s/CFG.speed).toFixed(0)+' s';
const nome=(A,i)=>A.name||('R'+i);
const P={H:SPAWN.H,C:SPAWN.C};
const linhas=[]; const log=(...a)=>{ const s=a.join(' '); linhas.push(s); console.log(s); };

// ---------- nascimentos até os altares ----------
log('== Nascimento até cada altar (caminho do A*, a pé a '+CFG.speed+' m/s) ==');
const rotas=[]; const tab={altares:[]};
ALTARS.forEach((A,i)=>{ const alvo={x:A.x,z:A.z+2}; const h=caminho(P.H,alvo), c=caminho(P.C,alvo);
  rotas.push({team:'H',pts:h.pts},{team:'C',pts:c.pts});
  const dif=h.L-c.L;
  tab.altares.push({i,nome:A.name,zona:Sim.zoneAt(A.x,A.z),caca:Math.round(h.L),culto:Math.round(c.L)});
  log(`${String(i).padStart(2)} ${A.name.padEnd(16)} ${Sim.zoneAt(A.x,A.z).padEnd(10)} caça ${String(Math.round(h.L)).padStart(4)} m (${seg(h.L)})  culto ${String(Math.round(c.L)).padStart(4)} m (${seg(c.L)})  diferença ${dif>0?'+':''}${Math.round(dif)} m`); });

// ---------- entre altares ----------
log('\n== Entre altares (m) ==');
const M=ALTARS.map((A,i)=>ALTARS.map((B,j)=>i===j?0:Math.round(caminho({x:A.x,z:A.z+2},{x:B.x,z:B.z+2}).L)));
log('    '+ALTARS.map((A,i)=>String(i).padStart(5)).join(''));
M.forEach((r,i)=>log(String(i).padStart(2)+'  '+r.map(v=>String(v).padStart(5)).join('')));
tab.entreAltares=M;
const viz=M.map((r,i)=>Math.min(...r.filter((v,j)=>j!==i)));
log('altar mais próximo de cada um: '+viz.join(', ')+' m; maior trajeto entre dois altares: '+Math.max(...M.flat())+' m');

// ---------- reagentes ----------
log('\n== Reagentes ==');
const zonasR={}; REAG.forEach(([x,z])=>{ const zn=Sim.zoneAt(x,z); zonasR[zn]=(zonasR[zn]||0)+1; });
log('por zona: '+Object.entries(zonasR).map(([k,v])=>k+' '+v).join(', '));
const dR=REAG.map(([x,z])=>({x,z,c:Math.round(caminho(P.C,{x:x+1.5,z}).L),h:Math.round(caminho(P.H,{x:x+1.5,z}).L)}));
dR.sort((a,b)=>a.c-b.c);
log('do nascimento do culto: '+dR.map(r=>r.c).join(', ')+' m; da caça: '+dR.map(r=>r.h).join(', ')+' m');
const rPerto=ALTARS.map(A=>Math.round(Math.min(...REAG.map(([x,z])=>caminho({x:A.x,z:A.z+2},{x:x+1.5,z}).L))));
log('reagente mais perto de cada altar: '+rPerto.join(', ')+' m');
tab.reagentes={porZona:zonasR,doCulto:dR.map(r=>r.c),daCaca:dR.map(r=>r.h),pertoDoAltar:rPerto};

// ---------- mercadores ----------
log('\n== Mercadores ==');
tab.mercadores=NPCS.map(n=>{ const h=Math.round(caminho(P.H,n).L), c=Math.round(caminho(P.C,n).L); log(`${n.nome.padEnd(22)} ${String(n.time||'todos').padEnd(6)} caça ${h} m, culto ${c} m`); return {nome:n.nome,time:n.time,caca:h,culto:c}; });

// ---------- exposição dos altares ----------
// 72 raios a 1,6 m de altura a partir do centro do círculo: alcance médio e máximo até a primeira peça alta,
// e a fração dos nós de navegação a até 40 m que enxergam o altar (quanto do entorno pode atirar em quem está nele).
log('\n== Exposição de cada altar ==');
tab.exposicao=ALTARS.map((A,i)=>{ const rs=[]; for(let k=0;k<72;k++){ const a=k/72*Math.PI*2; rs.push(raioParede(A.x,A.z,Math.cos(a),Math.sin(a),90,0)); }
  const med=rs.reduce((s,v)=>s+v,0)/rs.length, max=Math.max(...rs);
  let n=0,v=0; for(const w of WP){ if(NAV.comp[WP.indexOf(w)]!==NAV.principal) continue; const d=Math.hypot(w.x-A.x,w.z-A.z); if(d>40||d<5) continue; n++; if(losClear(w,A)) v++; }
  log(`${String(i).padStart(2)} ${A.name.padEnd(16)} visão média ${med.toFixed(0)} m, máxima ${max.toFixed(0)} m, ${Math.round(v/Math.max(1,n)*100)}% do entorno (5–40 m) enxerga o altar`);
  return {nome:A.name,media:+med.toFixed(1),maxima:+max.toFixed(1),entornoQueVe:+(v/Math.max(1,n)).toFixed(2)}; });

// ---------- invariantes do documento de design (seção 11) ----------
log('\n== Invariantes do documento de design ==');
{ const mH=tab.altares.reduce((s,a)=>s+a.caca,0)/tab.altares.length, mC=tab.altares.reduce((s,a)=>s+a.culto,0)/tab.altares.length, dif=Math.abs(mH-mC)/Math.min(mH,mC);
  log(`nascimentos equidistantes: média até os altares ${mH.toFixed(0)} m (caça) e ${mC.toFixed(0)} m (culto), diferença ${(dif*100).toFixed(1)}% ${dif<=.1?'ok':'FORA (máx. 10%)'}`);
  // posto de escuta: quantos altares um ponto qualquer ouve no raio do ritual de dois canalizadores (linha reta)
  const raio=CFG.rit[2].rad; let maxOuve=0;
  for(let x=-HALF.x;x<=HALF.x;x+=2) for(let z=-HALF.z;z<=HALF.z;z+=2){ const n=ALTARS.filter(A=>Math.hypot(A.x-x,A.z-z)<=raio).length; if(n>maxOuve) maxOuve=n; }
  log(`posto de escuta: um ponto ouve no máximo ${maxOuve} altar(es) no raio de ${raio} m ${maxOuve<=2?'ok':'FORA (máx. 2)'}`);
  // aproximação iluminada e escura: nós a 6–16 m do altar que chegam até ele, pela luz do lugar (sem sinalizadores)
  const g0={flares:[],altars:[]}; tab.aproximacao=[];
  ALTARS.forEach(A=>{ let claro=0,escuro=0; for(let k=0;k<WP.length;k++){ if(NAV.comp[k]!==NAV.principal) continue; const w=WP[k], d=Math.hypot(w.x-A.x,w.z-A.z); if(d<6||d>16) continue; if(Sim.lightAt(g0,w.x,w.z)>=.35) claro++; else escuro++; }
    const t=claro+escuro, ok=claro/t>=.1&&escuro/t>=.1; tab.aproximacao.push({nome:A.name,claro:+(claro/t).toFixed(2),escuro:+(escuro/t).toFixed(2)});
    log(`${A.name.padEnd(16)} entorno ${Math.round(claro/t*100)}% iluminado, ${Math.round(escuro/t*100)}% escuro ${ok?'ok':'FORA (precisa dos dois)'}`); }); }

// ---------- área andável por zona ----------
const area={}; WP.forEach((w,k)=>{ if(NAV.comp[k]!==NAV.principal||k>=NAV.nx*NAV.nz) return; const zn=Sim.zoneAt(w.x,w.z); area[zn]=(area[zn]||0)+NAV.passo*NAV.passo; });
log('\n== Área andável (m², pela grade de navegação) ==');
log(Object.entries(area).map(([k,v])=>k+' '+v).join(', '));
tab.area=area;
fs.writeFileSync(path.join(out,'fluxo.json'),JSON.stringify(tab,null,1));
fs.writeFileSync(path.join(out,'fluxo.txt'),linhas.join('\n')+'\n');

// ---------- planta ----------
const K=4, W=HALF.x*2*K, H=HALF.z*2*K, X=x=>((x+HALF.x)*K).toFixed(1), Z=z=>((z+HALF.z)*K).toFixed(1);
const cor={abismo:'#000000',parapeito:'#a09888',penhasco:'#6a6458',espinheiro:'#2c3a24',arvore_f:'#22301c',limite:'#555',escombro:'#7a7468',lapide:'#999',tumba:'#a9a49a',sebe:'#3d5a32',rocha:'#5f5f55',tronco:'#5a4632',raiz:'#5a4632',estatua:'#b0aca0',menir:'#8a8478',madeira:'#7a5a3a',tenda:'#6a5a3a',caixote:'#6a5040',ponte:'#8a6a48',abismo:'#05060a',agua:'#1a2a3a'};
const s=[]; s.push(`<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ${W} ${H}" width="${W}" height="${H}" font-family="sans-serif">`);
s.push(`<rect width="${W}" height="${H}" fill="#141a12"/>`);
(Sim.ABISMO||[]).forEach(w=>s.push(`<rect x="${X(w.x1)}" y="${Z(w.z1)}" width="${((w.x2-w.x1)*K).toFixed(1)}" height="${((w.z2-w.z1)*K).toFixed(1)}" fill="#020204"/>`));
Sim.CLAREIRAS.forEach(c=>s.push(`<circle cx="${X(c.x)}" cy="${Z(c.z)}" r="${c.r*K}" fill="#26321f"/>`));
Sim.TRILHAS.forEach(T=>s.push(`<polyline points="${T.pts.map(([x,z])=>X(x)+','+Z(z)).join(' ')}" fill="none" stroke="${T.estreita?'#3a3424':'#6a5a3c'}" stroke-width="${T.w*K}" stroke-linecap="round" stroke-linejoin="round"/>`));
// zonas da última para a primeira (a primeira da lista é a que vale onde se sobrepõem)
const corZona={Planalto:'#2a2a2c',Desfiladeiro:'#08090d',Catedral:'#4a453d',Claustro:'#3e4a36'};
if(Sim.ZONAS) Sim.ZONAS.slice().reverse().forEach(q=>{ const c=q.name==='Planalto'&&q.luar<.3?'#1e1f22':corZona[q.name]||'#333'; s.push(`<rect x="${X(q.x1)}" y="${Z(q.z1)}" width="${((q.x2-q.x1)*K).toFixed(1)}" height="${((q.z2-q.z1)*K).toFixed(1)}" fill="${c}"/>`); });
else { const C=Sim.CATEDRAL; s.push(`<rect x="${X(C.x1)}" y="${Z(C.z1)}" width="${(C.x2-C.x1)*K}" height="${(C.z2-C.z1)*K}" fill="#38342f"/>`); }
for(const w of Sim.WALLS){ if(w.kind==='abismo') continue; const c=cor[w.kind]||(w.tall?'#c8c0b0':'#8a8478'); s.push(`<rect x="${X(w.x1)}" y="${Z(w.z1)}" width="${((w.x2-w.x1)*K).toFixed(1)}" height="${((w.z2-w.z1)*K).toFixed(1)}" fill="${c}"/>`); }
Sim.PILLARS.forEach(([x,z])=>s.push(`<circle cx="${X(x)}" cy="${Z(z)}" r="${.85*K}" fill="#d8d0c0"/>`));
rotas.forEach(R=>s.push(`<polyline points="${R.pts.map(([x,z])=>X(x)+','+Z(z)).join(' ')}" fill="none" stroke="${R.team==='H'?'#e8c860':'#e04050'}" stroke-width="2.5" stroke-opacity=".75" stroke-dasharray="${R.team==='H'?'':'8 5'}"/>`));
REAG.forEach(([x,z])=>s.push(`<circle cx="${X(x)}" cy="${Z(z)}" r="6" fill="#ff3355" stroke="#fff" stroke-width="1.5"/>`));
NPCS.forEach(n=>s.push(`<rect x="${+X(n.x)-6}" y="${+Z(n.z)-6}" width="12" height="12" fill="#c080ff" stroke="#fff"/>`));
ALTARS.forEach((A,i)=>{ s.push(`<circle cx="${X(A.x)}" cy="${Z(A.z)}" r="${CFG.circleR*K}" fill="none" stroke="#ffb040" stroke-width="3"/>`);
  s.push(`<text x="${X(A.x)}" y="${+Z(A.z)-CFG.circleR*K-6}" fill="#ffd080" font-size="22" text-anchor="middle" font-weight="bold">${i} ${A.name}</text>`); });
for(const t of ['H','C']){ const p=SPAWN[t]; s.push(`<circle cx="${X(p.x)}" cy="${Z(p.z)}" r="12" fill="${t==='H'?'#e8c860':'#e04050'}" stroke="#fff" stroke-width="3"/>`);
  s.push(`<text x="${X(p.x)}" y="${+Z(p.z)+40}" fill="#fff" font-size="22" text-anchor="middle">${t==='H'?'Caçadores':'Cultistas'}</text>`); }
Sim.LUGARES.forEach(L=>s.push(`<text x="${X(L.x)}" y="${Z(L.z)}" fill="#cfd8c8" font-size="17" text-anchor="middle" opacity=".85">${L.name}</text>`));
s.push(`<text x="16" y="${H-16}" fill="#ccc" font-size="18">Planta da simulação (${HALF.x*2}×${HALF.z*2} m). Linha cheia amarela: rotas da caça; tracejada vermelha: do culto. Pontos vermelhos: reagentes; quadrados lilás: mercadores.</text>`);
s.push('</svg>');
fs.writeFileSync(path.join(out,'planta.svg'),s.join('\n'));
console.log('\ngravado em',out+'/planta.svg, fluxo.json, fluxo.txt');
