#!/usr/bin/env node
// Exporta a planta do jogo (a mesma que shared/sim.js gera com semente fixa) para docs/mapa.json,
// para montar o mapa em outra engine. Unidades em metros; plano do chão = (x, z); altura = y (h nas peças).
// Uso: node tools/exportar-mapa.js [saida]
'use strict';
const fs=require('fs'), path=require('path');
const Sim=require('../shared/sim.js');
const out=process.argv[2]||path.join(__dirname,'..','docs','mapa.json');
const UNITY=path.join(__dirname,'..','unity','Assets','RitualReversal','Resources','mapa.json'); // o importador e o mundo do Unity leem esta cópia
const r=v=>Math.round(v*100)/100;
const mapa={
  sobre:'Ritual Reversal: planta gerada por shared/sim.js. Metros; chão no plano x-z, y para cima. '+
    'Unity: X=x, Y=y, Z=-z (rotação em y: -rot). Unreal: X=x*100, Y=z*100, Z=y*100. Godot: x, y, z direto. '+
    'Sem listas dentro de listas: pontos são objetos {x,z}, para o JsonUtility do Unity ler direto.',
  limites:{meiaLargura:Sim.HALF, planoOriginal4v4:Sim.HALF0, catedral:Sim.CATEDRAL, claustro:Sim.CLAUSTRO},
  spawns:Sim.SPAWN,
  altares:Sim.ALTARS.map((A,i)=>({i,nome:A.name,x:r(A.x),z:r(A.z)})),
  reagentes:Sim.REAG.map(([x,z])=>({x,z})),
  mercadores:Sim.NPCS.map(n=>({id:n.id,nome:n.nome,x:r(n.x),z:r(n.z),time:n.time,vende:n.vende})),
  pontosDeTarefa:Sim.PONTOS_DEF,
  portais:Sim.PORTAIS,
  lugares:Sim.LUGARES.map(L=>({nome:L.name,x:r(L.x),z:r(L.z)})),
  luzes:Sim.CANDLES.map(([x,z,tipo])=>({x:r(x),z:r(z),tipo:tipo||'vela'})),
  trilhas:Sim.TRILHAS.map(T=>({largura:T.w,estreita:!!T.estreita,pontos:T.pts.map(([x,z])=>({x:r(x),z:r(z)}))})),
  clareiras:Sim.CLAREIRAS.map(c=>({x:r(c.x),z:r(c.z),raio:c.r})),
  arcos:Sim.ARCOS.map(([x1,z1,x2,z2])=>({x1,z1,x2,z2})),
  pilares:Sim.PILLARS.map(([x,z])=>({x,z,raio:.85,h:14})),
  bancos:Sim.PEWS.map(([x1,x2,z1,z2])=>({x1,x2,z1,z2,h:.95})),
  // caixas de colisão: tudo que bloqueia passagem. tall=true também bloqueia visão e tiros.
  pecas:Sim.WALLS.map(w=>{ const o={tipo:w.kind,x1:r(w.x1),x2:r(w.x2),z1:r(w.z1),z2:r(w.z2),h:r(w.h),tall:!!w.tall}; if(w.rot!=null) o.rot=w.rot; if(w.pose!=null) o.pose=w.pose; return o; }),
  arvores:Sim.ARVORES.map(q=>({x:q.x,z:q.z,raio:q.r,tipo:['anciã','alta','morta'][q.tipo],escala:q.s,rot:q.rot,inc:q.inc})),
  vegetacaoRasteira:Sim.ARBUSTOS.map(q=>({x:q.x,z:q.z,escala:q.s,rot:q.rot,tipo:q.tipo?'moita':'samambaia'})),
};
// Dados da simulação para o porte em C# (Scripts/Simulacao): precisão total, na ordem exata de shared/sim.js,
// porque a ordem das caixas decide como a colisão empurra os personagens. O C# refaz a navegação a partir daqui.
const simulacao={
  sobre:'Gerado por tools/exportar-mapa.js a partir de shared/sim.js. Lido por Scripts/Simulacao/Mapa.cs. Não edite à mão.',
  half:Sim.HALF, catedral:Sim.CATEDRAL, claustro:Sim.CLAUSTRO,
  caixas:Sim.BOX.map(b=>[b.x1,b.x2,b.z1,b.z2,b.h,b.tall?1:0]),
  altares:Sim.ALTARS.map(A=>({nome:A.name,x:A.x,z:A.z})),
  spawns:Sim.SPAWN, reagentes:Sim.REAG, portas:Sim.PORTAS.map(p=>[p.x,p.z]), portais:Sim.PORTAIS,
  pista:Sim.PISTA_PTS, erva:Sim.ERVA_PTS, sentinela:Sim.SENTINELA_PTS, pontos:Sim.PONTOS_DEF,
  mercadores:Sim.NPCS.map(n=>({id:n.id,nome:n.nome,x:n.x,z:n.z,time:n.time,vende:n.vende})),
  luzes:Sim.CANDLES.map(c=>[c[0],c[1]]), clareiras:Sim.CLAREIRAS.map(c=>[c.x,c.z,c.r]),
  trilhas:Sim.SEG_TRILHA.map(s=>[s.ax,s.az,s.bx,s.bz,s.w,s.estreita?1:0]),
  conferencia:{nos:Sim.WP.length,arestas:Sim.WP.reduce((s,w)=>s+w.n.length,0),principal:Sim.NAV.principal},
};
const SIM_UNITY=path.join(__dirname,'..','unity','Assets','RitualReversal','Resources','simulacao.json');
fs.mkdirSync(path.dirname(out),{recursive:true}); fs.writeFileSync(out,JSON.stringify(mapa));
if(!process.argv[2]){ fs.mkdirSync(path.dirname(UNITY),{recursive:true}); fs.writeFileSync(UNITY,JSON.stringify(mapa));
  fs.mkdirSync(path.dirname(SIM_UNITY),{recursive:true}); fs.writeFileSync(SIM_UNITY,JSON.stringify(simulacao)); }
const cont=Object.fromEntries(Object.entries(mapa).filter(([k,v])=>Array.isArray(v)).map(([k,v])=>[k,v.length]));
console.log('exportado',out,(fs.statSync(out).size/1024).toFixed(0)+' KB',JSON.stringify(cont));
