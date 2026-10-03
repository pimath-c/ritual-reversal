#!/usr/bin/env node
// Testa a pasta do app Android (mobile/www) num navegador de celular emulado SEM internet:
// o jogo tem que abrir e rodar contra bots só com os arquivos do app, e conectar num servidor digitado.
// Uso: node tools/build-mobile.js && NODE_PATH=$(npm root -g) node tools/app.js
'use strict';
const path=require('path'), http=require('http'), fs=require('fs'), {spawn}=require('child_process');
const {chromium}=require('playwright');
const ROOT=path.join(__dirname,'..'), WWW=path.join(ROOT,'mobile','www');
const TIPOS={'.html':'text/html; charset=utf-8','.js':'application/javascript'};
const estatico=http.createServer((q,r)=>{ const f=path.join(WWW,decodeURIComponent(q.url.split('?')[0]==='/'?'/index.html':q.url.split('?')[0]));
  if(!f.startsWith(WWW)||!fs.existsSync(f)){ r.writeHead(404); return r.end(); } r.writeHead(200,{'Content-Type':TIPOS[path.extname(f)]||'application/octet-stream'}); fs.createReadStream(f).pipe(r); });
(async()=>{
  await new Promise(ok=>estatico.listen(8123,ok));
  const srv=spawn(process.execPath,[path.join(ROOT,'server','server.js')],{env:{...process.env,PORT:'8089'},stdio:'ignore'});
  await new Promise(ok=>setTimeout(ok,800));
  const browser=await chromium.launch({args:['--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist']});
  const ctx=await browser.newContext({viewport:{width:844,height:390},isMobile:true,hasTouch:true,userAgent:'Mozilla/5.0 (Linux; Android 13; Pixel 7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0 Mobile Safari/537.36'});
  const page=await ctx.newPage(), erros=[], fora=[];
  page.on('pageerror',e=>erros.push(e.message)); page.on('console',m=>{ if(m.type()==='error'&&!/ERR_FAILED|net::/.test(m.text())) erros.push(m.text()); });
  await page.route('**/*',r=>{ const u=r.request().url(); if(/^(https?|wss?):\/\/localhost:(8123|8089)\//.test(u)) return r.continue(); fora.push(u); return r.abort(); }); // sem internet
  let falhas=0; const confere=(ok,msg)=>{ console.log((ok?'  ok   ':'  FALHA ')+msg); if(!ok) falhas++; };
  await page.goto('http://localhost:8123/#autotest'); await page.waitForFunction(()=>window.__T,null,{timeout:180000});
  confere(await page.evaluate(()=>window.RR_APP===true),'marcado como app');
  confere(await page.evaluate(()=>typeof THREE==='object'&&!!THREE.EffectComposer),'three.js carregou de dentro do app');
  confere(await page.evaluate(()=>!!document.querySelector('#iServ')),'tela inicial pede o endereço do servidor');
  // contra bots, sem internet
  await page.evaluate(()=>__T.startLocal()); await page.waitForFunction(()=>__T.game&&__T.game.phase==='intro',null,{timeout:30000});
  await page.evaluate(()=>__T.sendAct({type:'ready'})); await page.waitForFunction(()=>__T.game.phase==='play',null,{timeout:30000});
  confere(true,'partida contra bots começou sem internet');
  // servidor digitado
  await page.goto('http://localhost:8123/'); await page.waitForSelector('#iServ',{timeout:180000});
  await page.fill('#iServ','localhost:8089'); await page.tap('#bServ');
  await page.waitForFunction(()=>!!document.querySelector('#bCreate')&&document.querySelector('.online.on #bCreate'),null,{timeout:20000}).catch(()=>{});
  confere(await page.evaluate(()=>!!document.querySelector('.online.on #bCreate')),'achou o servidor em localhost:8089');
  await page.fill('#iName','Teste'); await page.tap('#bCreate');
  await page.waitForFunction(()=>/[A-Z]{4}/.test(document.querySelector('#scrBody').innerText)&&/sala|Sala/.test(document.querySelector('#scrBody').innerText),null,{timeout:20000}).catch(()=>{});
  const txt=await page.evaluate(()=>document.querySelector('#scrBody').innerText.slice(0,120).replace(/\n/g,' '));
  confere(/sala/i.test(txt),'criou uma sala pelo WebSocket: '+txt.slice(0,60));
  await page.reload(); await page.waitForSelector('#iServ',{timeout:180000});
  confere(await page.evaluate(()=>document.querySelector('#iServ').value==='localhost:8089'),'o endereço fica salvo');
  console.log('  pedidos para fora bloqueados:',[...new Set(fora.map(u=>new URL(u).host))].join(', ')||'nenhum');
  if(erros.length){ console.log('ERROS:'); [...new Set(erros)].slice(0,10).forEach(e=>console.log(' ',e)); falhas+=erros.length; }
  console.log(falhas?`${falhas} falha(s)`:'app: tudo passou');
  await browser.close(); srv.kill(); estatico.close(); process.exit(falhas?1:0);
})().catch(e=>{ console.error(e); process.exit(2); });
