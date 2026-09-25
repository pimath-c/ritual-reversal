// Ritual Reversal — servidor autoritativo.
// Uso: npm install && npm start   (porta 8080; mude com PORT=xxxx)
'use strict';
const http=require('http'), fs=require('fs'), path=require('path');
const {WebSocketServer}=require('ws');
const Sim=require('../shared/sim.js');

const PORT=+process.env.PORT||8080;
const ROOT=path.join(__dirname,'..');
const LOGDIR=path.join(ROOT,'logs'); fs.mkdirSync(LOGDIR,{recursive:true});
const TICK=1/30, SNAP_EVERY=1.5; // 30 Hz de simulação, 20 Hz de snapshots
const RESERVA_MS=120000; // quanto tempo a vaga de quem caiu fica guardada

const FILES={'/':['client/index.html','text/html; charset=utf-8'],'/index.html':['client/index.html','text/html; charset=utf-8'],'/sim.js':['shared/sim.js','application/javascript; charset=utf-8']};
const server=http.createServer((req,res)=>{
  const url=req.url.split('?')[0];
  if(url==='/api/info'){ res.writeHead(200,{'Content-Type':'application/json','Cache-Control':'no-store'}); return res.end(JSON.stringify({server:'ritual-reversal',rooms:rooms.size})); }
  if(url.startsWith('/logs/')){ const f=path.basename(url); const p=path.join(LOGDIR,f); if(/^[\w-]+\.json$/.test(f)&&fs.existsSync(p)){ res.writeHead(200,{'Content-Type':'application/json','Content-Disposition':`attachment; filename="${f}"`}); return fs.createReadStream(p).pipe(res); } }
  const f=FILES[url]; if(!f){ res.writeHead(404); return res.end('não encontrado'); }
  res.writeHead(200,{'Content-Type':f[1],'Cache-Control':'no-cache'}); fs.createReadStream(path.join(ROOT,f[0])).pipe(res);
});

const rooms=new Map();
let CID=1;
function code(){ const L='ABCDEFGHJKLMNPQRSTUVWXYZ'; let c; do{ c=''; for(let i=0;i<4;i++) c+=L[Math.floor(Math.random()*L.length)]; }while(rooms.has(c)); return c; }
function newRoom(){ const r={code:code(),clients:new Map(),host:null,slots:[0,1,2,3].map(i=>({team:i<2?'A':'B',cid:null,name:Sim.BOT_NAMES[i<2?'A':'B'][i%2]})),game:null,acc:0,snapAcc:0,logFile:null,logN:0,emptyT:0,reservas:new Map()}; rooms.set(r.code,r); return r; }
const send=(ws,o)=>{ if(ws.readyState===1) ws.send(JSON.stringify(o)); };
function lobbyState(r){ return {t:'lobby',code:r.code,host:r.host,inGame:!!r.game,slots:r.slots.map(s=>({team:s.team,name:s.name,cid:s.cid})),players:[...r.clients.values()].map(c=>({cid:c.cid,name:c.name}))}; }
function broadcastLobby(r){ const m=lobbyState(r); r.clients.forEach(c=>send(c.ws,m)); }
function seat(r,c){
  const hA=r.slots.filter(s=>s.cid&&s.team==='A').length, hB=r.slots.filter(s=>s.cid&&s.team==='B').length;
  const pref=hA<=hB?'A':'B'; let i=r.slots.findIndex(s=>!s.cid&&s.team===pref); if(i<0) i=r.slots.findIndex(s=>!s.cid);
  if(i<0) return false; r.slots[i].cid=c.cid; r.slots[i].name=c.name; return true; }
function unseat(r,cid){ r.slots.forEach((s,i)=>{ if(s.cid===cid){ s.cid=null; s.name=Sim.BOT_NAMES[s.team][i%2]; } }); }

