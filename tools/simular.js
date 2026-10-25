#!/usr/bin/env node
// Gera partidas só de bots e grava os registros, para testar mudanças de balanceamento sem jogar.
// Uso: node tools/simular.js [quantidade] [pasta]      (padrão: 20 partidas em ./logs)
// Com LAB=fogo=a,execucao=6 (mesmo formato de tools/comparar-cs.js), as partidas usam o laboratório de playtest.
// Com CFG=ecoProg=0,ataqueCanal=0 muda números do CFG (só nesta execução), para medir uma regra de cada vez.
// Com SEMENTE=123 as partidas são reproduzíveis: a partida i usa o sorteio Mulberry32(123 + i) e o id sim-s123-00i.
// Com MAPA=asilo as noites são no Asilo Blackwater (padrão: a catedral).
// Duas variantes com a mesma semente começam do mesmo sorteio (as noites se separam quando a regra muda algo).
'use strict';
const fs=require('fs'), path=require('path');
const Sim=require('../shared/sim.js');
const N=+process.argv[2]||20, dir=process.argv[3]||path.join(__dirname,'..','logs');
fs.mkdirSync(dir,{recursive:true});
const slots=[{team:'A',cid:null,name:'Bot A1'},{team:'A',cid:null,name:'Bot A2'},{team:'B',cid:null,name:'Bot B1'},{team:'B',cid:null,name:'Bot B2'}];
function labDoAmbiente(){ const s=process.env.LAB; if(!s) return undefined; const o={};
  for(const par of s.split(',')){ const [k,v]=par.split('=').map(x=>x&&x.trim()); if(!k||v==null) continue;
    o[k]=k==='crepusculo'||k==='execucao'?+v:k==='economia'?(v!=='0'&&v!=='false'):v; }
  return o; }
for(const par of (process.env.CFG||'').split(',').filter(Boolean)){ const [k,v]=par.split('='); if(!(k in Sim.CFG)) throw new Error('CFG desconhecido: '+k);
  Sim.CFG[k]=v==='true'?true:v==='false'?false:isNaN(+v)?v:+v; }
const lab=labDoAmbiente(), t0=Date.now(), semente=process.env.SEMENTE!=null?+process.env.SEMENTE:null;
function mulberry32(a){ return function(){ a|=0; a=a+0x6D2B79F5|0; let t=Math.imul(a^a>>>15,1|a); t=t+Math.imul(t^t>>>7,61|t)^t; return ((t^t>>>14)>>>0)/4294967296; }; }
for(let i=0;i<N;i++){
  if(semente!=null) Math.random=mulberry32(semente+i);
  const g=Sim.createGame({timers:true,slots,matchId:semente!=null?`sim-s${semente}-${String(i+1).padStart(3,'0')}`:`sim-${t0}-${String(i+1).padStart(3,'0')}`,lab,mapa:process.env.MAPA||undefined});
  let n=0; while(g.phase!=='final'&&n<30*60*20){ Sim.step(g,1/30); g.events.length=0; n++; }
  fs.writeFileSync(path.join(dir,g.matchId+'.json'),JSON.stringify({versao:Sim.VERSAO,registro:Sim.REGISTRO,mapa:g.mapa,semente:semente!=null?semente+i:null,cfg:process.env.CFG||null,match:g.matchId,lab:g.lab,slots:g.slots,scores:g.scores,stats:g.stats,events:g.log},null,1));
  process.stdout.write(`\r${i+1}/${N} partidas`);
}
console.log(`\nRegistros em ${dir}. Agora rode: node tools/analisar.js ${dir}`);
