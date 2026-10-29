#!/usr/bin/env node
// Exporta a planta do jogo (a mesma que shared/sim.js gera com semente fixa) para docs/mapa.json,
// para montar o mapa em outra engine. Unidades em metros; plano do chão = (x, z); altura = y (h nas peças).
// Uso: node tools/exportar-mapa.js [saida]
// Um par de arquivos por mapa: a catedral em mapa.json/simulacao.json e os outros em mapa-<id>.json/simulacao-<id>.json
// (o asilo: mapa-asilo.json e simulacao-asilo.json).
'use strict';
const fs=require('fs'), path=require('path');
const Sim=require('../shared/sim.js');
const out=process.argv[2]||path.join(__dirname,'..','docs','mapa.json');
const UNITY=path.join(__dirname,'..','unity','Assets','RitualReversal','Resources','mapa.json'); // o importador e o mundo do Unity leem esta cópia
const r=v=>Math.round(v*100)/100;
function exportar(id){ Sim.usarMapa(id);
const M=Sim.mapa(id);
const mapa={id,nome:M.nome,
  sobre:'Ritual Reversal: planta gerada por shared/sim.js. Metros; chão no plano x-z, y para cima. '+
    'Unity: X=x, Y=y, Z=-z (rotação em y: -rot). Unreal: X=x*100, Y=z*100, Z=y*100. Godot: x, y, z direto. '+
    'Sem listas dentro de listas: pontos são objetos {x,z}, para o JsonUtility do Unity ler direto.',
  limites:{meiaLargura:Sim.HALF, catedral:Sim.CATEDRAL, claustro:Sim.CLAUSTRO, promontorio:Sim.PROMONTORIO, desfiladeiro:Sim.DESFILADEIRO},
  zonas:Sim.ZONAS, travessias:Sim.TRAVESSIAS,
  spawns:Sim.SPAWN,
  altares:Sim.ALTARS.map((A,i)=>({i,nome:A.name,x:r(A.x),z:r(A.z)})),
  reagentes:Sim.REAG.map(([x,z])=>({x,z})),
  mercadores:Sim.NPCS.map(n=>({id:n.id,nome:n.nome,x:r(n.x),z:r(n.z),time:n.time,vende:n.vende})),
  pontosDeTarefa:Sim.PONTOS_DEF,
  portais:Sim.PORTAIS,
  lugares:Sim.LUGARES.map(L=>({nome:L.name,x:r(L.x),z:r(L.z)})),
  luzes:Sim.CANDLES.map(([x,z,tipo,setor])=>{ const o={x:r(x),z:r(z),tipo:tipo||'vela'}; if(setor) o.setor=setor; return o; }),
  trilhas:Sim.TRILHAS.map(T=>({largura:T.w,estreita:!!T.estreita,pontos:T.pts.map(([x,z])=>({x:r(x),z:r(z)}))})),
  clareiras:Sim.CLAREIRAS.map(c=>({x:r(c.x),z:r(c.z),raio:c.r})),
  arcos:Sim.ARCOS.map(([x1,z1,x2,z2])=>({x1,z1,x2,z2})),
  pilares:Sim.PILLARS.map(([x,z])=>({x,z,raio:.85,h:14})),
  bancos:Sim.PEWS.map(([x1,x2,z1,z2])=>({x1,x2,z1,z2,h:.95})),
  // caixas de colisão: tudo que bloqueia passagem. tall=true também bloqueia visão e tiros.
  pecas:Sim.WALLS.map(w=>{ const o={tipo:w.kind,x1:r(w.x1),x2:r(w.x2),z1:r(w.z1),z2:r(w.z2),h:r(w.h),tall:!!w.tall}; if(w.rot!=null) o.rot=w.rot; if(w.pose!=null) o.pose=w.pose; return o; }),
  arvores:Sim.ARVORES.map(q=>({x:q.x,z:q.z,raio:q.r,tipo:['anciã','alta','morta'][q.tipo],escala:q.s,rot:q.rot,inc:q.inc})),
  vegetacaoRasteira:Sim.ARBUSTOS.map(q=>({x:q.x,z:q.z,escala:q.s,rot:q.rot,tipo:q.tipo?'moita':'samambaia'})),
  // asilo: salas e corredores (int = coberto), vãos entre eles, portas que abrem e fecham, quadros de força, água rasa
  espacos:Sim.ESPACOS, vaos:Sim.VAOS, portasDin:Sim.PORTAS_DIN, quadros:Sim.QUADROS, agua:M.AGUA||[],
};
// Dados da simulação para o porte em C# (Scripts/Simulacao): precisão total, na ordem exata de shared/sim.js,
// porque a ordem das caixas decide como a colisão empurra os personagens. O C# refaz a navegação a partir daqui.
const simulacao={
  sobre:'Gerado por tools/exportar-mapa.js a partir de shared/sim.js. Lido por Scripts/Simulacao/Mapa.cs. Não edite à mão.',
  id, nome:M.nome,
  // o que muda de um mapa para outro nas regras: o nome fora das zonas, se fora das zonas é coberto, a zona com névoa,
  // a tarefa do Crepúsculo; e no asilo, portas, quadros de força (lâmpadas por setor) e água rasa
  zonaFora:M.zoneAt(1e6,1e6), cobertoFora:!!M.coberto(1e6,1e6), nevoa:id==='asilo'?'Terreno':'Floresta',
  tarefas0:M.TAREFAS?M.TAREFAS[0]:null,
  portasDin:Sim.PORTAS_DIN.map(p=>[p.i,p.x1,p.x2,p.z1,p.z2,p.x,p.z,p.eixo]), quadros:Sim.QUADROS.map(q=>[q.setor,q.x,q.z]), agua:(M.AGUA||[]).map(q=>[q.x1,q.x2,q.z1,q.z2]),
  luzSetor:Sim.CANDLES.map(c=>c[3]||null),
  half:Sim.HALF, catedral:Sim.CATEDRAL, claustro:Sim.CLAUSTRO, zonas:Sim.ZONAS,
  caixas:Sim.BOX.map(b=>[b.x1,b.x2,b.z1,b.z2,b.h,b.tall?1:0]),
  altares:Sim.ALTARS.map(A=>({nome:A.name,x:A.x,z:A.z})),
  spawns:Sim.SPAWN, reagentes:Sim.REAG, portas:Sim.PORTAS.map(p=>[p.x,p.z]), portais:Sim.PORTAIS,
  pista:Sim.PISTA_PTS, erva:Sim.ERVA_PTS, sentinela:Sim.SENTINELA_PTS, pontos:Sim.PONTOS_DEF,
  mercadores:Sim.NPCS.map(n=>({id:n.id,nome:n.nome,x:n.x,z:n.z,time:n.time,vende:n.vende})),
  luzes:Sim.CANDLES.map(c=>[c[0],c[1]]), clareiras:Sim.CLAREIRAS.map(c=>[c.x,c.z,c.r]),
  trilhas:Sim.SEG_TRILHA.map(s=>[s.ax,s.az,s.bx,s.bz,s.w,s.estreita?1:0]),
  conferencia:{nos:Sim.WP.length,arestas:Sim.WP.reduce((s,w)=>s+w.n.length,0),principal:Sim.NAV.principal},
};
const suf=id==='catedral'?'':'-'+id, dir=path.dirname(out);
const arq=process.argv[2]?(suf?out.replace(/\.json$/,suf+'.json'):out):path.join(dir,'mapa'+suf+'.json');
fs.mkdirSync(path.dirname(arq),{recursive:true}); fs.writeFileSync(arq,JSON.stringify(mapa));
if(!process.argv[2]){ const U=path.join(path.dirname(UNITY),'mapa'+suf+'.json'), SU=path.join(path.dirname(UNITY),'simulacao'+suf+'.json');
  fs.mkdirSync(path.dirname(U),{recursive:true}); fs.writeFileSync(U,JSON.stringify(mapa)); fs.writeFileSync(SU,JSON.stringify(simulacao)); }
const cont=Object.fromEntries(Object.entries(mapa).filter(([k,v])=>Array.isArray(v)&&v.length).map(([k,v])=>[k,v.length]));
console.log('exportado',arq,(fs.statSync(arq).size/1024).toFixed(0)+' KB',JSON.stringify(cont));
}
for(const id of Sim.IDS_MAPAS) exportar(id);
Sim.usarMapa('catedral');
