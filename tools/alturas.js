#!/usr/bin/env node
// O mapa não pode mentir: toda peça que a simulação trata como alta (bloqueia visão e tiro) precisa parecer alta.
// Abre o jogo no Chromium, desce raios de cima sobre cada peça alta e mede até onde o visual chega. Falha quando a peça
// inteira (em todos os pontos medidos) fica abaixo da altura do olho ou da própria caixa: aí dá para ver alguém por cima
// do que, para o servidor, bloqueia. Peças finas (estátuas, menires, troncos) passam porque o centro delas é alto.
// Uso: THREE_DIR=/caminho/node_modules/three NODE_PATH=$(npm root -g) node tools/alturas.js
'use strict';
const path=require('path'); const {chromium}=require('playwright');
const ROOT=path.join(__dirname,'..'), THREE_DIR=process.env.THREE_DIR;
(async()=>{ const browser=await chromium.launch({args:['--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist']});
  const page=await browser.newPage({viewport:{width:640,height:360}}); const erros=[]; page.on('pageerror',e=>erros.push(e.message));
  await page.route('**/*',r=>{ const u=r.request().url(); if(THREE_DIR&&u.includes('cdn.jsdelivr.net/npm/three@0.128.0/')) return r.fulfill({path:path.join(THREE_DIR,u.split('three@0.128.0/')[1]),contentType:'application/javascript'});
    if(u.includes('fonts.g')) return r.abort(); return r.continue(); });
  await page.goto('file://'+(process.env.HTML||path.join(ROOT,'dist','ritual-reversal.html'))+'#autotest'); await page.waitForFunction(()=>window.__T,null,{timeout:180000});
  const res=await page.evaluate(()=>{ const T=window.THREE, Sim=__T.Sim, sc=__T.cena(), alvos=[];
    sc.traverse(o=>{ if(o.isMesh&&!o.isInstancedMesh&&o.visible&&!(o.material&&o.material.transparent)) alvos.push(o); });
    const rc=new T.Raycaster(), OLHO=1.62+.9, ruins=[], pula=/arvore_f|espinheiro|abismo|limite|santuario/; let n=0;
    for(const w of Sim.WALLS){ if(w.h<=2.2||pula.test(w.kind)) continue; n++; let topo=-9;
      for(const [a,b] of [[.5,.5],[.2,.2],[.8,.8],[.2,.8],[.8,.2]]){ const x=w.x1+(w.x2-w.x1)*a, z=w.z1+(w.z2-w.z1)*b; rc.set(new T.Vector3(x,w.h+60,z),new T.Vector3(0,-1,0)); rc.far=w.h+70;
        const h=rc.intersectObjects(alvos,false); topo=Math.max(topo,h.length?h[0].point.y:-1); }
      if(topo<Math.min(w.h,OLHO)-.3) ruins.push(`${w.kind} (${w.x1.toFixed(1)}..${w.x2.toFixed(1)}, ${w.z1.toFixed(1)}..${w.z2.toFixed(1)}) caixa ${w.h} m, visual até ${topo.toFixed(2)} m`); }
    return {n,ruins}; });
  console.log(`${res.n} peças altas conferidas; ${res.ruins.length} com o visual abaixo do que bloqueia`); res.ruins.slice(0,40).forEach(r=>console.log('  '+r));
  if(erros.length) console.log('ERROS:\n'+erros.join('\n')); await browser.close(); process.exit(res.ruins.length||erros.length?1:0); })().catch(e=>{ console.error(e); process.exit(2); });
