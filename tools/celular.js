#!/usr/bin/env node
// Teste em celular emulado: tela 844 × 390 com toque. Confere que o jogo escolhe a qualidade mínima, mostra os
// controles de toque e que o analógico anda, o arrasto gira a câmera e o botão de atacar atira.
// Uso: THREE_DIR=/caminho/node_modules/three node tools/celular.js [pasta-das-capturas]
'use strict';
const fs=require('fs'), path=require('path');
const {chromium}=require('playwright');
const ROOT=path.join(__dirname,'..'), OUT=process.argv[2]||path.join(ROOT,'dist','capturas'), THREE_DIR=process.env.THREE_DIR;
fs.mkdirSync(OUT,{recursive:true});
(async()=>{
  const browser=await chromium.launch({args:['--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist']});
  const ctx=await browser.newContext({viewport:{width:844,height:390},deviceScaleFactor:3,isMobile:true,hasTouch:true,
    userAgent:'Mozilla/5.0 (Linux; Android 13; Pixel 7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0 Mobile Safari/537.36'});
  const page=await ctx.newPage(), erros=[];
  page.on('pageerror',e=>erros.push('pageerror: '+e.message));
  page.on('console',m=>{ const t=m.text(); if(m.type()==='error'&&!/api\/info|ERR_FAILED/.test(t)) erros.push('console: '+t); });
  await page.route('**/*',r=>{ const u=r.request().url();
    if(THREE_DIR&&u.includes('cdn.jsdelivr.net/npm/three@0.128.0/')){ const rel=u.split('three@0.128.0/')[1]; return r.fulfill({path:path.join(THREE_DIR,rel),contentType:'application/javascript'}); }
    if(u.includes('fonts.g')) return r.abort(); return r.continue(); });
  const t0=Date.now();
  await page.goto('file://'+(process.env.HTML||path.join(ROOT,'dist','ritual-reversal.html'))+'#autotest');
  await page.waitForFunction(()=>window.__T,null,{timeout:180000});
  console.log('carregamento até o menu:',((Date.now()-t0)/1000).toFixed(1),'s');
  await page.evaluate(()=>{ __T.startLocal(); });
  await page.waitForFunction(()=>__T.game&&__T.game.phase==='intro',null,{timeout:30000});
  await page.evaluate(()=>{ __T.sendAct({type:'ready'}); });
  await page.waitForFunction(()=>__T.game.phase==='play',null,{timeout:30000});
  await page.waitForTimeout(1000);
  if(await page.evaluate(()=>!!document.querySelector('#bRes'))){ await page.tap('#bRes'); await page.waitForTimeout(300); }
  const perfil=await page.evaluate(()=>__T.perfil());
  console.log('perfil:',JSON.stringify(perfil));
  let falhas=0; const confere=(ok,msg)=>{ console.log((ok?'  ok   ':'  FALHA ')+msg); if(!ok) falhas++; };
  confere(perfil.qual==='minimo','qualidade automática mínima no celular');
  confere(perfil.toque,'controles de toque ligados');
  confere(perfil.luzes<=8,`luzes pontuais ativas: ${perfil.luzes}`);
  const cdp=await ctx.newCDPSession(page);
  const toque=async(tipo,pts)=>cdp.send('Input.dispatchTouchEvent',{type:tipo,touchPoints:pts.map(([x,y],i)=>({x,y,id:i+1}))});
  const estado=()=>page.evaluate(()=>{ const m=__T.game.actors.find(a=>a.cid==='local'); return {x:m.x,z:m.z,yaw:m.yaw,ammo:m.ammo,shots:m.lastShotT}; });
  // analógico: encosta à esquerda e empurra para cima por 1,5 s
  await page.evaluate(()=>{ const m=__T.game.actors.find(a=>a.cid==='local'); m.x=-20; m.z=0; m.invuln=99; });
  let a=await estado();
  await toque('touchStart',[[150,260]]); for(let k=1;k<=6;k++){ await toque('touchMove',[[150,260-k*12]]); await page.waitForTimeout(30); }
  await page.waitForTimeout(1500); let b=await estado(); await toque('touchEnd',[]);
  confere(Math.hypot(b.x-a.x,b.z-a.z)>2,`analógico andou ${Math.hypot(b.x-a.x,b.z-a.z).toFixed(1)} m`);
  // olhar: arrasta à direita
  a=await estado(); await toque('touchStart',[[520,220]]); for(let k=1;k<=8;k++){ await toque('touchMove',[[520+k*15,220]]); await page.waitForTimeout(30); } await toque('touchEnd',[]);
  await page.waitForTimeout(200); b=await estado(); confere(Math.abs(b.yaw-a.yaw)>.2,`arrasto girou a câmera ${(b.yaw-a.yaw).toFixed(2)} rad`);
  // dois dedos ao mesmo tempo: anda e olha
  a=await estado(); await toque('touchStart',[[150,260],[520,220]]);
  for(let k=1;k<=8;k++){ await toque('touchMove',[[150,260-k*9],[520-k*12,220]]); await page.waitForTimeout(30); }
  await page.waitForTimeout(800); b=await estado(); await toque('touchEnd',[]);
  confere(Math.hypot(b.x-a.x,b.z-a.z)>1&&Math.abs(b.yaw-a.yaw)>.2,'dois dedos: anda e gira juntos');
  // atacar
  const fire=await page.evaluate(()=>{ const r=document.querySelector('#tqFire').getBoundingClientRect(); return [r.x+r.width/2,r.y+r.height/2]; });
  a=await estado(); await toque('touchStart',[fire]); await page.waitForTimeout(700); await toque('touchEnd',[]); await page.waitForTimeout(200); b=await estado();
  confere(b.shots>a.shots,'botão de atacar dispara');
  await page.waitForTimeout(3000); console.log('  quadros por segundo no emulador (GPU por software):',(await page.evaluate(()=>__T.perfil())).fps);
  await page.screenshot({path:path.join(OUT,'celular.png')});
  // pausa e volta
  const pausa=await page.evaluate(()=>{ const r=document.querySelector('#tqPausa').getBoundingClientRect(); return [r.x+r.width/2,r.y+r.height/2]; });
  await toque('touchStart',[pausa]); await toque('touchEnd',[]); await page.waitForTimeout(400);
  confere(await page.evaluate(()=>!!document.querySelector('#bRes')),'pausa abre a tela de pausa');
  await page.screenshot({path:path.join(OUT,'celular-pausa.png')});
  await page.tap('#bRes'); await page.waitForTimeout(2000); // no emulador (GPU por software) cada quadro demora
  const tela=await page.evaluate(()=>document.querySelector('#scr.on')?document.querySelector('#scrBody').innerText.slice(0,80):'');
  confere(!tela,'Continuar volta ao jogo'+(tela?` (ainda mostra: ${tela})`:''));
  // retrato pede para girar
  await page.setViewportSize({width:390,height:844}); await page.waitForTimeout(300);
  confere(await page.evaluate(()=>getComputedStyle(document.querySelector('#girar')).display!=='none'),'em pé, pede para girar o aparelho');
  if(erros.length){ console.log('ERROS:'); [...new Set(erros)].slice(0,20).forEach(e=>console.log(' ',e)); falhas+=erros.length; }
  console.log(falhas?`${falhas} falha(s)`:'celular: tudo passou');
  await browser.close(); process.exit(falhas?1:0);
})().catch(e=>{ console.error(e); process.exit(2); });
