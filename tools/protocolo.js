#!/usr/bin/env node
// Teste do servidor: sobe server.js numa porta livre, conecta dois clientes, cria e entra numa sala,
// começa a partida, joga alguns segundos mandando comandos, manda lixo e confere que o servidor segue de pé.
// Uso: node tools/protocolo.js      (precisa de `npm install` em server/)
'use strict';
const path=require('path'), {spawn}=require('child_process');
const WebSocket=require(path.join(__dirname,'..','server','node_modules','ws'));
const Sim=require(path.join(__dirname,'..','shared','sim.js'));
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
  A.envia({t:'hello',create:true,name:'Ana',token:'tokA',publica:true,vis:{carabina:'tempera',revolver:'<script>',cetro:'x'.repeat(40),lixo:'y'}});
  const w=await A.ate(m=>m.t==='welcome'); if(!w) return fim('sem welcome');
  B.envia({t:'hello',code:w.code,name:'Beto',token:'tokB'});
  if(!await B.ate(m=>m.t==='welcome')) return fim('segundo cliente não entrou');
  // v6: a skin de cada jogador chega aos outros pelo lobby; só nomes curtos em minúsculas passam, o resto some
  { const lb=await B.ate(m=>m.t==='lobby'&&m.players&&m.players.length===2), ana=lb&&lb.players.find(p=>p.name==='Ana');
    if(!ana||JSON.stringify(ana.vis)!=='{"carabina":"tempera"}') falha('skins no lobby: '+JSON.stringify(ana&&ana.vis)); else console.log('  skins no lobby: '+JSON.stringify(ana.vis)+' (o resto descartado)'); }
  // sala pública aparece na lista; chat chega a todos da sala, sem tags e com limite de tamanho
  { const l=await (await fetch(`http://localhost:${PORT}/api/salas`)).json(); if(!l.some(r=>r.code===w.code&&r.jogadores===2&&r.anfitriao==='Ana')) falha('sala pública fora da lista: '+JSON.stringify(l)); else console.log('  lista de salas: '+l.map(r=>r.code+' ('+r.jogadores+')').join(', ')); }
  A.envia({t:'chat',msg:'<b>oi</b> '+'x'.repeat(300)}); const ch=await B.ate(m=>m.t==='chat'); if(!ch) falha('chat não chegou'); else if(ch.msg.includes('<')||ch.msg.length>140||ch.de!=='Ana') falha('chat sem limpeza: '+JSON.stringify(ch).slice(0,80)); else console.log('  chat: chegou limpo ('+ch.msg.length+' caracteres)');
  // só o anfitrião muda a dificuldade dos bots
  B.envia({t:'opcoes',dif:'facil'}); A.envia({t:'opcoes',dif:'nada'}); A.envia({t:'opcoes',dif:'dificil'});
  { const lb=await B.ate(m=>m.t==='lobby'&&m.dif!=='normal'); if(!lb||lb.dif!=='dificil') falha('opção de dificuldade: '+(lb&&lb.dif)); else console.log('  dificuldade dos bots: '+lb.dif+' (só o anfitrião muda)'); }
  // laboratório de playtest: só o anfitrião muda, e valores desconhecidos voltam ao normal
  B.envia({t:'opcoes',lab:{crepusculo:90}}); A.envia({t:'opcoes',lab:[1,2]}); A.envia({t:'opcoes',lab:{crepusculo:120,fogo:'<x>',economia:false}});
  { const lb=await B.ate(m=>m.t==='lobby'&&m.lab); if(!lb||lb.lab.crepusculo!==120||lb.lab.fogo!=='atual'||lb.lab.economia!==false) falha('laboratório no lobby: '+JSON.stringify(lb&&lb.lab)); else console.log('  laboratório: crepúsculo '+lb.lab.crepusculo+' s, sem economia (só o anfitrião muda)'); }
  // lixo antes de começar
  for(const lixo of ['null','42','[]','{"t":5}','{','{"t":"in","mx":"x","yaw":null}',JSON.stringify({t:'act',a:null}),JSON.stringify({t:'hello',create:true})]) A.envia(lixo);
  A.envia({t:'start'});
  if(!await A.ate(m=>m.t==='snap')) return fim('partida não começou');
  // escolhe altares se for Cultista, confirma a intro
  for(let k=0;k<40;k++){ const ph=A.ultimo&&A.ultimo.s.phase; if(ph==='play') break;
    for(const C of [A,B]){ if(!C.ultimo) continue; const s=C.ultimo.s; if(s.phase==='pick'){ C.envia({t:'act',a:{type:'pickRandom'}}); C.envia({t:'act',a:{type:'pickConfirm'}}); } if(s.phase==='intro') C.envia({t:'act',a:{type:'ready'}}); }
    await espera(250); }
  if(!A.ultimo||A.ultimo.s.phase!=='play') return fim('não chegou à fase de jogo: '+(A.ultimo&&A.ultimo.s.phase));
  if(!A.ultimo.s.lab||A.ultimo.s.lab.crepusculo!==120) falha('a partida não usou o laboratório: '+JSON.stringify(A.ultimo.s.lab));
  // chat na partida: por padrão só a equipe; 'todos' chega aos adversários
  { const antes=B.msgs.length; A.envia({t:'chat',msg:'segredo da equipe'}); await espera(700); A.envia({t:'chat',msg:'boa sorte',para:'todos'});
    const meu=await A.ate(m=>m.t==='chat'&&m.msg==='segredo da equipe'), todos=await B.ate(m=>m.t==='chat'&&m.msg==='boa sorte');
    const vazou=B.msgs.slice(antes).some(m=>m.t==='chat'&&m.msg==='segredo da equipe'), mesmo=B.ultimo.s.actors.find(a=>a.id===B.ultimo.you).team===A.ultimo.s.actors.find(a=>a.id===A.ultimo.you).team;
    if(!meu||meu.para!=='equipe') falha('chat de equipe não voltou para quem falou'); else if(!mesmo&&vazou) falha('chat de equipe vazou para os adversários');
    else if(!todos||todos.para!=='todos') falha('chat para todos não chegou'); else console.log('  chat na partida: equipe fica na equipe, "todos" chega a todos'); }
  const s0=A.snaps, t0=Date.now(); let seq=0;
  while(Date.now()-t0<6000){ seq++; for(const C of [A,B]) C.envia({t:'in',seq,dt:1/30,mx:0,mz:-1,sp:true,yaw:Math.sin(seq/20),pitch:0,fire:seq%10===0,use:false,use2:false,c:{q:0,f:0,g:0,r:0,ab:0}}); await espera(33); }
  const taxa=(A.snaps-s0)/6; console.log(`  snapshots: ${taxa.toFixed(1)}/s (meta 20)`); if(taxa<15) falha('poucos snapshots');
  const eu=A.ultimo.s.actors.find(a=>a.id===A.ultimo.you); if(!eu) falha('snapshot sem o próprio personagem');
  else { console.log(`  Ana andou até (${eu.x.toFixed(1)}, ${eu.z.toFixed(1)}) no papel ${eu.team}`); const sp=Sim.SPAWN[eu.team]; if(sp&&Math.hypot(eu.x-sp.x,eu.z-sp.z)<3) falha('personagem não se moveu'); }
  // integridade: o que chegou a cada cliente durante a partida respeita a equipe e o alcance dos sons
  { for(const C of [A,B]){ const S=C.msgs.filter(m=>m.t==='snap'); if(!S.length) continue; const meuId=C.ultimo.you, papel=C.ultimo.s.actors.find(a=>a.id===meuId).team;
      let ruins=0, vistos=0;
      for(const m of S){ const eu=m.s.actors.find(a=>a.id===meuId);
        if(papel==='H'&&m.s.altars.some(A=>A.state==='active'&&!A.localized)) ruins++;
        for(const e of m.ev||[]){ if(papel==='H'&&e.type==='selando') ruins++; if(papel==='C'&&(e.type==='eco'||e.type==='presage')) ruins++;
          if(e.type==='sfx'&&e.a!==meuId){ const em=m.s.actors.find(a=>a.id===e.a); if(em&&em.team!==papel&&em.hidden&&eu){ vistos++; const r=(Sim.ALCANCE_SOM[e.k]||40)*1.2+4; if(Math.hypot(e.x-eu.x,e.z-eu.z)>r) ruins++; } } } }
      if(ruins) falha(`${C.nome} (${papel}) recebeu ${ruins} informações que não deveria`); else C.sonsConferidos=vistos; }
    console.log(`  integridade: snapshots e eventos por equipe; sons de inimigos escondidos conferidos: ${(A.sonsConferidos||0)+(B.sonsConferidos||0)}`); }
  // ping: volta para a própria equipe e não vaza para a outra; lixo no ping é ignorado
  if(eu){ A.envia({t:'act',a:{type:'ping',x:eu.x+5,z:eu.z,k:'ponto'}}); A.envia({t:'act',a:{type:'ping',x:'lixo',z:null,k:'<b>'}}); await espera(900);
    const tem=C=>C.msgs.some(m=>m.t==='snap'&&(m.ev||[]).some(e=>e.type==='ping'));
    const mesmoTime=B.ultimo&&B.ultimo.s.actors.find(a=>a.id===B.ultimo.you).team===eu.team;
    if(!tem(A)) falha('o ping não voltou para quem marcou'); if(!mesmoTime&&tem(B)) falha('o ping vazou para a outra equipe'); else console.log('  ping: só a própria equipe recebe'); }
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
  // sala no Asilo Blackwater: o mapa vai no welcome, no lobby e na partida; quem entra recebe o mapa da sala
  const As=cliente('Asilo'), Bs=cliente('Visita'); await As.pronto; await Bs.pronto; As.envia({t:'hello',create:true,name:'Asilo',mapa:'asilo'});
  const wa=await As.ate(m=>m.t==='welcome'); if(!wa||wa.mapa!=='asilo') falha('sala do asilo sem o mapa no welcome: '+JSON.stringify(wa));
  else { Bs.envia({t:'hello',code:wa.code,name:'Visita',mapa:'catedral'}); const wb=await Bs.ate(m=>m.t==='welcome'); if(!wb||wb.mapa!=='asilo') falha('quem entra não recebe o mapa da sala');
    if(!wb||wb.versao!==require('../shared/sim.js').VERSAO) falha('welcome sem a versão do servidor');
    // jogo anterior à v6.2 (hello sem mapa) não entra numa sala que não é na catedral
    const Velho=cliente('Velho'); await Velho.pronto; Velho.envia({t:'hello',code:wa.code,name:'Velho'}); const ev=await Velho.ate(m=>m.t==='error'||m.t==='welcome');
    if(!ev||ev.t!=='error') falha('jogo sem mapa entrou na sala do asilo: '+JSON.stringify(ev)); else console.log('  jogo antigo (sem mapa) recusado na sala do asilo');
    As.envia({t:'start'}); const sa=await As.ate(m=>m.t==='snap'&&m.s&&m.s.mapa,12000)||await As.ate(m=>m.t==='snap',4000); const mp=sa&&(sa.s?sa.s.mapa:sa.mapa);
    if(mp!=='asilo') falha('a partida da sala do asilo não está no asilo: '+mp);
    else { const st=sa.s||sa; if(!Array.isArray(st.portas)||st.portas.length!==18||!st.energia) falha('snapshot do asilo sem portas/energia'); else console.log('  asilo: welcome, lobby e partida no mapa da sala; 18 portas e energia no snapshot'); } }
  const lixoMapa=cliente('Lixo'); await lixoMapa.pronto; lixoMapa.envia({t:'hello',create:true,name:'Lixo',mapa:'../../etc'}); const wl=await lixoMapa.ate(m=>m.t==='welcome'); if(!wl||wl.mapa!=='catedral') falha('mapa desconhecido deveria virar catedral');
  if(/erro não tratado|TypeError|ReferenceError/.test(saida)) falha('erro no log do servidor:\n'+saida);
  fim();
})().catch(e=>fim(String(e&&e.stack||e)));
function fim(msg){ if(msg) falha(msg); srv.kill(); console.log(falhas?`${falhas} falha(s).`:'Protocolo: tudo passou.'); process.exit(falhas?1:0); }
