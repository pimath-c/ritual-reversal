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
  {nome:'acampamento',x:-134.3,z:3,yaw:-Math.PI/2,pitch:.05},
  {nome:'trilha-estreita',x:-86,z:-70.5,yaw:.2,pitch:0},
  {nome:'mata-fechada',x:-119.9,z:-30,yaw:0,pitch:0},
  {nome:'menires',x:-50,z:-78,yaw:0,pitch:-.02},
  {nome:'fachada-sul',x:4,z:68,yaw:.35,pitch:.25},
  {nome:'carvalho-oco',x:40,z:81.3,yaw:Math.PI,pitch:.12},
  {nome:'cemiterio',x:110.9,z:66.2,yaw:Math.PI*.85,pitch:0},
  {nome:'ruinas',x:-113.4,z:-79.6,yaw:0,pitch:.05},
  {nome:'nave-oeste',x:-90,z:-26,yaw:Math.PI,pitch:.12},
  {nome:'corredor-norte',x:-40,z:-31,yaw:-Math.PI/2,pitch:.15},
  {nome:'claustro',x:-30,z:-8,yaw:-.6,pitch:.2},
  {nome:'nartex',x:0,z:56,yaw:0,pitch:.1},
  {nome:'capela-oeste',x:-66,z:-38,yaw:0,pitch:.1},
  {nome:'brecha-norte',x:-35.5,z:-48,yaw:0,pitch:.08},{nome:'brecha-norte-fora',x:-38.5,z:-71,yaw:Math.PI,pitch:.1}];
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
  await page.evaluate(()=>{ const g=__T.game, m=g.actors.find(a=>a.cid==='local'); if(m.team!=='H') return; m.cls='rastreador'; m.x=-20; m.z=0; __T.LOOK.yaw=-Math.PI/2; __T.LOOK.pitch=-.35; m.inp.yaw=__T.LOOK.yaw; m.inp.pitch=__T.LOOK.pitch;
    for(let k=0;k<14;k++) g.pegadas.push({x:-17+k*.65,z:(k%2?.18:-.18),yaw:-Math.PI/2,t:g.t-k*.8});
    g.rastros.push({de:1,para:0,x:-12,z:3,t:g.rt,visto:true,lido:true});
    const c=g.actors.find(a=>a.team==='C'); c.x=-14; c.z=-2; c.veuT=30; c.invuln=99; });
  await page.waitForTimeout(1200); await page.screenshot({path:path.join(OUT,'rastreador-veu.png')});
  // Grande Ritual na Abside, visto do Claustro: céu e lua de sangue, vitrais acesos, feixe largo sobre o altar
  await page.evaluate(()=>{ const g=__T.game, m=g.actors.find(a=>a.cid==='local'), A=g.altars[1]; window.__antes={...A};
    Object.assign(A,{state:'active',grande:true,localized:true,prog:.4}); g.events.push({type:'grande',altar:1});
    m.x=-6; m.z=-12; m.cls='soldado'; __T.LOOK.yaw=0; __T.LOOK.pitch=.3; m.inp.yaw=0; m.inp.pitch=.3; for(const a of g.actors) if(a!==m){ a.x=a.team==='H'?-125:125; a.z=0; } });
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
  await page.waitForTimeout(1200);
  const pips=await page.evaluate(()=>[...document.querySelectorAll('#strip .pip')].map(p=>({c:p.className,i:p.querySelector('i').textContent,t:p.title})));
  console.log('faixa:',pips.map(p=>p.i+' '+p.t).join(' | '));
  if(!/clean/.test(pips[0].c)||!/seen/.test(pips[1].c)||!/purged/.test(pips[2].c)||pips[0].i===pips[1].i) erros.push('faixa não separa descartado, visitado e purgado: '+JSON.stringify(pips.slice(0,3)));
  await page.screenshot({path:path.join(OUT,'faixa-conhecimento.png')});
  await page.evaluate(()=>{ __T.game.know=window.__know; });
  await page.waitForTimeout(600);
  await page.keyboard.down('Tab'); await page.waitForTimeout(600); await page.screenshot({path:path.join(OUT,'planta.png')}); await page.keyboard.up('Tab');
  // partida acelerada: despausa, avança a simulação em blocos de 30 s e deixa o cliente desenhar e tratar os eventos entre eles
  await page.evaluate(()=>{ const b=document.querySelector('#bRes'); if(b) b.click(); const m=__T.game.actors.find(a=>a.cid==='local'); m.x=-120; m.z=0; m.invuln=0; });
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