const wss=new WebSocketServer({server,maxPayload:64*1024}); // nenhuma mensagem legítima passa de alguns KB
wss.on('connection',ws=>{
  ws.on('error',e=>{ /* mensagem grande demais ou conexão quebrada: derruba só esta conexão */ try{ ws.terminate(); }catch(_){} });
  const c={ws,cid:'c'+(CID++),name:'Jogador',room:null,lastSeq:0,budget:0,ev:[],c:null,token:null};
  ws.on('message',buf=>{ try{ tratar(buf); }catch(e){ console.error('mensagem descartada por erro:',e&&e.message); } });
  function tratar(buf){
    let m; try{ m=JSON.parse(buf); }catch(e){ return; }
    if(!m||typeof m!=='object'||Array.isArray(m)||typeof m.t!=='string') return; // null, números, listas: ignorados
    if(m.t==='hello'){ if(c.room) return; // já está numa sala: ignora o segundo pedido
      c.name=String(m.name||'Jogador').replace(/[<>]/g,'').slice(0,16)||'Jogador';
      c.token=typeof m.token==='string'?m.token.slice(0,40):null;
      let r;
      if(m.create) r=newRoom(); else { r=rooms.get(String(m.code||'').toUpperCase()); if(!r) return send(ws,{t:'error',msg:'Sala não encontrada. Confira o código.'}); }
      if(r.game){
        const res=c.token&&r.reservas.get(c.token);
        if(!res||res.ate<Date.now()) return send(ws,{t:'error',msg:'Essa partida já começou. Peça para o anfitrião voltar ao lobby.'});
        if(!Sim.reclaimHuman(r.game,res.i,c.cid,res.name)) return send(ws,{t:'error',msg:'Sua vaga já foi ocupada.'});
        r.reservas.delete(c.token); r.slots[res.i].cid=c.cid; r.slots[res.i].name=res.name; c.name=res.name;
        c.room=r; r.clients.set(c.cid,c); if(!r.host) r.host=c.cid;
        send(ws,{t:'welcome',cid:c.cid,code:r.code,voltou:true}); broadcastLobby(r);
        console.log(`[${r.code}] ${res.name} reconectou`); return;
      }
      if(!seat(r,c)) return send(ws,{t:'error',msg:'A sala está cheia (4 jogadores).'});
      c.room=r; r.clients.set(c.cid,c); if(!r.host) r.host=c.cid;
      send(ws,{t:'welcome',cid:c.cid,code:r.code}); broadcastLobby(r); return;
    }
    const r=c.room; if(!r) return;
    if(m.t==='slot'&&!r.game){ const i=m.i|0; if(r.slots[i]&&!r.slots[i].cid){ unseat(r,c.cid); r.slots[i].cid=c.cid; r.slots[i].name=c.name; broadcastLobby(r); } }
    if(m.t==='start'&&!r.game&&c.cid===r.host){
      r.game=Sim.createGame({timers:true,slots:r.slots,matchId:r.code+'-'+Date.now()});
      r.logFile=path.join(LOGDIR,r.game.matchId+'.json'); r.logN=0; broadcastLobby(r);
    }
    if(m.t==='lobby'&&r.game&&c.cid===r.host&&r.game.phase==='final'){ flushLog(r,true); r.game=null; r.reservas.clear(); broadcastLobby(r); }
    if(m.t==='in'&&r.game){
      const a=r.game.actors.find(a=>a.cid===c.cid);
      if(typeof m.seq==='number') c.lastSeq=m.seq;
      if(a){ let dt=Math.min(Math.max(+m.dt||0,0),.05); if(c.budget<dt) dt=Math.max(0,c.budget); c.budget-=dt; Sim.moveHuman(r.game,a,m,dt); }
    }
    if(m.t==='act'&&r.game) Sim.act(r.game,c.cid,m.a);
  }
  ws.on('close',()=>{ const r=c.room; if(!r) return; r.clients.delete(c.cid);
    if(r.game){ const i=r.game.slots.findIndex(s=>s.cid===c.cid);
      if(i>=0&&c.token&&r.game.phase!=='final') r.reservas.set(c.token,{i,name:c.name,ate:Date.now()+RESERVA_MS});
      Sim.dropHuman(r.game,c.cid); console.log(`[${r.code}] ${c.name} caiu; vaga guardada por ${RESERVA_MS/1000} s`); }
    unseat(r,c.cid);
    if(r.host===c.cid) r.host=r.clients.size?[...r.clients.keys()][0]:null;
    broadcastLobby(r); });
});

function flushLog(r,final){
  if(!r.game||!r.logFile) return;
  const L=r.game.log; if(L.length===r.logN&&!final) return;
  fs.writeFileSync(r.logFile,JSON.stringify({match:r.game.matchId,slots:r.slots,scores:r.game.scores,stats:r.game.stats,events:L},null,1)); r.logN=L.length;
}
function routeEvents(r){
  const g=r.game; if(!g.events.length) return;
  for(const c of r.clients.values()){
    const me=g.actors.find(a=>a.cid===c.cid); const slot=g.slots.find(s=>s.cid===c.cid); const role=slot?Sim.roleOf(g,slot.team):null;
    for(const e of g.events){
      if(e.type==='feed'&&e.team&&e.team!==role) continue;
      if(e.to!=null&&(!me||e.to!==me.id)) continue;
      if((e.type==='sensor'||e.type==='evp'||e.type==='blind'||e.type==='stun')&&(!me||e.a!==me.id)) continue;
      if(e.type==='presage'&&role!=='H') continue;
      c.ev.push(e);
    }
  }
  g.events.length=0;
}
let last=Date.now(), logT=0;
setInterval(()=>{
  const now=Date.now(); let el=Math.min(.25,(now-last)/1000); last=now;
  for(const r of rooms.values()){
    if(!r.clients.size){ r.emptyT+=el; if(r.emptyT>120){ flushLog(r,true); rooms.delete(r.code); } continue; } r.emptyT=0;
    if(!r.game) continue;
    r.acc+=el; let n=0;
    while(r.acc>=TICK&&n<8){ r.clients.forEach(c=>c.budget=Math.min(1,c.budget+TICK) /* aceita até 1 s de comandos atrasados por travada de rede */); Sim.step(r.game,TICK); routeEvents(r); r.acc-=TICK; n++; r.snapAcc++;
      if(r.snapAcc>=SNAP_EVERY){ r.snapAcc-=SNAP_EVERY; sendSnaps(r); } }
  }
  logT+=el; if(logT>2){ logT=0; for(const r of rooms.values()) flushLog(r,false); }
},1000/60);
function sendSnaps(r){
  const g=r.game, cache={};
  for(const c of r.clients.values()){
    const slot=g.slots.find(s=>s.cid===c.cid); const role=slot?Sim.roleOf(g,slot.team):'H';
    const s=cache[role]||(cache[role]=Sim.snapshot(g,role));
    const me=g.actors.find(a=>a.cid===c.cid);
    send(c.ws,{t:'snap',s,you:me?me.id:null,cid:c.cid,ack:c.lastSeq,ev:c.ev,log:g.phase==='final'?'/logs/'+g.matchId+'.json':null});
    c.ev=[];
  }
}
process.on('uncaughtException',e=>console.error('erro não tratado (servidor continua):',e&&e.stack||e));
server.listen(PORT,()=>console.log(`Ritual Reversal rodando em http://localhost:${PORT}\nPara amigos de fora: cloudflared tunnel --url http://localhost:${PORT}`));
