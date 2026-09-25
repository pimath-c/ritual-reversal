#!/usr/bin/env node
// Teste do servidor: sobe server.js numa porta livre, conecta dois clientes, cria e entra numa sala,
// começa a partida, joga alguns segundos mandando comandos, manda lixo e confere que o servidor segue de pé.
// Uso: node tools/protocolo.js      (precisa de `npm install` em server/)
'use strict';
const path=require('path'), {spawn}=require('child_process');
const WebSocket=require(path.join(__dirname,'..','server','node_modules','ws'));
const PORT=18000+Math.floor(Math.random()*1000);
const srv=spawn(process.execPath,[path.join(__dirname,'..','server','server.js')],{env:{...process.env,PORT:String(PORT)},stdio:['ignore','pipe','pipe']});
let saida=''; srv.stdout.on('data',d=>saida+=d); srv.stderr.on('data',d=>saida+=d);
let falhas=0; const falha=m=>{ falhas++; console.log('  FALHA:',m); };
const espera=ms=>new Promise(r=>setTimeout(r,ms));
function cliente(nome){ const ws=new WebSocket(`ws://localhost:${PORT}`), c={ws,nome,msgs:[],snaps:0,ultimo:null};
  ws.on('message',b=>{ const m=JSON.parse(b); c.msgs.push(m); if(m.t==='snap'){ c.snaps++; c.ultimo=m; } });
  c.pronto=new Promise((ok,erro)=>{ ws.on('open',ok); ws.on('error',erro); }); c.envia=o=>ws.send(typeof o==='string'?o:JSON.stringify(o));
  c.ate=async(fn,ms)=>{ const t=Date.now(); while(Date.now()-t<(ms||8000)){ const m=c.msgs.find(fn); if(m) return m; await espera(30); } return null; }; return c; }
(async()=>{
  for(let i=0;i<50&&!saida.includes('rodando');i++) await espera(100);
  const A=cliente('Ana'), B=cliente('Beto'); await A.pronto; await B.pronto;
  A.envia({t:'hello',create:true,name:'Ana',token:'tokA'});
  const w=await A.ate(m=>m.t==='welcome'); if(!w) return fim('sem welcome');
  B.envia({t:'hello',code:w.code,name:'Beto',token:'tokB'});
  if(!await B.ate(m=>m.t==='welcome')) return fim('segundo cliente não entrou');
  // lixo antes de começar
  for(const lixo of ['null','42','[]','{"t":5}','{','{"t":"in","mx":"x","yaw":null}',JSON.stringify({t:'act',a:null}),JSON.stringify({t:'hello',create:true})]) A.envia(lixo);
  A.envia({t:'start'});
  if(!await A.ate(m=>m.t==='snap')) return fim('partida não começou');
  // escolhe altares se for Cultista, confirma a intro
  for(let k=0;k<40;k++){ const ph=A.ultimo&&A.ultimo.s.phase; if(ph==='play') break;
    for(const C of [A,B]){ if(!C.ultimo) continue; const s=C.ultimo.s; if(s.phase==='pick'){ C.envia({t:'act',a:{type:'pickRandom'}}); C.envia({t:'act',a:{type:'pickConfirm'}}); } if(s.phase==='intro') C.envia({t:'act',a:{type:'ready'}}); }
    await espera(250); }
  if(!A.ultimo||A.ultimo.s.phase!=='play') return fim('não chegou à fase de jogo: '+(A.ultimo&&A.ultimo.s.phase));
  const s0=A.snaps, t0=Date.now(); let seq=0;
  while(Date.now()-t0<6000){ seq++; for(const C of [A,B]) C.envia({t:'in',seq,dt:1/30,mx:0,mz:-1,sp:true,yaw:Math.sin(seq/20),pitch:0,fire:seq%10===0,use:false,use2:false,c:{q:0,f:0,g:0,r:0,ab:0}}); await espera(33); }
  const taxa=(A.snaps-s0)/6; console.log(`  snapshots: ${taxa.toFixed(1)}/s (meta 20)`); if(taxa<15) falha('poucos snapshots');
  const eu=A.ultimo.s.actors.find(a=>a.id===A.ultimo.you); if(!eu) falha('snapshot sem o próprio personagem');
  else { console.log(`  Ana andou até (${eu.x.toFixed(1)}, ${eu.z.toFixed(1)}) no papel ${eu.team}`); if(Math.hypot(eu.x-(eu.team==='H'?-132:132),eu.z)<3) falha('personagem não se moveu'); }
  // lixo no meio da partida e reconexão
  A.envia('{"t":"in","mx":1e308,"mz":-1e308,"yaw":"NaN","dt":99}'); A.envia('x'.repeat(70000));
  B.ws.close(); await espera(400);
  const B2=cliente('Beto'); await B2.pronto; B2.envia({t:'hello',code:w.code,name:'Beto',token:'tokB'});
  const w2=await B2.ate(m=>m.t==='welcome'||m.t==='error'); if(!w2||w2.t!=='welcome'||!w2.voltou) falha('reconexão falhou: '+JSON.stringify(w2));
  const C3=cliente('Curioso'); await C3.pronto; C3.envia({t:'hello',code:w.code,name:'x'});
  const neg=await C3.ate(m=>m.t==='error'); if(!neg) falha('entrada no meio da partida deveria ser negada');
  await espera(500);
  const vivo=cliente('Depois'); await vivo.pronto.catch(()=>falha('servidor caiu')); vivo.envia({t:'hello',create:true,name:'Depois'});
  if(!await vivo.ate(m=>m.t==='welcome')) falha('servidor não responde depois do lixo');
  if(/erro não tratado|TypeError|ReferenceError/.test(saida)) falha('erro no log do servidor:\n'+saida);
  fim();
})().catch(e=>fim(String(e&&e.stack||e)));
function fim(msg){ if(msg) falha(msg); srv.kill(); console.log(falhas?`${falhas} falha(s).`:'Protocolo: tudo passou.'); process.exit(falhas?1:0); }
