#!/usr/bin/env node
// Exporta a planta do jogo (a mesma que shared/sim.js gera com semente fixa) para docs/mapa.json,
// para montar o mapa em outra engine. Unidades em metros; plano do chão = (x, z); altura = y (h nas peças).
// Uso: node tools/exportar-mapa.js [saida]
'use strict';
const fs=require('fs'), path=require('path');
const Sim=require('../shared/sim.js');
const out=process.argv[2]||path.join(__dirname,'..','docs','mapa.json');
const r=v=>Math.round(v*100)/100;
const mapa={
  sobre:'Ritual Reversal: planta gerada por shared/sim.js. Metros; chão no plano x-z, y para cima. '+
    'Unreal: X=x*100, Y=z*100, Z=y*100 (a troca de eixos já corrige a mão). Godot: x, y, z direto.',
  limites:{meiaLargura:Sim.HALF, planoOriginal4v4:Sim.HALF0, catedral:Sim.CATEDRAL, claustro:Sim.CLAUSTRO},
  spawns:Sim.SPAWN,
  altares:Sim.ALTARS.map((A,i)=>({i,nome:A.name,x:r(A.x),z:r(A.z)})),
  reagentes:Sim.REAG.map(([x,z])=>({x,z})),
  mercadores:Sim.NPCS.map(n=>({id:n.id,nome:n.nome,x:r(n.x),z:r(n.z),time:n.time,vende:n.vende})),
  pontosDeTarefa:Sim.PONTOS_DEF,
  portais:Sim.PORTAIS,
  lugares:Sim.LUGARES,
  luzes:Sim.CANDLES.map(([x,z,tipo])=>({x:r(x),z:r(z),tipo:tipo||'vela'})),
  trilhas:Sim.TRILHAS.map(T=>({largura:T.w,estreita:!!T.estreita,pontos:T.pts})),
  clareiras:Sim.CLAREIRAS.map(c=>({x:r(c.x),z:r(c.z),raio:c.r})),
  arcos:Sim.ARCOS,
  pilares:Sim.PILLARS.map(([x,z])=>({x,z,raio:.85,h:14})),
  bancos:Sim.PEWS.map(([x1,x2,z1,z2])=>({x1,x2,z1,z2,h:.95})),
  // caixas de colisão: tudo que bloqueia passagem. tall=true também bloqueia visão e tiros.
  pecas:Sim.WALLS.map(w=>{ const o={tipo:w.kind,x1:r(w.x1),x2:r(w.x2),z1:r(w.z1),z2:r(w.z2),h:r(w.h),tall:!!w.tall}; if(w.rot!=null) o.rot=w.rot; if(w.pose!=null) o.pose=w.pose; return o; }),
  arvores:Sim.ARVORES.map(q=>({x:q.x,z:q.z,raio:q.r,tipo:['anciã','alta','morta'][q.tipo],escala:q.s,rot:q.rot})),
  vegetacaoRasteira:Sim.ARBUSTOS.map(q=>({x:q.x,z:q.z,escala:q.s,rot:q.rot,tipo:q.tipo?'moita':'samambaia'})),
};
fs.mkdirSync(path.dirname(out),{recursive:true}); fs.writeFileSync(out,JSON.stringify(mapa));
const cont=Object.fromEntries(Object.entries(mapa).filter(([k,v])=>Array.isArray(v)).map(([k,v])=>[k,v.length]));
console.log('exportado',out,(fs.statSync(out).size/1024).toFixed(0)+' KB',JSON.stringify(cont));
