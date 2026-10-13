#!/usr/bin/env node
// Gera partidas só de bots e grava os registros, para testar mudanças de balanceamento sem jogar.
// Uso: node tools/simular.js [quantidade] [pasta]      (padrão: 20 partidas em ./logs)
'use strict';
const fs=require('fs'), path=require('path');
const Sim=require('../shared/sim.js');
const N=+process.argv[2]||20, dir=process.argv[3]||path.join(__dirname,'..','logs');
fs.mkdirSync(dir,{recursive:true});
const slots=[{team:'A',cid:null,name:'Bot A1'},{team:'A',cid:null,name:'Bot A2'},{team:'B',cid:null,name:'Bot B1'},{team:'B',cid:null,name:'Bot B2'}];
const t0=Date.now();
for(let i=0;i<N;i++){
  const g=Sim.createGame({timers:true,slots,matchId:`sim-${t0}-${String(i+1).padStart(3,'0')}`});
  let n=0; while(g.phase!=='final'&&n<30*60*20){ Sim.step(g,1/30); g.events.length=0; n++; }
  fs.writeFileSync(path.join(dir,g.matchId+'.json'),JSON.stringify({match:g.matchId,slots:g.slots,scores:g.scores,stats:g.stats,events:g.log},null,1));
  process.stdout.write(`\r${i+1}/${N} partidas`);
}
console.log(`\nRegistros em ${dir}. Agora rode: node tools/analisar.js ${dir}`);
