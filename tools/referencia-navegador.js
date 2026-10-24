#!/usr/bin/env node
// Fotos de referência do navegador: as mesmas 17 vistas do F10 do Unity (PartidaReferencia.cs), na mesma noite limpa
// (matchId "foto<i>") e sem o HUD. Depois, tools/comparar-referencia.py junta cada foto do Unity com a do navegador.
// Uso: THREE_DIR=<three@0.128.0> NODE_PATH=$(npm root -g) node tools/referencia-navegador.js [pasta] [largura] [altura]
// (padrão: dist/referencia/navegador, 1920 × 1080; use a resolução da janela do Unity para comparar sem esticar)
'use strict';
const fs=require('fs'), path=require('path');
const {chromium}=require('playwright');
const ROOT=path.join(__dirname,'..'), OUT=process.argv[2]||path.join(ROOT,'dist','referencia','navegador');
const W=+process.argv[3]||1920, H=+process.argv[4]||1080, THREE_DIR=process.env.THREE_DIR;
const VISTAS=[ // as VISTAS do tools/navegador.js e do VISTAS_REF do Unity, na mesma ordem
  ['acampamento',-122,-22,-Math.PI/2,.05],['ponte-grande',0,-2,0,.25],['ponte-caida',-106,-77,-Math.PI/2,.02],['aqueduto',106,-77,Math.PI/2,.02],
  ['nave',0,-38,0,.12],['cruzeiro',0,-66,0,.25],['claustro',-36,-46,-.7,.1],['sala-capitulo',-53,-47,Math.PI/4,-.05],['capela-desabada',22,-71,-Math.PI/2,.05],
  ['menires',-82,50,Math.PI,0],['encapuzados',82,52,Math.PI,.05],['portaria',0,10,-Math.PI/2,0],['pedreira',-46,28,Math.PI,.05],['charco',46,30,Math.PI,-.05],
  ['carvoaria',14,50,Math.PI,.05],['calvario',0,76,Math.PI,.1],['ruinas',-120,-76,0,.05]];
(async()=>{
  fs.mkdirSync(OUT,{recursive:true});
  const browser=await chromium.launch({args:['--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist']});
  const page=await browser.newPage({viewport:{width:W,height:H}}); const erros=[];
  page.on('pageerror',e=>erros.push(e.message));
  await page.route('**/*',r=>{ const u=r.request().url();
    if(THREE_DIR&&u.includes('cdn.jsdelivr.net/npm/three@0.128.0/')) return r.fulfill({path:path.join(THREE_DIR,u.split('three@0.128.0/')[1]),contentType:'application/javascript'});
    if(u.startsWith('file:')||u.startsWith('blob:')||u.startsWith('data:')) return r.continue(); return r.abort(); });
  await page.goto('file://'+(process.env.HTML||path.join(ROOT,'dist','ritual-reversal.html'))+'#autotest');
  await page.waitForFunction(()=>window.__T,null,{timeout:180000});
  await page.evaluate(()=>{ __T.startLocal(); }); await page.waitForFunction(()=>__T.game&&__T.game.phase==='intro',null,{timeout:30000});
  await page.evaluate(()=>{ __T.sendAct({type:'ready'}); }); await page.waitForFunction(()=>__T.game.phase==='play',null,{timeout:30000});
  await page.addStyleTag({content:'#scr,#hud,#feed,#legendas,#bussola,#anuncio,#lance,#dica,#tut{display:none!important}'});
  await page.evaluate(()=>{ let i=0; while(__T.climaDe('foto'+i+':1')!=='limpa') i++; __T.game.matchId='foto'+i; });   // a mesma conta do Unity
  await page.waitForTimeout(1500);
  for(let k=0;k<VISTAS.length;k++){ const [nome,x,z,yaw,pitch]=VISTAS[k];
    await page.evaluate(v=>{ const m=__T.game.actors.find(a=>a.cid==='local'); m.x=v.x; m.z=v.z; m.invuln=999; m.hp=m.maxHp; __T.LOOK.yaw=v.yaw; __T.LOOK.pitch=v.pitch; m.inp.yaw=v.yaw; m.inp.pitch=v.pitch;
      for(const a of __T.game.actors) if(a!==m){ a.x=a.team==='H'?-125:125; a.z=a.team==='H'?-8:8; } },{x,z,yaw,pitch});
    await page.waitForTimeout(2500);
    const arq=path.join(OUT,String(k+1).padStart(2,'0')+'-'+nome+'.png'); await page.screenshot({path:arq,timeout:180000}); console.log('foto',path.basename(arq)); }
  await browser.close();
  if(erros.length){ console.error('erros na página:\n'+erros.join('\n')); process.exit(1); }
  console.log('17 fotos em '+OUT);
})();
