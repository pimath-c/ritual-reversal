#!/usr/bin/env node
// O Asilo Blackwater no navegador: a página sobe no asilo sem erros, a porta fecha pela ação (segurar E) e a folha corre
// para o vão, o quadro de força apaga as lâmpadas da ala, a planta (Tab) desenha portas e alas sem luz, e fotos de seis lugares.
// Uso: THREE_DIR=<three@0.128.0> NODE_PATH=$(npm root -g) node tools/navegador-asilo.js [pasta]   (padrão: dist/asilo)
'use strict';
const fs=require('fs'), path=require('path');
const {chromium}=require('playwright');
const ROOT=path.join(__dirname,'..'), OUT=process.argv[2]||path.join(ROOT,'dist','asilo'), THREE_DIR=process.env.THREE_DIR;
const VISTAS=[['saguao',0,18,0,0],['corredor-oeste',-30,10,-Math.PI/2,0],['capela',0,-20,0,.1],['caldeiras',62,-42,0,.05],['gramado',0,40,0,.05],['enfermaria',-92,0,0,.05]];
(async()=>{
  fs.mkdirSync(OUT,{recursive:true}); const erros=[]; const falha=m=>{ erros.push(m); console.log('FALHA:',m); };
  const browser=await chromium.launch({args:['--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist']});
  const page=await browser.newPage({viewport:{width:1280,height:720}});
  page.setDefaultTimeout(240000);   // no swiftshader lento um screenshot pode passar de 30 s
  page.on('pageerror',e=>falha('erro na página: '+e.message));
  await page.addInitScript(()=>{ try{ sessionStorage.setItem('rr-mapa-boot','asilo'); }catch(e){} });   // o mesmo caminho da troca de mapa no título
  await page.route('**/*',r=>{ const u=r.request().url();
    if(THREE_DIR&&u.includes('cdn.jsdelivr.net/npm/three@0.128.0/')) return r.fulfill({path:path.join(THREE_DIR,u.split('three@0.128.0/')[1]),contentType:'application/javascript'});
    if(u.startsWith('file:')||u.startsWith('blob:')||u.startsWith('data:')) return r.continue(); return r.abort(); });
  await page.goto('file://'+(process.env.HTML||path.join(ROOT,'dist','ritual-reversal.html'))+'#autotest');
  await page.waitForFunction(()=>window.__T,null,{timeout:180000});
  if(await page.evaluate(()=>__T.MAPA)!=='asilo') falha('a página não subiu no asilo');
  await page.evaluate(()=>{ __T.startLocal(); }); await page.waitForFunction(()=>__T.game&&__T.game.phase==='intro',null,{timeout:30000});
  if(await page.evaluate(()=>__T.game.mapa)!=='asilo') falha('a partida local não está no asilo');
  await page.evaluate(()=>{ __T.sendAct({type:'ready'}); }); await page.waitForFunction(()=>__T.game.phase==='play',null,{timeout:30000});
  await page.addStyleTag({content:'#scr,#legendas,#anuncio,#lance,#dica,#tut{display:none!important}'});
  const longe=()=>page.evaluate(()=>{ for(const a of __T.game.actors) if(a.cid!=='local'){ a.x=a.team==='H'?-90:90; a.z=50; a.stunT=999; } });
  // porta do Arquivo (x -4..-3, z -8..-5, no corredor da Capela): o jogador no corredor completa a ação da porta
  const P=await page.evaluate(()=>{ const P=__T.Sim.PORTAS_DIN.find(p=>p.x1===-4&&p.z1===-8); const m=__T.game.actors.find(a=>a.cid==='local'); m.x=-1.6; m.z=P.z; m.invuln=999; return P; });
  let ctx=null;
  if(!P) falha('porta do Arquivo não encontrada');
  else {
    await longe();
    const folha=()=>page.evaluate(i=>__T.VIS_ASILO.PORTAS.find(D=>D.P.i===i).folhas[0].piv.position.z,P.i);
    await page.waitForFunction(i=>{ const D=__T.VIS_ASILO.PORTAS.find(D=>D.P.i===i); return D.ang>.99&&Math.abs(D.folhas[0].piv.position.z-D.folhas[0].z0)>1; },P.i,{timeout:60000}).catch(()=>{});
    const z0=await folha();
    // a ação da porta como o jogo a completa ao fim do segurar E (o contexto vem de Sim.contexts, como no HUD)
    ctx=await page.evaluate(()=>{ const g=__T.game, m=g.actors.find(a=>a.cid==='local'), c=__T.Sim.contexts(g,m); return c&&c.e; });
    if(!ctx||ctx.type!=='porta'||ctx.P!==P.i) falha('o contexto perto da porta não é a porta: '+JSON.stringify(ctx));
    else await page.evaluate(c=>{ const g=__T.game, m=g.actors.find(a=>a.cid==='local'); __T.Sim._doAction(g,m,c); },ctx);
    if(!await page.evaluate(i=>!__T.game.portas[i].aberta,P.i)) falha('a porta não fechou');
    await page.waitForFunction(i=>__T.VIS_ASILO.PORTAS.find(D=>D.P.i===i).ang<.01,P.i,{timeout:60000}).catch(()=>{});
    const z1=await folha();
    console.log('porta',P.i,'folha z:',z0.toFixed(2),'→',z1.toFixed(2));
    if(Math.abs(z1-P.z1)>.05) falha('a folha não voltou para o vão ao fechar ('+z1+')');
    if(Math.abs(z0-z1)<1) falha('a folha aberta não estava dentro da parede');
    const bloq=await page.evaluate(P=>{ const g=__T.game, m=g.actors.find(a=>a.cid==='local'); m.x=-1.6; m.z=P.z; for(let k=0;k<40;k++) __T.Sim.moveHuman(g,m,{mx:0,mz:1,yaw:-Math.PI/2,pitch:0},1/30); return m.x; },P);
    if(bloq<-2.7) falha('porta fechada não bloqueia a passagem (x='+bloq.toFixed(2)+')'); else console.log('andando contra a porta fechada, parou em x =',bloq.toFixed(2));
    await page.evaluate(()=>{ const m=__T.game.actors.find(a=>a.cid==='local'); m.x=2; __T.LOOK.yaw=-Math.PI/2; m.inp.yaw=__T.LOOK.yaw; __T.LOOK.pitch=0; m.inp.pitch=0; });
    await page.waitForTimeout(1500); await page.screenshot({path:path.join(OUT,'porta-fechada.png')});
    await page.evaluate(c=>{ const g=__T.game, m=g.actors.find(a=>a.cid==='local'); m.x=-1.6; __T.Sim._doAction(g,m,c); },ctx); }
  // dica da porta (primeira vez perto de uma porta)
  await page.evaluate(()=>{ const m=__T.game.actors.find(a=>a.cid==='local'); m.x=-1.6; m.z=-6.5; });   // (pode já ter aparecido na parte da porta)
  const dicaPorta=await page.waitForFunction(()=>{ let vistas={}; try{ vistas=JSON.parse(localStorage.getItem('rr-dicas')||'{}'); }catch(e){} return vistas.porta||__T.DC.fila.includes('porta')||/Porta: segure E/.test((document.querySelector('#dica')||{}).textContent||''); },null,{timeout:90000}).then(()=>true).catch(()=>false);
  if(!dicaPorta) falha('a dica da porta não apareceu perto da porta'); else console.log('dica da porta: ok');
  // a luz da ala onde o jogador está cai pelo quadro (um Cultista corta): aviso no meio da tela
  await page.evaluate(()=>{ const g=__T.game, m=g.actors.find(a=>a.cid==='local'), c=g.actors.find(a=>a.team==='C'); m.x=-30; m.z=10; __T.Sim._doAction(g,c,{type:'quadro',setor:'oeste'}); });
  const aviso=await page.waitForFunction(()=>/A luz caiu/.test((document.querySelector('#anuncio')||{}).textContent||''),null,{timeout:60000}).then(()=>true).catch(()=>false);
  if(!aviso) falha('o aviso "A luz caiu" não apareceu'); else console.log('aviso do apagão: ok');
  await page.evaluate(()=>{ __T.game.energia.oeste=0; });
  // quadro de força: corta a luz do oeste; as lâmpadas da ala apagam e as do centro continuam
  await page.evaluate(()=>{ const g=__T.game; g.energia.oeste=g.t+60; });
  await page.waitForFunction(()=>__T.VIS_ASILO.LAMP.oeste.every(l=>l.fonte.int===0),null,{timeout:60000}).catch(()=>{});
  const luz=await page.evaluate(()=>{ const L=__T.VIS_ASILO.LAMP;
    return {oeste:L.oeste.map(l=>l.fonte.int).reduce((a,b)=>a+b,0),centro:L.centro.map(l=>l.fonte.int).reduce((a,b)=>a+b,0),n:[L.oeste.length,L.centro.length,L.leste.length]}; });
  console.log('lâmpadas por ala',luz.n.join('/'),'intensidade oeste',luz.oeste.toFixed(2),'centro',luz.centro.toFixed(2));
  const semLuz=await page.evaluate(()=>{ const L=__T.VIS_ASILO.LAMP; return [...L.oeste,...L.centro,...L.leste].filter(l=>!__T.FONTES.includes(l.fonte)).length; });
  if(semLuz) falha(semLuz+' lâmpadas sem fonte de luz (FONTES)');
  if(luz.oeste>.01) falha('lâmpadas do oeste acesas com a luz cortada'); if(luz.centro<1) falha('lâmpadas do centro apagaram junto');
  // planta: portas e alas sem luz desenhadas
  await page.keyboard.down('Tab'); await page.waitForTimeout(800);
  const planta=await page.evaluate(()=>{ const s=document.createElementNS('http://www.w3.org/2000/svg','svg'); __T.mapSvg(s,{}); const sombra=[...s.querySelectorAll('rect')].filter(r=>r.getAttribute('opacity')==='.45').length; return {linhas:s.querySelectorAll('line').length,fechadas:[...s.querySelectorAll('line')].filter(l=>!l.getAttribute('stroke-dasharray')).length,salasSemLuz:sombra}; });
  await page.screenshot({path:path.join(OUT,'planta.png')}); await page.keyboard.up('Tab');
  console.log('planta:',JSON.stringify(planta)); if(!planta||planta.linhas!==18) falha('a planta não desenhou as 18 portas'); if(!planta||!planta.salasSemLuz) falha('a planta não escureceu a ala sem luz');
  await page.evaluate(()=>{ __T.game.energia.oeste=0; });
  for(const [nome,x,z,yaw,pitch] of VISTAS){ await longe();
    await page.evaluate(v=>{ const m=__T.game.actors.find(a=>a.cid==='local'); m.x=v.x; m.z=v.z; m.invuln=999; m.hp=m.maxHp; __T.LOOK.yaw=v.yaw; __T.LOOK.pitch=v.pitch; m.inp.yaw=v.yaw; m.inp.pitch=v.pitch; },{x,z,yaw,pitch});
    await page.waitForTimeout(1800); await page.screenshot({path:path.join(OUT,nome+'.png')}); console.log('foto',nome); }
  await browser.close();
  if(erros.length){ console.error(erros.length+' falha(s)'); process.exit(1); }
  console.log('Asilo no navegador: tudo passou.');
})().catch(e=>{ console.error(e); process.exit(1); });
