#!/usr/bin/env node
// Teste no navegador: abre o arquivo único gerado por tools/build.js no Chromium, entra numa partida solo,
// fotografa pontos do mapa e mede erros, chamadas de desenho e triângulos.
// Uso: THREE_DIR=/caminho/node_modules/three node tools/navegador.js [pasta-das-capturas]
// (THREE_DIR serve quando o CDN não está acessível; as requisições ao jsdelivr são atendidas por arquivos locais.)
'use strict';
const fs=require('fs'), path=require('path');
const {chromium}=require('playwright');
const ROOT=path.join(__dirname,'..'), OUT=process.argv[2]||path.join(ROOT,'dist','capturas');
const THREE_DIR=process.env.THREE_DIR;
fs.mkdirSync(OUT,{recursive:true});
const VISTAS=[
  {nome:'acampamento',x:-126,z:3,yaw:-Math.PI/2,pitch:.05},
  {nome:'trilha-oeste',x:-104,z:1,yaw:-Math.PI/2,pitch:.08},
  {nome:'mata-noroeste',x:-100,z:-60,yaw:-.6,pitch:0},
  {nome:'menires',x:-40,z:-76,yaw:0,pitch:-.02},
  {nome:'fachada-sul',x:4,z:68,yaw:.35,pitch:.25},
  {nome:'carvalho-oco',x:36,z:80,yaw:Math.PI,pitch:.12},
  {nome:'cemiterio',x:104,z:66,yaw:Math.PI*.85,pitch:0},
  {nome:'circulo-de-pedras',x:124,z:0,yaw:Math.PI/2,pitch:.02},
  {nome:'encruzilhada',x:40,z:-70,yaw:0,pitch:.04},
  {nome:'claustro-ceu',x:6,z:6,yaw:0,pitch:.9},
  {nome:'nave',x:-80,z:-10,yaw:-Math.PI/2,pitch:.05}];
(async()=>{
  const browser=await chromium.launch({args:['--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist']});
  const page=await browser.newPage({viewport:{width:1280,height:720}});
  const erros=[];
  page.on('pageerror',e=>erros.push('pageerror: '+e.message));
  page.on('console',m=>{ const t=m.text(); if(m.type()==='error'&&!/api\/info|ERR_FAILED/.test(t)) erros.push('console: '+t); }); // a sondagem do servidor falha de propósito em file://
  await page.route('**/*',r=>{ const u=r.request().url();
    if(THREE_DIR&&u.includes('cdn.jsdelivr.net/npm/three@0.128.0/')){ const rel=u.split('three@0.128.0/')[1]; return r.fulfill({path:path.join(THREE_DIR,rel),contentType:'application/javascript'}); }
    if(u.startsWith('file:')) return r.continue();
    if(u.includes('fonts.g')) return r.abort();
    return r.continue(); });
  const t0=Date.now();
  await page.goto('file://'+(process.env.HTML||path.join(ROOT,'dist','ritual-reversal.html'))+'#autotest');
  await page.waitForFunction(()=>window.__T,null,{timeout:120000});
  const boot=Date.now()-t0;
  await page.addStyleTag({content:'#scr{display:none!important}'}); // sem mouse capturado o jogo mostra a pausa por cima
  await page.evaluate(()=>{ __T.startLocal(); });
  await page.waitForFunction(()=>__T.game&&__T.game.phase==='intro',null,{timeout:30000});
  await page.evaluate(()=>{ __T.sendAct({type:'ready'}); });
  await page.waitForFunction(()=>__T.game.phase==='play',null,{timeout:30000});
  await page.waitForTimeout(800); // o primeiro quadro da partida alinha a câmera ao spawn; só depois as vistas valem
  const medidas=[];
  for(const v of VISTAS){
    await page.evaluate(v=>{ const m=__T.game.actors.find(a=>a.cid==='local'); m.x=v.x; m.z=v.z; m.invuln=999; m.hp=m.maxHp; __T.LOOK.yaw=v.yaw; __T.LOOK.pitch=v.pitch; m.inp.yaw=v.yaw; m.inp.pitch=v.pitch; for(const a of __T.game.actors) if(a!==m){ a.x=a.team==='H'?-140:140; a.z=a.team==='H'?-8:8; } },v);
    await page.waitForTimeout(1500);
    const info=await page.evaluate(()=>__T.info());
    medidas.push({vista:v.nome,...info});
    await page.screenshot({path:path.join(OUT,v.nome+'.png')});
  }
  await page.keyboard.down('Tab'); await page.waitForTimeout(600); await page.screenshot({path:path.join(OUT,'planta.png')}); await page.keyboard.up('Tab');
  // partida acelerada: despausa, avança a simulação em blocos de 30 s e deixa o cliente desenhar e tratar os eventos entre eles
  await page.evaluate(()=>{ const b=document.querySelector('#bRes'); if(b) b.click(); const m=__T.game.actors.find(a=>a.cid==='local'); m.x=-128; m.z=0; m.invuln=0; });
  let fases=[];
  for(let k=0;k<40;k++){ const r=await page.evaluate(()=>{ const g=__T.game; if(g.phase==='play') for(let i=0;i<900&&g.phase==='play';i++) __T.Sim.step(g,1/30); return {ph:g.phase,rt:Math.round(g.rt),fendas:g.altars.filter(A=>A.state==='fenda').length,far:g.altars.filter(A=>A.state==='farol').length}; });
    fases.push(r); await page.waitForTimeout(400); if(r.ph!=='play') break; }
  const ult=fases[fases.length-1]; console.log(`partida acelerada: ${fases.length} blocos, terminou em ${ult.ph} aos ${ult.rt} s, ${ult.fendas} Fendas, ${ult.far} Faróis`);
  await page.screenshot({path:path.join(OUT,'fim.png')});
  console.log('carregamento até o menu:',(boot/1000).toFixed(1),'s');
  console.table(medidas.map(m=>({vista:m.vista,chamadas:m.chamadas,triangulos:m.triangulos,geometrias:m.geometrias})));
  if(erros.length){ console.log('ERROS:'); [...new Set(erros)].slice(0,20).forEach(e=>console.log(' ',e)); }
  else console.log('nenhum erro no console');
  await browser.close();
  process.exit(erros.length?1:0);
})().catch(e=>{ console.error(e); process.exit(2); });
