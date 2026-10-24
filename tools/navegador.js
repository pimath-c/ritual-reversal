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
const VISTAS=[ // v6: um ponto de cada região e de cada travessia do mapa novo
  {nome:'acampamento',x:-122,z:-22,yaw:-Math.PI/2,pitch:.05},
  {nome:'ponte-grande',x:0,z:-2,yaw:0,pitch:.25},
  {nome:'ponte-caida',x:-106,z:-77,yaw:-Math.PI/2,pitch:.02},
  {nome:'aqueduto',x:106,z:-77,yaw:Math.PI/2,pitch:.02},
  {nome:'nave',x:0,z:-38,yaw:0,pitch:.12},
  {nome:'cruzeiro',x:0,z:-66,yaw:0,pitch:.25},
  {nome:'claustro',x:-36,z:-46,yaw:-.7,pitch:.1},
  {nome:'sala-capitulo',x:-53,z:-47,yaw:Math.PI/4,pitch:-.05},
  {nome:'capela-desabada',x:22,z:-71,yaw:-Math.PI/2,pitch:.05},
  {nome:'menires',x:-82,z:50,yaw:Math.PI,pitch:0},
  {nome:'encapuzados',x:82,z:52,yaw:Math.PI,pitch:.05},
  {nome:'portaria',x:0,z:10,yaw:-Math.PI/2,pitch:0},
  {nome:'pedreira',x:-46,z:28,yaw:Math.PI,pitch:.05},
  {nome:'charco',x:46,z:30,yaw:Math.PI,pitch:-.05},
  {nome:'carvoaria',x:14,z:50,yaw:Math.PI,pitch:.05},
  {nome:'calvario',x:0,z:76,yaw:Math.PI,pitch:.1},
  {nome:'ruinas',x:-120,z:-76,yaw:0,pitch:.05}];
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
  // laboratório de playtest no menu: abre, muda o Crepúsculo e volta ao normal
  { await page.waitForSelector('details.lab',{timeout:20000}); await page.evaluate(()=>{ document.querySelector('details.lab').open=true; }); await page.waitForTimeout(300);
    await page.click('button[data-lab="crepusculo"][data-v="120"]'); // o menu redesenha no próximo quadro, que no swiftshader demora
    await page.waitForFunction(()=>/alterada/.test((document.querySelector('details.lab summary')||{}).textContent||''),null,{timeout:20000}).catch(()=>{});
    const lab=await page.evaluate(()=>({resumo:(document.querySelector('details.lab summary')||{}).textContent||''}));
    await page.screenshot({path:path.join(OUT,'menu-laboratorio.png')}); if(!/1 regra alterada/.test(lab.resumo)) erros.push('laboratório não marcou a regra alterada: '+lab.resumo);
    await page.click('#bLabPadrao'); await page.waitForFunction(()=>!/alterada/.test((document.querySelector('details.lab summary')||{}).textContent||''),null,{timeout:20000}).catch(()=>erros.push('laboratório não voltou ao normal')); console.log('laboratório no menu:',lab.resumo.trim()); }
  // v6: Arsenal. Abre do menu, troca a skin (a escolha é salva e a arma faz a inspeção), muda de arma e volta ao menu
  { await page.click('#bArsenal'); await page.waitForSelector('#arsenal .skin',{timeout:60000}); await page.waitForTimeout(1500);
    await page.click('#arsenal .skin[data-skin="tempera"]'); await page.waitForTimeout(1800);
    const a1=await page.evaluate(()=>({esc:__T.SKINS.ESC.carabina,clipe:__T.ARMA.S.est.nome,sel:(document.querySelector('#arsenal .skin.sel b')||{}).textContent||''}));
    await page.screenshot({path:path.join(OUT,'arsenal-carabina.png')});
    await page.click('#arsenal [data-arma="revolver"]'); await page.waitForTimeout(900); await page.click('#arsenal .skin[data-skin="martir"]');
    await page.waitForFunction(()=>__T.ARMA.S.arma==='revolver'&&__T.ARMA.S.est.nome==='inspecao',null,{timeout:30000}).catch(()=>{});
    const a2=await page.evaluate(()=>({esc:__T.SKINS.ESC.revolver,arma:__T.ARMA.S.arma,clipe:__T.ARMA.S.est.nome,t:+__T.ARMA.S.est.t.toFixed(2)}));
    await page.screenshot({path:path.join(OUT,'arsenal-revolver.png')});
    await page.click('#arsenal [data-arma="cetro"]'); await page.waitForTimeout(900); await page.click('#arsenal .skin[data-skin="afogado"]'); await page.waitForTimeout(3200);
    await page.screenshot({path:path.join(OUT,'arsenal-cetro.png')});
    await page.click('#bArsSair'); await page.waitForSelector('#bSolo',{timeout:30000});
    console.log('arsenal:',JSON.stringify(a1),JSON.stringify(a2));
    if(a1.esc!=='tempera'||a1.clipe!=='inspecao'||!/Têmpera/.test(a1.sel)) erros.push('Arsenal não trocou a skin da carabina: '+JSON.stringify(a1));
    if(a2.esc!=='martir'||a2.arma!=='revolver'||a2.clipe!=='inspecao') erros.push('Arsenal não trocou a skin do revólver: '+JSON.stringify(a2));
    // volta às de serviço, para as fotos seguintes serem as de sempre
    await page.evaluate(()=>{ for(const k of ['carabina','revolver','cetro']) __T.ARMA.trocarSkin(k,__T.SKINS.SKINS[k][0].id); }); }
  await page.addStyleTag({content:'#scr{display:none!important}'}); // sem mouse capturado o jogo mostra a pausa por cima
  await page.evaluate(()=>{ __T.startLocal(); });
  await page.waitForFunction(()=>__T.game&&__T.game.phase==='intro',null,{timeout:30000});
  await page.evaluate(()=>{ __T.sendAct({type:'ready'}); });
  await page.waitForFunction(()=>__T.game.phase==='play',null,{timeout:30000});
  await page.waitForTimeout(800); // o primeiro quadro da partida alinha a câmera ao spawn; só depois as vistas valem
  const medidas=[];
  for(const v of VISTAS){
    await page.evaluate(v=>{ const m=__T.game.actors.find(a=>a.cid==='local'); m.x=v.x; m.z=v.z; m.invuln=999; m.hp=m.maxHp; __T.LOOK.yaw=v.yaw; __T.LOOK.pitch=v.pitch; m.inp.yaw=v.yaw; m.inp.pitch=v.pitch; for(const a of __T.game.actors) if(a!==m){ a.x=a.team==='H'?-125:125; a.z=a.team==='H'?-8:8; } },v);
    await page.waitForTimeout(1500);
    const info=await page.evaluate(()=>__T.info());
    medidas.push({vista:v.nome,...info});
    await page.screenshot({path:path.join(OUT,v.nome+'.png')});
  }
  // Rastreador: pegadas de um Cultista que passou há pouco, marcas de arrasto num altar e um Cultista de Véu a 6 m
  await page.evaluate(()=>{ const g=__T.game, m=g.actors.find(a=>a.cid==='local'); if(m.team!=='H') return; m.cls='rastreador'; m.x=-30; m.z=-28; __T.LOOK.yaw=-Math.PI/2; __T.LOOK.pitch=-.35; m.inp.yaw=__T.LOOK.yaw; m.inp.pitch=__T.LOOK.pitch;
    for(let k=0;k<14;k++) g.pegadas.push({x:-27+k*.65,z:-28+(k%2?.18:-.18),yaw:-Math.PI/2,t:g.t-k*.8});
    g.rastros.push({de:1,para:0,x:-22,z:-25,t:g.rt,visto:true,lido:true});
    const c=g.actors.find(a=>a.team==='C'); c.x=-24; c.z=-30; c.veuT=30; c.invuln=99; });
  await page.waitForTimeout(1200); await page.screenshot({path:path.join(OUT,'rastreador-veu.png')});
  // Grande Ritual na Abside, visto da Ponte Grande: céu e lua de sangue, vitrais acesos, feixe largo sobre o altar
  await page.evaluate(()=>{ const g=__T.game, m=g.actors.find(a=>a.cid==='local'), A=g.altars[1]; window.__antes={...A};
    Object.assign(A,{state:'active',grande:true,localized:true,prog:.4}); g.events.push({type:'grande',altar:1});
    m.x=0; m.z=-4; m.cls='soldado'; __T.LOOK.yaw=0; __T.LOOK.pitch=.35; m.inp.yaw=0; m.inp.pitch=.35; for(const a of g.actors) if(a!==m){ a.x=a.team==='H'?-125:125; a.z=0; } });
  await page.waitForTimeout(6000); await page.screenshot({path:path.join(OUT,'grande-ritual.png')});
  const gr=await page.evaluate(()=>({corpo:document.body.classList.contains('grande'),relogio:document.querySelector('#roundName').textContent,bussola:document.querySelectorAll('#bussola .ct').length,legendas:document.querySelectorAll('#legendas .lg').length}));
  if(!gr.bussola) erros.push('bússola vazia'); if(!gr.legendas) erros.push('o Grande Ritual não gerou legenda de som');
  console.log('grande ritual:',JSON.stringify(gr)); if(!gr.corpo) erros.push('Grande Ritual não mudou a interface');
  await page.evaluate(()=>{ Object.assign(__T.game.altars[1],window.__antes); });
  // mostrador do círculo: ritual localizado na Capela Oeste com 60% e selo em 35%, visto de perto
  await page.evaluate(()=>{ const g=__T.game, m=g.actors.find(a=>a.cid==='local'), A=g.altars[0]; window.__antes0={...A};
    Object.assign(A,{state:'active',localized:true,prog:.6,cp:.25,seal:.35,chosen:true}); m.x=A.x; m.z=A.z+4.2; __T.LOOK.yaw=0; __T.LOOK.pitch=-.95; m.inp.yaw=0; m.inp.pitch=-.95;
    for(const a of g.actors) if(a!==m){ a.x=a.team==='H'?-125:125; a.z=0; } });
  await page.waitForTimeout(5000); await page.screenshot({path:path.join(OUT,'circulo.png')});
  const dial=await page.evaluate(()=>{ const O=__T.AV&&__T.AV[0]; return O?{visivel:O.dial.visible,op:+O.dial.material.uniforms.op.value.toFixed(2),prog:O.dial.material.uniforms.prog.value}:null; });
  console.log('mostrador:',JSON.stringify(dial)); if(!dial||!dial.visivel||dial.prog<.59) erros.push('mostrador do círculo não apareceu');
  // v5.7: interrupção, Farol com nomes e lance da noite, no mesmo altar
  await page.evaluate(()=>{ const g=__T.game, m=g.actors.find(a=>a.cid==='local'), o=g.actors.find(a=>a.team!==m.team);
    g.events.push({type:'interrupcao',tipo:'selo',causa:'queda',a:m.id,alvo:o.id,altar:0,team:null}); });
  await page.waitForFunction(()=>/interrompid/i.test(document.querySelector('#habNota').textContent),null,{timeout:8000}).catch(()=>{});
  const intr=await page.evaluate(()=>({nota:document.querySelector('#habNota').textContent,cor:__T.AV[0].dial.material.uniforms.cF.value.getHexString()})); // a onda some em menos de 1 s; a cor diz que ela foi disparada
  await page.screenshot({path:path.join(OUT,'interrupcao.png')});
  await page.evaluate(()=>{ const g=__T.game, m=g.actors.find(a=>a.cid==='local');
    g.events.push({type:'sealed',altar:0,por:[m.name,'Irmã Vela']},{type:'lance',tipo:'selo_limite',quem:[m.name],altar:0,team:null,v:.93}); });
  await page.waitForFunction(()=>document.querySelector('#lance').classList.contains('on'),null,{timeout:8000}).catch(()=>{});
  const lan=await page.evaluate(()=>({anuncio:document.querySelector('#anuncio').textContent,lance:document.querySelector('#lance').textContent,on:document.querySelector('#lance').classList.contains('on')}));
  console.log('interrupção e lance:',JSON.stringify(intr),JSON.stringify(lan));
  if(!/interrompid/i.test(intr.nota)||intr.cor!=='ff3a5a') erros.push('interrupção sem aviso ou sem piscar o mostrador');
  if(!/selou o ritual/.test(lan.anuncio)||!/Irmã Vela/.test(lan.anuncio)) erros.push('o Farol não anunciou quem selou: '+lan.anuncio);
  if(!lan.on||!/último instante/.test(lan.lance)||!/93%/.test(lan.lance)) erros.push('o lance da noite não apareceu: '+lan.lance);
  await page.screenshot({path:path.join(OUT,'lance.png')});
  // v5.8: as gravações carregaram (nenhuma falhou ao decodificar)
  const total=await page.evaluate(()=>Object.values(window.SONS_RR||{}).reduce((n,l)=>n+l.length,0));
  await page.waitForFunction(t=>__T.AMOS.prontos+__T.AMOS.falhas>=t,total,{timeout:90000}).catch(()=>{});   // 107 gravações na v6
  const som=await page.evaluate(()=>({prontos:__T.AMOS.prontos,falhas:__T.AMOS.falhas}));
  console.log('sons gravados:',JSON.stringify(som),'de',total); if(som.falhas||som.prontos!==total) erros.push('sons gravados não carregaram: '+JSON.stringify(som));
  // v5.8: combate. Um Cultista a 6 m; tiro na cabeça confirmado pelo servidor, depois um golpe no escudo, depois tiro na parede
  const cb=await page.evaluate(()=>{ const g=__T.game, m=g.actors.find(a=>a.cid==='local'), c=g.actors.find(a=>a.team!==m.team);
    m.x=-30; m.z=-28; c.x=-36; c.z=-28; c.st='alive'; c.hp=c.maxHp; c.invuln=99; c.shield=0; __T.LOOK.yaw=Math.PI/2; __T.LOOK.pitch=0; m.inp.yaw=__T.LOOK.yaw; m.inp.pitch=0; return {m:m.id,c:c.id}; });
  await page.waitForTimeout(1500);
  await page.evaluate(o=>{ const g=__T.game, m=g.actors.find(a=>a.id===o.m), c=g.actors.find(a=>a.id===o.c);
    g.events.push({type:'tracer',a:m.id,x0:m.x,y0:1.42,z0:m.z,x1:c.x+.3,y1:1.7,z1:c.z,alvo:c.id,cab:true},{type:'hit',a:m.id,alvo:c.id,derrubou:false}); },cb);
  await page.waitForFunction(id=>{ const mm=__T.actorMeshes.get(id); return mm&&mm.userData.anim.reac; },cb.c,{timeout:8000}).catch(()=>{});
  await page.screenshot({path:path.join(OUT,'combate-acerto.png')});
  const ac=await page.evaluate(id=>({marca:document.querySelector('#hitmark').className,reacao:!!(__T.actorMeshes.get(id)&&__T.actorMeshes.get(id).userData.anim.reac)}),cb.c);
  await page.evaluate(o=>{ __T.game.events.push({type:'hit',a:o.m,alvo:o.c,derrubou:false,escudo:true}); },cb);
  await page.waitForFunction(()=>document.querySelector('#hitmark').className==='escudo',null,{timeout:8000}).catch(()=>{});
  const esc=await page.evaluate(()=>document.querySelector('#hitmark').className);
  await page.evaluate(o=>{ const g=__T.game, m=g.actors.find(a=>a.id===o.m); g.events.push({type:'tracer',a:m.id,x0:m.x,y0:1.42,z0:m.z,x1:m.x-9,y1:1.4,z1:m.z+.5,alvo:null,cab:false}); },cb);
  await page.waitForTimeout(300); await page.screenshot({path:path.join(OUT,'combate-parede.png')});
  console.log('combate:',JSON.stringify(ac),'escudo:',esc);
  if(ac.marca!=='head') erros.push('tiro na cabeça confirmado sem a marca: '+ac.marca); if(!ac.reacao) erros.push('o alvo atingido não reagiu'); if(esc!=='escudo') erros.push('golpe no escudo sem a marca de bloqueio');
  // v5.8: confronto carregado por 10 s (tiros que acertam e que erram, sigilos estourando, ritual em disputa): mede o
  // tempo de CPU por quadro e confere que efeitos, traçadores e geometrias voltam ao normal depois (nada acumula)
  await page.evaluate(()=>__T.zerarQuadros()); await page.waitForTimeout(5000);
  const antes=await page.evaluate(()=>({q:__T.quadros(),geo:__T.info().geometrias,ef:__T.efeitos,tr:__T.tracers})); await page.evaluate(()=>__T.zerarQuadros());
  const A0=await page.evaluate(()=>{ const A=__T.game.altars[0]; return {state:A.state,localized:A.localized,prog:A.prog,seal:A.seal}; });
  await page.evaluate(o=>{ const g=__T.game, m=g.actors.find(a=>a.id===o.m), c=g.actors.find(a=>a.id===o.c), A=g.altars[0]; A.state='active'; A.localized=true; A.prog=.5;
    let k=0; window.__carga=setInterval(()=>{ k++; const y=1.2+Math.random()*.6;
      g.events.push({type:'tracer',a:m.id,x0:m.x,y0:1.42,z0:m.z,x1:c.x+.2,y1:y,z1:c.z+Math.random()*.4-.2,alvo:c.id,cab:y>1.6},{type:'hit',a:m.id,alvo:c.id,derrubou:k%15===0});
      g.events.push({type:'tracer',a:m.id,x0:m.x,y0:1.42,z0:m.z,x1:m.x-6-Math.random()*4,y1:.02,z1:m.z+Math.random()*6-3,alvo:null,cab:false});
      if(k%2) g.events.push({type:'burst',x:m.x-3+Math.random()*2,y:1+Math.random(),z:m.z+Math.random()*4-2});
      if(k%5===0) g.events.push({type:'sfx',k:'shot',x:c.x,z:c.z,a:c.id},{type:'sfx',k:'sigil',x:c.x,z:c.z,a:c.id});
      A.seal=(A.seal+.01)%1; },100); },cb);
  await page.waitForTimeout(10000);
  const durante=await page.evaluate(()=>({q:__T.quadros(),fps:__T.perfil().fps,ef:__T.efeitos,tr:__T.tracers,geo:__T.info().geometrias}));
  await page.screenshot({path:path.join(OUT,'combate-carga.png')});
  await page.evaluate(A0=>{ clearInterval(window.__carga); Object.assign(__T.game.altars[0],A0); },A0);
  await page.waitForTimeout(4000); await page.evaluate(()=>__T.zerarQuadros()); await page.waitForTimeout(5000);
  const depois=await page.evaluate(()=>({q:__T.quadros(),geo:__T.info().geometrias,ef:__T.efeitos,tr:__T.tracers}));
  if(durante.q.logicaP95>Math.max(30,antes.q.logicaP95*3)) erros.push('a lógica do quadro pesou demais no confronto: '+JSON.stringify(durante.q));
  console.log('carga:',JSON.stringify({antes,durante,depois}));
  if(depois.ef>antes.ef+2||depois.tr>antes.tr+2) erros.push('efeitos ou traçadores acumularam depois do confronto: '+JSON.stringify({antes,depois}));
  if(depois.geo>antes.geo+6) erros.push('geometrias acumularam depois do confronto (vazamento): '+antes.geo+' -> '+depois.geo);
  await page.evaluate(o=>{ const c=__T.game.actors.find(a=>a.id===o.c); c.x=125; c.z=8; },cb);
  // v5.8: o ritual como evento. A vista do mostrador começou o ritual (dormente -> ativo): as runas acenderam.
  const rit=await page.evaluate(()=>{ const O=__T.AV[0]; return {st:O.rv.st,runas:O.runas.filter(r=>r.visible).length}; });
  if(rit.st!=='active'||rit.runas<3) erros.push('ritual sem runas acesas: '+JSON.stringify(rit));
  // selo em andamento: o selo sobe por 1,5 s e as runas tremem e douram (a câmera volta para perto do altar)
  await page.evaluate(()=>{ const g=__T.game, m=g.actors.find(a=>a.cid==='local'), A=g.altars[0]; m.x=A.x+1; m.z=A.z+6.5; __T.LOOK.yaw=.15; __T.LOOK.pitch=-.45; m.inp.yaw=.15; m.inp.pitch=-.45;
    for(const a of g.actors) if(a!==m){ a.x=a.team==='H'?-125:125; a.z=0; } });
  await page.waitForTimeout(1200);
  await page.evaluate(()=>{ const A=__T.game.altars[0]; window.__selo=setInterval(()=>{ A.seal=Math.min(.9,A.seal+.012); },50); });
  await page.waitForFunction(()=>__T.AV[0].rv.contest>.3,null,{timeout:8000}).catch(()=>{});
  const cont=await page.evaluate(()=>+__T.AV[0].rv.contest.toFixed(2)); await page.screenshot({path:path.join(OUT,'ritual-contestado.png')});
  await page.evaluate(()=>clearInterval(window.__selo)); if(!(cont>.3)) erros.push('selo subindo não marcou a disputa no altar: '+cont);
  // Farol: as runas convergem e a coluna dourada sobe
  await page.evaluate(()=>{ __T.game.events.push({type:'sealed',altar:0,por:['Teste']}); });
  await page.waitForFunction(()=>__T.AV[0].runas.some(r=>r.userData.converge!=null),null,{timeout:8000}).catch(()=>{});
  const conv=await page.evaluate(()=>__T.AV[0].runas.filter(r=>r.userData.converge!=null).length); await page.waitForTimeout(500); await page.screenshot({path:path.join(OUT,'ritual-selado.png')});
  console.log('ritual:',JSON.stringify(rit),'disputa',cont,'convergindo',conv); if(conv<8) erros.push('o Farol não fechou as runas no altar');
  await page.evaluate(()=>{ Object.assign(__T.game.altars[0],window.__antes0); });
  // eco do coro a leste: a faixa marca os dois altares do setor e a legenda diz de onde vem o coro
  await page.evaluate(()=>{ const g=__T.game; g.eco={setor:'leste',t:g.rt}; g.events.push({type:'eco',setor:'leste',altares:[2,5]}); });
  await page.waitForTimeout(1500);
  const eco=await page.evaluate(()=>({pips:document.querySelectorAll('#strip .pip.eco').length,legenda:[...document.querySelectorAll('#legendas div')].map(d=>d.textContent).join(' | ')}));
  console.log('eco:',JSON.stringify(eco)); if(eco.pips!==2) erros.push('eco não marcou os dois altares do setor'); if(!/Coro distante/.test(eco.legenda)) erros.push('eco sem legenda');
  await page.screenshot({path:path.join(OUT,'eco.png')});
  await page.evaluate(()=>{ __T.game.eco=null; });
  // faixa do topo: descartado, visitado e purgado são marcas diferentes, cada uma com a sua explicação
  await page.evaluate(()=>{ const g=__T.game; window.__know=g.know.slice(); g.know=['descartado','visto','purgado','desperto','?','chamariz']; });
  await page.waitForFunction(()=>/clean/.test((document.querySelector('#strip .pip')||{}).className||''),null,{timeout:15000}).catch(()=>{});
  const pips=await page.evaluate(()=>[...document.querySelectorAll('#strip .pip')].map(p=>({c:p.className,i:p.querySelector('i').textContent,t:p.title})));
  console.log('faixa:',pips.map(p=>p.i+' '+p.t).join(' | '));
  if(!/clean/.test(pips[0].c)||!/seen/.test(pips[1].c)||!/purged/.test(pips[2].c)||pips[0].i===pips[1].i) erros.push('faixa não separa descartado, visitado e purgado: '+JSON.stringify(pips.slice(0,3)));
  await page.screenshot({path:path.join(OUT,'faixa-conhecimento.png')});
  await page.evaluate(()=>{ __T.game.know=window.__know; });
  await page.waitForTimeout(600);
  await page.keyboard.down('Tab'); await page.waitForTimeout(600); await page.screenshot({path:path.join(OUT,'planta.png')}); await page.keyboard.up('Tab');
  // v6: o relógio de velas do altar (metade do ritual, primeira etapa garantida): 4 acesas, as 2 primeiras vermelho-escuro
  { await page.evaluate(()=>{ const g=__T.game, m=g.actors.find(a=>a.cid==='local'), A=g.altars[0]; window.__antesV={...A}; Object.assign(A,{state:'active',localized:true,prog:.5,cp:.25,seal:0,chosen:true});
      m.x=A.x; m.z=A.z+5; __T.LOOK.yaw=0; __T.LOOK.pitch=-.5; m.inp.yaw=0; m.inp.pitch=-.5; for(const a of g.actors) if(a!==m){ a.x=a.team==='H'?-125:125; a.z=0; } });
    await page.waitForFunction(()=>__T.AV[0].velas.filter(v=>v.ch.material.opacity>.35).length===4&&__T.AV[0].velas[0].cor.g<.3,null,{timeout:30000}).catch(()=>{});   // quadros lentos no swiftshader
    const vel=await page.evaluate(()=>__T.AV[0].velas.map(v=>({o:+v.ch.material.opacity.toFixed(2),r:+v.cor.r.toFixed(2),g:+v.cor.g.toFixed(2)})));
    await page.screenshot({path:path.join(OUT,'velas.png')}); await page.evaluate(()=>{ Object.assign(__T.game.altars[0],window.__antesV); });
    const acesas=vel.filter(v=>v.o>.35).length; console.log('velas:',acesas,'acesas',JSON.stringify(vel.slice(0,4)));
    if(acesas!==4) erros.push('o relógio de velas não mostra metade do ritual: '+acesas+' acesas');
    if(!(vel[0].g<.35&&vel[3].g>.45)) erros.push('as velas garantidas não ficaram vermelho-escuro: '+JSON.stringify(vel.slice(0,4))); }
  // v6: reagente físico. Um Cultista segurando E na raiz de mandrágora: a raiz sobe com o progresso; coletado, some
  { const ri=await page.evaluate(()=>{ const g=__T.game, i=g.reagents.findIndex((R,j)=>R.has&&__T.REAGM[j].tipo==='mandragora'), R=g.reagents[i], m=g.actors.find(a=>a.cid==='local'), c=g.actors.find(a=>a.team==='C');
      m.x=R.x; m.z=R.z+3.2; __T.LOOK.yaw=0; __T.LOOK.pitch=-.45; m.inp.yaw=0; m.inp.pitch=-.45; c.x=R.x+1; c.z=R.z; c.st='alive'; c.hold={key:'col'+i,t:__T.Sim.CFG.collect*.8,max:__T.Sim.CFG.collect}; return i; });
    await page.waitForFunction(i=>__T.REAGM[i].k>.5,ri,{timeout:30000}).catch(()=>{}); const k=await page.evaluate(i=>({k:__T.REAGM[i].k,y:__T.REAGM[i].raiz.position.y}),ri); await page.screenshot({path:path.join(OUT,'reagente-coleta.png')});
    await page.evaluate(i=>{ const g=__T.game, c=g.actors.find(a=>a.team==='C'); c.hold={key:'',t:0,max:0}; g.reagents[i].has=false; },ri); await page.waitForFunction(i=>!__T.REAGM[i].g.visible,ri,{timeout:30000}).catch(()=>{});
    const some=await page.evaluate(i=>!__T.REAGM[i].g.visible,ri); await page.evaluate(i=>{ __T.game.reagents[i].has=true; },ri);
    console.log('reagente:',JSON.stringify(k),'some ao coletar:',some); if(!(k.k>.5&&k.y>.1)) erros.push('a raiz não subiu com o progresso da coleta: '+JSON.stringify(k)); if(!some) erros.push('o reagente coletado continuou no mundo'); }
  // v6: execução. O jogador segura E sobre um Cultista caído; quando o servidor confirma a morte, o fecho aparece
  { const ex=await page.evaluate(()=>{ const g=__T.game, m=g.actors.find(a=>a.cid==='local'), c=g.actors.find(a=>a.team==='C'); m.x=-30; m.z=-28; c.x=-30; c.z=-29.4; c.st='down'; c.hp=0; c.downT=20; c.invuln=0;
      m.hold={key:'exe'+c.id,t:4.1,max:4.5}; __T.LOOK.yaw=0; __T.LOOK.pitch=-.7; m.inp.yaw=0; m.inp.pitch=-.7; window.__fx0=__T.fxExecucoes(); return {c:c.id}; });
    await page.waitForFunction(()=>__T.ARMA.S.est.nome==='executar',null,{timeout:30000}).catch(()=>{}); await page.waitForTimeout(600); const pose=await page.evaluate(()=>__T.ARMA.S.est.nome); await page.screenshot({path:path.join(OUT,'execucao-segurando.png')});
    await page.evaluate(o=>{ const g=__T.game, m=g.actors.find(a=>a.cid==='local'), c=g.actors.find(a=>a.id===o.c); m.hold={key:'',t:0,max:0}; c.st='dead'; c.respawnT=30; },ex);
    await page.waitForFunction(()=>__T.fxExecucoes()>window.__fx0,null,{timeout:30000}).catch(()=>{}); await page.waitForTimeout(300); await page.screenshot({path:path.join(OUT,'execucao-fecho.png')}); const n=await page.evaluate(()=>__T.fxExecucoes()-window.__fx0);
    await page.evaluate(o=>{ const c=__T.game.actors.find(a=>a.id===o.c); c.st='alive'; c.hp=c.maxHp; c.x=125; c.z=8; },ex);
    console.log('execução: pose',pose,'fechos',n); if(pose!=='executar') erros.push('a primeira pessoa não entrou na pose de execução: '+pose); if(n!==1) erros.push('o fecho da execução não apareceu: '+n); }
  // partida acelerada: despausa, avança a simulação em blocos de 30 s e deixa o cliente desenhar e tratar os eventos entre eles
  await page.evaluate(()=>{ const b=document.querySelector('#bRes'); if(b) b.click(); const m=__T.game.actors.find(a=>a.cid==='local'); m.x=-122; m.z=-22; m.invuln=0; });
  let fases=[];
  for(let k=0;k<40;k++){ const r=await page.evaluate(()=>{ const g=__T.game; if(g.phase==='play') for(let i=0;i<900&&g.phase==='play';i++) __T.Sim.step(g,1/30); return {ph:g.phase,rt:Math.round(g.rt),fendas:g.altars.filter(A=>A.state==='fenda').length,far:g.altars.filter(A=>A.state==='farol').length}; });
    fases.push(r); await page.waitForTimeout(400); if(r.ph!=='play') break; }
  const ult=fases[fases.length-1]; console.log(`partida acelerada: ${fases.length} blocos, terminou em ${ult.ph} aos ${ult.rt} s, ${ult.fendas} Fendas, ${ult.far} Faróis`);
  await page.screenshot({path:path.join(OUT,'fim.png')});
  // até o resultado: a tela final precisa abrir (a noite única quebrava aqui) com a linha do tempo da noite
  if(ult.ph==='summary'){ await page.evaluate(()=>{ document.querySelectorAll('style').forEach(st=>{ if(st.textContent.includes('#scr{display:none')) st.remove(); });
      __T.game.log.push({t:100,r:1,ev:'lance',tipo:'chamariz',quem:['b2'],altar:'Menires',team:'C',v:31}); __T.sendAct({type:'ready'}); });
    await page.waitForFunction(()=>__T.game.phase==='final',null,{timeout:30000}); await page.waitForTimeout(2500);
    const fin=await page.evaluate(()=>({titulo:(document.querySelector('#scrBody h2')||{}).textContent||'',grafico:!!document.querySelector('svg.linhaTempo'),lances:[...document.querySelectorAll('.lances div')].map(d=>d.textContent).join(' | ')}));
    if(!/Chamariz de b2/.test(fin.lances)) erros.push('o resultado não listou os lances da noite: '+fin.lances);
    console.log('resultado:',JSON.stringify(fin)); if(!/Vitória|Empate/.test(fin.titulo)) erros.push('tela de resultado não abriu: '+fin.titulo); if(!fin.grafico) erros.push('resultado sem a linha do tempo');
    await page.screenshot({path:path.join(OUT,'resultado.png')});
    // relatório do playtest: tabelas da noite, perguntas e o texto para copiar
    await page.click('#bRel'); await page.waitForFunction(()=>document.querySelectorAll('.perguntas textarea').length===6,null,{timeout:20000}).catch(()=>{});
    const rel=await page.evaluate(()=>({tabelas:document.querySelectorAll('table.rel').length,perguntas:document.querySelectorAll('.perguntas textarea').length}));
    await page.fill('.perguntas textarea[data-i="0"]','Quando a Lanterna mostrou o Chamariz.');
    const txt=await page.evaluate(()=>__T.textoRelatorio?__T.textoRelatorio():'');
    console.log('relatório:',JSON.stringify(rel)); if(rel.tabelas<3||rel.perguntas!==6) erros.push('relatório do playtest incompleto');
    if(!/Lanterna mostrou/.test(txt)) erros.push('a resposta não entrou no texto do relatório');
    await page.screenshot({path:path.join(OUT,'relatorio.png'),fullPage:true}); }
  console.log('carregamento até o menu:',(boot/1000).toFixed(1),'s');
  console.table(medidas.map(m=>({vista:m.vista,chamadas:m.chamadas,triangulos:m.triangulos,geometrias:m.geometrias})));
  if(erros.length){ console.log('ERROS:'); [...new Set(erros)].slice(0,20).forEach(e=>console.log(' ',e)); }
  else console.log('nenhum erro no console');
  await browser.close();
  process.exit(erros.length?1:0);
})().catch(e=>{ console.error(e); process.exit(2); });
