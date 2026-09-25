#!/usr/bin/env node
// Varre variações de parâmetros e mostra o efeito nas métricas, sem precisar jogar.
// Uso: node tools/varredura.js [partidas por variante]   (padrão: 16)
'use strict';
const Sim=require('../shared/sim.js'); const CFG=Sim.CFG;
const N=+process.argv[2]||16;
const base={}; Object.keys(CFG).forEach(k=>base[k]=CFG[k]);
const VARIANTES=[
  ['como está agora',{}],
  ['queimar custa 6 s',{burn:6}],
  ['reagente renasce em 25 s',{regrow:25}],
  ['iniciar não gasta reagente',{iniciarDeGraca:true}],
  ['queimar 6 s + renascer 25 s',{burn:6,regrow:25}],
  ['queimar 6 s + iniciar de graça',{burn:6,iniciarDeGraca:true}],
];
const linha=(a,...r)=>console.log(a.padEnd(32)+r.map(v=>String(v).padStart(13)).join(''));
linha('Variante','Rituais/rodada','Completos','Selados','Reagentes','Queimados');
console.log('-'.repeat(32+13*5));
for(const [nome,ov] of VARIANTES){
  Object.assign(CFG,base,ov);
  let rit=0,comp=0,sel=0,col=0,burn=0,rodadas=0;
  for(let i=0;i<N;i++){
    const g=Sim.createGame({timers:true,slots:[0,1,2,3].map(j=>({team:j<2?'A':'B',cid:null,name:'B'+j}))});
    let n=0; while(g.phase!=='final'&&n<30*60*20){ Sim.step(g,1/30); g.events.length=0; n++; }
    g.log.forEach(e=>{ if(e.ev==='ritual_start')rit++; if(e.ev==='ritual_complete')comp++; if(e.ev==='sealed')sel++; if(e.ev==='collect')col++; if(e.ev==='burn')burn++; if(e.ev==='round_start')rodadas++; });
  }
  linha(nome,(rit/rodadas).toFixed(2),(comp/rodadas).toFixed(2),(sel/rodadas).toFixed(2),(col/rodadas).toFixed(1),(burn/rodadas).toFixed(1));
}
Object.assign(CFG,base);
console.log('\nAlvo de projeto: 3 rituais por rodada como teto, com 1 a 2 completados.');
